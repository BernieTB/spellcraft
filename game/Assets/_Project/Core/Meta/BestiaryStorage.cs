using System;

namespace Game.Core.Meta
{
    /// <summary>Loads and saves the <see cref="Bestiary"/> through an <see cref="IBestiaryStore"/>.</summary>
    public static class BestiaryStorage
    {
        /// <summary>
        /// Loads the bestiary. Never throws for a bad save: a missing or unreadable save gives an empty bestiary,
        /// and the result says why so the caller can report it.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="store"/> is null.</exception>
        public static BestiaryLoadResult Load(IBestiaryStore store)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (!store.TryRead(out var text))
            {
                return new BestiaryLoadResult(new Bestiary(), BestiaryLoadStatus.Missing, null);
            }

            try
            {
                return new BestiaryLoadResult(BestiaryJson.Deserialize(text), BestiaryLoadStatus.Loaded, null);
            }
            catch (FormatException exception)
            {
                return new BestiaryLoadResult(new Bestiary(), BestiaryLoadStatus.Unreadable, exception.Message);
            }
        }

        /// <summary>Saves <paramref name="bestiary"/> to <paramref name="store"/>.</summary>
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        public static void Save(IBestiaryStore store, Bestiary bestiary)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            store.Write(BestiaryJson.Serialize(bestiary));
        }
    }
}
