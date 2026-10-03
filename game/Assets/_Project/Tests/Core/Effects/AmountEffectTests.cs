using System;
using Game.Core.Effects;
using NUnit.Framework;

namespace Game.Core.Tests.Effects
{
    public class AmountEffectTests
    {
        // Arbitrary test data, not balance values.
        private const int Amount = 4;
        private const int NewAmount = 9;

        private static IAmountEffect Create(BonusKind kind)
        {
            switch (kind)
            {
                case BonusKind.Damage:
                    return new DealDamageEffect(Amount);
                case BonusKind.Heal:
                    return new HealEffect(Amount);
                default:
                    return new GainShieldEffect(Amount);
            }
        }

        [TestCase(BonusKind.Damage)]
        [TestCase(BonusKind.Heal)]
        [TestCase(BonusKind.Shield)]
        public void Kind_MatchesTheBonusTheEffectConsumes(BonusKind kind)
        {
            Assert.AreEqual(kind, Create(kind).Kind);
        }

        [TestCase(BonusKind.Damage)]
        [TestCase(BonusKind.Heal)]
        [TestCase(BonusKind.Shield)]
        public void WithAmount_ReturnsSameEffectTypeWithNewAmount(BonusKind kind)
        {
            var original = Create(kind);

            var changed = (IAmountEffect)original.WithAmount(NewAmount);

            Assert.AreEqual(original.GetType(), changed.GetType());
            Assert.AreEqual(NewAmount, changed.Amount);
            Assert.AreEqual(Amount, original.Amount);
        }

        [TestCase(BonusKind.Damage)]
        [TestCase(BonusKind.Heal)]
        [TestCase(BonusKind.Shield)]
        public void WithAmount_Negative_Throws(BonusKind kind)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(kind).WithAmount(-1));
        }
    }
}
