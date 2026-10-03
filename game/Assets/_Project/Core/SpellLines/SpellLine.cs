using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Game.Core.SpellLines
{
    /// <summary>
    /// The player's spell line: an ordered sequence of cards, with a maximum size, that plays in a loop.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Positions are zero-based, from the first card (0) to the last card (<see cref="Count"/> - 1).
    /// </para>
    /// <para>
    /// The line loops: <see cref="PositionAfter"/> goes from the last card back to the first and
    /// <see cref="PositionBefore"/> from the first card to the last. In a single-card line both return the same
    /// position. The line holds no playing cursor: which card is being cast is fight state, kept by the combat
    /// loop, so editing a line between fights never carries over a position from an earlier fight.
    /// </para>
    /// <para>
    /// The line knows nothing about timing or neighbour effects. It is generic over the card type so it does not
    /// depend on how cards are defined. The same card may sit at several positions.
    /// </para>
    /// </remarks>
    /// <typeparam name="TCard">The card type. Null cards are rejected.</typeparam>
    public sealed class SpellLine<TCard>
    {
        private readonly List<TCard> _cards;
        private readonly ReadOnlyCollection<TCard> _readOnlyCards;

        /// <summary>
        /// Creates an empty spell line.
        /// </summary>
        /// <param name="capacity">Maximum number of cards, read from data. Must be at least 1.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 1.</exception>
        public SpellLine(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be at least 1.");
            }

            Capacity = capacity;
            _cards = new List<TCard>(capacity);
            _readOnlyCards = _cards.AsReadOnly();
        }

        /// <summary>Maximum number of cards the line can hold.</summary>
        public int Capacity { get; private set; }

        /// <summary>Number of cards in the line.</summary>
        public int Count => _cards.Count;

        /// <summary>True when the line holds no card.</summary>
        public bool IsEmpty => _cards.Count == 0;

        /// <summary>True when the line holds <see cref="Capacity"/> cards.</summary>
        public bool IsFull => _cards.Count == Capacity;

        /// <summary>The cards in order, from the first position to the last. Read-only live view.</summary>
        public IReadOnlyList<TCard> Cards => _readOnlyCards;

        /// <summary>The card at <paramref name="position"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is not a valid position.</exception>
        public TCard this[int position]
        {
            get
            {
                ValidatePosition(position, nameof(position));
                return _cards[position];
            }
        }

        /// <summary>
        /// The position played after <paramref name="position"/>: the next one, or the first after the last.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is not a valid position.</exception>
        public int PositionAfter(int position)
        {
            ValidatePosition(position, nameof(position));
            return (position + 1) % _cards.Count;
        }

        /// <summary>
        /// The position played before <paramref name="position"/>: the previous one, or the last before the first.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is not a valid position.</exception>
        public int PositionBefore(int position)
        {
            ValidatePosition(position, nameof(position));
            return (position + _cards.Count - 1) % _cards.Count;
        }

        /// <summary>
        /// Adds slots to the line. The cards and their order do not change, and <see cref="Cards"/> stays the same
        /// live view.
        /// </summary>
        /// <param name="slots">Number of slots to add. At least 1.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="slots"/> is less than 1.</exception>
        public void IncreaseCapacity(int slots)
        {
            if (slots < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(slots), slots, "Add at least one slot.");
            }

            Capacity = checked(Capacity + slots);
        }

        /// <summary>Adds a card after the last position.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The line is full.</exception>
        public void Add(TCard card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (IsFull)
            {
                throw new InvalidOperationException($"The spell line is full (capacity {Capacity}).");
            }

            _cards.Add(card);
        }

        /// <summary>Removes the card at <paramref name="position"/>; the following cards move up one position.</summary>
        /// <returns>The removed card.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is not a valid position.</exception>
        public TCard RemoveAt(int position)
        {
            ValidatePosition(position, nameof(position));
            var card = _cards[position];
            _cards.RemoveAt(position);
            return card;
        }

        /// <summary>Exchanges the cards at two positions. Swapping a position with itself does nothing.</summary>
        /// <exception cref="ArgumentOutOfRangeException">A position is not valid.</exception>
        public void Swap(int firstPosition, int secondPosition)
        {
            ValidatePosition(firstPosition, nameof(firstPosition));
            ValidatePosition(secondPosition, nameof(secondPosition));
            var first = _cards[firstPosition];
            _cards[firstPosition] = _cards[secondPosition];
            _cards[secondPosition] = first;
        }

        /// <summary>
        /// Moves the card at <paramref name="fromPosition"/> to <paramref name="toPosition"/>, shifting the cards
        /// in between by one position. Keeps every other card in the same relative order.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">A position is not valid.</exception>
        public void Move(int fromPosition, int toPosition)
        {
            ValidatePosition(fromPosition, nameof(fromPosition));
            ValidatePosition(toPosition, nameof(toPosition));
            var card = _cards[fromPosition];
            _cards.RemoveAt(fromPosition);
            _cards.Insert(toPosition, card);
        }

        private void ValidatePosition(int position, string paramName)
        {
            if (position < 0 || position >= _cards.Count)
            {
                throw new ArgumentOutOfRangeException(
                    paramName, position, $"Position is outside the spell line ({_cards.Count} cards).");
            }
        }
    }
}
