using System;
using System.Collections.Generic;
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
    /// Only one session is open at a time. While it is, the run refuses every other change (steps, cards, upgrades,
    /// capacity, line edits through <see cref="Run"/>): the session owns the line.
    /// </para>
    /// <para>
    /// Line changes apply to the run's own card instances as they happen, so <see cref="Run.Line"/> and
    /// <see cref="Run.Reserve"/> always show the current state, which is the state replaying
    /// <see cref="FightResult.LineChanges"/> on the instances would give. A fight that is cancelled keeps the changes
    /// made so far: they are edits of the player's build. The fight itself is not counted and no random draw is
    /// consumed, so beginning the same step again plays the same encounter.
    /// </para>
    /// <para>
    /// Whatever the number of ticks per frame the screen plays, the same seed and the same changes at the same ticks
    /// give the same fight, as <see cref="Run.Play"/> (which is a session with no changes) does.
    /// </para>
    /// </remarks>
    public sealed class RunFightSession
    {
        private readonly Run _run;
        private readonly IReadOnlyList<FightParticipant> _participants;
        private readonly IReadOnlyList<CombatantSnapshot> _snapshots;
        private readonly List<CardInstance> _startLine;
        private bool _closed;

        internal RunFightSession(
            Run run,
            RunStep step,
            EncounterDefinition encounter,
            Fight fight,
            IReadOnlyList<FightParticipant> participants,
            IReadOnlyList<CombatantSnapshot> snapshots,
            IEnumerable<CardInstance> startLine,
            Pcg32Random drawRandomAfter)
        {
            _run = run;
            Step = step;
            Encounter = encounter;
            Fight = fight;
            _participants = participants;
            _snapshots = snapshots;
            _startLine = new List<CardInstance>(startLine);
            DrawRandomAfter = drawRandomAfter;
        }

        /// <summary>The step being played.</summary>
        public RunStep Step { get; }

        /// <summary>The encounter being fought (for a regular fight, the one drawn from the pool).</summary>
        public EncounterDefinition Encounter { get; }

        /// <summary>
        /// The Core fight, to read the tick, the health and shield of each side and the line as the fight sees it
        /// (upgraded cards). Advance it and change its line through the session, not directly: the session keeps the
        /// run's card instances in step.
        /// </summary>
        public Fight Fight { get; }

        /// <summary>True when the player may edit the hero's line during this fight (regular fights only).</summary>
        public bool LineEditsAllowed => Fight.LineEditsAllowed;

        /// <summary>Number of ticks simulated so far; the next change must be stamped with <c>Tick + 1</c>.</summary>
        public int Tick => Fight.Tick;

        /// <summary>True once a side has won or the time limit is reached; call <see cref="Complete"/> then.</summary>
        public bool IsOver => Fight.IsOver;

        /// <summary>True until the session is completed or cancelled.</summary>
        public bool IsOpen => !_closed;

        internal Pcg32Random DrawRandomAfter { get; }

        internal IReadOnlyList<FightParticipant> Participants => _participants;

        internal IReadOnlyList<CombatantSnapshot> Snapshots => _snapshots;

        internal IReadOnlyList<CardInstance> StartLine => _startLine;

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
        /// <see cref="Fight.Run(IReadOnlyList{LineChange})"/> does. Changes stamped after the tick the fight ends on are
        /// not applied. If a change is refused part-way, the ones before it stay applied.
        /// </summary>
        /// <param name="lineChanges">Changes in tick order (several on one tick apply in the order given); may be null.</param>
        /// <exception cref="ArgumentNullException">A change is null.</exception>
        /// <exception cref="ArgumentException">The changes are not in tick order or start before the next tick.</exception>
        /// <exception cref="InvalidOperationException">
        /// The session is closed, or changes are given for a fight whose line is fixed.
        /// </exception>
        public void RunToEnd(IReadOnlyList<LineChange> lineChanges = null)
        {
            EnsureOpen();
            var changes = lineChanges ?? Array.Empty<LineChange>();
            if (changes.Count > 0 && !LineEditsAllowed)
            {
                throw new InvalidOperationException("The hero's line is fixed in this fight (mini-boss or professor).");
            }

            var previousTick = Tick + 1;
            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i]
                    ?? throw new ArgumentNullException(nameof(lineChanges), $"Line change {i} is null.");
                if (change.Tick < previousTick)
                {
                    throw new ArgumentException(
                        $"Line change {i} ({change}) is out of tick order or before the next tick ({Tick + 1}).",
                        nameof(lineChanges));
                }

                previousTick = change.Tick;
            }

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
        /// <see cref="Run.Play"/> does (XP, levels, fights won, outcome, random draw). If it throws, the run is
        /// unchanged: cancel the session.
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

        private void EnsureOpen()
        {
            if (_closed)
            {
                throw new InvalidOperationException("The fight session is closed.");
            }
        }
    }
}
