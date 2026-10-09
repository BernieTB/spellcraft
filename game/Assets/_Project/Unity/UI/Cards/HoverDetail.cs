using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.UI.Cards
{
    /// <summary>
    /// Shows an element's <see cref="VisualElement.tooltip"/> text in a detail panel while the pointer is over it (#123).
    /// UI Toolkit's own tooltip is "only supported in the Editor UI" (the <c>VisualElement.tooltip</c> documentation of
    /// Unity 6000.3): a runtime panel never shows it, so the panel here is drawn by the screen itself. The text is
    /// still stored in <c>tooltip</c> (the view-model decides it, tests read it). Styled by <c>.hover-detail</c> in
    /// <c>Styles/Common.uss</c>; no rule here, only placement.
    /// </summary>
    public static class HoverDetail
    {
        public const string PanelName = "hover-detail";
        public const string PanelClass = "hover-detail";
        public const string TargetClass = "has-hover-detail";
        public const string HiddenClass = "hidden";
        private const float Margin = 8f;
        private const float EstimatedHeight = 320f;

        /// <summary>Makes <paramref name="target"/> show its tooltip text on hover. Safe to call twice on one element.</summary>
        public static void Enable(VisualElement target)
        {
            if (target == null || target.ClassListContains(TargetClass))
            {
                return;
            }

            target.AddToClassList(TargetClass);
            target.RegisterCallback<PointerEnterEvent>(_ => Show(target));
            target.RegisterCallback<PointerLeaveEvent>(_ => Hide(target));
            target.RegisterCallback<DetachFromPanelEvent>(_ => Hide(target));
        }

        /// <summary>Sets the element's detail text and enables the hover panel for it.</summary>
        public static void Set(VisualElement target, string text)
        {
            target.tooltip = text ?? string.Empty;
            Enable(target);
        }

        private static void Show(VisualElement target)
        {
            var host = FindHost(target);
            if (host == null)
            {
                return;
            }

            var text = target.tooltip;
            var panel = host.Q<Label>(PanelName);
            if (string.IsNullOrEmpty(text))
            {
                panel?.AddToClassList(HiddenClass);
                return;
            }

            if (panel == null)
            {
                panel = new Label { name = PanelName, pickingMode = PickingMode.Ignore };
                panel.AddToClassList(PanelClass);
                panel.style.position = Position.Absolute;
                host.Add(panel);
            }

            panel.text = text;
            panel.BringToFront();
            panel.RemoveFromClassList(HiddenClass);

            // Under the element, or above it when there is no room below; aligned to its left, kept inside the host.
            var hostBox = host.worldBound;
            var box = target.worldBound;
            var left = Mathf.Max(Margin, box.x - hostBox.x);
            panel.style.left = left;
            if (box.yMax - hostBox.y + EstimatedHeight + Margin > hostBox.height && box.y - hostBox.y > EstimatedHeight)
            {
                panel.style.top = StyleKeyword.Auto;
                panel.style.bottom = hostBox.yMax - box.y + Margin;
            }
            else
            {
                panel.style.bottom = StyleKeyword.Auto;
                panel.style.top = box.yMax - hostBox.y + Margin;
            }
        }

        private static void Hide(VisualElement target)
        {
            FindHost(target)?.Q<Label>(PanelName)?.AddToClassList(HiddenClass);
        }

        /// <summary>The screen root (the first ancestor carrying the style sheets), where the panel is added.</summary>
        private static VisualElement FindHost(VisualElement target)
        {
            VisualElement top = null;
            for (var element = target.hierarchy.parent; element != null; element = element.hierarchy.parent)
            {
                if (element.panel != null && element.hierarchy.parent == element.panel.visualTree)
                {
                    top = element;
                    break;
                }

                top = element;
                if (element.styleSheets.count > 0)
                {
                    return element;
                }
            }

            return top;
        }
    }
}
