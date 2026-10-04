using System;
using Game.Core.Enemies;
using Game.Core.Meta;

namespace Game.Core.Runs
{
    /// <summary>
    /// What the first victory over a secret room's mini-boss gave
    /// (<c>docs/adr/0010-secret-rooms-and-mini-boss-rewards.md</c>). The slot and the card are already in the run;
    /// the revelation about the professor is not in any bestiary until the caller applies it with
    /// <see cref="RevealTo"/>: the bestiary outlives runs and is saved by the caller after each reveal
    /// (<c>docs/adr/0014-boss-preparation-and-recap.md</c>), so the run does not own it.
    /// </summary>
    public sealed class SecretRoomRewards
    {
        internal SecretRoomRewards(
            string roomId,
            int lineSlotsGained,
            CardInstance card,
            EnemyDefinition professor,
            ProfessorRevelation revelation)
        {
            RoomId = roomId ?? throw new ArgumentNullException(nameof(roomId));
            LineSlotsGained = lineSlotsGained;
            Card = card ?? throw new ArgumentNullException(nameof(card));
            Professor = professor ?? throw new ArgumentNullException(nameof(professor));
            Revelation = revelation ?? throw new ArgumentNullException(nameof(revelation));
        }

        /// <summary>The room whose mini-boss was beaten.</summary>
        public string RoomId { get; }

        /// <summary>Spell line slots added to the run's line.</summary>
        public int LineSlotsGained { get; }

        /// <summary>The unique card, as the new instance the run holds (in the line or the reserve).</summary>
        public CardInstance Card { get; }

        /// <summary>The biome's professor, whom the revelation is about.</summary>
        public EnemyDefinition Professor { get; }

        /// <summary>What the victory teaches about <see cref="Professor"/>.</summary>
        public ProfessorRevelation Revelation { get; }

        /// <summary>
        /// Records the revelation in a bestiary. Safe to call again: nothing changes the second time.
        /// </summary>
        /// <param name="bestiary">The player's bestiary.</param>
        /// <returns>
        /// True when the player learned something new, so the bestiary must be saved (<c>BestiaryStorage.Save</c>).
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="bestiary"/> is null.</exception>
        public bool RevealTo(Bestiary bestiary)
        {
            if (bestiary == null)
            {
                throw new ArgumentNullException(nameof(bestiary));
            }

            return bestiary.Reveal(Professor, Revelation);
        }
    }
}
