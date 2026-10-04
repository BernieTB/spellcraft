namespace Game.Core.Combat.Log
{
    /// <summary>
    /// What a <see cref="CombatEvent"/> reports. Values are explicit and must not be renumbered: they may be
    /// stored in saved logs.
    /// </summary>
    public enum CombatEventKind
    {
        /// <summary>A card finished casting and resolves. Always comes before the events its effects cause.</summary>
        CardCast = 0,

        /// <summary>Damage applied to the target: absorbed by its shield, then removed from its health.</summary>
        Damage = 1,

        /// <summary>Health restored to the caster.</summary>
        Heal = 2,

        /// <summary>Shield added to the caster.</summary>
        ShieldGain = 3,

        /// <summary>A combatant's health reached zero.</summary>
        Death = 4,

        /// <summary>
        /// The player changed the hero's spell line during the fight (<see cref="CombatEvent.LineChange"/>). Comes
        /// before the casts of its tick.
        /// </summary>
        LineChanged = 5,

        /// <summary>
        /// A card of the hero's reached an evolution stage (<see cref="CombatEvent.EvolutionStage"/>, ADR 0013). Comes
        /// after the events of the cast that reached the stage; the next cast of that card copy uses the new stage.
        /// </summary>
        Evolved = 6,
    }
}
