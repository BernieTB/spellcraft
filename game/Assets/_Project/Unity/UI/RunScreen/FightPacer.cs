using System;

namespace Game.Unity.UI.RunScreen
{
    /// <summary>
    /// Turns real time into whole fight ticks for the run screen. Same idea as the debug viewer's
    /// <c>FightPlayback</c> (ticks per second at speed x1, a speed multiplier, a pause), but for a fight driven tick
    /// by tick from Core: it only says how many ticks to simulate, the fight decides what they do. Plain C#, so the
    /// pacing is tested without a scene.
    /// </summary>
    public sealed class FightPacer
    {
        private double _carry;
        private int _speedIndex;

        /// <param name="ticksPerSecond">Ticks played per second of real time at speed x1. Above zero.</param>
        /// <param name="speeds">The speed multipliers the player can pick, ascending, each above zero. At least one.</param>
        /// <exception cref="ArgumentOutOfRangeException">A value is zero or negative.</exception>
        /// <exception cref="ArgumentException"><paramref name="speeds"/> is empty or not ascending.</exception>
        public FightPacer(double ticksPerSecond, double[] speeds)
        {
            if (!(ticksPerSecond > 0d))
            {
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), ticksPerSecond, "Ticks per second must be above zero.");
            }

            if (speeds == null || speeds.Length == 0)
            {
                throw new ArgumentException("Give at least one speed.", nameof(speeds));
            }

            for (var i = 0; i < speeds.Length; i++)
            {
                if (!(speeds[i] > 0d))
                {
                    throw new ArgumentOutOfRangeException(nameof(speeds), speeds[i], "Speeds must be above zero.");
                }

                if (i > 0 && speeds[i] <= speeds[i - 1])
                {
                    throw new ArgumentException("Speeds must be ascending.", nameof(speeds));
                }
            }

            TicksPerSecond = ticksPerSecond;
            Speeds = (double[])speeds.Clone();
        }

        /// <summary>Ticks played per second at speed x1.</summary>
        public double TicksPerSecond { get; }

        /// <summary>The speed multipliers on offer, ascending.</summary>
        public double[] Speeds { get; }

        /// <summary>Index in <see cref="Speeds"/> of the current speed.</summary>
        public int SpeedIndex
        {
            get => _speedIndex;
            set
            {
                if (value < 0 || value >= Speeds.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"There are {Speeds.Length} speeds.");
                }

                _speedIndex = value;
            }
        }

        /// <summary>The current speed multiplier.</summary>
        public double Speed => Speeds[_speedIndex];

        /// <summary>True while paused: no tick is due.</summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// How many whole ticks are due after <paramref name="seconds"/> of real time. The fraction of a tick left
        /// over is kept for the next call. Zero while paused (time spent paused is not owed afterwards).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="seconds"/> is negative.</exception>
        public int TicksDue(double seconds)
        {
            if (seconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Time cannot go backwards.");
            }

            if (IsPaused)
            {
                return 0;
            }

            _carry += seconds * TicksPerSecond * Speed;
            var ticks = (int)Math.Floor(_carry);
            _carry -= ticks;
            return ticks;
        }

        /// <summary>Forgets the fraction of a tick kept so far (a new fight starts clean).</summary>
        public void Reset()
        {
            _carry = 0d;
        }
    }
}
