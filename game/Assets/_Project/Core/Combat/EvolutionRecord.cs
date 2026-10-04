using System;
using Game.Core.Cards;

namespace Game.Core.Combat
{
    /// <summary>
    /// A card of the hero's that evolved during a fight (<c>docs/adr/0013-card-evolution.md</c>): the cast that
    /// reached the stage, the stage and the total casts of that card copy.
    /// </summary>
    public sealed class EvolutionRecord
    {
        /// <param name="tick">The tick of the cast that reached the stage.</param>
        /// <param name="position">
        /// The position of that cast in the hero's line (the slot the card left, if it was swapped out while casting).
        /// </param>
        /// <param name="card">The card at its new stage: same id and cast time as before.</param>
        /// <param name="casts">Total casts of this card copy over the run, the cast just resolved included.</param>
        /// <param name="castIndex">Index of the cast in <see cref="FightResult.Casts"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// A number is out of range, or the card's stage is 0 (it did not evolve).
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> is null.</exception>
        public EvolutionRecord(int tick, int position, CardDefinition card, int casts, int castIndex)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            if (tick < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(tick), tick, "Ticks start at 1.");
            }

            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), position, "Position cannot be negative.");
            }

            if (card.Stage < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(card), card.Stage, "An evolved card is at stage 1 or more.");
            }

            if (casts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(casts), casts, "A card that evolved has been cast.");
            }

            if (castIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(castIndex), castIndex, "Cast index cannot be negative.");
            }

            Tick = tick;
            Position = position;
            Casts = casts;
            CastIndex = castIndex;
        }

        /// <summary>The tick of the cast that reached the stage.</summary>
        public int Tick { get; }

        /// <summary>The position of that cast in the hero's line.</summary>
        public int Position { get; }

        /// <summary>The card at its new stage.</summary>
        public CardDefinition Card { get; }

        /// <summary>The stage reached, 1 or 2.</summary>
        public int Stage => Card.Stage;

        /// <summary>Total casts of this card copy over the run, the cast just resolved included.</summary>
        public int Casts { get; }

        /// <summary>Index, in <see cref="FightResult.Casts"/>, of the cast after which the card evolved.</summary>
        public int CastIndex { get; }
    }
}
