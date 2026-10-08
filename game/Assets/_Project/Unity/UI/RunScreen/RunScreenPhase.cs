namespace Game.Unity.UI.RunScreen
{
    /// <summary>What the run screen is doing.</summary>
    public enum RunScreenPhase
    {
        /// <summary>Between fights: the player picks the next step.</summary>
        ChoosingStep = 0,

        /// <summary>A fight plays tick by tick.</summary>
        Fighting = 1,

        /// <summary>A fight just ended: its result is shown until the player continues.</summary>
        FightResult = 2,

        /// <summary>The run is over (biome cleared or hero defeated).</summary>
        RunEnded = 3,
    }
}
