namespace Game.Core.Effects
{
    /// <summary>
    /// Which effect kind a bonus adds to (<c>docs/adr/0005-neighbour-modifier-resolution.md</c>). Values are
    /// explicit because card assets serialize them as integers: never renumber or reuse one, only append.
    /// </summary>
    public enum BonusKind
    {
        /// <summary>Adds to the damage of a <see cref="DealDamageEffect"/>.</summary>
        Damage = 0,

        /// <summary>Adds to the healing of a <see cref="HealEffect"/>.</summary>
        Heal = 1,

        /// <summary>Adds to the shield of a <see cref="GainShieldEffect"/>.</summary>
        Shield = 2,
    }
}
