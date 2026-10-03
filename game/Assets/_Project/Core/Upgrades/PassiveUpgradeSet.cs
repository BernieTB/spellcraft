using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Effects;
using Game.Core.SpellLines;

namespace Game.Core.Upgrades
{
    /// <summary>
    /// The passive upgrades the hero has taken in a run, in the order taken, and how they change the hero's fights
    /// (<c>docs/adr/0012-linked-choices-and-spell-line-editing.md</c>). Immutable: <see cref="With"/> returns a new
    /// set. The same upgrade taken several times stacks: its amounts add up.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Upgrades are applied when the hero's fight participant is built, before the fight is created
    /// (<see cref="ApplyTo(FightParticipant)"/>), so the combat loop itself does not know about them:
    /// </para>
    /// <list type="bullet">
    /// <item>max health and starting shield raise the hero's combatant stats;</item>
    /// <item>an effect amount upgrade raises the amount of <b>every</b> effect of its kind on the hero's cards (a card
    /// with two damage effects gets the bonus on each, so +2X; confirmed by the owner on 2026-10-04), unlike a neighbour
    /// bonus, which only adds to the first effect of its kind in one cast;</item>
    /// <item>a neighbour bonus upgrade raises the amount of every neighbour modifier on the hero's cards, whatever
    /// its kind (confirmed by the owner on 2026-10-04), so a raised bonus that lands on a card without a matching effect
    /// is wasted like any other.</item>
    /// </list>
    /// <para>
    /// Upgraded cards keep their id and cast time, so the combat log shows the card with its upgraded amounts and the
    /// outcomes it really produced. An effect or neighbour modifier whose amount is 0 in data becomes X once upgraded.
    /// Effects that are not <see cref="IAmountEffect"/> are kept unchanged.
    /// </para>
    /// </remarks>
    public sealed class PassiveUpgradeSet
    {
        private readonly PassiveUpgrade[] _upgrades;

        private PassiveUpgradeSet(PassiveUpgrade[] upgrades)
        {
            _upgrades = upgrades;
            Upgrades = new ReadOnlyCollection<PassiveUpgrade>(upgrades);

            var effectBonus = EffectBonus.None;
            foreach (var upgrade in upgrades)
            {
                switch (upgrade.Kind)
                {
                    case PassiveUpgradeKind.MaxHealth:
                        MaxHealthBonus = checked(MaxHealthBonus + upgrade.Amount);
                        break;
                    case PassiveUpgradeKind.StartingShield:
                        StartingShieldBonus = checked(StartingShieldBonus + upgrade.Amount);
                        break;
                    case PassiveUpgradeKind.EffectAmount:
                        effectBonus = effectBonus.Plus(EffectBonus.Of(upgrade.EffectKind, upgrade.Amount));
                        break;
                    case PassiveUpgradeKind.NeighbourBonus:
                        NeighbourBonus = checked(NeighbourBonus + upgrade.Amount);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown passive upgrade kind {upgrade.Kind}.");
                }
            }

            EffectBonus = effectBonus;
        }

        /// <summary>A set with no upgrade: applying it changes nothing.</summary>
        public static PassiveUpgradeSet Empty { get; } = new PassiveUpgradeSet(Array.Empty<PassiveUpgrade>());

        /// <summary>The upgrades taken, in order. The same upgrade can appear several times.</summary>
        public IReadOnlyList<PassiveUpgrade> Upgrades { get; }

        /// <summary>True when the set has no upgrade.</summary>
        public bool IsEmpty => _upgrades.Length == 0;

        /// <summary>Total extra max health.</summary>
        public int MaxHealthBonus { get; }

        /// <summary>Total extra shield at the start of every fight.</summary>
        public int StartingShieldBonus { get; }

        /// <summary>Total extra amount for every effect of each kind the hero casts.</summary>
        public EffectBonus EffectBonus { get; }

        /// <summary>Total extra amount for every neighbour modifier on the hero's cards.</summary>
        public int NeighbourBonus { get; }

        /// <summary>A new set with <paramref name="upgrade"/> added after the current ones.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="upgrade"/> is null.</exception>
        /// <exception cref="OverflowException">A total exceeds <see cref="int.MaxValue"/>.</exception>
        public PassiveUpgradeSet With(PassiveUpgrade upgrade)
        {
            if (upgrade == null)
            {
                throw new ArgumentNullException(nameof(upgrade));
            }

            var upgrades = new PassiveUpgrade[_upgrades.Length + 1];
            Array.Copy(_upgrades, upgrades, _upgrades.Length);
            upgrades[_upgrades.Length] = upgrade;
            return new PassiveUpgradeSet(upgrades);
        }

        /// <summary>
        /// The hero's participant for one fight with every upgrade applied: a new combatant with the extra max health
        /// (at full health) and starting shield, and a new spell line of upgraded cards. The given participant is
        /// not changed.
        /// </summary>
        /// <param name="hero">The hero as built from data, before upgrades, at full health (each fight starts fresh,
        /// <c>docs/adr/0009-vertical-slice-run-pacing.md</c>).</param>
        /// <exception cref="ArgumentNullException"><paramref name="hero"/> is null.</exception>
        /// <exception cref="ArgumentException">The hero's combatant is not at full health.</exception>
        /// <exception cref="OverflowException">An upgraded value exceeds <see cref="int.MaxValue"/>.</exception>
        public FightParticipant ApplyTo(FightParticipant hero)
        {
            if (hero == null)
            {
                throw new ArgumentNullException(nameof(hero));
            }

            var combatant = hero.Combatant;
            if (combatant.CurrentHealth != combatant.MaxHealth)
            {
                throw new ArgumentException("Upgrades apply to a hero at full health, before the fight.", nameof(hero));
            }

            if (IsEmpty)
            {
                return hero;
            }

            var upgraded = new Combatant(
                checked(combatant.MaxHealth + MaxHealthBonus),
                checked(combatant.Shield + StartingShieldBonus));
            return new FightParticipant(upgraded, ApplyTo(hero.SpellLine));
        }

        /// <summary>A new spell line, with the same capacity, holding every card upgraded in the same order.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="line"/> is null.</exception>
        /// <exception cref="OverflowException">An upgraded amount exceeds <see cref="int.MaxValue"/>.</exception>
        public SpellLine<CardDefinition> ApplyTo(SpellLine<CardDefinition> line)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            var upgraded = new SpellLine<CardDefinition>(line.Capacity);
            foreach (var card in line.Cards)
            {
                upgraded.Add(ApplyTo(card));
            }

            return upgraded;
        }

        /// <summary>
        /// The card with its effect amounts and neighbour modifiers upgraded; the same instance when no upgrade
        /// changes cards.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> is null.</exception>
        /// <exception cref="OverflowException">An upgraded amount exceeds <see cref="int.MaxValue"/>.</exception>
        public CardDefinition ApplyTo(CardDefinition card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (EffectBonus.IsNone && NeighbourBonus == 0)
            {
                return card;
            }

            var effects = new List<IEffect>(card.Effects.Count);
            foreach (var effect in card.Effects)
            {
                effects.Add(effect is IAmountEffect amountEffect
                    ? amountEffect.WithAmount(checked(amountEffect.Amount + EffectBonus.Get(amountEffect.Kind)))
                    : effect);
            }

            var modifiers = new List<NeighbourModifier>(card.NeighbourModifiers.Count);
            foreach (var modifier in card.NeighbourModifiers)
            {
                modifiers.Add(new NeighbourModifier(
                    modifier.Kind,
                    modifier.Direction,
                    checked(modifier.Amount + NeighbourBonus)));
            }

            return new CardDefinition(card.Id, card.CastTime, effects, modifiers);
        }
    }
}
