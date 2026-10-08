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

        /// <param name="ticksPerSecond">Ticks per second at speed x1. Above zero.</param>
        /// <param name="speeds">Speed multipliers, ascending. Null for the defaults.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="ticksPerSecond"/> is zero or negative.</exception>
        public RunScreenSettings(double ticksPerSecond = DefaultTicksPerSecond, double[] speeds = null)
        {
            if (!(ticksPerSecond > 0d))
            {
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), ticksPerSecond, "Ticks per second must be above zero.");
            }

            TicksPerSecond = ticksPerSecond;
            Speeds = (double[])(speeds ?? DefaultSpeeds).Clone();
        }

        /// <summary>Ticks per second at speed x1.</summary>
        public double TicksPerSecond { get; }

        /// <summary>Speed multipliers the player can pick, ascending.</summary>
        public double[] Speeds { get; }
    }
}
