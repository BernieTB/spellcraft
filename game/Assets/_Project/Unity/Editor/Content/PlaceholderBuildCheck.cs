using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Game.Unity.Editor.Content
{
    /// <summary>
    /// Fails any player build whose scenes or Resources reference a placeholder card.
    /// </summary>
    public sealed class PlaceholderBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var violations = PlaceholderGuard.FindBuildViolations();
            if (violations.Count > 0)
            {
                throw new BuildFailedException(
                    "Placeholder cards are test-only and must not ship. Remove these references:\n"
                    + string.Join("\n", violations));
            }
        }
    }
}
