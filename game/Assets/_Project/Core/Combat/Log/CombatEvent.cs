using System;
using Game.Core.Effects;

namespace Game.Core.Combat.Log
{
    /// <summary>
    /// One entry of a <see cref="CombatLog"/>. Immutable. Every event is tied to the cast that caused it: its tick,
    /// the card, the card's position in the caster's spell line, the caster and the combatant the event concerns.
    /// </summary>
    /// <remarks>
    /// Combatants are identified by their fight index: <see cref="Fight.HeroIndex"/> (0) for the hero, then 1 to N
    /// for the enemies in the order they were given.
    /// </remarks>
    public sealed class CombatEvent
    {
        /// <summary>Creates an event with no neighbour bonus.</summary>
        /// <exception cref="ArgumentException"><paramref name="cardId"/> is null or empty.</exception>
        public CombatEvent(
            int sequence,
            int tick,
            CombatEventKind kind,
            string cardId,
            int position,
            int casterIndex,
            int targetIndex,
            int amount,
            int absorbedByShield,
            int healthLost,
            int targetHealth,
            int targetShield)
            : this(
                sequence,
                tick,
                kind,
                cardId,
                position,
                casterIndex,
                targetIndex,
                amount,
                absorbedByShield,
                healthLost,
                targetHealth,
                targetShield,
                EffectBonus.None,
                EffectBonus.None)
        {
        }

        /// <summary>
        /// Creates an event; <paramref name="bonus"/> and <paramref name="wastedBonus"/> are meant for
        /// <see cref="CombatEventKind.CardCast"/> (see <see cref="Bonus"/>).
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="cardId"/> is null or empty.</exception>
        public CombatEvent(
            int sequence,
            int tick,
            CombatEventKind kind,
            string cardId,
            int position,
            int casterIndex,
            int targetIndex,
            int amount,
            int absorbedByShield,
            int healthLost,
            int targetHealth,
            int targetShield,
            EffectBonus bonus,
            EffectBonus wastedBonus)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                throw new ArgumentException("Card id cannot be null or empty.", nameof(cardId));
            }

            Sequence = sequence;
            Tick = tick;
            Kind = kind;
            CardId = cardId;
            Position = position;
            CasterIndex = casterIndex;
            TargetIndex = targetIndex;
            Amount = amount;
            AbsorbedByShield = absorbedByShield;
            HealthLost = healthLost;
            TargetHealth = targetHealth;
            TargetShield = targetShield;
            Bonus = bonus;
            WastedBonus = wastedBonus;
        }

        /// <summary>Index of the event in its log, from 0. Events are stored in this order.</summary>
        public int Sequence { get; }

        /// <summary>The tick on which the cast resolved, from 1.</summary>
        public int Tick { get; }

        /// <summary>What happened.</summary>
        public CombatEventKind Kind { get; }

        /// <summary>Id of the card whose cast caused the event.</summary>
        public string CardId { get; }

        /// <summary>Position of that card in the caster's spell line (zero-based).</summary>
        public int Position { get; }

        /// <summary>Fight index of the combatant who cast the card.</summary>
        public int CasterIndex { get; }

        /// <summary>
        /// Fight index of the combatant the event concerns: the combatant aimed at for
        /// <see cref="CombatEventKind.CardCast"/>, the damaged one for <see cref="CombatEventKind.Damage"/>, the
        /// caster for <see cref="CombatEventKind.Heal"/> and <see cref="CombatEventKind.ShieldGain"/>, and the dead
        /// one for <see cref="CombatEventKind.Death"/>.
        /// </summary>
        public int TargetIndex { get; }

        /// <summary>
        /// Amount actually applied: total damage (shield absorbed plus health lost), health restored or shield
        /// gained. Zero for <see cref="CombatEventKind.CardCast"/> and <see cref="CombatEventKind.Death"/>.
        /// </summary>
        public int Amount { get; }

        /// <summary>Damage absorbed by the target's shield. Zero except for <see cref="CombatEventKind.Damage"/>.</summary>
        public int AbsorbedByShield { get; }

        /// <summary>Damage removed from the target's health. Zero except for <see cref="CombatEventKind.Damage"/>.</summary>
        public int HealthLost { get; }

        /// <summary>Health of <see cref="TargetIndex"/> right after the event.</summary>
        public int TargetHealth { get; }

        /// <summary>Shield of <see cref="TargetIndex"/> right after the event.</summary>
        public int TargetShield { get; }

        /// <summary>
        /// The neighbour bonus the cast received (<see cref="CastRecord.Bonus"/>). Set only on
        /// <see cref="CombatEventKind.CardCast"/>; <see cref="EffectBonus.None"/> on other kinds and on casts that
        /// received no bonus. The used part is already included in the amounts of the cast's effect events.
        /// </summary>
        public EffectBonus Bonus { get; }

        /// <summary>
        /// The part of <see cref="Bonus"/> the cast wasted because the card has no effect of that kind
        /// (<see cref="CastRecord.WastedBonus"/>). Set only on <see cref="CombatEventKind.CardCast"/>.
        /// </summary>
        public EffectBonus WastedBonus { get; }
    }
}
