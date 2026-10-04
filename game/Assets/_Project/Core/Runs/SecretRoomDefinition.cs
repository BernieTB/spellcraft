using System;
using Game.Core.Cards;
using Game.Core.Enemies;
using Game.Core.Meta;

namespace Game.Core.Runs
{
    /// <summary>
    /// Immutable description of a secret room, built from data (<c>docs/adr/0010-secret-rooms-and-mini-boss-rewards.md</c>):
    /// the objective that unlocks it, the mini-boss fight inside, and what the first victory over it gives.
    /// </summary>
    public sealed class SecretRoomDefinition
    {
        /// <param name="id">Stable identifier from data, unique among the biome's rooms. Not empty or whitespace.</param>
        /// <param name="objective">What unlocks the room.</param>
        /// <param name="miniBossEncounter">The mini-boss's fight.</param>
        /// <param name="bonusLineSlots">Spell line slots the first victory gives. Zero or more (ADR 0010: one).</param>
        /// <param name="uniqueCard">The card the first victory gives, found nowhere else.</param>
        /// <param name="revelation">What the first victory teaches about the biome's professor. May reveal nothing.</param>
        /// <exception cref="ArgumentException"><paramref name="id"/> is null, empty or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Another argument is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bonusLineSlots"/> is negative.</exception>
        public SecretRoomDefinition(
            string id,
            ObjectiveDefinition objective,
            EncounterDefinition miniBossEncounter,
            int bonusLineSlots,
            CardDefinition uniqueCard,
            ProfessorRevelation revelation)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Secret room id cannot be null, empty or whitespace.", nameof(id));
            }

            if (bonusLineSlots < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bonusLineSlots), bonusLineSlots, "Bonus line slots cannot be negative.");
            }

            Id = id;
            Objective = objective ?? throw new ArgumentNullException(nameof(objective));
            MiniBossEncounter = miniBossEncounter ?? throw new ArgumentNullException(nameof(miniBossEncounter));
            BonusLineSlots = bonusLineSlots;
            UniqueCard = uniqueCard ?? throw new ArgumentNullException(nameof(uniqueCard));
            Revelation = revelation ?? throw new ArgumentNullException(nameof(revelation));
        }

        /// <summary>Stable identifier, unique among the biome's secret rooms.</summary>
        public string Id { get; }

        /// <summary>What unlocks the room.</summary>
        public ObjectiveDefinition Objective { get; }

        /// <summary>The mini-boss's fight.</summary>
        public EncounterDefinition MiniBossEncounter { get; }

        /// <summary>Spell line slots the first victory gives.</summary>
        public int BonusLineSlots { get; }

        /// <summary>The card the first victory gives.</summary>
        public CardDefinition UniqueCard { get; }

        /// <summary>What the first victory teaches about the biome's professor.</summary>
        public ProfessorRevelation Revelation { get; }
    }
}
