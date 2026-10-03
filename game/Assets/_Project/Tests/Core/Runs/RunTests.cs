using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Runs;
using NUnit.Framework;

namespace Game.Core.Tests.Runs
{
    public class RunTests
    {
        // Placeholder ids and test values: not game content or balance numbers.
        private const int HeroHealth = 20;
        private const int TimeLimit = 50;

        private static readonly CardDefinition Strike = new CardDefinition("TestStrike", 1, new IEffect[] { new DealDamageEffect(10) });
        private static readonly CardDefinition Guard = new CardDefinition("TestGuard", 1, new IEffect[] { new GainShieldEffect(5) });
        private static readonly CardDefinition EnemyHit = new CardDefinition("TestEnemyHit", 1, new IEffect[] { new DealDamageEffect(3) });
        private static readonly CardDefinition EnemySmash = new CardDefinition("TestEnemySmash", 1, new IEffect[] { new DealDamageEffect(50) });

        // Loses 10 per hero cast and hits back once: the hero wins on tick 2 with 17 health left.
        private static readonly EncounterDefinition Weak = Encounter("TestWeak", new EnemyDefinition("TestEnemy1", 15, 0, new[] { EnemyHit }));
        private static readonly EncounterDefinition OtherWeak = Encounter("TestOtherWeak", new EnemyDefinition("TestEnemy2", 15, 0, new[] { EnemyHit }));

        // Kills the hero on tick 1.
        private static readonly EncounterDefinition Strong = Encounter("TestStrong", new EnemyDefinition("TestEnemy3", 100, 0, new[] { EnemySmash }));

        // Deals no damage and outlasts the time limit.
        private static readonly EncounterDefinition Wall = Encounter("TestWall", new EnemyDefinition("TestEnemy4", 1000, 0, new[] { Guard }));

        private static readonly EncounterDefinition Professor = Encounter("TestProfessor", new EnemyDefinition("TestProfessor1", 15, 0, new[] { EnemyHit }));

        private static EncounterDefinition Encounter(string id, EnemyDefinition enemy)
        {
            return new EncounterDefinition(id, new[] { enemy });
        }

        private static Run CreateRun(
            EncounterDefinition[] pool = null,
            int minimumRegularFights = 2,
            EncounterDefinition professor = null,
            CardDefinition[] deck = null,
            int capacity = 4,
            ulong seed = 1)
        {
            var heroClass = new ClassDefinition("TestClass", HeroHealth, 0, capacity, deck ?? new[] { Strike }, new CardDefinition[0]);
            var biome = new BiomeDefinition("TestBiome", pool ?? new[] { Weak }, minimumRegularFights, professor ?? Professor);
            return new Run(heroClass, biome, new RunRules(TimeLimit), seed);
        }

        private static List<string> PlayRegularFights(Run run, int count)
        {
            var ids = new List<string>();
            for (var i = 0; i < count; i++)
            {
                ids.Add(run.Play(RunStep.RegularFight).Encounter.Id);
            }

            return ids;
        }

        // --- Start ---

        [Test]
        public void NewRun_LineHoldsTheStartingDeckAsDistinctInstances()
        {
            var run = CreateRun(deck: new[] { Strike, Strike });

            Assert.AreEqual(2, run.Line.Count);
            Assert.AreSame(Strike, run.Line[0].Definition);
            Assert.AreSame(Strike, run.Line[1].Definition);
            Assert.AreNotEqual(run.Line[0].Id, run.Line[1].Id);
            Assert.AreEqual(4, run.LineCapacity);
            Assert.IsEmpty(run.Reserve);
        }

        [Test]
        public void NewRun_IsInProgressWithOnlyTheRegularFight()
        {
            var run = CreateRun();

            Assert.AreEqual(RunOutcome.InProgress, run.Outcome);
            Assert.AreEqual(0, run.RegularFightsWon);
            CollectionAssert.AreEqual(new[] { RunStep.RegularFight }, run.AvailableSteps);
        }

        [Test]
        public void NewRun_NoMinimumFights_ProfessorIsAvailable()
        {
            var run = CreateRun(minimumRegularFights: 0);

            CollectionAssert.AreEqual(new[] { RunStep.RegularFight, RunStep.Professor }, run.AvailableSteps);
        }

        [Test]
        public void Constructor_NullArgument_Throws()
        {
            var run = CreateRun();
            Assert.Throws<ArgumentNullException>(() => new Run(null, run.Biome, run.Rules, 1));
            Assert.Throws<ArgumentNullException>(() => new Run(run.HeroClass, null, run.Rules, 1));
            Assert.Throws<ArgumentNullException>(() => new Run(run.HeroClass, run.Biome, null, 1));
        }

        // --- Fights ---

        [Test]
        public void Play_RegularFightWon_CountsItAndKeepsTheRunGoing()
        {
            var run = CreateRun();

            var report = run.Play(RunStep.RegularFight);

            Assert.IsTrue(report.HeroWon);
            Assert.AreSame(Weak, report.Encounter);
            Assert.AreEqual(1, run.RegularFightsWon);
            Assert.AreEqual(1, run.FightsPlayed);
            Assert.AreEqual(RunOutcome.InProgress, run.Outcome);
        }

        [Test]
        public void Play_MinimumRegularFightsWon_OpensTheProfessor()
        {
            var run = CreateRun(minimumRegularFights: 2);

            run.Play(RunStep.RegularFight);
            Assert.IsFalse(run.IsProfessorAvailable);
            run.Play(RunStep.RegularFight);

            Assert.IsTrue(run.IsProfessorAvailable);
            CollectionAssert.Contains(run.AvailableSteps, RunStep.Professor);
        }

        [Test]
        public void Play_ProfessorBeforeTheMinimum_Throws()
        {
            var run = CreateRun(minimumRegularFights: 1);

            Assert.Throws<InvalidOperationException>(() => run.Play(RunStep.Professor));
        }

        [Test]
        public void Play_FarmingAfterTheProfessorOpens_IsAllowed()
        {
            var run = CreateRun(minimumRegularFights: 1);

            PlayRegularFights(run, 3);

            Assert.AreEqual(3, run.RegularFightsWon);
            Assert.AreEqual(RunOutcome.InProgress, run.Outcome);
        }

        [Test]
        public void Play_ProfessorDefeated_WinsTheRun()
        {
            var run = CreateRun(minimumRegularFights: 0);

            var report = run.Play(RunStep.Professor);

            Assert.IsTrue(report.HeroWon);
            Assert.AreEqual(RunOutcome.Victory, run.Outcome);
            Assert.IsEmpty(run.AvailableSteps);
            Assert.AreEqual(0, run.RegularFightsWon);
        }

        [Test]
        public void Play_RegularFightLost_EndsTheRun()
        {
            var run = CreateRun(pool: new[] { Strong });

            var report = run.Play(RunStep.RegularFight);

            Assert.IsFalse(report.HeroWon);
            Assert.IsFalse(report.TimedOut);
            Assert.AreEqual(RunOutcome.Defeat, run.Outcome);
            Assert.IsEmpty(run.AvailableSteps);
        }

        [Test]
        public void Play_ProfessorLost_EndsTheRun()
        {
            var run = CreateRun(minimumRegularFights: 0, professor: Strong);

            run.Play(RunStep.Professor);

            Assert.AreEqual(RunOutcome.Defeat, run.Outcome);
        }

        [Test]
        public void Play_FightReachesTheTimeLimit_IsADefeat()
        {
            var run = CreateRun(pool: new[] { Wall });

            var report = run.Play(RunStep.RegularFight);

            Assert.IsTrue(report.TimedOut);
            Assert.AreEqual(TimeLimit, report.Log.Ticks);
            Assert.AreEqual(RunOutcome.Defeat, run.Outcome);
        }

        [Test]
        public void Play_AfterTheRunIsOver_Throws()
        {
            var run = CreateRun(pool: new[] { Strong });
            run.Play(RunStep.RegularFight);

            Assert.Throws<InvalidOperationException>(() => run.Play(RunStep.RegularFight));
            Assert.Throws<InvalidOperationException>(() => run.AddCard(Strike));
            Assert.Throws<InvalidOperationException>(() => run.MoveInLine(0, 0));
        }

        [Test]
        public void Play_NullStep_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CreateRun().Play(null));
        }

        [Test]
        public void Play_EveryFightStartsAtMaxHealth()
        {
            var run = CreateRun();

            var first = run.Play(RunStep.RegularFight);
            var second = run.Play(RunStep.RegularFight);

            var heroWasHit = false;
            foreach (var e in first.Log.Events)
            {
                heroWasHit |= e.TargetIndex == 0 && e.TargetHealth < HeroHealth;
            }

            Assert.IsTrue(heroWasHit, "The first fight should damage the hero.");
            Assert.AreEqual(HeroHealth, second.Log.Combatants[0].Health);
            Assert.AreEqual(0, second.Log.Combatants[0].Shield);
        }

        [Test]
        public void Play_UsesTheCurrentSpellLine()
        {
            var run = CreateRun();
            run.AddCard(Guard);

            var report = run.Play(RunStep.RegularFight);

            CollectionAssert.AreEqual(new[] { "TestStrike", "TestGuard" }, report.Log.Combatants[0].SpellLineCardIds);
        }

        // --- Determinism ---

        [Test]
        public void Play_SameSeedAndChoices_GiveTheSameRun()
        {
            var first = CreateRun(pool: new[] { Weak, OtherWeak }, seed: 42);
            var second = CreateRun(pool: new[] { Weak, OtherWeak }, seed: 42);

            for (var i = 0; i < 10; i++)
            {
                var a = first.Play(RunStep.RegularFight);
                var b = second.Play(RunStep.RegularFight);
                Assert.AreEqual(a.Encounter.Id, b.Encounter.Id);
                Assert.AreEqual(a.Log.ToJson(), b.Log.ToJson());
            }
        }

        [Test]
        public void Play_DifferentSeeds_DrawDifferentEncounters()
        {
            var first = PlayRegularFights(CreateRun(pool: new[] { Weak, OtherWeak }, seed: 1), 20);
            var second = PlayRegularFights(CreateRun(pool: new[] { Weak, OtherWeak }, seed: 2), 20);

            CollectionAssert.AreNotEqual(first, second);
        }

        [Test]
        public void Play_RegularFights_DrawEveryEncounterOfThePool()
        {
            var ids = PlayRegularFights(CreateRun(pool: new[] { Weak, OtherWeak }), 20);

            CollectionAssert.Contains(ids, "TestWeak");
            CollectionAssert.Contains(ids, "TestOtherWeak");
        }

        // --- Secret rooms ---

        [Test]
        public void UnlockSecretRoom_AddsItsStepBeforeTheProfessor()
        {
            var run = CreateRun(minimumRegularFights: 0);

            run.UnlockSecretRoom("TestRoom", Weak);

            CollectionAssert.AreEqual(
                new[] { RunStep.RegularFight, RunStep.SecretRoom("TestRoom"), RunStep.Professor }, run.AvailableSteps);
        }

        [Test]
        public void UnlockSecretRoom_Twice_KeepsOneStep()
        {
            var run = CreateRun();

            run.UnlockSecretRoom("TestRoom", Weak);
            run.UnlockSecretRoom("TestRoom", OtherWeak);

            Assert.AreEqual(2, run.AvailableSteps.Count);
        }

        [Test]
        public void Play_SecretRoom_FightsItsEncounterWithoutCountingAsRegular()
        {
            var run = CreateRun();
            run.UnlockSecretRoom("TestRoom", OtherWeak);

            var report = run.Play(RunStep.SecretRoom("TestRoom"));

            Assert.AreSame(OtherWeak, report.Encounter);
            Assert.AreEqual(0, run.RegularFightsWon);
            CollectionAssert.Contains(run.AvailableSteps, RunStep.SecretRoom("TestRoom"));
        }

        [Test]
        public void Play_LockedSecretRoom_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => CreateRun().Play(RunStep.SecretRoom("TestRoom")));
        }

        [Test]
        public void UnlockSecretRoom_InvalidArguments_Throw()
        {
            var run = CreateRun();
            Assert.Throws<ArgumentException>(() => run.UnlockSecretRoom(" ", Weak));
            Assert.Throws<ArgumentNullException>(() => run.UnlockSecretRoom("TestRoom", null));
        }

        // --- Cards, line and reserve ---

        [Test]
        public void AddCard_FreeSlot_GoesToTheEndOfTheLine()
        {
            var run = CreateRun();

            var card = run.AddCard(Guard);

            Assert.AreSame(card, run.Line[1]);
            Assert.AreSame(Guard, card.Definition);
            Assert.AreNotEqual(run.Line[0].Id, card.Id);
        }

        [Test]
        public void AddCard_LineFull_GoesToTheReserve()
        {
            var run = CreateRun(capacity: 1);

            var card = run.AddCard(Guard);

            Assert.AreEqual(1, run.Line.Count);
            CollectionAssert.AreEqual(new[] { card }, run.Reserve);
        }

        [Test]
        public void IncreaseLineCapacity_AddsSlotsAndKeepsTheLine()
        {
            var run = CreateRun(capacity: 1);
            var first = run.Line[0];
            run.AddCard(Guard);

            run.IncreaseLineCapacity(1);
            run.MoveFromReserve(0);

            Assert.AreEqual(2, run.LineCapacity);
            Assert.AreSame(first, run.Line[0]);
            Assert.AreSame(Guard, run.Line[1].Definition);
            Assert.IsEmpty(run.Reserve);
        }

        [Test]
        public void IncreaseLineCapacity_BelowOne_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateRun().IncreaseLineCapacity(0));
        }

        [Test]
        public void MoveFromReserve_LineFull_Throws()
        {
            var run = CreateRun(capacity: 1);
            run.AddCard(Guard);

            Assert.Throws<InvalidOperationException>(() => run.MoveFromReserve(0));
        }

        [Test]
        public void SwapWithReserve_ExchangesThePlaces()
        {
            var run = CreateRun(deck: new[] { Strike, Strike }, capacity: 2);
            var lineCard = run.Line[0];
            var reserveCard = run.AddCard(Guard);

            run.SwapWithReserve(0, 0);

            Assert.AreSame(reserveCard, run.Line[0]);
            CollectionAssert.AreEqual(new[] { lineCard }, run.Reserve);
            Assert.AreEqual(2, run.Line.Count);
        }

        [Test]
        public void SwapWithReserve_InvalidIndex_ThrowsAndChangesNothing()
        {
            var run = CreateRun();
            var line = new List<CardInstance>(run.Line);

            Assert.Throws<ArgumentOutOfRangeException>(() => run.SwapWithReserve(0, 0));
            CollectionAssert.AreEqual(line, run.Line);
        }

        [Test]
        public void MoveToReserve_MovesTheCard()
        {
            var run = CreateRun();
            var guard = run.AddCard(Guard);

            run.MoveToReserve(1);

            Assert.AreEqual(1, run.Line.Count);
            CollectionAssert.AreEqual(new[] { guard }, run.Reserve);
        }

        [Test]
        public void MoveToReserve_LastCardOfTheLine_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => CreateRun().MoveToReserve(0));
        }

        [Test]
        public void MoveInLine_ReordersTheLine()
        {
            var run = CreateRun();
            var strike = run.Line[0];
            var guard = run.AddCard(Guard);

            run.MoveInLine(1, 0);

            CollectionAssert.AreEqual(new[] { guard, strike }, run.Line);
        }
    }
}
