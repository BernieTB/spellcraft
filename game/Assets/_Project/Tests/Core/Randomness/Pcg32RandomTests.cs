using System;
using Game.Core.Randomness;
using NUnit.Framework;

namespace Game.Core.Tests.Randomness
{
    public class Pcg32RandomTests
    {
        // Arbitrary test seeds.
        private const ulong Seed = 12345UL;
        private const ulong OtherSeed = 67890UL;
        private const int Draws = 100;

        [Test]
        public void Clone_GivesTheSameNumbersAndStaysIndependent()
        {
            var random = new Pcg32Random(Seed);
            random.NextUInt();
            var clone = random.Clone();

            var fromClone = new uint[Draws];
            for (var i = 0; i < Draws; i++)
            {
                fromClone[i] = clone.NextUInt();
            }

            for (var i = 0; i < Draws; i++)
            {
                Assert.AreEqual(fromClone[i], random.NextUInt());
            }
        }

        [Test]
        public void NextUInt_ReferenceSeed_MatchesPublishedPcg32Output()
        {
            // First outputs of the reference pcg32-demo (pcg-c-basic) for seed 42, sequence 54. Guards against any
            // change to the algorithm, which would silently change every seeded fight.
            var random = new Pcg32Random(42UL, 54UL);
            var expected = new uint[] { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };

            var actual = new uint[expected.Length];
            for (var i = 0; i < actual.Length; i++)
            {
                actual[i] = random.NextUInt();
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void NextUInt_SameSeed_SameSequence()
        {
            var first = new Pcg32Random(Seed);
            var second = new Pcg32Random(Seed);

            for (var i = 0; i < Draws; i++)
            {
                Assert.AreEqual(first.NextUInt(), second.NextUInt());
            }
        }

        [Test]
        public void NextUInt_DifferentSeeds_DifferentSequences()
        {
            var first = new Pcg32Random(Seed);
            var second = new Pcg32Random(OtherSeed);

            var allEqual = true;
            for (var i = 0; i < Draws; i++)
            {
                allEqual &= first.NextUInt() == second.NextUInt();
            }

            Assert.IsFalse(allEqual);
        }

        [Test]
        public void NextInt_Range_StaysWithinBounds()
        {
            var random = new Pcg32Random(Seed);

            for (var i = 0; i < Draws; i++)
            {
                var value = random.NextInt(-3, 4);
                Assert.That(value, Is.InRange(-3, 3));
            }
        }

        [Test]
        public void NextInt_SingleValueRange_ReturnsIt()
        {
            Assert.AreEqual(7, new Pcg32Random(Seed).NextInt(7, 8));
        }

        [Test]
        public void NextInt_FullIntRange_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new Pcg32Random(Seed).NextInt(int.MinValue, int.MaxValue));
        }

        [TestCase(5, 5)]
        [TestCase(5, 4)]
        public void NextInt_EmptyRange_Throws(int minInclusive, int maxExclusive)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Pcg32Random(Seed).NextInt(minInclusive, maxExclusive));
        }
    }
}
