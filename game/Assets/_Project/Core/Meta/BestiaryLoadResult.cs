namespace Game.Core.Meta
{
    /// <summary>The outcome of <see cref="BestiaryStorage.Load"/>.</summary>
    public sealed class BestiaryLoadResult
    {
        internal BestiaryLoadResult(Bestiary bestiary, BestiaryLoadStatus status, string error)
        {
            Bestiary = bestiary;
            Status = status;
            Error = error;
        }

        /// <summary>The loaded bestiary; empty when the save was missing or unreadable. Never null.</summary>
        public Bestiary Bestiary { get; }

        /// <summary>Whether the save was read, missing or unreadable.</summary>
        public BestiaryLoadStatus Status { get; }

        /// <summary>Why the save was unreadable; <c>null</c> otherwise.</summary>
        public string Error { get; }
    }
}
