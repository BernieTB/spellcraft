using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Game.Core.Meta
{
    /// <summary>
    /// What one reveal teaches the player about a professor: its health, its shield and some cards of its spell
    /// line, identified by their position in the line. Built from mini-boss data (ADR 0010, ADR 0014).
    /// </summary>
    public sealed class ProfessorRevelation
    {
        /// <param name="revealsHealth">Whether the professor's health becomes known.</param>
        /// <param name="revealsShield">Whether the professor's shield becomes known.</param>
        /// <param name="cardPositions">Zero-based positions in the professor's spell line. May be empty.</param>
        /// <exception cref="ArgumentNullException"><paramref name="cardPositions"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A position is negative.</exception>
        public ProfessorRevelation(bool revealsHealth, bool revealsShield, IEnumerable<int> cardPositions)
        {
            if (cardPositions == null)
            {
                throw new ArgumentNullException(nameof(cardPositions));
            }

            var positions = new List<int>();
            foreach (var position in cardPositions)
            {
                if (position < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(cardPositions), position, "A card position cannot be negative.");
                }

                if (!positions.Contains(position))
                {
                    positions.Add(position);
                }
            }

            positions.Sort();
            RevealsHealth = revealsHealth;
            RevealsShield = revealsShield;
            CardPositions = new ReadOnlyCollection<int>(positions);
        }

        /// <summary>Whether the professor's health becomes known.</summary>
        public bool RevealsHealth { get; }

        /// <summary>Whether the professor's shield becomes known.</summary>
        public bool RevealsShield { get; }

        /// <summary>Distinct positions in the professor's spell line, in ascending order.</summary>
        public IReadOnlyList<int> CardPositions { get; }
    }
}
