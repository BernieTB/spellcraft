using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Enemies;
using Game.Core.Meta;

namespace Game.Core.Runs
{
    /// <summary>
    /// The preparation phase before a mini-boss or professor fight (#82,
    /// <c>docs/adr/0012-linked-choices-and-spell-line-editing.md</c>,
    /// <c>docs/adr/0014-boss-preparation-and-recap.md</c>), opened with <see cref="Run.BeginPreparation"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The player rearranges the spell line and swaps cards with the reserve (no size limit), using the run's own
    /// line edits. <see cref="Start"/> begins the fight and ends the preparation: the line is then fixed, since the
    /// session of these fights refuses line changes, and any further edit through this object throws.
    /// </para>
    /// <para>
    /// There is no retry: a lost or timed-out fight ends the run, so a preparation can never be played again, and
    /// a preparation starts one fight at most.
    /// </para>
    /// <para>
    /// For the professor, only <see cref="ProfessorKnowledge"/> is exposed, built from the bestiary: the encounter
    /// itself stays hidden. For a mini-boss, <see cref="MiniBossEncounter"/> is exposed as is; what the player knows
    /// about a mini-boss before its fight is an open question of the GDD, so this simplest default may change.
    /// </para>
    /// </remarks>
    public sealed class BossPreparation
    {
        private readonly Run _run;

        internal BossPreparation(Run run, RunStep step, EncounterDefinition miniBossEncounter, ProfessorKnowledge knowledge)
        {
            _run = run;
            Step = step;
            MiniBossEncounter = miniBossEncounter;
            ProfessorKnowledge = knowledge;
        }

        /// <summary>The fight this prepares: a secret room or the professor.</summary>
        public RunStep Step { get; }

        /// <summary>The mini-boss's encounter for a secret room step, otherwise null.</summary>
        public EncounterDefinition MiniBossEncounter { get; }

        /// <summary>
        /// What the bestiary reveals of the professor for the professor step, otherwise null. Health and shield are
        /// null when hidden; the spell line has the professor's length with null for each hidden card.
        /// </summary>
        public ProfessorKnowledge ProfessorKnowledge { get; }

        /// <summary>True once <see cref="Start"/> has begun the fight: the line is fixed.</summary>
        public bool IsStarted { get; private set; }

        /// <summary>The spell line, in casting order.</summary>
        public IReadOnlyList<CardInstance> Line => _run.Line;

        /// <summary>Every card the player owns outside the line; it has no size limit.</summary>
        public IReadOnlyList<CardInstance> Reserve => _run.Reserve;

        /// <summary>How many cards the line can hold.</summary>
        public int LineCapacity => _run.LineCapacity;

        /// <summary>Moves a line card to another position, shifting the cards in between.</summary>
        /// <exception cref="ArgumentOutOfRangeException">A position is not valid.</exception>
        /// <exception cref="InvalidOperationException">The fight has started, or the run cannot change.</exception>
        public void MoveInLine(int fromPosition, int toPosition)
        {
            EnsureEditable();
            _run.MoveInLine(fromPosition, toPosition);
        }

        /// <summary>Exchanges a line card with a reserve card.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The position or the reserve index is not valid.</exception>
        /// <exception cref="InvalidOperationException">The fight has started, or the run cannot change.</exception>
        public void SwapWithReserve(int linePosition, int reserveIndex)
        {
            EnsureEditable();
            _run.SwapWithReserve(linePosition, reserveIndex);
        }

        /// <summary>Moves a line card to the end of the reserve (the line keeps at least one card).</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="linePosition"/> is not valid.</exception>
        /// <exception cref="InvalidOperationException">The fight has started, or the run cannot change.</exception>
        public void MoveToReserve(int linePosition)
        {
            EnsureEditable();
            _run.MoveToReserve(linePosition);
        }

        /// <summary>Moves a reserve card to the end of the line.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="reserveIndex"/> is not valid.</exception>
        /// <exception cref="InvalidOperationException">The fight has started, the line is full, or the run cannot change.</exception>
        public void MoveFromReserve(int reserveIndex)
        {
            EnsureEditable();
            _run.MoveFromReserve(reserveIndex);
        }

        /// <summary>
        /// Ends the preparation and begins the fight, with the line as arranged. The returned session refuses line
        /// changes (<see cref="RunStep.AllowsLineEditing"/> is false for these steps).
        /// </summary>
        /// <exception cref="InvalidOperationException">The fight has already started, or the run cannot change.</exception>
        public RunFightSession Start()
        {
            EnsureEditable();
            var session = _run.BeginFight(Step);
            IsStarted = true;
            return session;
        }

        private void EnsureEditable()
        {
            if (IsStarted)
            {
                throw new InvalidOperationException("The fight has started: the spell line is fixed.");
            }
        }
    }
}
