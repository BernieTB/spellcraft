using System;
using System.Collections.Generic;
using Game.Core.Meta;
using Game.Core.Runs;
using Game.Unity.Cards;
using Game.Unity.Content;
using Game.Unity.Enemies;
using UnityEngine;

namespace Game.Unity.Runs
{
    /// <summary>
    /// Authoring asset for one secret room of a biome (ADR 0010): the objective that unlocks it ("defeat N of enemy
    /// X"), the mini-boss's fight, and what the first victory gives: line slots, a unique card and a revelation about
    /// the biome's professor. <see cref="ToDefinition"/> converts it to a Core <see cref="SecretRoomDefinition"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSecretRoom", menuName = "Game/Secret Room")]
    public sealed class SecretRoomAsset : ScriptableObject, IPlaceholderContent
    {
        [SerializeField]
        [Tooltip("Stable identifier, unique among the biome's secret rooms (test ids such as test_room_01 until content exists).")]
        private string _id;

        [SerializeField]
        [Tooltip("The enemy the objective asks the player to defeat in regular fights. It must appear in a regular encounter of the biome.")]
        private EnemyAsset _objectiveEnemy;

        [SerializeField]
        [Min(1)]
        [Tooltip("How many of that enemy the player must defeat to unlock the room.")]
        private int _objectiveCount = 1;

        [SerializeField]
        [Tooltip("The mini-boss's fight.")]
        private EncounterAsset _miniBossEncounter;

        [SerializeField]
        [Min(0)]
        [Tooltip("Spell line slots the first victory over the mini-boss gives (ADR 0010: one).")]
        private int _bonusLineSlots = 1;

        [SerializeField]
        [Tooltip("The card the first victory gives, found nowhere else.")]
        private CardAsset _uniqueCard;

        [SerializeField]
        [Tooltip("Whether the first victory reveals the professor's health.")]
        private bool _revealsProfessorHealth;

        [SerializeField]
        [Tooltip("Whether the first victory reveals the professor's shield.")]
        private bool _revealsProfessorShield;

        [SerializeField]
        [Tooltip("Zero-based positions in the professor's spell line of the cards the first victory reveals. May be empty.")]
        private List<int> _revealedProfessorCardPositions = new List<int>();

        [SerializeField]
        [Tooltip("Test-only content. Tests and builds fail if an asset outside the placeholder and test folders references it.")]
        private bool _isPlaceholder;

        /// <summary>Stable identifier of the secret room.</summary>
        public string Id => _id;

        /// <inheritdoc />
        public bool IsPlaceholder => _isPlaceholder;

        /// <summary>Converts the asset to an immutable Core <see cref="SecretRoomDefinition"/>.</summary>
        /// <exception cref="InvalidOperationException">The asset data is invalid; the message names the asset.</exception>
        public SecretRoomDefinition ToDefinition()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_id))
                {
                    throw new InvalidOperationException("The secret room has no id.");
                }

                if (_objectiveEnemy == null)
                {
                    throw new InvalidOperationException("The secret room has no objective enemy.");
                }

                if (_miniBossEncounter == null)
                {
                    throw new InvalidOperationException("The secret room has no mini-boss encounter.");
                }

                if (_uniqueCard == null)
                {
                    throw new InvalidOperationException("The secret room has no unique card.");
                }

                return new SecretRoomDefinition(
                    _id,
                    new ObjectiveDefinition(_objectiveEnemy.Id, _objectiveCount),
                    _miniBossEncounter.ToDefinition(),
                    _bonusLineSlots,
                    _uniqueCard.ToDefinition(),
                    new ProfessorRevelation(_revealsProfessorHealth, _revealsProfessorShield, _revealedProfessorCardPositions));
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                throw new InvalidOperationException($"Secret room asset '{name}' is invalid: {exception.Message}", exception);
            }
        }
    }
}
