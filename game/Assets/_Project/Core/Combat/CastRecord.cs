using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Effects;

namespace Game.Core.Combat
{
    /// <summary>
    /// One resolved cast in a fight: who cast which card, from which position, at which tick, on whom, and what
    /// its effects changed. The raw material for the combat log and recap.
    /// </summary>
    /// <remarks>
    /// Combatants are identified by their index in the fight: <see cref="Fight.HeroIndex"/> (0) for the hero,
    /// then 1 to N for the enemies in the order they were given.
    /// </remarks>
    public sealed class CastRecord
    {
        /// <exception cref="ArgumentNullException"><paramref name="card"/> or <paramref name="effectOutcomes"/> is null.</exception>
        public CastRecord(
            int tick,
            int casterIndex,
            int position,
            CardDefinition card,
            int targetIndex,
            IReadOnlyList<EffectOutcome> effectOutcomes)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            EffectOutcomes = effectOutcomes ?? throw new ArgumentNullException(nameof(effectOutcomes));
            Tick = tick;
            CasterIndex = casterIndex;
            Position = position;
            TargetIndex = targetIndex;

            var total = EffectOutcome.None;
            foreach (var outcome in effectOutcomes)
            {
                total = total.Plus(outcome);
            }

            Outcome = total;
        }

        /// <summary>The tick on which the card resolved, from 1.</summary>
        public int Tick { get; }

        /// <summary>Fight index of the combatant who cast the card.</summary>
        public int CasterIndex { get; }

        /// <summary>Position of the card in the caster's spell line (zero-based).</summary>
        public int Position { get; }

        /// <summary>The card that resolved.</summary>
        public CardDefinition Card { get; }

        /// <summary>Fight index of the combatant the card was aimed at.</summary>
        public int TargetIndex { get; }

        /// <summary>The outcome of each effect of the card, in the card's effect order.</summary>
        public IReadOnlyList<EffectOutcome> EffectOutcomes { get; }

        /// <summary>Sum of <see cref="EffectOutcomes"/>.</summary>
        public EffectOutcome Outcome { get; }
    }
}
