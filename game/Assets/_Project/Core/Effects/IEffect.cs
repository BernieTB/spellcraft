namespace Game.Core.Effects
{
    /// <summary>
    /// One atomic thing a card does when it resolves. Implementations are immutable: their parameters
    /// (amounts...) come from data at construction and never change, so one instance can be shared.
    /// </summary>
    /// <remarks>
    /// Representation choice: <c>docs/adr/0003-effect-representation.md</c>.
    /// </remarks>
    public interface IEffect
    {
        /// <summary>
        /// Applies the effect to the combatants of <paramref name="context"/>.
        /// </summary>
        /// <returns>What the effect actually changed, after the combatant rules (shield, caps, death).</returns>
        EffectOutcome Apply(EffectContext context);
    }
}
