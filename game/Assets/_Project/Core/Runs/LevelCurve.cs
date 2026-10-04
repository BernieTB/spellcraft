using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Game.Core.Runs
{
    /// <summary>
    /// How much XP each level-up costs, from data (<c>docs/adr/0009-vertical-slice-run-pacing.md</c>): each level
    /// costs more than the previous one and there is no level cap.
    /// </summary>
    /// <remarks>
    /// <para>The curve is a list of explicit costs followed by a linear tail:</para>
    /// <list type="bullet">
    /// <item><see cref="LevelCosts"/>[i] is the XP needed to go from level i + 1 to level i + 2 (the hero starts at
    /// level 1). The costs are strictly increasing.</item>
    /// <item>Past the list, each level costs <see cref="CostIncreaseAfterList"/> more than the one before, so farming
    /// still brings levels, more and more slowly.</item>
    /// </list>
    /// <para>XP is cumulative: the cost of a level is paid from the XP left over after the previous ones.</para>
    /// </remarks>
    public sealed class LevelCurve
    {
        /// <param name="levelCosts">
        /// XP cost of each level-up from level 1, in order. At least one cost; every cost greater than zero and
        /// greater than the one before.
        /// </param>
        /// <param name="costIncreaseAfterList">
        /// How much each level past the list costs more than the previous one. Greater than zero.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="levelCosts"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="levelCosts"/> is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// A cost is not greater than zero or not greater than the previous one, or
        /// <paramref name="costIncreaseAfterList"/> is not greater than zero.
        /// </exception>
        public LevelCurve(IEnumerable<int> levelCosts, int costIncreaseAfterList)
        {
            if (levelCosts == null)
            {
                throw new ArgumentNullException(nameof(levelCosts));
            }

            var costs = new List<int>(levelCosts);
            if (costs.Count == 0)
            {
                throw new ArgumentException("A level curve needs at least one level cost.", nameof(levelCosts));
            }

            for (var i = 0; i < costs.Count; i++)
            {
                if (costs[i] <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(levelCosts), costs[i], $"The level cost at index {i} must be greater than zero.");
                }

                if (i > 0 && costs[i] <= costs[i - 1])
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(levelCosts), costs[i], $"The level cost at index {i} must be greater than the one before ({costs[i - 1]}).");
                }
            }

            if (costIncreaseAfterList <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(costIncreaseAfterList), costIncreaseAfterList, "The cost increase after the list must be greater than zero.");
            }

            LevelCosts = new ReadOnlyCollection<int>(costs);
            CostIncreaseAfterList = costIncreaseAfterList;
        }

        /// <summary>XP cost of each level-up from level 1, in order.</summary>
        public IReadOnlyList<int> LevelCosts { get; }

        /// <summary>How much each level past <see cref="LevelCosts"/> costs more than the previous one.</summary>
        public int CostIncreaseAfterList { get; }

        /// <summary>The XP needed to go from <paramref name="level"/> to the next level.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> is less than 1.</exception>
        /// <exception cref="OverflowException">The cost does not fit in a <see cref="long"/>.</exception>
        public long CostToLevelUpFrom(int level)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), level, "Levels start at 1.");
            }

            var index = level - 1;
            if (index < LevelCosts.Count)
            {
                return LevelCosts[index];
            }

            var levelsPastList = (long)(index - LevelCosts.Count + 1);
            return checked(LevelCosts[LevelCosts.Count - 1] + levelsPastList * CostIncreaseAfterList);
        }

        /// <summary>The level reached with <paramref name="totalXp"/> XP earned since level 1.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="totalXp"/> is negative.</exception>
        public int LevelForTotalXp(long totalXp)
        {
            if (totalXp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalXp), totalXp, "Total XP cannot be negative.");
            }

            var level = 1;
            var remaining = totalXp;
            while (true)
            {
                var cost = CostToLevelUpFrom(level);
                if (remaining < cost)
                {
                    return level;
                }

                remaining -= cost;
                level = checked(level + 1);
            }
        }
    }
}
