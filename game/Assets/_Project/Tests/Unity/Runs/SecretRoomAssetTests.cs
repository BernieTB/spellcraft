using System;
using Game.Core.Runs;
using Game.Unity.Tests.Enemies;
using NUnit.Framework;

namespace Game.Unity.Tests.Runs
{
    /// <summary>Conversion of <see cref="Game.Unity.Runs.SecretRoomAsset"/> to Core. Ids and numbers are arbitrary test data.</summary>
    public class SecretRoomAssetTests
    {
        private TestAssets _assets;

        [SetUp]
        public void SetUp()
        {
            _assets = new TestAssets();
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        private Game.Unity.Runs.SecretRoomAsset ValidRoom(
            string id = "test_room",
            int objectiveCount = 3,
            int bonusLineSlots = 1,
            bool revealsHealth = false,
            bool revealsShield = false,
            params int[] positions)
        {
            var card = _assets.Card("test_card_a");
            var goblin = _assets.Enemy("test_goblin", Game.Unity.Enemies.EnemyRank.Regular, 5, 0, card);
            var boss = _assets.Enemy("test_boss", Game.Unity.Enemies.EnemyRank.MiniBoss, 20, 0, card);
            var bossFight = _assets.Encounter("test_boss_fight", boss);
            return _assets.SecretRoom(
                id, goblin, objectiveCount, bossFight, bonusLineSlots, _assets.Card("test_unique"), revealsHealth, revealsShield, positions);
        }

        [Test]
        public void ToDefinition_ValidAsset_CopiesIdObjectiveAndRewards()
        {
            var asset = ValidRoom(bonusLineSlots: 2);

            var definition = asset.ToDefinition();

            Assert.AreEqual("test_room", definition.Id);
            Assert.AreEqual("test_goblin", definition.Objective.EnemyId);
            Assert.AreEqual(3, definition.Objective.Count);
            Assert.AreEqual("test_boss_fight", definition.MiniBossEncounter.Id);
            Assert.AreEqual(2, definition.BonusLineSlots);
            Assert.AreEqual("test_unique", definition.UniqueCard.Id);
        }

        [Test]
        public void ToDefinition_ValidAsset_CopiesTheRevelation()
        {
            var asset = ValidRoom(revealsHealth: true, revealsShield: false, positions: new[] { 2, 0 });

            var revelation = asset.ToDefinition().Revelation;

            Assert.IsTrue(revelation.RevealsHealth);
            Assert.IsFalse(revelation.RevealsShield);
            CollectionAssert.AreEqual(new[] { 0, 2 }, revelation.CardPositions);
        }

        [Test]
        public void ToDefinition_NoRevelation_RevealsNothing()
        {
            var revelation = ValidRoom().ToDefinition().Revelation;

            Assert.IsFalse(revelation.RevealsHealth);
            Assert.IsFalse(revelation.RevealsShield);
            Assert.IsEmpty(revelation.CardPositions);
        }

        [Test]
        public void ToDefinition_NoBonusSlots_IsAllowed()
        {
            Assert.AreEqual(0, ValidRoom(bonusLineSlots: 0).ToDefinition().BonusLineSlots);
        }

        [Test]
        public void ToDefinition_NoId_ThrowsNamingTheAsset()
        {
            var asset = ValidRoom(id: "");

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            StringAssert.Contains("no id", exception.Message);
        }

        [Test]
        public void ToDefinition_NoObjectiveEnemy_Throws()
        {
            var asset = _assets.SecretRoom(
                "test_room", null, 1, _assets.Encounter("test_boss_fight", _assets.Enemy("test_boss", Game.Unity.Enemies.EnemyRank.MiniBoss, 20, 0, _assets.Card("test_card_a"))), 1, _assets.Card("test_unique"));

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            StringAssert.Contains("'test_room'", exception.Message);
            StringAssert.Contains("objective enemy", exception.Message);
        }

        [Test]
        public void ToDefinition_NoMiniBossEncounter_Throws()
        {
            var goblin = _assets.Enemy("test_goblin", Game.Unity.Enemies.EnemyRank.Regular, 5, 0, _assets.Card("test_card_a"));
            var asset = _assets.SecretRoom("test_room", goblin, 1, null, 1, _assets.Card("test_unique"));

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            StringAssert.Contains("mini-boss encounter", exception.Message);
        }

        [Test]
        public void ToDefinition_NoUniqueCard_Throws()
        {
            var card = _assets.Card("test_card_a");
            var goblin = _assets.Enemy("test_goblin", Game.Unity.Enemies.EnemyRank.Regular, 5, 0, card);
            var bossFight = _assets.Encounter("test_boss_fight", _assets.Enemy("test_boss", Game.Unity.Enemies.EnemyRank.MiniBoss, 20, 0, card));
            var asset = _assets.SecretRoom("test_room", goblin, 1, bossFight, 1, null);

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            StringAssert.Contains("unique card", exception.Message);
        }

        [Test]
        public void ToDefinition_ObjectiveCountBelowOne_ThrowsWithTheCoreCause()
        {
            var asset = ValidRoom(objectiveCount: 0);

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            StringAssert.Contains("'test_room'", exception.Message);
            Assert.IsInstanceOf<ArgumentOutOfRangeException>(exception.InnerException);
        }

        [Test]
        public void ToDefinition_NegativeBonusSlots_ThrowsWithTheCoreCause()
        {
            var asset = ValidRoom(bonusLineSlots: -1);

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            Assert.IsInstanceOf<ArgumentOutOfRangeException>(exception.InnerException);
        }

        [Test]
        public void ToDefinition_NegativeRevealedPosition_ThrowsWithTheCoreCause()
        {
            var asset = ValidRoom(positions: new[] { -1 });

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            Assert.IsInstanceOf<ArgumentOutOfRangeException>(exception.InnerException);
        }

        [Test]
        public void ToDefinition_InvalidMiniBossEncounter_NamesTheRoomAndKeepsTheCause()
        {
            var goblin = _assets.Enemy("test_goblin", Game.Unity.Enemies.EnemyRank.Regular, 5, 0, _assets.Card("test_card_a"));
            var emptyFight = _assets.Encounter("test_empty_fight");
            var asset = _assets.SecretRoom("test_room", goblin, 1, emptyFight, 1, _assets.Card("test_unique"));

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            StringAssert.Contains("Secret room asset 'test_room'", exception.Message);
            StringAssert.Contains("no enemy", exception.Message);
        }

        [Test]
        public void IsPlaceholder_ReadsTheFlag()
        {
            var asset = ValidRoom();
            Assert.IsFalse(asset.IsPlaceholder);

            var serialized = new UnityEditor.SerializedObject(asset);
            serialized.FindProperty("_isPlaceholder").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsTrue(asset.IsPlaceholder);
        }
    }
}
