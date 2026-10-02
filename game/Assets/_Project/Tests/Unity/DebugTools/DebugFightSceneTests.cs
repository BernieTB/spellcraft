using System.Linq;
using Game.Unity.DebugTools;
using Game.Unity.EditorTools.Content;
using Game.Unity.EditorTools.DebugTools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Unity.Tests.DebugTools
{
    /// <summary>
    /// Checks the generated debug fight scene and its setup asset without entering Play mode.
    /// </summary>
    public class DebugFightSceneTests
    {
        [Test]
        public void SetupAsset_Simulates_ProducesEvents()
        {
            var setup = AssetDatabase.LoadAssetAtPath<DebugFightSetup>(DebugFightSceneBuilder.SetupPath);
            Assert.IsNotNull(setup, $"Missing {DebugFightSceneBuilder.SetupPath}: run Tools > Game > Rebuild Debug Fight Scene.");

            var log = setup.Simulate();
            TestContext.WriteLine(log.ToText());

            Assert.IsNotEmpty(log.Events);
            Assert.Greater(setup.TicksPerSecond, 0f);
        }

        [Test]
        public void Scene_HasAViewerWiredToTheSetupAsset()
        {
            var scene = EditorSceneManager.OpenScene(DebugFightSceneBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                var viewers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<FightViewer>()).ToList();
                Assert.AreEqual(1, viewers.Count);

                var setup = new SerializedObject(viewers[0]).FindProperty("_setup").objectReferenceValue;
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<DebugFightSetup>(DebugFightSceneBuilder.SetupPath), setup);

                Assert.IsTrue(scene.GetRootGameObjects().Any(root => root.GetComponent<Camera>() != null), "The scene needs a camera.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void Scene_IsNotInTheBuildSettings()
        {
            Assert.That(EditorBuildSettings.scenes.Select(scene => scene.path), Has.No.Member(DebugFightSceneBuilder.ScenePath));
        }

        [Test]
        public void Scene_IsInTheDebugFolder()
        {
            Assert.IsTrue(PlaceholderGuard.IsDebugAsset(DebugFightSceneBuilder.ScenePath));
            Assert.IsTrue(PlaceholderGuard.IsDebugAsset(DebugFightSceneBuilder.SetupPath));
        }

        [Test]
        public void FindBuildViolations_DebugSceneAsBuildRoot_ReportsSetupAndPlaceholders()
        {
            var violations = PlaceholderGuard.FindBuildViolations(new[] { DebugFightSceneBuilder.ScenePath });

            Assert.That(violations, Has.Some.Contains(DebugFightSceneBuilder.SetupPath + " (debug-only asset)"));
            Assert.That(violations, Has.Some.Contains("(placeholder card)"));
        }
    }
}
