using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Game.Unity.EditorTools.Content
{
    /// <summary>
    /// Fails any player build while an asset outside the placeholder, test and debug tools folders is or
    /// references a placeholder card (same check as the EditMode test), or while a build root (scene in the build
    /// settings, Resources asset, preloaded asset) lies in one of those folders, such as the debug fight scene.
    /// </summary>
    public sealed class PlaceholderBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var violations = PlaceholderGuard.FindProjectViolations()
                .Concat(PlaceholderGuard.FindBuildViolations(PlaceholderGuard.FindBuildRoots()))
                .ToList();
            if (violations.Count > 0)
            {
                throw new BuildFailedException(
                    "Placeholder cards and debug tools are test-only and must not ship. Remove these references:\n"
                    + string.Join("\n", violations));
            }
        }
    }
}
