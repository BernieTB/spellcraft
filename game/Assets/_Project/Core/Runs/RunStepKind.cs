namespace Game.Core.Runs
{
    /// <summary>
    /// The kinds of step a player can choose between fights. Values are explicit and must not be renumbered.
    /// </summary>
    public enum RunStepKind
    {
        /// <summary>A regular fight drawn from the biome's pool.</summary>
        RegularFight = 0,

        /// <summary>The mini-boss fight of an unlocked secret room.</summary>
        SecretRoom = 1,

        /// <summary>The professor's fight.</summary>
        Professor = 2,
    }
}
