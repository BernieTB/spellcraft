namespace Game.Core.Combat
{
    /// <summary>
    /// What a <see cref="LineChange"/> does to the hero's spell line during a fight. Values are explicit and must
    /// not be renumbered: they may be stored in saved logs.
    /// </summary>
    public enum LineChangeKind
    {
        /// <summary>A card moves to another position of the line; the cards in between shift by one.</summary>
        Move = 1,

        /// <summary>A card of the line and a card of the reserve exchange places.</summary>
        SwapWithReserve = 2,
    }
}
