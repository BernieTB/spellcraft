using System;
using Game.Core.Combat;

namespace Game.Core.Simulation
{
    /// <summary>
    /// Runs a batch of fights over a consecutive range of seeds and summarises them. Headless: no scene, no frame
    /// loop, no wall-clock time (<c>docs/adr/0006-headless-simulation-runner.md</c>).
    /// </summary>
    public static class FightBatch
    {
        /// <summary>
        /// Runs <paramref name="fightCount"/> fights, the <c>i</c>-th (from 0) with seed <c>firstSeed + i</c>, and
        /// returns their summary. The same factory and seeds always give the same summary.
        /// </summary>
        /// <param name="firstSeed">Seed of the first fight.</param>
        /// <param name="fightCount">Number of fights, at least 1.</param>
        /// <param name="createFight">
        /// Builds a fresh fight for a seed, with new combatants (a fight changes its combatants' health and shield)
        /// and a random source seeded with that seed.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="createFight"/> is null or returns null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="fightCount"/> is less than 1, or the last seed would overflow.
        /// </exception>
        public static SimulationSummary Run(long firstSeed, int fightCount, Func<long, Fight> createFight)
        {
            if (createFight == null)
            {
                throw new ArgumentNullException(nameof(createFight));
            }

            if (fightCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(fightCount), fightCount, "Run at least one fight.");
            }

            if (firstSeed > long.MaxValue - (fightCount - 1))
            {
                throw new ArgumentOutOfRangeException(nameof(firstSeed), firstSeed, "The seed range would overflow.");
            }

            var builder = new SimulationSummaryBuilder();
            for (var i = 0; i < fightCount; i++)
            {
                var seed = firstSeed + i;
                var fight = createFight(seed)
                    ?? throw new ArgumentNullException(nameof(createFight), $"The factory returned no fight for seed {seed}.");
                builder.Add(fight.Run());
            }

            return builder.Build(firstSeed);
        }
    }
}
