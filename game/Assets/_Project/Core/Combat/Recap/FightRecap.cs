using System.Collections.Generic;

namespace Game.Core.Combat.Recap
{
    /// <summary>
    /// The recap of one fight (ADR 0014): what each card of each side produced, the neighbour bonuses used and
    /// wasted, and for a lost fight where the chain broke. Immutable. Built only from a combat log by
    /// <see cref="FightRecapBuilder"/>.
    /// </summary>
    public sealed class FightRecap
    {
        public FightRecap(
            FightRecapOutcome outcome,
            int ticks,
            IReadOnlyList<CombatantRecap> combatants,
            DefeatAnalysis defeat)
        {
            Outcome = outcome;
            Ticks = ticks;
            Combatants = combatants;
            Defeat = defeat;
        }

        /// <summary>How the fight ended for the hero.</summary>
        public FightRecapOutcome Outcome { get; }

        /// <summary>True when the fight ended on its time limit (<see cref="FightRecapOutcome.TimeLimit"/>).</summary>
        public bool RanOutOfTime => Outcome == FightRecapOutcome.TimeLimit;

        /// <summary>Number of ticks simulated.</summary>
        public int Ticks { get; }

        /// <summary>One entry per combatant, by fight index: the hero first, then the enemies.</summary>
        public IReadOnlyList<CombatantRecap> Combatants { get; }

        /// <summary>The hero's entry.</summary>
        public CombatantRecap Hero => Combatants[Fight.HeroIndex];

        /// <summary>
        /// Where the chain broke, for a fight that was not won; null for a victory, and null on the time limit when
        /// the hero was not behind at the end.
        /// </summary>
        public DefeatAnalysis Defeat { get; }
    }
}
