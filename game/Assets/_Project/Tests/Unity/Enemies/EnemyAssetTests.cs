using System;
using System.Linq;
using Game.Unity.Enemies;
using NUnit.Framework;

namespace Game.Unity.Tests.Enemies
{
    public class EnemyAssetTests
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
        public void ToParticipant_ValidAsset_CopiesStats()
        {
            var enemy = _assets.Enemy("test_enemy", EnemyRank.Regular, 12, 3, _assets.Card("test_card_a"));

            var combatant = enemy.ToParticipant().Combatant;

            Assert.AreEqual(12, combatant.MaxHealth);
            Assert.AreEqual(12, combatant.CurrentHealth);
            Assert.AreEqual(3, combatant.Shield);
        }

        [Test]
        public void ToParticipant_ValidAsset_CopiesSpellLineInOrder()
        {
            var enemy = _assets.Enemy("test_enemy", EnemyRank.Professor, 5, 0,
                _assets.Card("test_card_a"), _assets.Card("test_card_b"), _assets.Card("test_card_c"));

            var line = enemy.ToParticipant().SpellLine;

            CollectionAssert.AreEqual(
                new[] { "test_card_a", "test_card_b", "test_card_c" },
                Enumerable.Range(0, line.Count).Select(i => line[i].Id));
        }

        [Test]
        public void ToParticipant_CalledTwice_ReturnsIndependentCombatants()
        {
            var enemy = _assets.Enemy("test_enemy", EnemyRank.Regular, 5, 0, _assets.Card("test_card_a"));

            Assert.AreNotSame(enemy.ToParticipant().Combatant, enemy.ToParticipant().Combatant);
        }

        [Test]
        public void Properties_ValidAsset_ExposeIdAndRank()
        {
            var enemy = _assets.Enemy("test_enemy", EnemyRank.MiniBoss, 5, 0, _assets.Card("test_card_a"));

            Assert.AreEqual("test_enemy", enemy.Id);
            Assert.AreEqual(EnemyRank.MiniBoss, enemy.Rank);
            Assert.IsFalse(enemy.IsPlaceholder);
        }

        [Test]
        public void ToParticipant_EmptySpellLine_ThrowsNamingTheAsset()
        {
            var enemy = _assets.Enemy("test_enemy", EnemyRank.Regular, 5, 0);

            var exception = Assert.Throws<InvalidOperationException>(() => enemy.ToParticipant());

            StringAssert.Contains("'test_enemy'", exception.Message);
            StringAssert.Contains("no card", exception.Message);
        }

        [Test]
        public void ToParticipant_EmptyCardSlot_ThrowsNamingThePosition()
        {
            var enemy = _assets.Enemy("test_enemy", EnemyRank.Regular, 5, 0, _assets.Card("test_card_a"), null);

            var exception = Assert.Throws<InvalidOperationException>(() => enemy.ToParticipant());

            StringAssert.Contains("position 1", exception.Message);
        }

        [Test]
        public void ToParticipant_MissingId_Throws()
        {
            var enemy = _assets.Enemy("", EnemyRank.Regular, 5, 0, _assets.Card("test_card_a"));

            var exception = Assert.Throws<InvalidOperationException>(() => enemy.ToParticipant());

            StringAssert.Contains("no id", exception.Message);
        }

        [Test]
        public void ToParticipant_InvalidHealth_ThrowsNamingTheAsset()
        {
            var enemy = _assets.Enemy("test_enemy", EnemyRank.Regular, 0, 0, _assets.Card("test_card_a"));

            var exception = Assert.Throws<InvalidOperationException>(() => enemy.ToParticipant());

            StringAssert.Contains("'test_enemy'", exception.Message);
        }
    }
}
