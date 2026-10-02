using System;

namespace Game.Core.Effects
{
    /// <summary>
    /// Placeholder effect: heals the context's caster by a fixed amount, from data.
    /// </summary>
    public sealed class HealEffect : IEffect
    {
        /// <param name="amount">Healing applied, from card data. Zero or more.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/> is negative.</exception>
        public HealEffect(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Heal amount cannot be negative.");
            }

            Amount = amount;
        }

        /// <summary>
        /// Healing applied each time the effect is applied.
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

            return EffectOutcome.FromHeal(context.Caster.Heal(Amount));
        }
    }
}
