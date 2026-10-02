using System;

namespace Game.Core.Effects
{
    /// <summary>
    /// Placeholder effect: heals the context's caster by a fixed amount, from data.
    /// The cast's neighbour bonus of the matching kind, if any and not consumed by an earlier effect of the
    /// card, is added to the amount (<see cref="EffectContext.ConsumeBonus"/>).
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
        /// Healing applied each time the effect is applied, before any neighbour bonus.
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

            var amount = checked(Amount + context.ConsumeBonus(BonusKind.Heal));
            return EffectOutcome.FromHeal(context.Caster.Heal(amount));
        }
    }
}
