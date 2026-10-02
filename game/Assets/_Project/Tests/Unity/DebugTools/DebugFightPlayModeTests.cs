using System.Collections;
using Game.Unity.DebugTools;
using Game.Unity.EditorTools.DebugTools;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Unity.Tests.DebugTools
{
    /// <summary>
    /// Smoke test: the debug fight scene enters Play mode, loads its fight and plays it back, without errors in
    /// the log (any error logged fails the test). It does not check what is drawn on screen.
    /// </summary>
    public class DebugFightPlayModeTests
    {
        private const int Frames = 30;

        [UnityTest]
        public IEnumerator DebugScene_InPlayMode_LoadsAndPlaysTheFight()
        {
            EditorSceneManager.OpenScene(DebugFightSceneBuilder.ScenePath, OpenSceneMode.Single);
            yield return new EnterPlayMode();

            for (var i = 0; i < Frames; i++)
            {
                yield return null;
            }

            var viewer = Object.FindAnyObjectByType<FightViewer>();
            var playback = viewer != null ? viewer.Playback : null;
            var position = playback?.Position ?? 0d;

            yield return new ExitPlayMode();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Assert.IsNotNull(viewer, "No FightViewer in the debug scene.");
            Assert.IsNotNull(playback, "The FightViewer did not load its fight.");
            Assert.Greater(position, 0d, "Playback did not advance.");
        }
    }
}
