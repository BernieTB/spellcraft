using System;
using System.Collections.Generic;

namespace Game.Core.Combat
{
    /// <summary>
    /// How a fight ended: the winner, how long it took and every cast that resolved.
    /// </summary>
    public sealed class FightResult
    {
        /// <exception cref="ArgumentNullException"><paramref name="casts"/> is null.</exception>
        public FightResult(FightWinner winner, int ticks, IReadOnlyList<CastRecord> casts)
        {
            Winner = winner;
            Ticks = ticks;
            Casts = casts ?? throw new ArgumentNullException(nameof(casts));
        }

        /// <summary>The winning side, or <see cref="FightWinner.None"/> on timeout.</summary>
        public FightWinner Winner { get; }

        /// <summary>True when the fight hit its maximum number of ticks before a side won.</summary>
        public bool TimedOut => Winner == FightWinner.None;

        /// <summary>Number of ticks simulated: the tick on which the fight ended, or the maximum on timeout.</summary>
        public int Ticks { get; }

        /// <summary>Every resolved cast, in resolution order.</summary>
        public IReadOnlyList<CastRecord> Casts { get; }
    }
}
