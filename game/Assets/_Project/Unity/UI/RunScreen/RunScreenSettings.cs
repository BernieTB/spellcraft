using System;

namespace Game.Unity.UI.RunScreen
{
    /// <summary>
    /// Presentation settings of the run screen: how fast a fight plays on screen. They change how long a fight takes
    /// to watch, never what happens in it, so they are not balance numbers. ADR 0009 aims at 30 to 40 seconds per
    /// fight including screens; the defaults below are a first guess to tune by looking at the screen.
    /// </summary>
    public sealed class RunScreenSettings
    {
        /// <summary>Default ticks per second at speed x1.</summary>
        public const double DefaultTicksPerSecond = 4d;

        /// <summary>Default speed multipliers the player can pick.</summary>
        public static readonly double[] DefaultSpeeds = { 1d, 2d, 4d, 8d };

        /// <summary>Most ticks simulated in one call to <c>Advance</c>, so a long frame cannot freeze the screen.</summary>
        public const int MaxTicksPerAdvance = 64;

        /// <summary>Default pause, in seconds at speed x1, between a won regular fight and the next one (ADR 0016).</summary>
        public const double DefaultNextFightDelaySeconds = 1.5d;

        /// <param name="ticksPerSecond">Ticks per second at speed x1. Above zero.</param>
        /// <param name="speeds">Speed multipliers, ascending. Null for the defaults.</param>
        /// <param name="nextFightDelaySeconds">Pause before the next regular fight starts by itself. Zero or more.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="ticksPerSecond"/> is zero or negative, or <paramref name="nextFightDelaySeconds"/> is negative.
        /// </exception>
        public RunScreenSettings(
            double ticksPerSecond = DefaultTicksPerSecond,
            double[] speeds = null,
            double nextFightDelaySeconds = DefaultNextFightDelaySeconds)
        {
            if (!(nextFightDelaySeconds >= 0d))
            {
                throw new ArgumentOutOfRangeException(nameof(nextFightDelaySeconds), nextFightDelaySeconds, "The delay cannot be negative.");
            }

            NextFightDelaySeconds = nextFightDelaySeconds;
            if (!(ticksPerSecond > 0d))
            {
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), ticksPerSecond, "Ticks per second must be above zero.");
            }

            TicksPerSecond = ticksPerSecond;
            Speeds = (double[])(speeds ?? DefaultSpeeds).Clone();
        }

        /// <summary>
        /// Pause between a won regular fight and the next one, in seconds at speed x1 (it shortens with the speed and
        /// stops while paused). It changes how the loop feels, never what happens in a fight.
        /// </summary>
        public double NextFightDelaySeconds { get; }

        /// <summary>Ticks per second at speed x1.</summary>
        public double TicksPerSecond { get; }

        /// <summary>Speed multipliers the player can pick, ascending.</summary>
        public double[] Speeds { get; }
    }
}
