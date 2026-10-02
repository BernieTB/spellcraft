namespace Game.Core.Cards
{
    /// <summary>
    /// Which neighbour of its card a <see cref="NeighbourModifier"/> applies to, in the card's own spell line.
    /// Values are explicit because card assets serialize them as integers: never renumber or reuse one.
    /// </summary>
    public enum NeighbourDirection
    {
        /// <summary>The card played after it (the first card, after the last one).</summary>
        Next = 0,

        /// <summary>The card played before it (the last card, before the first one).</summary>
        Previous = 1,
    }
}
