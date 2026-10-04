using System;
using System.IO;
using System.Text;
using Game.Core.Meta;
using UnityEngine;

namespace Game.Unity.Meta
{
    /// <summary>
    /// Keeps the bestiary save in a local file, by default <c>bestiary.json</c> in
    /// <see cref="Application.persistentDataPath"/>. Writes go through a temporary file so a crash never leaves a
    /// half-written save.
    /// </summary>
    public sealed class FileBestiaryStore : IBestiaryStore
    {
        /// <summary>File name of the save in the persistent data folder.</summary>
        public const string FileName = "bestiary.json";

        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        /// <param name="path">Full path of the save file. Its folder is created on the first write.</param>
        /// <exception cref="ArgumentException"><paramref name="path"/> is null, empty or whitespace.</exception>
        public FileBestiaryStore(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("The bestiary save path cannot be null, empty or whitespace.", nameof(path));
            }

            Path = path;
        }

        /// <summary>Full path of the save file.</summary>
        public string Path { get; }

        /// <summary>Path of the copy kept when a save cannot be read (<see cref="Load"/>).</summary>
        public string UnreadableCopyPath => System.IO.Path.ChangeExtension(Path, ".unreadable.json");

        /// <summary>A store for the game's save file in <see cref="Application.persistentDataPath"/>.</summary>
        public static FileBestiaryStore ForPersistentData()
        {
            return new FileBestiaryStore(System.IO.Path.Combine(Application.persistentDataPath, FileName));
        }

        /// <inheritdoc />
        public bool TryRead(out string text)
        {
            if (!File.Exists(Path))
            {
                text = null;
                return false;
            }

            text = File.ReadAllText(Path, Utf8);
            return true;
        }

        /// <inheritdoc />
        public void Write(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var folder = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var temporary = Path + ".tmp";
            File.WriteAllText(temporary, text, Utf8);
            if (File.Exists(Path))
            {
                File.Replace(temporary, Path, null);
            }
            else
            {
                File.Move(temporary, Path);
            }
        }

        /// <summary>
        /// Loads the bestiary from this file. A missing file gives an empty bestiary. An unreadable file also gives
        /// an empty bestiary, logs a warning and is copied to <see cref="UnreadableCopyPath"/> first, so the next
        /// save does not erase it.
        /// </summary>
        public BestiaryLoadResult Load()
        {
            var result = BestiaryStorage.Load(this);
            if (result.Status == BestiaryLoadStatus.Unreadable)
            {
                File.Copy(Path, UnreadableCopyPath, true);
                Debug.LogWarning(
                    "[Bestiary] The save '" + Path + "' could not be read and the bestiary starts empty. A copy was kept at '"
                        + UnreadableCopyPath + "'. Reason: " + result.Error);
            }

            return result;
        }
    }
}
