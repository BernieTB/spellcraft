using System;

namespace Game.Core.Effects
{
    /// <summary>
    /// Placeholder effect: deals a fixed amount of damage, from data, to the context's target.
    /// </summary>
    public sealed class DealDamageEffect : IEffect
    {
        /// <param name="amount">Damage dealt, from card data. Zero or more.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/> is negative.</exception>
        public DealDamageEffect(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Damage amount cannot be negative.");
            }

            Amount = amount;
        }

        /// <summary>
        /// Damage dealt each time the effect is applied.
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

            return EffectOutcome.FromDamage(context.Target.TakeDamage(Amount));
        }
    }
}
