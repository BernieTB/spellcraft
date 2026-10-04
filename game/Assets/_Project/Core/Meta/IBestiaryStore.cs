namespace Game.Core.Meta
{
    /// <summary>
    /// Where the bestiary save text lives. Core only reads and writes text through this interface, so it stays free
    /// of file IO; the game provides a file implementation (<c>Game.Unity.Meta.FileBestiaryStore</c>).
    /// </summary>
    public interface IBestiaryStore
    {
        /// <summary>Reads the saved text.</summary>
        /// <returns><c>false</c> when nothing has been saved yet.</returns>
        bool TryRead(out string text);

        /// <summary>Replaces the saved text.</summary>
        void Write(string text);
    }
}
