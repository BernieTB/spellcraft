using System;
using System.Collections.Generic;
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
            // The fight validates its arguments; it is created before taking snapshots so invalid input fails with
            // the fight's own messages.
            var fight = new Fight(hero, enemies, maxTicks, random);

            var participants = new List<FightParticipant>(enemies.Count + 1) { hero };
            participants.AddRange(enemies);

            var snapshots = new CombatantSnapshot[participants.Count];
            for (var i = 0; i < participants.Count; i++)
            {
                snapshots[i] = CombatantSnapshot.Of(i, participants[i]);
            }

            var log = CombatLog.Build(snapshots, fight.Run());
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
