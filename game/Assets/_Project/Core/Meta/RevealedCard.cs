using System;

namespace Game.Core.Meta
{
    /// <summary>
    /// A card of a professor's spell line that the player knows: its position in the line and the id of the card
    /// that was there when it was revealed.
    /// </summary>
    /// <remarks>
    /// The position tells two copies of the same card apart; the card id lets the bestiary ignore a reveal that no
    /// longer matches the professor's data (the line was changed after the save).
    /// </remarks>
    public readonly struct RevealedCard : IEquatable<RevealedCard>
    {
        /// <param name="position">Zero-based position in the professor's spell line. Zero or more.</param>
        /// <param name="cardId">Id of the card at that position. Not empty or whitespace.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="position"/> is negative.</exception>
        /// <exception cref="ArgumentException"><paramref name="cardId"/> is null, empty or whitespace.</exception>
        public RevealedCard(int position, string cardId)
        {
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), position, "A card position cannot be negative.");
            }

            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("A card id cannot be null, empty or whitespace.", nameof(cardId));
            }

            Position = position;
            CardId = cardId;
        }

        /// <summary>Zero-based position in the professor's spell line.</summary>
        public int Position { get; }

        /// <summary>Id of the card at that position when it was revealed.</summary>
        public string CardId { get; }

        /// <inheritdoc />
        public bool Equals(RevealedCard other)
        {
            return Position == other.Position && string.Equals(CardId, other.CardId, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is RevealedCard other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Position * 397) ^ (CardId == null ? 0 : StringComparer.Ordinal.GetHashCode(CardId));
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Position + ":" + CardId;
        }
    }
}
