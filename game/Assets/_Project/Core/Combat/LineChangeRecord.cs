using System;
using Game.Core.Cards;

namespace Game.Core.Combat
{
    /// <summary>
    /// A <see cref="LineChange"/> the fight applied, with the cards it moved. The raw material for the combat log,
    /// and what a run replays on its own spell line and reserve after the fight.
    /// </summary>
    public sealed class LineChangeRecord
    {
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        public LineChangeRecord(LineChange change, CardDefinition card, CardDefinition incomingCard)
        {
            Change = change ?? throw new ArgumentNullException(nameof(change));
            Card = card ?? throw new ArgumentNullException(nameof(card));
            IncomingCard = incomingCard ?? throw new ArgumentNullException(nameof(incomingCard));
        }

        /// <summary>The change, with its tick.</summary>
        public LineChange Change { get; }

        /// <summary>The line card at <see cref="LineChange.Position"/> before the change: the card that moved, or the
        /// one that left for the reserve.</summary>
        public CardDefinition Card { get; }

        /// <summary>The card that arrived in the line: the reserve card for a swap, the same card as
        /// <see cref="Card"/> for a move.</summary>
        public CardDefinition IncomingCard { get; }
    }
}
