using System;
using Game.Unity.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.EditorTools.UI
{
    /// <summary>
    /// An editor window that shows the game's UI Toolkit screens filled with demo data, so a screen can be looked
    /// at without playing a run. Open it from <c>Tools &gt; Game &gt; Preview Screens</c>.
    /// </summary>
    /// <remarks>
    /// It uses the same runtime theme, the same layouts and the same <see cref="ScreenHost"/> as the game. Screens
    /// are listed in <see cref="ScreenPreviews"/>. The window is only a viewer: it changes no asset and holds no
    /// rule. Note that an editor window does not scale to the game's 1920x1080 reference resolution, so sizes look
    /// larger than in the game window; resize the window to judge proportions.
    /// </remarks>
    public sealed class ScreenPreviewWindow : EditorWindow
    {
        private ScreenHost _host;
        private ScreenPreview _shown;
        private Label _status;

        [MenuItem("Tools/Game/Preview Screens")]
        public static void Open()
        {
            var window = GetWindow<ScreenPreviewWindow>("Screen Preview");
            window.minSize = new Vector2(900f, 600f);
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.flexGrow = 1f;
            AddStyleSheets(root);

            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.flexWrap = Wrap.Wrap;
            foreach (var preview in ScreenPreviews.All)
            {
                var captured = preview;
                bar.Add(new Button(() => Show(captured)) { text = preview.Name });
            }

            _status = new Label();
            var stage = new VisualElement();
            stage.style.flexGrow = 1f;
            root.Add(bar);
            root.Add(_status);
            root.Add(stage);

            _host = new ScreenHost(stage);
            if (ScreenPreviews.All.Count > 0)
            {
                Show(_shown ?? ScreenPreviews.All[0]);
            }
        }

        private static void AddStyleSheets(VisualElement root)
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(BootstrapSceneBuilder.ThemePath);
            if (theme != null)
            {
                root.styleSheets.Add(theme);
            }
        }

        private void Show(ScreenPreview preview)
        {
            try
            {
                preview.ShowIn(_host);
                _shown = preview;
                _status.text = $"Showing: {preview.Name}";
            }
            catch (Exception exception)
            {
                _host.Clear();
                _status.text = $"Could not show '{preview.Name}': {exception.Message}";
                Debug.LogException(exception);
            }
        }
    }
}
