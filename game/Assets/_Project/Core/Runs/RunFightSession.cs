using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Combat.Log;
using Game.Core.Enemies;
using Game.Core.Randomness;

namespace Game.Core.Runs
{
    /// <summary>
    /// One fight of a run, started with <see cref="Run.BeginFight"/> and advanced tick by tick, so the run screen can
    /// let the player edit the spell line while a regular fight plays
    /// (<c>docs/adr/0012-linked-choices-and-spell-line-editing.md</c>,
    /// <c>docs/adr/0015-live-spell-line-editing-details.md</c>). <see cref="Complete"/> commits the result to the run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The screen that plays a session must call <see cref="Cancel"/> when it leaves a fight that was not completed
    /// (quitting to the menu, closing the screen): an open session blocks every other change of the run, and
    /// <see cref="Run.AvailableSteps"/> does not tell it apart, so the screen tests <see cref="Run.CurrentFight"/>.
    /// </para>
    /// <para>
    /// Only one session is open at a time. While it is, the run refuses every other change (steps, cards, upgrades,
    /// capacity, line edits through <see cref="Run"/>): the session owns the line.
    /// </para>
    /// <para>
    /// Line changes apply to the run's own card instances as they happen, so <see cref="Run.Line"/> and
    /// <see cref="Run.Reserve"/> always show the current state, which is the state replaying
    /// <see cref="FightResult.LineChanges"/> on the instances would give. A fight that is cancelled (confirmed by the owner on 2026-10-04) keeps the changes
    /// made so far: they are edits of the player's build. The fight itself is not counted and no random draw is
    /// consumed, so beginning the same step again plays the same encounter.
    /// </para>
    /// <para>
    /// Whatever the number of ticks per frame the screen plays, the same seed and the same changes at the same ticks
    /// give the same fight, as <see cref="Run.Play"/> (which is a session with no changes) does.
    /// </para>
    /// <para>
    /// Card evolution (<c>docs/adr/0013-card-evolution.md</c>): the fight counts the casts of every card copy of the
    /// hero, in the line and in the reserve, starting from the counts the run's <see cref="CardInstance"/>s already
    /// have, and evolves the cards in the fight (the next cast of a copy uses the stage it just reached, confirmed by
    /// the owner on 2026-10-04). <see cref="Complete"/> hands the final counts to the instances, by the copies
    /// they belong to, so a card swapped into the reserve while casting keeps its count and stage. A session that is
    /// cancelled does not count the fight, so it keeps none of its casts either: unlike the line changes, which are
    /// edits of the player's build and stay, the counters belong to a fight that never counted. Beginning the step
    /// again replays the fight from the counts the instances had.
    /// </para>
    /// </remarks>
    public sealed class RunFightSession
    {
        private readonly Run _run;
        private readonly IReadOnlyList<FightParticipant> _participants;
        private readonly IReadOnlyList<CombatantSnapshot> _snapshots;
        private readonly List<CardInstance> _startLine;
        private readonly List<CardInstance> _startReserve;
        private bool _closed;

        internal RunFightSession(
            Run run,
            RunStep step,
            EncounterDefinition encounter,
            Fight fight,
            IReadOnlyList<FightParticipant> participants,
            IReadOnlyList<CombatantSnapshot> snapshots,
            IEnumerable<CardInstance> startLine,
            IEnumerable<CardInstance> startReserve,
            Pcg32Random drawRandomAfter)
        {
            _run = run;
            Step = step;
            Encounter = encounter;
            Fight = fight;
            _participants = participants;
            _snapshots = snapshots;
            _startLine = new List<CardInstance>(startLine);
            _startReserve = new List<CardInstance>(startReserve);
            DrawRandomAfter = drawRandomAfter;
        }

        /// <summary>The step being played.</summary>
        public RunStep Step { get; }

        /// <summary>The encounter being fought (for a regular fight, the one drawn from the pool).</summary>
        public EncounterDefinition Encounter { get; }

        // The Core fight stays internal: it is advanced and its line changed only through the session, which keeps
        // the run's card instances in step.
        internal Fight Fight { get; }

        /// <summary>True when the player may edit the hero's line during this fight (regular fights only).</summary>
        public bool LineEditsAllowed => Fight.LineEditsAllowed;

        /// <summary>Number of ticks simulated so far; the next change must be stamped with <c>Tick + 1</c>.</summary>
        public int Tick => Fight.Tick;

        /// <summary>True once a side has won or the time limit is reached; call <see cref="Complete"/> then.</summary>
        public bool IsOver => Fight.IsOver;

        /// <summary>
        /// The outcome once <see cref="IsOver"/>: the hero when every enemy is dead, the enemies when the hero is
        /// dead, nobody (time limit) otherwise. <see cref="FightWinner.None"/> while the fight goes on.
        /// </summary>
        public FightWinner Winner
        {
            get
            {
                if (!Fight.IsOver)
                {
                    return FightWinner.None;
                }

                if (_participants[Core.Combat.Fight.HeroIndex].Combatant.IsDead)
                {
                    return FightWinner.Enemies;
                }

                for (var i = 1; i < _participants.Count; i++)
                {
                    if (!_participants[i].Combatant.IsDead)
                    {
                        return FightWinner.None;
                    }
                }

                return FightWinner.Hero;
            }
        }

        /// <summary>The hero's spell line as the fight sees it now (upgraded cards), in order. Read-only live view.</summary>
        public IReadOnlyList<CardDefinition> HeroLine => Fight.HeroLine;

        /// <summary>The hero's reserve as the fight sees it now (upgraded cards), in order. Read-only live view.</summary>
        public IReadOnlyList<CardDefinition> HeroReserve => Fight.HeroReserve;

        /// <summary>The hero's current health.</summary>
        public int HeroHealth => _participants[Core.Combat.Fight.HeroIndex].Combatant.CurrentHealth;

        /// <summary>The hero's max health for this fight (upgrades included).</summary>
        public int HeroMaxHealth => _participants[Core.Combat.Fight.HeroIndex].Combatant.MaxHealth;

        /// <summary>The hero's current shield.</summary>
        public int HeroShield => _participants[Core.Combat.Fight.HeroIndex].Combatant.Shield;

        /// <summary>Number of enemies in the fight, in encounter order.</summary>
        public int EnemyCount => _participants.Count - 1;

        /// <summary>The current health of the enemy at <paramref name="enemyIndex"/> (0 is the first enemy).</summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is not an enemy.</exception>
        public int EnemyHealth(int enemyIndex) => Enemy(enemyIndex).CurrentHealth;

        /// <summary>The max health of the enemy at <paramref name="enemyIndex"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is not an enemy.</exception>
        public int EnemyMaxHealth(int enemyIndex) => Enemy(enemyIndex).MaxHealth;

        /// <summary>The current shield of the enemy at <paramref name="enemyIndex"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is not an enemy.</exception>
        public int EnemyShield(int enemyIndex) => Enemy(enemyIndex).Shield;

        /// <summary>True until the session is completed or cancelled.</summary>
        public bool IsOpen => !_closed;

        internal Pcg32Random DrawRandomAfter { get; }

        internal IReadOnlyList<FightParticipant> Participants => _participants;

        internal IReadOnlyList<CombatantSnapshot> Snapshots => _snapshots;

        internal IReadOnlyList<CardInstance> StartLine => _startLine;

        internal IReadOnlyList<CardInstance> StartReserve => _startReserve;

        /// <summary>
        /// Changes the hero's line at the start of the next tick, in the fight and on the run's card instances. See
        /// <see cref="Core.Combat.Fight.ApplyLineChange"/>; <paramref name="change"/> must be stamped with
        /// <c>Tick + 1</c>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="change"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// The session is closed, the fight is over, or its line is fixed (mini-boss and professor fights).
        /// </exception>
        /// <exception cref="ArgumentException">The change is not stamped with the next tick.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A position or reserve index is out of range.</exception>
        public void ApplyLineChange(LineChange change)
        {
            EnsureOpen();
            Fight.ApplyLineChange(change);
            _run.MirrorLineChange(change);
        }

        /// <summary>Moves a card of the line at the start of the next tick (see <see cref="ApplyLineChange"/>).</summary>
        public void MoveCard(int fromPosition, int toPosition)
        {
            ApplyLineChange(LineChange.Move(Tick + 1, fromPosition, toPosition));
        }

        /// <summary>
        /// Swaps a line card with a reserve card at the start of the next tick (see <see cref="ApplyLineChange"/>).
        /// </summary>
        public void SwapWithReserve(int linePosition, int reserveIndex)
        {
            ApplyLineChange(LineChange.SwapWithReserve(Tick + 1, linePosition, reserveIndex));
        }

        /// <summary>Simulates one tick. Apply the line changes of that tick first.</summary>
        /// <returns>True when the fight is over after this tick.</returns>
        /// <exception cref="InvalidOperationException">The session is closed or the fight is already over.</exception>
        public bool Advance()
        {
            EnsureOpen();
            return Fight.Step();
        }

        /// <summary>
        /// Plays the fight to its end, applying <paramref name="lineChanges"/> at the start of their ticks as
        /// <see cref="Core.Combat.Fight.Run(IReadOnlyList{LineChange})"/> does. Changes stamped after the tick the fight
        /// ends on are not applied. The schedule is checked as a whole first: a refused schedule changes nothing.
        /// </summary>
        /// <param name="lineChanges">Changes in tick order (several on one tick apply in the order given); may be null.</param>
        /// <exception cref="ArgumentNullException">A change is null.</exception>
        /// <exception cref="ArgumentException">
        /// The changes are not in tick order, start before the next tick or after the time limit.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">A position or reserve index is out of range.</exception>
        /// <exception cref="InvalidOperationException">
        /// The session is closed, or changes are given for a fight whose line is fixed.
        /// </exception>
        public void RunToEnd(IReadOnlyList<LineChange> lineChanges = null)
        {
            EnsureOpen();
            var changes = lineChanges ?? Array.Empty<LineChange>();

            // The whole schedule is checked before anything is played, as Fight.Run does: a refused schedule changes
            // neither the fight nor the run. The sizes of the line and the reserve never change during a fight.
            Fight.ValidateSchedule(changes);

            var next = 0;
            while (!Fight.IsOver)
            {
                while (next < changes.Count && changes[next].Tick == Tick + 1)
                {
                    ApplyLineChange(changes[next]);
                    next++;
                }

                Fight.Step();
            }
        }

        /// <summary>
        /// Ends the session once the fight is over: builds the combat log and updates the run exactly as
        /// <see cref="Run.Play"/> does (XP, levels, fights won, outcome, random draw, and the casts and evolution
        /// of every card copy). If it throws, the run is unchanged: cancel the session.
        /// </summary>
        /// <returns>The report of the fight; <see cref="RunFightReport.HeroLine"/> is the line when it started.</returns>
        /// <exception cref="InvalidOperationException">The session is closed or the fight is not over yet.</exception>
        public RunFightReport Complete()
        {
            EnsureOpen();
            if (!Fight.IsOver)
            {
                throw new InvalidOperationException("The fight is not over yet.");
            }

            var log = CombatLogRecorder.Finish(Fight, _participants, _snapshots);
            var report = _run.CommitFight(this, log);
            _closed = true;
            return report;
        }

        /// <summary>
        /// Abandons the fight without counting it. The line changes already made stay. Nothing happens if the session
        /// is already closed.
        /// </summary>
        public void Cancel()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;
            _run.ReleaseFight(this);
        }

        private Combatant Enemy(int enemyIndex)
        {
            if (enemyIndex < 0 || enemyIndex >= EnemyCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(enemyIndex), enemyIndex, $"There are {EnemyCount} enemies in this fight.");
            }

            return _participants[enemyIndex + 1].Combatant;
        }

        private void EnsureOpen()
        {
            if (_closed)
            {
                throw new InvalidOperationException("The fight session is closed.");
            }
        }
    }
}
