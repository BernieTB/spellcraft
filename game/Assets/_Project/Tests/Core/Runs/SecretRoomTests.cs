using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Meta;
using Game.Core.Runs;
using NUnit.Framework;

namespace Game.Core.Tests.Runs
{
    /// <summary>Objectives, secret room unlocking and first-victory rewards (#71, ADR 0010). Ids and numbers are arbitrary test data.</summary>
    public class SecretRoomTests
    {
        private const int HeroHealth = 20;
        private const int TimeLimit = 50;
        private const string RoomA = "TestRoomA";
        private const string RoomB = "TestRoomB";

        private static readonly LevelCurve Curve = new LevelCurve(new[] { 10, 20 }, 5);

        private static readonly CardDefinition Strike = new CardDefinition("TestStrike", 1, new IEffect[] { new DealDamageEffect(10) });
        private static readonly CardDefinition Unique = new CardDefinition("TestUniqueA", 1, new IEffect[] { new DealDamageEffect(1) });
        private static readonly CardDefinition OtherUnique = new CardDefinition("TestUniqueB", 1, new IEffect[] { new DealDamageEffect(2) });
        private static readonly CardDefinition EnemyHit = new CardDefinition("TestEnemyHit", 1, new IEffect[] { new DealDamageEffect(3) });
        private static readonly CardDefinition EnemyJab = new CardDefinition("TestEnemyJab", 1, new IEffect[] { new DealDamageEffect(1) });
        private static readonly CardDefinition EnemyPoke = new CardDefinition("TestEnemyPoke", 1, new IEffect[] { new DealDamageEffect(2) });
        private static readonly CardDefinition EnemySmash = new CardDefinition("TestEnemySmash", 1, new IEffect[] { new DealDamageEffect(50) });

        private static readonly EnemyDefinition Goblin = new EnemyDefinition("TestGoblin", 15, 0, new[] { EnemyHit }, 1);
        private static readonly EnemyDefinition Imp = new EnemyDefinition("TestImp", 15, 0, new[] { EnemyHit }, 1);
        private static readonly EnemyDefinition Professor =
            new EnemyDefinition("TestProfessor1", 15, 4, new[] { EnemyHit, EnemyJab, EnemyPoke }, 30);

        private static readonly EncounterDefinition GoblinFight = new EncounterDefinition("TestGoblinFight", new[] { Goblin });
        private static readonly EncounterDefinition ImpFight = new EncounterDefinition("TestImpFight", new[] { Imp });
        private static readonly EncounterDefinition TwoGoblins = new EncounterDefinition("TestTwoGoblins", new[] { Goblin, Goblin });
        private static readonly EncounterDefinition ProfessorFight = new EncounterDefinition("TestProfessorFight", new[] { Professor });

        // Mini-bosses: the first can be beaten, the second kills the hero at once.
        private static readonly EnemyDefinition MiniBossAEnemy = new EnemyDefinition("TestMiniBossA", 15, 0, new[] { EnemyHit }, 7);
        private static readonly EncounterDefinition MiniBossA = new EncounterDefinition("TestMiniBossAFight", new[] { MiniBossAEnemy });
        private static readonly EncounterDefinition MiniBossB = new EncounterDefinition(
            "TestMiniBossBFight", new[] { new EnemyDefinition("TestMiniBossB", 100, 0, new[] { EnemySmash }, 9) });

        private static SecretRoomDefinition RoomAData(
            int goblins = 3,
            int bonusSlots = 1,
            ProfessorRevelation revelation = null,
            EncounterDefinition miniBoss = null)
        {
            return new SecretRoomDefinition(
                RoomA,
                new ObjectiveDefinition(Goblin.Id, goblins),
                miniBoss ?? MiniBossA,
                bonusSlots,
                Unique,
                revelation ?? new ProfessorRevelation(true, false, new[] { 1 }));
        }

        private static SecretRoomDefinition RoomBData(int imps = 1)
        {
            return new SecretRoomDefinition(
                RoomB, new ObjectiveDefinition(Imp.Id, imps), MiniBossB, 1, OtherUnique, new ProfessorRevelation(false, true, new int[0]));
        }

        private static Run CreateRun(
            SecretRoomDefinition[] rooms = null,
            EncounterDefinition[] pool = null,
            int capacity = 4,
            ulong seed = 1)
        {
            var heroClass = new ClassDefinition("TestClass", HeroHealth, 0, capacity, new[] { Strike }, new CardDefinition[0]);
            var biome = new BiomeDefinition(
                "TestBiome",
                pool ?? new[] { GoblinFight },
                2,
                ProfessorFight,
                Professor,
                rooms ?? new[] { RoomAData() });
            return new Run(heroClass, biome, new RunRules(TimeLimit, Curve), seed);
        }

        private static void PlayRegularFights(Run run, int count)
        {
            for (var i = 0; i < count; i++)
            {
                run.Play(RunStep.RegularFight);
            }
        }

        private static SecretRoomStatus Status(Run run, string roomId)
        {
            return run.SecretRooms.Single(s => s.Definition.Id == roomId);
        }

        // --- Objectives ---

        [Test]
        public void NewRun_SecretRoomsAreLockedWithNoProgress()
        {
            var run = CreateRun();

            var status = Status(run, RoomA);
            Assert.AreEqual(0, status.ObjectiveProgress);
            Assert.IsFalse(status.IsUnlocked);
            Assert.IsFalse(status.IsCleared);
            CollectionAssert.AreEqual(new[] { RunStep.RegularFight }, run.AvailableSteps);
        }

        [Test]
        public void Play_RegularFightWon_CountsTheObjectiveEnemy()
        {
            var run = CreateRun();

            var report = run.Play(RunStep.RegularFight);

            Assert.AreEqual(1, Status(run, RoomA).ObjectiveProgress);
            Assert.IsEmpty(report.UnlockedRoomIds);
            Assert.IsFalse(Status(run, RoomA).IsUnlocked);
        }

        [Test]
        public void Play_ObjectiveCompleted_UnlocksTheRoomAndReportsIt()
        {
            var run = CreateRun();

            PlayRegularFights(run, 2);
            var report = run.Play(RunStep.RegularFight);

            CollectionAssert.AreEqual(new[] { RoomA }, report.UnlockedRoomIds);
            Assert.IsTrue(Status(run, RoomA).IsUnlocked);
            Assert.AreEqual(3, Status(run, RoomA).ObjectiveProgress);
            CollectionAssert.Contains(run.AvailableSteps, RunStep.SecretRoom(RoomA));
        }

        [Test]
        public void Play_AnUnlockedRoomIsReportedOnlyByTheFightThatOpenedIt()
        {
            var run = CreateRun(new[] { RoomAData(goblins: 1) });

            var first = run.Play(RunStep.RegularFight);
            var second = run.Play(RunStep.RegularFight);

            CollectionAssert.AreEqual(new[] { RoomA }, first.UnlockedRoomIds);
            Assert.IsEmpty(second.UnlockedRoomIds);
            Assert.AreEqual(1, run.AvailableSteps.Count(s => s.Kind == RunStepKind.SecretRoom));
        }

        [Test]
        public void Play_EncounterWithSeveralObjectiveEnemies_CountsEachOfThem()
        {
            var run = CreateRun(new[] { RoomAData(goblins: 4) }, new[] { TwoGoblins });

            run.Play(RunStep.RegularFight);
            Assert.AreEqual(2, Status(run, RoomA).ObjectiveProgress);
            var report = run.Play(RunStep.RegularFight);

            CollectionAssert.AreEqual(new[] { RoomA }, report.UnlockedRoomIds);
        }

        [Test]
        public void Play_ProgressNeverExceedsTheObjectiveCount()
        {
            var run = CreateRun(new[] { RoomAData(goblins: 3) }, new[] { TwoGoblins });

            PlayRegularFights(run, 3);

            Assert.AreEqual(3, Status(run, RoomA).ObjectiveProgress);
        }

        [Test]
        public void Play_OtherEnemiesOfTheEncounter_DoNotCount()
        {
            var mixed = new EncounterDefinition("TestMixed", new[] { Imp, Goblin });
            var run = CreateRun(new[] { RoomAData(goblins: 3) }, new[] { mixed });

            PlayRegularFights(run, 2);

            Assert.AreEqual(2, Status(run, RoomA).ObjectiveProgress);
            Assert.IsFalse(Status(run, RoomA).IsUnlocked);
        }

        [Test]
        public void Play_SeveralObjectives_AreCountedTogetherAndOpenInListingOrder()
        {
            var both = new EncounterDefinition("TestBoth", new[] { Imp, Goblin });
            var run = CreateRun(new[] { RoomAData(goblins: 1), RoomBData(imps: 1) }, new[] { both });

            var report = run.Play(RunStep.RegularFight);

            CollectionAssert.AreEqual(new[] { RoomA, RoomB }, report.UnlockedRoomIds);
            CollectionAssert.AreEqual(
                new[] { RunStep.RegularFight, RunStep.SecretRoom(RoomA), RunStep.SecretRoom(RoomB) },
                run.AvailableSteps);
        }

        [Test]
        public void Play_OnlyRegularFightsCount()
        {
            // Room B's objective enemy also stands in room A's mini-boss fight: beating it must not count.
            var miniBoss = new EncounterDefinition("TestMiniBossWithImp", new[] { MiniBossAEnemy, Imp });
            var run = CreateRun(new[] { RoomAData(goblins: 1, miniBoss: miniBoss), RoomBData(imps: 2) }, new[] { GoblinFight, ImpFight });
            run.UnlockSecretRoom(RoomA, miniBoss);

            var report = run.Play(RunStep.SecretRoom(RoomA));

            Assert.IsTrue(report.HeroWon);
            Assert.AreEqual(0, Status(run, RoomB).ObjectiveProgress);
        }

        [Test]
        public void Play_LostRegularFight_DoesNotCount()
        {
            var deadly = new EncounterDefinition("TestDeadlyGoblin", new[] { new EnemyDefinition(Goblin.Id, 100, 0, new[] { EnemySmash }, 1) });
            var run = CreateRun(new[] { RoomAData() }, new[] { deadly });

            var report = run.Play(RunStep.RegularFight);

            Assert.IsFalse(report.HeroWon);
            Assert.AreEqual(0, Status(run, RoomA).ObjectiveProgress);
        }

        [Test]
        public void SecretRooms_ABiomeWithoutRooms_IsEmpty()
        {
            var run = CreateRun(new SecretRoomDefinition[0]);

            Assert.IsEmpty(run.SecretRooms);
        }

        // --- Unlocking by hand ---

        [Test]
        public void UnlockSecretRoom_ABiomeRoom_OpensItAndKeepsItsProgress()
        {
            var run = CreateRun();
            run.Play(RunStep.RegularFight);

            run.UnlockSecretRoom(RoomA, MiniBossA);

            Assert.IsTrue(Status(run, RoomA).IsUnlocked);
            Assert.AreEqual(1, Status(run, RoomA).ObjectiveProgress);
            CollectionAssert.Contains(run.AvailableSteps, RunStep.SecretRoom(RoomA));
        }

        [Test]
        public void UnlockSecretRoom_ABiomeRoomWithAnotherEncounter_Throws()
        {
            var run = CreateRun();

            Assert.Throws<InvalidOperationException>(() => run.UnlockSecretRoom(RoomA, MiniBossB));
        }

        [Test]
        public void UnlockSecretRoom_AnEquivalentEncounterInstance_IsAcceptedAndUsesTheBiomesOwn()
        {
            var run = CreateRun();
            var equivalent = new EncounterDefinition(MiniBossA.Id, new[] { MiniBossAEnemy });

            run.UnlockSecretRoom(RoomA, equivalent);
            run.UnlockSecretRoom(RoomA, MiniBossA);
            run.UnlockSecretRoom(RoomA, equivalent);

            Assert.IsTrue(Status(run, RoomA).IsUnlocked);
            Assert.AreEqual(1, run.AvailableSteps.Count(s => s.Kind == RunStepKind.SecretRoom));
            Assert.AreSame(MiniBossA, run.Play(RunStep.SecretRoom(RoomA)).Encounter);
        }

        [Test]
        public void UnlockSecretRoom_TheSameInstanceTwice_DoesNothingTheSecondTime()
        {
            var run = CreateRun();

            run.UnlockSecretRoom(RoomA, MiniBossA);
            run.UnlockSecretRoom(RoomA, MiniBossA);

            Assert.AreEqual(1, run.AvailableSteps.Count(s => s.Kind == RunStepKind.SecretRoom));
        }

        [Test]
        public void UnlockSecretRoom_ARoomOutsideTheBiome_UsesTheGivenEncounterAndComparesByIdAfterwards()
        {
            var run = CreateRun();
            var other = new EncounterDefinition("TestOtherRoomFight", new[] { MiniBossAEnemy });

            run.UnlockSecretRoom("TestOutsideRoom", other);
            run.UnlockSecretRoom("TestOutsideRoom", new EncounterDefinition(other.Id, new[] { MiniBossAEnemy }));

            Assert.Throws<InvalidOperationException>(() => run.UnlockSecretRoom("TestOutsideRoom", MiniBossB));
            Assert.AreEqual(1, run.AvailableSteps.Count(s => s.Kind == RunStepKind.SecretRoom));
        }

        [Test]
        public void Play_ARoomOpenedByHandIsNotOpenedAgainByItsObjective()
        {
            var run = CreateRun(new[] { RoomAData(goblins: 1) });
            run.UnlockSecretRoom(RoomA, MiniBossA);

            var report = run.Play(RunStep.RegularFight);

            Assert.IsEmpty(report.UnlockedRoomIds);
            Assert.AreEqual(1, run.AvailableSteps.Count(s => s.Kind == RunStepKind.SecretRoom));
        }

        // --- First victory ---

        private static Run RunWithRoomA(int capacity = 4, int bonusSlots = 1, ProfessorRevelation revelation = null)
        {
            var run = CreateRun(new[] { RoomAData(1, bonusSlots, revelation) }, capacity: capacity);
            run.Play(RunStep.RegularFight);
            return run;
        }

        [Test]
        public void Play_FirstVictoryOverTheMiniBoss_GivesSlotAndUniqueCard()
        {
            var run = RunWithRoomA();

            var report = run.Play(RunStep.SecretRoom(RoomA));

            Assert.IsTrue(report.HeroWon);
            Assert.AreEqual(5, run.LineCapacity);
            Assert.AreEqual(2, run.Line.Count);
            Assert.AreSame(Unique, run.Line[1].Definition);
            Assert.IsTrue(Status(run, RoomA).IsCleared);
            Assert.AreEqual(RoomA, report.SecretRoomRewards.RoomId);
            Assert.AreEqual(1, report.SecretRoomRewards.LineSlotsGained);
            Assert.AreSame(run.Line[1], report.SecretRoomRewards.Card);
        }

        [Test]
        public void Play_FirstVictory_TheUniqueCardTakesTheNewSlotSoTheReserveStaysEmpty()
        {
            // The line is full before the fight: the slot is added first, so the card joins the line.
            var run = RunWithRoomA(capacity: 1);

            run.Play(RunStep.SecretRoom(RoomA));

            Assert.AreEqual(2, run.LineCapacity);
            Assert.AreSame(Unique, run.Line[1].Definition);
            Assert.IsEmpty(run.Reserve);
        }

        [Test]
        public void Play_FirstVictoryWithNoBonusSlot_SendsTheUniqueCardToTheReserveWhenTheLineIsFull()
        {
            var run = RunWithRoomA(capacity: 1, bonusSlots: 0);

            var report = run.Play(RunStep.SecretRoom(RoomA));

            Assert.AreEqual(1, run.LineCapacity);
            Assert.AreEqual(1, run.Line.Count);
            Assert.AreEqual(1, run.Reserve.Count);
            Assert.AreSame(Unique, run.Reserve[0].Definition);
            Assert.AreSame(run.Reserve[0], report.SecretRoomRewards.Card);
            Assert.AreEqual(0, report.SecretRoomRewards.LineSlotsGained);
        }

        [Test]
        public void Play_FirstVictory_AlsoGivesTheMiniBossXp()
        {
            var run = RunWithRoomA();
            var xpBefore = run.TotalXp;

            var report = run.Play(RunStep.SecretRoom(RoomA));

            Assert.AreEqual(MiniBossAEnemy.XpReward, report.XpGained);
            Assert.AreEqual(xpBefore + MiniBossAEnemy.XpReward, run.TotalXp);
        }

        [Test]
        public void Play_RepeatVictory_GivesXpOnly()
        {
            var run = RunWithRoomA();
            run.Play(RunStep.SecretRoom(RoomA));

            var report = run.Play(RunStep.SecretRoom(RoomA));

            Assert.IsTrue(report.HeroWon);
            Assert.IsNull(report.SecretRoomRewards);
            Assert.AreEqual(MiniBossAEnemy.XpReward, report.XpGained);
            Assert.AreEqual(5, run.LineCapacity);
            Assert.AreEqual(2, run.Line.Count);
            Assert.IsEmpty(run.Reserve);
        }

        [Test]
        public void Play_DefeatInASecretRoom_EndsTheRunWithNoRewards()
        {
            var run = CreateRun(new[] { RoomBData(imps: 1) }, new[] { ImpFight });
            run.Play(RunStep.RegularFight);

            var report = run.Play(RunStep.SecretRoom(RoomB));

            Assert.IsFalse(report.HeroWon);
            Assert.IsNull(report.SecretRoomRewards);
            Assert.AreEqual(RunOutcome.Defeat, run.Outcome);
            Assert.AreEqual(4, run.LineCapacity);
            Assert.IsFalse(Status(run, RoomB).IsCleared);
        }

        [Test]
        public void Play_RegularFightsAndTheProfessor_NeverHaveSecretRoomRewards()
        {
            var run = RunWithRoomA();

            var regular = run.Play(RunStep.RegularFight);
            var professor = run.Play(RunStep.Professor);

            Assert.IsNull(regular.SecretRoomRewards);
            Assert.IsNull(professor.SecretRoomRewards);
            Assert.AreEqual(RunOutcome.Victory, run.Outcome);
        }

        [Test]
        public void BeginFight_CompletedSession_GivesTheSameRewardsAsPlay()
        {
            var run = RunWithRoomA();
            var session = run.BeginFight(RunStep.SecretRoom(RoomA));
            session.RunToEnd();

            var report = session.Complete();

            Assert.IsNotNull(report.SecretRoomRewards);
            Assert.AreEqual(5, run.LineCapacity);
            Assert.AreSame(Unique, run.Line[1].Definition);
        }

        [Test]
        public void BeginFight_CancelledSession_GivesNoRewardAndTheRoomIsNotCleared()
        {
            var run = RunWithRoomA();
            var session = run.BeginFight(RunStep.SecretRoom(RoomA));
            session.RunToEnd();

            session.Cancel();

            Assert.AreEqual(4, run.LineCapacity);
            Assert.IsFalse(Status(run, RoomA).IsCleared);
            Assert.IsNotNull(run.Play(RunStep.SecretRoom(RoomA)).SecretRoomRewards);
        }

        // --- Revelation ---

        [Test]
        public void RevealTo_FirstVictory_TeachesWhatTheRoomRevealsAboutTheProfessor()
        {
            var run = RunWithRoomA(revelation: new ProfessorRevelation(true, false, new[] { 1 }));
            var report = run.Play(RunStep.SecretRoom(RoomA));
            var bestiary = new Bestiary();

            var learned = report.SecretRoomRewards.RevealTo(bestiary);

            Assert.IsTrue(learned);
            var entry = bestiary.Find(Professor.Id);
            Assert.IsTrue(entry.HealthKnown);
            Assert.IsFalse(entry.ShieldKnown);
            CollectionAssert.AreEqual(new[] { new RevealedCard(1, EnemyJab.Id) }, entry.Cards);
        }

        [Test]
        public void RevealTo_CalledAgainOrKnownAlready_LearnsNothingNew()
        {
            var run = RunWithRoomA();
            var report = run.Play(RunStep.SecretRoom(RoomA));
            var bestiary = new Bestiary();
            report.SecretRoomRewards.RevealTo(bestiary);

            Assert.IsFalse(report.SecretRoomRewards.RevealTo(bestiary));
        }

        [Test]
        public void RevealTo_NullBestiary_Throws()
        {
            var run = RunWithRoomA();
            var report = run.Play(RunStep.SecretRoom(RoomA));

            Assert.Throws<ArgumentNullException>(() => report.SecretRoomRewards.RevealTo(null));
        }

        [Test]
        public void Rewards_ConcernTheBiomesProfessor()
        {
            var run = RunWithRoomA();

            var rewards = run.Play(RunStep.SecretRoom(RoomA)).SecretRoomRewards;

            Assert.AreSame(Professor, rewards.Professor);
            Assert.AreSame(run.Biome.Professor, rewards.Professor);
            Assert.IsTrue(rewards.Revelation.RevealsHealth);
        }

        // --- More rooms, cancellation, the unique card, atomicity ---

        [Test]
        public void Play_TwoThresholdsReachedByTheSameFight_UnlockBothRoomsInListingOrder()
        {
            var roomC = new SecretRoomDefinition(
                "TestRoomC", new ObjectiveDefinition(Goblin.Id, 2), MiniBossB, 1, OtherUnique, new ProfessorRevelation(false, false, new int[0]));
            var run = CreateRun(new[] { RoomAData(goblins: 1), roomC }, new[] { TwoGoblins });

            var report = run.Play(RunStep.RegularFight);

            CollectionAssert.AreEqual(new[] { RoomA, "TestRoomC" }, report.UnlockedRoomIds);
            Assert.IsTrue(Status(run, RoomA).IsUnlocked);
            Assert.IsTrue(Status(run, "TestRoomC").IsUnlocked);
        }

        [Test]
        public void BeginFight_CancelledRegularFight_DoesNotCountTheObjectiveThatWouldHaveBeenReached()
        {
            var run = CreateRun(new[] { RoomAData(goblins: 1) });
            var session = run.BeginFight(RunStep.RegularFight);
            session.RunToEnd();

            session.Cancel();

            Assert.AreEqual(0, Status(run, RoomA).ObjectiveProgress);
            Assert.IsFalse(Status(run, RoomA).IsUnlocked);
            CollectionAssert.AreEqual(new[] { RunStep.RegularFight }, run.AvailableSteps);
            CollectionAssert.AreEqual(new[] { RoomA }, run.Play(RunStep.RegularFight).UnlockedRoomIds);
        }

        [Test]
        public void Play_TheUniqueCardIsANewInstanceWithNoCastsThatCountsLikeAnyOther()
        {
            var run = RunWithRoomA();
            var before = run.Line.Select(c => c.Id).Concat(run.Reserve.Select(c => c.Id)).ToList();

            var card = run.Play(RunStep.SecretRoom(RoomA)).SecretRoomRewards.Card;

            CollectionAssert.DoesNotContain(before, card.Id);
            Assert.AreEqual(0, card.Casts);
            run.Play(RunStep.RegularFight);
            Assert.Greater(card.Casts, 0);
            Assert.AreSame(card, run.Line[1]);
        }

        [Test]
        public void Play_TwoRoomsClearedWithTheSameUniqueCard_GiveTwoDistinctInstances()
        {
            var twin = new SecretRoomDefinition(
                "TestRoomTwin", new ObjectiveDefinition(Goblin.Id, 1), MiniBossA, 1, Unique, new ProfessorRevelation(false, false, new int[0]));
            var run = CreateRun(new[] { RoomAData(1), twin });
            run.Play(RunStep.RegularFight);

            var first = run.Play(RunStep.SecretRoom(RoomA)).SecretRoomRewards.Card;
            var second = run.Play(RunStep.SecretRoom("TestRoomTwin")).SecretRoomRewards.Card;

            Assert.AreNotSame(first, second);
            Assert.AreNotEqual(first.Id, second.Id);
            Assert.AreEqual(6, run.LineCapacity);
        }

        [Test]
        public void Play_RewardsThatFail_LeaveTheRunUnchanged()
        {
            // A line that cannot take the room's slots: the fight is refused as a whole, and nothing was counted.
            var hugeBonus = new SecretRoomDefinition(
                RoomA,
                new ObjectiveDefinition(Goblin.Id, 1),
                MiniBossA,
                int.MaxValue,
                Unique,
                new ProfessorRevelation(true, false, new[] { 1 }));
            var run = CreateRun(new[] { hugeBonus });
            run.Play(RunStep.RegularFight);
            var fights = run.FightsPlayed;
            var xp = run.TotalXp;
            var level = run.Level;
            var pending = run.PendingLevelUps;
            var lineCount = run.Line.Count;

            Assert.Throws<OverflowException>(() => run.Play(RunStep.SecretRoom(RoomA)));

            Assert.AreEqual(fights, run.FightsPlayed);
            Assert.AreEqual(xp, run.TotalXp);
            Assert.AreEqual(level, run.Level);
            Assert.AreEqual(pending, run.PendingLevelUps);
            Assert.AreEqual(4, run.LineCapacity);
            Assert.AreEqual(lineCount, run.Line.Count);
            Assert.IsEmpty(run.Reserve);
            Assert.IsFalse(Status(run, RoomA).IsCleared);
            Assert.AreEqual(RunOutcome.InProgress, run.Outcome);
            Assert.IsNull(run.CurrentFight);
            Assert.IsTrue(run.IsInProgress);
        }

        // --- Determinism ---

        [Test]
        public void Run_SameSeedAndSameChoices_GivesTheSameRun()
        {
            string Play(ulong seed)
            {
                var run = CreateRun(new[] { RoomAData(2), RoomBData(1) }, new[] { GoblinFight, ImpFight, TwoGoblins }, seed: seed);
                var logs = new List<string>();
                for (var i = 0; i < 4; i++)
                {
                    logs.Add(run.Play(RunStep.RegularFight).Log.ToJson());
                }

                foreach (var step in run.AvailableSteps.Where(s => s.Kind == RunStepKind.SecretRoom).ToList())
                {
                    if (run.IsInProgress)
                    {
                        logs.Add(run.Play(step).Log.ToJson());
                    }
                }

                var statuses = string.Join(
                    ";", run.SecretRooms.Select(s => $"{s.Definition.Id}:{s.ObjectiveProgress}:{s.IsUnlocked}:{s.IsCleared}"));
                return string.Join("|", logs) + "#" + statuses + "#" + run.LineCapacity + "#" + run.Outcome;
            }

            Assert.AreEqual(Play(7), Play(7));
        }

        // --- Biome and room definitions ---

        [Test]
        public void BiomeDefinition_Professor_IsTheEnemyTheDataNames()
        {
            var sidekick = new EnemyDefinition("TestSidekick", 5, 0, new[] { EnemyHit }, 1);
            var fight = new EncounterDefinition("TestProfessorAndSidekick", new[] { sidekick, Professor });

            var biome = new BiomeDefinition("TestBiome", new[] { GoblinFight }, 0, fight, Professor);

            Assert.AreSame(Professor, biome.Professor);
            Assert.IsEmpty(biome.SecretRooms);
        }

        [Test]
        public void BiomeDefinition_ProfessorGivenAsAnEquivalentInstance_IsMatchedById()
        {
            var copy = new EnemyDefinition(Professor.Id, 15, 4, new[] { EnemyHit, EnemyJab, EnemyPoke }, 30);

            var biome = new BiomeDefinition("TestBiome", new[] { GoblinFight }, 0, ProfessorFight, copy);

            Assert.AreSame(Professor, biome.Professor);
        }

        [Test]
        public void BiomeDefinition_ProfessorNotInTheProfessorEncounter_ThrowsNamingTheBiome()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new BiomeDefinition("TestBiome", new[] { GoblinFight }, 0, ProfessorFight, Goblin));

            StringAssert.Contains("TestBiome", exception.Message);
            StringAssert.Contains(Goblin.Id, exception.Message);
        }

        [Test]
        public void BiomeDefinition_NullProfessor_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BiomeDefinition("TestBiome", new[] { GoblinFight }, 0, ProfessorFight, null));
        }

        [Test]
        public void BiomeDefinition_SecretRoomErrors_NameTheBiome()
        {
            var duplicate = Assert.Throws<ArgumentException>(
                () => new BiomeDefinition("TestBiomeX", new[] { GoblinFight }, 0, ProfessorFight, Professor, new[] { RoomAData(), RoomAData() }));
            var missing = Assert.Throws<ArgumentException>(
                () => new BiomeDefinition("TestBiomeX", new[] { ImpFight }, 0, ProfessorFight, Professor, new[] { RoomAData() }));
            var beyond = Assert.Throws<ArgumentOutOfRangeException>(
                () => new BiomeDefinition(
                    "TestBiomeX",
                    new[] { GoblinFight },
                    0,
                    ProfessorFight,
                    Professor,
                    new[] { RoomAData(revelation: new ProfessorRevelation(false, false, new[] { 3 })) }));

            StringAssert.Contains("TestBiomeX", duplicate.Message);
            StringAssert.Contains("TestBiomeX", missing.Message);
            StringAssert.Contains("TestBiomeX", beyond.Message);
        }

        [Test]
        public void BiomeDefinition_SecretRooms_KeepTheirOrder()
        {
            var biome = new BiomeDefinition(
                "TestBiome", new[] { GoblinFight, ImpFight }, 0, ProfessorFight, Professor, new[] { RoomBData(), RoomAData() });

            CollectionAssert.AreEqual(new[] { RoomB, RoomA }, biome.SecretRooms.Select(r => r.Id));
        }

        [Test]
        public void BiomeDefinition_NullSecretRoom_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BiomeDefinition("TestBiome", new[] { GoblinFight }, 0, ProfessorFight, Professor, new SecretRoomDefinition[] { null }));
        }

        [Test]
        public void BiomeDefinition_TwoRoomsWithTheSameId_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new BiomeDefinition("TestBiome", new[] { GoblinFight }, 0, ProfessorFight, Professor, new[] { RoomAData(), RoomAData() }));
        }

        [Test]
        public void BiomeDefinition_ObjectiveEnemyInNoRegularEncounter_Throws()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new BiomeDefinition("TestBiome", new[] { ImpFight }, 0, ProfessorFight, Professor, new[] { RoomAData() }));

            StringAssert.Contains(RoomA, exception.Message);
            StringAssert.Contains(Goblin.Id, exception.Message);
        }

        [Test]
        public void BiomeDefinition_RevelationBeyondTheProfessorsLine_Throws()
        {
            var room = RoomAData(revelation: new ProfessorRevelation(false, false, new[] { 3 }));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BiomeDefinition("TestBiome", new[] { GoblinFight }, 0, ProfessorFight, Professor, new[] { room }));
        }

        [Test]
        public void SecretRoomDefinition_InvalidArguments_Throw()
        {
            var objective = new ObjectiveDefinition(Goblin.Id, 1);
            var revelation = new ProfessorRevelation(false, false, new int[0]);

            Assert.Throws<ArgumentException>(() => new SecretRoomDefinition(" ", objective, MiniBossA, 1, Unique, revelation));
            Assert.Throws<ArgumentNullException>(() => new SecretRoomDefinition(RoomA, null, MiniBossA, 1, Unique, revelation));
            Assert.Throws<ArgumentNullException>(() => new SecretRoomDefinition(RoomA, objective, null, 1, Unique, revelation));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SecretRoomDefinition(RoomA, objective, MiniBossA, -1, Unique, revelation));
            Assert.Throws<ArgumentNullException>(() => new SecretRoomDefinition(RoomA, objective, MiniBossA, 1, null, revelation));
            Assert.Throws<ArgumentNullException>(() => new SecretRoomDefinition(RoomA, objective, MiniBossA, 1, Unique, null));
        }

        [Test]
        public void ObjectiveDefinition_InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentException>(() => new ObjectiveDefinition("", 1));
            Assert.Throws<ArgumentException>(() => new ObjectiveDefinition(null, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObjectiveDefinition(Goblin.Id, 0));
        }

        [Test]
        public void RunFightReport_NullUnlockedRoomId_Throws()
        {
            var run = CreateRun();
            var report = run.Play(RunStep.RegularFight);

            Assert.Throws<ArgumentNullException>(
                () => new RunFightReport(report.Step, report.Encounter, report.HeroLine, report.Log, 0, 0, new string[] { null }));
        }
    }
}
