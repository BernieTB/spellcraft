using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Combat;
using Game.Core.Combat.Log;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Runs;
using Game.Core.Upgrades;
using NUnit.Framework;

namespace Game.Core.Tests.Runs
{
    /// <summary>
    /// Passive upgrades and live line editing wired into the run (issue #106, ADR 0012 and ADR 0015).
    /// </summary>
    public class RunFightSessionTests
    {
        // Placeholder ids and arbitrary test values, not game content or balance numbers.
        private const int HeroHealth = 20;
        private const int TimeLimit = 100;

        private static readonly LevelCurve Curve = new LevelCurve(new[] { 10, 20 }, 5);

        private static readonly CardDefinition Strike = new CardDefinition("TestStrike", 1, new IEffect[] { new DealDamageEffect(10) });
        private static readonly CardDefinition Smite = new CardDefinition("TestSmite", 1, new IEffect[] { new DealDamageEffect(20) });
        private static readonly CardDefinition Guard = new CardDefinition("TestGuard", 1, new IEffect[] { new GainShieldEffect(5) });
        private static readonly CardDefinition Nibble = new CardDefinition("TestNibble", 1, new IEffect[] { new DealDamageEffect(1) });
        private static readonly CardDefinition Smash = new CardDefinition("TestSmash", 1, new IEffect[] { new DealDamageEffect(50) });

        // Lasts several ticks and barely hurts the hero, so line changes have time to happen.
        private static readonly EncounterDefinition Tank =
            new EncounterDefinition("TestTank", new[] { new EnemyDefinition("TestTankEnemy", 60, 0, new[] { Nibble }, 4) });

        private static readonly EncounterDefinition OtherTank =
            new EncounterDefinition("TestOtherTank", new[] { new EnemyDefinition("TestOtherTankEnemy", 60, 0, new[] { Nibble }) });

        private static readonly EncounterDefinition Deadly =
            new EncounterDefinition("TestDeadly", new[] { new EnemyDefinition("TestDeadlyEnemy", 500, 0, new[] { Smash }) });

        // --- Helpers ---

        private static Run CreateRun(
            ulong seed = 1,
            CardDefinition[] deck = null,
            int capacity = 2,
            EncounterDefinition[] pool = null,
            int minimumRegularFights = 1,
            EncounterDefinition professor = null)
        {
            var heroClass = new ClassDefinition(
                "TestClass", HeroHealth, 0, capacity, deck ?? new[] { Strike, Guard }, new CardDefinition[0]);
            var biome = new BiomeDefinition("TestBiome", pool ?? new[] { Tank }, minimumRegularFights, professor ?? Tank);
            return new Run(heroClass, biome, new RunRules(TimeLimit, Curve), seed);
        }

        private static List<string> Ids(IEnumerable<CardInstance> cards) => cards.Select(c => c.Definition.Id).ToList();

        private static List<string> Ids(IEnumerable<CardDefinition> cards) => cards.Select(c => c.Id).ToList();

        private static CombatEvent FirstHeroDamage(CombatLog log) =>
            log.Events.First(e => e.Kind == CombatEventKind.Damage && e.CasterIndex == Fight.HeroIndex);

        private static List<LineChange> Schedule() => new List<LineChange>
        {
            LineChange.Move(3, 0, 1),
            LineChange.SwapWithReserve(5, 0, 0),
        };

        // A run with a reserve of one card (Smite): the line [Strike, Guard] is full, so the card goes to the reserve.
        private static Run CreateRunWithReserve(ulong seed = 1)
        {
            var run = CreateRun(seed);
            run.AddCard(Smite);
            return run;
        }

        // --- Passive upgrades ---

        [Test]
        public void TakeUpgrade_MaxHealth_RaisesTheHeroOfEveryFight()
        {
            var run = CreateRun();
            run.TakeUpgrade(new PassiveUpgrade("TestUpgrade", PassiveUpgradeKind.MaxHealth, 5));

            var first = run.Play(RunStep.RegularFight);
            var second = run.Play(RunStep.RegularFight);

            Assert.AreEqual(HeroHealth + 5, first.Log.Combatants[Fight.HeroIndex].MaxHealth);
            Assert.AreEqual(HeroHealth + 5, first.Log.Combatants[Fight.HeroIndex].Health);
            Assert.AreEqual(HeroHealth + 5, second.Log.Combatants[Fight.HeroIndex].MaxHealth);
        }

        [Test]
        public void TakeUpgrade_StartingShield_RaisesTheShieldOfEveryFight()
        {
            var run = CreateRun();
            run.TakeUpgrade(new PassiveUpgrade("TestUpgrade", PassiveUpgradeKind.StartingShield, 7));

            Assert.AreEqual(7, run.Play(RunStep.RegularFight).Log.Combatants[Fight.HeroIndex].Shield);
            Assert.AreEqual(7, run.Play(RunStep.RegularFight).Log.Combatants[Fight.HeroIndex].Shield);
        }

        [Test]
        public void TakeUpgrade_EffectAmount_RaisesTheEffectsOfTheLine()
        {
            var run = CreateRun();
            run.TakeUpgrade(new PassiveUpgrade("TestUpgrade", PassiveUpgradeKind.EffectAmount, BonusKind.Damage, 5));

            var report = run.Play(RunStep.RegularFight);

            Assert.AreEqual(15, FirstHeroDamage(report.Log).Amount);
        }

        [Test]
        public void TakeUpgrade_TakenTwice_Stacks()
        {
            var run = CreateRun();
            run.TakeUpgrade(new PassiveUpgrade("TestUpgrade", PassiveUpgradeKind.EffectAmount, BonusKind.Damage, 5));
            run.TakeUpgrade(new PassiveUpgrade("TestUpgrade", PassiveUpgradeKind.EffectAmount, BonusKind.Damage, 5));

            Assert.AreEqual(20, FirstHeroDamage(run.Play(RunStep.RegularFight).Log).Amount);
            Assert.AreEqual(2, run.Upgrades.Upgrades.Count);
        }

        [Test]
        public void Play_WithoutUpgrades_LeavesTheHeroAsTheClassDefinesIt()
        {
            var report = CreateRun().Play(RunStep.RegularFight);

            Assert.AreEqual(HeroHealth, report.Log.Combatants[Fight.HeroIndex].MaxHealth);
            Assert.AreEqual(10, FirstHeroDamage(report.Log).Amount);
        }

        [Test]
        public void TakeUpgrade_AppliesToACardSwappedInFromTheReserve()
        {
            var run = CreateRunWithReserve();
            run.TakeUpgrade(new PassiveUpgrade("TestUpgrade", PassiveUpgradeKind.EffectAmount, BonusKind.Damage, 5));

            var session = run.BeginFight(RunStep.RegularFight);
            session.SwapWithReserve(0, 0);
            session.RunToEnd();
            var report = session.Complete();

            // Smite is 20 damage, +5 from the upgrade, and it is the first card cast after the swap.
            Assert.AreEqual(25, FirstHeroDamage(report.Log).Amount);
        }

        [Test]
        public void TakeUpgrade_NullUpgrade_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CreateRun().TakeUpgrade(null));
        }

        [Test]
        public void TakeUpgrade_AfterTheRunEnds_Throws()
        {
            var run = CreateRun(pool: new[] { Deadly });
            run.Play(RunStep.RegularFight);

            Assert.Throws<InvalidOperationException>(
                () => run.TakeUpgrade(new PassiveUpgrade("TestUpgrade", PassiveUpgradeKind.MaxHealth, 1)));
        }

        // --- Live line editing ---

        [Test]
        public void BeginFight_RegularFight_AllowsLineEdits()
        {
            var session = CreateRun().BeginFight(RunStep.RegularFight);

            Assert.IsTrue(session.LineEditsAllowed);
            Assert.IsTrue(session.IsOpen);
            Assert.AreEqual(0, session.Tick);
        }

        [Test]
        public void BeginFight_SecretRoomAndProfessor_RefuseLineEdits()
        {
            var run = CreateRun(minimumRegularFights: 0);
            run.UnlockSecretRoom("TestRoom", OtherTank);

            foreach (var step in new[] { RunStep.SecretRoom("TestRoom"), RunStep.Professor })
            {
                var session = run.BeginFight(step);

                Assert.IsFalse(session.LineEditsAllowed, step.ToString());
                Assert.Throws<InvalidOperationException>(() => session.MoveCard(0, 1), step.ToString());
                Assert.Throws<InvalidOperationException>(
                    () => session.RunToEnd(new[] { LineChange.Move(2, 0, 1) }), step.ToString());
                Assert.Throws<InvalidOperationException>(() => session.SwapWithReserve(0, 0), step.ToString());
                session.Cancel();
            }
        }

        [Test]
        public void RunStep_AllowsLineEditing_OnlyForRegularFights()
        {
            Assert.IsTrue(RunStep.RegularFight.AllowsLineEditing);
            Assert.IsFalse(RunStep.Professor.AllowsLineEditing);
            Assert.IsFalse(RunStep.SecretRoom("TestRoom").AllowsLineEditing);
        }

        [Test]
        public void LineChanges_DuringARegularFight_UpdateTheRunsLineAndReserve()
        {
            var run = CreateRunWithReserve();
            var strike = run.Line[0];
            var guard = run.Line[1];
            var smite = run.Reserve[0];

            var session = run.BeginFight(RunStep.RegularFight);
            session.RunToEnd(Schedule());
            var report = session.Complete();

            // Move(3, 0 -> 1) gives [Guard, Strike]; the swap at tick 5 puts Smite in position 0 and Guard in reserve.
            CollectionAssert.AreEqual(new[] { "TestSmite", "TestStrike" }, Ids(run.Line));
            CollectionAssert.AreEqual(new[] { "TestGuard" }, Ids(run.Reserve));
            Assert.AreSame(smite, run.Line[0]);
            Assert.AreSame(strike, run.Line[1]);
            Assert.AreSame(guard, run.Reserve[0]);
            Assert.AreEqual(2, report.Log.Events.Count(e => e.Kind == CombatEventKind.LineChanged));
        }

        [Test]
        public void LineChanges_TheFightsLineAndTheRunsLineAgree()
        {
            var run = CreateRunWithReserve();

            var session = run.BeginFight(RunStep.RegularFight);
            session.RunToEnd(Schedule());

            CollectionAssert.AreEqual(Ids(session.Fight.HeroLine), Ids(run.Line));
            CollectionAssert.AreEqual(Ids(session.Fight.HeroReserve), Ids(run.Reserve));
            session.Complete();
        }

        [Test]
        public void LineChanges_AreVisibleOnTheRunAsTheyHappen()
        {
            var run = CreateRunWithReserve();
            var session = run.BeginFight(RunStep.RegularFight);

            session.MoveCard(0, 1);

            CollectionAssert.AreEqual(new[] { "TestGuard", "TestStrike" }, Ids(run.Line));
            session.Advance();
            session.SwapWithReserve(1, 0);
            CollectionAssert.AreEqual(new[] { "TestGuard", "TestSmite" }, Ids(run.Line));
            CollectionAssert.AreEqual(new[] { "TestStrike" }, Ids(run.Reserve));
            session.Cancel();
        }

        [Test]
        public void Complete_AfterLineChanges_ReportsTheLineWhenTheFightStarted()
        {
            var run = CreateRunWithReserve();
            var strike = run.Line[0];
            var guard = run.Line[1];

            var session = run.BeginFight(RunStep.RegularFight);
            session.RunToEnd(Schedule());
            var report = session.Complete();

            Assert.AreEqual(2, report.HeroLine.Count);
            Assert.AreSame(strike, report.HeroLine[0]);
            Assert.AreSame(guard, report.HeroLine[1]);
        }

        [Test]
        public void LineChanges_SameSeedAndSameChanges_GiveTheSameRun()
        {
            var first = CreateRunWithReserve(seed: 5);
            var second = CreateRunWithReserve(seed: 5);

            var firstSession = first.BeginFight(RunStep.RegularFight);
            firstSession.RunToEnd(Schedule());
            var firstReport = firstSession.Complete();
            var secondSession = second.BeginFight(RunStep.RegularFight);
            secondSession.RunToEnd(Schedule());
            var secondReport = secondSession.Complete();

            Assert.AreEqual(firstReport.Log.ToJson(), secondReport.Log.ToJson());
            CollectionAssert.AreEqual(Ids(first.Line), Ids(second.Line));
            CollectionAssert.AreEqual(Ids(first.Reserve), Ids(second.Reserve));
            Assert.AreEqual(first.TotalXp, second.TotalXp);
        }

        [Test]
        public void LineChanges_ThroughTheSessionOrTickByTick_GiveTheSameFight()
        {
            var scheduled = CreateRunWithReserve(seed: 9);
            var scheduledSession = scheduled.BeginFight(RunStep.RegularFight);
            scheduledSession.RunToEnd(Schedule());
            var scheduledReport = scheduledSession.Complete();

            var stepped = CreateRunWithReserve(seed: 9);
            var steppedSession = stepped.BeginFight(RunStep.RegularFight);
            while (!steppedSession.IsOver)
            {
                if (steppedSession.Tick + 1 == 3)
                {
                    steppedSession.MoveCard(0, 1);
                }

                if (steppedSession.Tick + 1 == 5)
                {
                    steppedSession.SwapWithReserve(0, 0);
                }

                steppedSession.Advance();
            }

            var steppedReport = steppedSession.Complete();

            Assert.AreEqual(scheduledReport.Log.ToText(), steppedReport.Log.ToText());
        }

        // --- Play and the session ---

        [Test]
        public void Play_AndASessionSteppedWithoutChanges_GiveTheSameFightAndTheSameRun()
        {
            var played = CreateRun(seed: 3, pool: new[] { Tank, OtherTank });
            var stepped = CreateRun(seed: 3, pool: new[] { Tank, OtherTank });

            var playedReport = played.Play(RunStep.RegularFight);
            var session = stepped.BeginFight(RunStep.RegularFight);
            while (!session.IsOver)
            {
                session.Advance();
            }

            var steppedReport = session.Complete();

            Assert.AreEqual(playedReport.Log.ToText(), steppedReport.Log.ToText());
            Assert.AreEqual(playedReport.Encounter.Id, steppedReport.Encounter.Id);
            Assert.AreEqual(played.FightsPlayed, stepped.FightsPlayed);
            Assert.AreEqual(played.TotalXp, stepped.TotalXp);
            Assert.AreEqual(played.Level, stepped.Level);
            Assert.AreEqual(played.RegularFightsWon, stepped.RegularFightsWon);
            Assert.AreEqual(played.PendingLevelUps, stepped.PendingLevelUps);
        }

        [Test]
        public void Complete_Victory_GivesTheXpAndCountsTheFight()
        {
            var run = CreateRun();
            var session = run.BeginFight(RunStep.RegularFight);
            session.RunToEnd();

            var report = session.Complete();

            Assert.IsTrue(report.HeroWon);
            Assert.AreEqual(4, report.XpGained);
            Assert.AreEqual(4, run.TotalXp);
            Assert.AreEqual(1, run.FightsPlayed);
            Assert.AreEqual(1, run.RegularFightsWon);
            Assert.IsNull(run.CurrentFight);
            Assert.IsFalse(session.IsOpen);
        }

        [Test]
        public void Complete_Defeat_EndsTheRun()
        {
            var run = CreateRun(pool: new[] { Deadly });
            var session = run.BeginFight(RunStep.RegularFight);
            session.RunToEnd();

            var report = session.Complete();

            Assert.IsFalse(report.HeroWon);
            Assert.AreEqual(0, report.XpGained);
            Assert.AreEqual(RunOutcome.Defeat, run.Outcome);
        }

        [Test]
        public void Complete_ProfessorVictory_WinsTheRun()
        {
            var run = CreateRun(minimumRegularFights: 0);
            var session = run.BeginFight(RunStep.Professor);
            session.RunToEnd();
            session.Complete();

            Assert.AreEqual(RunOutcome.Victory, run.Outcome);
        }

        [Test]
        public void Complete_BeforeTheFightIsOver_Throws()
        {
            var session = CreateRun().BeginFight(RunStep.RegularFight);
            session.Advance();

            Assert.Throws<InvalidOperationException>(() => session.Complete());
            Assert.IsTrue(session.IsOpen);
        }

        [Test]
        public void ClosedSession_RefusesFurtherUse()
        {
            var session = CreateRun().BeginFight(RunStep.RegularFight);
            session.RunToEnd();
            session.Complete();

            Assert.Throws<InvalidOperationException>(() => session.Complete());
            Assert.Throws<InvalidOperationException>(() => session.Advance());
            Assert.Throws<InvalidOperationException>(() => session.MoveCard(0, 1));
            Assert.Throws<InvalidOperationException>(() => session.RunToEnd());
            Assert.DoesNotThrow(() => session.Cancel());
        }

        [Test]
        public void OpenSession_BlocksEveryOtherChangeOfTheRun()
        {
            var run = CreateRunWithReserve();
            var session = run.BeginFight(RunStep.RegularFight);

            Assert.AreSame(session, run.CurrentFight);
            Assert.Throws<InvalidOperationException>(() => run.Play(RunStep.RegularFight));
            Assert.Throws<InvalidOperationException>(() => run.BeginFight(RunStep.RegularFight));
            Assert.Throws<InvalidOperationException>(() => run.AddCard(Strike));
            Assert.Throws<InvalidOperationException>(() => run.MoveInLine(0, 1));
            Assert.Throws<InvalidOperationException>(() => run.SwapWithReserve(0, 0));
            Assert.Throws<InvalidOperationException>(() => run.MoveToReserve(0));
            Assert.Throws<InvalidOperationException>(() => run.MoveFromReserve(0));
            Assert.Throws<InvalidOperationException>(() => run.IncreaseLineCapacity(1));
            Assert.Throws<InvalidOperationException>(() => run.UnlockSecretRoom("TestRoom", OtherTank));
            Assert.Throws<InvalidOperationException>(
                () => run.TakeUpgrade(new PassiveUpgrade("TestUpgrade", PassiveUpgradeKind.MaxHealth, 1)));
            session.Cancel();
        }

        [Test]
        public void Cancel_FreesTheRun_KeepsTheLineChangesAndDoesNotCountTheFight()
        {
            var pool = new[] { Tank, OtherTank };
            var reference = CreateRun(seed: 11, pool: pool);
            var run = CreateRun(seed: 11, pool: pool);

            var session = run.BeginFight(RunStep.RegularFight);
            var drawn = session.Encounter.Id;
            session.MoveCard(0, 1);
            session.Advance();
            session.Cancel();

            Assert.IsNull(run.CurrentFight);
            Assert.AreEqual(0, run.FightsPlayed);
            Assert.AreEqual(0, run.TotalXp);
            Assert.AreEqual(RunOutcome.InProgress, run.Outcome);
            CollectionAssert.AreEqual(new[] { "TestGuard", "TestStrike" }, Ids(run.Line));
            Assert.AreEqual(reference.BeginFight(RunStep.RegularFight).Encounter.Id, drawn);
            Assert.AreEqual(drawn, run.BeginFight(RunStep.RegularFight).Encounter.Id);
        }

        [Test]
        public void RunToEnd_ChangesOutOfOrder_Throws()
        {
            var session = CreateRunWithReserve().BeginFight(RunStep.RegularFight);

            Assert.Throws<ArgumentException>(
                () => session.RunToEnd(new[] { LineChange.Move(4, 0, 1), LineChange.Move(2, 1, 0) }));
        }

        [Test]
        public void BeginFight_NullStep_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CreateRun().BeginFight(null));
        }

        [Test]
        public void BeginFight_UnavailableStep_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => CreateRun(minimumRegularFights: 3).BeginFight(RunStep.Professor));
        }

        [Test]
        public void ConsumePendingLevelUp_WhileASessionIsOpen_IsAllowed()
        {
            // The run screen may take the linked choice of a level reached earlier whenever it likes.
            var run = CreateRun();
            run.Play(RunStep.RegularFight);
            run.Play(RunStep.RegularFight);
            run.Play(RunStep.RegularFight);
            var session = run.BeginFight(RunStep.RegularFight);

            Assert.DoesNotThrow(() => run.ConsumePendingLevelUp());
            session.Cancel();
        }
    }
}
