using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Game.Unity.EditorTools.Content
{
    /// <summary>
    /// Fails any player build while an asset outside the placeholder and test folders is or references a
    /// placeholder card (same check as the EditMode test).
    /// </summary>
    public sealed class PlaceholderBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var violations = PlaceholderGuard.FindProjectViolations();
            if (violations.Count > 0)
            {
                throw new BuildFailedException(
                    "Placeholder cards are test-only and must not ship. Remove these references:\n"
                    + string.Join("\n", violations));
            }
        }
    }
}
