namespace Game.Core.Effects
{
    /// <summary>
    /// An effect built around one amount of one <see cref="BonusKind"/> (deal damage, heal, gain shield). Lets rules
    /// that raise every effect of a kind, such as passive upgrades, rebuild the effect with a new amount without
    /// knowing its concrete type.
    /// </summary>
    public interface IAmountEffect : IEffect
    {
        /// <summary>The kind of amount this effect applies, and the kind of bonus it consumes.</summary>
        BonusKind Kind { get; }

        /// <summary>The amount from data, before any bonus.</summary>
        int Amount { get; }

        /// <summary>The same effect with <paramref name="amount"/> instead of <see cref="Amount"/>.</summary>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="amount"/> is negative.</exception>
        IAmountEffect WithAmount(int amount);
    }
}
