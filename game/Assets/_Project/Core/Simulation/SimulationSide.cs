namespace Game.Core.Simulation
{
    /// <summary>
    /// Which side cast a card, for per-card statistics. Values are explicit and must not be renumbered.
    /// </summary>
    public enum SimulationSide
    {
        /// <summary>The hero (<see cref="Combat.Fight.HeroIndex"/>).</summary>
        Hero = 0,

        /// <summary>Any enemy.</summary>
        Enemies = 1,
    }
}
