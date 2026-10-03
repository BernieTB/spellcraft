namespace Game.Core.Combat.Recap
{
    /// <summary>A possible cause of a defeat. Values are explicit and must not be renumbered.</summary>
    public enum DefeatCause
    {
        /// <summary>The hero's cards wasted neighbour bonuses in the analysed loop.</summary>
        WastedBonuses = 1,

        /// <summary>
        /// An enemy brought the hero's shield from above zero to zero between the start of the analysed loop and the
        /// turning point.
        /// </summary>
        ShieldBroken = 2,

        /// <summary>A card of the hero produced less than the others in the analysed loop.</summary>
        WeakestCard = 3,
    }
}
