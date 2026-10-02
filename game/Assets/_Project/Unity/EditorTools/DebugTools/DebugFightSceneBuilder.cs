using Game.Unity.Cards;
using Game.Unity.DebugTools;
using Game.Unity.EditorTools.Content;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Unity.EditorTools.DebugTools
{
    /// <summary>
    /// Creates the debug fight scene (<see cref="ScenePath"/>) and, if missing, its setup asset
    /// (<see cref="SetupPath"/>), so neither is hand-written YAML.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Run from the menu <c>Tools &gt; Game &gt; Rebuild Debug Fight Scene</c>, or headless with
    /// <c>-executeMethod Game.Unity.EditorTools.DebugTools.DebugFightSceneBuilder.Build</c>. The scene is rebuilt
    /// every time (a camera and a <see cref="FightViewer"/>). The setup asset is only created when it does not
    /// exist: after that, the owner edits it in the inspector and a rebuild keeps those edits.
    /// </para>
    /// <para>
    /// The scene is never added to the build settings: it references placeholder cards and is debug only
    /// (<see cref="PlaceholderGuard.FindBuildViolations"/>).
    /// </para>
    /// </remarks>
    public static class DebugFightSceneBuilder
    {
        /// <summary>Path of the debug fight scene.</summary>
        public const string ScenePath = PlaceholderGuard.DebugFolder + "/DebugFight.unity";

        /// <summary>Path of the setup asset the scene plays.</summary>
        public const string SetupPath = PlaceholderGuard.DebugFolder + "/DebugFightSetup.asset";

        [MenuItem("Tools/Game/Rebuild Debug Fight Scene")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder(PlaceholderGuard.DebugFolder);

            // Load the setup only after the new scene exists: opening a scene in Single mode can unload assets
            // loaded before it, which would leave the viewer with a missing reference.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var setup = LoadOrCreateSetup();

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var viewerObject = new GameObject("Fight Viewer");
            var viewer = viewerObject.AddComponent<FightViewer>();
            var serializedViewer = new SerializedObject(viewer);
            serializedViewer.FindProperty("_setup").objectReferenceValue = setup;
            serializedViewer.ApplyModifiedPropertiesWithoutUndo();

            if (serializedViewer.FindProperty("_setup").objectReferenceValue == null)
            {
                throw new System.InvalidOperationException($"Could not assign {SetupPath} to the fight viewer.");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Debug fight scene written to {ScenePath} (setup: {SetupPath}). It is not in the build settings.");
        }

        private static DebugFightSetup LoadOrCreateSetup()
        {
            var setup = AssetDatabase.LoadAssetAtPath<DebugFightSetup>(SetupPath);
            if (setup != null)
            {
                return setup;
            }

            setup = ScriptableObject.CreateInstance<DebugFightSetup>();
            AssetDatabase.CreateAsset(setup, SetupPath);
            WriteInitialData(setup);
            EditorUtility.SetDirty(setup);
            return setup;
        }

        // Starting values of a new setup asset only: arbitrary test data using the placeholder cards, not game
        // content or balance. The asset is the source of truth afterwards; edit it in the inspector.
        private static void WriteInitialData(DebugFightSetup setup)
        {
            var serialized = new SerializedObject(setup);
            serialized.FindProperty("_seed").longValue = 1L;
            serialized.FindProperty("_maxTicks").intValue = 500;
            serialized.FindProperty("_ticksPerSecond").floatValue = 2f;

            WriteCombatant(serialized.FindProperty("_hero"), 50, 0, "test_card_01", "test_card_04", "test_card_01", "test_card_05");

            var enemies = serialized.FindProperty("_enemies");
            enemies.arraySize = 2;
            WriteCombatant(enemies.GetArrayElementAtIndex(0), 15, 0, "test_card_01");
            WriteCombatant(enemies.GetArrayElementAtIndex(1), 10, 2, "test_card_04");

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteCombatant(SerializedProperty combatant, int maxHealth, int shield, params string[] cardIds)
        {
            combatant.FindPropertyRelative("_maxHealth").intValue = maxHealth;
            combatant.FindPropertyRelative("_startingShield").intValue = shield;
            var line = combatant.FindPropertyRelative("_spellLine");
            line.arraySize = cardIds.Length;
            for (var i = 0; i < cardIds.Length; i++)
            {
                var path = PlaceholderCardGenerator.AssetPath(cardIds[i]);
                var card = AssetDatabase.LoadAssetAtPath<CardAsset>(path);
                if (card == null)
                {
                    throw new System.InvalidOperationException(
                        $"Placeholder card missing at {path}. Run Tools > Game > Regenerate Placeholder Cards first.");
                }

                line.GetArrayElementAtIndex(i).objectReferenceValue = card;
            }
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
