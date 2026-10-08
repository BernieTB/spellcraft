using System;
using Game.Core;
using UnityEngine.UIElements;

namespace Game.Unity.UI
{
    /// <summary>
    /// Placeholder title screen (<c>Screens/TitleScreen.uxml</c>), the first UI Toolkit screen (ADR 0008): the working
    /// title and the buttons that start a run or quit. It only displays and forwards clicks (#88).
    /// </summary>
    public static class TitleScreenView
    {
        /// <summary>Name of the label that shows the game title.</summary>
        public const string TitleElement = "title";

        /// <summary>Name of the button that starts a new run.</summary>
        public const string StartButtonElement = "start-button";

        /// <summary>Name of the button that quits the game.</summary>
        public const string QuitButtonElement = "quit-button";

        /// <summary>
        /// Fills a cloned title screen tree.
        /// </summary>
        /// <param name="root">The cloned screen.</param>
        /// <param name="onStart">Called when the player starts a new run. May be null.</param>
        /// <param name="onQuit">Called when the player quits. May be null.</param>
        /// <exception cref="InvalidOperationException">The tree lacks the title label or a button.</exception>
        public static void Bind(VisualElement root, Action onStart = null, Action onQuit = null)
        {
            var title = root.Q<Label>(TitleElement)
                ?? throw new InvalidOperationException($"The title screen has no '{TitleElement}' label.");
            title.text = GameInfo.Name;
            Connect(root, StartButtonElement, onStart);
            Connect(root, QuitButtonElement, onQuit);
        }

        private static void Connect(VisualElement root, string name, Action onClick)
        {
            var button = root.Q<Button>(name)
                ?? throw new InvalidOperationException($"The title screen has no '{name}' button.");
            if (onClick != null)
            {
                button.clicked += onClick;
            }
        }
    }
}
