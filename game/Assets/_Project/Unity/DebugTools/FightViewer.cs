using System;
using Game.Core.Combat.Log;
using UnityEngine;

namespace Game.Unity.DebugTools
{
    /// <summary>
    /// Debug viewer: records the fight described by a <see cref="DebugFightSetup"/> (in Core) and plays its
    /// <see cref="CombatLog"/> back with a plain IMGUI overlay. It only displays log events; pacing is in
    /// <see cref="FightPlayback"/> and every rule is in Core.
    /// </summary>
    /// <remarks>
    /// IMGUI is used on purpose for this debug tool: it needs no prefab, canvas, event system, panel settings or
    /// UI asset, works with the Input System only setting, and keeps the scene to two GameObjects.
    /// Controls: Space pause/play, Right arrow step to the next event tick, R restart, + / - change speed, and
    /// Reload setup to simulate again after editing the setup asset during Play mode.
    /// </remarks>
    public sealed class FightViewer : MonoBehaviour
    {
        // UI layout constants of this debug overlay (not gameplay numbers).
        private const float MinSpeedExponent = -2f; // x0.25
        private const float MaxSpeedExponent = 3f; // x8
        private const int VisibleEventCount = 22;
        private const float ReferenceHeight = 720f;
        private const float BarWidth = 220f;
        private const float BarHeight = 12f;

        [SerializeField]
        [Tooltip("The fight to simulate and replay.")]
        private DebugFightSetup _setup;

        private FightPlayback _playback;
        private float _speedExponent;
        private string _error;
        private Vector2 _scroll;

        // Commands that change what is drawn are applied in Update, never in the middle of an IMGUI pass, so the
        // Layout and Repaint passes of a frame always see the same controls.
        private bool _stepRequested;
        private bool _restartRequested;
        private bool _reloadRequested;

        /// <summary>The playback of the loaded fight, or null when the setup is missing or invalid.</summary>
        public FightPlayback Playback => _playback;

        private void Start()
        {
            Load();
        }

        private void Update()
        {
            if (_reloadRequested)
            {
                _reloadRequested = false;
                Load();
            }

            if (_playback == null)
            {
                return;
            }

            if (_restartRequested)
            {
                _restartRequested = false;
                _playback.Restart();
            }

            if (_stepRequested)
            {
                _stepRequested = false;
                _playback.StepToNextEventTick();
            }

            _playback.Advance(Time.unscaledDeltaTime);
        }

        private void Load()
        {
            _error = null;
            _playback = null;
            if (_setup == null)
            {
                _error = "No DebugFightSetup assigned to the FightViewer.";
                return;
            }

            try
            {
                var log = _setup.Simulate();
                _playback = new FightPlayback(log, _setup.TicksPerSecond) { Speed = Mathf.Pow(2f, _speedExponent) };
                Debug.Log($"[Debug fight] {CombatEventText.Outcome(log.Winner)} after {log.Ticks} ticks, "
                    + $"{log.Events.Count} events.\n{log.ToText()}");
            }
            catch (Exception exception) when (exception is InvalidOperationException || exception is ArgumentException)
            {
                _error = exception.Message;
                Debug.LogException(exception, this);
            }
        }

        private void OnGUI()
        {
            var scale = Mathf.Max(1f, Screen.height / ReferenceHeight);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var area = new Rect(10f, 10f, (Screen.width / scale) - 20f, (Screen.height / scale) - 20f);

            GUILayout.BeginArea(area);
            if (_playback == null)
            {
                GUILayout.Label("Debug fight: " + (_error ?? "not loaded."));
                if (GUILayout.Button("Reload setup", GUILayout.Width(120f)))
                {
                    _reloadRequested = true;
                }

                GUILayout.EndArea();
                return;
            }

            HandleKeys(Event.current);
            DrawControls();
            GUILayout.Space(8f);
            DrawCombatants();
            GUILayout.Space(8f);
            DrawEvents();
            GUILayout.EndArea();
        }

        private void HandleKeys(Event current)
        {
            if (current.type != EventType.KeyDown)
            {
                return;
            }

            switch (current.keyCode)
            {
                case KeyCode.Space:
                    _playback.IsPaused = !_playback.IsPaused;
                    break;
                case KeyCode.RightArrow:
                    _stepRequested = true;
                    break;
                case KeyCode.R:
                    _restartRequested = true;
                    break;
                case KeyCode.Equals:
                case KeyCode.Plus:
                case KeyCode.KeypadPlus:
                    SetSpeedExponent(_speedExponent + 1f);
                    break;
                case KeyCode.Minus:
                case KeyCode.KeypadMinus:
                    SetSpeedExponent(_speedExponent - 1f);
                    break;
                default:
                    return;
            }

            current.Use();
        }

        private void SetSpeedExponent(float exponent)
        {
            _speedExponent = Mathf.Clamp(exponent, MinSpeedExponent, MaxSpeedExponent);
            _playback.Speed = Mathf.Pow(2f, _speedExponent);
        }

        private void DrawControls()
        {
            var log = _playback.Log;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_playback.IsPaused ? "Play (Space)" : "Pause (Space)", GUILayout.Width(110f)))
            {
                _playback.IsPaused = !_playback.IsPaused;
            }

            if (GUILayout.Button("Step (->)", GUILayout.Width(80f)))
            {
                _stepRequested = true;
            }

            if (GUILayout.Button("Restart (R)", GUILayout.Width(90f)))
            {
                _restartRequested = true;
            }

            GUILayout.Label($"Speed x{_playback.Speed:0.##} (+/-)", GUILayout.Width(110f));
            var exponent = GUILayout.HorizontalSlider(_speedExponent, MinSpeedExponent, MaxSpeedExponent, GUILayout.Width(140f));
            if (!Mathf.Approximately(exponent, _speedExponent))
            {
                SetSpeedExponent(exponent);
            }

            GUILayout.Space(12f);
            if (GUILayout.Button("Reload setup", GUILayout.Width(100f)))
            {
                _reloadRequested = true;
            }

            if (GUILayout.Button("Copy JSON", GUILayout.Width(90f)))
            {
                GUIUtility.systemCopyBuffer = log.ToJson();
            }

            GUILayout.EndHorizontal();

            var status = _playback.IsFinished
                ? $"Finished: {CombatEventText.Outcome(log.Winner)} after {log.Ticks} ticks"
                : $"Tick {Mathf.FloorToInt((float)_playback.Position)} / {log.Ticks}{(_playback.IsPaused ? "  (paused)" : string.Empty)}";
            GUILayout.Label($"{status}    Events {_playback.RevealedCount} / {log.Events.Count}");
        }

        private void DrawCombatants()
        {
            for (var i = 0; i < _playback.Log.Combatants.Count; i++)
            {
                var view = _playback.GetCombatant(i);
                GUILayout.BeginHorizontal();
                GUILayout.Label(CombatEventText.CombatantName(i) + (view.IsDead ? " (dead)" : string.Empty), GUILayout.Width(110f));

                var bar = GUILayoutUtility.GetRect(BarWidth, BarHeight, GUILayout.Width(BarWidth));
                bar.y += 4f;
                DrawBar(bar, view.MaxHealth > 0 ? (float)view.Health / view.MaxHealth : 0f, view.IsDead ? Color.gray : new Color(0.8f, 0.2f, 0.2f));
                GUILayout.Label($"HP {view.Health}/{view.MaxHealth}   Shield {view.Shield}", GUILayout.Width(170f));

                for (var p = 0; p < view.SpellLineCardIds.Count; p++)
                {
                    var current = p == view.LastCastPosition;
                    var previous = GUI.color;
                    GUI.color = current ? Color.yellow : previous;
                    GUILayout.Label(current ? $"[{view.SpellLineCardIds[p]}]" : view.SpellLineCardIds[p], GUILayout.ExpandWidth(false));
                    GUI.color = previous;
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawBar(Rect rect, float fraction, Color color)
        {
            var previous = GUI.color;
            GUI.color = new Color(0.15f, 0.15f, 0.15f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawEvents()
        {
            GUILayout.Label("Events (newest last):");
            var events = _playback.Log.Events;
            var first = Mathf.Max(0, _playback.RevealedCount - VisibleEventCount);
            _scroll = GUILayout.BeginScrollView(_scroll);
            for (var i = first; i < _playback.RevealedCount; i++)
            {
                GUILayout.Label(CombatEventText.Describe(events[i]));
            }

            GUILayout.EndScrollView();
        }
    }
}
