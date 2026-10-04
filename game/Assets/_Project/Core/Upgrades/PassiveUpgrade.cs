using System;
using Game.Core.Effects;

namespace Game.Core.Upgrades
{
    /// <summary>
    /// One passive upgrade of the hero, the non-card half of a level-up linked choice: an immutable improvement that
    /// lasts for the rest of the run (<c>docs/adr/0012-linked-choices-and-spell-line-editing.md</c>). Built from data;
    /// applied through a <see cref="PassiveUpgradeSet"/>.
    /// </summary>
    public sealed class PassiveUpgrade
    {
        /// <summary>Creates an upgrade that does not depend on an effect kind (max health, starting shield, neighbour bonus).</summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="id"/> is null, empty or whitespace, or <paramref name="kind"/> is
        /// <see cref="PassiveUpgradeKind.EffectAmount"/>, which needs an effect kind.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="kind"/> is not a defined value, or <paramref name="amount"/> is negative.
        /// </exception>
        public PassiveUpgrade(string id, PassiveUpgradeKind kind, int amount)
            : this(id, kind, BonusKind.Damage, amount)
        {
            if (kind == PassiveUpgradeKind.EffectAmount)
            {
                throw new ArgumentException("An effect amount upgrade needs an effect kind.", nameof(kind));
            }
        }

        /// <param name="id">Stable identifier from data. Not empty or whitespace.</param>
        /// <param name="kind">What the upgrade improves.</param>
        /// <param name="effectKind">
        /// The effect kind raised by a <see cref="PassiveUpgradeKind.EffectAmount"/> upgrade. Ignored by other kinds.
        /// </param>
        /// <param name="amount">The X of the upgrade, from data. Zero or more.</param>
        /// <exception cref="ArgumentException"><paramref name="id"/> is null, empty or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="kind"/> or <paramref name="effectKind"/> is not a defined value, or
        /// <paramref name="amount"/> is negative.
        /// </exception>
        public PassiveUpgrade(string id, PassiveUpgradeKind kind, BonusKind effectKind, int amount)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Passive upgrade id cannot be null, empty or whitespace.", nameof(id));
            }

            if (!Enum.IsDefined(typeof(PassiveUpgradeKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown passive upgrade kind.");
            }

            if (!Enum.IsDefined(typeof(BonusKind), effectKind))
            {
                throw new ArgumentOutOfRangeException(nameof(effectKind), effectKind, "Unknown effect kind.");
            }

            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Passive upgrade amount cannot be negative.");
            }

            Id = id;
            Kind = kind;
            EffectKind = effectKind;
            Amount = amount;
        }

        /// <summary>Stable identifier, unique among passive upgrades.</summary>
        public string Id { get; }

        /// <summary>What the upgrade improves.</summary>
        public PassiveUpgradeKind Kind { get; }

        /// <summary>
        /// The effect kind raised by a <see cref="PassiveUpgradeKind.EffectAmount"/> upgrade. Meaningless for other
        /// kinds.
        /// </summary>
        public BonusKind EffectKind { get; }

        /// <summary>The X of the upgrade.</summary>
        public int Amount { get; }
    }
}
