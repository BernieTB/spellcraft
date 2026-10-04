namespace Game.Core.Meta
{
    /// <summary>How loading the bestiary went (<see cref="BestiaryStorage.Load"/>).</summary>
    public enum BestiaryLoadStatus
    {
        /// <summary>The save was read.</summary>
        Loaded = 1,

        /// <summary>No save exists yet: the bestiary starts empty.</summary>
        Missing = 2,

        /// <summary>The save could not be read (invalid or unknown version): the bestiary starts empty.</summary>
        Unreadable = 3,
    }
}
