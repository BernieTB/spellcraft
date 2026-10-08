using System;
using System.Collections.Generic;
using Game.Core.Enemies;
using Game.Core.Runs;
using Game.Unity.Content;
using Game.Unity.Enemies;
using UnityEngine;

namespace Game.Unity.Runs
{
    /// <summary>
    /// Authoring data for one biome (ADR 0009, 0010): its regular encounter pool, the regular fights before the
    /// professor, the professor's fight and its secret rooms, converted to a Core <see cref="BiomeDefinition"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBiome", menuName = "Game/Biome")]
    public sealed class BiomeAsset : ScriptableObject, IPlaceholderContent
    {
        [SerializeField]
        [Tooltip("Stable identifier (placeholder ids such as BIOME_01 until content exists).")]
        private string _id;

        [SerializeField]
        [Tooltip("Pool of regular fights, one drawn per regular fight with equal chances. List an encounter several times to make it more common.")]
        private List<EncounterAsset> _regularEncounters = new List<EncounterAsset>();

        [SerializeField]
        [Min(0)]
        [Tooltip("Regular fights to win before the professor is available (ADR 0009: about 8).")]
        private int _minimumRegularFights = 8;

        [SerializeField]
        [Tooltip("The professor's fight.")]
        private EncounterAsset _professorEncounter;

        [SerializeField]
        [Tooltip("The professor: one of the enemies of the professor's encounter.")]
        private EnemyAsset _professor;

        [SerializeField]
        [Tooltip("The biome's secret rooms, in the order they are listed to the player.")]
        private List<SecretRoomAsset> _secretRooms = new List<SecretRoomAsset>();

        [SerializeField]
        [Tooltip("Test-only content. Tests and builds fail if an asset outside the placeholder and test folders references it.")]
        private bool _isPlaceholder;

        public string Id => _id;

        public IReadOnlyList<EncounterAsset> RegularEncounters => _regularEncounters;

        public IReadOnlyList<SecretRoomAsset> SecretRooms => _secretRooms;

        public bool IsPlaceholder => _isPlaceholder;

        /// <summary>Converts the asset to Core data.</summary>
        /// <exception cref="InvalidOperationException">The asset or one it references is invalid; the message names it.</exception>
        public BiomeDefinition ToDefinition()
        {
            try
            {
                if (_professorEncounter == null)
                {
                    throw new InvalidOperationException("The biome has no professor encounter.");
                }

                if (_professor == null)
                {
                    throw new InvalidOperationException("The biome has no professor.");
                }

                var pool = new List<EncounterDefinition>(_regularEncounters.Count);
                for (var i = 0; i < _regularEncounters.Count; i++)
                {
                    if (_regularEncounters[i] == null)
                    {
                        throw new InvalidOperationException($"The regular encounter slot {i} is empty.");
                    }

                    pool.Add(_regularEncounters[i].ToDefinition());
                }

                var rooms = new List<SecretRoomDefinition>(_secretRooms.Count);
                for (var i = 0; i < _secretRooms.Count; i++)
                {
                    if (_secretRooms[i] == null)
                    {
                        throw new InvalidOperationException($"The secret room slot {i} is empty.");
                    }

                    rooms.Add(_secretRooms[i].ToDefinition());
                }

                return new BiomeDefinition(
                    _id, pool, _minimumRegularFights, _professorEncounter.ToDefinition(), _professor.ToDefinition(), rooms);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                var idText = string.IsNullOrWhiteSpace(_id) ? string.Empty : $" (id '{_id}')";
                throw new InvalidOperationException($"Biome asset '{name}'{idText} is invalid: {exception.Message}", exception);
            }
        }
    }
}
