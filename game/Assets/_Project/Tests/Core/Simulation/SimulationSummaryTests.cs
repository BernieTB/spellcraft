using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Effects;
using Game.Core.Simulation;
using NUnit.Framework;

namespace Game.Core.Tests.Simulation
{
    public class SimulationSummaryTests
    {
        // Placeholder ids and arbitrary test data, not real content or balance values.
        private static readonly CardDefinition CardA = new CardDefinition("test_card_a", 1, Array.Empty<IEffect>());
        private static readonly CardDefinition CardB = new CardDefinition("test_card_b", 1, Array.Empty<IEffect>());

        private static CastRecord Cast(int caster, CardDefinition card, params EffectOutcome[] outcomes) =>
            new CastRecord(1, caster, 0, card, caster == Fight.HeroIndex ? 1 : Fight.HeroIndex, outcomes);

        private static EffectOutcome Damage(int absorbed, int healthLost) =>
            EffectOutcome.FromDamage(new DamageResult(absorbed, healthLost));

        private static FightResult Result(FightWinner winner, int ticks, params CastRecord[] casts) =>
            new FightResult(winner, ticks, casts);

        private static SimulationSummary Summarise(params FightResult[] results)
        {
            var builder = new SimulationSummaryBuilder();
            foreach (var result in results)
            {
                builder.Add(result);
            }

            return builder.Build(10L);
        }

        [Test]
        public void Build_NoFight_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new SimulationSummaryBuilder().Build(0L));
        }

        [Test]
        public void Add_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SimulationSummaryBuilder().Add(null));
        }

        [Test]
        public void Build_CountsOutcomesAndRates()
        {
            var summary = Summarise(
                Result(FightWinner.Hero, 1),
                Result(FightWinner.Hero, 1),
                Result(FightWinner.Enemies, 1),
                Result(FightWinner.None, 1));

            Assert.AreEqual(4, summary.FightCount);
            Assert.AreEqual(2, summary.HeroWins);
            Assert.AreEqual(1, summary.EnemyWins);
            Assert.AreEqual(1, summary.Timeouts);
            Assert.AreEqual(0.5, summary.HeroWinRate);
            Assert.AreEqual(0.25, summary.EnemyWinRate);
            Assert.AreEqual(0.25, summary.TimeoutRate);
        }

        [Test]
        public void Build_ComputesTickAverageMinAndMax()
        {
            var summary = Summarise(Result(FightWinner.Hero, 3), Result(FightWinner.Hero, 10), Result(FightWinner.Hero, 8));

            Assert.AreEqual(21L, summary.TotalTicks);
            Assert.AreEqual(7.0, summary.AverageTicks);
            Assert.AreEqual(3, summary.MinTicks);
            Assert.AreEqual(10, summary.MaxTicks);
        }

        [Test]
        public void Build_SeedRangeOverflows_Throws()
        {
            var builder = new SimulationSummaryBuilder();
            builder.Add(Result(FightWinner.Hero, 1));
            builder.Add(Result(FightWinner.Hero, 1));

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Build(long.MaxValue));
        }

        [Test]
        public void Build_SumsDamagePerSideAndCard_HealthLostPlusShieldAbsorbed()
        {
            var summary = Summarise(
                Result(FightWinner.Hero, 5,
                    Cast(Fight.HeroIndex, CardA, Damage(2, 3)),
                    Cast(Fight.HeroIndex, CardA, Damage(0, 4), EffectOutcome.FromHeal(1)),
                    Cast(1, CardA, Damage(1, 0))),
                Result(FightWinner.Hero, 5,
                    Cast(Fight.HeroIndex, CardA, Damage(0, 1), EffectOutcome.FromShieldGain(2))));

            var hero = summary.Cards[0];
            Assert.AreEqual(SimulationSide.Hero, hero.Side);
            Assert.AreEqual("test_card_a", hero.CardId);
            Assert.AreEqual(3L, hero.Casts);
            Assert.AreEqual(8L, hero.HealthLost);
            Assert.AreEqual(2L, hero.ShieldAbsorbed);
            Assert.AreEqual(10L, hero.Damage);
            Assert.AreEqual(5.0, hero.AverageDamagePerFight);
            Assert.AreEqual(1L, hero.Healed);
            Assert.AreEqual(2L, hero.ShieldGained);

            var enemy = summary.Cards[1];
            Assert.AreEqual(SimulationSide.Enemies, enemy.Side);
            Assert.AreEqual(1L, enemy.Damage);
            Assert.AreEqual(0.5, enemy.AverageDamagePerFight);
        }

        [Test]
        public void Build_OrdersCardsBySideThenId()
        {
            var summary = Summarise(Result(FightWinner.Hero, 1,
                Cast(2, CardB),
                Cast(Fight.HeroIndex, CardB),
                Cast(1, CardA),
                Cast(Fight.HeroIndex, CardA)));

            var order = new List<string>();
            foreach (var card in summary.Cards)
            {
                order.Add(SimulationSummary.SideName(card.Side) + ":" + card.CardId);
            }

            CollectionAssert.AreEqual(
                new[] { "hero:test_card_a", "hero:test_card_b", "enemies:test_card_a", "enemies:test_card_b" },
                order);
        }

        [Test]
        public void ToJson_WritesEveryFieldInFixedOrder()
        {
            var summary = Summarise(
                Result(FightWinner.Hero, 4, Cast(Fight.HeroIndex, CardA, Damage(1, 2))),
                Result(FightWinner.Enemies, 2),
                Result(FightWinner.None, 3));

            const string expected =
                "{\n"
                + "  \"fightCount\":3,\n"
                + "  \"firstSeed\":10,\n"
                + "  \"lastSeed\":12,\n"
                + "  \"outcomes\":{\n"
                + "    \"hero\":{\"count\":1,\"rate\":0.3333},\n"
                + "    \"enemies\":{\"count\":1,\"rate\":0.3333},\n"
                + "    \"timeout\":{\"count\":1,\"rate\":0.3333}\n"
                + "  },\n"
                + "  \"ticks\":{\n"
                + "    \"average\":3.0,\n"
                + "    \"min\":2,\n"
                + "    \"max\":4,\n"
                + "    \"total\":9\n"
                + "  },\n"
                + "  \"cards\":[\n"
                + "    {\"side\":\"hero\",\"cardId\":\"test_card_a\",\"casts\":1,\"damage\":3,\"healthLost\":2,"
                + "\"shieldAbsorbed\":1,\"averageDamagePerFight\":1.0,\"healed\":0,\"shieldGained\":0}\n"
                + "  ]\n"
                + "}\n";
            Assert.AreEqual(expected, summary.ToJson());
        }

        [Test]
        public void ToJson_RoundsHalfAwayFromZero_WithInvariantCulture()
        {
            // 1 win in 32 fights: a rate of exactly 0.03125, a midpoint at 4 decimals.
            var results = new FightResult[32];
            results[0] = Result(FightWinner.Hero, 1);
            for (var i = 1; i < results.Length; i++)
            {
                results[i] = Result(FightWinner.Enemies, 1);
            }

            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                StringAssert.Contains("\"hero\":{\"count\":1,\"rate\":0.0313}", Summarise(results).ToJson());
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void ToJson_NoCardCast_WritesEmptyArray()
        {
            StringAssert.Contains("\"cards\":[]\n}", Summarise(Result(FightWinner.None, 1)).ToJson());
        }
    }
}
