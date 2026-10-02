namespace Game.Core.Combat
{
    /// <summary>
    /// Which side won a fight. Values are explicit and must not be renumbered.
    /// </summary>
    public enum FightWinner
    {
        /// <summary>Nobody: the fight reached its maximum number of ticks first (timeout).</summary>
        None = 0,

        /// <summary>The hero: every enemy is dead.</summary>
        Hero = 1,

        /// <summary>The enemies: the hero is dead.</summary>
        Enemies = 2,
    }
}
