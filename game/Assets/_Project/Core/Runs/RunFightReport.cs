using System;
using Game.Core.Combat;
using Game.Core.Combat.Log;
using Game.Core.Enemies;

namespace Game.Core.Runs
{
    /// <summary>
    /// What happened when a step was played: which encounter was fought and how the fight went. Later systems read
    /// it to give XP (#70), count objectives (#71) and build the recap (#83).
    /// </summary>
    public sealed class RunFightReport
    {
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        public RunFightReport(RunStep step, EncounterDefinition encounter, CombatLog log)
        {
            Step = step ?? throw new ArgumentNullException(nameof(step));
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            Log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>The step that was played.</summary>
        public RunStep Step { get; }

        /// <summary>The encounter fought (for a regular fight, the one drawn from the pool).</summary>
        public EncounterDefinition Encounter { get; }

        /// <summary>The fight's combat log. The hero is combatant 0, the enemies follow in encounter order.</summary>
        public CombatLog Log { get; }

        /// <summary>True when the hero won the fight.</summary>
        public bool HeroWon => Log.Winner == FightWinner.Hero;

        /// <summary>True when the fight reached the time limit, which counts as a defeat.</summary>
        public bool TimedOut => Log.Winner == FightWinner.None;
    }
}
