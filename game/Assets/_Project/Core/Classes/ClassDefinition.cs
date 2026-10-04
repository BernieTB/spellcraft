using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Cards;

namespace Game.Core.Classes
{
    /// <summary>
    /// Immutable description of a playable class, built from data: the hero's base stats, the starting spell line
    /// and the pool of cards offered at level-ups (<c>docs/adr/0007-mvp-class-and-starting-deck.md</c>,
    /// <c>docs/adr/0009-vertical-slice-run-pacing.md</c>). Nothing here is a balance number.
    /// </summary>
    public sealed class ClassDefinition
    {
        /// <param name="id">Stable identifier from data. Not empty or whitespace.</param>
        /// <param name="maxHealth">The hero's max health at the start of every fight. Greater than zero.</param>
        /// <param name="startingShield">The hero's shield at the start of every fight. Zero or more.</param>
        /// <param name="startingLineCapacity">Number of spell line slots at the start of a run. At least 1.</param>
        /// <param name="startingDeck">
        /// The starting cards, in line order. The starting deck is the starting spell line, so it holds at least one
        /// card and no more than <paramref name="startingLineCapacity"/>.
        /// </param>
        /// <param name="cardPool">Cards that level-up offers can draw from. May be empty; the same card may appear twice.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is null, empty or whitespace, or <paramref name="startingDeck"/> is empty or larger
        /// than <paramref name="startingLineCapacity"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">A stat or the capacity is out of range.</exception>
        /// <exception cref="ArgumentNullException">A card list or one of its items is null.</exception>
        public ClassDefinition(
            string id,
            int maxHealth,
            int startingShield,
            int startingLineCapacity,
            IEnumerable<CardDefinition> startingDeck,
            IEnumerable<CardDefinition> cardPool)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Class id cannot be null, empty or whitespace.", nameof(id));
            }

            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Max health must be greater than zero.");
            }

            if (startingShield < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingShield), startingShield, "Starting shield cannot be negative.");
            }

            if (startingLineCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingLineCapacity), startingLineCapacity, "Starting line capacity must be at least 1.");
            }

            var deck = CopyCards(startingDeck, nameof(startingDeck));
            if (deck.Count == 0)
            {
                throw new ArgumentException("The starting deck needs at least one card.", nameof(startingDeck));
            }

            if (deck.Count > startingLineCapacity)
            {
                throw new ArgumentException(
                    $"The starting deck has {deck.Count} cards but the starting line holds {startingLineCapacity}.",
                    nameof(startingDeck));
            }

            Id = id;
            MaxHealth = maxHealth;
            StartingShield = startingShield;
            StartingLineCapacity = startingLineCapacity;
            StartingDeck = new ReadOnlyCollection<CardDefinition>(deck);
            CardPool = new ReadOnlyCollection<CardDefinition>(CopyCards(cardPool, nameof(cardPool)));
        }

        /// <summary>Stable identifier, unique among classes.</summary>
        public string Id { get; }

        /// <summary>The hero's max health at the start of every fight.</summary>
        public int MaxHealth { get; }

        /// <summary>The hero's shield at the start of every fight.</summary>
        public int StartingShield { get; }

        /// <summary>Number of spell line slots at the start of a run.</summary>
        public int StartingLineCapacity { get; }

        /// <summary>The starting cards, in line order. They form the starting spell line.</summary>
        public IReadOnlyList<CardDefinition> StartingDeck { get; }

        /// <summary>Cards that level-up offers can draw from.</summary>
        public IReadOnlyList<CardDefinition> CardPool { get; }

        private static List<CardDefinition> CopyCards(IEnumerable<CardDefinition> cards, string paramName)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(paramName);
            }

            var copy = new List<CardDefinition>(cards);
            if (copy.Contains(null))
            {
                throw new ArgumentNullException(paramName, "A card list cannot contain a null card.");
            }

            return copy;
        }
    }
}
