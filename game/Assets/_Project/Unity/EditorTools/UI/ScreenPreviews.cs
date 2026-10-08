using System;
using System.Collections.Generic;
using Game.Unity.UI;
using UnityEngine.UIElements;

namespace Game.Unity.EditorTools.UI
{
    /// <summary>
    /// One screen the preview window can show: its name, its layout and how to fill it with demo data.
    /// </summary>
    public sealed class ScreenPreview
    {
        /// <param name="name">Name shown on the preview window's button. Unique in <see cref="ScreenPreviews"/>.</param>
        /// <param name="layoutPath">Asset path of the screen's UXML.</param>
        /// <param name="bind">Fills the cloned screen with demo data, usually the screen view's <c>Bind</c>.</param>
        /// <exception cref="ArgumentException"><paramref name="name"/> or <paramref name="layoutPath"/> is empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="bind"/> is null.</exception>
        public ScreenPreview(string name, string layoutPath, Action<VisualElement> bind)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A preview needs a name.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(layoutPath))
            {
                throw new ArgumentException("A preview needs a layout path.", nameof(layoutPath));
            }

            Name = name;
            LayoutPath = layoutPath;
            Bind = bind ?? throw new ArgumentNullException(nameof(bind));
        }

        /// <summary>Name shown on the preview window's button.</summary>
        public string Name { get; }

        /// <summary>Asset path of the screen's UXML.</summary>
        public string LayoutPath { get; }

        /// <summary>Fills the cloned screen with demo data.</summary>
        public Action<VisualElement> Bind { get; }

        /// <summary>Shows the screen in <paramref name="host"/> filled with its demo data, and returns its tree.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="host"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The layout is missing.</exception>
        public VisualElement ShowIn(ScreenHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            var layout = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath)
                ?? throw new InvalidOperationException($"Screen layout missing at {LayoutPath} (preview '{Name}').");
            var screen = host.Show(layout);
            Bind(screen);
            return screen;
        }
    }

    /// <summary>
    /// The screens the preview window (<c>Tools &gt; Game &gt; Preview Screens</c>) can show. A new screen
    /// registers here with one line: its name, its layout path and how to fill it with demo data.
    /// </summary>
    public static class ScreenPreviews
    {
        /// <summary>Every previewable screen, in the order of the window's buttons.</summary>
        public static IReadOnlyList<ScreenPreview> All { get; } = new List<ScreenPreview>
        {
            new ScreenPreview("Title", BootstrapSceneBuilder.TitleScreenPath, TitleScreenView.Bind),
            new ScreenPreview(
                "Recap: victory",
                BootstrapSceneBuilder.RecapScreenPath,
                root => RecapScreenView.Bind(root, ScreenPreviewFights.Victory())),
            new ScreenPreview(
                "Recap: defeat",
                BootstrapSceneBuilder.RecapScreenPath,
                root => RecapScreenView.Bind(root, ScreenPreviewFights.Defeat())),
            new ScreenPreview(
                "Recap: out of time",
                BootstrapSceneBuilder.RecapScreenPath,
                root => RecapScreenView.Bind(root, ScreenPreviewFights.TimeLimit())),
            new ScreenPreview(
                "Level-up: free slot",
                BootstrapSceneBuilder.LevelUpScreenPath,
                root => LevelUpScreenView.Bind(root, ScreenPreviewLevelUps.FreeSlot(), choice => { })),
            new ScreenPreview(
                "Level-up: full line",
                BootstrapSceneBuilder.LevelUpScreenPath,
                root => LevelUpScreenView.Bind(root, ScreenPreviewLevelUps.FullLine(), choice => { })),
        }.AsReadOnly();
    }
}
