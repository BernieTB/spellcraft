using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Effects;

namespace Game.Core.Cards
{
    /// <summary>
    /// Immutable description of a card, built from data: a stable id, a cast time and an ordered list of
    /// effects. The card's word, neighbour rules and evolution are not modelled yet.
    /// </summary>
    public sealed class CardDefinition
    {
        /// <param name="id">Stable identifier from data. Not empty or whitespace.</param>
        /// <param name="castTime">Simulation ticks needed to cast the card, from data. Greater than zero.</param>
        /// <param name="effects">Effects applied, in this order, when the card resolves. May be empty.</param>
        /// <exception cref="ArgumentException"><paramref name="id"/> is null, empty or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="castTime"/> is zero or negative.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="effects"/> or one of its items is null.</exception>
        public CardDefinition(string id, int castTime, IEnumerable<IEffect> effects)
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

            Id = id;
            CastTime = castTime;
            Effects = new ReadOnlyCollection<IEffect>(copy);
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
