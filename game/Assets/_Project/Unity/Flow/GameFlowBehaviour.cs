using System;
using Game.Unity.Meta;
using Game.Unity.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.Flow
{
    /// <summary>
    /// The thin Unity side of the game flow (#88), on the bootstrap scene: it loads the bestiary save, creates the
    /// <see cref="GameFlow"/> and a <see cref="GameFlowPresenter"/> on the UI document, and quits the application when
    /// the flow asks. No rule lives here. The seed of each run is the clock at its creation, the one
    /// non-deterministic source of the game, which the flow receives as a function.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameFlowBehaviour : MonoBehaviour
    {
        [SerializeField]
        private GameConfigAsset _config;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Pause, in seconds at speed x1, between a won regular fight and the next one (ADR 0016).")]
        private float _nextFightDelaySeconds = (float)Game.Unity.UI.RunScreen.RunScreenSettings.DefaultNextFightDelaySeconds;

        private GameFlow _flow;
        private GameFlowPresenter _presenter;

        /// <summary>The seed of a new run: the UTC clock in ticks.</summary>
        private static ulong ClockSeed() => unchecked((ulong)DateTime.UtcNow.Ticks);

        private void Start()
        {
            if (_config == null)
            {
                Debug.LogError("[Game] The game flow has no config: rebuild the scene with Tools > Game > Rebuild Bootstrap Scene.");
                return;
            }

            var store = FileBestiaryStore.ForPersistentData();
            var loaded = store.Load();
            Debug.Log($"[Bestiary] {loaded.Status}: {loaded.Bestiary.Entries.Count} professor(s) known ({store.Path}).");

            var settings = new Game.Unity.UI.RunScreen.RunScreenSettings(
                nextFightDelaySeconds: Mathf.Max(0f, _nextFightDelaySeconds));
            _flow = new GameFlow(_config.CreateRun, _config.ToPassivePool(), loaded.Bestiary, store, ClockSeed, Debug.LogWarning, settings);
            _flow.QuitRequested += Quit;
            var container = GetComponent<UIDocument>().rootVisualElement;
            _presenter = new GameFlowPresenter(_flow, new ScreenHost(container), _config);
        }

        private void OnDestroy()
        {
            _presenter?.Dispose();
            if (_flow != null)
            {
                _flow.QuitRequested -= Quit;
                _flow.Dispose();
            }
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
