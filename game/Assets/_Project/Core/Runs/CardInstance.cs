using System;
using Game.Core.Cards;

namespace Game.Core.Runs
{
    /// <summary>
    /// One copy of a card owned during a run. Two copies of the same definition are two instances, so per-copy
    /// state (such as evolution, <c>docs/adr/0013-card-evolution.md</c>) can be tracked separately. An instance
    /// counts its own casts over the whole run, wherever it is (spell line or reserve): its evolution stage follows
    /// from them and from the stages in the card's data. The run updates the count after every fight.
    /// </summary>
    public sealed class CardInstance
    {
        /// <param name="id">Identifier unique within the run, given by the run in the order cards are gained.</param>
        /// <param name="definition">The card's data.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> is less than 1.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="definition"/> is null.</exception>
        public CardInstance(int id, CardDefinition definition)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "Card instance ids start at 1.");
            }

            Id = id;
            Definition = (definition ?? throw new ArgumentNullException(nameof(definition))).AtStage(0);
        }

        /// <summary>Identifier unique within the run.</summary>
        public int Id { get; }

        /// <summary>The card's data, as authored (stage 0).</summary>
        public CardDefinition Definition { get; }

        /// <summary>Total casts of this copy over the run, in every fight so far. Starts at 0.</summary>
        public int Casts { get; private set; }

        /// <summary>The evolution stage this copy has reached: 0 until its first evolution, at most 2.</summary>
        public int Stage => Definition.StageForCasts(Casts);

        /// <summary>The card as this copy is now: the card at its <see cref="Stage"/>.</summary>
        public CardDefinition CurrentDefinition => Definition.AtStage(Stage);

        // Set by the run after a fight, from the counts the fight reports.
        internal void SetCasts(int casts)
        {
            if (casts < Casts)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(casts), casts, $"The casts of {this} cannot go down from {Casts}.");
            }

            Casts = casts;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Definition.Id}#{Id}";
        }
    }
}
