using System;

namespace Game.Core.Randomness
{
    /// <summary>
    /// Deterministic seeded random number generator: PCG32 (PCG-XSH-RR, 64-bit state, 32-bit output), as published
    /// by M. E. O'Neill (pcg-random.org, "pcg32" in the minimal C library). Same seed and sequence, same numbers,
    /// on every platform.
    /// </summary>
    /// <remarks>
    /// Not thread-safe and not suitable for security. Each fight should get its own instance.
    /// </remarks>
    public sealed class Pcg32Random : IRandom
    {
        private const ulong Multiplier = 6364136223846793005UL;

        private ulong _state;
        private readonly ulong _increment;

        /// <summary>Creates a generator on sequence 0.</summary>
        /// <param name="seed">Starting seed. Any value.</param>
        public Pcg32Random(ulong seed)
            : this(seed, 0UL)
        {
        }

        /// <summary>Creates a generator on a given sequence (stream).</summary>
        /// <param name="seed">Starting seed. Any value.</param>
        /// <param name="sequence">
        /// Stream selector. Two generators with the same seed and different sequences give unrelated numbers.
        /// </param>
        public Pcg32Random(ulong seed, ulong sequence)
        {
            unchecked
            {
                _state = 0UL;
                _increment = (sequence << 1) | 1UL;
                NextUInt();
                _state += seed;
                NextUInt();
            }
        }

        private Pcg32Random(Pcg32Random source)
        {
            _state = source._state;
            _increment = source._increment;
        }

        /// <summary>
        /// Returns an independent generator in the same state: it gives the same numbers as this one from now on,
        /// and drawing from either does not change the other.
        /// </summary>
        public Pcg32Random Clone()
        {
            return new Pcg32Random(this);
        }

        /// <inheritdoc />
        public uint NextUInt()
        {
            unchecked
            {
                var oldState = _state;
                _state = oldState * Multiplier + _increment;
                var xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
                var rotation = (int)(oldState >> 59);
                return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
            }
        }

        /// <inheritdoc />
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive), maxExclusive, "The upper bound must be greater than the lower bound.");
            }

            unchecked
            {
                // Rejection sampling (pcg32_boundedrand) so every value in the range is equally likely.
                var bound = (uint)((long)maxExclusive - minInclusive);
                var threshold = (0U - bound) % bound;
                while (true)
                {
                    var value = NextUInt();
                    if (value >= threshold)
                    {
                        return (int)(minInclusive + (long)(value % bound));
                    }
                }
            }
        }
    }
}
