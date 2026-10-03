using System;
using Game.Core.Effects;
using Game.Core.Upgrades;
using Game.Unity.Upgrades;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Tests.Upgrades
{
    public class PassiveUpgradeAssetTests
    {
        // Placeholder id and arbitrary test data, not real content or balance values.
        private const string Id = "test_upgrade_01";
        private const int Amount = 3;

        private PassiveUpgradeAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<PassiveUpgradeAsset>();
            _asset.name = "TestPassiveUpgradeAsset";
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_asset);
        }

        /// <summary>
        /// Writes the asset through its serialized fields, as the inspector would.
        /// </summary>
        private void Author(string id, int kind, int effectKind, int amount, bool isPlaceholder = false)
        {
            var serialized = new SerializedObject(_asset);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_kind").intValue = kind;
            serialized.FindProperty("_effectKind").intValue = effectKind;
            serialized.FindProperty("_amount").intValue = amount;
            serialized.FindProperty("_isPlaceholder").boolValue = isPlaceholder;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TestCase(PassiveUpgradeKind.MaxHealth)]
        [TestCase(PassiveUpgradeKind.StartingShield)]
        [TestCase(PassiveUpgradeKind.NeighbourBonus)]
        public void ToUpgrade_KindWithoutEffectKind_CopiesIdKindAndAmount(PassiveUpgradeKind kind)
        {
            Author(Id, (int)kind, (int)BonusKind.Shield, Amount);

            var upgrade = _asset.ToUpgrade();

            Assert.AreEqual(Id, upgrade.Id);
            Assert.AreEqual(kind, upgrade.Kind);
            Assert.AreEqual(Amount, upgrade.Amount);
        }

        [TestCase(BonusKind.Damage)]
        [TestCase(BonusKind.Heal)]
        [TestCase(BonusKind.Shield)]
        public void ToUpgrade_EffectAmount_CopiesEffectKind(BonusKind effectKind)
        {
            Author(Id, (int)PassiveUpgradeKind.EffectAmount, (int)effectKind, Amount);

            var upgrade = _asset.ToUpgrade();

            Assert.AreEqual(PassiveUpgradeKind.EffectAmount, upgrade.Kind);
            Assert.AreEqual(effectKind, upgrade.EffectKind);
            Assert.AreEqual(Amount, upgrade.Amount);
        }

        [Test]
        public void ToUpgrade_MissingId_ThrowsNamingTheAsset()
        {
            Author(string.Empty, (int)PassiveUpgradeKind.MaxHealth, 0, Amount);

            var exception = Assert.Throws<InvalidOperationException>(() => _asset.ToUpgrade());
            StringAssert.Contains(_asset.name, exception.Message);
        }

        [Test]
        public void ToUpgrade_NegativeAmount_ThrowsNamingTheAsset()
        {
            Author(Id, (int)PassiveUpgradeKind.MaxHealth, 0, -1);

            var exception = Assert.Throws<InvalidOperationException>(() => _asset.ToUpgrade());
            StringAssert.Contains(_asset.name, exception.Message);
        }

        [Test]
        public void ToUpgrade_UnknownKind_ThrowsNamingTheAsset()
        {
            Author(Id, 99, 0, Amount);

            var exception = Assert.Throws<InvalidOperationException>(() => _asset.ToUpgrade());
            StringAssert.Contains(_asset.name, exception.Message);
        }

        [Test]
        public void IsPlaceholder_ReadsTheFlag()
        {
            Author(Id, (int)PassiveUpgradeKind.MaxHealth, 0, Amount, isPlaceholder: true);

            Assert.IsTrue(_asset.IsPlaceholder);
        }
    }
}
