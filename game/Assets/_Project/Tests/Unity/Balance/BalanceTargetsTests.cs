using System.Collections.Generic;
using System.Linq;
using Game.Unity.Tests.Balance;
using NUnit.Framework;

namespace Game.Unity.Tests.Balance
{
    /// <summary>
    /// The difficulty targets of the Vertical slice (#125), checked by playing complete runs of the biome with the bots
    /// of <see cref="BalanceBot"/> on fixed seeds (so the result is deterministic): a player who never touches the line
    /// loses, one who orders it wins about half of the time, one who optimises it wins most of the time, the
    /// mini-bosses are spikes and the professor is a wall without the rooms' rewards and revelations. The ranges are the
    /// owner's targets once confirmed; they fail when a data change breaks them.
    /// </summary>
    public class BalanceTargetsTests
    {
        private const int SeedCount = 60;

        private static readonly Dictionary<(BotKind, StepPolicy), List<BotRunResult>> Results =
            new Dictionary<(BotKind, StepPolicy), List<BotRunResult>>();

        private static List<BotRunResult> Play(BotKind kind, StepPolicy policy = StepPolicy.FullBiome)
        {
            if (!Results.TryGetValue((kind, policy), out var results))
            {
                results = BalanceReport.Play(AssetBalanceContent.Load(), kind, policy, SeedCount);
                Results[(kind, policy)] = results;
            }

            return results;
        }

        private static double WinPercent(List<BotRunResult> results) => 100d * results.Count(r => r.Won) / results.Count;

        private static double DeathPercent(List<BotRunResult> results, string stage) =>
            100d * results.Count(r => r.DeathStage != null && r.DeathStage.StartsWith(stage)) / results.Count;

        [Test, Timeout(900000)]
        public void Naive_NeverTouchingTheLine_LosesMostRuns()
        {
            Assert.LessOrEqual(WinPercent(Play(BotKind.Naive)), 25d);
        }

        [Test, Timeout(900000)]
        public void Intermediate_OrderingTheLine_WinsAboutHalfOfTheRuns()
        {
            Assert.That(WinPercent(Play(BotKind.Intermediate)), Is.InRange(40d, 70d));
        }

        [Test, Timeout(900000)]
        public void Expert_OptimisingTheLine_WinsMostRuns()
        {
            Assert.That(WinPercent(Play(BotKind.Expert)), Is.InRange(75d, 95d));
        }

        [Test, Timeout(900000)]
        public void Skill_IsRewarded()
        {
            Assert.Less(WinPercent(Play(BotKind.Naive)), WinPercent(Play(BotKind.Intermediate)));
            Assert.Less(WinPercent(Play(BotKind.Intermediate)), WinPercent(Play(BotKind.Expert)));
        }

        [Test, Timeout(900000)]
        public void RegularFights_AreSafeForAnOrderedLineAndPunishADisorderedOne()
        {
            Assert.LessOrEqual(DeathPercent(Play(BotKind.Intermediate), "Regular"), 5d);
            Assert.LessOrEqual(DeathPercent(Play(BotKind.Expert), "Regular"), 5d);
            Assert.Greater(DeathPercent(Play(BotKind.Naive), "Regular"), DeathPercent(Play(BotKind.Intermediate), "Regular"));
        }

        [Test, Timeout(900000)]
        public void MiniBosses_AreSpikes()
        {
            var naive = Play(BotKind.Naive);

            Assert.GreaterOrEqual(DeathPercent(naive, "MiniBoss"), 40d, "most runs of a player who never touches the line end on a mini-boss");
            var regularTicks = naive.Sum(r => r.RegularTicks) / (double)naive.Sum(r => r.RegularFights);
            var miniBossTicks = naive.Sum(r => r.MiniBossTicks) / (double)naive.Sum(r => r.MiniBossFights);
            Assert.GreaterOrEqual(miniBossTicks, 2d * regularTicks, "mini-boss fights are much longer than regular ones");
        }

        [Test, Timeout(900000)]
        public void Professor_IsAWallWithoutTheRoomsRewardsAndRevelations()
        {
            foreach (var kind in new[] { BotKind.Intermediate, BotKind.Expert })
            {
                var shortPath = WinPercent(Play(kind, StepPolicy.ShortPath));
                Assert.LessOrEqual(shortPath, 30d, kind.ToString());
                Assert.LessOrEqual(shortPath, WinPercent(Play(kind)) / 2d, kind.ToString());
            }
        }

        [Test, Timeout(900000)]
        public void ShortPath_LastsSixToSevenMinutesAtNormalSpeed()
        {
            var minutes = Play(BotKind.Intermediate, StepPolicy.ShortPath).Average(r => r.Seconds) / 60d;

            Assert.That(minutes, Is.InRange(5d, 9d));
        }

        [Test, Timeout(900000)]
        public void FightTimeLimit_IsAboveTheLongestFightOfTheSkilledBots()
        {
            var limit = AssetBalanceContent.Load().Rules.FightTimeLimit;
            var longest = new[] { BotKind.Intermediate, BotKind.Expert }
                .SelectMany(kind => new[] { StepPolicy.FullBiome, StepPolicy.ShortPath }.SelectMany(policy => Play(kind, policy)))
                .Max(r => r.LongestFight);

            Assert.Greater(limit, longest, "a legitimate fight must not reach the safety net");
            Assert.That(Play(BotKind.Expert).Count(r => r.TimedOut), Is.Zero);
        }
    }
}
