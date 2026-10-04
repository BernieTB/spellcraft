using System;
using System.IO;

namespace Game.Core.Meta
{
    /// <summary>Loads and saves the <see cref="Bestiary"/> through an <see cref="IBestiaryStore"/>.</summary>
    public static class BestiaryStorage
    {
        /// <summary>
        /// Loads the bestiary. Never throws for a bad save: a missing save gives an empty bestiary
        /// (<see cref="BestiaryLoadStatus.Missing"/>); a save that cannot be read (IO error, access denied, invalid
        /// JSON or unknown version) also gives an empty bestiary, with <see cref="BestiaryLoadStatus.Unreadable"/>
        /// and the reason in <see cref="BestiaryLoadResult.Error"/>.
        /// </summary>
        /// <remarks>
        /// After an unreadable load the save may still hold real progress: the caller must not save over it without
        /// keeping a copy first (<c>FileBestiaryStore</c> does this and refuses to write when it could not).
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="store"/> is null.</exception>
        public static BestiaryLoadResult Load(IBestiaryStore store)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            string text;
            try
            {
                if (!store.TryRead(out text))
                {
                    return new BestiaryLoadResult(new Bestiary(), BestiaryLoadStatus.Missing, null);
                }
            }
            catch (IOException exception)
            {
                return Unreadable("The save could not be read: " + exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                return Unreadable("The save could not be read: " + exception.Message);
            }

            try
            {
                return new BestiaryLoadResult(BestiaryJson.Deserialize(text), BestiaryLoadStatus.Loaded, null);
            }
            catch (FormatException exception)
            {
                return Unreadable(exception.Message);
            }
        }

        /// <summary>Saves <paramref name="bestiary"/> to <paramref name="store"/>.</summary>
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        /// <exception cref="IOException">The store could not write the save.</exception>
        /// <exception cref="UnauthorizedAccessException">The store was denied access to the save.</exception>
        /// <exception cref="InvalidOperationException">The store refuses to write (see the store's documentation).</exception>
        public static void Save(IBestiaryStore store, Bestiary bestiary)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            store.Write(BestiaryJson.Serialize(bestiary));
        }

        private static BestiaryLoadResult Unreadable(string error)
        {
            return new BestiaryLoadResult(new Bestiary(), BestiaryLoadStatus.Unreadable, error);
        }
    }
}
