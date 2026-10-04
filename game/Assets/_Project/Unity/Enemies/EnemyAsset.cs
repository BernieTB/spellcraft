using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Enemies;
using Game.Unity.Cards;
using Game.Unity.Content;
using UnityEngine;

namespace Game.Unity.Enemies
{
    /// <summary>
    /// Authoring asset for one enemy: a regular monster, a mini-boss or a professor. Holds its stats and its own
    /// spell line of cards (enemies use the same spell-line system as the hero, ADR 0002); <see cref="ToParticipant"/>
    /// converts it to a fresh Core <see cref="FightParticipant"/>. Ids are placeholders until the owner writes content.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Game/Enemy")]
    public sealed class EnemyAsset : ScriptableObject, IPlaceholderContent
    {
        [SerializeField]
        [Tooltip("Stable identifier, unique among enemies (placeholder ids such as test_enemy_01 until content exists).")]
        private string _id;

        [SerializeField]
        [Tooltip("Regular monster, mini-boss or professor.")]
        private EnemyRank _rank;

        [SerializeField]
        [Min(1)]
        private int _maxHealth = 1;

        [SerializeField]
        [Min(0)]
        private int _startingShield;

        [SerializeField]
        [Min(0)]
        [Tooltip("XP the hero earns when this enemy is defeated. Mini-bosses usually carry more than regular monsters.")]
        private int _xpReward;

        [SerializeField]
        [Tooltip("Cards of the enemy's spell line, in order. At least one.")]
        private List<CardAsset> _spellLine = new List<CardAsset>();

        [SerializeField]
        [Tooltip("Test-only content. Tests and builds fail if an asset outside the placeholder and test folders references it.")]
        private bool _isPlaceholder;

        /// <summary>Stable identifier of the enemy.</summary>
        public string Id => _id;

        /// <summary>Regular monster, mini-boss or professor.</summary>
        public EnemyRank Rank => _rank;

        /// <inheritdoc />
        public bool IsPlaceholder => _isPlaceholder;

        /// <summary>
        /// Converts the asset to an immutable Core <see cref="EnemyDefinition"/>, which runs use to create a fresh
        /// participant for every fight.
        /// </summary>
        /// <exception cref="InvalidOperationException">The asset data is invalid; the message names the asset.</exception>
        public EnemyDefinition ToDefinition()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_id))
                {
                    throw new InvalidOperationException("The enemy has no id.");
                }

                if (_spellLine.Count == 0)
                {
                    throw new InvalidOperationException("The enemy's spell line has no card.");
                }

                var cards = new List<CardDefinition>(_spellLine.Count);
                for (var i = 0; i < _spellLine.Count; i++)
                {
                    var card = _spellLine[i];
                    if (card == null)
                    {
                        throw new InvalidOperationException($"The enemy's spell line has an empty slot at position {i}.");
                    }

                    cards.Add(card.ToDefinition());
                }

                return new EnemyDefinition(_id, _maxHealth, _startingShield, cards, _xpReward);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                throw new InvalidOperationException($"Enemy asset '{name}' is invalid: {exception.Message}", exception);
            }
        }

        /// <summary>
        /// Builds a new combatant with this enemy's stats and spell line. Each call returns an independent
        /// participant, so the same enemy can appear several times in one fight.
        /// </summary>
        /// <exception cref="InvalidOperationException">The asset data is invalid; the message names the asset.</exception>
        public FightParticipant ToParticipant()
        {
            return ToDefinition().CreateParticipant();
        }
    }
}
