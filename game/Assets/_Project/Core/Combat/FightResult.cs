using System;
using System.Collections.Generic;

namespace Game.Core.Combat
{
    /// <summary>
    /// How a fight ended: the winner, how long it took, every cast that resolved and every change the player made
    /// to the hero's line.
    /// </summary>
    public sealed class FightResult
    {
        /// <summary>Creates the result of a fight without line changes.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="casts"/> is null.</exception>
        public FightResult(FightWinner winner, int ticks, IReadOnlyList<CastRecord> casts)
            : this(winner, ticks, casts, Array.Empty<LineChangeRecord>())
        {
        }

        /// <exception cref="ArgumentNullException">
        /// <paramref name="casts"/> or <paramref name="lineChanges"/> is null.
        /// </exception>
        public FightResult(
            FightWinner winner,
            int ticks,
            IReadOnlyList<CastRecord> casts,
            IReadOnlyList<LineChangeRecord> lineChanges)
        {
            Winner = winner;
            Ticks = ticks;
            Casts = casts ?? throw new ArgumentNullException(nameof(casts));
            LineChanges = lineChanges ?? throw new ArgumentNullException(nameof(lineChanges));
        }

        /// <summary>The winning side, or <see cref="FightWinner.None"/> on timeout.</summary>
        public FightWinner Winner { get; }

        /// <summary>True when the fight hit its maximum number of ticks before a side won.</summary>
        public bool TimedOut => Winner == FightWinner.None;

        /// <summary>Number of ticks simulated: the tick on which the fight ended, or the maximum on timeout.</summary>
        public int Ticks { get; }

        /// <summary>Every resolved cast, in resolution order.</summary>
        public IReadOnlyList<CastRecord> Casts { get; }

        /// <summary>
        /// Every change applied to the hero's line, in the order applied (each at the start of its tick, before the
        /// casts of that tick). Empty when the line never changed.
        /// </summary>
        public IReadOnlyList<LineChangeRecord> LineChanges { get; }
    }
}
