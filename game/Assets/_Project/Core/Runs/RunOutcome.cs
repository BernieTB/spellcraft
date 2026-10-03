namespace Game.Core.Runs
{
    /// <summary>
    /// State of a run. Values are explicit and must not be renumbered.
    /// </summary>
    public enum RunOutcome
    {
        /// <summary>The run goes on: the player can choose the next step.</summary>
        InProgress = 0,

        /// <summary>The professor is defeated: the biome is cleared.</summary>
        Victory = 1,

        /// <summary>The hero lost a fight (or ran out of time): the run is over.</summary>
        Defeat = 2,
    }
}
