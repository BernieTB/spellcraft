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

        private static readonly Regex CouldNotRead = new Regex(@"^\[Bestiary\] The save .* could not be read");

        private string _folder;
        private FileBestiaryStore _store;

        private string SaveFolder => Path.GetDirectoryName(_store.Path);

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

        private void WriteSave(string text)
        {
            Directory.CreateDirectory(SaveFolder);
            File.WriteAllText(_store.Path, text);
        }

        [Test]
        public void Load_NoFile_IsMissingAndEmpty()
        {
            var result = _store.Load();

            Assert.AreEqual(BestiaryLoadStatus.Missing, result.Status);
            Assert.AreEqual(0, result.Bestiary.Entries.Count);
            Assert.IsFalse(_store.WritesBlocked);
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
            WriteSave("{ broken");
            LogAssert.Expect(LogType.Warning, CouldNotRead);

            var result = _store.Load();

            Assert.AreEqual(BestiaryLoadStatus.Unreadable, result.Status);
            Assert.AreEqual(0, result.Bestiary.Entries.Count);
            Assert.AreEqual(Path.Combine(SaveFolder, "bestiary.unreadable.json"), _store.UnreadableCopyPath);
            Assert.AreEqual("{ broken", File.ReadAllText(_store.UnreadableCopyPath));
            Assert.IsFalse(_store.WritesBlocked);
        }

        [Test]
        public void Load_UnknownVersion_KeepsACopy()
        {
            WriteSave("{\"version\":99,\"professors\":[]}");
            LogAssert.Expect(LogType.Warning, CouldNotRead);

            var result = _store.Load();

            Assert.AreEqual(BestiaryLoadStatus.Unreadable, result.Status);
            Assert.AreEqual("{\"version\":99,\"professors\":[]}", File.ReadAllText(_store.UnreadableCopyPath));
        }

        [Test]
        public void Load_UnreadableTwice_NeverOverwritesAnOlderCopy()
        {
            WriteSave("{ first");
            LogAssert.Expect(LogType.Warning, CouldNotRead);
            _store.Load();
            var firstCopy = _store.UnreadableCopyPath;

            WriteSave("{ second");
            LogAssert.Expect(LogType.Warning, CouldNotRead);
            _store.Load();

            Assert.AreEqual("{ first", File.ReadAllText(firstCopy));
            Assert.AreEqual(Path.Combine(SaveFolder, "bestiary.unreadable-1.json"), _store.UnreadableCopyPath);
            Assert.AreEqual("{ second", File.ReadAllText(_store.UnreadableCopyPath));
        }

        [Test]
        public void Load_LockedFile_IsUnreadableAndBlocksWrites()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                Assert.Ignore("Exclusive file locks are only enforced on Windows.");
            }

            WriteSave("{\"version\":1,\"professors\":[]}");
            using (new FileStream(_store.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                LogAssert.Expect(LogType.Warning, new Regex(@"^\[Bestiary\] The save .* no copy could be kept"));

                var result = _store.Load();

                Assert.AreEqual(BestiaryLoadStatus.Unreadable, result.Status);
                Assert.IsTrue(_store.WritesBlocked);
                Assert.IsNull(_store.UnreadableCopyPath);
            }

            Assert.Throws<InvalidOperationException>(() => BestiaryStorage.Save(_store, new Bestiary()));
            Assert.AreEqual("{\"version\":1,\"professors\":[]}", File.ReadAllText(_store.Path));
        }

        [Test]
        public void ForPersistentData_UsesThePersistentDataFolder()
        {
            var store = FileBestiaryStore.ForPersistentData();

            Assert.AreEqual(Path.Combine(Application.persistentDataPath, FileBestiaryStore.FileName), store.Path);
        }

        [TestCase(null)]
        [TestCase(" ")]
        [TestCase("saves/bestiary.json")]
        public void Constructor_BlankOrRelativePath_Throws(string path)
        {
            Assert.Throws<ArgumentException>(() => new FileBestiaryStore(path));
        }
    }
}
