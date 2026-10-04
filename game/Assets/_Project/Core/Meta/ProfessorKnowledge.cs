using System.Collections.Generic;
using Game.Core.Cards;

namespace Game.Core.Meta
{
    /// <summary>
    /// What the player knows about a professor during the preparation phase (ADR 0014): its health and shield if
    /// revealed, and its spell line with unknown cards left as <c>null</c>.
    /// </summary>
    public sealed class ProfessorKnowledge
    {
        internal ProfessorKnowledge(string professorId, int? maxHealth, int? shield, IReadOnlyList<CardDefinition> spellLine)
        {
            ProfessorId = professorId;
            MaxHealth = maxHealth;
            Shield = shield;
            SpellLine = spellLine;
        }

        /// <summary>Id of the professor.</summary>
        public string ProfessorId { get; }

        /// <summary>The professor's max health, or <c>null</c> while it is unknown.</summary>
        public int? MaxHealth { get; }

        /// <summary>The professor's starting shield, or <c>null</c> while it is unknown.</summary>
        public int? Shield { get; }

        /// <summary>
        /// The professor's spell line in order, one entry per card; an entry is <c>null</c> while that card is
        /// unknown. The length of the line is always shown.
        /// </summary>
        public IReadOnlyList<CardDefinition> SpellLine { get; }

        /// <summary>Whether nothing about the professor is known yet.</summary>
        public bool IsUnknown
        {
            get
            {
                if (MaxHealth.HasValue || Shield.HasValue)
                {
                    return false;
                }

                foreach (var card in SpellLine)
                {
                    if (card != null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
