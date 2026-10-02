using System;

namespace Game.Core.Effects
{
    /// <summary>
    /// Extra amounts added to the effects of one cast, one value per <see cref="BonusKind"/>. Built by the combat
    /// loop from the neighbour modifiers waiting on a spell-line position, and read by effects through
    /// <see cref="EffectContext"/>. Immutable; values are zero or more.
    /// </summary>
    public readonly struct EffectBonus : IEquatable<EffectBonus>
    {
        /// <exception cref="ArgumentOutOfRangeException">A value is negative.</exception>
        public EffectBonus(int damage, int heal, int shield)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage bonus cannot be negative.");
            }

            if (heal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(heal), heal, "Heal bonus cannot be negative.");
            }

            if (shield < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(shield), shield, "Shield bonus cannot be negative.");
            }

            Damage = damage;
            Heal = heal;
            Shield = shield;
        }

        /// <summary>No bonus.</summary>
        public static EffectBonus None => default;

        /// <summary>Extra damage for a <see cref="DealDamageEffect"/>.</summary>
        public int Damage { get; }

        /// <summary>Extra healing for a <see cref="HealEffect"/>.</summary>
        public int Heal { get; }

        /// <summary>Extra shield for a <see cref="GainShieldEffect"/>.</summary>
        public int Shield { get; }

        /// <summary>Creates a bonus holding <paramref name="amount"/> for <paramref name="kind"/> only.</summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="kind"/> is not a defined kind, or <paramref name="amount"/> is negative.
        /// </exception>
        public static EffectBonus Of(BonusKind kind, int amount)
        {
            switch (kind)
            {
                case BonusKind.Damage:
                    return new EffectBonus(amount, 0, 0);
                case BonusKind.Heal:
                    return new EffectBonus(0, amount, 0);
                case BonusKind.Shield:
                    return new EffectBonus(0, 0, amount);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown bonus kind.");
            }
        }

        /// <summary>The value for <paramref name="kind"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined kind.</exception>
        public int Get(BonusKind kind)
        {
            switch (kind)
            {
                case BonusKind.Damage:
                    return Damage;
                case BonusKind.Heal:
                    return Heal;
                case BonusKind.Shield:
                    return Shield;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown bonus kind.");
            }
        }

        /// <summary>This bonus with the value for <paramref name="kind"/> set to zero.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined kind.</exception>
        public EffectBonus Without(BonusKind kind)
        {
            switch (kind)
            {
                case BonusKind.Damage:
                    return new EffectBonus(0, Heal, Shield);
                case BonusKind.Heal:
                    return new EffectBonus(Damage, 0, Shield);
                case BonusKind.Shield:
                    return new EffectBonus(Damage, Heal, 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown bonus kind.");
            }
        }

        /// <summary>The sum of this bonus and <paramref name="other"/>, value by value.</summary>
        /// <exception cref="OverflowException">A sum exceeds <see cref="int.MaxValue"/>.</exception>
        public EffectBonus Plus(EffectBonus other) =>
            new EffectBonus(
                checked(Damage + other.Damage),
                checked(Heal + other.Heal),
                checked(Shield + other.Shield));

        /// <inheritdoc />
        public bool Equals(EffectBonus other) =>
            Damage == other.Damage && Heal == other.Heal && Shield == other.Shield;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is EffectBonus other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Damage;
                hash = (hash * 397) ^ Heal;
                hash = (hash * 397) ^ Shield;
                return hash;
            }
        }

        /// <inheritdoc />
        public override string ToString() => $"damage+{Damage} heal+{Heal} shield+{Shield}";
    }
}
