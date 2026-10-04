using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Game.Core.Meta
{
    /// <summary>
    /// What the bestiary has recorded about one professor: whether its health and shield are known, and which
    /// cards of its spell line were revealed. Values themselves are not stored: they are read from the
    /// professor's current data when the knowledge is shown (<see cref="Bestiary.GetKnowledge"/>).
    /// </summary>
    public sealed class ProfessorEntry
    {
        internal ProfessorEntry(string professorId, bool healthKnown, bool shieldKnown, IEnumerable<RevealedCard> cards)
        {
            ProfessorId = professorId;
            HealthKnown = healthKnown;
            ShieldKnown = shieldKnown;

            var sorted = new List<RevealedCard>(cards);
            sorted.Sort((a, b) => a.Position.CompareTo(b.Position));
            Cards = new ReadOnlyCollection<RevealedCard>(sorted);
        }

        /// <summary>Id of the professor (<see cref="Enemies.EnemyDefinition.Id"/>).</summary>
        public string ProfessorId { get; }

        /// <summary>Whether the professor's health was revealed.</summary>
        public bool HealthKnown { get; }

        /// <summary>Whether the professor's shield was revealed.</summary>
        public bool ShieldKnown { get; }

        /// <summary>Revealed cards, one per position, in ascending position order.</summary>
        public IReadOnlyList<RevealedCard> Cards { get; }

        internal ProfessorEntry Merge(bool healthKnown, bool shieldKnown, IEnumerable<RevealedCard> cards, out bool changed)
        {
            changed = (healthKnown && !HealthKnown) || (shieldKnown && !ShieldKnown);
            var merged = new List<RevealedCard>(Cards);
            foreach (var card in cards)
            {
                var index = merged.FindIndex(existing => existing.Position == card.Position);
                if (index < 0)
                {
                    merged.Add(card);
                    changed = true;
                }
                else if (!merged[index].Equals(card))
                {
                    // The professor's data changed since the old reveal: the newest reveal wins.
                    merged[index] = card;
                    changed = true;
                }
            }

            return changed
                ? new ProfessorEntry(ProfessorId, HealthKnown || healthKnown, ShieldKnown || shieldKnown, merged)
                : this;
        }

        internal static ProfessorEntry Empty(string professorId)
        {
            if (string.IsNullOrWhiteSpace(professorId))
            {
                throw new ArgumentException("A professor id cannot be null, empty or whitespace.", nameof(professorId));
            }

            return new ProfessorEntry(professorId, false, false, Array.Empty<RevealedCard>());
        }
    }
}
