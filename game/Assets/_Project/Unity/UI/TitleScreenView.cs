using System;
using Game.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.UI
{
    /// <summary>
    /// Placeholder title screen (<c>Screens/TitleScreen.uxml</c>), the first UI Toolkit screen (ADR 0008). It only
    /// shows the working title; later screens follow the same pattern: a UXML layout and a view that fills it.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TitleScreenView : MonoBehaviour
    {
        /// <summary>Name of the label that shows the game title.</summary>
        public const string TitleElement = "title";

        private void OnEnable()
        {
            Bind(GetComponent<UIDocument>().rootVisualElement);
        }

        /// <summary>
        /// Fills a cloned title screen tree.
        /// </summary>
        /// <exception cref="InvalidOperationException">The tree has no <see cref="TitleElement"/> label.</exception>
        public static void Bind(VisualElement root)
        {
            var title = root.Q<Label>(TitleElement)
                ?? throw new InvalidOperationException($"The title screen has no '{TitleElement}' label.");
            title.text = GameInfo.Name;
        }
    }
}
