using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Combat;
using Game.Core.Combat.Log;
using Game.Core.Enemies;

namespace Game.Core.Runs
{
    /// <summary>
    /// What happened when a step was played: which encounter was fought and how the fight went. Later systems read
    /// it to offer level-ups (#76), count objectives (#71) and build the recap (#83).
    /// </summary>
    public sealed class RunFightReport
    {
        /// <param name="step">The step that was played.</param>
        /// <param name="encounter">The encounter fought.</param>
        /// <param name="heroLine">The hero's spell line when the fight started, in order. Copied.</param>
        /// <param name="log">The fight's combat log.</param>
        /// <param name="xpGained">XP the hero earned from this fight. Zero or more.</param>
        /// <param name="levelsGained">Levels the hero reached thanks to this fight. Zero or more.</param>
        /// <param name="unlockedRoomIds">Secret rooms this fight unlocked (#71). Null for none.</param>
        /// <param name="secretRoomRewards">What the first victory over a mini-boss gave (#71). Null when nothing.</param>
        /// <exception cref="ArgumentNullException">
        /// An argument or a card of <paramref name="heroLine"/> is null, or <paramref name="unlockedRoomIds"/> holds null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="xpGained"/> or <paramref name="levelsGained"/> is negative.</exception>
        public RunFightReport(
            RunStep step,
            EncounterDefinition encounter,
            IEnumerable<CardInstance> heroLine,
            CombatLog log,
            long xpGained = 0,
            int levelsGained = 0,
            IEnumerable<string> unlockedRoomIds = null,
            SecretRoomRewards secretRoomRewards = null)
        {
            Step = step ?? throw new ArgumentNullException(nameof(step));
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            if (heroLine == null)
            {
                throw new ArgumentNullException(nameof(heroLine));
            }

            var line = new List<CardInstance>(heroLine);
            if (line.Contains(null))
            {
                throw new ArgumentNullException(nameof(heroLine), "The hero's line cannot contain a null card.");
            }

            HeroLine = new ReadOnlyCollection<CardInstance>(line);
            Log = log ?? throw new ArgumentNullException(nameof(log));
            if (xpGained < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(xpGained), xpGained, "XP gained cannot be negative.");
            }

            if (levelsGained < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(levelsGained), levelsGained, "Levels gained cannot be negative.");
            }

            XpGained = xpGained;
            LevelsGained = levelsGained;

            var unlocked = unlockedRoomIds == null ? new List<string>() : new List<string>(unlockedRoomIds);
            if (unlocked.Contains(null))
            {
                throw new ArgumentNullException(nameof(unlockedRoomIds), "The unlocked room ids cannot contain null.");
            }

            UnlockedRoomIds = new ReadOnlyCollection<string>(unlocked);
            SecretRoomRewards = secretRoomRewards;
        }

        /// <summary>The step that was played.</summary>
        public RunStep Step { get; }

        /// <summary>The encounter fought (for a regular fight, the one drawn from the pool).</summary>
        public EncounterDefinition Encounter { get; }

        /// <summary>
        /// The hero's spell line when the fight started: the card instance at each position, so a position in the
        /// <see cref="Log"/> can be traced to the instance that cast it (for example for evolution, #79).
        /// </summary>
        public IReadOnlyList<CardInstance> HeroLine { get; }

        /// <summary>The fight's combat log. The hero is combatant 0, the enemies follow in encounter order.</summary>
        public CombatLog Log { get; }

        /// <summary>True when the hero won the fight.</summary>
        public bool HeroWon => Log.Winner == FightWinner.Hero;

        /// <summary>True when the fight reached the time limit, which counts as a defeat.</summary>
        public bool TimedOut => Log.Winner == FightWinner.None;

        /// <summary>
        /// XP the hero earned: the sum of the XP rewards of the encounter's enemies when the hero won, zero otherwise.
        /// </summary>
        public long XpGained { get; }

        /// <summary>
        /// Levels the hero reached thanks to this fight; several at once are possible. Each one is also added to
        /// <see cref="Run.PendingLevelUps"/> so a linked choice can be offered (#76).
        /// </summary>
        public int LevelsGained { get; }

        /// <summary>
        /// Ids of the secret rooms this fight unlocked: a regular fight won that completed their objective (#71).
        /// Empty otherwise.
        /// </summary>
        public IReadOnlyList<string> UnlockedRoomIds { get; }

        /// <summary>
        /// What the first victory over a secret room's mini-boss gave, or null: for any other fight, for a defeat and
        /// when the room was already cleared (a repeat fight gives XP only).
        /// </summary>
        public SecretRoomRewards SecretRoomRewards { get; }
    }
}
