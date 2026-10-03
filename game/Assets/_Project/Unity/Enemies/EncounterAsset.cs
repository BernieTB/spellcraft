using System;
using System.Collections.Generic;
using Game.Core.Combat;
using Game.Core.Enemies;
using Game.Unity.Content;
using UnityEngine;

namespace Game.Unity.Enemies
{
    /// <summary>
    /// Authoring asset for one fight's opposition: the enemies the hero faces together, in resolution order (the
    /// hero aims at the first living one, ADR 0004). <see cref="ToParticipants"/> converts them to Core.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEncounter", menuName = "Game/Encounter")]
    public sealed class EncounterAsset : ScriptableObject, IPlaceholderContent
    {
        [SerializeField]
        [Tooltip("Stable identifier, unique among encounters (placeholder ids such as test_encounter_01 until content exists).")]
        private string _id;

        [SerializeField]
        [Tooltip("The enemies of the fight, in resolution order. At least one; the same enemy may appear several times.")]
        private List<EnemyAsset> _enemies = new List<EnemyAsset>();

        [SerializeField]
        [Tooltip("Test-only content. Tests and builds fail if an asset outside the placeholder and test folders references it.")]
        private bool _isPlaceholder;

        /// <summary>Stable identifier of the encounter.</summary>
        public string Id => _id;

        /// <summary>The enemies of the fight, in resolution order, as authored.</summary>
        public IReadOnlyList<EnemyAsset> Enemies => _enemies;

        /// <inheritdoc />
        public bool IsPlaceholder => _isPlaceholder;

        /// <summary>
        /// Converts the asset to an immutable Core <see cref="EncounterDefinition"/>, which runs use to create fresh
        /// participants for every fight.
        /// </summary>
        /// <exception cref="InvalidOperationException">The asset data is invalid; the message names the asset.</exception>
        public EncounterDefinition ToDefinition()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_id))
                {
                    throw new InvalidOperationException("The encounter has no id.");
                }

                if (_enemies.Count == 0)
                {
                    throw new InvalidOperationException("The encounter has no enemy.");
                }

                var enemies = new List<EnemyDefinition>(_enemies.Count);
                for (var i = 0; i < _enemies.Count; i++)
                {
                    var enemy = _enemies[i];
                    if (enemy == null)
                    {
                        throw new InvalidOperationException($"Enemy slot {i} is empty.");
                    }

                    enemies.Add(enemy.ToDefinition());
                }

                return new EncounterDefinition(_id, enemies);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                throw new InvalidOperationException($"Encounter asset '{name}' is invalid: {exception.Message}", exception);
            }
        }

        /// <summary>
        /// Builds a new Core participant for each enemy, in order, ready to pass to a <see cref="Fight"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The asset data is invalid; the message names the asset.</exception>
        public List<FightParticipant> ToParticipants()
        {
            return ToDefinition().CreateParticipants();
        }
    }
}
