using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Randomness;
using Game.Core.Runs;
using Game.Core.Upgrades;
using NUnit.Framework;

namespace Game.Core.Tests.Runs
{
    public class LevelUpOfferTests
    {
        // Placeholder ids and test values: not game content or balance numbers.
        private static readonly LevelCurve Curve = new LevelCurve(new[] { 10, 20 }, 5);

        private static readonly CardDefinition Strike = new CardDefinition("TestStrike", 1, new IEffect[] { new DealDamageEffect(10) });
        private static readonly CardDefinition EnemyHit = new CardDefinition("TestEnemyHit", 1, new IEffect[] { new DealDamageEffect(3) });
        private static readonly CardDefinition PoolA = new CardDefinition("TestPoolA", 1, new IEffect[] { new DealDamageEffect(1) });
        private static readonly CardDefinition PoolB = new CardDefinition("TestPoolB", 1, new IEffect[] { new DealDamageEffect(2) });
        private static readonly CardDefinition PoolC = new CardDefinition("TestPoolC", 1, new IEffect[] { new DealDamageEffect(3) });
        private static readonly CardDefinition PoolD = new CardDefinition("TestPoolD", 1, new IEffect[] { new DealDamageEffect(4) });

        private static readonly PassiveUpgrade Health = new PassiveUpgrade("TestHealth", PassiveUpgradeKind.MaxHealth, 5);
        private static readonly PassiveUpgrade Shield = new PassiveUpgrade("TestShield", PassiveUpgradeKind.StartingShield, 2);
        private static readonly PassiveUpgrade Damage = new PassiveUpgrade("TestDamage", PassiveUpgradeKind.EffectAmount, BonusKind.Damage, 1);

        private static readonly CardDefinition[] CardPool = { PoolA, PoolB, PoolC, PoolD };
        private static readonly PassiveUpgrade[] PassivePool = { Health, Shield, Damage };

        // Wins in two casts and gives enough XP for two level-ups at once.
        private static readonly EncounterDefinition Rich = new EncounterDefinition(
            "TestRich", new[] { new EnemyDefinition("TestEnemy1", 15, 0, new[] { EnemyHit }, 30) });

        private static Run CreateRun(int capacity = 4, ulong seed = 1, CardDefinition[] pool = null, CardDefinition[] deck = null)
        {
            var heroClass = new ClassDefinition(
                "TestClass", 20, 0, capacity, deck ?? new[] { Strike }, pool ?? CardPool);
            var professor = new EncounterDefinition(
                "TestProfessor", new[] { new EnemyDefinition("TestProfessor1", 15, 0, new[] { EnemyHit }) });
            var biome = new BiomeDefinition("TestBiome", new[] { Rich }, 2, professor, professor.Enemies[0]);
            return new Run(heroClass, biome, new RunRules(50, Curve), seed);
        }

        private static Run RunWithPendingLevelUps(int capacity = 4, ulong seed = 1, CardDefinition[] pool = null, CardDefinition[] deck = null)
        {
            var run = CreateRun(capacity, seed, pool, deck);
            run.Play(RunStep.RegularFight);
            Assert.AreEqual(2, run.PendingLevelUps);
            return run;
        }

        private static string Describe(LevelUpOffer offer)
        {
            var parts = new List<string>();
            foreach (var package in offer.Packages)
            {
                parts.Add(package.Card.Id + "+" + package.Passive.Id);
            }

            return string.Join(",", parts);
        }

        // --- Generator ---

        [Test]
        public void Generate_OffersThreePackagesFromThePools()
        {
            var offer = LevelUpOfferGenerator.Generate(CardPool, PassivePool, new Pcg32Random(7));

            Assert.AreEqual(3, offer.Packages.Count);
            foreach (var package in offer.Packages)
            {
                CollectionAssert.Contains(CardPool, package.Card);
                CollectionAssert.Contains(PassivePool, package.Passive);
            }
        }

        [Test]
        public void Generate_SameSeed_GivesTheSameOffers()
        {
            var first = new Pcg32Random(42, 1);
            var second = new Pcg32Random(42, 1);

            for (var i = 0; i < 5; i++)
            {
                Assert.AreEqual(
                    Describe(LevelUpOfferGenerator.Generate(CardPool, PassivePool, first)),
                    Describe(LevelUpOfferGenerator.Generate(CardPool, PassivePool, second)));
            }
        }

        [Test]
        public void Generate_DifferentSeeds_GiveDifferentOffersSomewhere()
        {
            var offers = new HashSet<string>();
            for (ulong seed = 1; seed <= 20; seed++)
            {
                offers.Add(Describe(LevelUpOfferGenerator.Generate(CardPool, PassivePool, new Pcg32Random(seed, 1))));
            }

            Assert.Greater(offers.Count, 1);
        }

        [Test]
        public void Generate_SingleCardPool_OffersDuplicates()
        {
            var offer = LevelUpOfferGenerator.Generate(new[] { PoolA }, new[] { Health }, new Pcg32Random(1));

            foreach (var package in offer.Packages)
            {
                Assert.AreSame(PoolA, package.Card);
                Assert.AreSame(Health, package.Passive);
            }
        }

        [Test]
        public void Generate_EveryPoolEntryCanBeOffered()
        {
            var cards = new HashSet<string>();
            var passives = new HashSet<string>();
            var random = new Pcg32Random(3);
            for (var i = 0; i < 100; i++)
            {
                foreach (var package in LevelUpOfferGenerator.Generate(CardPool, PassivePool, random).Packages)
                {
                    cards.Add(package.Card.Id);
                    passives.Add(package.Passive.Id);
                }
            }

            Assert.AreEqual(CardPool.Length, cards.Count);
            Assert.AreEqual(PassivePool.Length, passives.Count);
        }

        [Test]
        public void Generate_EmptyPool_Throws()
        {
            var random = new Pcg32Random(1);
            Assert.Throws<InvalidOperationException>(
                () => LevelUpOfferGenerator.Generate(new CardDefinition[0], PassivePool, random));
            Assert.Throws<InvalidOperationException>(
                () => LevelUpOfferGenerator.Generate(CardPool, new PassiveUpgrade[0], random));
        }

        [Test]
        public void Generate_BadArguments_Throw()
        {
            var random = new Pcg32Random(1);
            Assert.Throws<ArgumentNullException>(() => LevelUpOfferGenerator.Generate(null, PassivePool, random));
            Assert.Throws<ArgumentNullException>(() => LevelUpOfferGenerator.Generate(CardPool, null, random));
            Assert.Throws<ArgumentNullException>(() => LevelUpOfferGenerator.Generate(CardPool, PassivePool, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => LevelUpOfferGenerator.Generate(CardPool, PassivePool, random, 0));
        }

        [Test]
        public void LevelUpOffer_Constructor_RejectsEmptyAndNull()
        {
            Assert.Throws<ArgumentNullException>(() => new LevelUpOffer(null));
            Assert.Throws<ArgumentException>(() => new LevelUpOffer(new LevelUpPackage[0]));
            Assert.Throws<ArgumentNullException>(() => new LevelUpOffer(new LevelUpPackage[] { null }));
            Assert.Throws<ArgumentNullException>(() => new LevelUpPackage(null, Health));
            Assert.Throws<ArgumentNullException>(() => new LevelUpPackage(PoolA, null));
        }

        // --- Run offers ---

        [Test]
        public void HasPendingChoice_FollowsPendingLevelUps()
        {
            var run = CreateRun();
            Assert.IsFalse(run.HasPendingChoice);

            run.Play(RunStep.RegularFight);

            Assert.IsTrue(run.HasPendingChoice);
        }

        [Test]
        public void Play_WithPendingLevelUp_StillStartsAFight()
        {
            // The choice is not forced before the next fight (decision left to the caller, see HasPendingChoice).
            var run = RunWithPendingLevelUps();

            Assert.DoesNotThrow(() => run.Play(RunStep.RegularFight));
        }

        [Test]
        public void GetLevelUpOffer_UsesTheClassPoolAndThePassivePool()
        {
            var run = RunWithPendingLevelUps();

            var offer = run.GetLevelUpOffer(PassivePool);

            Assert.AreEqual(3, offer.Packages.Count);
            foreach (var package in offer.Packages)
            {
                CollectionAssert.Contains(run.HeroClass.CardPool, package.Card);
                CollectionAssert.Contains(PassivePool, package.Passive);
            }
        }

        [Test]
        public void GetLevelUpOffer_AskedAgain_GivesTheSameOffer()
        {
            var run = RunWithPendingLevelUps();

            Assert.AreSame(run.GetLevelUpOffer(PassivePool), run.GetLevelUpOffer(PassivePool));
        }

        [Test]
        public void GetLevelUpOffer_SameSeedAndChoices_GiveTheSameOffers()
        {
            var first = RunWithPendingLevelUps(seed: 9);
            var second = RunWithPendingLevelUps(seed: 9);

            var firstOffer = Describe(first.GetLevelUpOffer(PassivePool));
            first.TakeLevelUpPackage(1);
            var firstNext = Describe(first.GetLevelUpOffer(PassivePool));
            var secondOffer = Describe(second.GetLevelUpOffer(PassivePool));
            second.TakeLevelUpPackage(1);
            var secondNext = Describe(second.GetLevelUpOffer(PassivePool));

            Assert.AreEqual(firstOffer, secondOffer);
            Assert.AreEqual(firstNext, secondNext);
        }

        [Test]
        public void GetLevelUpOffer_NextLevelUp_DrawsANewOffer()
        {
            var run = RunWithPendingLevelUps(seed: 5);
            var offers = new HashSet<string> { Describe(run.GetLevelUpOffer(PassivePool)) };

            run.TakeLevelUpPackage(0);
            offers.Add(Describe(run.GetLevelUpOffer(PassivePool)));

            // Two draws with the 4x3 pools can coincide, so compare to what the offer sequence gives.
            var random = new Pcg32Random(5, Run.LevelUpOfferSequence);
            var expected = new HashSet<string>
            {
                Describe(LevelUpOfferGenerator.Generate(CardPool, PassivePool, random)),
                Describe(LevelUpOfferGenerator.Generate(CardPool, PassivePool, random)),
            };
            CollectionAssert.AreEquivalent(expected, offers);
        }

        [Test]
        public void GetLevelUpOffer_UsesOnlyTheOfferSequence()
        {
            // Fights draw on other sequences: fighting again does not change the offer.
            var played = RunWithPendingLevelUps(seed: 11);
            played.Play(RunStep.RegularFight);
            var fresh = RunWithPendingLevelUps(seed: 11);

            Assert.AreEqual(Describe(fresh.GetLevelUpOffer(PassivePool)), Describe(played.GetLevelUpOffer(PassivePool)));
        }

        [Test]
        public void GetLevelUpOffer_NoPendingLevelUp_Throws()
        {
            var run = CreateRun();

            Assert.Throws<InvalidOperationException>(() => run.GetLevelUpOffer(PassivePool));
        }

        [Test]
        public void GetLevelUpOffer_EmptyClassPool_Throws()
        {
            var run = RunWithPendingLevelUps(pool: new CardDefinition[0]);

            Assert.Throws<InvalidOperationException>(() => run.GetLevelUpOffer(PassivePool));
            Assert.AreEqual(2, run.PendingLevelUps);
        }

        [Test]
        public void GetLevelUpOffer_NullPool_Throws()
        {
            var run = RunWithPendingLevelUps();

            Assert.Throws<ArgumentNullException>(() => run.GetLevelUpOffer(null));
        }

        // --- Taking a package ---

        [Test]
        public void TakeLevelUpPackage_LineHasRoom_AddsCardAtTheEndAndPassiveAndConsumesOneLevelUp()
        {
            var run = RunWithPendingLevelUps();
            var package = run.GetLevelUpOffer(PassivePool).Packages[2];

            var added = run.TakeLevelUpPackage(2);

            Assert.AreEqual(2, run.Line.Count);
            Assert.AreSame(added, run.Line[1]);
            Assert.AreEqual(package.Card.Id, added.Definition.Id);
            Assert.IsEmpty(run.Reserve);
            CollectionAssert.Contains(run.Upgrades.Upgrades, package.Passive);
            Assert.AreEqual(1, run.PendingLevelUps);
        }

        [Test]
        public void TakeLevelUpPackage_LineFull_AddsCardToTheReserve()
        {
            var run = RunWithPendingLevelUps(capacity: 1);
            var package = run.GetLevelUpOffer(PassivePool).Packages[0];
            var lineCard = run.Line[0];

            var added = run.TakeLevelUpPackage(0);

            Assert.AreEqual(1, run.Line.Count);
            Assert.AreSame(lineCard, run.Line[0]);
            Assert.AreEqual(1, run.Reserve.Count);
            Assert.AreSame(added, run.Reserve[0]);
            Assert.AreEqual(package.Card.Id, added.Definition.Id);
            CollectionAssert.Contains(run.Upgrades.Upgrades, package.Passive);
            Assert.AreEqual(1, run.PendingLevelUps);
        }

        [Test]
        public void TakeLevelUpPackage_ReplacingALineCard_SendsItToTheReserve()
        {
            var run = RunWithPendingLevelUps(capacity: 2, deck: new[] { Strike, EnemyHit });
            var replaced = run.Line[0];
            var kept = run.Line[1];
            var package = run.GetLevelUpOffer(PassivePool).Packages[1];

            var added = run.TakeLevelUpPackage(1, replacedLinePosition: 0);

            Assert.AreEqual(2, run.Line.Count);
            Assert.AreSame(added, run.Line[0]);
            Assert.AreSame(kept, run.Line[1]);
            Assert.AreEqual(package.Card.Id, added.Definition.Id);
            Assert.AreEqual(1, run.Reserve.Count);
            Assert.AreSame(replaced, run.Reserve[0]);
            Assert.AreEqual(1, run.PendingLevelUps);
        }

        [Test]
        public void TakeLevelUpPackage_NewInstanceIsDistinctFromOwnedCopies()
        {
            // Duplicates: the pool card may already be owned; the new copy is a new instance.
            var run = RunWithPendingLevelUps(pool: new[] { Strike });
            run.GetLevelUpOffer(PassivePool);

            var added = run.TakeLevelUpPackage(0);

            Assert.AreNotSame(run.Line[0], added);
            Assert.AreNotEqual(run.Line[0].Id, added.Id);
            Assert.AreEqual(2, run.Line.Count);
        }

        [Test]
        public void TakeLevelUpPackage_PassiveAppliesToLaterFights()
        {
            var run = RunWithPendingLevelUps(pool: new[] { PoolA });
            var offer = run.GetLevelUpOffer(new[] { Damage });

            run.TakeLevelUpPackage(0);

            Assert.AreEqual(1, run.Upgrades.EffectBonus.Damage);
            Assert.AreSame(Damage, offer.Packages[0].Passive);
        }

        [Test]
        public void TakeLevelUpPackage_TakingAllPendingLevelUps_ClearsTheChoice()
        {
            var run = RunWithPendingLevelUps();

            run.GetLevelUpOffer(PassivePool);
            run.TakeLevelUpPackage(0);
            run.GetLevelUpOffer(PassivePool);
            run.TakeLevelUpPackage(0);

            Assert.AreEqual(0, run.PendingLevelUps);
            Assert.IsFalse(run.HasPendingChoice);
            Assert.AreEqual(3, run.Line.Count);
            Assert.Throws<InvalidOperationException>(() => run.GetLevelUpOffer(PassivePool));
        }

        [Test]
        public void TakeLevelUpPackage_BadIndex_ChangesNothing()
        {
            var run = RunWithPendingLevelUps();
            run.GetLevelUpOffer(PassivePool);

            Assert.Throws<ArgumentOutOfRangeException>(() => run.TakeLevelUpPackage(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => run.TakeLevelUpPackage(3));

            AssertUntouched(run);
        }

        [Test]
        public void TakeLevelUpPackage_BadReplacePosition_ChangesNothing()
        {
            var run = RunWithPendingLevelUps(capacity: 1);
            run.GetLevelUpOffer(PassivePool);

            Assert.Throws<ArgumentOutOfRangeException>(() => run.TakeLevelUpPackage(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => run.TakeLevelUpPackage(0, -1));

            Assert.AreEqual(2, run.PendingLevelUps);
            Assert.IsEmpty(run.Reserve);
            Assert.IsTrue(run.Upgrades.IsEmpty);
        }

        [Test]
        public void TakeLevelUpPackage_ReplacingWhileTheLineHasRoom_ChangesNothing()
        {
            var run = RunWithPendingLevelUps();
            run.GetLevelUpOffer(PassivePool);

            Assert.Throws<InvalidOperationException>(() => run.TakeLevelUpPackage(0, 0));

            AssertUntouched(run);
        }

        [Test]
        public void TakeLevelUpPackage_UpgradeOverflow_ChangesNothing()
        {
            var huge = new PassiveUpgrade("TestHuge", PassiveUpgradeKind.MaxHealth, int.MaxValue);
            var run = RunWithPendingLevelUps(pool: new[] { PoolA });
            run.GetLevelUpOffer(new[] { huge });
            run.TakeLevelUpPackage(0);
            run.GetLevelUpOffer(new[] { huge });

            Assert.Throws<OverflowException>(() => run.TakeLevelUpPackage(0));

            Assert.AreEqual(1, run.PendingLevelUps);
            Assert.AreEqual(2, run.Line.Count);
            Assert.IsEmpty(run.Reserve);
            Assert.AreEqual(1, run.Upgrades.Upgrades.Count);
        }

        [Test]
        public void TakeLevelUpPackage_WithoutOffer_Throws()
        {
            var run = RunWithPendingLevelUps();

            Assert.Throws<InvalidOperationException>(() => run.TakeLevelUpPackage(0));
            AssertUntouched(run);
        }

        [Test]
        public void TakeLevelUpPackage_FightSessionOpen_ThrowsAndChangesNothing()
        {
            var run = RunWithPendingLevelUps();
            run.GetLevelUpOffer(PassivePool);
            var session = run.BeginFight(RunStep.RegularFight);

            Assert.Throws<InvalidOperationException>(() => run.TakeLevelUpPackage(0));
            Assert.AreEqual(2, run.PendingLevelUps);
            Assert.IsTrue(run.Upgrades.IsEmpty);

            session.Cancel();
        }

        private static void AssertUntouched(Run run)
        {
            Assert.AreEqual(2, run.PendingLevelUps);
            Assert.AreEqual(1, run.Line.Count);
            Assert.IsEmpty(run.Reserve);
            Assert.IsTrue(run.Upgrades.IsEmpty);
        }
    }
}
