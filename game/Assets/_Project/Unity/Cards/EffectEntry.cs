using System;
using Game.Core.Effects;
using UnityEngine;

namespace Game.Unity.Cards
{
    /// <summary>
    /// Authoring data for one effect of a card: a kind and an amount. Converted to a Core <see cref="IEffect"/>
    /// by <see cref="ToEffect"/>; Core validates the amount.
    /// </summary>
    [Serializable]
    public struct EffectEntry
    {
        [SerializeField]
        private EffectKind _kind;

        [SerializeField]
        [Min(0)]
        private int _amount;

        /// <summary>
        /// Creates the Core effect described by this entry.
        /// </summary>
        /// <exception cref="InvalidOperationException">The kind has no Core mapping.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The amount is rejected by Core (negative).</exception>
        public IEffect ToEffect()
        {
            switch (_kind)
            {
                case EffectKind.DealDamage:
                    return new DealDamageEffect(_amount);
                case EffectKind.Heal:
                    return new HealEffect(_amount);
                case EffectKind.GainShield:
                    return new GainShieldEffect(_amount);
                default:
                    throw new InvalidOperationException($"Effect kind {(int)_kind} has no Core mapping.");
            }
        }
    }
}
