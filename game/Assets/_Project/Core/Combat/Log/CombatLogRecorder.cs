using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Randomness;

namespace Game.Core.Combat.Log
{
    /// <summary>
    /// Runs a fight and records its <see cref="CombatLog"/>: snapshots the participants, runs the
    /// <see cref="Fight"/>, then builds the log from the result.
    /// </summary>
    public static class CombatLogRecorder
    {
        /// <summary>
        /// Creates a <see cref="Fight"/> with these arguments, runs it and returns its log. The participants'
        /// combatants are changed by the fight, as with <see cref="Fight.Run"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">An argument or an enemy is null.</exception>
        /// <exception cref="ArgumentException">The participants are rejected by <see cref="Fight"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxTicks"/> is less than 1.</exception>
        /// <exception cref="InvalidOperationException">
        /// The final health or shield in the log differs from the combatants' (a fight outcome the log cannot
        /// represent yet).
        /// </exception>
        public static CombatLog Record(
            FightParticipant hero,
            IReadOnlyList<FightParticipant> enemies,
            int maxTicks,
            IRandom random)
        {
            return Record(
                hero, enemies, maxTicks, random, Array.Empty<CardDefinition>(), false, Array.Empty<LineChange>());
        }

        /// <summary>
        /// Like <see cref="Record(FightParticipant, IReadOnlyList{FightParticipant}, int, IRandom)"/>, for a fight
        /// whose hero line may change (ADR 0012): creates the <see cref="Fight"/> with the hero's reserve and
        /// <paramref name="lineEditsAllowed"/>, runs it with <paramref name="lineChanges"/> and logs the applied
        /// changes as <see cref="CombatEventKind.LineChanged"/> events.
        /// </summary>
        /// <exception cref="ArgumentNullException">An argument, an enemy, a reserve card or a change is null.</exception>
        /// <exception cref="ArgumentException">
        /// The participants are rejected by <see cref="Fight"/>, or the changes are not in tick order or out of the
        /// fight's ticks.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="maxTicks"/> is less than 1, or a change has a position or reserve index out of range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Changes are given while <paramref name="lineEditsAllowed"/> is false, or the log is out of sync with the
        /// fight.
        /// </exception>
        public static CombatLog Record(
            FightParticipant hero,
            IReadOnlyList<FightParticipant> enemies,
            int maxTicks,
            IRandom random,
            IReadOnlyList<CardDefinition> heroReserve,
            bool lineEditsAllowed,
            IReadOnlyList<LineChange> lineChanges)
        {
            return Record(
                hero, enemies, maxTicks, random, heroReserve, lineEditsAllowed, lineChanges, null, null);
        }

        /// <summary>
        /// Like the overload above, for a fight that also counts the casts of the hero's cards and evolves them (ADR
        /// 0013): <paramref name="heroLineCasts"/> and <paramref name="heroReserveCasts"/> are the total casts each
        /// card copy of the hero already has (see the constructor of <see cref="Fight"/>), or both null to not count.
        /// The evolutions are logged as <see cref="CombatEventKind.Evolved"/> events and the final counts are in
        /// <see cref="CombatLog.HeroCardCasts"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">An argument, an enemy, a reserve card or a change is null.</exception>
        /// <exception cref="ArgumentException">
        /// The participants or the cast counts are rejected by <see cref="Fight"/>, or the changes are not in tick
        /// order or out of the fight's ticks.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="maxTicks"/> is less than 1, or a change has a position or reserve index out of range.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Changes are given while <paramref name="lineEditsAllowed"/> is false, or the log is out of sync with the
        /// fight.
        /// </exception>
        public static CombatLog Record(
            FightParticipant hero,
            IReadOnlyList<FightParticipant> enemies,
            int maxTicks,
            IRandom random,
            IReadOnlyList<CardDefinition> heroReserve,
            bool lineEditsAllowed,
            IReadOnlyList<LineChange> lineChanges,
            IReadOnlyList<int> heroLineCasts,
            IReadOnlyList<int> heroReserveCasts)
        {
            // The fight validates its arguments; it is created before taking snapshots so invalid input fails with
            // the fight's own messages.
            var fight = new Fight(
                hero, enemies, maxTicks, random, heroReserve, lineEditsAllowed, heroLineCasts, heroReserveCasts);

            var participants = new List<FightParticipant>(enemies.Count + 1) { hero };
            participants.AddRange(enemies);

            var snapshots = new CombatantSnapshot[participants.Count];
            for (var i = 0; i < participants.Count; i++)
            {
                snapshots[i] = CombatantSnapshot.Of(i, participants[i]);
            }

            var log = CombatLog.Build(snapshots, fight.Run(lineChanges));
            CheckFinalState(log, participants);
            return log;
        }

        // Guards against the log drifting from the simulation, e.g. if a new effect changes stats in a way that
        // EffectOutcome does not report.
        private static void CheckFinalState(CombatLog log, IReadOnlyList<FightParticipant> participants)
        {
            var health = new int[participants.Count];
            var shield = new int[participants.Count];
            for (var i = 0; i < participants.Count; i++)
            {
                health[i] = log.Combatants[i].Health;
                shield[i] = log.Combatants[i].Shield;
            }

            foreach (var e in log.Events)
            {
                health[e.TargetIndex] = e.TargetHealth;
                shield[e.TargetIndex] = e.TargetShield;
            }

            for (var i = 0; i < participants.Count; i++)
            {
                var combatant = participants[i].Combatant;
                if (combatant.CurrentHealth != health[i] || combatant.Shield != shield[i])
                {
                    throw new InvalidOperationException(
                        $"Combat log out of sync for combatant {i}: log says health {health[i]} shield {shield[i]}, "
                        + $"fight says health {combatant.CurrentHealth} shield {combatant.Shield}.");
                }
            }
        }
    }
}
