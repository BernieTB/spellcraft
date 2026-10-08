using System.Linq;
using Game.Unity.EditorTools.Content;
using Game.Unity.EditorTools.UI;
using Game.Unity.Flow;
using Game.Unity.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.Tests.UI
{
    /// <summary>
    /// Checks the generated bootstrap scene, its panel settings and the build settings without entering Play mode.
    /// </summary>
    public class BootstrapSceneTests
    {
        [Test]
        public void PanelSettings_HasTheGameTheme()
        {
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(BootstrapSceneBuilder.PanelSettingsPath);
            Assert.IsNotNull(panelSettings, $"Missing {BootstrapSceneBuilder.PanelSettingsPath}: run Tools > Game > Rebuild Bootstrap Scene.");

            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(BootstrapSceneBuilder.ThemePath), panelSettings.themeStyleSheet);
        }

        [Test]
        public void Scene_HasADocumentDrivenByTheGameFlow()
        {
            var scene = EditorSceneManager.OpenScene(BootstrapSceneBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var documents = roots.SelectMany(root => root.GetComponentsInChildren<UIDocument>()).ToList();
                Assert.AreEqual(1, documents.Count);

                var document = documents[0];
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<PanelSettings>(BootstrapSceneBuilder.PanelSettingsPath), document.panelSettings);
                var flow = document.GetComponent<GameFlowBehaviour>();
                Assert.IsNotNull(flow);
                Assert.AreEqual(
                    AssetDatabase.LoadAssetAtPath<GameConfigAsset>(BootstrapSceneBuilder.GameConfigPath),
                    new SerializedObject(flow).FindProperty("_config").objectReferenceValue);

                Assert.IsTrue(roots.Any(root => root.GetComponent<GameBootstrap>() != null), "The scene needs the game bootstrap.");
                Assert.IsTrue(roots.Any(root => root.GetComponent<Camera>() != null), "The scene needs a camera.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void BuildSettings_ContainOnlyTheBootstrapScene()
        {
            Assert.That(EditorBuildSettings.scenes.Select(scene => scene.path), Is.EqualTo(new[] { BootstrapSceneBuilder.ScenePath }));
        }

        [Test]
        public void FindBuildViolations_ProjectBuildRoots_ReportsNothing()
        {
            Assert.IsEmpty(PlaceholderGuard.FindBuildViolations(PlaceholderGuard.FindBuildRoots()));
        }

        [Test]
        public void FindPlaceholderReferences_BootstrapScene_ReportsNothing()
        {
            Assert.IsEmpty(PlaceholderGuard.FindPlaceholderReferences(new[] { BootstrapSceneBuilder.ScenePath }));
        }
    }
}
