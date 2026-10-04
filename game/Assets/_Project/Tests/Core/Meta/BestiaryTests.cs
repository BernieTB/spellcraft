using System;
using Game.Core.Cards;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Meta;
using NUnit.Framework;

namespace Game.Core.Tests.Meta
{
    public class BestiaryTests
    {
        // Placeholder ids and test values: not game content.
        private static readonly CardDefinition Strike = new CardDefinition("TestCard1", 1, new IEffect[] { new DealDamageEffect(2) });
        private static readonly CardDefinition Guard = new CardDefinition("TestCard2", 1, new IEffect[] { new GainShieldEffect(2) });

        private static EnemyDefinition Professor(string id = "TestProfessor1", params CardDefinition[] line)
        {
            return new EnemyDefinition(id, 40, 5, line.Length > 0 ? line : new[] { Strike, Guard, Strike });
        }

        private static ProfessorRevelation Reveal(bool health = false, bool shield = false, params int[] positions)
        {
            return new ProfessorRevelation(health, shield, positions);
        }

        [Test]
        public void GetKnowledge_UnknownProfessor_HidesEverythingButTheLineLength()
        {
            var knowledge = new Bestiary().GetKnowledge(Professor());

            Assert.IsTrue(knowledge.IsUnknown);
            Assert.IsNull(knowledge.MaxHealth);
            Assert.IsNull(knowledge.Shield);
            CollectionAssert.AreEqual(new CardDefinition[] { null, null, null }, knowledge.SpellLine);
        }

        [Test]
        public void Find_UnknownProfessor_ReturnsNull()
        {
            Assert.IsNull(new Bestiary().Find("TestProfessor1"));
        }

        [Test]
        public void Reveal_HealthShieldAndCards_AreKnown()
        {
            var bestiary = new Bestiary();
            var professor = Professor();

            Assert.IsTrue(bestiary.Reveal(professor, Reveal(health: true, shield: true, positions: new[] { 1 })));

            var knowledge = bestiary.GetKnowledge(professor);
            Assert.AreEqual(40, knowledge.MaxHealth);
            Assert.AreEqual(5, knowledge.Shield);
            CollectionAssert.AreEqual(new[] { null, Guard, null }, knowledge.SpellLine);
            Assert.IsFalse(knowledge.IsUnknown);
        }

        [Test]
        public void Reveal_SameRevelationTwice_IsIdempotent()
        {
            var bestiary = new Bestiary();
            var professor = Professor();
            bestiary.Reveal(professor, Reveal(health: true, positions: new[] { 0 }));
            var entry = bestiary.Find(professor.Id);

            Assert.IsFalse(bestiary.Reveal(professor, Reveal(health: true, positions: new[] { 0 })));
            Assert.AreSame(entry, bestiary.Find(professor.Id));
        }

        [Test]
        public void Reveal_AddsToEarlierRevelations()
        {
            var bestiary = new Bestiary();
            var professor = Professor();
            bestiary.Reveal(professor, Reveal(health: true, positions: new[] { 2 }));

            Assert.IsTrue(bestiary.Reveal(professor, Reveal(shield: true, positions: new[] { 0 })));

            var knowledge = bestiary.GetKnowledge(professor);
            Assert.AreEqual(40, knowledge.MaxHealth);
            Assert.AreEqual(5, knowledge.Shield);
            CollectionAssert.AreEqual(new[] { Strike, null, Strike }, knowledge.SpellLine);
        }

        [Test]
        public void Reveal_TwoCopiesOfACard_AreTrackedByPosition()
        {
            var bestiary = new Bestiary();
            var professor = Professor();

            bestiary.Reveal(professor, Reveal(positions: new[] { 0 }));

            CollectionAssert.AreEqual(new[] { Strike, null, null }, bestiary.GetKnowledge(professor).SpellLine);
        }

        [Test]
        public void Reveal_NothingRevealed_RecordsNothing()
        {
            var bestiary = new Bestiary();

            Assert.IsFalse(bestiary.Reveal(Professor(), Reveal()));
            Assert.AreEqual(0, bestiary.Entries.Count);
        }

        [Test]
        public void Reveal_PositionOutsideTheLine_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Bestiary().Reveal(Professor(), Reveal(positions: new[] { 3 })));
        }

        [Test]
        public void Reveal_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new Bestiary().Reveal(null, Reveal(health: true)));
            Assert.Throws<ArgumentNullException>(() => new Bestiary().Reveal(Professor(), null));
        }

        [Test]
        public void Revelation_NegativePosition_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Reveal(positions: new[] { -1 }));
        }

        [Test]
        public void Revelation_Positions_AreDistinctAndSorted()
        {
            CollectionAssert.AreEqual(new[] { 0, 2 }, Reveal(positions: new[] { 2, 0, 2 }).CardPositions);
        }

        [Test]
        public void GetKnowledge_CardChangedSinceTheReveal_IsUnknown()
        {
            var bestiary = new Bestiary();
            bestiary.Reveal(Professor(), Reveal(positions: new[] { 0 }));
            var changedProfessor = Professor("TestProfessor1", Guard, Guard, Strike);

            CollectionAssert.AreEqual(new CardDefinition[] { null, null, null }, bestiary.GetKnowledge(changedProfessor).SpellLine);
        }

        [Test]
        public void GetKnowledge_LineShortenedSinceTheReveal_IgnoresTheMissingPosition()
        {
            var bestiary = new Bestiary();
            bestiary.Reveal(Professor(), Reveal(positions: new[] { 2 }));

            CollectionAssert.AreEqual(new CardDefinition[] { null }, bestiary.GetKnowledge(Professor("TestProfessor1", Strike)).SpellLine);
        }

        [Test]
        public void Reveal_AfterTheDataChanged_ReplacesTheOldCard()
        {
            var bestiary = new Bestiary();
            bestiary.Reveal(Professor(), Reveal(positions: new[] { 0 }));
            var changedProfessor = Professor("TestProfessor1", Guard, Guard, Strike);

            Assert.IsTrue(bestiary.Reveal(changedProfessor, Reveal(positions: new[] { 0 })));
            Assert.AreEqual("TestCard2", bestiary.Find("TestProfessor1").Cards[0].CardId);
        }

        [Test]
        public void Entries_AreSortedByProfessorId()
        {
            var bestiary = new Bestiary();
            bestiary.Reveal(Professor("TestProfessorC"), Reveal(health: true));
            bestiary.Reveal(Professor("TestProfessorA"), Reveal(health: true));
            bestiary.Reveal(Professor("TestProfessorB"), Reveal(health: true));

            Assert.AreEqual("TestProfessorA", bestiary.Entries[0].ProfessorId);
            Assert.AreEqual("TestProfessorB", bestiary.Entries[1].ProfessorId);
            Assert.AreEqual("TestProfessorC", bestiary.Entries[2].ProfessorId);
        }

        [Test]
        public void Knowledge_IsKeptPerProfessor()
        {
            var bestiary = new Bestiary();
            bestiary.Reveal(Professor("TestProfessorA"), Reveal(health: true));

            Assert.IsTrue(bestiary.GetKnowledge(Professor("TestProfessorB")).IsUnknown);
        }
    }
}
