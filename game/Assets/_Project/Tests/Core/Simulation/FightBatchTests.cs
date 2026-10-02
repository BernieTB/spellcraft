using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.Simulation;
using Game.Core.SpellLines;
using NUnit.Framework;

namespace Game.Core.Tests.Simulation
{
    public class FightBatchTests
    {
        // Placeholder ids and arbitrary test data, not real content or balance values.
        private const int MaxTicks = 200;

        private static FightParticipant Participant(int health, int shield, params CardDefinition[] cards)
        {
            var line = new SpellLine<CardDefinition>(cards.Length);
            foreach (var card in cards)
            {
                line.Add(card);
            }

            return new FightParticipant(new Combatant(health, shield), line);
        }

        // Fresh combatants for every fight: a fight changes its combatants.
        private static Fight CreateFight(long seed)
        {
            var strike = new CardDefinition("test_card_01", 1, new IEffect[] { new DealDamageEffect(2) });
            var guard = new CardDefinition("test_card_02", 2, new IEffect[] { new GainShieldEffect(1), new DealDamageEffect(1) });
            var hero = Participant(30, 0, strike, guard);
            var enemies = new[] { Participant(12, 2, strike), Participant(8, 0, guard) };
            return new Fight(hero, enemies, MaxTicks, new Pcg32Random(unchecked((ulong)seed)));
        }

        [Test]
        public void Run_SameSeeds_GiveIdenticalJson()
        {
            var first = FightBatch.Run(5L, 50, CreateFight).ToJson();
            var second = FightBatch.Run(5L, 50, CreateFight).ToJson();

            Assert.AreEqual(first, second);
        }

        [Test]
        public void Run_UsesConsecutiveSeedsFromTheFirst()
        {
            var seeds = new List<long>();
            var summary = FightBatch.Run(-1L, 3, seed =>
            {
                seeds.Add(seed);
                return CreateFight(seed);
            });

            CollectionAssert.AreEqual(new[] { -1L, 0L, 1L }, seeds);
            Assert.AreEqual(-1L, summary.FirstSeed);
            Assert.AreEqual(1L, summary.LastSeed);
        }

        [Test]
        public void Run_MatchesTheFightsRunOneByOne()
        {
            var builder = new SimulationSummaryBuilder();
            for (var seed = 0L; seed < 4L; seed++)
            {
                builder.Add(CreateFight(seed).Run());
            }

            Assert.AreEqual(builder.Build(0L).ToJson(), FightBatch.Run(0L, 4, CreateFight).ToJson());
        }

        [Test]
        public void Run_NullFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => FightBatch.Run(0L, 1, null));
        }

        [Test]
        public void Run_FactoryReturnsNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => FightBatch.Run(0L, 1, seed => null));
        }

        [Test]
        public void Run_NoFight_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FightBatch.Run(0L, 0, CreateFight));
        }

        [Test]
        public void Run_SeedRangeOverflows_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FightBatch.Run(long.MaxValue, 2, CreateFight));
        }
    }
}
