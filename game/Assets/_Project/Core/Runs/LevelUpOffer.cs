using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Game.Core.Runs
{
    /// <summary>The packages offered at one level-up: the player takes exactly one.</summary>
    public sealed class LevelUpOffer
    {
        /// <exception cref="ArgumentNullException"><paramref name="packages"/> or one of its items is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="packages"/> is empty.</exception>
        public LevelUpOffer(IEnumerable<LevelUpPackage> packages)
        {
            if (packages == null)
            {
                throw new ArgumentNullException(nameof(packages));
            }

            var copy = new List<LevelUpPackage>(packages);
            if (copy.Count == 0)
            {
                throw new ArgumentException("An offer needs at least one package.", nameof(packages));
            }

            if (copy.Contains(null))
            {
                throw new ArgumentNullException(nameof(packages), "An offer cannot contain a null package.");
            }

            Packages = new ReadOnlyCollection<LevelUpPackage>(copy);
        }

        /// <summary>The packages, in offer order.</summary>
        public IReadOnlyList<LevelUpPackage> Packages { get; }
    }
}
