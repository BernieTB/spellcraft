using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Effects;
using UnityEngine;

namespace Game.Unity.Cards
{
    /// <summary>
    /// One evolution stage of a <see cref="CardAsset"/>, authored in the inspector and converted to a Core
    /// <see cref="CardEvolution"/> (<c>docs/adr/0013-card-evolution.md</c>). The stage lists the full effects and
    /// neighbour modifiers of the card at that stage; the card's word and cast time never change.
    /// </summary>
    [Serializable]
    public struct CardEvolutionEntry
    {
        [SerializeField]
        [Min(1)]
        [Tooltip("Total casts of one copy of the card, over the whole run, needed to reach this stage.")]
        private int _castsRequired;

        [SerializeField]
        [Tooltip("Effects of the card at this stage, in the order they resolve. They replace the card's effects.")]
        private List<EffectEntry> _effects;

        [SerializeField]
        [Tooltip("Bonuses the card gives to its neighbours at this stage. They replace the card's modifiers.")]
        private List<NeighbourModifierEntry> _neighbourModifiers;

        /// <summary>Converts the stage to Core data.</summary>
        /// <exception cref="ArgumentException">The stage is invalid (see <see cref="CardEvolution"/>).</exception>
        /// <exception cref="InvalidOperationException">An effect has no Core mapping.</exception>
        public CardEvolution ToEvolution()
        {
            var effects = new List<IEffect>();
            if (_effects != null)
            {
                foreach (var entry in _effects)
                {
                    effects.Add(entry.ToEffect());
                }
            }

            var modifiers = new List<NeighbourModifier>();
            if (_neighbourModifiers != null)
            {
                foreach (var entry in _neighbourModifiers)
                {
                    modifiers.Add(entry.ToModifier());
                }
            }

            return new CardEvolution(_castsRequired, effects, modifiers);
        }
    }
}
