using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Combat;

namespace Game.Core.Enemies
{
    /// <summary>
    /// Immutable description of one fight's enemies, built from data, in the order the fight processes them.
    /// The same enemy definition may appear several times: each appearance becomes its own combatant.
    /// </summary>
    public sealed class EncounterDefinition
    {
        /// <param name="id">Stable identifier from data. Not empty or whitespace.</param>
        /// <param name="enemies">The enemies, at least one, in fight order.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is null, empty or whitespace, or <paramref name="enemies"/> is empty.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="enemies"/> or one of its items is null.</exception>
        public EncounterDefinition(string id, IEnumerable<EnemyDefinition> enemies)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Encounter id cannot be null, empty or whitespace.", nameof(id));
            }

            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            var copy = new List<EnemyDefinition>(enemies);
            if (copy.Contains(null))
            {
                throw new ArgumentNullException(nameof(enemies), "An encounter cannot contain a null enemy.");
            }

            if (copy.Count == 0)
            {
                throw new ArgumentException("An encounter needs at least one enemy.", nameof(enemies));
            }

            Id = id;
            Enemies = new ReadOnlyCollection<EnemyDefinition>(copy);
        }

        /// <summary>Stable identifier, unique among encounters.</summary>
        public string Id { get; }

        /// <summary>The enemies, in fight order.</summary>
        public IReadOnlyList<EnemyDefinition> Enemies { get; }

        /// <summary>Creates new fight participants at full health, one per enemy, in fight order.</summary>
        public List<FightParticipant> CreateParticipants()
        {
            var participants = new List<FightParticipant>(Enemies.Count);
            foreach (var enemy in Enemies)
            {
                participants.Add(enemy.CreateParticipant());
            }

            return participants;
        }
    }
}
