namespace Game.Unity.Cards
{
    /// <summary>
    /// Effect kinds that can be authored on a <see cref="CardAsset"/>. Values are serialized as integers in
    /// assets: never renumber or reuse one, only append.
    /// </summary>
    public enum EffectKind
    {
        DealDamage = 0,
        Heal = 1,
        GainShield = 2,
    }
}
