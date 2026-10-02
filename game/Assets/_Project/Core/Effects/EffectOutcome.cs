using Game.Core.Combat;

namespace Game.Core.Effects
{
    /// <summary>
    /// What applying an effect actually changed, for the fight result and, later, the combat log and recap.
    /// Values are the amounts really applied (after shield, health and death rules), not the amounts asked for.
    /// </summary>
    /// <remarks>
    /// Who each part lands on is fixed per effect kind for now (<c>docs/adr/0003-effect-representation.md</c>):
    /// <see cref="Damage"/> is taken by the target, <see cref="Healed"/> and <see cref="ShieldGained"/> go to the
    /// caster. Outcomes can be summed with <see cref="Plus"/>, for example to total a card's effects.
    /// </remarks>
    public readonly struct EffectOutcome
    {
        /// <param name="damage">Damage applied to the target, split between shield and health.</param>
        /// <param name="healed">Health actually restored to the caster.</param>
        /// <param name="shieldGained">Shield actually added to the caster.</param>
        public EffectOutcome(DamageResult damage, int healed, int shieldGained)
        {
            Damage = damage;
            Healed = healed;
            ShieldGained = shieldGained;
        }

        /// <summary>An outcome where nothing changed.</summary>
        public static EffectOutcome None => default;

        /// <summary>Damage applied to the target: absorbed by its shield, then removed from its health.</summary>
        public DamageResult Damage { get; }

        /// <summary>Health actually restored to the caster (overhealing is not counted).</summary>
        public int Healed { get; }

        /// <summary>Shield actually added to the caster.</summary>
        public int ShieldGained { get; }

        /// <summary>Creates an outcome holding only damage.</summary>
        public static EffectOutcome FromDamage(DamageResult damage) => new EffectOutcome(damage, 0, 0);

        /// <summary>Creates an outcome holding only healing.</summary>
        public static EffectOutcome FromHeal(int healed) => new EffectOutcome(default, healed, 0);

        /// <summary>Creates an outcome holding only a shield gain.</summary>
        public static EffectOutcome FromShieldGain(int shieldGained) => new EffectOutcome(default, 0, shieldGained);

        /// <summary>Returns the sum of this outcome and <paramref name="other"/>, field by field.</summary>
        public EffectOutcome Plus(EffectOutcome other) =>
            new EffectOutcome(
                new DamageResult(
                    Damage.AbsorbedByShield + other.Damage.AbsorbedByShield,
                    Damage.HealthLost + other.Damage.HealthLost),
                Healed + other.Healed,
                ShieldGained + other.ShieldGained);
    }
}
