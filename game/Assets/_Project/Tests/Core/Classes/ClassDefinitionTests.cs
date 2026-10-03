using System;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Effects;
using NUnit.Framework;

namespace Game.Core.Tests.Classes
{
    public class ClassDefinitionTests
    {
        // Placeholder ids and test values: not game content.
        private static readonly CardDefinition CardA = new CardDefinition("TestCard1", 1, new IEffect[] { new DealDamageEffect(1) });
        private static readonly CardDefinition CardB = new CardDefinition("TestCard2", 2, new IEffect[] { new GainShieldEffect(1) });

        private static ClassDefinition Create(
            string id = "TestClass",
            int maxHealth = 20,
            int startingShield = 0,
            int capacity = 4,
            CardDefinition[] deck = null,
            CardDefinition[] pool = null)
        {
            return new ClassDefinition(
                id, maxHealth, startingShield, capacity, deck ?? new[] { CardA, CardB }, pool ?? new[] { CardA });
        }

        [Test]
        public void Constructor_ValidData_KeepsValues()
        {
            var definition = Create(maxHealth: 25, startingShield: 3, capacity: 5, pool: new[] { CardB, CardB });

            Assert.AreEqual("TestClass", definition.Id);
            Assert.AreEqual(25, definition.MaxHealth);
            Assert.AreEqual(3, definition.StartingShield);
            Assert.AreEqual(5, definition.StartingLineCapacity);
            CollectionAssert.AreEqual(new[] { CardA, CardB }, definition.StartingDeck);
            CollectionAssert.AreEqual(new[] { CardB, CardB }, definition.CardPool);
        }

        [Test]
        public void Constructor_DeckFillsCapacity_IsAccepted()
        {
            Assert.DoesNotThrow(() => Create(capacity: 2));
        }

        [Test]
        public void Constructor_EmptyCardPool_IsAccepted()
        {
            Assert.IsEmpty(Create(pool: new CardDefinition[0]).CardPool);
        }

        [Test]
        public void Constructor_CopiesTheCardLists()
        {
            var deck = new[] { CardA };
            var definition = Create(deck: deck);

            deck[0] = CardB;

            Assert.AreSame(CardA, definition.StartingDeck[0]);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void Constructor_BlankId_Throws(string id)
        {
            Assert.Throws<ArgumentException>(() => Create(id: id));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_MaxHealthNotPositive_Throws(int maxHealth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(maxHealth: maxHealth));
        }

        [Test]
        public void Constructor_NegativeStartingShield_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(startingShield: -1));
        }

        [Test]
        public void Constructor_CapacityBelowOne_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(capacity: 0, deck: new[] { CardA }));
        }

        [Test]
        public void Constructor_EmptyDeck_Throws()
        {
            Assert.Throws<ArgumentException>(() => Create(deck: new CardDefinition[0]));
        }

        [Test]
        public void Constructor_DeckLargerThanCapacity_Throws()
        {
            Assert.Throws<ArgumentException>(() => Create(capacity: 1));
        }

        [Test]
        public void Constructor_NullCardInDeck_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Create(deck: new[] { CardA, null }));
        }

        [Test]
        public void Constructor_NullCardInPool_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Create(pool: new CardDefinition[] { null }));
        }

        [Test]
        public void Constructor_NullLists_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new ClassDefinition("TestClass", 20, 0, 4, null, new[] { CardA }));
            Assert.Throws<ArgumentNullException>(() => new ClassDefinition("TestClass", 20, 0, 4, new[] { CardA }, null));
        }
    }
}
