using System;
using System.IO;
using System.Text.RegularExpressions;
using Game.Core.Cards;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Meta;
using Game.Unity.Meta;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Unity.Tests.Meta
{
    public class FileBestiaryStoreTests
    {
        // Placeholder ids and test values: not game content.
        private static readonly EnemyDefinition Professor = new EnemyDefinition(
            "TestProfessor1",
            40,
            5,
            new[] { new CardDefinition("TestCard1", 1, new IEffect[] { new DealDamageEffect(2) }) });

        private string _folder;
        private FileBestiaryStore _store;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "SpellcraftBestiaryTests", Guid.NewGuid().ToString("N"));
            _store = new FileBestiaryStore(Path.Combine(_folder, "saves", FileBestiaryStore.FileName));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, true);
            }
        }

        [Test]
        public void Load_NoFile_IsMissingAndEmpty()
        {
            var result = _store.Load();

            Assert.AreEqual(BestiaryLoadStatus.Missing, result.Status);
            Assert.AreEqual(0, result.Bestiary.Entries.Count);
        }

        [Test]
        public void SaveThenLoad_CreatesTheFolderAndKeepsTheKnowledge()
        {
            var bestiary = new Bestiary();
            bestiary.Reveal(Professor, new ProfessorRevelation(false, true, new[] { 0 }));

            BestiaryStorage.Save(_store, bestiary);
            var result = _store.Load();

            Assert.AreEqual(BestiaryLoadStatus.Loaded, result.Status);
            Assert.AreEqual(5, result.Bestiary.GetKnowledge(Professor).Shield);
        }

        [Test]
        public void Save_Twice_ReplacesTheFileAndLeavesNoTemporaryFile()
        {
            var bestiary = new Bestiary();
            BestiaryStorage.Save(_store, bestiary);
            bestiary.Reveal(Professor, new ProfessorRevelation(true, false, new int[0]));

            BestiaryStorage.Save(_store, bestiary);

            Assert.AreEqual(BestiaryJson.Serialize(bestiary), File.ReadAllText(_store.Path));
            Assert.IsFalse(File.Exists(_store.Path + ".tmp"));
        }

        [Test]
        public void Load_CorruptFile_StartsEmptyWarnsAndKeepsACopy()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_store.Path));
            File.WriteAllText(_store.Path, "{ broken");
            LogAssert.Expect(LogType.Warning, new Regex(@"^\[Bestiary\] The save .* could not be read"));

            var result = _store.Load();

            Assert.AreEqual(BestiaryLoadStatus.Unreadable, result.Status);
            Assert.AreEqual(0, result.Bestiary.Entries.Count);
            Assert.AreEqual("{ broken", File.ReadAllText(_store.UnreadableCopyPath));
        }

        [Test]
        public void ForPersistentData_UsesThePersistentDataFolder()
        {
            var store = FileBestiaryStore.ForPersistentData();

            Assert.AreEqual(Path.Combine(Application.persistentDataPath, FileBestiaryStore.FileName), store.Path);
        }

        [TestCase(null)]
        [TestCase(" ")]
        public void Constructor_BlankPath_Throws(string path)
        {
            Assert.Throws<ArgumentException>(() => new FileBestiaryStore(path));
        }
    }
}
