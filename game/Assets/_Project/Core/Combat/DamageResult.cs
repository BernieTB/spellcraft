namespace Game.Core.Combat
{
    /// <summary>
    /// Outcome of <see cref="Combatant.TakeDamage"/>: how much damage the shield absorbed and how much health
    /// was lost. Useful for the post-fight recap.
    /// </summary>
    public readonly struct DamageResult
    {
        public DamageResult(int absorbedByShield, int healthLost)
        {
            AbsorbedByShield = absorbedByShield;
            HealthLost = healthLost;
        }

        /// <summary>
        /// Damage removed from the shield.
        /// </summary>
        public int AbsorbedByShield { get; }

        /// <summary>
        /// Damage removed from health.
        /// </summary>
        public int HealthLost { get; }

        /// <summary>
        /// Damage actually applied: shield absorbed plus health lost. Overkill is not counted.
        /// </summary>
        public int Total => AbsorbedByShield + HealthLost;
    }
}
