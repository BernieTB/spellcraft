using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Enemies;

namespace Game.Core.Runs
{
    /// <summary>
    /// Immutable description of a biome, built from data: the pool regular fights are drawn from, how many regular
    /// fights open the professor, and the professor's encounter (<c>docs/adr/0009-vertical-slice-run-pacing.md</c>).
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
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is null, empty or whitespace, or <paramref name="regularEncounters"/> is empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">An encounter list, one of its items or the professor is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minimumRegularFights"/> is negative.</exception>
        public BiomeDefinition(
            string id,
            IEnumerable<EncounterDefinition> regularEncounters,
            int minimumRegularFights,
            EncounterDefinition professorEncounter)
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

            Id = id;
            RegularEncounters = new ReadOnlyCollection<EncounterDefinition>(pool);
            MinimumRegularFights = minimumRegularFights;
            ProfessorEncounter = professorEncounter ?? throw new ArgumentNullException(nameof(professorEncounter));
        }

        /// <summary>Stable identifier, unique among biomes.</summary>
        public string Id { get; }

        /// <summary>Pool regular fights are drawn from, with equal chances per entry.</summary>
        public IReadOnlyList<EncounterDefinition> RegularEncounters { get; }

        /// <summary>Regular fights to win before the professor is available.</summary>
        public int MinimumRegularFights { get; }

        /// <summary>The professor's fight.</summary>
        public EncounterDefinition ProfessorEncounter { get; }
    }
}
