using System;
using Game.Core.Combat;

namespace Game.Core.Effects
{
    /// <summary>
    /// Who an effect acts on when it is applied. Kept minimal on purpose: targeting, timing and neighbour
    /// information will be added when those rules are designed (spike #8).
    /// </summary>
    public sealed class EffectContext
    {
        /// <param name="caster">The combatant casting the card.</param>
        /// <param name="target">The combatant the card is aimed at.</param>
        /// <exception cref="ArgumentNullException">A combatant is null.</exception>
        public EffectContext(Combatant caster, Combatant target)
        {
            Caster = caster ?? throw new ArgumentNullException(nameof(caster));
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        /// <summary>
        /// The combatant casting the card.
        /// </summary>
        public Combatant Caster { get; }

        /// <summary>
        /// The combatant the card is aimed at.
        /// </summary>
        public Combatant Target { get; }
    }
}
