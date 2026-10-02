using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Effects;
using NUnit.Framework;

namespace Game.Core.Tests.Cards
{
    public class NeighbourModifierTests
    {
        // Placeholder id and arbitrary test data, not real content or balance values.
        private const string Id = "test_card_01";
        private const int CastTime = 2;
        private const int Amount = 3;

        [Test]
        public void Constructor_ValidValues_StoresKindDirectionAndAmount()
        {
            var modifier = new NeighbourModifier(BonusKind.Heal, NeighbourDirection.Previous, Amount);

            Assert.AreEqual(
                (BonusKind.Heal, NeighbourDirection.Previous, Amount),
                (modifier.Kind, modifier.Direction, modifier.Amount));
        }

        [Test]
        public void Constructor_ZeroAmount_IsAllowed()
        {
            Assert.AreEqual(0, new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, 0).Amount);
        }

        [Test]
        public void Constructor_NegativeAmount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, -1));
        }

        [Test]
        public void Constructor_UnknownKind_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new NeighbourModifier((BonusKind)99, NeighbourDirection.Next, Amount));
        }

        [Test]
        public void Constructor_UnknownDirection_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new NeighbourModifier(BonusKind.Damage, (NeighbourDirection)99, Amount));
        }

        [TestCase(BonusKind.Damage, Amount, 0, 0)]
        [TestCase(BonusKind.Heal, 0, Amount, 0)]
        [TestCase(BonusKind.Shield, 0, 0, Amount)]
        public void ToBonus_Kind_HoldsAmountForThatKindOnly(BonusKind kind, int damage, int heal, int shield)
        {
            var modifier = new NeighbourModifier(kind, NeighbourDirection.Next, Amount);

            Assert.AreEqual(new EffectBonus(damage, heal, shield), modifier.ToBonus());
        }

        // --- On CardDefinition ---

        [Test]
        public void CardDefinition_WithoutModifiers_HasNone()
        {
            var card = new CardDefinition(Id, CastTime, new IEffect[0]);

            Assert.IsEmpty(card.NeighbourModifiers);
        }

        [Test]
        public void CardDefinition_WithModifiers_StoresThemInOrder()
        {
            var first = new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, 1);
            var second = new NeighbourModifier(BonusKind.Shield, NeighbourDirection.Previous, 2);

            var card = new CardDefinition(Id, CastTime, new IEffect[0], new[] { first, second });

            CollectionAssert.AreEqual(new[] { first, second }, card.NeighbourModifiers);
        }

        [Test]
        public void CardDefinition_ModifierSourceChangedAfterwards_ModifiersUnchanged()
        {
            var source = new List<NeighbourModifier> { new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, 1) };
            var card = new CardDefinition(Id, CastTime, new IEffect[0], source);

            source.Add(new NeighbourModifier(BonusKind.Heal, NeighbourDirection.Next, 1));

            Assert.AreEqual(1, card.NeighbourModifiers.Count);
        }

        [Test]
        public void CardDefinition_NullModifiers_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CardDefinition(Id, CastTime, new IEffect[0], null));
        }

        [Test]
        public void CardDefinition_NullModifierItem_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new CardDefinition(Id, CastTime, new IEffect[0], new NeighbourModifier[] { null }));
        }
    }
}
