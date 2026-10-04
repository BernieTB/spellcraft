using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Enemies;

namespace Game.Core.Runs
{
    /// <summary>
    /// Immutable description of a biome, built from data: the pool regular fights are drawn from, how many regular
    /// fights open the professor, the professor's encounter (<c>docs/adr/0009-vertical-slice-run-pacing.md</c>) and
    /// its secret rooms (<c>docs/adr/0010-secret-rooms-and-mini-boss-rewards.md</c>).
    /// </summary>
    public sealed class BiomeDefinition
    {
        /// <param name="id">Stable identifier from data. Not empty or whitespace.</param>
        /// <param name="regularEncounters">
        /// Pool of regular fights, at least one. Each regular fight draws one with the run's seed, with equal
        /// chances; list an encounter several times to make it more common.
        /// </param>
        /// <param name="minimumRegularFights">Regular fights to win before the professor is available. Zero or more.</param>
        /// <param name="professorEncounter">The professor's fight.</param>
        /// <param name="professor">
        /// The professor, one of the enemies of <paramref name="professorEncounter"/> (matched by id): the one the
        /// secret rooms' revelations are about. Enemies have no rank in Core yet, so data says which one it is.
        /// </param>
        /// <param name="secretRooms">
        /// The biome's secret rooms, in the order they are listed to the player; null for none. Ids are unique, each
        /// objective's enemy appears in at least one regular encounter (otherwise the room could never be unlocked),
        /// and each revelation only points at cards the <see cref="Professor"/> has.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is null, empty or whitespace, or <paramref name="regularEncounters"/> is empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// An encounter list, one of its items, the professor encounter, the professor or one of the secret rooms is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="minimumRegularFights"/> is negative, or a revelation points at a card the professor lacks.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Two secret rooms share an id, an objective's enemy is in no regular encounter, or the professor is not an
        /// enemy of the professor encounter.
        /// </exception>
        public BiomeDefinition(
            string id,
            IEnumerable<EncounterDefinition> regularEncounters,
            int minimumRegularFights,
            EncounterDefinition professorEncounter,
            EnemyDefinition professor,
            IEnumerable<SecretRoomDefinition> secretRooms = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Biome id cannot be null, empty or whitespace.", nameof(id));
            }

            if (regularEncounters == null)
            {
                throw new ArgumentNullException(nameof(regularEncounters));
            }

            var pool = new List<EncounterDefinition>(regularEncounters);
            if (pool.Contains(null))
            {
                throw new ArgumentNullException(nameof(regularEncounters), "The encounter pool cannot contain a null encounter.");
            }

            if (pool.Count == 0)
            {
                throw new ArgumentException("A biome needs at least one regular encounter.", nameof(regularEncounters));
            }

            if (minimumRegularFights < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumRegularFights), minimumRegularFights, "Minimum regular fights cannot be negative.");
            }

            if (professorEncounter == null)
            {
                throw new ArgumentNullException(nameof(professorEncounter));
            }

            var rooms = secretRooms == null ? new List<SecretRoomDefinition>() : new List<SecretRoomDefinition>(secretRooms);
            if (rooms.Contains(null))
            {
                throw new ArgumentNullException(nameof(secretRooms), "The secret rooms cannot contain a null room.");
            }

            if (professor == null)
            {
                throw new ArgumentNullException(nameof(professor));
            }

            EnemyDefinition professorInEncounter = null;
            foreach (var enemy in professorEncounter.Enemies)
            {
                if (string.Equals(enemy.Id, professor.Id, StringComparison.Ordinal))
                {
                    professorInEncounter = enemy;
                    break;
                }
            }

            if (professorInEncounter == null)
            {
                throw new ArgumentException(
                    $"Biome '{id}': the professor '{professor.Id}' is not an enemy of the professor encounter '{professorEncounter.Id}'.",
                    nameof(professor));
            }

            var seenIds = new List<string>();
            foreach (var room in rooms)
            {
                if (seenIds.Contains(room.Id))
                {
                    throw new ArgumentException($"Biome '{id}': two secret rooms share the id '{room.Id}'.", nameof(secretRooms));
                }

                seenIds.Add(room.Id);
                if (!PoolHasEnemy(pool, room.Objective.EnemyId))
                {
                    throw new ArgumentException(
                        $"Biome '{id}': the objective of secret room '{room.Id}' needs enemy '{room.Objective.EnemyId}', which is in no regular encounter.",
                        nameof(secretRooms));
                }

                foreach (var position in room.Revelation.CardPositions)
                {
                    if (position >= professorInEncounter.SpellLine.Count)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(secretRooms),
                            position,
                            $"Biome '{id}': secret room '{room.Id}' reveals card position {position}, but professor '{professorInEncounter.Id}' has {professorInEncounter.SpellLine.Count} cards.");
                    }
                }
            }

            Id = id;
            RegularEncounters = new ReadOnlyCollection<EncounterDefinition>(pool);
            MinimumRegularFights = minimumRegularFights;
            ProfessorEncounter = professorEncounter;
            Professor = professorInEncounter;
            SecretRooms = new ReadOnlyCollection<SecretRoomDefinition>(rooms);
        }

        /// <summary>Stable identifier, unique among biomes.</summary>
        public string Id { get; }

        /// <summary>Pool regular fights are drawn from, with equal chances per entry.</summary>
        public IReadOnlyList<EncounterDefinition> RegularEncounters { get; }

        /// <summary>Regular fights to win before the professor is available.</summary>
        public int MinimumRegularFights { get; }

        /// <summary>The professor's fight.</summary>
        public EncounterDefinition ProfessorEncounter { get; }

        /// <summary>
        /// The professor, whom the secret rooms' revelations are about: the enemy of
        /// <see cref="ProfessorEncounter"/> the data names (enemies have no rank in Core yet).
        /// </summary>
        public EnemyDefinition Professor { get; }

        /// <summary>The biome's secret rooms, in listing order. Empty when it has none.</summary>
        public IReadOnlyList<SecretRoomDefinition> SecretRooms { get; }

        private static bool PoolHasEnemy(List<EncounterDefinition> pool, string enemyId)
        {
            foreach (var encounter in pool)
            {
                foreach (var enemy in encounter.Enemies)
                {
                    if (string.Equals(enemy.Id, enemyId, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
