using System;
using Game.Core.Combat;

namespace Game.Core.Effects
{
    /// <summary>
    /// Who an effect acts on when it is applied. Kept minimal on purpose: neighbour modifiers
    /// (<c>docs/adr/0002-first-pass-combat-rules.md</c>) will be added by the neighbour effects work (#13).
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
