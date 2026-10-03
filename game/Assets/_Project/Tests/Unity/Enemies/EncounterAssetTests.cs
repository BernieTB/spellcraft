using System;
using System.Linq;
using Game.Unity.Enemies;
using NUnit.Framework;

namespace Game.Unity.Tests.Enemies
{
    public class EncounterAssetTests
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

        [Test]
        public void ToParticipants_ValidAsset_KeepsEnemyOrder()
        {
            var card = _assets.Card("test_card_a");
            var first = _assets.Enemy("test_enemy_a", EnemyRank.Regular, 7, 0, card);
            var second = _assets.Enemy("test_enemy_b", EnemyRank.Regular, 9, 0, card);
            var encounter = _assets.Encounter("test_encounter", first, second);

            var participants = encounter.ToParticipants();

            CollectionAssert.AreEqual(new[] { 7, 9 }, participants.Select(p => p.Combatant.MaxHealth));
        }

        [Test]
        public void ToParticipants_SameEnemyTwice_ReturnsIndependentCombatants()
        {
            var enemy = _assets.Enemy("test_enemy", EnemyRank.Regular, 5, 0, _assets.Card("test_card_a"));
            var encounter = _assets.Encounter("test_encounter", enemy, enemy);

            var participants = encounter.ToParticipants();

            Assert.AreEqual(2, participants.Count);
            Assert.AreNotSame(participants[0].Combatant, participants[1].Combatant);
        }

        [Test]
        public void Properties_ValidAsset_ExposeIdAndEnemies()
        {
            var enemy = _assets.Enemy("test_enemy", EnemyRank.Regular, 5, 0, _assets.Card("test_card_a"));
            var encounter = _assets.Encounter("test_encounter", enemy);

            Assert.AreEqual("test_encounter", encounter.Id);
            CollectionAssert.AreEqual(new[] { enemy }, encounter.Enemies);
            Assert.IsFalse(encounter.IsPlaceholder);
        }

        [Test]
        public void ToParticipants_NoEnemy_ThrowsNamingTheAsset()
        {
            var encounter = _assets.Encounter("test_encounter");

            var exception = Assert.Throws<InvalidOperationException>(() => encounter.ToParticipants());

            StringAssert.Contains("'test_encounter'", exception.Message);
            StringAssert.Contains("no enemy", exception.Message);
        }

        [Test]
        public void ToParticipants_EmptyEnemySlot_ThrowsNamingTheSlot()
        {
            var enemy = _assets.Enemy("test_enemy", EnemyRank.Regular, 5, 0, _assets.Card("test_card_a"));
            var encounter = _assets.Encounter("test_encounter", enemy, null);

            var exception = Assert.Throws<InvalidOperationException>(() => encounter.ToParticipants());

            StringAssert.Contains("slot 1", exception.Message);
        }

        [Test]
        public void ToParticipants_MissingId_Throws()
        {
            var enemy = _assets.Enemy("test_enemy", EnemyRank.Regular, 5, 0, _assets.Card("test_card_a"));
            var encounter = _assets.Encounter("", enemy);

            var exception = Assert.Throws<InvalidOperationException>(() => encounter.ToParticipants());

            StringAssert.Contains("no id", exception.Message);
        }

        [Test]
        public void ToParticipants_InvalidEnemy_ThrowsNamingBothAssets()
        {
            var enemy = _assets.Enemy("test_enemy", EnemyRank.Regular, 5, 0);
            var encounter = _assets.Encounter("test_encounter", enemy);

            var exception = Assert.Throws<InvalidOperationException>(() => encounter.ToParticipants());

            StringAssert.Contains("'test_encounter'", exception.Message);
            StringAssert.Contains("'test_enemy'", exception.Message);
        }
    }
}
