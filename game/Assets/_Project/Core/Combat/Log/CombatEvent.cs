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
        /// Creates an event; <paramref name="bonus"/> and <paramref name="wastedBonus"/> are only allowed on
        /// <see cref="CombatEventKind.CardCast"/> (see <see cref="Bonus"/>).
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="cardId"/> is null or empty, <paramref name="wastedBonus"/> exceeds
        /// <paramref name="bonus"/> for some kind, or an event other than a cast has a bonus.
        /// </exception>
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
            if (kind == CombatEventKind.LineChanged)
            {
                throw new ArgumentException("Create a line change event with ForLineChange.", nameof(kind));
            }

            if (kind == CombatEventKind.Evolved)
            {
                throw new ArgumentException("Create an evolution event with ForEvolution.", nameof(kind));
            }

            if (string.IsNullOrEmpty(cardId))
            {
                throw new ArgumentException("Card id cannot be null or empty.", nameof(cardId));
            }

            if (!bonus.Covers(wastedBonus))
            {
                throw new ArgumentException(
                    $"Wasted bonus ({wastedBonus}) exceeds the bonus received ({bonus}).",
                    nameof(wastedBonus));
            }

            if (kind != CombatEventKind.CardCast && !bonus.IsNone)
            {
                throw new ArgumentException("Only a cast event can carry a neighbour bonus.", nameof(bonus));
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

        private CombatEvent(
            int sequence,
            LineChange change,
            string cardId,
            string incomingCardId,
            int heroHealth,
            int heroShield)
        {
            Sequence = sequence;
            Tick = change.Tick;
            Kind = CombatEventKind.LineChanged;
            CardId = cardId;
            Position = change.Position;
            CasterIndex = Fight.HeroIndex;
            TargetIndex = Fight.HeroIndex;
            TargetHealth = heroHealth;
            TargetShield = heroShield;
            Bonus = EffectBonus.None;
            WastedBonus = EffectBonus.None;
            LineChange = change;
            IncomingCardId = incomingCardId;
        }

        private CombatEvent(
            int sequence,
            int tick,
            string cardId,
            int position,
            int stage,
            int casts,
            int heroHealth,
            int heroShield)
        {
            Sequence = sequence;
            Tick = tick;
            Kind = CombatEventKind.Evolved;
            CardId = cardId;
            Position = position;
            CasterIndex = Fight.HeroIndex;
            TargetIndex = Fight.HeroIndex;
            TargetHealth = heroHealth;
            TargetShield = heroShield;
            Bonus = EffectBonus.None;
            WastedBonus = EffectBonus.None;
            EvolutionStage = stage;
            EvolutionCasts = casts;
        }

        /// <summary>
        /// Creates a <see cref="CombatEventKind.LineChanged"/> event: the player changed the hero's line. Its
        /// <see cref="Position"/> and <see cref="CardId"/> are the line card that moved or left for the reserve; the
        /// hero is both <see cref="CasterIndex"/> and <see cref="TargetIndex"/>, and the amounts are zero.
        /// </summary>
        /// <param name="sequence">Index of the event in its log.</param>
        /// <param name="change">The change, with its tick.</param>
        /// <param name="cardId">
        /// Id of the line card at <see cref="Game.Core.Combat.LineChange.Position"/> before the change.
        /// </param>
        /// <param name="incomingCardId">Id of the card that arrived in the line (the same card for a move).</param>
        /// <param name="heroHealth">The hero's health at that moment.</param>
        /// <param name="heroShield">The hero's shield at that moment.</param>
        /// <exception cref="ArgumentNullException"><paramref name="change"/> is null.</exception>
        /// <exception cref="ArgumentException">A card id is null or empty.</exception>
        public static CombatEvent ForLineChange(
            int sequence,
            LineChange change,
            string cardId,
            string incomingCardId,
            int heroHealth,
            int heroShield)
        {
            if (change == null)
            {
                throw new ArgumentNullException(nameof(change));
            }

            if (string.IsNullOrEmpty(cardId))
            {
                throw new ArgumentException("Card id cannot be null or empty.", nameof(cardId));
            }

            if (string.IsNullOrEmpty(incomingCardId))
            {
                throw new ArgumentException("Card id cannot be null or empty.", nameof(incomingCardId));
            }

            return new CombatEvent(sequence, change, cardId, incomingCardId, heroHealth, heroShield);
        }

        /// <summary>
        /// Creates a <see cref="CombatEventKind.Evolved"/> event: a card of the hero's reached an evolution stage
        /// with the cast just logged. Its <see cref="Position"/> and <see cref="CardId"/> are those of that cast; the
        /// hero is both <see cref="CasterIndex"/> and <see cref="TargetIndex"/>, and the amounts are zero.
        /// </summary>
        /// <param name="sequence">Index of the event in its log.</param>
        /// <param name="tick">The tick of the cast.</param>
        /// <param name="cardId">Id of the card (it does not change when it evolves).</param>
        /// <param name="position">Position of the cast in the hero's line.</param>
        /// <param name="stage">The stage reached, 1 or more.</param>
        /// <param name="casts">Total casts of that card copy over the run, the cast just logged included.</param>
        /// <param name="heroHealth">The hero's health at that moment.</param>
        /// <param name="heroShield">The hero's shield at that moment.</param>
        /// <exception cref="ArgumentException">The card id is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The stage or the casts are less than 1.</exception>
        public static CombatEvent ForEvolution(
            int sequence,
            int tick,
            string cardId,
            int position,
            int stage,
            int casts,
            int heroHealth,
            int heroShield)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                throw new ArgumentException("Card id cannot be null or empty.", nameof(cardId));
            }

            if (stage < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(stage), stage, "An evolution reaches stage 1 or more.");
            }

            if (casts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(casts), casts, "A card that evolved has been cast.");
            }

            return new CombatEvent(sequence, tick, cardId, position, stage, casts, heroHealth, heroShield);
        }

        /// <summary>
        /// The change to the hero's line, for <see cref="CombatEventKind.LineChanged"/>; null for every other kind.
        /// </summary>
        public LineChange LineChange { get; }

        /// <summary>
        /// Id of the card that arrived in the line, for <see cref="CombatEventKind.LineChanged"/> (the reserve card
        /// for a swap, <see cref="CardId"/> for a move); null for every other kind.
        /// </summary>
        public string IncomingCardId { get; }

        /// <summary>
        /// The stage a card reached, for <see cref="CombatEventKind.Evolved"/> (1 or 2); 0 for every other kind.
        /// </summary>
        public int EvolutionStage { get; }

        /// <summary>
        /// Total casts of the card copy that evolved, for <see cref="CombatEventKind.Evolved"/>; 0 for every other
        /// kind.
        /// </summary>
        public int EvolutionCasts { get; }

        /// <summary>Index of the event in its log, from 0. Events are stored in this order.</summary>
        public int Sequence { get; }

        /// <summary>The tick on which the cast resolved, or the line changed, from 1.</summary>
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
