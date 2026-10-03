namespace Game.Core.Upgrades
{
    /// <summary>
    /// What a <see cref="PassiveUpgrade"/> improves (<c>docs/adr/0012-linked-choices-and-spell-line-editing.md</c>).
    /// Values are explicit because upgrade assets serialize them as integers: never renumber or reuse one, only
    /// append.
    /// </summary>
    public enum PassiveUpgradeKind
    {
        /// <summary>+X max health for the hero.</summary>
        MaxHealth = 0,

        /// <summary>+X shield for the hero at the start of every fight.</summary>
        StartingShield = 1,

        /// <summary>+X to every effect of one <see cref="Game.Core.Effects.BonusKind"/> the hero casts.</summary>
        EffectAmount = 2,

        /// <summary>+X to every neighbour modifier granted by the hero's cards.</summary>
        NeighbourBonus = 3,
    }
}
