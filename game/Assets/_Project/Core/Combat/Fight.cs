using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Cards;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;

namespace Game.Core.Combat
{
    /// <summary>
    /// A deterministic fight between one hero and one or more enemies, each casting its own spell line in a loop.
    /// Create it with the participants, a maximum number of ticks and a seeded random source, then call
    /// <see cref="Run()"/> once, or advance it tick by tick with <see cref="Step"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rules (<c>docs/adr/0002-first-pass-combat-rules.md</c>; its provisional details are confirmed by the owner in
    /// <c>docs/adr/0004-confirm-first-pass-combat-rules.md</c>):
    /// </para>
    /// <list type="bullet">
    /// <item>The fight advances in discrete ticks, numbered from 1. Nothing reads wall-clock time.</item>
    /// <item>Every living combatant casts the cards of its spell line in order, looping from the last card back to
    /// the first, starting at position 0. A card resolves on the tick its cast time has elapsed (a card with cast
    /// time <c>c</c> started on tick <c>s</c> resolves on tick <c>s + c - 1</c>); the next card starts on the
    /// following tick.</item>
    /// <item>On one tick, combatants are processed in a fixed order: the hero first, then enemies in the order
    /// given. A combatant killed earlier in the tick does not resolve its cast.</item>
    /// <item>A dead combatant stops casting. The fight ends as soon as the hero is dead (enemies win) or every
    /// enemy is dead (hero wins), even in the middle of a tick.</item>
    /// <item>If neither happens within the maximum number of ticks, the fight ends with no winner (timeout).</item>
    /// </list>
    /// <para>
    /// Targeting (confirmed by the owner, <c>docs/adr/0004-confirm-first-pass-combat-rules.md</c>): the hero aims at
    /// the first living enemy in order; every enemy aims at the hero. Effects that act on the caster (heal, shield)
    /// ignore the target.
    /// </para>
    /// <para>
    /// Neighbour modifiers (<see cref="NeighbourModifier"/>, <c>docs/adr/0005-neighbour-modifier-resolution.md</c>):
    /// when a card resolves, each of its modifiers adds a pending bonus to the next or previous position of the
    /// caster's own spell line, following the loop (a single card is its own neighbour). Pending bonuses belong to
    /// a position of one combatant, not to a card, so the same card at two positions or in two lines is tracked
    /// separately. A position's pending bonuses add up and are all used up by its next cast, which first takes
    /// them and then grants its own modifiers; a "previous" bonus therefore applies on the next loop. A bonus only
    /// adds to the first effect of its kind in the cast (<see cref="EffectContext.ConsumeBonus"/>). Bonuses waiting
    /// for a dead combatant are never used.
    /// </para>
    /// <para>
    /// Spell lines are copied when the fight is created, so editing a line afterwards does not change the fight.
    /// Combatants are not copied: the fight changes their health and shield.
    /// </para>
    /// <para>
    /// Live line editing (<c>docs/adr/0012-linked-choices-and-spell-line-editing.md</c>): in a fight created with
    /// <c>lineEditsAllowed</c> (regular fights), the player may change the hero's line at the start of any tick with
    /// a <see cref="LineChange"/>: move a card within the line, or swap a line card with a card of the hero's reserve
    /// (given to the fight, copied like the lines). Mini-boss and professor fights are created without it and refuse
    /// every change. A change applies at the start of its tick, before any combatant acts, so it is immediate:
    /// </para>
    /// <list type="bullet">
    /// <item>A cast takes its card and the bonus pending on its position when it starts (its first tick). The card
    /// being cast when the line changes finishes its cast and resolves as started, even if it was moved or swapped
    /// out.</item>
    /// <item>The line then continues from the position after that card. While the card is in the line, that
    /// position follows it when it or another card moves. Once it has gone to the reserve, it is the slot it left,
    /// and it stays that slot even if the incoming card moves later. Its neighbour modifiers aim at the neighbours
    /// of that same position, in the line as it is when the card resolves.</item>
    /// <item>Pending neighbour bonuses stay at their position: the card that now sits there receives them.</item>
    /// <item>Between two casts (the tick after a resolution), no card is being cast: the next cast plays the card
    /// now at the position that was next.</item>
    /// <item>Several changes on the same tick apply in the order given. Moving a card onto its own position is
    /// ignored (no record, no log event).</item>
    /// </list>
    /// <para>
    /// The details above are recorded in <c>docs/adr/0015-live-spell-line-editing-details.md</c> (confirmed by
    /// the owner on 2026-10-04), including that a cast takes its pending bonus when it starts, which refines
    /// ADR 0005. Enemies never change their lines. Applied changes are listed in
    /// <see cref="FightResult.LineChanges"/>, so a run can replay them on its own spell line and reserve.
    /// </para>
    /// <para>
    /// Card evolution (<c>docs/adr/0013-card-evolution.md</c>): a fight created with the casts each card copy of the
    /// hero already has counts the casts of the hero's cards. Every copy keeps its own count wherever line changes
    /// move it. When a resolved cast brings a copy to the casts required by an evolution stage, the card becomes its
    /// evolved form, in the line or in the reserve: the cast that reached the stage resolved with the old form, and
    /// the next cast of the copy uses the new one (same id and cast time, new effects and modifiers; this timing was
    /// confirmed by the owner on 2026-10-04). Each evolution is listed in <see cref="FightResult.Evolutions"/>, and
    /// the final counts in <see cref="FightResult.HeroCardCasts"/>, so a run can keep them for the next fight. Only
    /// the hero's cards evolve, and a fight not given the counts does not count casts or evolve anything.
    /// </para>
    /// <para>
    /// Every resolution produces a <see cref="CastRecord"/> in <see cref="FightResult.Casts"/>; that is the
    /// intended hook for the combat event log. Effect outcomes include neighbour bonuses; the record also carries the
    /// bonus the cast received and the part of it no effect used (<see cref="CastRecord.WastedBonus"/>).
    /// </para>
    /// </remarks>
    public sealed class Fight
    {
        /// <summary>Fight index of the hero. Enemies follow, from 1 to N, in the order given.</summary>
        public const int HeroIndex = 0;

        private readonly Combatant[] _combatants;
        private readonly SpellLine<CardDefinition>[] _spellLines;
        private readonly int[] _positions;
        private readonly int[] _elapsedTicks;
        private readonly EffectBonus[][] _pendingBonuses;
        private readonly CardDefinition[] _castCards;
        private readonly int[] _castPositions;
        private readonly EffectBonus[] _castBonuses;
        private readonly List<CardDefinition> _heroReserve;
        private readonly IReadOnlyList<CardDefinition> _heroReserveView;
        private bool _heroCastLeftLine;
        private readonly List<HeroCardState> _lineStates;
        private readonly List<HeroCardState> _reserveStates;
        private readonly HeroCardState[] _allStates;
        private HeroCardState _heroCastState;
        private readonly List<EvolutionRecord> _evolutions = new List<EvolutionRecord>();
        private IReadOnlyList<EvolutionRecord> _evolutionsView;
        private readonly List<CastRecord> _casts = new List<CastRecord>();
        private readonly List<LineChangeRecord> _lineChanges = new List<LineChangeRecord>();
        private readonly int _maxTicks;
        private FightWinner _winner = FightWinner.None;
        private bool _hasRun;

        /// <param name="hero">The player's side.</param>
        /// <param name="enemies">The enemies, at least one, in resolution order.</param>
        /// <param name="maxTicks">
        /// Guard against endless fights: the fight stops with no winner after this many ticks. From data or the
        /// caller, at least 1.
        /// </param>
        /// <param name="random">
        /// Seeded random source for the fight. No first-pass rule is random, so it is not used yet; it is injected
        /// now so random rules can be added without changing callers.
        /// </param>
        /// <exception cref="ArgumentNullException">An argument or an enemy is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enemies"/> is empty, a spell line is empty, a combatant is already dead, or the same
        /// combatant appears twice.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxTicks"/> is less than 1.</exception>
        public Fight(FightParticipant hero, IReadOnlyList<FightParticipant> enemies, int maxTicks, IRandom random)
            : this(hero, enemies, maxTicks, random, Array.Empty<CardDefinition>(), false)
        {
        }

        /// <summary>
        /// Creates a fight whose hero line may be edited live when <paramref name="lineEditsAllowed"/> is true
        /// (regular fights, ADR 0012).
        /// </summary>
        /// <param name="hero">The player's side.</param>
        /// <param name="enemies">The enemies, at least one, in resolution order.</param>
        /// <param name="maxTicks">Guard against endless fights, at least 1 (see the other constructor).</param>
        /// <param name="random">Seeded random source for the fight.</param>
        /// <param name="heroReserve">
        /// The hero's reserve, in the run's order: the cards a <see cref="LineChangeKind.SwapWithReserve"/> change
        /// can bring into the line. Copied. May be empty.
        /// </param>
        /// <param name="lineEditsAllowed">
        /// True for regular fights; false for mini-boss and professor fights, whose line is fixed.
        /// </param>
        /// <exception cref="ArgumentNullException">An argument, an enemy or a reserve card is null.</exception>
        /// <exception cref="ArgumentException">See the other constructor.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxTicks"/> is less than 1.</exception>
        public Fight(
            FightParticipant hero,
            IReadOnlyList<FightParticipant> enemies,
            int maxTicks,
            IRandom random,
            IReadOnlyList<CardDefinition> heroReserve,
            bool lineEditsAllowed)
            : this(hero, enemies, maxTicks, random, heroReserve, lineEditsAllowed, null, null)
        {
        }

        /// <summary>
        /// Creates a fight that also counts the casts of the hero's cards and evolves them (ADR 0013, see the
        /// remarks of <see cref="Fight"/>).
        /// </summary>
        /// <param name="hero">The player's side. Its line holds each card at the stage its casts have reached.</param>
        /// <param name="enemies">The enemies, at least one, in resolution order.</param>
        /// <param name="maxTicks">Guard against endless fights, at least 1 (see the first constructor).</param>
        /// <param name="random">Seeded random source for the fight.</param>
        /// <param name="heroReserve">The hero's reserve, in the run's order. Copied. May be empty.</param>
        /// <param name="lineEditsAllowed">True for regular fights; false for mini-boss and professor fights.</param>
        /// <param name="heroLineCasts">
        /// The total casts each card copy of the hero's line already has, in line order, or null (with
        /// <paramref name="heroReserveCasts"/>) to not count casts. Not negative.
        /// </param>
        /// <param name="heroReserveCasts">The same for the reserve, in reserve order.</param>
        /// <exception cref="ArgumentNullException">An argument, an enemy or a reserve card is null.</exception>
        /// <exception cref="ArgumentException">
        /// See the other constructors; or only one of the two cast lists is given, a list does not have one count per
        /// card, a count is negative, or a card is not at the stage its casts have reached.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxTicks"/> is less than 1.</exception>
        public Fight(
            FightParticipant hero,
            IReadOnlyList<FightParticipant> enemies,
            int maxTicks,
            IRandom random,
            IReadOnlyList<CardDefinition> heroReserve,
            bool lineEditsAllowed,
            IReadOnlyList<int> heroLineCasts,
            IReadOnlyList<int> heroReserveCasts)
        {
            if (hero == null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            if (enemies.Count == 0)
            {
                throw new ArgumentException("A fight needs at least one enemy.", nameof(enemies));
            }

            if (maxTicks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTicks), maxTicks, "Max ticks must be at least 1.");
            }

            Random = random ?? throw new ArgumentNullException(nameof(random));
            _maxTicks = maxTicks;
            LineEditsAllowed = lineEditsAllowed;

            if (heroReserve == null)
            {
                throw new ArgumentNullException(nameof(heroReserve));
            }

            _heroReserve = new List<CardDefinition>(heroReserve.Count);
            for (var i = 0; i < heroReserve.Count; i++)
            {
                var card = heroReserve[i]
                    ?? throw new ArgumentNullException(nameof(heroReserve), $"Reserve card {i} is null.");
                _heroReserve.Add(card);
            }

            _heroReserveView = _heroReserve.AsReadOnly();

            var participants = new List<FightParticipant>(enemies.Count + 1) { hero };
            for (var i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null)
                {
                    throw new ArgumentNullException(nameof(enemies), $"Enemy {i} is null.");
                }

                participants.Add(enemies[i]);
            }

            var count = participants.Count;
            _combatants = new Combatant[count];
            _spellLines = new SpellLine<CardDefinition>[count];
            _positions = new int[count];
            _elapsedTicks = new int[count];
            _pendingBonuses = new EffectBonus[count][];
            _castCards = new CardDefinition[count];
            _castPositions = new int[count];
            _castBonuses = new EffectBonus[count];

            for (var i = 0; i < count; i++)
            {
                var participant = participants[i];
                var paramName = i == HeroIndex ? nameof(hero) : nameof(enemies);

                if (participant.SpellLine.IsEmpty)
                {
                    throw new ArgumentException($"Combatant {i} has an empty spell line.", paramName);
                }

                if (participant.Combatant.IsDead)
                {
                    throw new ArgumentException($"Combatant {i} is already dead.", paramName);
                }

                if (Array.IndexOf(_combatants, participant.Combatant, 0, i) >= 0)
                {
                    throw new ArgumentException($"Combatant {i} appears more than once in the fight.", paramName);
                }

                _combatants[i] = participant.Combatant;
                _spellLines[i] = Copy(participant.SpellLine);
                _pendingBonuses[i] = new EffectBonus[_spellLines[i].Count];
            }

            if ((heroLineCasts == null) != (heroReserveCasts == null))
            {
                throw new ArgumentException(
                    "Give the casts of both the line and the reserve, or neither.",
                    heroLineCasts == null ? nameof(heroLineCasts) : nameof(heroReserveCasts));
            }

            if (heroLineCasts != null)
            {
                var line = _spellLines[HeroIndex];
                var states = new List<HeroCardState>(line.Count + _heroReserve.Count);
                _lineStates = CreateStates(line.Cards, heroLineCasts, nameof(heroLineCasts), "line", states);
                _reserveStates = CreateStates(
                    _heroReserve, heroReserveCasts, nameof(heroReserveCasts), "reserve", states);
                _allStates = states.ToArray();
            }
        }

        /// <summary>The seeded random source of the fight. Unused by the first-pass rules.</summary>
        public IRandom Random { get; }

        /// <summary>True when the hero's line may be changed during the fight (regular fights).</summary>
        public bool LineEditsAllowed { get; }

        /// <summary>Number of ticks simulated so far. The next tick is <c>Tick + 1</c>.</summary>
        public int Tick { get; private set; }

        /// <summary>True once a side has won or the maximum number of ticks is reached.</summary>
        public bool IsOver => _winner != FightWinner.None || Tick >= _maxTicks;

        /// <summary>
        /// The evolutions of the hero's cards so far, in order (ADR 0013). A read-only live view, so a screen can show
        /// an evolution as it happens; <see cref="FightResult.Evolutions"/> has the same list once the fight is over.
        /// </summary>
        public IReadOnlyList<EvolutionRecord> HeroEvolutions =>
            _evolutionsView ?? (_evolutionsView = new ReadOnlyCollection<EvolutionRecord>(_evolutions));

        /// <summary>The hero's spell line as it is now, in order. Read-only live view.</summary>
        public IReadOnlyList<CardDefinition> HeroLine => _spellLines[HeroIndex].Cards;

        /// <summary>The hero's reserve as it is now, in order. Read-only live view.</summary>
        public IReadOnlyList<CardDefinition> HeroReserve => _heroReserveView;

        /// <summary>
        /// Where combatant <paramref name="combatantIndex"/> is in its line: the card being cast and its progress, or
        /// the position that will be cast next. A read-only snapshot for display.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is not a combatant of the fight.</exception>
        public CastProgress GetCastProgress(int combatantIndex)
        {
            CheckCombatant(combatantIndex);
            var card = _castCards[combatantIndex];
            return card == null
                ? new CastProgress(false, _positions[combatantIndex], null, 0, 0)
                : new CastProgress(true, _castPositions[combatantIndex], card.Id, _elapsedTicks[combatantIndex], card.CastTime);
        }

        /// <summary>
        /// The neighbour bonus waiting at a line position of a combatant (the next cast there takes it). Bonuses stay
        /// at their position when the hero's line is edited (ADR 0012).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is not a combatant, or the position is not in its line.</exception>
        public EffectBonus GetPendingBonus(int combatantIndex, int position)
        {
            CheckCombatant(combatantIndex);
            var pending = _pendingBonuses[combatantIndex];
            if (position < 0 || position >= pending.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(position), position, $"The line has {pending.Length} positions.");
            }

            return pending[position];
        }

        private void CheckCombatant(int combatantIndex)
        {
            if (combatantIndex < 0 || combatantIndex >= _combatants.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(combatantIndex), combatantIndex, $"There are {_combatants.Length} combatants.");
            }
        }

        /// <summary>
        /// Changes the hero's line at the start of the next tick (see the remarks of <see cref="Fight"/>).
        /// <paramref name="change"/> must be stamped with that tick, <c>Tick + 1</c>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="change"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// The fight is over, or this fight does not allow line changes (mini-boss and professor fights).
        /// </exception>
        /// <exception cref="ArgumentException">The change is not stamped with the next tick.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A position or reserve index is out of range.</exception>
        public void ApplyLineChange(LineChange change)
        {
            if (change == null)
            {
                throw new ArgumentNullException(nameof(change));
            }

            if (IsOver)
            {
                throw new InvalidOperationException("The fight is over: its line can no longer change.");
            }

            if (!LineEditsAllowed)
            {
                throw new InvalidOperationException("The hero's line is fixed in this fight (mini-boss or professor).");
            }

            if (change.Tick != Tick + 1)
            {
                throw new ArgumentException(
                    $"A line change applies to the next tick ({Tick + 1}), not to tick {change.Tick}.", nameof(change));
            }

            Validate(change, nameof(change));
            if (change.Kind == LineChangeKind.Move && change.Position == change.ToPosition)
            {
                // Moving a card onto its own position changes nothing: ignored, with no record or log event.
                return;
            }

            var line = _spellLines[HeroIndex];
            var card = line[change.Position];
            var casting = _castCards[HeroIndex] != null && !_heroCastLeftLine;
            CardDefinition incoming;
            if (change.Kind == LineChangeKind.Move)
            {
                line.Move(change.Position, change.ToPosition);
                MoveState(change.Position, change.ToPosition);
                incoming = card;
                if (casting)
                {
                    // While the card being cast is in the line, its anchor follows it.
                    _castPositions[HeroIndex] =
                        PositionAfterMove(_castPositions[HeroIndex], change.Position, change.ToPosition);
                }
            }
            else
            {
                incoming = _heroReserve[change.ReserveIndex];
                line.RemoveAt(change.Position);
                InsertAt(line, change.Position, incoming);
                _heroReserve[change.ReserveIndex] = card;
                SwapStates(change.Position, change.ReserveIndex);
                if (casting && change.Position == _castPositions[HeroIndex])
                {
                    // Once the card being cast leaves for the reserve, its anchor stays on the slot it left, even if
                    // the incoming card is moved later (ADR 0015).
                    _heroCastLeftLine = true;
                }
            }

            _lineChanges.Add(new LineChangeRecord(change, card, incoming));
        }

        /// <summary>
        /// Simulates one tick. Apply the line changes for that tick with <see cref="ApplyLineChange"/> first.
        /// </summary>
        /// <returns>True when the fight is over after this tick.</returns>
        /// <exception cref="InvalidOperationException">The fight is already over.</exception>
        public bool Step()
        {
            if (IsOver)
            {
                throw new InvalidOperationException("The fight is over.");
            }

            Tick++;
            for (var i = 0; i < _combatants.Length; i++)
            {
                if (_combatants[i].IsDead)
                {
                    continue;
                }

                if (_castCards[i] == null)
                {
                    // A cast takes its card and the bonus waiting on its position when it starts. No bonus can reach
                    // a position of this combatant while it casts (only its own resolutions grant them, one at a
                    // time), so this matches taking it on resolution when the line never changes.
                    var start = _positions[i];
                    _castCards[i] = _spellLines[i][start];
                    _castPositions[i] = start;
                    _castBonuses[i] = TakePendingBonus(i, start);
                    _elapsedTicks[i] = 0;
                    if (i == HeroIndex && _lineStates != null)
                    {
                        _heroCastState = _lineStates[start];
                    }
                }

                _elapsedTicks[i]++;
                var card = _castCards[i];
                if (_elapsedTicks[i] < card.CastTime)
                {
                    continue;
                }

                var position = _castPositions[i];
                var bonus = _castBonuses[i];
                var targetIndex = SelectTarget(i);
                var context = new EffectContext(_combatants[i], _combatants[targetIndex], bonus);
                var outcomes = card.Resolve(context);
                GrantNeighbourBonuses(i, position, card);
                _casts.Add(new CastRecord(Tick, i, position, card, targetIndex, outcomes, bonus, context.RemainingBonus));
                if (i == HeroIndex && _heroCastState != null)
                {
                    CountHeroCast(card, position);
                }

                _positions[i] = _spellLines[i].PositionAfter(position);
                _castCards[i] = null;
                if (i == HeroIndex)
                {
                    _heroCastLeftLine = false;
                    _heroCastState = null;
                }

                _castBonuses[i] = EffectBonus.None;
                _elapsedTicks[i] = 0;

                _winner = CurrentWinner();
                if (_winner != FightWinner.None)
                {
                    break;
                }
            }

            return IsOver;
        }

        /// <summary>
        /// Runs the fight to its end: a side wins or the maximum number of ticks is reached. A fight advanced with
        /// <see cref="Step"/> continues from where it is.
        /// </summary>
        /// <returns>The winner, the number of ticks, every resolved cast and every applied line change.</returns>
        /// <exception cref="InvalidOperationException">The fight has already been run.</exception>
        public FightResult Run() => Run(Array.Empty<LineChange>());

        /// <summary>
        /// Runs the fight to its end, applying <paramref name="lineChanges"/> at the start of their ticks. Changes
        /// stamped after the tick the fight ends on are not applied.
        /// </summary>
        /// <param name="lineChanges">Changes in tick order (several on one tick apply in the order given).</param>
        /// <exception cref="ArgumentNullException"><paramref name="lineChanges"/> or one of them is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// The fight has already been run, or it is given changes while line edits are not allowed.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The changes are not in tick order, or one is stamped before the next tick or after the maximum number of
        /// ticks.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">A position or reserve index is out of range.</exception>
        public FightResult Run(IReadOnlyList<LineChange> lineChanges)
        {
            if (lineChanges == null)
            {
                throw new ArgumentNullException(nameof(lineChanges));
            }

            if (_hasRun)
            {
                throw new InvalidOperationException("A fight can only be run once.");
            }

            ValidateSchedule(lineChanges);
            _hasRun = true;

            var next = 0;
            while (!IsOver)
            {
                while (next < lineChanges.Count && lineChanges[next].Tick == Tick + 1)
                {
                    ApplyLineChange(lineChanges[next]);
                    next++;
                }

                Step();
            }

            return new FightResult(
                _winner,
                Tick,
                new ReadOnlyCollection<CastRecord>(_casts.ToArray()),
                new ReadOnlyCollection<LineChangeRecord>(_lineChanges.ToArray()),
                new ReadOnlyCollection<EvolutionRecord>(_evolutions.ToArray()),
                new ReadOnlyCollection<int>(FinalCasts()));
        }

        // The whole schedule is checked before the fight runs, so a bad change never leaves a half-run fight. The
        // sizes of the line and the reserve never change during a fight, so positions can be checked up front.
        internal void ValidateSchedule(IReadOnlyList<LineChange> lineChanges)
        {
            if (lineChanges.Count > 0 && !LineEditsAllowed)
            {
                throw new InvalidOperationException("The hero's line is fixed in this fight (mini-boss or professor).");
            }

            var previousTick = Tick + 1;
            for (var i = 0; i < lineChanges.Count; i++)
            {
                var change = lineChanges[i]
                    ?? throw new ArgumentNullException(nameof(lineChanges), $"Line change {i} is null.");
                if (change.Tick < previousTick)
                {
                    throw new ArgumentException(
                        $"Line change {i} ({change}) is out of tick order or before the next tick ({Tick + 1}).",
                        nameof(lineChanges));
                }

                if (change.Tick > _maxTicks)
                {
                    throw new ArgumentException(
                        $"Line change {i} ({change}) is after the last tick of the fight ({_maxTicks}).",
                        nameof(lineChanges));
                }

                Validate(change, nameof(lineChanges));
                previousTick = change.Tick;
            }
        }

        private void Validate(LineChange change, string paramName)
        {
            var count = _spellLines[HeroIndex].Count;
            if (change.Position >= count)
            {
                throw new ArgumentOutOfRangeException(
                    paramName, change.Position, $"{change}: position is outside the hero's line ({count} cards).");
            }

            if (change.Kind == LineChangeKind.Move && change.ToPosition >= count)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    change.ToPosition,
                    $"{change}: target position is outside the hero's line ({count} cards).");
            }

            if (change.Kind == LineChangeKind.SwapWithReserve && change.ReserveIndex >= _heroReserve.Count)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    change.ReserveIndex,
                    $"{change}: index is outside the hero's reserve ({_heroReserve.Count} cards).");
            }
        }

        // One state per card copy, in line order then reserve order, each checked against the card it goes with.
        private static List<HeroCardState> CreateStates(
            IReadOnlyList<CardDefinition> cards,
            IReadOnlyList<int> casts,
            string paramName,
            string where,
            List<HeroCardState> all)
        {
            if (casts.Count != cards.Count)
            {
                throw new ArgumentException(
                    $"The hero's {where} has {cards.Count} cards but {casts.Count} casts were given.", paramName);
            }

            var states = new List<HeroCardState>(cards.Count);
            for (var i = 0; i < cards.Count; i++)
            {
                if (casts[i] < 0)
                {
                    throw new ArgumentException($"The casts of {where} card {i} are negative ({casts[i]}).", paramName);
                }

                if (cards[i].Stage != cards[i].StageForCasts(casts[i]))
                {
                    throw new ArgumentException(
                        $"The hero's {where} card {i} ('{cards[i].Id}') is at stage {cards[i].Stage} but "
                        + $"{casts[i]} casts reach stage {cards[i].StageForCasts(casts[i])}.",
                        paramName);
                }

                var state = new HeroCardState { Casts = casts[i] };
                states.Add(state);
                all.Add(state);
            }

            return states;
        }

        // The states follow their cards when the line changes.
        private void MoveState(int from, int to)
        {
            if (_lineStates == null)
            {
                return;
            }

            var state = _lineStates[from];
            _lineStates.RemoveAt(from);
            _lineStates.Insert(to, state);
        }

        private void SwapStates(int linePosition, int reserveIndex)
        {
            if (_lineStates == null)
            {
                return;
            }

            var fromLine = _lineStates[linePosition];
            _lineStates[linePosition] = _reserveStates[reserveIndex];
            _reserveStates[reserveIndex] = fromLine;
        }

        // Counts a resolved cast of the hero's card copy being cast. When the count reaches a stage, the copy turns
        // into its evolved form where it is now (line or reserve): the next cast of the copy uses it.
        private void CountHeroCast(CardDefinition card, int position)
        {
            var state = _heroCastState;
            state.Casts = checked(state.Casts + 1);
            var stage = card.StageForCasts(state.Casts);
            if (stage <= card.Stage)
            {
                return;
            }

            var evolved = card.AtStage(stage);
            var slot = _lineStates.IndexOf(state);
            if (slot >= 0)
            {
                _spellLines[HeroIndex].Replace(slot, evolved);
            }
            else
            {
                _heroReserve[_reserveStates.IndexOf(state)] = evolved;
            }

            _evolutions.Add(new EvolutionRecord(Tick, position, evolved, state.Casts, _casts.Count - 1));
        }

        private int[] FinalCasts()
        {
            if (_allStates == null)
            {
                return Array.Empty<int>();
            }

            var casts = new int[_allStates.Length];
            for (var i = 0; i < casts.Length; i++)
            {
                casts[i] = _allStates[i].Casts;
            }

            return casts;
        }

        // Where the card at `position` ends up when the card at `from` moves to `to` (SpellLine.Move).
        private static int PositionAfterMove(int position, int from, int to)
        {
            if (position == from)
            {
                return to;
            }

            if (from < to && position > from && position <= to)
            {
                return position - 1;
            }

            if (to < from && position >= to && position < from)
            {
                return position + 1;
            }

            return position;
        }

        // SpellLine has no insert: re-adding the tail keeps its capacity rules in one place. The line is never full
        // here, since a card was just removed.
        private static void InsertAt(SpellLine<CardDefinition> line, int position, CardDefinition card)
        {
            var tail = new List<CardDefinition>();
            while (line.Count > position)
            {
                tail.Add(line.RemoveAt(position));
            }

            line.Add(card);
            foreach (var moved in tail)
            {
                line.Add(moved);
            }
        }

        // Targeting rule (ADR 0004): the hero aims at the first living enemy, enemies aim at the hero.
        // Only called while the fight is not over, so a living enemy always exists.
        private int SelectTarget(int casterIndex)
        {
            if (casterIndex != HeroIndex)
            {
                return HeroIndex;
            }

            for (var i = HeroIndex + 1; i < _combatants.Length; i++)
            {
                if (!_combatants[i].IsDead)
                {
                    return i;
                }
            }

            throw new InvalidOperationException("No living enemy to target.");
        }

        // Every bonus waiting on this position is used up by this cast.
        private EffectBonus TakePendingBonus(int combatantIndex, int position)
        {
            var bonus = _pendingBonuses[combatantIndex][position];
            _pendingBonuses[combatantIndex][position] = EffectBonus.None;
            return bonus;
        }

        // Called after the card's own pending bonus was taken, so in a single-card line the card boosts its next cast.
        private void GrantNeighbourBonuses(int combatantIndex, int position, CardDefinition card)
        {
            var line = _spellLines[combatantIndex];
            var pending = _pendingBonuses[combatantIndex];
            foreach (var modifier in card.NeighbourModifiers)
            {
                var neighbour = modifier.Direction == NeighbourDirection.Next
                    ? line.PositionAfter(position)
                    : line.PositionBefore(position);
                pending[neighbour] = pending[neighbour].Plus(modifier.ToBonus());
            }
        }

        private FightWinner CurrentWinner()
        {
            if (_combatants[HeroIndex].IsDead)
            {
                return FightWinner.Enemies;
            }

            for (var i = HeroIndex + 1; i < _combatants.Length; i++)
            {
                if (!_combatants[i].IsDead)
                {
                    return FightWinner.None;
                }
            }

            return FightWinner.Hero;
        }

        private static SpellLine<CardDefinition> Copy(SpellLine<CardDefinition> source)
        {
            var copy = new SpellLine<CardDefinition>(source.Capacity);
            foreach (var card in source.Cards)
            {
                copy.Add(card);
            }

            return copy;
        }

        // The casts of one card copy of the hero. A reference, so it follows the copy through line changes.
        private sealed class HeroCardState
        {
            public int Casts;
        }
    }
}
