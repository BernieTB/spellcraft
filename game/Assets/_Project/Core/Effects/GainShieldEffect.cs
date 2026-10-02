using System;

namespace Game.Core.Effects
{
    /// <summary>
    /// Placeholder effect: gives the context's caster a fixed amount of shield, from data.
    /// </summary>
    public sealed class GainShieldEffect : IEffect
    {
        /// <param name="amount">Shield gained, from card data. Zero or more.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/> is negative.</exception>
        public GainShieldEffect(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Shield amount cannot be negative.");
            }

            Amount = amount;
        }

        /// <summary>
        /// Shield gained each time the effect is applied.
        /// </summary>
        public int Amount { get; }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
        public EffectOutcome Apply(EffectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return EffectOutcome.FromShieldGain(context.Caster.GainShield(Amount));
        }
    }
}
