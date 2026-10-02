using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Game.Core.Combat.Log
{
    /// <summary>
    /// The state of one combatant when a fight starts, as recorded in a <see cref="CombatLog"/>: its stats and the
    /// ids of the cards of its spell line, in order. Immutable.
    /// </summary>
    public sealed class CombatantSnapshot
    {
        /// <exception cref="ArgumentNullException"><paramref name="spellLineCardIds"/> is null.</exception>
        public CombatantSnapshot(int index, int maxHealth, int health, int shield, IEnumerable<string> spellLineCardIds)
        {
            if (spellLineCardIds == null)
            {
                throw new ArgumentNullException(nameof(spellLineCardIds));
            }

            Index = index;
            MaxHealth = maxHealth;
            Health = health;
            Shield = shield;
            SpellLineCardIds = new ReadOnlyCollection<string>(new List<string>(spellLineCardIds));
        }

        /// <summary>Fight index: <see cref="Fight.HeroIndex"/> for the hero, then 1 to N for the enemies.</summary>
        public int Index { get; }

        /// <summary>Maximum health.</summary>
        public int MaxHealth { get; }

        /// <summary>Health when the fight starts.</summary>
        public int Health { get; }

        /// <summary>Shield when the fight starts.</summary>
        public int Shield { get; }

        /// <summary>Ids of the cards of the spell line, by position.</summary>
        public IReadOnlyList<string> SpellLineCardIds { get; }

        /// <summary>
        /// Captures <paramref name="participant"/> as it is now. Call it before the fight runs: the fight changes
        /// the combatant's stats.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="participant"/> is null.</exception>
        public static CombatantSnapshot Of(int index, FightParticipant participant)
        {
            if (participant == null)
            {
                throw new ArgumentNullException(nameof(participant));
            }

            var ids = new List<string>(participant.SpellLine.Count);
            foreach (var card in participant.SpellLine.Cards)
            {
                ids.Add(card.Id);
            }

            var combatant = participant.Combatant;
            return new CombatantSnapshot(index, combatant.MaxHealth, combatant.CurrentHealth, combatant.Shield, ids);
        }
    }
}
