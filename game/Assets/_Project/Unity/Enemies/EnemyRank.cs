namespace Game.Unity.Enemies
{
    /// <summary>
    /// Kind of enemy (GDD, "Run structure"). Descriptive only for now: every rank fights with the same rules.
    /// </summary>
    public enum EnemyRank
    {
        /// <summary>A regular monster of a biome.</summary>
        Regular = 0,

        /// <summary>A mythical creature hidden in a secret room.</summary>
        MiniBoss = 1,

        /// <summary>The boss at the end of a biome.</summary>
        Professor = 2,
    }
}
