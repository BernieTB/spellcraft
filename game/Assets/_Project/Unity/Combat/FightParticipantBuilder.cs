using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.SpellLines;
using Game.Unity.Cards;

namespace Game.Unity.Combat
{
    /// <summary>
    /// Converts authored combatant data (stats and a spell line of <see cref="CardAsset"/>s) to a fresh Core
    /// <see cref="FightParticipant"/>. Used by the debug fight setup; enemies convert through Core definitions instead.
    /// </summary>
    public static class FightParticipantBuilder
    {
        /// <summary>
        /// Builds a new combatant and its spell line. The line's capacity is the number of cards given.
        /// </summary>
        /// <param name="maxHealth">Maximum and starting health, from data.</param>
        /// <param name="startingShield">Starting shield, from data.</param>
        /// <param name="spellLine">Cards of the spell line, in order.</param>
        /// <param name="label">Names the combatant in error messages, for example "hero" or "enemy 2".</param>
        /// <exception cref="ArgumentNullException"><paramref name="spellLine"/> is null.</exception>
        /// <exception cref="InvalidOperationException">A card slot is empty or a card asset is invalid.</exception>
        /// <exception cref="ArgumentException">The stats are invalid (checked by Core).</exception>
        public static FightParticipant Create(int maxHealth, int startingShield, IReadOnlyList<CardAsset> spellLine, string label)
        {
            if (spellLine == null)
            {
                throw new ArgumentNullException(nameof(spellLine));
            }

            var line = new SpellLine<CardDefinition>(Math.Max(1, spellLine.Count));
            for (var i = 0; i < spellLine.Count; i++)
            {
                var card = spellLine[i];
                if (card == null)
                {
                    throw new InvalidOperationException($"The {label}'s spell line has an empty slot at position {i}.");
                }

                line.Add(card.ToDefinition());
            }

            return new FightParticipant(new Combatant(maxHealth, startingShield), line);
        }
    }
}
