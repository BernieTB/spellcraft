using System;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Runs;
using NUnit.Framework;

namespace Game.Core.Tests.Runs
{
    public class RunXpTests
    {
        // Placeholder ids and test values: not game content or balance numbers.
        private const int HeroHealth = 20;
        private const int TimeLimit = 50;

        // Level 1 -> 2 costs 10 XP, 2 -> 3 costs 20, 3 -> 4 costs 25, 4 -> 5 costs 30, 5 -> 6 costs 35...
        private static readonly LevelCurve Curve = new LevelCurve(new[] { 10, 20 }, 5);

        private static readonly CardDefinition Strike = new CardDefinition("TestStrike", 1, new IEffect[] { new DealDamageEffect(10) });
        private static readonly CardDefinition EnemyHit = new CardDefinition("TestEnemyHit", 1, new IEffect[] { new DealDamageEffect(3) });
        private static readonly CardDefinition EnemySmash = new CardDefinition("TestEnemySmash", 1, new IEffect[] { new DealDamageEffect(50) });

        private static EnemyDefinition Weak(string id, int xp)
        {
            // Loses 10 per hero cast: dies on the hero's second cast.
            return new EnemyDefinition(id, 15, 0, new[] { EnemyHit }, xp);
        }

        private static EncounterDefinition Encounter(string id, params EnemyDefinition[] enemies)
        {
            return new EncounterDefinition(id, enemies);
        }

        private static Run CreateRun(EncounterDefinition regular, EncounterDefinition professor = null)
        {
            var heroClass = new ClassDefinition("TestClass", HeroHealth, 0, 4, new[] { Strike }, new CardDefinition[0]);
            var biome = new BiomeDefinition("TestBiome", new[] { regular }, 0, professor ?? Encounter("TestProfessor", Weak("TestProfessor1", 0)));
            return new Run(heroClass, biome, new RunRules(TimeLimit, Curve), 1);
        }

        [Test]
        public void NewRun_StartsAtLevelOneWithoutXp()
        {
            var run = CreateRun(Encounter("TestWeak", Weak("TestEnemy1", 4)));

            Assert.AreEqual(1, run.Level);
            Assert.AreEqual(0, run.TotalXp);
            Assert.AreEqual(0, run.PendingLevelUps);
        }

        [Test]
        public void Play_Victory_GivesTheXpOfEveryEnemyOfTheEncounter()
        {
            var run = CreateRun(Encounter("TestPair", Weak("TestEnemy1", 4), Weak("TestEnemy2", 3)));

            var report = run.Play(RunStep.RegularFight);

            Assert.IsTrue(report.HeroWon);
            Assert.AreEqual(7, report.XpGained);
            Assert.AreEqual(7, run.TotalXp);
            Assert.AreEqual(0, report.LevelsGained);
            Assert.AreEqual(1, run.Level);
        }

        [Test]
        public void Play_XpReachesTheNextLevel_ReportsOneLevelUp()
        {
            var run = CreateRun(Encounter("TestWeak", Weak("TestEnemy1", 6)));

            run.Play(RunStep.RegularFight);
            var report = run.Play(RunStep.RegularFight);

            Assert.AreEqual(12, run.TotalXp);
            Assert.AreEqual(1, report.LevelsGained);
            Assert.AreEqual(2, run.Level);
            Assert.AreEqual(1, run.PendingLevelUps);
        }

        [Test]
        public void Play_EnoughXpForSeveralLevels_ReportsThemAllAtOnce()
        {
            var run = CreateRun(Encounter("TestWeak", Weak("TestEnemy1", 30)));

            var report = run.Play(RunStep.RegularFight);

            Assert.AreEqual(2, report.LevelsGained);
            Assert.AreEqual(3, run.Level);
            Assert.AreEqual(2, run.PendingLevelUps);
        }

        [Test]
        public void Play_LongFarming_FollowsTheCurve()
        {
            var run = CreateRun(Encounter("TestWeak", Weak("TestEnemy1", 10)));

            for (var i = 0; i < 10; i++)
            {
                run.Play(RunStep.RegularFight);
            }

            // 100 XP: levels cost 10, 20, 25, 30 (85 in total), the next one 35.
            Assert.AreEqual(100, run.TotalXp);
            Assert.AreEqual(5, run.Level);
            Assert.AreEqual(4, run.PendingLevelUps);
        }

        [Test]
        public void Play_Defeat_GivesNoXp()
        {
            var strong = Encounter("TestStrong", new EnemyDefinition("TestEnemy3", 100, 0, new[] { EnemySmash }, 50));
            var run = CreateRun(strong);

            var report = run.Play(RunStep.RegularFight);

            Assert.IsFalse(report.HeroWon);
            Assert.AreEqual(0, report.XpGained);
            Assert.AreEqual(0, report.LevelsGained);
            Assert.AreEqual(0, run.TotalXp);
            Assert.AreEqual(1, run.Level);
        }

        [Test]
        public void Play_SecretRoomFoughtAgain_GivesXpEachTime()
        {
            var run = CreateRun(Encounter("TestWeak", Weak("TestEnemy1", 1)));
            run.UnlockSecretRoom("TestRoom", Encounter("TestMiniBoss", Weak("TestMiniBoss1", 7)));

            run.Play(RunStep.SecretRoom("TestRoom"));
            run.Play(RunStep.SecretRoom("TestRoom"));

            Assert.AreEqual(14, run.TotalXp);
        }

        [Test]
        public void Play_ProfessorVictory_GivesXp()
        {
            var run = CreateRun(Encounter("TestWeak", Weak("TestEnemy1", 1)), Encounter("TestProfessor", Weak("TestProfessor1", 12)));

            var report = run.Play(RunStep.Professor);

            Assert.AreEqual(RunOutcome.Victory, run.Outcome);
            Assert.AreEqual(12, report.XpGained);
            Assert.AreEqual(2, run.Level);
        }

        [Test]
        public void ConsumePendingLevelUp_RemovesOne()
        {
            var run = CreateRun(Encounter("TestWeak", Weak("TestEnemy1", 30)));
            run.Play(RunStep.RegularFight);

            run.ConsumePendingLevelUp();

            Assert.AreEqual(1, run.PendingLevelUps);
            Assert.AreEqual(3, run.Level);
        }

        [Test]
        public void ConsumePendingLevelUp_NonePending_Throws()
        {
            var run = CreateRun(Encounter("TestWeak", Weak("TestEnemy1", 1)));

            Assert.Throws<InvalidOperationException>(() => run.ConsumePendingLevelUp());
        }
    }
}
