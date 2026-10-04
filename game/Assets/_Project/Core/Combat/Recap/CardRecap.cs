using Game.Core.Effects;

namespace Game.Core.Combat.Recap
{
    /// <summary>
    /// What one card of a spell line did during a fight. Immutable. Built by <see cref="FightRecapBuilder"/>.
    /// </summary>
    public sealed class CardRecap
    {
        public CardRecap(
            int combatantIndex,
            int position,
            string cardId,
            int casts,
            int damage,
            int healing,
            int shieldGained,
            EffectBonus bonusReceived,
            EffectBonus bonusWasted)
        {
            CombatantIndex = combatantIndex;
            Position = position;
            CardId = cardId;
            Casts = casts;
            Damage = damage;
            Healing = healing;
            ShieldGained = shieldGained;
            BonusReceived = bonusReceived;
            BonusWasted = bonusWasted;
        }

        /// <summary>Fight index of the card's owner: <see cref="Fight.HeroIndex"/> for the hero, then the enemies.</summary>
        public int CombatantIndex { get; }

        /// <summary>Position of the card in its owner's spell line (zero-based).</summary>
        public int Position { get; }

        /// <summary>Id of the card.</summary>
        public string CardId { get; }

        /// <summary>Number of casts that resolved.</summary>
        public int Casts { get; }

        /// <summary>Damage dealt: shield absorbed plus health removed, as in the combat log (overkill not counted).</summary>
        public int Damage { get; }

        /// <summary>Health actually restored (healing above max health not counted).</summary>
        public int Healing { get; }

        /// <summary>Shield gained.</summary>
        public int ShieldGained { get; }

        /// <summary>Total neighbour bonus the card's casts received.</summary>
        public EffectBonus BonusReceived { get; }

        /// <summary>
        /// Part of <see cref="BonusReceived"/> that was wasted because the card has no effect of that kind
        /// (<see cref="WastedBonusReason.NoEffectOfKind"/>).
        /// </summary>
        public EffectBonus BonusWasted { get; }

        /// <summary>Part of <see cref="BonusReceived"/> that was added to the card's effects.</summary>
        public EffectBonus BonusUsed =>
            new EffectBonus(
                BonusReceived.Damage - BonusWasted.Damage,
                BonusReceived.Heal - BonusWasted.Heal,
                BonusReceived.Shield - BonusWasted.Shield);

        /// <summary>Damage plus healing plus shield gained: the card's total output, used to find the weakest card.</summary>
        public long Output => (long)Damage + Healing + ShieldGained;
    }
}
