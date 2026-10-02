using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Effects;

namespace Game.Core.Cards
{
    /// <summary>
    /// Immutable description of a card, built from data: a stable id, a cast time, an ordered list of effects
    /// and the neighbour modifiers it grants. The card's word and evolution are not modelled yet.
    /// </summary>
    public sealed class CardDefinition
    {
        /// <summary>Creates a card with no neighbour modifiers.</summary>
        /// <param name="id">Stable identifier from data. Not empty or whitespace.</param>
        /// <param name="castTime">Simulation ticks needed to cast the card, from data. Greater than zero.</param>
        /// <param name="effects">Effects applied, in this order, when the card resolves. May be empty.</param>
        /// <exception cref="ArgumentException"><paramref name="id"/> is null, empty or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="castTime"/> is zero or negative.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="effects"/> or one of its items is null.</exception>
        public CardDefinition(string id, int castTime, IEnumerable<IEffect> effects)
            : this(id, castTime, effects, Array.Empty<NeighbourModifier>())
        {
        }

        /// <param name="id">Stable identifier from data. Not empty or whitespace.</param>
        /// <param name="castTime">Simulation ticks needed to cast the card, from data. Greater than zero.</param>
        /// <param name="effects">Effects applied, in this order, when the card resolves. May be empty.</param>
        /// <param name="neighbourModifiers">
        /// Bonuses granted to neighbours each time the card resolves, applied by the combat loop. May be empty.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="id"/> is null, empty or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="castTime"/> is zero or negative.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="effects"/>, <paramref name="neighbourModifiers"/> or one of their items is null.
        /// </exception>
        public CardDefinition(
            string id,
            int castTime,
            IEnumerable<IEffect> effects,
            IEnumerable<NeighbourModifier> neighbourModifiers)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Card id cannot be null, empty or whitespace.", nameof(id));
            }

            if (castTime <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(castTime), castTime, "Cast time must be greater than zero ticks.");
            }

            if (effects == null)
            {
                throw new ArgumentNullException(nameof(effects));
            }

            var copy = new List<IEffect>(effects);
            if (copy.Contains(null))
            {
                throw new ArgumentNullException(nameof(effects), "A card cannot contain a null effect.");
            }

            if (neighbourModifiers == null)
            {
                throw new ArgumentNullException(nameof(neighbourModifiers));
            }

            var modifiers = new List<NeighbourModifier>(neighbourModifiers);
            if (modifiers.Contains(null))
            {
                throw new ArgumentNullException(nameof(neighbourModifiers), "A card cannot contain a null neighbour modifier.");
            }

            Id = id;
            CastTime = castTime;
            Effects = new ReadOnlyCollection<IEffect>(copy);
            NeighbourModifiers = new ReadOnlyCollection<NeighbourModifier>(modifiers);
        }

        /// <summary>
        /// Stable identifier, unique among card definitions.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Simulation ticks needed to cast the card. Used by the combat loop, not by the card itself.
        /// </summary>
        public int CastTime { get; }

        /// <summary>
        /// Effects of the card, in resolution order.
        /// </summary>
        public IReadOnlyList<IEffect> Effects { get; }

        /// <summary>
        /// Bonuses this card grants to its neighbours in the spell line each time it resolves. Applied by the combat
        /// loop, not by <see cref="Resolve"/>: they belong to positions in a spell line, which a card does not know.
        /// </summary>
        public IReadOnlyList<NeighbourModifier> NeighbourModifiers { get; }

        /// <summary>
        /// Applies every effect of the card, in order, to the combatants of <paramref name="context"/>.
        /// </summary>
        /// <returns>The outcome of each effect, in the same order as <see cref="Effects"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
        public IReadOnlyList<EffectOutcome> Resolve(EffectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var outcomes = new EffectOutcome[Effects.Count];
            for (var i = 0; i < Effects.Count; i++)
            {
                outcomes[i] = Effects[i].Apply(context);
            }

            return new ReadOnlyCollection<EffectOutcome>(outcomes);
        }
    }
}
