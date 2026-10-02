using System;
using Game.Core.Combat;

namespace Game.Core.Effects
{
    /// <summary>
    /// The state one card cast works with: who casts it, who it is aimed at, and the neighbour bonus granted to
    /// this cast (<c>docs/adr/0005-neighbour-modifier-resolution.md</c>). Create one context per cast.
    /// </summary>
    /// <remarks>
    /// The bonus is consumed by the card's effects: the first effect of a kind takes the whole bonus of that kind
    /// through <see cref="ConsumeBonus"/>, so a cast gets each bonus once, however many effects of that kind the
    /// card has. A bonus with no matching effect is never consumed and does nothing.
    /// </remarks>
    public sealed class EffectContext
    {
        private EffectBonus _remainingBonus;

        /// <summary>Creates a context with no bonus.</summary>
        /// <param name="caster">The combatant casting the card.</param>
        /// <param name="target">The combatant the card is aimed at.</param>
        /// <exception cref="ArgumentNullException">A combatant is null.</exception>
        public EffectContext(Combatant caster, Combatant target)
            : this(caster, target, EffectBonus.None)
        {
        }

        /// <param name="caster">The combatant casting the card.</param>
        /// <param name="target">The combatant the card is aimed at.</param>
        /// <param name="bonus">Neighbour bonus granted to this cast.</param>
        /// <exception cref="ArgumentNullException">A combatant is null.</exception>
        public EffectContext(Combatant caster, Combatant target, EffectBonus bonus)
        {
            Caster = caster ?? throw new ArgumentNullException(nameof(caster));
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Bonus = bonus;
            _remainingBonus = bonus;
        }

        /// <summary>
        /// The combatant casting the card.
        /// </summary>
        public Combatant Caster { get; }

        /// <summary>
        /// The combatant the card is aimed at.
        /// </summary>
        public Combatant Target { get; }

        /// <summary>
        /// The neighbour bonus granted to this cast, as given (not reduced by <see cref="ConsumeBonus"/>).
        /// </summary>
        public EffectBonus Bonus { get; }

        /// <summary>
        /// The part of <see cref="Bonus"/> not consumed yet by <see cref="ConsumeBonus"/>. Once the card has
        /// resolved, this is the wasted bonus: kinds for which the card has no matching effect.
        /// </summary>
        public EffectBonus RemainingBonus => _remainingBonus;

        /// <summary>
        /// Takes the bonus of <paramref name="kind"/> not consumed yet in this context. An effect calls it to get
        /// the extra amount to add; later calls for the same kind return zero.
        /// </summary>
        /// <returns>The remaining bonus of that kind, zero or more.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined kind.</exception>
        public int ConsumeBonus(BonusKind kind)
        {
            var amount = _remainingBonus.Get(kind);
            _remainingBonus = _remainingBonus.Without(kind);
            return amount;
        }
    }
}
