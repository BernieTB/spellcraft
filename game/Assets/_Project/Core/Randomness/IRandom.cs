namespace Game.Core.Randomness
{
    /// <summary>
    /// Source of random numbers for gameplay. Core never creates its own randomness: an implementation is injected
    /// (for example into a fight), so the same seed and inputs always give the same result.
    /// </summary>
    /// <remarks>
    /// Implementations must be deterministic for a given seed on every platform and runtime. Do not wrap
    /// <c>System.Random</c>: its algorithm is not guaranteed to stay the same across .NET versions. Use
    /// <see cref="Pcg32Random"/>.
    /// </remarks>
    public interface IRandom
    {
        /// <summary>Returns the next 32-bit value, uniformly distributed over the whole <see cref="uint"/> range.</summary>
        uint NextUInt();

        /// <summary>
        /// Returns a value uniformly distributed in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// <paramref name="maxExclusive"/> is not greater than <paramref name="minInclusive"/>.
        /// </exception>
        int NextInt(int minInclusive, int maxExclusive);
    }
}
