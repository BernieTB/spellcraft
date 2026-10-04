using System;
using System.Collections.Generic;
using System.Linq;
using Game.Unity.Content;
using UnityEditor;

namespace Game.Unity.EditorTools.Content
{
    /// <summary>
    /// Finds references to placeholder content (any asset implementing <see cref="IPlaceholderContent"/> with
    /// <see cref="IPlaceholderContent.IsPlaceholder"/> set: cards, enemies, encounters, classes, passive upgrades) so
    /// test-only content cannot ship by accident. Used by <see cref="PlaceholderBuildCheck"/> before every build and by an EditMode test.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only the placeholder folder, the test folder and the debug tools folder may hold or reference placeholders.
    /// Any other asset that is placeholder content or depends on some (directly or through other assets) is a
    /// violation (<see cref="FindProjectViolations"/>). This is stricter than checking only what a build includes,
    /// and needs no knowledge of how content gets into a build (scenes, Resources, later Addressables).
    /// </para>
    /// <para>
    /// Those folders are exempt, so nothing in them may be where a build starts: <see cref="FindBuildViolations"/>
    /// rejects any build root (scene in the build settings, Resources asset, preloaded asset) inside them, such as
    /// the debug fight scene. Every other build root is covered by <see cref="FindProjectViolations"/>, so
    /// together the two checks keep placeholder content out of player builds.
    /// </para>
    /// </remarks>
    public static class PlaceholderGuard
    {
        /// <summary>Folder holding the placeholder content assets (cards, enemies, encounters, classes, passive upgrades).</summary>
        public const string PlaceholderFolder = "Assets/_Project/Unity/Content/Placeholders";

        /// <summary>Folder holding the tests, which may reference placeholders.</summary>
        public const string TestsFolder = "Assets/_Project/Tests";

        /// <summary>
        /// Folder holding editor-only debug scenes and their setup assets, which may reference placeholders but
        /// must never be part of a player build.
        /// </summary>
        public const string DebugFolder = "Assets/_Project/Unity/DebugTools";

        /// <summary>
        /// True when <paramref name="assetPath"/> is inside the placeholder, test or debug tools folder.
        /// </summary>
        public static bool IsInPlaceholderArea(string assetPath)
        {
            return assetPath.StartsWith(PlaceholderFolder + "/", StringComparison.Ordinal)
                || assetPath.StartsWith(TestsFolder + "/", StringComparison.Ordinal)
                || assetPath.StartsWith(DebugFolder + "/", StringComparison.Ordinal);
        }

        /// <summary>
        /// True when the main asset at <paramref name="assetPath"/> is content flagged as placeholder.
        /// </summary>
        public static bool IsPlaceholderAsset(string assetPath)
        {
            var type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (type == null || !typeof(IPlaceholderContent).IsAssignableFrom(type))
            {
                return false;
            }

            return AssetDatabase.LoadMainAssetAtPath(assetPath) is IPlaceholderContent content && content.IsPlaceholder;
        }

        /// <summary>
        /// Lists every placeholder asset that the given assets are or depend on, recursively.
        /// </summary>
        /// <returns>One "root -> placeholder" line per violation, sorted. Empty when there is none.</returns>
        public static IReadOnlyList<string> FindPlaceholderReferences(IEnumerable<string> rootPaths)
        {
            var violations = new List<string>();
            foreach (var root in rootPaths.Distinct().OrderBy(path => path, StringComparer.Ordinal))
            {
                foreach (var dependency in AssetDatabase.GetDependencies(root, true))
                {
                    if (IsPlaceholderAsset(dependency))
                    {
                        violations.Add($"{root} -> {dependency}");
                    }
                }
            }

            violations.Sort(StringComparer.Ordinal);
            return violations;
        }

        /// <summary>
        /// Lists the build roots that lie in the placeholder, test or debug tools folder
        /// (<see cref="IsInPlaceholderArea"/>). Their dependencies need no check: roots elsewhere are covered by
        /// <see cref="FindProjectViolations"/>.
        /// </summary>
        /// <returns>One line per violation, sorted. Empty when there is none.</returns>
        public static IReadOnlyList<string> FindBuildViolations(IEnumerable<string> buildRoots)
        {
            return buildRoots
                .Where(IsInPlaceholderArea)
                .Distinct()
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => $"{path} (build root in a placeholder, test or debug tools folder)")
                .ToList();
        }

        /// <summary>
        /// The assets a player build starts from: enabled scenes of the build settings, assets in any
        /// <c>Resources</c> folder and the preloaded assets of the player settings.
        /// </summary>
        /// <remarks>
        /// Builds that pass their own scene list to <c>BuildPipeline.BuildPlayer</c> are not covered: the project
        /// builds (CI included) use the build settings.
        /// </remarks>
        public static IReadOnlyList<string> FindBuildRoots()
        {
            var roots = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .Concat(AssetDatabase.GetAllAssetPaths()
                    .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
                    .Where(path => path.Contains("/Resources/"))
                    .Where(path => !AssetDatabase.IsValidFolder(path)))
                .Concat(PlayerSettings.GetPreloadedAssets()
                    .Where(asset => asset != null)
                    .Select(AssetDatabase.GetAssetPath))
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct()
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
            return roots;
        }

        /// <summary>
        /// Checks every asset of the project outside the placeholder, test and debug tools folders.
        /// </summary>
        public static IReadOnlyList<string> FindProjectViolations()
        {
            var roots = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
                .Where(path => !AssetDatabase.IsValidFolder(path))
                .Where(path => !IsInPlaceholderArea(path));
            return FindPlaceholderReferences(roots);
        }
    }
}
