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
        /// <param name="levelCurve">XP cost of each level-up (<c>docs/adr/0009-vertical-slice-run-pacing.md</c>).</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="fightTimeLimit"/> is less than 1.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="levelCurve"/> is null.</exception>
        public RunRules(int fightTimeLimit, LevelCurve levelCurve)
        {
            if (fightTimeLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(fightTimeLimit), fightTimeLimit, "The fight time limit must be at least 1 tick.");
            }

            FightTimeLimit = fightTimeLimit;
            LevelCurve = levelCurve ?? throw new ArgumentNullException(nameof(levelCurve));
        }

        /// <summary>Maximum ticks of any fight; reaching it is a defeat.</summary>
        public int FightTimeLimit { get; }

        /// <summary>XP cost of each level-up.</summary>
        public LevelCurve LevelCurve { get; }
    }
}
