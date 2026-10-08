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
    /// <summary>Preparation phase before mini-boss and professor fights (#82, ADR 0012 and 0014). Ids and numbers are arbitrary test data.</summary>
    public class BossPreparationTests
    {
        private const string Room = "TestRoom";

        private static readonly LevelCurve Curve = new LevelCurve(new[] { 10, 20 }, 5);
        private static readonly CardDefinition Strike = new CardDefinition("TestStrike", 1, new IEffect[] { new DealDamageEffect(10) });
        private static readonly CardDefinition Guard = new CardDefinition("TestGuard", 1, new IEffect[] { new GainShieldEffect(2) });
        private static readonly CardDefinition Mend = new CardDefinition("TestMend", 1, new IEffect[] { new HealEffect(1) });
        private static readonly CardDefinition Unique = new CardDefinition("TestUnique", 1, new IEffect[] { new DealDamageEffect(1) });
        private static readonly CardDefinition EnemyHit = new CardDefinition("TestEnemyHit", 1, new IEffect[] { new DealDamageEffect(3) });
        private static readonly CardDefinition EnemyJab = new CardDefinition("TestEnemyJab", 1, new IEffect[] { new DealDamageEffect(1) });
        private static readonly CardDefinition EnemyPoke = new CardDefinition("TestEnemyPoke", 1, new IEffect[] { new DealDamageEffect(2) });

        private static readonly EnemyDefinition Goblin = new EnemyDefinition("TestGoblin", 15, 0, new[] { EnemyHit }, 1);
        private static readonly EnemyDefinition Professor =
            new EnemyDefinition("TestProfessor1", 15, 4, new[] { EnemyHit, EnemyJab, EnemyPoke }, 30);
        private static readonly EnemyDefinition MiniBossEnemy = new EnemyDefinition("TestMiniBoss", 15, 0, new[] { EnemyHit }, 7);

        private static readonly EncounterDefinition GoblinFight = new EncounterDefinition("TestGoblinFight", new[] { Goblin });
        private static readonly EncounterDefinition ProfessorFight = new EncounterDefinition("TestProfessorFight", new[] { Professor });
        private static readonly EncounterDefinition MiniBoss = new EncounterDefinition("TestMiniBossFight", new[] { MiniBossEnemy });

        // A run ready for both kinds of preparation: the room is unlocked and the professor is available. The line
        // starts with Strike; the cards Guard and Mend are added after (line with a free slot, else the reserve).
        private static Run CreateReadyRun(int capacity = 3, int heroHealth = 100)
        {
            var heroClass = new ClassDefinition("TestClass", heroHealth, 0, capacity, new[] { Strike }, new CardDefinition[0]);
            var room = new SecretRoomDefinition(
                Room, new ObjectiveDefinition(Goblin.Id, 1), MiniBoss, 1, Unique, new ProfessorRevelation(false, false, new int[0]));
            var biome = new BiomeDefinition("TestBiome", new[] { GoblinFight }, 2, ProfessorFight, Professor, new[] { room });
            var run = new Run(heroClass, biome, new RunRules(50, Curve), 1);
            run.Play(RunStep.RegularFight);
            run.Play(RunStep.RegularFight);
            run.AddCard(Guard);
            run.AddCard(Mend);
            return run;
        }

        // A run whose professor fight is already lost: the only line card is a shield, so the hero cannot win.
        private static Run CreateLosingRun()
        {
            var run = CreateReadyRun(capacity: 1);
            var preparation = run.BeginPreparation(RunStep.Professor);
            preparation.SwapWithReserve(0, 0);
            var session = preparation.Start();
            session.RunToEnd();
            session.Complete();
            return run;
        }

        private static Bestiary BestiaryWith(bool health, bool shield, params int[] positions)
        {
            var bestiary = new Bestiary();
            bestiary.Reveal(Professor, new ProfessorRevelation(health, shield, positions));
            return bestiary;
        }

        private static string[] Ids(IEnumerable<CardInstance> cards)
        {
            return cards.Select(c => c.Definition.Id).ToArray();
        }

        // --- Opening ---

        [Test]
        public void BeginPreparation_RegularFight_Throws()
        {
            var run = CreateReadyRun();

            Assert.Throws<ArgumentException>(() => run.BeginPreparation(RunStep.RegularFight));
        }

        [Test]
        public void BeginPreparation_NullStep_Throws()
        {
            var run = CreateReadyRun();

            Assert.Throws<ArgumentNullException>(() => run.BeginPreparation(null));
        }

        [Test]
        public void BeginPreparation_ProfessorNotAvailableYet_Throws()
        {
            var heroClass = new ClassDefinition("TestClass", 100, 0, 3, new[] { Strike }, new CardDefinition[0]);
            var biome = new BiomeDefinition("TestBiome", new[] { GoblinFight }, 2, ProfessorFight, Professor);
            var run = new Run(heroClass, biome, new RunRules(50, Curve), 1);

            Assert.Throws<InvalidOperationException>(() => run.BeginPreparation(RunStep.Professor));
        }

        [Test]
        public void BeginPreparation_SecretRoomStillLocked_Throws()
        {
            var run = CreateReadyRun();

            Assert.Throws<InvalidOperationException>(() => run.BeginPreparation(RunStep.SecretRoom("TestOtherRoom")));
        }

        [Test]
        public void BeginPreparation_ARunThatIsOver_Throws()
        {
            var run = CreateLosingRun();

            Assert.IsFalse(run.IsInProgress);
            Assert.Throws<InvalidOperationException>(() => run.BeginPreparation(RunStep.Professor));
        }

        [Test]
        public void BeginPreparation_WhileAFightSessionIsOpen_Throws()
        {
            var run = CreateReadyRun();
            run.BeginFight(RunStep.RegularFight);

            Assert.Throws<InvalidOperationException>(() => run.BeginPreparation(RunStep.Professor));
        }

        [Test]
        public void BeginPreparation_ChangesNothingByItself()
        {
            var run = CreateReadyRun();
            var fights = run.FightsPlayed;
            var line = Ids(run.Line);

            var preparation = run.BeginPreparation(RunStep.Professor);

            Assert.IsFalse(preparation.IsStarted);
            Assert.IsNull(run.CurrentFight);
            Assert.AreEqual(fights, run.FightsPlayed);
            CollectionAssert.AreEqual(line, Ids(preparation.Line));
        }

        // --- Editing ---

        [Test]
        public void Edits_RearrangeTheLine()
        {
            var run = CreateReadyRun();
            var preparation = run.BeginPreparation(RunStep.SecretRoom(Room));
            Assert.AreEqual(3, preparation.LineCapacity);
            CollectionAssert.AreEqual(new[] { Strike.Id, Guard.Id, Mend.Id }, Ids(preparation.Line));

            preparation.MoveInLine(0, 2);
            CollectionAssert.AreEqual(new[] { Guard.Id, Mend.Id, Strike.Id }, Ids(preparation.Line));

            preparation.MoveToReserve(0);
            preparation.MoveFromReserve(0);
            CollectionAssert.AreEqual(new[] { Mend.Id, Strike.Id, Guard.Id }, Ids(preparation.Line));
            Assert.IsEmpty(preparation.Reserve);
            CollectionAssert.AreEqual(Ids(run.Line), Ids(preparation.Line));
        }

        [Test]
        public void Edits_SwapWithReserve_ExchangesCards()
        {
            var run = CreateReadyRun(capacity: 1);
            var preparation = run.BeginPreparation(RunStep.Professor);
            CollectionAssert.AreEqual(new[] { Strike.Id }, Ids(preparation.Line));
            CollectionAssert.AreEqual(new[] { Guard.Id, Mend.Id }, Ids(preparation.Reserve));

            preparation.SwapWithReserve(0, 1);

            CollectionAssert.AreEqual(new[] { Mend.Id }, Ids(preparation.Line));
            CollectionAssert.AreEqual(new[] { Guard.Id, Strike.Id }, Ids(preparation.Reserve));
        }

        [Test]
        public void Edits_TheReserveHasNoSizeLimit()
        {
            var run = CreateReadyRun(capacity: 1);
            for (var i = 0; i < 20; i++)
            {
                run.AddCard(Unique);
            }

            var preparation = run.BeginPreparation(RunStep.Professor);

            Assert.AreEqual(22, preparation.Reserve.Count);
            preparation.SwapWithReserve(0, 21);
            Assert.AreEqual(22, preparation.Reserve.Count);
        }

        [Test]
        public void Edits_InvalidIndexes_Throw()
        {
            var run = CreateReadyRun(capacity: 1);
            var preparation = run.BeginPreparation(RunStep.Professor);

            Assert.Throws<ArgumentOutOfRangeException>(() => preparation.MoveInLine(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => preparation.SwapWithReserve(0, 9));
        }

        // --- Start: the line is fixed ---

        [Test]
        public void Start_OpensAFightWhoseLineIsFixed()
        {
            var run = CreateReadyRun();
            var preparation = run.BeginPreparation(RunStep.Professor);
            preparation.MoveInLine(0, 2);
            var arranged = Ids(preparation.Line);

            var session = preparation.Start();

            Assert.IsTrue(preparation.IsStarted);
            Assert.AreSame(session, run.CurrentFight);
            Assert.AreEqual(RunStep.Professor, session.Step);
            Assert.IsFalse(session.LineEditsAllowed);
            CollectionAssert.AreEqual(arranged, session.HeroLine.Select(c => c.Id).ToArray());
        }

        [Test]
        public void Start_TheSessionRefusesLineChanges()
        {
            var run = CreateReadyRun();
            var session = run.BeginPreparation(RunStep.SecretRoom(Room)).Start();
            var before = Ids(run.Line);

            Assert.Catch<InvalidOperationException>(() => session.MoveCard(0, 1));
            Assert.Catch<InvalidOperationException>(() => session.SwapWithReserve(0, 0));
            CollectionAssert.AreEqual(before, Ids(run.Line));
        }

        [Test]
        public void AfterStart_EditsAndASecondStartThrow()
        {
            var run = CreateReadyRun();
            var preparation = run.BeginPreparation(RunStep.Professor);
            preparation.Start();

            Assert.Throws<InvalidOperationException>(() => preparation.MoveInLine(0, 1));
            Assert.Throws<InvalidOperationException>(() => preparation.SwapWithReserve(0, 0));
            Assert.Throws<InvalidOperationException>(() => preparation.MoveToReserve(0));
            Assert.Throws<InvalidOperationException>(() => preparation.MoveFromReserve(0));
            Assert.Throws<InvalidOperationException>(() => preparation.Start());
            Assert.Throws<InvalidOperationException>(() => run.MoveInLine(0, 1));
        }

        [Test]
        public void Start_TheArrangementIsWhatTheFightPlays()
        {
            var first = CreateReadyRun();
            var preparation = first.BeginPreparation(RunStep.Professor);
            preparation.MoveInLine(0, 2);
            var session = preparation.Start();
            session.RunToEnd();
            var report = session.Complete();

            Assert.IsTrue(report.HeroWon);
            CollectionAssert.AreEqual(new[] { Guard.Id, Mend.Id, Strike.Id }, Ids(report.HeroLine));
        }

        // --- No retry ---

        [Test]
        public void Defeat_EndsTheRunSoThereIsNoSecondPreparation()
        {
            var run = CreateReadyRun(capacity: 1);
            var preparation = run.BeginPreparation(RunStep.Professor);
            preparation.SwapWithReserve(0, 0);
            var session = preparation.Start();
            session.RunToEnd();

            var report = session.Complete();

            Assert.IsFalse(report.HeroWon);
            Assert.IsFalse(run.IsInProgress);
            Assert.Throws<InvalidOperationException>(() => run.BeginPreparation(RunStep.Professor));
            Assert.Throws<InvalidOperationException>(() => run.BeginFight(RunStep.Professor));
        }

        // --- Information ---

        [Test]
        public void ProfessorKnowledge_EmptyBestiary_HidesEverythingButTheLineLength()
        {
            var run = CreateReadyRun();

            var knowledge = run.BeginPreparation(RunStep.Professor, new Bestiary()).ProfessorKnowledge;

            Assert.IsNull(knowledge.MaxHealth);
            Assert.IsNull(knowledge.Shield);
            Assert.AreEqual(3, knowledge.SpellLine.Count);
            Assert.IsTrue(knowledge.IsUnknown);
        }

        [Test]
        public void ProfessorKnowledge_NoBestiary_IsTheSameAsAnEmptyOne()
        {
            var run = CreateReadyRun();

            var knowledge = run.BeginPreparation(RunStep.Professor).ProfessorKnowledge;

            Assert.AreEqual(Professor.Id, knowledge.ProfessorId);
            Assert.IsTrue(knowledge.IsUnknown);
        }

        [Test]
        public void ProfessorKnowledge_ShowsOnlyWhatTheBestiaryRevealed()
        {
            var run = CreateReadyRun();

            var knowledge = run.BeginPreparation(RunStep.Professor, BestiaryWith(true, false, 1)).ProfessorKnowledge;

            Assert.AreEqual(Professor.MaxHealth, knowledge.MaxHealth);
            Assert.IsNull(knowledge.Shield);
            Assert.IsNull(knowledge.SpellLine[0]);
            Assert.AreSame(EnemyJab, knowledge.SpellLine[1]);
            Assert.IsNull(knowledge.SpellLine[2]);
        }

        [Test]
        public void ProfessorKnowledge_ShieldRevealedAlone_ShowsTheShield()
        {
            var run = CreateReadyRun();

            var knowledge = run.BeginPreparation(RunStep.Professor, BestiaryWith(false, true)).ProfessorKnowledge;

            Assert.IsNull(knowledge.MaxHealth);
            Assert.AreEqual(Professor.Shield, knowledge.Shield);
        }

        [Test]
        public void Steps_ProfessorHidesTheEncounter_MiniBossHasNoKnowledge()
        {
            var run = CreateReadyRun();

            var professor = run.BeginPreparation(RunStep.Professor);
            var miniBoss = run.BeginPreparation(RunStep.SecretRoom(Room));

            Assert.IsNull(professor.MiniBossEncounter);
            Assert.IsNotNull(professor.ProfessorKnowledge);
            Assert.AreSame(MiniBoss, miniBoss.MiniBossEncounter);
            Assert.IsNull(miniBoss.ProfessorKnowledge);
        }
    }
}
