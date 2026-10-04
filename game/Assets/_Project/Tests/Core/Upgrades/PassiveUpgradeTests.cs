using System;
using Game.Core.Effects;
using Game.Core.Upgrades;
using NUnit.Framework;

namespace Game.Core.Tests.Upgrades
{
    public class PassiveUpgradeTests
    {
        // Placeholder id and arbitrary test data, not real content or balance values.
        private const string Id = "test_upgrade_01";
        private const int Amount = 3;

        [Test]
        public void Constructor_EffectAmount_StoresEveryValue()
        {
            var upgrade = new PassiveUpgrade(Id, PassiveUpgradeKind.EffectAmount, BonusKind.Heal, Amount);

            Assert.AreEqual(Id, upgrade.Id);
            Assert.AreEqual(PassiveUpgradeKind.EffectAmount, upgrade.Kind);
            Assert.AreEqual(BonusKind.Heal, upgrade.EffectKind);
            Assert.AreEqual(Amount, upgrade.Amount);
        }

        [Test]
        public void Constructor_KindWithoutEffectKind_StoresKindAndAmount()
        {
            var upgrade = new PassiveUpgrade(Id, PassiveUpgradeKind.MaxHealth, Amount);

            Assert.AreEqual(PassiveUpgradeKind.MaxHealth, upgrade.Kind);
            Assert.AreEqual(Amount, upgrade.Amount);
        }

        [Test]
        public void Constructor_EffectAmountWithoutEffectKind_Throws()
        {
            Assert.Throws<ArgumentException>(() => new PassiveUpgrade(Id, PassiveUpgradeKind.EffectAmount, Amount));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_MissingId_Throws(string id)
        {
            Assert.Throws<ArgumentException>(() => new PassiveUpgrade(id, PassiveUpgradeKind.MaxHealth, Amount));
        }

        [Test]
        public void Constructor_NegativeAmount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PassiveUpgrade(Id, PassiveUpgradeKind.MaxHealth, -1));
        }

        [Test]
        public void Constructor_UnknownKind_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PassiveUpgrade(Id, (PassiveUpgradeKind)99, Amount));
        }

        [Test]
        public void Constructor_UnknownEffectKind_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PassiveUpgrade(Id, PassiveUpgradeKind.EffectAmount, (BonusKind)99, Amount));
        }
    }
}
