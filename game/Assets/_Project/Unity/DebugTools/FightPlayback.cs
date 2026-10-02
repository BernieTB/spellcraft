using System;
using System.Collections.Generic;
using Game.Core.Combat.Log;

namespace Game.Unity.DebugTools
{
    /// <summary>
    /// Replays a <see cref="CombatLog"/> over real time, for display: decides which events are visible at the
    /// current playback position and what each combatant looks like at that point. Plain C#, no Unity types, so
    /// the pacing is tested without a scene.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The playback position is measured in ticks. It starts at 0 and moves forward by
    /// <c>seconds * TicksPerSecond * Speed</c> on each <see cref="Advance"/>, unless paused. Every event whose tick
    /// is at or before the position is revealed, in log order. The position stops at the last tick of the fight.
    /// </para>
    /// <para>
    /// Combatant states are read from the log (each event carries its target's health and shield afterwards);
    /// nothing is computed from game rules here.
    /// </para>
    /// </remarks>
    public sealed class FightPlayback
    {
        private readonly int[] _health;
        private readonly int[] _shield;
        private readonly bool[] _dead;
        private readonly int[] _lastCastPosition;
        private double _speed = 1d;

        /// <param name="log">The fight to replay.</param>
        /// <param name="ticksPerSecond">Ticks played per second of real time at speed x1, from data. Above zero.</param>
        /// <exception cref="ArgumentNullException"><paramref name="log"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="ticksPerSecond"/> is zero or negative.</exception>
        public FightPlayback(CombatLog log, double ticksPerSecond)
        {
            Log = log ?? throw new ArgumentNullException(nameof(log));
            if (!(ticksPerSecond > 0d))
            {
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), ticksPerSecond, "Ticks per second must be above zero.");
            }

            TicksPerSecond = ticksPerSecond;
            var count = log.Combatants.Count;
            _health = new int[count];
            _shield = new int[count];
            _dead = new bool[count];
            _lastCastPosition = new int[count];
            Restart();
        }

        /// <summary>The fight being replayed.</summary>
        public CombatLog Log { get; }

        /// <summary>Ticks played per second of real time at speed x1.</summary>
        public double TicksPerSecond { get; }

        /// <summary>Playback speed multiplier. Above zero; pause with <see cref="IsPaused"/> instead.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
        public double Speed
        {
            get => _speed;
            set
            {
                if (!(value > 0d))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Speed must be above zero.");
                }

                _speed = value;
            }
        }

        /// <summary>True while playback is paused: <see cref="Advance"/> does nothing.</summary>
        public bool IsPaused { get; set; }

        /// <summary>Playback position in ticks, from 0 to the fight's <see cref="CombatLog.Ticks"/>.</summary>
        public double Position { get; private set; }

        /// <summary>Number of events revealed so far: the first <c>RevealedCount</c> events of the log.</summary>
        public int RevealedCount { get; private set; }

        /// <summary>True when every event is revealed and the position has reached the last tick.</summary>
        public bool IsFinished => RevealedCount == Log.Events.Count && Position >= Log.Ticks;

        /// <summary>
        /// Moves the playback forward by <paramref name="seconds"/> of real time, scaled by
        /// <see cref="TicksPerSecond"/> and <see cref="Speed"/>. Does nothing while paused.
        /// </summary>
        /// <returns>The number of events revealed by this call.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="seconds"/> is negative.</exception>
        public int Advance(double seconds)
        {
            if (seconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Time cannot go backwards; use Restart.");
            }

            if (IsPaused)
            {
                return 0;
            }

            return MoveTo(Position + (seconds * TicksPerSecond * Speed));
        }

        /// <summary>
        /// Jumps to the tick of the next hidden event and reveals every event of that tick, even while paused.
        /// At the end of the log, jumps to the last tick.
        /// </summary>
        /// <returns>The number of events revealed.</returns>
        public int StepToNextEventTick()
        {
            if (RevealedCount < Log.Events.Count)
            {
                return MoveTo(Log.Events[RevealedCount].Tick);
            }

            return MoveTo(Log.Ticks);
        }

        /// <summary>Goes back to the start of the fight: position 0, no event revealed. Keeps speed and pause.</summary>
        public void Restart()
        {
            Position = 0d;
            RevealedCount = 0;
            for (var i = 0; i < Log.Combatants.Count; i++)
            {
                var snapshot = Log.Combatants[i];
                _health[i] = snapshot.Health;
                _shield[i] = snapshot.Shield;
                _dead[i] = snapshot.Health == 0;
                _lastCastPosition[i] = -1;
            }
        }

        /// <summary>The state of combatant <paramref name="index"/> at the current position.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a combatant of the log.</exception>
        public CombatantView GetCombatant(int index)
        {
            if (index < 0 || index >= Log.Combatants.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "No such combatant in the log.");
            }

            var snapshot = Log.Combatants[index];
            return new CombatantView(
                index,
                snapshot.MaxHealth,
                _health[index],
                _shield[index],
                _dead[index],
                _lastCastPosition[index],
                snapshot.SpellLineCardIds);
        }

        private int MoveTo(double position)
        {
            Position = Math.Min(position, Log.Ticks);
            var before = RevealedCount;
            while (RevealedCount < Log.Events.Count && Log.Events[RevealedCount].Tick <= Position)
            {
                Reveal(Log.Events[RevealedCount]);
                RevealedCount++;
            }

            return RevealedCount - before;
        }

        private void Reveal(CombatEvent e)
        {
            if (e.Kind == CombatEventKind.CardCast)
            {
                _lastCastPosition[e.CasterIndex] = e.Position;
                return;
            }

            _health[e.TargetIndex] = e.TargetHealth;
            _shield[e.TargetIndex] = e.TargetShield;
            if (e.Kind == CombatEventKind.Death)
            {
                _dead[e.TargetIndex] = true;
            }
        }

        /// <summary>What to display for one combatant at the current playback position.</summary>
        public readonly struct CombatantView
        {
            public CombatantView(
                int index,
                int maxHealth,
                int health,
                int shield,
                bool isDead,
                int lastCastPosition,
                IReadOnlyList<string> spellLineCardIds)
            {
                Index = index;
                MaxHealth = maxHealth;
                Health = health;
                Shield = shield;
                IsDead = isDead;
                LastCastPosition = lastCastPosition;
                SpellLineCardIds = spellLineCardIds;
            }

            /// <summary>Fight index (0 = hero).</summary>
            public int Index { get; }

            public int MaxHealth { get; }

            public int Health { get; }

            public int Shield { get; }

            /// <summary>True once a revealed event reported the combatant's death.</summary>
            public bool IsDead { get; }

            /// <summary>Line position of the last card it cast so far, or -1 before its first cast.</summary>
            public int LastCastPosition { get; }

            public IReadOnlyList<string> SpellLineCardIds { get; }
        }
    }
}
