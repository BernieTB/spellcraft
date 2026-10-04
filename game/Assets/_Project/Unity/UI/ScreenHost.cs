using System;
using UnityEngine.UIElements;

namespace Game.Unity.UI
{
    /// <summary>
    /// Shows one screen at a time inside a container (ADR 0008): the game runs in a single scene and switches
    /// screens in code. The host clones the layout of the next screen, replaces the current one and hands back the
    /// cloned tree for the screen's view to fill. It holds no game state and no rule.
    /// </summary>
    /// <remarks>
    /// In the game the container is the root of the <see cref="UIDocument"/>. Plain <see cref="VisualElement"/>
    /// containers work too, so hosting is testable without Play mode. A screen view must release what it
    /// registered (callbacks, schedules) when its screen is replaced: the host only removes the tree.
    /// </remarks>
    public sealed class ScreenHost
    {
        /// <summary>Class of the wrapper put around a cloned screen: it fills the host (<c>Common.uss</c>).</summary>
        public const string SlotClass = "screen-slot";

        private readonly VisualElement _container;

        /// <exception cref="ArgumentNullException"><paramref name="container"/> is null.</exception>
        public ScreenHost(VisualElement container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>The tree of the screen being shown, or null when none is.</summary>
        public VisualElement Current { get; private set; }

        /// <summary>
        /// Replaces the screen being shown with a clone of <paramref name="layout"/> and returns it.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="layout"/> is null.</exception>
        public VisualElement Show(VisualTreeAsset layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            Clear();
            var screen = layout.CloneTree();
            screen.AddToClassList(SlotClass);
            _container.Add(screen);
            Current = screen;
            return screen;
        }

        /// <summary>Removes the screen being shown, if any.</summary>
        public void Clear()
        {
            Current?.RemoveFromHierarchy();
            Current = null;
        }
    }
}
