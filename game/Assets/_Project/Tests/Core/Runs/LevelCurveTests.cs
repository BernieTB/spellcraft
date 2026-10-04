using System;
using Game.Core.Runs;
using NUnit.Framework;

namespace Game.Core.Tests.Runs
{
    public class LevelCurveTests
    {
        // Test values, not balance numbers: 1 -> 2 costs 10, 2 -> 3 costs 20, then +5 per level.
        private static LevelCurve Curve()
        {
            return new LevelCurve(new[] { 10, 20 }, 5);
        }

        [Test]
        public void Constructor_ValidData_KeepsValues()
        {
            var curve = Curve();

            CollectionAssert.AreEqual(new[] { 10, 20 }, curve.LevelCosts);
            Assert.AreEqual(5, curve.CostIncreaseAfterList);
        }

        [Test]
        public void Constructor_CopiesTheCosts()
        {
            var costs = new[] { 10, 20 };
            var curve = new LevelCurve(costs, 5);

            costs[0] = 99;

            Assert.AreEqual(10, curve.LevelCosts[0]);
        }

        [Test]
        public void Constructor_InvalidData_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new LevelCurve(null, 5));
            Assert.Throws<ArgumentException>(() => new LevelCurve(new int[0], 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelCurve(new[] { 0 }, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelCurve(new[] { -1 }, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelCurve(new[] { 10, 10 }, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelCurve(new[] { 20, 10 }, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LevelCurve(new[] { 10 }, 0));
        }

        [Test]
        public void CostToLevelUpFrom_InTheList_ReturnsTheListedCost()
        {
            Assert.AreEqual(10, Curve().CostToLevelUpFrom(1));
            Assert.AreEqual(20, Curve().CostToLevelUpFrom(2));
        }

        [Test]
        public void CostToLevelUpFrom_PastTheList_AddsTheIncreasePerLevel()
        {
            Assert.AreEqual(25, Curve().CostToLevelUpFrom(3));
            Assert.AreEqual(30, Curve().CostToLevelUpFrom(4));
            Assert.AreEqual(20 + 98 * 5, Curve().CostToLevelUpFrom(100));
        }

        [Test]
        public void CostToLevelUpFrom_EveryLevelCostsMoreThanThePrevious()
        {
            var curve = Curve();

            for (var level = 2; level <= 500; level++)
            {
                Assert.Greater(curve.CostToLevelUpFrom(level), curve.CostToLevelUpFrom(level - 1), $"level {level}");
            }
        }

        [Test]
        public void CostToLevelUpFrom_LevelBelowOne_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Curve().CostToLevelUpFrom(0));
        }

        [TestCase(0, 1)]
        [TestCase(9, 1)]
        [TestCase(10, 2)]
        [TestCase(29, 2)]
        [TestCase(30, 3)]
        [TestCase(54, 3)]
        [TestCase(55, 4)]
        public void LevelForTotalXp_ReturnsTheLevelReached(long totalXp, int expectedLevel)
        {
            Assert.AreEqual(expectedLevel, Curve().LevelForTotalXp(totalXp));
        }

        [Test]
        public void LevelForTotalXp_LongFarming_KeepsLevellingMoreAndMoreSlowly()
        {
            var curve = Curve();

            var levelAt1000 = curve.LevelForTotalXp(1000);
            var levelAt2000 = curve.LevelForTotalXp(2000);

            Assert.Greater(levelAt2000, levelAt1000);
            Assert.Less(levelAt2000 - levelAt1000, levelAt1000 - 1, "the second 1000 XP give fewer levels than the first");
        }

        [Test]
        public void LevelForTotalXp_NegativeXp_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Curve().LevelForTotalXp(-1));
        }
    }
}
