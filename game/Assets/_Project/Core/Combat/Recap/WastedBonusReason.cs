namespace Game.Core.Combat.Recap
{
    /// <summary>Why a neighbour bonus was wasted. Values are explicit and must not be renumbered.</summary>
    public enum WastedBonusReason
    {
        /// <summary>
        /// The receiving card has no effect of the bonus's kind (for example a +damage bonus on a shield card), the
        /// only way a bonus is wasted in the first-pass rules (ADR 0004, ADR 0005).
        /// </summary>
        NoEffectOfKind = 1,
    }
}
