using System;
using Game.Core.Cards;
using Game.Core.SpellLines;

namespace Game.Core.Combat
{
    /// <summary>
    /// One side's entry in a <see cref="Fight"/>: a combatant and the spell line it casts. The hero and enemies use
    /// the same type, because they use the same combat engine (<c>docs/adr/0002-first-pass-combat-rules.md</c>).
    /// </summary>
    public sealed class FightParticipant
    {
        /// <param name="combatant">The combatant, holding its health and shield.</param>
        /// <param name="spellLine">The cards it casts, in order, in a loop. Copied when the fight is created.</param>
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        public FightParticipant(Combatant combatant, SpellLine<CardDefinition> spellLine)
        {
            Combatant = combatant ?? throw new ArgumentNullException(nameof(combatant));
            SpellLine = spellLine ?? throw new ArgumentNullException(nameof(spellLine));
        }

        /// <summary>The combatant. The fight changes its stats.</summary>
        public Combatant Combatant { get; }

        /// <summary>The spell line, as given. The fight casts a copy taken when it is created.</summary>
        public SpellLine<CardDefinition> SpellLine { get; }
    }
}
