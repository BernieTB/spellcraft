using System;
using Game.Core.Effects;
using NUnit.Framework;

namespace Game.Core.Tests.Effects
{
    public class EffectBonusTests
    {
        // Arbitrary test data, not balance values.

        [Test]
        public void Constructor_ValidValues_StoresEachValue()
        {
            var bonus = new EffectBonus(1, 2, 3);

            Assert.AreEqual((1, 2, 3), (bonus.Damage, bonus.Heal, bonus.Shield));
        }

        [TestCase(-1, 0, 0)]
        [TestCase(0, -1, 0)]
        [TestCase(0, 0, -1)]
        public void Constructor_NegativeValue_Throws(int damage, int heal, int shield)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EffectBonus(damage, heal, shield));
        }

        [Test]
        public void None_IsAllZero()
        {
            Assert.AreEqual(new EffectBonus(0, 0, 0), EffectBonus.None);
        }

        [Test]
        public void IsNone_AllZero_IsTrue()
        {
            Assert.IsTrue(EffectBonus.None.IsNone);
        }

        [TestCase(1, 0, 0)]
        [TestCase(0, 1, 0)]
        [TestCase(0, 0, 1)]
        public void IsNone_AnyValueAboveZero_IsFalse(int damage, int heal, int shield)
        {
            Assert.IsFalse(new EffectBonus(damage, heal, shield).IsNone);
        }

        [TestCase(BonusKind.Damage, 4, 0, 0)]
        [TestCase(BonusKind.Heal, 0, 4, 0)]
        [TestCase(BonusKind.Shield, 0, 0, 4)]
        public void Of_Kind_SetsOnlyThatValue(BonusKind kind, int damage, int heal, int shield)
        {
            Assert.AreEqual(new EffectBonus(damage, heal, shield), EffectBonus.Of(kind, 4));
        }

        [Test]
        public void Of_UnknownKind_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EffectBonus.Of((BonusKind)99, 1));
        }

        [Test]
        public void Of_NegativeAmount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EffectBonus.Of(BonusKind.Damage, -1));
        }

        [TestCase(BonusKind.Damage, 1)]
        [TestCase(BonusKind.Heal, 2)]
        [TestCase(BonusKind.Shield, 3)]
        public void Get_Kind_ReturnsThatValue(BonusKind kind, int expected)
        {
            Assert.AreEqual(expected, new EffectBonus(1, 2, 3).Get(kind));
        }

        [Test]
        public void Get_UnknownKind_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EffectBonus(1, 2, 3).Get((BonusKind)99));
        }

        [TestCase(BonusKind.Damage, 0, 2, 3)]
        [TestCase(BonusKind.Heal, 1, 0, 3)]
        [TestCase(BonusKind.Shield, 1, 2, 0)]
        public void Without_Kind_ZeroesOnlyThatValue(BonusKind kind, int damage, int heal, int shield)
        {
            Assert.AreEqual(new EffectBonus(damage, heal, shield), new EffectBonus(1, 2, 3).Without(kind));
        }

        [Test]
        public void Plus_SumsEachValue()
        {
            Assert.AreEqual(new EffectBonus(5, 7, 9), new EffectBonus(1, 2, 3).Plus(new EffectBonus(4, 5, 6)));
        }

        [Test]
        public void Plus_Overflow_Throws()
        {
            Assert.Throws<OverflowException>(() => new EffectBonus(int.MaxValue, 0, 0).Plus(new EffectBonus(1, 0, 0)));
        }
    }
}
