using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Randomness;
using Game.Core.Upgrades;

namespace Game.Core.Runs
{
    /// <summary>
    /// Draws the packages offered at a level-up (<c>docs/adr/0012-linked-choices-and-spell-line-editing.md</c>): each
    /// pairs one card of the class's card pool with one passive of the passive pool, drawn independently. The same
    /// pools and the same generator state give the same offer. Duplicates are allowed, inside an offer and across
    /// offers, and there is no rarity.
    /// </summary>
    public static class LevelUpOfferGenerator
    {
        /// <summary>Packages in an offer (ADR 0012).</summary>
        public const int PackagesPerOffer = 3;

        /// <summary>Draws an offer. Per package, the card is drawn first, then the passive.</summary>
        /// <param name="cardPool">The class's card pool. Not empty.</param>
        /// <param name="passivePool">The passive upgrades that can be offered. Not empty.</param>
        /// <param name="random">The generator to draw with; it advances.</param>
        /// <param name="packageCount">Number of packages. At least 1.</param>
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="packageCount"/> is less than 1.</exception>
        /// <exception cref="InvalidOperationException">A pool is empty.</exception>
        public static LevelUpOffer Generate(
            IReadOnlyList<CardDefinition> cardPool,
            IReadOnlyList<PassiveUpgrade> passivePool,
            IRandom random,
            int packageCount = PackagesPerOffer)
        {
            if (cardPool == null)
            {
                throw new ArgumentNullException(nameof(cardPool));
            }

            if (passivePool == null)
            {
                throw new ArgumentNullException(nameof(passivePool));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (packageCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(packageCount), packageCount, "Offer at least one package.");
            }

            if (cardPool.Count == 0)
            {
                throw new InvalidOperationException("The card pool is empty: nothing to offer.");
            }

            if (passivePool.Count == 0)
            {
                throw new InvalidOperationException("The passive pool is empty: nothing to offer.");
            }

            var packages = new List<LevelUpPackage>(packageCount);
            for (var i = 0; i < packageCount; i++)
            {
                var card = cardPool[random.NextInt(0, cardPool.Count)];
                var passive = passivePool[random.NextInt(0, passivePool.Count)];
                packages.Add(new LevelUpPackage(card, passive));
            }

            return new LevelUpOffer(packages);
        }
    }
}
