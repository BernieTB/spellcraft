using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Effects;

namespace Game.Core.Combat
{
    /// <summary>
    /// One resolved cast in a fight: who cast which card, from which position, at which tick, on whom, what its
    /// effects changed, and the neighbour bonus it received and wasted. The raw material for the combat log and
    /// recap.
    /// </summary>
    /// <remarks>
    /// Combatants are identified by their index in the fight: <see cref="Fight.HeroIndex"/> (0) for the hero,
    /// then 1 to N for the enemies in the order they were given.
    /// </remarks>
    public sealed class CastRecord
    {
        /// <summary>Creates the record of a cast that received no neighbour bonus.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> or <paramref name="effectOutcomes"/> is null.</exception>
        public CastRecord(
            int tick,
            int casterIndex,
            int position,
            CardDefinition card,
            int targetIndex,
            IReadOnlyList<EffectOutcome> effectOutcomes)
            : this(tick, casterIndex, position, card, targetIndex, effectOutcomes, EffectBonus.None, EffectBonus.None)
        {
        }

        /// <param name="tick">The tick on which the card resolved.</param>
        /// <param name="casterIndex">Fight index of the caster.</param>
        /// <param name="position">Position of the card in the caster's spell line.</param>
        /// <param name="card">The card that resolved.</param>
        /// <param name="targetIndex">Fight index of the combatant aimed at.</param>
        /// <param name="effectOutcomes">The outcome of each effect, in the card's effect order.</param>
        /// <param name="bonus">The neighbour bonus the cast received.</param>
        /// <param name="wastedBonus">
        /// The part of <paramref name="bonus"/> no effect consumed (see <see cref="EffectContext.RemainingBonus"/>).
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> or <paramref name="effectOutcomes"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="wastedBonus"/> exceeds <paramref name="bonus"/> for some kind.
        /// </exception>
        public CastRecord(
            int tick,
            int casterIndex,
            int position,
            CardDefinition card,
            int targetIndex,
            IReadOnlyList<EffectOutcome> effectOutcomes,
            EffectBonus bonus,
            EffectBonus wastedBonus)
        {
            if (!bonus.Covers(wastedBonus))
            {
                throw new ArgumentException(
                    $"Wasted bonus ({wastedBonus}) exceeds the bonus received ({bonus}).",
                    nameof(wastedBonus));
            }

            Card = card ?? throw new ArgumentNullException(nameof(card));
            EffectOutcomes = effectOutcomes ?? throw new ArgumentNullException(nameof(effectOutcomes));
            Tick = tick;
            CasterIndex = casterIndex;
            Position = position;
            TargetIndex = targetIndex;
            Bonus = bonus;
            WastedBonus = wastedBonus;

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

        /// <summary>
        /// The neighbour bonus the cast received: every bonus that was waiting on its position
        /// (<c>docs/adr/0005-neighbour-modifier-resolution.md</c>). <see cref="EffectBonus.None"/> when there was none.
        /// </summary>
        public EffectBonus Bonus { get; }

        /// <summary>
        /// The part of <see cref="Bonus"/> that no effect of the card consumed, because the card has no effect of
        /// that kind: lost with the cast (ADR 0004). The used part is already included in
        /// <see cref="EffectOutcomes"/>.
        /// </summary>
        public EffectBonus WastedBonus { get; }
    }
}
