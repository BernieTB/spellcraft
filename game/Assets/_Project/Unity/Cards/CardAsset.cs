using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Effects;
using UnityEngine;

namespace Game.Unity.Cards
{
    /// <summary>
    /// Authoring asset for one card. Holds data only; <see cref="ToDefinition"/> converts it to the Core
    /// <see cref="CardDefinition"/> used by the simulation. Ids are placeholders until the owner writes content.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "Game/Card")]
    public sealed class CardAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Stable identifier, unique among cards (placeholder ids such as test_card_01 until content exists).")]
        private string _id;

        [SerializeField]
        [Min(1)]
        [Tooltip("Simulation ticks needed to cast the card. Must be greater than zero.")]
        private int _castTime;

        [SerializeField]
        [Tooltip("Effects applied in this order when the card resolves.")]
        private List<EffectEntry> _effects = new List<EffectEntry>();

        public string Id => _id;

        public int CastTime => _castTime;

        public IReadOnlyList<EffectEntry> Effects => _effects;

        /// <summary>
        /// Converts this asset to an immutable Core card definition.
        /// </summary>
        /// <exception cref="InvalidOperationException">The asset data is invalid; the message names the asset.</exception>
        public CardDefinition ToDefinition()
        {
            try
            {
                var effects = new List<IEffect>(_effects.Count);
                foreach (var entry in _effects)
                {
                    effects.Add(entry.ToEffect());
                }

                return new CardDefinition(_id, _castTime, effects);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"Card asset '{name}' is invalid: {exception.Message}", exception);
            }
            catch (InvalidOperationException exception)
            {
                throw new InvalidOperationException($"Card asset '{name}' is invalid: {exception.Message}", exception);
            }
        }
    }
}
