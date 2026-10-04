using System;
using Game.Core.Cards;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Meta;
using NUnit.Framework;

namespace Game.Core.Tests.Meta
{
    public class BestiaryStorageTests
    {
        // Placeholder ids and test values: not game content.
        private static readonly EnemyDefinition Professor = new EnemyDefinition(
            "TestProfessor1",
            40,
            5,
            new[] { new CardDefinition("TestCard1", 1, new IEffect[] { new DealDamageEffect(2) }) });

        private sealed class MemoryStore : IBestiaryStore
        {
            public string Text;

            public bool TryRead(out string text)
            {
                text = Text;
                return Text != null;
            }

            public void Write(string text)
            {
                Text = text;
            }
        }

        [Test]
        public void Load_NothingSaved_IsMissingAndEmpty()
        {
            var result = BestiaryStorage.Load(new MemoryStore());

            Assert.AreEqual(BestiaryLoadStatus.Missing, result.Status);
            Assert.AreEqual(0, result.Bestiary.Entries.Count);
            Assert.IsNull(result.Error);
        }

        [Test]
        public void SaveThenLoad_KeepsTheKnowledge()
        {
            var store = new MemoryStore();
            var bestiary = new Bestiary();
            bestiary.Reveal(Professor, new ProfessorRevelation(true, false, new[] { 0 }));

            BestiaryStorage.Save(store, bestiary);
            var result = BestiaryStorage.Load(store);

            Assert.AreEqual(BestiaryLoadStatus.Loaded, result.Status);
            var knowledge = result.Bestiary.GetKnowledge(Professor);
            Assert.AreEqual(40, knowledge.MaxHealth);
            Assert.IsNull(knowledge.Shield);
            Assert.AreSame(Professor.SpellLine[0], knowledge.SpellLine[0]);
        }

        [Test]
        public void Load_CorruptSave_IsUnreadableAndEmpty()
        {
            var result = BestiaryStorage.Load(new MemoryStore { Text = "{ broken" });

            Assert.AreEqual(BestiaryLoadStatus.Unreadable, result.Status);
            Assert.AreEqual(0, result.Bestiary.Entries.Count);
            Assert.IsNotEmpty(result.Error);
        }

        [Test]
        public void Load_UnknownVersion_IsUnreadable()
        {
            var result = BestiaryStorage.Load(new MemoryStore { Text = "{\"version\":99,\"professors\":[]}" });

            Assert.AreEqual(BestiaryLoadStatus.Unreadable, result.Status);
            StringAssert.Contains("version 99", result.Error);
        }

        [Test]
        public void NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => BestiaryStorage.Load(null));
            Assert.Throws<ArgumentNullException>(() => BestiaryStorage.Save(null, new Bestiary()));
            Assert.Throws<ArgumentNullException>(() => BestiaryStorage.Save(new MemoryStore(), null));
        }
    }
}
