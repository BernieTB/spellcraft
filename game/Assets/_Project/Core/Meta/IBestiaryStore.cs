using System;
using System.IO;

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
        /// <exception cref="IOException">The save exists but could not be read.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the save was denied.</exception>
        bool TryRead(out string text);

        /// <summary>Replaces the saved text.</summary>
        /// <exception cref="IOException">The save could not be written.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the save was denied.</exception>
        /// <exception cref="InvalidOperationException">The store refuses to write, for example to protect a save it could not read.</exception>
        void Write(string text);
    }
}
