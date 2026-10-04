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
using Game.Core.Upgrades;

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
    /// <item>Secret rooms are unlocked by objectives, "defeat N of enemy X", counted over regular fights won
    /// (<c>docs/adr/0010-secret-rooms-and-mini-boss-rewards.md</c>). The first victory over a room's mini-boss gives
    /// its line slots, its unique card and a revelation about the professor; a repeat fight gives XP only.</item>
    /// <item>Every fight starts fresh: the hero has the class's max health and starting shield, whatever happened
    /// before.</item>
    /// <item>Any defeat ends the run, including a fight that reaches <see cref="RunRules.FightTimeLimit"/>. Defeating
    /// the professor wins it.</item>
    /// <item>A won fight gives the XP of every enemy of its encounter, whatever the step (regular fight, secret room
    /// fought again, professor). Levels follow <see cref="RunRules.LevelCurve"/>; each level reached adds a pending
    /// level-up for the linked choice (#76). A lost fight gives no XP.</item>
    /// </list>
    /// <para>
    /// Determinism: every random source of the run is a <see cref="Pcg32Random"/> seeded with <see cref="Seed"/>, on
    /// its own sequence, so the same seed and the same choices give the same run:
    /// </para>
    /// <list type="bullet">
    /// <item>sequence <see cref="EncounterDrawSequence"/> (0): drawing regular encounters;</item>
    /// <item>sequence <see cref="LevelUpOfferSequence"/> (1): reserved for level-up offers (#76);</item>
    /// <item>sequence <see cref="FirstFightSequence"/> + k (2 + k): fight number k, counted from 0.</item>
    /// </list>
    /// <para>
    /// Sequences stay below 2^63: <see cref="Pcg32Random"/> shifts the sequence left by one bit, so larger values would
    /// share a stream with smaller ones.
    /// </para>
    /// <para>Extension points for later systems, not implemented here:</para>
    /// <list type="bullet">
    /// <item>Level-up offers (#76) read <see cref="PendingLevelUps"/> and call <see cref="ConsumePendingLevelUp"/>,
    /// then <see cref="TakeUpgrade"/> and <see cref="AddCard"/>.</item>
    /// <item>The report returned by <see cref="Play"/> or <see cref="RunFightSession.Complete"/> tells which rooms the
    /// fight unlocked and what a first victory gave (<see cref="RunFightReport.SecretRoomRewards"/>), whose
    /// revelation the caller records in the bestiary and saves.</item>
    /// <item>The preparation phase (#82) happens before a step whose <see cref="RunStep.RequiresPreparation"/> is
    /// true, using the line and reserve edits below.</item>
    /// </list>
    /// <para>
    /// Passive upgrades (#75): the run holds the hero's <see cref="Upgrades"/>, applied to the hero of every fight,
    /// including the reserve cards the player may swap in during a regular fight. Live line editing (#97): a regular
    /// fight is played with <see cref="BeginFight"/>, which returns a <see cref="RunFightSession"/> advanced tick by tick
    /// with the line editable; <see cref="Play"/> is the same fight run to its end with no change (simulation,
    /// tests). Mini-boss and professor fights never allow line edits (<see cref="RunStep.AllowsLineEditing"/>).
    /// While a session is open, nothing else may change the run.
    /// </para>
    /// </remarks>
    public sealed class Run
    {
        /// <summary>Generator sequence used to draw regular encounters.</summary>
        public const ulong EncounterDrawSequence = 0UL;

        /// <summary>Generator sequence reserved for level-up offers (#76).</summary>
        public const ulong LevelUpOfferSequence = 1UL;

        /// <summary>Generator sequence of the first fight; fight k (from 0) uses this value + k.</summary>
        public const ulong FirstFightSequence = 2UL;

        private Pcg32Random _drawRandom;
        private readonly List<CardInstance> _reserve = new List<CardInstance>();
        private readonly ReadOnlyCollection<CardInstance> _readOnlyReserve;
        private readonly List<SecretRoom> _secretRooms = new List<SecretRoom>();
        private readonly List<RoomState> _roomStates = new List<RoomState>();
        private readonly SpellLine<CardInstance> _line;
        private int _nextCardInstanceId = 1;
        private PassiveUpgradeSet _upgrades = PassiveUpgradeSet.Empty;
        private RunFightSession _currentFight;

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
            _drawRandom = new Pcg32Random(seed, EncounterDrawSequence);
            _readOnlyReserve = _reserve.AsReadOnly();

            _line = new SpellLine<CardInstance>(heroClass.StartingLineCapacity);
            foreach (var card in heroClass.StartingDeck)
            {
                _line.Add(NewInstance(card));
            }

            foreach (var room in biome.SecretRooms)
            {
                _roomStates.Add(new RoomState(room));
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

        /// <summary>Total XP earned since the start of the run.</summary>
        public long TotalXp { get; private set; }

        /// <summary>The hero's level, from <see cref="TotalXp"/> and <see cref="RunRules.LevelCurve"/>. Starts at 1.</summary>
        public int Level { get; private set; } = 1;

        /// <summary>
        /// Levels reached whose linked choice has not been taken yet. Each level reached adds one; the level-up offer
        /// (#76) removes one with <see cref="ConsumePendingLevelUp"/>.
        /// </summary>
        public int PendingLevelUps { get; private set; }

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

        /// <summary>The passive upgrades taken so far, applied to the hero of every fight (#75).</summary>
        public PassiveUpgradeSet Upgrades => _upgrades;

        /// <summary>
        /// The biome's secret rooms with the run's progress on each: objective progress, whether the room is open and
        /// whether its mini-boss was already beaten. In the biome's listing order. A snapshot: read it again after a fight.
        /// </summary>
        public IReadOnlyList<SecretRoomStatus> SecretRooms
        {
            get
            {
                var statuses = new List<SecretRoomStatus>(_roomStates.Count);
                foreach (var state in _roomStates)
                {
                    statuses.Add(new SecretRoomStatus(state.Definition, state.Progress, state.IsUnlocked, state.IsCleared));
                }

                return statuses.AsReadOnly();
            }
        }

        /// <summary>The fight being played tick by tick, or null when no session is open.</summary>
        public RunFightSession CurrentFight => _currentFight;

        /// <summary>
        /// The steps the player can choose now, in a fixed order: the regular fight, unlocked secret rooms in the
        /// order they were unlocked, then the professor when available. Empty once the run is over. It does not look
        /// at an open fight session, which blocks every step: the screen tests <see cref="CurrentFight"/> first.
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
        /// Plays a step to its end with no line change: picks its encounter, fights it with a fresh hero (upgrades
        /// applied) and the current spell line, and updates the run. A lost or timed-out fight ends the run; a won
        /// professor fight wins it. If the fight throws, the run is left unchanged (no draw is consumed and the fight
        /// is not counted). To let the player edit the line during a regular fight, use <see cref="BeginFight"/>.
        /// </summary>
        /// <returns>The encounter fought and the fight's combat log.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="step"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// The run is over, the step is not available, or a fight session is open.
        /// </exception>
        public RunFightReport Play(RunStep step)
        {
            var session = BeginFight(step);
            try
            {
                session.RunToEnd();
                return session.Complete();
            }
            catch
            {
                session.Cancel();
                throw;
            }
        }

        /// <summary>
        /// Starts a step as a fight advanced tick by tick: picks its encounter, builds the fight with a fresh hero
        /// (upgrades applied) and the current spell line and reserve, and opens a <see cref="RunFightSession"/>. The
        /// run only counts the fight, and the random draw, when the session is completed. For a regular fight the
        /// session allows line edits; for a secret room or the professor the line is fixed
        /// (<see cref="RunStep.AllowsLineEditing"/>).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="step"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// The run is over, the step is not available, or a fight session is already open.
        /// </exception>
        public RunFightSession BeginFight(RunStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            EnsureCanChange();
            if (!IsAvailable(step))
            {
                throw new InvalidOperationException($"The step {step} is not available.");
            }

            var drawRandom = _drawRandom.Clone();
            var encounter = EncounterFor(step, drawRandom);
            var startLine = new List<CardInstance>(_line.Cards);
            var startReserve = new List<CardInstance>(_reserve);
            var fightRandom = new Pcg32Random(Seed, FirstFightSequence + (ulong)FightsPlayed);
            var hero = CreateHero();
            var enemies = encounter.CreateParticipants();

            // Card evolution (ADR 0013): the fight counts the casts of every card copy of the hero, line and reserve,
            // from the counts the copies already have. The session hands the final counts back to the instances.
            var fight = new Fight(
                hero,
                enemies,
                Rules.FightTimeLimit,
                fightRandom,
                CreateFightReserve(),
                step.AllowsLineEditing,
                CastsOf(startLine),
                CastsOf(startReserve));

            var participants = new List<FightParticipant>(enemies.Count + 1) { hero };
            participants.AddRange(enemies);
            var snapshots = CombatLogRecorder.Snapshot(participants);

            _currentFight = new RunFightSession(
                this, step, encounter, fight, participants, snapshots, startLine, startReserve, drawRandom);
            return _currentFight;
        }

        /// <summary>
        /// Marks one pending level-up as handled, once its linked choice is taken (#76). Allowed after the run ends,
        /// so a level reached in the last fight can still be shown, and while a fight session is open (confirmed by
        /// the owner on 2026-10-04): a passive upgrade taken then applies from the next fight, not to the one playing.
        /// </summary>
        /// <exception cref="InvalidOperationException">There is no pending level-up.</exception>
        public void ConsumePendingLevelUp()
        {
            if (PendingLevelUps == 0)
            {
                throw new InvalidOperationException("There is no pending level-up.");
            }

            PendingLevelUps--;
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

            EnsureCanChange();
            return AddCardCore(card);
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

            EnsureCanChange();
            _line.IncreaseCapacity(slots);
        }

        /// <summary>
        /// Gives the hero a passive upgrade for the rest of the run (#75): it is applied to the hero of every later
        /// fight. Upgrades stack. Level-up offers (#76) call it with the chosen package.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="upgrade"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The run is over or a fight session is open.</exception>
        /// <exception cref="OverflowException">A total of the upgrades exceeds <see cref="int.MaxValue"/>.</exception>
        public void TakeUpgrade(PassiveUpgrade upgrade)
        {
            if (upgrade == null)
            {
                throw new ArgumentNullException(nameof(upgrade));
            }

            EnsureCanChange();
            _upgrades = _upgrades.With(upgrade);
        }

        /// <summary>
        /// Makes a secret room available as a step until the end of the run. Objectives unlock the biome's rooms by
        /// themselves when they are completed; this opens one at once (tests, tools). Unlocking a room again with the
        /// same encounter does nothing. Encounters are compared by id. A room the biome defines must be unlocked with
        /// its own mini-boss encounter.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="roomId"/> is null, empty or whitespace.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="miniBossEncounter"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// The run is over, or the room is already unlocked with another encounter.
        /// </exception>
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

            EnsureCanChange();

            // Encounters are compared by id, not by reference: data converted twice gives equal but distinct objects.
            var state = FindRoomState(roomId);
            if (state != null && !SameEncounter(state.Definition.MiniBossEncounter, miniBossEncounter))
            {
                throw new InvalidOperationException(
                    $"Secret room '{roomId}' of the biome has encounter '{state.Definition.MiniBossEncounter.Id}', not '{miniBossEncounter.Id}'.");
            }

            var existing = FindSecretRoom(roomId);
            if (existing == null)
            {
                // A room the biome defines always opens with the biome's own encounter definition.
                _secretRooms.Add(new SecretRoom(roomId, state != null ? state.Definition.MiniBossEncounter : miniBossEncounter));
            }
            else if (!SameEncounter(existing.Encounter, miniBossEncounter))
            {
                throw new InvalidOperationException(
                    $"Secret room '{roomId}' is already unlocked with encounter '{existing.Encounter.Id}'.");
            }

            if (state != null)
            {
                state.IsUnlocked = true;
            }
        }

        /// <summary>Moves a card of the spell line to another position, shifting the cards in between.</summary>
        /// <exception cref="ArgumentOutOfRangeException">A position is not valid.</exception>
        /// <exception cref="InvalidOperationException">The run is over.</exception>
        public void MoveInLine(int fromPosition, int toPosition)
        {
            EnsureCanChange();
            _line.Move(fromPosition, toPosition);
        }

        /// <summary>Exchanges a card of the spell line with a card of the reserve; each takes the other's place.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The position or the reserve index is not valid.</exception>
        /// <exception cref="InvalidOperationException">The run is over.</exception>
        public void SwapWithReserve(int linePosition, int reserveIndex)
        {
            EnsureCanChange();
            SwapCore(linePosition, reserveIndex);
        }

        /// <summary>
        /// Moves a card of the spell line to the end of the reserve. The line keeps at least one card, since a
        /// fight needs one.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="linePosition"/> is not valid.</exception>
        /// <exception cref="InvalidOperationException">The run is over, or it is the line's last card.</exception>
        public void MoveToReserve(int linePosition)
        {
            EnsureCanChange();
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
            EnsureCanChange();
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

        private EncounterDefinition EncounterFor(RunStep step, IRandom drawRandom)
        {
            switch (step.Kind)
            {
                case RunStepKind.RegularFight:
                    var pool = Biome.RegularEncounters;
                    return pool[drawRandom.NextInt(0, pool.Count)];
                case RunStepKind.SecretRoom:
                    return FindSecretRoom(step.SecretRoomId).Encounter;
                case RunStepKind.Professor:
                    return Biome.ProfessorEncounter;
                default:
                    throw new InvalidOperationException($"Unknown step kind {step.Kind}.");
            }
        }

        private static long XpRewardOf(EncounterDefinition encounter)
        {
            var xp = 0L;
            foreach (var enemy in encounter.Enemies)
            {
                xp = checked(xp + enemy.XpReward);
            }

            return xp;
        }

        private FightParticipant CreateHero()
        {
            // Each card is at the stage its casts have reached; the upgrades then apply to every stage of it
            // (CardDefinition.MapForms), so an upgraded card keeps its evolutions.
            var line = new SpellLine<CardDefinition>(_line.Capacity);
            foreach (var card in _line.Cards)
            {
                line.Add(card.CurrentDefinition);
            }

            var hero = new FightParticipant(new Combatant(HeroClass.MaxHealth, HeroClass.StartingShield), line);
            return _upgrades.ApplyTo(hero);
        }

        // The reserve a fight lets the player swap in: the same cards in the same order, upgraded like the line.
        private List<CardDefinition> CreateFightReserve()
        {
            var reserve = new List<CardDefinition>(_reserve.Count);
            foreach (var card in _reserve)
            {
                reserve.Add(_upgrades.ApplyTo(card.CurrentDefinition));
            }

            return reserve;
        }

        private void SwapCore(int linePosition, int reserveIndex)
        {
            ValidateReserveIndex(reserveIndex);
            var fromReserve = _reserve[reserveIndex];
            var fromLine = _line.RemoveAt(linePosition);
            _line.Add(fromReserve);
            _line.Move(_line.Count - 1, linePosition);
            _reserve[reserveIndex] = fromLine;
        }

        // Called by the open session after the fight accepted a line change: the run's own card instances follow.
        internal void MirrorLineChange(LineChange change)
        {
            if (change.Kind == LineChangeKind.Move)
            {
                if (change.Position != change.ToPosition)
                {
                    _line.Move(change.Position, change.ToPosition);
                }
            }
            else
            {
                SwapCore(change.Position, change.ReserveIndex);
            }
        }

        // Called by the session when it completes: counts the fight and updates XP, level and outcome.
        internal RunFightReport CommitFight(RunFightSession session, CombatLog log)
        {
            var step = session.Step;
            var encounter = session.Encounter;
            var won = log.Winner == FightWinner.Hero;
            var xpGained = won ? XpRewardOf(encounter) : 0L;
            var totalXp = checked(TotalXp + xpGained);
            var level = Rules.LevelCurve.LevelForTotalXp(totalXp);
            var levelsGained = level - Level;

            // Everything that can throw is built here, before the run changes: the objective and reward plans and the
            // report. The changes are applied below, none of which can throw, so a failure leaves the run unchanged.
            var objectiveUpdates = new List<ObjectiveUpdate>();
            RoomState clearedRoom = null;
            CardInstance rewardCard = null;
            SecretRoomRewards rewards = null;
            if (won && step.Kind == RunStepKind.RegularFight)
            {
                PlanObjectives(encounter, objectiveUpdates);
            }
            else if (won && step.Kind == RunStepKind.SecretRoom)
            {
                var state = FindRoomState(step.SecretRoomId);
                if (state != null && !state.IsCleared)
                {
                    var room = state.Definition;
                    _ = checked(_line.Capacity + room.BonusLineSlots);
                    clearedRoom = state;
                    rewardCard = new CardInstance(_nextCardInstanceId, room.UniqueCard);
                    rewards = new SecretRoomRewards(room.Id, room.BonusLineSlots, rewardCard, Biome.Professor, room.Revelation);
                }
            }

            var unlockedRoomIds = new List<string>();
            foreach (var update in objectiveUpdates)
            {
                if (update.Unlocks)
                {
                    unlockedRoomIds.Add(update.State.Definition.Id);
                }
            }

            var report = new RunFightReport(
                step, encounter, session.StartLine, log, xpGained, levelsGained, unlockedRoomIds, rewards);

            // Card evolution (ADR 0013): every card copy of the hero keeps the casts the fight counted for it. The
            // counts follow the copies' starting order (line, then reserve), wherever line changes moved them.
            var casts = log.HeroCardCasts;
            var startLine = session.StartLine;
            var startReserve = session.StartReserve;
            if (casts.Count != startLine.Count + startReserve.Count)
            {
                throw new InvalidOperationException(
                    $"The fight counted the casts of {casts.Count} card copies, the run has {startLine.Count + startReserve.Count}.");
            }

            // Every count is checked before any is applied, so a failure leaves the run unchanged.
            for (var i = 0; i < startLine.Count; i++)
            {
                startLine[i].EnsureCanSetCasts(casts[i]);
            }

            for (var i = 0; i < startReserve.Count; i++)
            {
                startReserve[i].EnsureCanSetCasts(casts[startLine.Count + i]);
            }

            for (var i = 0; i < startLine.Count; i++)
            {
                startLine[i].SetCasts(casts[i]);
            }

            for (var i = 0; i < startReserve.Count; i++)
            {
                startReserve[i].SetCasts(casts[startLine.Count + i]);
            }

            _drawRandom = session.DrawRandomAfter;
            _currentFight = null;
            FightsPlayed++;
            TotalXp = totalXp;
            PendingLevelUps += levelsGained;
            Level = level;

            if (!won)
            {
                Outcome = RunOutcome.Defeat;
            }
            else if (step.Kind == RunStepKind.RegularFight)
            {
                RegularFightsWon++;
                ApplyObjectives(objectiveUpdates);
            }
            else if (step.Kind == RunStepKind.SecretRoom)
            {
                ApplyRewards(clearedRoom, rewardCard);
            }
            else if (step.Kind == RunStepKind.Professor)
            {
                Outcome = RunOutcome.Victory;
            }

            return report;
        }

        // A regular fight was won: every defeated enemy counts for the objectives still in progress, and a room whose
        // objective is complete opens. This only plans the changes, in the biome's listing order; ApplyObjectives
        // makes them.
        private void PlanObjectives(EncounterDefinition encounter, List<ObjectiveUpdate> updates)
        {
            foreach (var state in _roomStates)
            {
                if (state.IsUnlocked)
                {
                    continue;
                }

                var objective = state.Definition.Objective;
                var progress = state.Progress;
                foreach (var enemy in encounter.Enemies)
                {
                    if (string.Equals(enemy.Id, objective.EnemyId, StringComparison.Ordinal))
                    {
                        progress = Math.Min(objective.Count, progress + 1);
                    }
                }

                updates.Add(new ObjectiveUpdate(state, progress, progress >= objective.Count));
            }
        }

        private void ApplyObjectives(List<ObjectiveUpdate> updates)
        {
            foreach (var update in updates)
            {
                update.State.Progress = update.Progress;
                if (!update.Unlocks)
                {
                    continue;
                }

                update.State.IsUnlocked = true;
                if (FindSecretRoom(update.State.Definition.Id) == null)
                {
                    _secretRooms.Add(new SecretRoom(update.State.Definition.Id, update.State.Definition.MiniBossEncounter));
                }
            }
        }

        // A secret room's mini-boss was beaten for the first time (the card was planned in CommitFight): the room is
        // cleared, the line gets its slots, then the unique card joins the end of the line, or the reserve when the
        // line is still full. XP and levels are committed by the same call, after the slots and the card.
        private void ApplyRewards(RoomState clearedRoom, CardInstance rewardCard)
        {
            if (clearedRoom == null)
            {
                return;
            }

            clearedRoom.IsCleared = true;
            _nextCardInstanceId++;
            if (clearedRoom.Definition.BonusLineSlots > 0)
            {
                _line.IncreaseCapacity(clearedRoom.Definition.BonusLineSlots);
            }

            if (_line.IsFull)
            {
                _reserve.Add(rewardCard);
            }
            else
            {
                _line.Add(rewardCard);
            }
        }

        // Called by the session when it is cancelled: the run is free again; nothing was counted or drawn.
        internal void ReleaseFight(RunFightSession session)
        {
            if (ReferenceEquals(_currentFight, session))
            {
                _currentFight = null;
            }
        }

        private static int[] CastsOf(IReadOnlyList<CardInstance> cards)
        {
            var casts = new int[cards.Count];
            for (var i = 0; i < casts.Length; i++)
            {
                casts[i] = cards[i].Casts;
            }

            return casts;
        }

        private CardInstance NewInstance(CardDefinition card)
        {
            return new CardInstance(_nextCardInstanceId++, card);
        }

        // Gives a new copy of a card: the end of the line when a slot is free, otherwise the reserve.
        private CardInstance AddCardCore(CardDefinition card)
        {
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

        private static bool SameEncounter(EncounterDefinition a, EncounterDefinition b)
        {
            return string.Equals(a.Id, b.Id, StringComparison.Ordinal);
        }

        private RoomState FindRoomState(string roomId)
        {
            foreach (var state in _roomStates)
            {
                if (string.Equals(state.Definition.Id, roomId, StringComparison.Ordinal))
                {
                    return state;
                }
            }

            return null;
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

        // Every change of the run goes through this: the run must be going on and no fight session may be open (the
        // session owns the line, the reserve and the random draw until it is completed or cancelled).
        private void EnsureCanChange()
        {
            EnsureInProgress();
            if (_currentFight != null)
            {
                throw new InvalidOperationException("A fight session is open: complete or cancel it first.");
            }
        }

        // The run's progress on one of the biome's secret rooms.
        private sealed class RoomState
        {
            public RoomState(SecretRoomDefinition definition)
            {
                Definition = definition;
            }

            public SecretRoomDefinition Definition { get; }

            public int Progress { get; set; }

            public bool IsUnlocked { get; set; }

            public bool IsCleared { get; set; }
        }

        // A planned change of one room's objective progress.
        private sealed class ObjectiveUpdate
        {
            public ObjectiveUpdate(RoomState state, int progress, bool unlocks)
            {
                State = state;
                Progress = progress;
                Unlocks = unlocks;
            }

            public RoomState State { get; }

            public int Progress { get; }

            public bool Unlocks { get; }
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
