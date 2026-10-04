using System;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Core.Meta;
using UnityEngine;

namespace Game.Unity.Meta
{
    /// <summary>
    /// Keeps the bestiary save in a local file, by default <c>bestiary.json</c> in
    /// <see cref="Application.persistentDataPath"/>. Writes go through a flushed temporary file so a crash never leaves
    /// a half-written save.
    /// </summary>
    /// <remarks>
    /// When <see cref="Load"/> finds a save it cannot read, it keeps a copy (<c>bestiary.unreadable.json</c>, then
    /// <c>bestiary.unreadable-1.json</c>... never overwriting an older copy) before the bestiary starts empty. If no
    /// copy could be made, <see cref="WritesBlocked"/> becomes <c>true</c> and <see cref="Write"/> refuses to replace
    /// the save, so progress the game could not read is never lost.
    /// </remarks>
    public sealed class FileBestiaryStore : IBestiaryStore
    {
        /// <summary>File name of the save in the persistent data folder.</summary>
        public const string FileName = "bestiary.json";

        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        /// <param name="path">Absolute path of the save file. Its folder is created on the first write.</param>
        /// <exception cref="ArgumentException"><paramref name="path"/> is null, empty, whitespace or not absolute.</exception>
        public FileBestiaryStore(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("The bestiary save path cannot be null, empty or whitespace.", nameof(path));
            }

            if (!System.IO.Path.IsPathRooted(path))
            {
                throw new ArgumentException("The bestiary save path must be absolute: '" + path + "'.", nameof(path));
            }

            Path = path;
        }

        /// <summary>Full path of the save file.</summary>
        public string Path { get; }

        /// <summary>
        /// Whether <see cref="Write"/> refuses to replace the save: the last <see cref="Load"/> could not read it and
        /// could not keep a copy either.
        /// </summary>
        public bool WritesBlocked { get; private set; }

        /// <summary>Path of the copy kept by the last <see cref="Load"/> of an unreadable save, or <c>null</c>.</summary>
        public string UnreadableCopyPath { get; private set; }

        /// <summary>A store for the game's save file in <see cref="Application.persistentDataPath"/>.</summary>
        /// <exception cref="InvalidOperationException">Unity gives no persistent data folder.</exception>
        public static FileBestiaryStore ForPersistentData()
        {
            var folder = Application.persistentDataPath;
            if (string.IsNullOrEmpty(folder))
            {
                throw new InvalidOperationException("Unity gives no persistent data folder: the bestiary cannot be saved.");
            }

            return new FileBestiaryStore(System.IO.Path.Combine(folder, FileName));
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
        /// <exception cref="InvalidOperationException"><see cref="WritesBlocked"/> is <c>true</c>.</exception>
        /// <exception cref="IOException">The file could not be written.</exception>
        /// <exception cref="UnauthorizedAccessException">Access to the file was denied.</exception>
        public void Write(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (WritesBlocked)
            {
                throw new InvalidOperationException(
                    "The bestiary save '" + Path + "' could not be read or copied, so it is not overwritten.");
            }

            var folder = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var temporary = Path + ".tmp";
            var bytes = Utf8.GetBytes(text);
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

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
        /// Loads the bestiary from this file and never throws for a bad save. A missing file gives an empty
        /// bestiary. An unreadable file also gives an empty bestiary and logs a warning; a copy of it is kept first
        /// (<see cref="UnreadableCopyPath"/>), or, if that fails, <see cref="WritesBlocked"/> is set.
        /// </summary>
        public BestiaryLoadResult Load()
        {
            WritesBlocked = false;
            UnreadableCopyPath = null;
            var result = BestiaryStorage.Load(this);
            if (result.Status != BestiaryLoadStatus.Unreadable)
            {
                return result;
            }

            try
            {
                var copy = NextUnreadableCopyPath();
                File.Copy(Path, copy, false);
                UnreadableCopyPath = copy;
                Debug.LogWarning(
                    "[Bestiary] The save '" + Path + "' could not be read and the bestiary starts empty. A copy was kept at '"
                        + copy + "'. Reason: " + result.Error);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                WritesBlocked = true;
                Debug.LogWarning(
                    "[Bestiary] The save '" + Path + "' could not be read and no copy could be kept (" + exception.Message
                        + "). The bestiary starts empty and will not be saved over it. Reason: " + result.Error);
            }

            return result;
        }

        private string NextUnreadableCopyPath()
        {
            var folder = System.IO.Path.GetDirectoryName(Path) ?? string.Empty;
            var stem = System.IO.Path.GetFileNameWithoutExtension(Path) + ".unreadable";
            for (var i = 0; ; i++)
            {
                var name = i == 0 ? stem + ".json" : stem + "-" + i.ToString(CultureInfo.InvariantCulture) + ".json";
                var candidate = System.IO.Path.Combine(folder, name);
                if (!File.Exists(candidate) && !Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
        }
    }
}
