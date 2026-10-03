using System;

namespace Game.Core.Runs
{
    /// <summary>
    /// Global rules shared by every run, from one data asset.
    /// </summary>
    public sealed class RunRules
    {
        /// <param name="fightTimeLimit">
        /// Maximum ticks of any fight. A fight that reaches it counts as a defeat
        /// (<c>docs/adr/0011-enemy-tiers-and-fight-time-limit.md</c>). At least 1.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="fightTimeLimit"/> is less than 1.</exception>
        public RunRules(int fightTimeLimit)
        {
            if (fightTimeLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(fightTimeLimit), fightTimeLimit, "The fight time limit must be at least 1 tick.");
            }

            FightTimeLimit = fightTimeLimit;
        }

        /// <summary>Maximum ticks of any fight; reaching it is a defeat.</summary>
        public int FightTimeLimit { get; }
    }
}
