namespace Game.Unity.UI.RunScreen
{
    /// <summary>What the run screen is doing.</summary>
    public enum RunScreenPhase
    {
        /// <summary>
        /// Between fights: the next regular fight starts by itself after a short pause, unless a level-up waits (the
        /// loop is paused until it is taken) or a secret room or the professor was asked for (ADR 0016).
        /// </summary>
        BetweenFights = 0,

        /// <summary>A fight plays tick by tick.</summary>
        Fighting = 1,

        /// <summary>A fight that needs the recap just ended (mini-boss, professor, defeat): its result is shown until the player continues.</summary>
        FightResult = 2,

        /// <summary>The run is over (biome cleared or hero defeated).</summary>
        RunEnded = 3,
    }
}
