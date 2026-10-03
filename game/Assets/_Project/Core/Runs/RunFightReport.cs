using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        /// <param name="step">The step that was played.</param>
        /// <param name="encounter">The encounter fought.</param>
        /// <param name="heroLine">The hero's spell line when the fight started, in order. Copied.</param>
        /// <param name="log">The fight's combat log.</param>
        /// <exception cref="ArgumentNullException">An argument or a card of <paramref name="heroLine"/> is null.</exception>
        public RunFightReport(RunStep step, EncounterDefinition encounter, IEnumerable<CardInstance> heroLine, CombatLog log)
        {
            Step = step ?? throw new ArgumentNullException(nameof(step));
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            if (heroLine == null)
            {
                throw new ArgumentNullException(nameof(heroLine));
            }

            var line = new List<CardInstance>(heroLine);
            if (line.Contains(null))
            {
                throw new ArgumentNullException(nameof(heroLine), "The hero's line cannot contain a null card.");
            }

            HeroLine = new ReadOnlyCollection<CardInstance>(line);
            Log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>The step that was played.</summary>
        public RunStep Step { get; }

        /// <summary>The encounter fought (for a regular fight, the one drawn from the pool).</summary>
        public EncounterDefinition Encounter { get; }

        /// <summary>
        /// The hero's spell line when the fight started: the card instance at each position, so a position in the
        /// <see cref="Log"/> can be traced to the instance that cast it (for example for evolution, #79).
        /// </summary>
        public IReadOnlyList<CardInstance> HeroLine { get; }

        /// <summary>The fight's combat log. The hero is combatant 0, the enemies follow in encounter order.</summary>
        public CombatLog Log { get; }

        /// <summary>True when the hero won the fight.</summary>
        public bool HeroWon => Log.Winner == FightWinner.Hero;

        /// <summary>True when the fight reached the time limit, which counts as a defeat.</summary>
        public bool TimedOut => Log.Winner == FightWinner.None;
    }
}
