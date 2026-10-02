using Game.Unity.EditorTools.Content;
using NUnit.Framework;

namespace Game.Unity.Tests.Content
{
    /// <summary>
    /// Keeps placeholder cards out of shipped content.
    /// </summary>
    public class PlaceholderGuardTests
    {
        [Test]
        public void FindProjectViolations_CurrentProject_IsEmpty()
        {
            var violations = PlaceholderGuard.FindProjectViolations();

            Assert.IsEmpty(violations,
                "Placeholder cards may only be referenced from the placeholder or test folders:\n"
                + string.Join("\n", violations));
        }

        [Test]
        public void FindBuildViolations_CurrentBuildSettings_IsEmpty()
        {
            var violations = PlaceholderGuard.FindBuildViolations();

            Assert.IsEmpty(violations, string.Join("\n", violations));
        }

        [Test]
        public void FindPlaceholderReferences_PlaceholderCard_ReportsIt()
        {
            var path = PlaceholderCardGenerator.AssetPath(PlaceholderCardGenerator.CardIds[0]);

            var violations = PlaceholderGuard.FindPlaceholderReferences(new[] { path });

            CollectionAssert.AreEqual(new[] { $"{path} -> {path}" }, violations);
        }

        [Test]
        public void IsInPlaceholderArea_PlaceholderFolder_IsTrue()
        {
            Assert.IsTrue(PlaceholderGuard.IsInPlaceholderArea(PlaceholderGuard.PlaceholderFolder + "/x.asset"));
        }

        [Test]
        public void IsInPlaceholderArea_TestsFolder_IsTrue()
        {
            Assert.IsTrue(PlaceholderGuard.IsInPlaceholderArea(PlaceholderGuard.TestsFolder + "/x.asset"));
        }

        [Test]
        public void IsInPlaceholderArea_OtherFolder_IsFalse()
        {
            Assert.IsFalse(PlaceholderGuard.IsInPlaceholderArea("Assets/_Project/Unity/Content/x.asset"));
        }
    }
}
