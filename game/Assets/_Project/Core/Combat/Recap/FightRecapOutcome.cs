namespace Game.Core.Combat.Recap
{
    /// <summary>
    /// How a fight ended, from the hero's point of view. Values are explicit and must not be renumbered.
    /// </summary>
    public enum FightRecapOutcome
    {
        /// <summary>Every enemy is dead.</summary>
        Victory = 1,

        /// <summary>The hero is dead.</summary>
        Defeat = 2,

        /// <summary>
        /// The fight reached its maximum number of ticks. It counts as a defeat (ADR 0011), and the recap says the
        /// fight ran out of time.
        /// </summary>
        TimeLimit = 3,
    }
}
