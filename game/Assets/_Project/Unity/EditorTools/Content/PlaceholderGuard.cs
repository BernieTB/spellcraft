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
    /// <para>
    /// Only the placeholder folder, the test folder and the debug tools folder may hold or reference placeholders.
    /// Any other asset that is a placeholder card or depends on one (directly or through other assets) is a
    /// violation (<see cref="FindProjectViolations"/>). This is stricter than checking only what a build includes,
    /// and needs no knowledge of how content gets into a build (scenes, Resources, later Addressables).
    /// </para>
    /// <para>
    /// The debug tools folder is allowed so that debug scenes can use placeholder cards in the editor, but its
    /// assets must never ship: <see cref="FindBuildViolations"/> rejects any build root (scene in the build
    /// settings, Resources asset, preloaded asset) that is or depends on a debug asset or a placeholder card.
    /// </para>
    /// </remarks>
    public static class PlaceholderGuard
    {
        /// <summary>Folder holding the placeholder card assets.</summary>
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
                || IsDebugAsset(assetPath);
        }

        /// <summary>
        /// True when <paramref name="assetPath"/> is a debug-only asset: inside <see cref="DebugFolder"/> and not
        /// a script (scripts are compiled into the game assembly whatever happens; scenes and assets are not).
        /// </summary>
        public static bool IsDebugAsset(string assetPath)
        {
            return assetPath.StartsWith(DebugFolder + "/", StringComparison.Ordinal)
                && !assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
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
        /// Lists every placeholder card and every debug-only asset (<see cref="IsDebugAsset"/>) that the given build
        /// roots are or depend on, recursively. Unlike <see cref="FindPlaceholderReferences"/>, no folder is exempt.
        /// </summary>
        /// <returns>One "root -> asset" line per violation, sorted. Empty when there is none.</returns>
        public static IReadOnlyList<string> FindBuildViolations(IEnumerable<string> buildRoots)
        {
            var violations = new List<string>();
            foreach (var root in buildRoots.Distinct().OrderBy(path => path, StringComparer.Ordinal))
            {
                foreach (var dependency in AssetDatabase.GetDependencies(root, true))
                {
                    if (IsPlaceholderCard(dependency))
                    {
                        violations.Add($"{root} -> {dependency} (placeholder card)");
                    }
                    else if (IsDebugAsset(dependency))
                    {
                        violations.Add($"{root} -> {dependency} (debug-only asset)");
                    }
                }
            }

            violations.Sort(StringComparer.Ordinal);
            return violations;
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
