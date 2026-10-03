using System.Collections.Generic;
using Game.Core.Effects;

namespace Game.Core.Combat.Recap
{
    /// <summary>
    /// What one combatant's spell line did during a fight, card by card. Immutable. Built by
    /// <see cref="FightRecapBuilder"/>.
    /// </summary>
    public sealed class CombatantRecap
    {
        public CombatantRecap(
            int index,
            IReadOnlyList<CardRecap> cards,
            IReadOnlyList<BonusWaste> wastedBonuses,
            EffectBonus bonusReceived,
            EffectBonus bonusWasted)
        {
            Index = index;
            Cards = cards;
            WastedBonuses = wastedBonuses;
            BonusReceived = bonusReceived;
            BonusWasted = bonusWasted;
        }

        /// <summary>Fight index: <see cref="Fight.HeroIndex"/> for the hero, then 1 to N for the enemies.</summary>
        public int Index { get; }

        /// <summary>True for the hero.</summary>
        public bool IsHero => Index == Fight.HeroIndex;

        /// <summary>
        /// One entry per card of the starting spell line, by position, including cards that never resolved; then,
        /// if the log shows a card at a position that differs from the starting line, one entry for it after the
        /// others (ordered by position, then card id).
        /// </summary>
        public IReadOnlyList<CardRecap> Cards { get; }

        /// <summary>Every wasted bonus of this line, by position, then kind (damage, heal, shield).</summary>
        public IReadOnlyList<BonusWaste> WastedBonuses { get; }

        /// <summary>Total neighbour bonus received by the line's casts.</summary>
        public EffectBonus BonusReceived { get; }

        /// <summary>Total neighbour bonus wasted by the line's casts.</summary>
        public EffectBonus BonusWasted { get; }

        /// <summary>Total neighbour bonus added to the line's effects.</summary>
        public EffectBonus BonusUsed =>
            new EffectBonus(
                BonusReceived.Damage - BonusWasted.Damage,
                BonusReceived.Heal - BonusWasted.Heal,
                BonusReceived.Shield - BonusWasted.Shield);
    }
}
