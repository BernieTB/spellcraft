using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Effects;

namespace Game.Core.Cards
{
    /// <summary>
    /// One evolution stage of a card (<c>docs/adr/0013-card-evolution.md</c>): the number of casts of a card copy
    /// that reaches the stage, and what the card is at that stage. A stage replaces the card's effects and neighbour
    /// modifiers; the id (the word) and the cast time never change, so they are not part of a stage.
    /// </summary>
    public sealed class CardEvolution
    {
        /// <param name="castsRequired">
        /// Total casts of one card copy, over the whole run, needed to reach this stage. At least 1.
        /// </param>
        /// <param name="effects">Effects applied, in this order, when the evolved card resolves. May be empty.</param>
        /// <param name="neighbourModifiers">Bonuses the evolved card grants to its neighbours. May be empty.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="castsRequired"/> is less than 1.</exception>
        /// <exception cref="ArgumentNullException">An argument or one of its items is null.</exception>
        public CardEvolution(
            int castsRequired,
            IEnumerable<IEffect> effects,
            IEnumerable<NeighbourModifier> neighbourModifiers)
        {
            if (castsRequired < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(castsRequired), castsRequired, "An evolution needs at least one cast.");
            }

            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            if (neighbourModifiers == null)
            {
                throw new ArgumentNullException(nameof(neighbourModifiers));
            }

            var effectList = new List<IEffect>(effects);
            if (effectList.Contains(null))
            {
                throw new ArgumentNullException(nameof(effects), "An evolution cannot contain a null effect.");
            }

            var modifierList = new List<NeighbourModifier>(neighbourModifiers);
            if (modifierList.Contains(null))
            {
                throw new ArgumentNullException(
                    nameof(neighbourModifiers), "An evolution cannot contain a null neighbour modifier.");
            }

            CastsRequired = castsRequired;
            Effects = new ReadOnlyCollection<IEffect>(effectList);
            NeighbourModifiers = new ReadOnlyCollection<NeighbourModifier>(modifierList);
        }

        /// <summary>Total casts of one card copy needed to reach this stage.</summary>
        public int CastsRequired { get; }

        /// <summary>Effects of the card at this stage, in resolution order.</summary>
        public IReadOnlyList<IEffect> Effects { get; }

        /// <summary>Bonuses the card grants to its neighbours at this stage.</summary>
        public IReadOnlyList<NeighbourModifier> NeighbourModifiers { get; }
    }
}
