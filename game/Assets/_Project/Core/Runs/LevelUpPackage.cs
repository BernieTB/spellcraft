using System;
using Game.Core.Cards;
using Game.Core.Upgrades;

namespace Game.Core.Runs
{
    /// <summary>
    /// One package of a level-up offer: a card and a passive upgrade, taken together
    /// (<c>docs/adr/0012-linked-choices-and-spell-line-editing.md</c>).
    /// </summary>
    public sealed class LevelUpPackage
    {
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        public LevelUpPackage(CardDefinition card, PassiveUpgrade passive)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            Passive = passive ?? throw new ArgumentNullException(nameof(passive));
        }

        /// <summary>The card, drawn from the class's card pool.</summary>
        public CardDefinition Card { get; }

        /// <summary>The passive upgrade, drawn from the passive pool.</summary>
        public PassiveUpgrade Passive { get; }
    }
}
