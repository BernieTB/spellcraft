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

        private static List<string> Play(SpellLine<string> line, int times)
        {
            var played = new List<string>();
            for (var i = 0; i < times; i++)
            {
                played.Add(line.Next());
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
        public void Next_SeveralCards_ReturnsCardsInOrder()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            CollectionAssert.AreEqual(new[] { CardA, CardB, CardC }, Play(line, 3));
        }

        [Test]
        public void Next_AfterLastCard_LoopsBackToFirst()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            CollectionAssert.AreEqual(new[] { CardA, CardB, CardC, CardA, CardB, CardC, CardA }, Play(line, 7));
        }

        [Test]
        public void Next_SingleCard_AlwaysReturnsThatCard()
        {
            var line = CreateLine(3, CardA);

            CollectionAssert.AreEqual(new[] { CardA, CardA, CardA }, Play(line, 3));
        }

        [Test]
        public void Next_EmptyLine_Throws()
        {
            var line = new SpellLine<string>(3);

            Assert.Throws<InvalidOperationException>(() => line.Next());
        }

        [Test]
        public void Next_SameCardsInTwoLines_GivesSameSequence()
        {
            var first = CreateLine(4, CardA, CardB, CardC);
            var second = CreateLine(4, CardA, CardB, CardC);

            CollectionAssert.AreEqual(Play(first, 10), Play(second, 10));
        }

        [Test]
        public void NextPosition_AfterLastCard_IsZero()
        {
            var line = CreateLine(2, CardA, CardB);
            Play(line, 2);

            Assert.AreEqual(0, line.NextPosition);
        }

        [Test]
        public void NextPosition_EmptyLine_IsZero()
        {
            var line = new SpellLine<string>(3);

            Assert.AreEqual(0, line.NextPosition);
        }

        [Test]
        public void ResetCursor_MidLoop_NextReturnsFirstCard()
        {
            var line = CreateLine(3, CardA, CardB, CardC);
            Play(line, 2);

            line.ResetCursor();

            Assert.AreEqual(CardA, line.Next());
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
        public void TryAdd_NotFull_ReturnsTrueAndAdds()
        {
            var line = new SpellLine<string>(1);

            Assert.IsTrue(line.TryAdd(CardA));
            Assert.AreEqual(CardA, line[0]);
        }

        [Test]
        public void TryAdd_LineFull_ReturnsFalse()
        {
            var line = CreateLine(1, CardA);

            Assert.IsFalse(line.TryAdd(CardB));
        }

        [Test]
        public void TryAdd_NullCard_Throws()
        {
            var line = new SpellLine<string>(1);

            Assert.Throws<ArgumentNullException>(() => line.TryAdd(null));
        }

        [Test]
        public void Add_DuringLoop_NewCardPlaysAfterLastCard()
        {
            var line = CreateLine(3, CardA, CardB);
            Play(line, 1);

            line.Add(CardC);

            CollectionAssert.AreEqual(new[] { CardB, CardC, CardA }, Play(line, 3));
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

            Assert.IsTrue(line.TryAdd(CardC));
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

        [Test]
        public void RemoveAt_LastCardWhenCursorOnIt_CursorGoesBackToFirst()
        {
            var line = CreateLine(3, CardA, CardB, CardC);
            Play(line, 2);

            line.RemoveAt(2);

            Assert.AreEqual(CardA, line.Next());
        }

        [Test]
        public void RemoveAt_OnlyCard_LeavesEmptyLineWithCursorAtZero()
        {
            var line = CreateLine(3, CardA);

            line.RemoveAt(0);

            Assert.IsTrue(line.IsEmpty);
            Assert.AreEqual(0, line.NextPosition);
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

        [Test]
        public void Swap_TwoPositions_ChangesLoopingOrder()
        {
            var line = CreateLine(3, CardA, CardB, CardC);

            line.Swap(0, 1);

            CollectionAssert.AreEqual(new[] { CardB, CardA, CardC, CardB }, Play(line, 4));
        }

        [Test]
        public void Swap_DuringLoop_CursorStaysOnSamePosition()
        {
            var line = CreateLine(3, CardA, CardB, CardC);
            Play(line, 1);

            line.Swap(1, 2);

            Assert.AreEqual(CardC, line.Next());
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

        // --- Clearing ---

        [Test]
        public void Clear_WithCards_EmptiesLine()
        {
            var line = CreateLine(3, CardA, CardB);

            line.Clear();

            Assert.IsTrue(line.IsEmpty);
        }

        [Test]
        public void Clear_MidLoop_ResetsCursor()
        {
            var line = CreateLine(3, CardA, CardB);
            Play(line, 1);

            line.Clear();

            Assert.AreEqual(0, line.NextPosition);
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
