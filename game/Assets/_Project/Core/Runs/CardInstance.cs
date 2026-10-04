using System;
using Game.Core.Cards;

namespace Game.Core.Runs
{
    /// <summary>
    /// One copy of a card owned during a run. Two copies of the same definition are two instances, so per-copy
    /// state (such as evolution, <c>docs/adr/0013-card-evolution.md</c>) can be tracked separately.
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
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        /// <summary>Identifier unique within the run.</summary>
        public int Id { get; }

        /// <summary>The card's data.</summary>
        public CardDefinition Definition { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Definition.Id}#{Id}";
        }
    }
}
