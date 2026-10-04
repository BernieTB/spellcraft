using System;
using System.Collections.Generic;
using Game.Core.SpellLines;
using NUnit.Framework;

namespace Game.Core.Tests.SpellLines
{
    public class SpellLineTests
    {
        // Placeholder cards and test capacities: not game content.
        private const string CardA = "TestCard1";
        private const string CardB = "TestCard2";
        private const string CardC = "TestCard3";
        private const string CardD = "TestCard4";

        private static SpellLine<string> CreateLine(int capacity, params string[] cards)
        {
            var line = new SpellLine<string>(capacity);
            foreach (var card in cards)
            {
                line.Add(card);
            }

            return line;
        }

        /// <summary>
        /// Plays the line from the first position, following <see cref="SpellLine{TCard}.PositionAfter"/>.
        /// </summary>
        private static List<string> Play(SpellLine<string> line, int times)
        {
            var played = new List<string>();
            var position = 0;
            for (var i = 0; i < times; i++)
            {
                played.Add(line[position]);
                position = line.PositionAfter(position);
            }

            return played;
        }

        // --- Construction ---

        [Test]
        public void Constructor_ValidCapacity_IsEmptyWithThatCapacity()
        {
            var line = new SpellLine<string>(3);

            Assert.AreEqual(3, line.Capacity);
            Assert.AreEqual(0, line.Count);
            Assert.IsTrue(line.IsEmpty);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_CapacityBelowOne_Throws(int capacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SpellLine<string>(capacity));
        }

        // --- Looping order ---

        [Test]
        public void PositionAfter_PlayedRepeatedly_LoopsThroughCardsInOrder()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            CollectionAssert.AreEqual(new[] { CardA, CardB, CardC, CardA, CardB, CardC, CardA }, Play(line, 7));
        }

        [Test]
        public void PositionAfter_MiddlePosition_ReturnsNextPosition()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            Assert.AreEqual(2, line.PositionAfter(1));
        }

        [Test]
        public void PositionAfter_LastPosition_ReturnsFirst()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            Assert.AreEqual(0, line.PositionAfter(2));
        }

        [Test]
        public void PositionAfter_SingleCard_ReturnsSamePosition()
        {
            var line = CreateLine(3, CardA);

            Assert.AreEqual(0, line.PositionAfter(0));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void PositionAfter_InvalidPosition_Throws(int position)
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            Assert.Throws<ArgumentOutOfRangeException>(() => line.PositionAfter(position));
        }

        [Test]
        public void PositionAfter_EmptyLine_Throws()
        {
            var line = new SpellLine<string>(3);

            Assert.Throws<ArgumentOutOfRangeException>(() => line.PositionAfter(0));
        }

        [Test]
        public void PositionBefore_MiddlePosition_ReturnsPreviousPosition()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            Assert.AreEqual(0, line.PositionBefore(1));
        }

        [Test]
        public void PositionBefore_FirstPosition_ReturnsLast()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            Assert.AreEqual(2, line.PositionBefore(0));
        }

        [Test]
        public void PositionBefore_SingleCard_ReturnsSamePosition()
        {
            var line = CreateLine(3, CardA);

            Assert.AreEqual(0, line.PositionBefore(0));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void PositionBefore_InvalidPosition_Throws(int position)
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            Assert.Throws<ArgumentOutOfRangeException>(() => line.PositionBefore(position));
        }

        // --- Adding and capacity ---

        [Test]
        public void Add_NotFull_AppendsAfterLastCard()
        {
            var line = CreateLine(3, CardA, CardB);

            line.Add(CardC);

            CollectionAssert.AreEqual(new[] { CardA, CardB, CardC }, line.Cards);
        }

        [Test]
        public void Add_UpToCapacity_IsFull()
        {
            var line = CreateLine(2, CardA, CardB);

            Assert.IsTrue(line.IsFull);
        }

        [Test]
        public void Add_LineFull_Throws()
        {
            var line = CreateLine(2, CardA, CardB);

            Assert.Throws<InvalidOperationException>(() => line.Add(CardC));
        }

        [Test]
        public void Add_LineFull_LeavesCardsUnchanged()
        {
            var line = CreateLine(2, CardA, CardB);

            Assert.Catch(() => line.Add(CardC));

            CollectionAssert.AreEqual(new[] { CardA, CardB }, line.Cards);
        }

        [Test]
        public void Add_NullCard_Throws()
        {
            var line = new SpellLine<string>(2);

            Assert.Throws<ArgumentNullException>(() => line.Add(null));
        }

        [Test]
        public void Add_SameCardTwice_KeepsBothPositions()
        {
            var line = CreateLine(3, CardA, CardA);

            CollectionAssert.AreEqual(new[] { CardA, CardA }, line.Cards);
        }

        [Test]
        public void IncreaseCapacity_AddsSlotsAndKeepsCards()
        {
            var line = CreateLine(2, CardA, CardB);

            line.IncreaseCapacity(2);

            Assert.AreEqual(4, line.Capacity);
            Assert.IsFalse(line.IsFull);
            CollectionAssert.AreEqual(new[] { CardA, CardB }, line.Cards);
        }

        [Test]
        public void IncreaseCapacity_CardsViewCapturedBefore_SeesCardsAddedAfter()
        {
            var line = CreateLine(1, CardA);
            var view = line.Cards;

            line.IncreaseCapacity(1);
            line.Add(CardB);

            CollectionAssert.AreEqual(new[] { CardA, CardB }, view);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void IncreaseCapacity_BelowOne_Throws(int slots)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateLine(1, CardA).IncreaseCapacity(slots));
        }

        // --- Removing ---

        [Test]
        public void RemoveAt_ValidPosition_ReturnsRemovedCard()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            Assert.AreEqual(CardB, line.RemoveAt(1));
        }

        [Test]
        public void RemoveAt_ValidPosition_ShiftsFollowingCards()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            line.RemoveAt(1);

            CollectionAssert.AreEqual(new[] { CardA, CardC }, line.Cards);
        }

        [Test]
        public void RemoveAt_FullLine_FreesRoomForAdd()
        {
            var line = CreateLine(2, CardA, CardB);

            line.RemoveAt(0);
            line.Add(CardC);

            CollectionAssert.AreEqual(new[] { CardB, CardC }, line.Cards);
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void RemoveAt_InvalidPosition_Throws(int position)
        {
            var line = CreateLine(3, CardA, CardB);

            Assert.Throws<ArgumentOutOfRangeException>(() => line.RemoveAt(position));
        }

        [Test]
        public void RemoveAt_EmptyLine_Throws()
        {
            var line = new SpellLine<string>(3);

            Assert.Throws<ArgumentOutOfRangeException>(() => line.RemoveAt(0));
        }

        // --- Swapping ---

        [Test]
        public void Swap_TwoPositions_ExchangesCards()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            line.Swap(0, 2);

            CollectionAssert.AreEqual(new[] { CardC, CardB, CardA }, line.Cards);
        }

        [Test]
        public void Swap_SamePosition_LeavesCardsUnchanged()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            line.Swap(1, 1);

            CollectionAssert.AreEqual(new[] { CardA, CardB, CardC }, line.Cards);
        }

        [TestCase(-1, 0)]
        [TestCase(0, 3)]
        public void Swap_InvalidPosition_Throws(int firstPosition, int secondPosition)
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            Assert.Throws<ArgumentOutOfRangeException>(() => line.Swap(firstPosition, secondPosition));
        }

        [Test]
        public void Swap_InvalidPosition_LeavesCardsUnchanged()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            Assert.Catch(() => line.Swap(0, 3));

            CollectionAssert.AreEqual(new[] { CardA, CardB, CardC }, line.Cards);
        }

        // --- Moving ---

        [Test]
        public void Move_ForwardPosition_ShiftsCardsInBetweenBack()
        {
            var line = CreateLine(4, CardA, CardB, CardC, CardD);

            line.Move(0, 2);

            CollectionAssert.AreEqual(new[] { CardB, CardC, CardA, CardD }, line.Cards);
        }

        [Test]
        public void Move_BackwardPosition_ShiftsCardsInBetweenForward()
        {
            var line = CreateLine(4, CardA, CardB, CardC, CardD);

            line.Move(3, 1);

            CollectionAssert.AreEqual(new[] { CardA, CardD, CardB, CardC }, line.Cards);
        }

        [Test]
        public void Move_SamePosition_LeavesCardsUnchanged()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            line.Move(1, 1);

            CollectionAssert.AreEqual(new[] { CardA, CardB, CardC }, line.Cards);
        }

        [Test]
        public void Move_LastToFirst_ChangesLoopingOrder()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            line.Move(2, 0);

            CollectionAssert.AreEqual(new[] { CardC, CardA, CardB, CardC }, Play(line, 4));
        }

        [TestCase(-1, 0)]
        [TestCase(0, 3)]
        public void Move_InvalidPosition_Throws(int fromPosition, int toPosition)
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            Assert.Throws<ArgumentOutOfRangeException>(() => line.Move(fromPosition, toPosition));
        }

        [Test]
        public void Move_InvalidPosition_LeavesCardsUnchanged()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            Assert.Catch(() => line.Move(0, 3));

            CollectionAssert.AreEqual(new[] { CardA, CardB, CardC }, line.Cards);
        }

        // --- Read access ---

        [Test]
        public void Indexer_ValidPosition_ReturnsCard()
        {
            var line = CreateLine(3, CardA, CardB);

            Assert.AreEqual(CardB, line[1]);
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void Indexer_InvalidPosition_Throws(int position)
        {
            var line = CreateLine(3, CardA, CardB);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = line[position]);
        }

        [Test]
        public void Cards_AfterChange_ReflectsCurrentOrder()
        {
            var line = CreateLine(3, CardA, CardB);
            var cards = line.Cards;

            line.Swap(0, 1);

            CollectionAssert.AreEqual(new[] { CardB, CardA }, cards);
        }

        [Test]
        public void Cards_CastToList_CannotBeModified()
        {
            var line = CreateLine(3, CardA);

            var cards = (IList<string>)line.Cards;

            Assert.Throws<NotSupportedException>(() => cards.Add(CardB));
        }
    }
}
