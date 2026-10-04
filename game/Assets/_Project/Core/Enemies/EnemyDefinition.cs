using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.SpellLines;

namespace Game.Core.Enemies
{
    /// <summary>
    /// Immutable description of an enemy, built from data: its stats and its own spell line. A run creates a fresh
    /// <see cref="FightParticipant"/> from it for every fight, so a definition is never changed by a fight.
    /// </summary>
    public sealed class EnemyDefinition
    {
        /// <param name="id">Stable identifier from data. Not empty or whitespace.</param>
        /// <param name="maxHealth">Max health at the start of a fight. Greater than zero.</param>
        /// <param name="shield">Shield at the start of a fight. Zero or more.</param>
        /// <param name="spellLine">The cards the enemy casts, in order, in a loop. At least one card.</param>
        /// <param name="xpReward">
        /// XP the hero earns when this enemy is defeated (<c>docs/adr/0009-vertical-slice-run-pacing.md</c>). Zero or
        /// more; mini-bosses simply carry more XP in their data.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is null, empty or whitespace, or <paramref name="spellLine"/> is empty.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">A stat or <paramref name="xpReward"/> is out of range.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="spellLine"/> or one of its cards is null.</exception>
        public EnemyDefinition(string id, int maxHealth, int shield, IEnumerable<CardDefinition> spellLine, int xpReward = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Enemy id cannot be null, empty or whitespace.", nameof(id));
            }

            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Max health must be greater than zero.");
            }

            if (shield < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(shield), shield, "Shield cannot be negative.");
            }

            if (xpReward < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(xpReward), xpReward, "XP reward cannot be negative.");
            }

            if (spellLine == null)
            {
                throw new ArgumentNullException(nameof(spellLine));
            }

            var cards = new List<CardDefinition>(spellLine);
            if (cards.Contains(null))
            {
                throw new ArgumentNullException(nameof(spellLine), "A spell line cannot contain a null card.");
            }

            if (cards.Count == 0)
            {
                throw new ArgumentException("An enemy needs at least one card in its spell line.", nameof(spellLine));
            }

            Id = id;
            MaxHealth = maxHealth;
            Shield = shield;
            SpellLine = new ReadOnlyCollection<CardDefinition>(cards);
            XpReward = xpReward;
        }

        /// <summary>Stable identifier, unique among enemies.</summary>
        public string Id { get; }

        /// <summary>Max health at the start of a fight.</summary>
        public int MaxHealth { get; }

        /// <summary>Shield at the start of a fight.</summary>
        public int Shield { get; }

        /// <summary>The cards the enemy casts, in line order.</summary>
        public IReadOnlyList<CardDefinition> SpellLine { get; }

        /// <summary>XP the hero earns when this enemy is defeated.</summary>
        public int XpReward { get; }

        /// <summary>Creates a new fight participant at full health, with its own combatant and spell line.</summary>
        public FightParticipant CreateParticipant()
        {
            var line = new SpellLine<CardDefinition>(SpellLine.Count);
            foreach (var card in SpellLine)
            {
                line.Add(card);
            }

            return new FightParticipant(new Combatant(MaxHealth, Shield), line);
        }
    }
}
