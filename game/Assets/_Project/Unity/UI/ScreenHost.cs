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
    /// containers work too, so hosting is testable without Play mode.
    /// <para>
    /// A screen view that registers something (a callback, a schedule, an event on another object) passes a hide
    /// callback to <see cref="Show"/>: the host calls it exactly once when the screen stops being shown, whether
    /// it was replaced by <see cref="Show"/>, removed by <see cref="Clear"/>, or taken out of the container by
    /// someone else (noticed the next time the host is asked, see <see cref="Current"/>).
    /// </para>
    /// </remarks>
    public sealed class ScreenHost
    {
        /// <summary>Class of the wrapper put around a cloned screen: it fills the host (<c>Common.uss</c>).</summary>
        public const string SlotClass = "screen-slot";

        private readonly VisualElement _container;
        private VisualElement _current;
        private Action<VisualElement> _onHide;

        /// <exception cref="ArgumentNullException"><paramref name="container"/> is null.</exception>
        public ScreenHost(VisualElement container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        /// The tree of the screen being shown, or null when none is. A screen that someone else took out of the
        /// container is no longer shown: asking for it releases it (its hide callback runs) and gives null.
        /// </summary>
        public VisualElement Current
        {
            get
            {
                if (_current != null && _current.parent != _container)
                {
                    Release();
                }

                return _current;
            }
        }

        /// <summary>
        /// Replaces the screen being shown with a clone of <paramref name="layout"/> and returns it. The returned
        /// tree is the clone's root (a <c>TemplateContainer</c> with the <see cref="SlotClass"/> class); the
        /// layout's own elements are inside it, so queries by name work on it.
        /// </summary>
        /// <param name="layout">The layout of the screen.</param>
        /// <param name="onHide">
        /// Called once, with the screen's tree, when the screen stops being shown; the place to release what the
        /// screen's view registered. May be null.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="layout"/> is null.</exception>
        public VisualElement Show(VisualTreeAsset layout, Action<VisualElement> onHide = null)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            Clear();
            var screen = layout.CloneTree();
            screen.AddToClassList(SlotClass);
            _container.Add(screen);
            _current = screen;
            _onHide = onHide;
            return screen;
        }

        /// <summary>Removes the screen being shown, if any, and runs its hide callback.</summary>
        public void Clear()
        {
            Release();
        }

        /// <summary>Forgets the screen, takes it out of the container if it is still there, then runs its callback.</summary>
        private void Release()
        {
            var screen = _current;
            var onHide = _onHide;
            _current = null;
            _onHide = null;
            if (screen == null)
            {
                return;
            }

            screen.RemoveFromHierarchy();
            onHide?.Invoke(screen);
        }
    }
}
