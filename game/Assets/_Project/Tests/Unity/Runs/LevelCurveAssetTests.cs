using System;
using Game.Unity.Runs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Tests.Runs
{
    public class LevelCurveAssetTests
    {
        private LevelCurveAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<LevelCurveAsset>();
            _asset.name = "test_level_curve";
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_asset);
        }

        // Test values, not balance numbers.
        private void Set(int[] costs, int increase)
        {
            var serialized = new SerializedObject(_asset);
            var list = serialized.FindProperty("_levelCosts");
            list.arraySize = costs.Length;
            for (var i = 0; i < costs.Length; i++)
            {
                list.GetArrayElementAtIndex(i).intValue = costs[i];
            }

            serialized.FindProperty("_costIncreaseAfterList").intValue = increase;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void ToDefinition_ValidAsset_CopiesTheCurve()
        {
            Set(new[] { 10, 20 }, 5);

            var curve = _asset.ToDefinition();

            CollectionAssert.AreEqual(new[] { 10, 20 }, curve.LevelCosts);
            Assert.AreEqual(5, curve.CostIncreaseAfterList);
        }

        [Test]
        public void ToDefinition_NoCost_ThrowsNamingTheAsset()
        {
            Set(new int[0], 5);

            var exception = Assert.Throws<InvalidOperationException>(() => _asset.ToDefinition());

            StringAssert.Contains("'test_level_curve'", exception.Message);
            Assert.IsInstanceOf<ArgumentException>(exception.InnerException);
        }

        [Test]
        public void ToDefinition_CostsNotIncreasing_ThrowsNamingTheAsset()
        {
            Set(new[] { 20, 10 }, 5);

            var exception = Assert.Throws<InvalidOperationException>(() => _asset.ToDefinition());

            StringAssert.Contains("'test_level_curve'", exception.Message);
            Assert.IsInstanceOf<ArgumentOutOfRangeException>(exception.InnerException);
        }
    }
}
