using System;
using System.Collections.Generic;
using System.Linq;
using Game.Unity.Cards;
using UnityEditor;

namespace Game.Unity.EditorTools.Content
{
    /// <summary>
    /// Finds references to placeholder cards (<see cref="CardAsset.IsPlaceholder"/>) so test-only content cannot
    /// ship by accident. Used by <see cref="PlaceholderBuildCheck"/> before every build and by an EditMode test.
    /// </summary>
    /// <remarks>
    /// Only the placeholder folder and the test folder may hold or reference placeholders. Any other asset that
    /// is a placeholder card or depends on one (directly or through other assets) is a violation.
    /// </remarks>
    public static class PlaceholderGuard
    {
        /// <summary>Folder holding the placeholder card assets.</summary>
        public const string PlaceholderFolder = "Assets/_Project/Unity/Content/Placeholders";

        /// <summary>Folder holding the tests, which may reference placeholders.</summary>
        public const string TestsFolder = "Assets/_Project/Tests";

        /// <summary>
        /// True when <paramref name="assetPath"/> is inside the placeholder folder or the test folder.
        /// </summary>
        public static bool IsInPlaceholderArea(string assetPath)
        {
            return assetPath.StartsWith(PlaceholderFolder + "/", StringComparison.Ordinal)
                || assetPath.StartsWith(TestsFolder + "/", StringComparison.Ordinal);
        }

        /// <summary>
        /// True when the main asset at <paramref name="assetPath"/> is a card flagged as placeholder.
        /// </summary>
        public static bool IsPlaceholderCard(string assetPath)
        {
            if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(CardAsset))
            {
                return false;
            }

            var card = AssetDatabase.LoadAssetAtPath<CardAsset>(assetPath);
            return card != null && card.IsPlaceholder;
        }

        /// <summary>
        /// Lists every placeholder card that the given assets are or depend on, recursively.
        /// </summary>
        /// <returns>One "root -> placeholder" line per violation, sorted. Empty when there is none.</returns>
        public static IReadOnlyList<string> FindPlaceholderReferences(IEnumerable<string> rootPaths)
        {
            var violations = new List<string>();
            foreach (var root in rootPaths.Distinct().OrderBy(path => path, StringComparer.Ordinal))
            {
                foreach (var dependency in AssetDatabase.GetDependencies(root, true))
                {
                    if (IsPlaceholderCard(dependency))
                    {
                        violations.Add($"{root} -> {dependency}");
                    }
                }
            }

            violations.Sort(StringComparer.Ordinal);
            return violations;
        }

        /// <summary>
        /// Checks every asset of the project outside the placeholder and test folders.
        /// </summary>
        public static IReadOnlyList<string> FindProjectViolations()
        {
            var roots = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
                .Where(path => !AssetDatabase.IsValidFolder(path))
                .Where(path => !IsInPlaceholderArea(path));
            return FindPlaceholderReferences(roots);
        }

        /// <summary>
        /// Checks what a player build includes: the enabled scenes of the build settings and every asset in a
        /// Resources folder.
        /// </summary>
        public static IReadOnlyList<string> FindBuildViolations()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path);
            var resources = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
                .Where(path => path.Contains("/Resources/"))
                .Where(path => !AssetDatabase.IsValidFolder(path));
            return FindPlaceholderReferences(scenes.Concat(resources));
        }
    }
}
