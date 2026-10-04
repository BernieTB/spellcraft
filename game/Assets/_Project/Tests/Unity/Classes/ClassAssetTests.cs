using System;
using System.Linq;
using Game.Unity.Cards;
using Game.Unity.Tests.Enemies;
using NUnit.Framework;

namespace Game.Unity.Tests.Classes
{
    /// <summary>Conversion of <see cref="Game.Unity.Classes.ClassAsset"/> to Core. Ids and numbers are arbitrary test data.</summary>
    public class ClassAssetTests
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
        public void ToDefinition_ValidAsset_CopiesIdAndStats()
        {
            var asset = _assets.Class("test_class", 30, 2, 4, new[] { _assets.Card("test_card_a") }, new CardAsset[0]);

            var definition = asset.ToDefinition();

            Assert.AreEqual("test_class", definition.Id);
            Assert.AreEqual(30, definition.MaxHealth);
            Assert.AreEqual(2, definition.StartingShield);
            Assert.AreEqual(4, definition.StartingLineCapacity);
        }

        [Test]
        public void ToDefinition_ValidAsset_CopiesStartingDeckInOrder()
        {
            var asset = _assets.Class("test_class", 30, 0, 4,
                new[] { _assets.Card("test_card_a"), _assets.Card("test_card_b"), _assets.Card("test_card_c") },
                new CardAsset[0]);

            var deck = asset.ToDefinition().StartingDeck;

            CollectionAssert.AreEqual(new[] { "test_card_a", "test_card_b", "test_card_c" }, deck.Select(c => c.Id));
        }

        [Test]
        public void ToDefinition_ValidAsset_CopiesCardPoolWithDuplicates()
        {
            var pooled = _assets.Card("test_card_p");
            var asset = _assets.Class("test_class", 30, 0, 4, new[] { _assets.Card("test_card_a") },
                new[] { pooled, _assets.Card("test_card_q"), pooled });

            var pool = asset.ToDefinition().CardPool;

            CollectionAssert.AreEqual(new[] { "test_card_p", "test_card_q", "test_card_p" }, pool.Select(c => c.Id));
        }

        [Test]
        public void ToDefinition_EmptyCardPool_IsAllowed()
        {
            var asset = _assets.Class("test_class", 30, 0, 4, new[] { _assets.Card("test_card_a") }, new CardAsset[0]);

            Assert.IsEmpty(asset.ToDefinition().CardPool);
        }

        [Test]
        public void Properties_ValidAsset_ExposeIdAndPlaceholderFlag()
        {
            var asset = _assets.Class("test_class", 30, 0, 4, new[] { _assets.Card("test_card_a") }, new CardAsset[0]);

            Assert.AreEqual("test_class", asset.Id);
            Assert.IsFalse(asset.IsPlaceholder);
        }

        [Test]
        public void IsPlaceholder_FlaggedAsset_IsTrue()
        {
            var asset = _assets.Class("test_class", 30, 0, 4, new[] { _assets.Card("test_card_a") }, new CardAsset[0],
                isPlaceholder: true);

            Assert.IsTrue(asset.IsPlaceholder);
        }

        [Test]
        public void ToDefinition_MissingId_ThrowsNamingTheAsset()
        {
            var asset = _assets.Class("", 30, 0, 4, new[] { _assets.Card("test_card_a") }, new CardAsset[0]);

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            StringAssert.Contains("no id", exception.Message);
            StringAssert.Contains("'unnamed_class'", exception.Message);
        }

        [Test]
        public void ToDefinition_EmptyStartingDeck_ThrowsNamingTheAsset()
        {
            var asset = _assets.Class("test_class", 30, 0, 4, new CardAsset[0], new CardAsset[0]);

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            StringAssert.Contains("'test_class'", exception.Message);
            StringAssert.Contains("no card", exception.Message);
        }

        [Test]
        public void ToDefinition_EmptyStartingDeckSlot_ThrowsNamingThePosition()
        {
            var asset = _assets.Class("test_class", 30, 0, 4, new[] { _assets.Card("test_card_a"), null }, new CardAsset[0]);

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            StringAssert.Contains("'test_class'", exception.Message);
            StringAssert.Contains("starting deck has an empty slot at position 1", exception.Message);
        }

        [Test]
        public void ToDefinition_EmptyCardPoolSlot_ThrowsNamingThePosition()
        {
            var asset = _assets.Class("test_class", 30, 0, 4, new[] { _assets.Card("test_card_a") },
                new[] { _assets.Card("test_card_p"), null });

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            StringAssert.Contains("'test_class'", exception.Message);
            StringAssert.Contains("card pool has an empty slot at position 1", exception.Message);
        }

        [Test]
        public void ToDefinition_StartingDeckLargerThanCapacity_ThrowsNamingTheAsset()
        {
            var asset = _assets.Class("test_class", 30, 0, 1,
                new[] { _assets.Card("test_card_a"), _assets.Card("test_card_b") }, new CardAsset[0]);

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            StringAssert.Contains("'test_class'", exception.Message);
            Assert.IsInstanceOf<ArgumentException>(exception.InnerException);
            StringAssert.Contains("starting line holds 1", exception.Message);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ToDefinition_InvalidHealth_ThrowsNamingTheAsset(int maxHealth)
        {
            var asset = _assets.Class("test_class", maxHealth, 0, 4, new[] { _assets.Card("test_card_a") }, new CardAsset[0]);

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            StringAssert.Contains("'test_class'", exception.Message);
            Assert.IsInstanceOf<ArgumentOutOfRangeException>(exception.InnerException);
            StringAssert.Contains("Max health", exception.Message);
        }

        [Test]
        public void ToDefinition_NegativeShield_ThrowsNamingTheAsset()
        {
            var asset = _assets.Class("test_class", 30, -1, 4, new[] { _assets.Card("test_card_a") }, new CardAsset[0]);

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            StringAssert.Contains("'test_class'", exception.Message);
            Assert.IsInstanceOf<ArgumentOutOfRangeException>(exception.InnerException);
            StringAssert.Contains("Starting shield", exception.Message);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ToDefinition_InvalidCapacity_ThrowsNamingTheAsset(int capacity)
        {
            var asset = _assets.Class("test_class", 30, 0, capacity, new[] { _assets.Card("test_card_a") }, new CardAsset[0]);

            var exception = Assert.Throws<InvalidOperationException>(() => asset.ToDefinition());

            StringAssert.Contains("'test_class'", exception.Message);
            Assert.IsInstanceOf<ArgumentOutOfRangeException>(exception.InnerException);
            StringAssert.Contains("Starting line capacity", exception.Message);
        }
    }
}
