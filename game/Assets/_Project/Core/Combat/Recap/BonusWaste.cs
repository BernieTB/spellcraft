using Game.Core.Effects;

namespace Game.Core.Combat.Recap
{
    /// <summary>
    /// Neighbour bonus of one kind wasted by one card over a fight, and why. Immutable. Built by
    /// <see cref="FightRecapBuilder"/>.
    /// </summary>
    public sealed class BonusWaste
    {
        public BonusWaste(int combatantIndex, int position, string cardId, BonusKind kind, int amount, WastedBonusReason reason)
        {
            CombatantIndex = combatantIndex;
            Position = position;
            CardId = cardId;
            Kind = kind;
            Amount = amount;
            Reason = reason;
        }

        /// <summary>Fight index of the card's owner.</summary>
        public int CombatantIndex { get; }

        /// <summary>Position of the receiving card in its owner's spell line (zero-based).</summary>
        public int Position { get; }

        /// <summary>Id of the receiving card.</summary>
        public string CardId { get; }

        /// <summary>Kind of the wasted bonus.</summary>
        public BonusKind Kind { get; }

        /// <summary>Total amount wasted over the fight.</summary>
        public int Amount { get; }

        /// <summary>Why it was wasted.</summary>
        public WastedBonusReason Reason { get; }
    }
}
