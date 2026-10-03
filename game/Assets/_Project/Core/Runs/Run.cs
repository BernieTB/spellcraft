using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Combat;
using Game.Core.Combat.Log;
using Game.Core.Enemies;
using Game.Core.Randomness;
using Game.Core.SpellLines;

namespace Game.Core.Runs
{
    /// <summary>
    /// One run through one biome: the hero's spell line and reserve, the fights won so far and the outcome. The
    /// player picks a step from <see cref="AvailableSteps"/> and plays it with <see cref="Play"/>, fight after fight,
    /// until the professor is defeated or a fight is lost.
    /// </summary>
    /// <remarks>
    /// <para>Rules (<c>docs/adr/0009-vertical-slice-run-pacing.md</c>, <c>docs/adr/0011-enemy-tiers-and-fight-time-limit.md</c>):</para>
    /// <list type="bullet">
    /// <item>A regular fight is always available. Its encounter is drawn from the biome's pool with the run's seed
    /// when it is played.</item>
    /// <item>The professor is available once <see cref="BiomeDefinition.MinimumRegularFights"/> regular fights are
    /// won; the player may keep fighting before facing them.</item>
    /// <item>An unlocked secret room stays available, so its mini-boss can be fought again.</item>
    /// <item>Every fight starts fresh: the hero has the class's max health and starting shield, whatever happened
    /// before.</item>
    /// <item>Any defeat ends the run, including a fight that reaches <see cref="RunRules.FightTimeLimit"/>. Defeating
    /// the professor wins it.</item>
    /// </list>
    /// <para>
    /// Determinism: draws use a generator seeded with <see cref="Seed"/> on sequence 0, and fight <c>n</c> (from 1)
    /// gets its own generator on sequence <c>n</c>, so the same seed and the same choices give the same run.
    /// </para>
    /// <para>Extension points for later systems, not implemented here:</para>
    /// <list type="bullet">
    /// <item>Level-ups (#70) and objectives (#71) read the <see cref="RunFightReport"/> returned by <see cref="Play"/>.
    /// Passive upgrades (#75) will change the hero's stats used when a fight starts (see <c>CreateHero</c>).</item>
    /// <item>Secret rooms are unlocked by objectives through <see cref="UnlockSecretRoom"/>; their rewards call
    /// <see cref="AddCard"/> and <see cref="IncreaseLineCapacity"/> (#71). Level-up cards also use <see cref="AddCard"/> (#76).</item>
    /// <item>The preparation phase (#82) happens before a step whose <see cref="RunStep.RequiresPreparation"/> is
    /// true, using the line and reserve edits below.</item>
    /// <item>Live line editing during regular fights (#97) will replace the single <see cref="Fight.Run"/> call
    /// with a fight advanced tick by tick.</item>
    /// </list>
    /// </remarks>
    public sealed class Run
    {
        private readonly IRandom _drawRandom;
        private readonly List<CardInstance> _reserve = new List<CardInstance>();
        private readonly ReadOnlyCollection<CardInstance> _readOnlyReserve;
        private readonly List<SecretRoom> _secretRooms = new List<SecretRoom>();
        private SpellLine<CardInstance> _line;
        private int _nextCardInstanceId = 1;

        /// <param name="heroClass">The hero's class: stats, starting line and capacity.</param>
        /// <param name="biome">The biome to play.</param>
        /// <param name="rules">Global rules such as the fight time limit.</param>
        /// <param name="seed">The run's seed. Same seed and same choices, same run.</param>
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        public Run(ClassDefinition heroClass, BiomeDefinition biome, RunRules rules, ulong seed)
        {
            HeroClass = heroClass ?? throw new ArgumentNullException(nameof(heroClass));
            Biome = biome ?? throw new ArgumentNullException(nameof(biome));
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Seed = seed;
            _drawRandom = new Pcg32Random(seed, 0UL);
            _readOnlyReserve = _reserve.AsReadOnly();

            _line = new SpellLine<CardInstance>(heroClass.StartingLineCapacity);
            foreach (var card in heroClass.StartingDeck)
            {
                _line.Add(NewInstance(card));
            }
        }

        /// <summary>The hero's class.</summary>
        public ClassDefinition HeroClass { get; }

        /// <summary>The biome being played.</summary>
        public BiomeDefinition Biome { get; }

        /// <summary>Global rules of the run.</summary>
        public RunRules Rules { get; }

        /// <summary>The run's seed.</summary>
        public ulong Seed { get; }

        /// <summary>Whether the run goes on, was won or was lost.</summary>
        public RunOutcome Outcome { get; private set; } = RunOutcome.InProgress;

        /// <summary>True while the run goes on.</summary>
        public bool IsInProgress => Outcome == RunOutcome.InProgress;

        /// <summary>Number of fights played so far, of any kind.</summary>
        public int FightsPlayed { get; private set; }

        /// <summary>Number of regular fights won. Secret room and professor fights do not count.</summary>
        public int RegularFightsWon { get; private set; }

        /// <summary>True once enough regular fights are won to face the professor.</summary>
        public bool IsProfessorAvailable => RegularFightsWon >= Biome.MinimumRegularFights;

        /// <summary>The spell line's number of slots.</summary>
        public int LineCapacity => _line.Capacity;

        /// <summary>The cards of the spell line, in order. Read-only live view.</summary>
        public IReadOnlyList<CardInstance> Line => _line.Cards;

        /// <summary>
        /// Owned cards that are not in the spell line, in the order they arrived. No size limit
        /// (<c>docs/adr/0014-boss-preparation-and-recap.md</c>). Read-only live view.
        /// </summary>
        public IReadOnlyList<CardInstance> Reserve => _readOnlyReserve;

        /// <summary>
        /// The steps the player can choose now, in a fixed order: the regular fight, unlocked secret rooms in the
        /// order they were unlocked, then the professor when available. Empty once the run is over.
        /// </summary>
        public IReadOnlyList<RunStep> AvailableSteps
        {
            get
            {
                var steps = new List<RunStep>();
                if (!IsInProgress)
                {
                    return steps.AsReadOnly();
                }

                steps.Add(RunStep.RegularFight);
                foreach (var room in _secretRooms)
                {
                    steps.Add(RunStep.SecretRoom(room.Id));
                }

                if (IsProfessorAvailable)
                {
                    steps.Add(RunStep.Professor);
                }

                return steps.AsReadOnly();
            }
        }

        /// <summary>
        /// Plays a step: picks its encounter, fights it with a fresh hero and the current spell line, and updates
        /// the run. A lost or timed-out fight ends the run; a won professor fight wins it.
        /// </summary>
        /// <returns>The encounter fought and the fight's combat log.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="step"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The run is over or the step is not available.</exception>
        public RunFightReport Play(RunStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            EnsureInProgress();
            if (!IsAvailable(step))
            {
                throw new InvalidOperationException($"The step {step} is not available.");
            }

            var encounter = EncounterFor(step);
            FightsPlayed++;
            var fightRandom = new Pcg32Random(Seed, (ulong)FightsPlayed);
            var log = CombatLogRecorder.Record(CreateHero(), encounter.CreateParticipants(), Rules.FightTimeLimit, fightRandom);
            var report = new RunFightReport(step, encounter, log);

            if (!report.HeroWon)
            {
                Outcome = RunOutcome.Defeat;
            }
            else if (step.Kind == RunStepKind.RegularFight)
            {
                RegularFightsWon++;
            }
            else if (step.Kind == RunStepKind.Professor)
            {
                Outcome = RunOutcome.Victory;
            }

            return report;
        }

        /// <summary>
        /// Gives the hero a new copy of a card: at the end of the spell line when a slot is free, otherwise in the
        /// reserve (<c>docs/adr/0012-linked-choices-and-spell-line-editing.md</c>).
        /// </summary>
        /// <returns>The new card instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The run is over.</exception>
        public CardInstance AddCard(CardDefinition card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            EnsureInProgress();
            var instance = NewInstance(card);
            if (_line.IsFull)
            {
                _reserve.Add(instance);
            }
            else
            {
                _line.Add(instance);
            }

            return instance;
        }

        /// <summary>Adds slots to the spell line, for example after a first mini-boss victory (#71).</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="slots"/> is less than 1.</exception>
        /// <exception cref="InvalidOperationException">The run is over.</exception>
        public void IncreaseLineCapacity(int slots)
        {
            if (slots < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(slots), slots, "Add at least one slot.");
            }

            EnsureInProgress();
            var line = new SpellLine<CardInstance>(checked(_line.Capacity + slots));
            foreach (var card in _line.Cards)
            {
                line.Add(card);
            }

            _line = line;
        }

        /// <summary>
        /// Makes a secret room available as a step until the end of the run (#71 calls it when an objective is
        /// completed). Unlocking a room twice does nothing.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="roomId"/> is null, empty or whitespace.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="miniBossEncounter"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The run is over.</exception>
        public void UnlockSecretRoom(string roomId, EncounterDefinition miniBossEncounter)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                throw new ArgumentException("Secret room id cannot be null, empty or whitespace.", nameof(roomId));
            }

            if (miniBossEncounter == null)
            {
                throw new ArgumentNullException(nameof(miniBossEncounter));
            }

            EnsureInProgress();
            if (FindSecretRoom(roomId) == null)
            {
                _secretRooms.Add(new SecretRoom(roomId, miniBossEncounter));
            }
        }

        /// <summary>Moves a card of the spell line to another position, shifting the cards in between.</summary>
        /// <exception cref="ArgumentOutOfRangeException">A position is not valid.</exception>
        /// <exception cref="InvalidOperationException">The run is over.</exception>
        public void MoveInLine(int fromPosition, int toPosition)
        {
            EnsureInProgress();
            _line.Move(fromPosition, toPosition);
        }

        /// <summary>Exchanges a card of the spell line with a card of the reserve; each takes the other's place.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The position or the reserve index is not valid.</exception>
        /// <exception cref="InvalidOperationException">The run is over.</exception>
        public void SwapWithReserve(int linePosition, int reserveIndex)
        {
            EnsureInProgress();
            ValidateReserveIndex(reserveIndex);
            var fromReserve = _reserve[reserveIndex];
            var fromLine = _line.RemoveAt(linePosition);
            _line.Add(fromReserve);
            _line.Move(_line.Count - 1, linePosition);
            _reserve[reserveIndex] = fromLine;
        }

        /// <summary>
        /// Moves a card of the spell line to the end of the reserve. The line keeps at least one card, since a
        /// fight needs one.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="linePosition"/> is not valid.</exception>
        /// <exception cref="InvalidOperationException">The run is over, or it is the line's last card.</exception>
        public void MoveToReserve(int linePosition)
        {
            EnsureInProgress();
            if (_line.Count == 1 && linePosition == 0)
            {
                throw new InvalidOperationException("The spell line must keep at least one card.");
            }

            _reserve.Add(_line.RemoveAt(linePosition));
        }

        /// <summary>Moves a card of the reserve to the end of the spell line.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="reserveIndex"/> is not valid.</exception>
        /// <exception cref="InvalidOperationException">The run is over or the spell line is full.</exception>
        public void MoveFromReserve(int reserveIndex)
        {
            EnsureInProgress();
            ValidateReserveIndex(reserveIndex);
            if (_line.IsFull)
            {
                throw new InvalidOperationException($"The spell line is full (capacity {_line.Capacity}).");
            }

            _line.Add(_reserve[reserveIndex]);
            _reserve.RemoveAt(reserveIndex);
        }

        private bool IsAvailable(RunStep step)
        {
            foreach (var available in AvailableSteps)
            {
                if (available.Equals(step))
                {
                    return true;
                }
            }

            return false;
        }

        private EncounterDefinition EncounterFor(RunStep step)
        {
            switch (step.Kind)
            {
                case RunStepKind.RegularFight:
                    var pool = Biome.RegularEncounters;
                    return pool[_drawRandom.NextInt(0, pool.Count)];
                case RunStepKind.SecretRoom:
                    return FindSecretRoom(step.SecretRoomId).Encounter;
                case RunStepKind.Professor:
                    return Biome.ProfessorEncounter;
                default:
                    throw new InvalidOperationException($"Unknown step kind {step.Kind}.");
            }
        }

        private FightParticipant CreateHero()
        {
            var line = new SpellLine<CardDefinition>(_line.Capacity);
            foreach (var card in _line.Cards)
            {
                line.Add(card.Definition);
            }

            return new FightParticipant(new Combatant(HeroClass.MaxHealth, HeroClass.StartingShield), line);
        }

        private CardInstance NewInstance(CardDefinition card)
        {
            return new CardInstance(_nextCardInstanceId++, card);
        }

        private SecretRoom FindSecretRoom(string roomId)
        {
            foreach (var room in _secretRooms)
            {
                if (string.Equals(room.Id, roomId, StringComparison.Ordinal))
                {
                    return room;
                }
            }

            return null;
        }

        private void ValidateReserveIndex(int reserveIndex)
        {
            if (reserveIndex < 0 || reserveIndex >= _reserve.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reserveIndex), reserveIndex, $"Index is outside the reserve ({_reserve.Count} cards).");
            }
        }

        private void EnsureInProgress()
        {
            if (!IsInProgress)
            {
                throw new InvalidOperationException($"The run is over ({Outcome}).");
            }
        }

        private sealed class SecretRoom
        {
            public SecretRoom(string id, EncounterDefinition encounter)
            {
                Id = id;
                Encounter = encounter;
            }

            public string Id { get; }

            public EncounterDefinition Encounter { get; }
        }
    }
}
