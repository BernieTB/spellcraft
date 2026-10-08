using Game.Unity.EditorTools.Content;
using NUnit.Framework;

namespace Game.Unity.Tests.Content
{
    /// <summary>
    /// Keeps placeholder content out of shipped content.
    /// </summary>
    public class PlaceholderGuardTests
    {
        [Test]
        public void FindProjectViolations_CurrentProject_IsEmpty()
        {
            var violations = PlaceholderGuard.FindProjectViolations();

            Assert.IsEmpty(violations,
                "Placeholder content may only be referenced from the placeholder, test or debug tools folders:\n"
                + string.Join("\n", violations));
        }

        [Test]
        public void FindPlaceholderReferences_PlaceholderCard_ReportsIt()
        {
            var path = PlaceholderCardGenerator.AssetPath(PlaceholderCardGenerator.CardIds[0]);

            var violations = PlaceholderGuard.FindPlaceholderReferences(new[] { path });

            CollectionAssert.AreEqual(new[] { $"{path} -> {path}" }, violations);
        }

        [Test]
        public void FindPlaceholderReferences_PlaceholderEncounter_ReportsItsEnemyAndCards()
        {
            var path = PlaceholderEnemyGenerator.AssetPath(PlaceholderEnemyGenerator.EncounterIds[0]);

            var violations = PlaceholderGuard.FindPlaceholderReferences(new[] { path });

            CollectionAssert.Contains(violations, $"{path} -> {path}");
            CollectionAssert.Contains(violations, $"{path} -> {PlaceholderEnemyGenerator.AssetPath(PlaceholderEnemyGenerator.EnemyIds[0])}");
            CollectionAssert.Contains(violations, $"{path} -> {PlaceholderCardGenerator.AssetPath(PlaceholderCardGenerator.CardIds[0])}");
        }

        [Test]
        public void IsInPlaceholderArea_PlaceholderFolder_IsTrue()
        {
            Assert.IsTrue(PlaceholderGuard.IsInPlaceholderArea(PlaceholderGuard.PlaceholderFolder + "/x.asset"));
        }

        [Test]
        public void IsInPlaceholderArea_SliceBiomeFolder_IsFalse()
        {
            Assert.IsFalse(PlaceholderGuard.IsInPlaceholderArea(PlaceholderBiomeGenerator.Folder + "/x.asset"));
        }

        [Test]
        public void IsInPlaceholderArea_TestsFolder_IsTrue()
        {
            Assert.IsTrue(PlaceholderGuard.IsInPlaceholderArea(PlaceholderGuard.TestsFolder + "/x.asset"));
        }

        [Test]
        public void IsInPlaceholderArea_DebugFolder_IsTrue()
        {
            Assert.IsTrue(PlaceholderGuard.IsInPlaceholderArea(PlaceholderGuard.DebugFolder + "/x.asset"));
        }

        [Test]
        public void FindBuildViolations_CurrentBuildRoots_IsEmpty()
        {
            var violations = PlaceholderGuard.FindBuildViolations(PlaceholderGuard.FindBuildRoots());

            Assert.IsEmpty(violations,
                "A player build would start from a placeholder, test or debug tools asset:\n" + string.Join("\n", violations));
        }

        [Test]
        public void FindBuildViolations_RootInExemptFolder_ReportsIt()
        {
            var path = PlaceholderCardGenerator.AssetPath(PlaceholderCardGenerator.CardIds[0]);

            var violations = PlaceholderGuard.FindBuildViolations(new[] { path, "Assets/_Project/Unity/Content/x.asset" });

            CollectionAssert.AreEqual(new[] { $"{path} (build root in a placeholder, test or debug tools folder)" }, violations);
        }

        [Test]
        public void IsInPlaceholderArea_OtherFolder_IsFalse()
        {
            Assert.IsFalse(PlaceholderGuard.IsInPlaceholderArea("Assets/_Project/Unity/Content/x.asset"));
        }
    }
}
