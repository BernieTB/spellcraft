using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Cards;
using Game.Core.Enemies;

namespace Game.Core.Meta
{
    /// <summary>
    /// Permanent knowledge about professors, kept across runs (ADR 0014): their health, shield and spell line cards
    /// are hidden until a mini-boss reveals them, and a revealed piece stays known. Saved with
    /// <see cref="BestiaryJson"/> through an <see cref="IBestiaryStore"/>.
    /// </summary>
    /// <remarks>
    /// Entries are kept sorted by professor id (ordinal), so iteration and the save file are deterministic.
    /// </remarks>
    public sealed class Bestiary
    {
        private readonly List<ProfessorEntry> _entries = new List<ProfessorEntry>();

        /// <summary>Creates an empty bestiary: nothing is known.</summary>
        public Bestiary()
        {
            Entries = new ReadOnlyCollection<ProfessorEntry>(_entries);
        }

        /// <summary>One entry per professor with at least one revealed piece, sorted by professor id.</summary>
        public IReadOnlyList<ProfessorEntry> Entries { get; }

        /// <summary>
        /// Records what <paramref name="revelation"/> teaches about <paramref name="professor"/>. Revealing a piece
        /// that is already known changes nothing.
        /// </summary>
        /// <returns><c>true</c> when something new became known.</returns>
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// A card position is outside the professor's spell line.
        /// </exception>
        public bool Reveal(EnemyDefinition professor, ProfessorRevelation revelation)
        {
            if (professor == null)
            {
                throw new ArgumentNullException(nameof(professor));
            }

            if (revelation == null)
            {
                throw new ArgumentNullException(nameof(revelation));
            }

            var cards = new List<RevealedCard>(revelation.CardPositions.Count);
            foreach (var position in revelation.CardPositions)
            {
                if (position >= professor.SpellLine.Count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(revelation),
                        position,
                        "Card position " + position + " is outside the spell line of professor '" + professor.Id + "' ("
                            + professor.SpellLine.Count + " cards).");
                }

                cards.Add(new RevealedCard(position, professor.SpellLine[position].Id));
            }

            return Merge(professor.Id, revelation.RevealsHealth, revelation.RevealsShield, cards);
        }

        /// <summary>The recorded entry for <paramref name="professorId"/>, or <c>null</c> when nothing is known.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="professorId"/> is null.</exception>
        public ProfessorEntry Find(string professorId)
        {
            if (professorId == null)
            {
                throw new ArgumentNullException(nameof(professorId));
            }

            var index = IndexOf(professorId);
            return index >= 0 ? _entries[index] : null;
        }

        /// <summary>
        /// What the player knows about <paramref name="professor"/>, read against its current data: known values
        /// are taken from the definition, unknown ones are <c>null</c>. A recorded card whose id no longer matches
        /// the card at its position (the data changed since it was revealed) is treated as unknown.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="professor"/> is null.</exception>
        public ProfessorKnowledge GetKnowledge(EnemyDefinition professor)
        {
            if (professor == null)
            {
                throw new ArgumentNullException(nameof(professor));
            }

            var entry = Find(professor.Id);
            var line = new CardDefinition[professor.SpellLine.Count];
            if (entry != null)
            {
                foreach (var card in entry.Cards)
                {
                    if (card.Position < line.Length
                        && string.Equals(professor.SpellLine[card.Position].Id, card.CardId, StringComparison.Ordinal))
                    {
                        line[card.Position] = professor.SpellLine[card.Position];
                    }
                }
            }

            return new ProfessorKnowledge(
                professor.Id,
                entry != null && entry.HealthKnown ? professor.MaxHealth : (int?)null,
                entry != null && entry.ShieldKnown ? professor.Shield : (int?)null,
                new ReadOnlyCollection<CardDefinition>(line));
        }

        /// <summary>Adds a recorded entry as read from a save. Merges with an existing entry for the same id.</summary>
        internal void Restore(string professorId, bool healthKnown, bool shieldKnown, IEnumerable<RevealedCard> cards)
        {
            Merge(professorId, healthKnown, shieldKnown, cards);
        }

        private bool Merge(string professorId, bool healthKnown, bool shieldKnown, IEnumerable<RevealedCard> cards)
        {
            var index = IndexOf(professorId);
            var existing = index >= 0 ? _entries[index] : ProfessorEntry.Empty(professorId);
            var merged = existing.Merge(healthKnown, shieldKnown, cards, out var changed);
            if (!changed)
            {
                return false;
            }

            if (index >= 0)
            {
                _entries[index] = merged;
            }
            else
            {
                _entries.Insert(~index, merged);
            }

            return true;
        }

        // Binary search on the ordinal-sorted list; returns the bitwise complement of the insertion point when absent.
        private int IndexOf(string professorId)
        {
            var low = 0;
            var high = _entries.Count - 1;
            while (low <= high)
            {
                var middle = low + ((high - low) / 2);
                var comparison = string.CompareOrdinal(_entries[middle].ProfessorId, professorId);
                if (comparison == 0)
                {
                    return middle;
                }

                if (comparison < 0)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return ~low;
        }
    }
}
