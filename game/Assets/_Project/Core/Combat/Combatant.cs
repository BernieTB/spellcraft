using System;

namespace Game.Core.Combat
{
    /// <summary>
    /// A participant in a fight (the player character or an enemy). Holds the combat stats and the rules that
    /// change them. Stat values come from data passed to the constructor; nothing here is a balance number.
    /// </summary>
    /// <remarks>
    /// Stats are kept to what the first cards need: health and shield. See <c>docs/GLOSSARY.md</c> for the
    /// definition of each stat.
    /// </remarks>
    public sealed class Combatant
    {
        /// <summary>
        /// Creates a combatant at full health.
        /// </summary>
        /// <param name="maxHealth">Maximum health, from data. Must be greater than zero.</param>
        /// <param name="shield">Starting shield, from data. Zero or more.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="maxHealth"/> is zero or negative, or <paramref name="shield"/> is negative.
        /// </exception>
        public Combatant(int maxHealth, int shield)
        {
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), maxHealth, "Max health must be greater than zero.");
            }

            if (shield < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(shield), shield, "Shield cannot be negative.");
            }

            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            Shield = shield;
        }

        /// <summary>
        /// Upper bound of <see cref="CurrentHealth"/>. Healing never goes above it.
        /// </summary>
        public int MaxHealth { get; }

        /// <summary>
        /// Remaining health, between zero and <see cref="MaxHealth"/>.
        /// </summary>
        public int CurrentHealth { get; private set; }

        /// <summary>
        /// Damage absorbed before health is reduced. Zero or more, with no upper bound.
        /// </summary>
        public int Shield { get; private set; }

        /// <summary>
        /// True once <see cref="CurrentHealth"/> has reached zero.
        /// </summary>
        public bool IsDead => CurrentHealth == 0;

        /// <summary>
        /// Applies damage: shield absorbs it first, the remainder reduces health. Health never goes below zero;
        /// reaching zero kills the combatant. Damage beyond shield and health is lost. A dead combatant takes no
        /// further damage.
        /// </summary>
        /// <param name="amount">Damage to apply. Zero is allowed and does nothing.</param>
        /// <returns>How the damage was split between shield and health.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/> is negative.</exception>
        public DamageResult TakeDamage(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Damage cannot be negative. Use Heal to restore health.");
            }

            if (IsDead)
            {
                return new DamageResult(0, 0);
            }

            var absorbed = Math.Min(amount, Shield);
            Shield -= absorbed;

            var healthLost = Math.Min(amount - absorbed, CurrentHealth);
            CurrentHealth -= healthLost;

            return new DamageResult(absorbed, healthLost);
        }

        /// <summary>
        /// Restores health, capped at <see cref="MaxHealth"/>. Does not restore shield. Healing a dead combatant
        /// does nothing: death is final (placeholder rule, no revival mechanic is designed).
        /// </summary>
        /// <param name="amount">Healing to apply. Zero is allowed and does nothing.</param>
        /// <returns>The health actually restored (less than <paramref name="amount"/> when it overheals).</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/> is negative.</exception>
        public int Heal(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Healing cannot be negative. Use TakeDamage to remove health.");
            }

            if (IsDead)
            {
                return 0;
            }

            var healed = Math.Min(amount, MaxHealth - CurrentHealth);
            CurrentHealth += healed;
            return healed;
        }

        /// <summary>
        /// Adds shield. Shield has no upper bound. A dead combatant gains no shield.
        /// </summary>
        /// <param name="amount">Shield to add. Zero is allowed and does nothing.</param>
        /// <returns>The shield actually added (zero when dead).</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/> is negative.</exception>
        /// <exception cref="OverflowException">The resulting shield exceeds <see cref="int.MaxValue"/>.</exception>
        public int GainShield(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Shield gain cannot be negative. Use TakeDamage to remove shield.");
            }

            if (IsDead)
            {
                return 0;
            }

            Shield = checked(Shield + amount);
            return amount;
        }
    }
}
