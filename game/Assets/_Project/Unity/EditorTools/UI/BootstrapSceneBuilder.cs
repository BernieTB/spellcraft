using System.Linq;
using Game.Unity.EditorTools.Content;
using Game.Unity.Flow;
using Game.Unity.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.EditorTools.UI
{
    /// <summary>
    /// Creates the bootstrap scene (<see cref="ScenePath"/>), the only scene of the player build, and, if missing,
    /// the shared panel settings (<see cref="PanelSettingsPath"/>), so neither is hand-written YAML (ADR 0008).
    /// </summary>
    /// <remarks>
    /// Run from the menu <c>Tools &gt; Game &gt; Rebuild Bootstrap Scene</c>, or headless with
    /// <c>-executeMethod Game.Unity.EditorTools.UI.BootstrapSceneBuilder.Build</c>. The scene is rebuilt every time
    /// (a camera, the <see cref="GameBootstrap"/> and a <see cref="UIDocument"/> driven by the <see cref="GameFlowBehaviour"/>, which references the game config) and set
    /// as the only scene in the build settings. The panel settings are only created when they do not exist, so
    /// edits made in the inspector survive a rebuild.
    /// </remarks>
    public static class BootstrapSceneBuilder
    {
        /// <summary>Folder holding the UI assets.</summary>
        public const string UiFolder = "Assets/_Project/Unity/UI";

        /// <summary>Path of the bootstrap scene.</summary>
        public const string ScenePath = "Assets/_Project/Unity/Scenes/Bootstrap.unity";

        /// <summary>Path of the shared panel settings.</summary>
        public const string PanelSettingsPath = UiFolder + "/GamePanelSettings.asset";

        /// <summary>Path of the runtime theme assigned to the panel settings.</summary>
        public const string ThemePath = UiFolder + "/Themes/GameTheme.tss";

        /// <summary>Path of the shared style sheet (palette and sizes).</summary>
        public const string CommonStylePath = UiFolder + "/Styles/Common.uss";

        /// <summary>Path of the title screen layout.</summary>
        public const string TitleScreenPath = UiFolder + "/Screens/TitleScreen.uxml";

        /// <summary>Path of the recap screen layout (not part of the bootstrap scene: screens are switched in code).</summary>
        public const string RecapScreenPath = UiFolder + "/Screens/RecapScreen.uxml";

        /// <summary>Path of the recap screen style sheet.</summary>
        public const string RecapStylePath = UiFolder + "/Styles/Recap.uss";

        /// <summary>Path of the run screen layout (not part of the bootstrap scene: screens are switched in code).</summary>
        public const string RunScreenPath = UiFolder + "/Screens/RunScreen.uxml";

        /// <summary>Path of the run screen style sheet.</summary>
        public const string RunStylePath = UiFolder + "/Styles/Run.uss";

        /// <summary>Path of the level-up choice screen layout.</summary>
        public const string LevelUpScreenPath = UiFolder + "/Screens/LevelUpScreen.uxml";

        /// <summary>Path of the end-of-run screen layout.</summary>
        public const string EndScreenPath = UiFolder + "/Screens/EndScreen.uxml";

        /// <summary>Path of the game configuration the scene references (class, biome, rules, pool, screen layouts).</summary>
        public const string GameConfigPath = "Assets/_Project/Unity/Content/GameConfig.asset";

        [MenuItem("Tools/Game/Rebuild Bootstrap Scene")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder(ScenePath.Substring(0, ScenePath.LastIndexOf('/')));

            // Load assets only after the new scene exists: opening a scene in Single mode can unload assets
            // loaded before it.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var panelSettings = LoadOrCreatePanelSettings();
            var config = WriteGameConfig();

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            new GameObject("Game Bootstrap").AddComponent<GameBootstrap>();

            var uiObject = new GameObject("UI");
            var document = uiObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            var flow = uiObject.AddComponent<GameFlowBehaviour>();
            var flowObject = new SerializedObject(flow);
            flowObject.FindProperty("_config").objectReferenceValue = config;
            flowObject.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);

            if (document.panelSettings == null || flow.GetComponent<UIDocument>() != document)
            {
                throw new System.InvalidOperationException($"Could not assign {PanelSettingsPath} to the UI document.");
            }

            var removed = EditorBuildSettings.scenes
                .Select(buildScene => buildScene.path)
                .Where(path => path != ScenePath)
                .ToList();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            if (removed.Count > 0)
            {
                Debug.Log($"Removed from the build settings: {string.Join(", ", removed)}.");
            }

            Debug.Log($"Bootstrap scene written to {ScenePath} and set as the only scene in the build settings.");
        }

        /// <summary>
        /// Creates or updates the game configuration asset (<see cref="GameConfigPath"/>) and points it to the class,
        /// biome, rules, passive pool and screen layouts. The assets must already exist (regenerate the content first).
        /// </summary>
        public static GameConfigAsset WriteGameConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfigAsset>(GameConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfigAsset>();
                AssetDatabase.CreateAsset(config, GameConfigPath);
            }

            var serialized = new SerializedObject(config);
            Set(serialized, "_heroClass", MvpClassContentGenerator.ClassPath);
            Set(serialized, "_biome", PlaceholderBiomeGenerator.BiomePath);
            Set(serialized, "_fightTimeLimit", PlaceholderBiomeGenerator.FightTimeLimitPath);
            Set(serialized, "_levelCurve", PlaceholderBiomeGenerator.LevelCurvePath);
            Set(serialized, "_passivePool", PassiveUpgradePoolGenerator.PoolPath);
            Set(serialized, "_titleScreen", TitleScreenPath);
            Set(serialized, "_runScreen", RunScreenPath);
            Set(serialized, "_levelUpScreen", LevelUpScreenPath);
            Set(serialized, "_preparationScreen", PreparationScreenView.LayoutPath);
            Set(serialized, "_recapScreen", RecapScreenPath);
            Set(serialized, "_endScreen", EndScreenPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            config.Validate();
            return config;
        }

        private static void Set(SerializedObject serialized, string field, string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath)
                ?? throw new System.InvalidOperationException($"Asset missing at {assetPath} (needed by the game config).");
            serialized.FindProperty(field).objectReferenceValue = asset;
        }

        private static PanelSettings LoadOrCreatePanelSettings()
        {
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings != null)
            {
                return panelSettings;
            }

            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath)
                ?? throw new System.InvalidOperationException($"Runtime theme missing at {ThemePath}.");

            // Layout defaults of a new asset only (a 1080p reference resolution scaled to the window); the asset
            // is the source of truth afterwards.
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.themeStyleSheet = theme;
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            return panelSettings;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var separator = folder.LastIndexOf('/');
            var parent = folder.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder.Substring(separator + 1));
        }
    }
}
