using System;
using Game.Core.Runs;
using Game.Unity.UI;
using Game.Unity.UI.RunScreen;
using UnityEngine.UIElements;

namespace Game.Unity.Flow
{
    /// <summary>
    /// Shows the screen of a <see cref="GameFlow"/> through a <see cref="ScreenHost"/> each time the flow changes
    /// screen (#88), and gives the player's clicks to the flow. It holds no rule: layouts come from the
    /// <see cref="GameConfigAsset"/>, state from the flow, and every view only displays it. Works on any container,
    /// so it is tested without Play mode.
    /// </summary>
    public sealed class GameFlowPresenter : IDisposable
    {
        private readonly GameFlow _flow;
        private readonly ScreenHost _host;
        private readonly GameConfigAsset _config;
        private bool _disposed;

        /// <summary>Starts following <paramref name="flow"/> and shows its current screen.</summary>
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        public GameFlowPresenter(GameFlow flow, ScreenHost host, GameConfigAsset config)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _flow.ScreenChanged += Show;
            Show();
        }

        /// <summary>Stops following the flow and removes the screen.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _flow.ScreenChanged -= Show;
            _host.Clear();
        }

        private void Show()
        {
            switch (_flow.Screen)
            {
                case GameScreen.Title:
                    TitleScreenView.Bind(_host.Show(_config.TitleScreen), _flow.StartRun, _flow.Quit);
                    break;
                case GameScreen.Run:
                    ShowRun();
                    break;
                case GameScreen.LevelUp:
                    LevelUpScreenView.Bind(_host.Show(_config.LevelUpScreen), _flow.LevelUp, _flow.TakeLevelUp);
                    break;
                case GameScreen.Preparation:
                    ShowPreparation();
                    break;
                case GameScreen.Recap:
                    ShowRecap();
                    break;
                default:
                    ShowRunEnd();
                    break;
            }
        }

        private void ShowRun()
        {
            RunScreenView.RunScreenBinding binding = null;
            var screen = _host.Show(_config.RunScreen, _ => binding?.Dispose());
            binding = RunScreenView.Bind(screen, _flow.RunController);
        }

        private void ShowPreparation()
        {
            Action stop = null;
            var screen = _host.Show(_config.PreparationScreen, _ => stop?.Invoke());
            stop = PreparationScreenView.Bind(screen, _flow.Preparation, _flow.PreparationStarted);
        }

        private void ShowRecap()
        {
            var screen = _host.Show(_config.RecapScreen);
            RecapScreenView.Bind(screen, _flow.Recap);
            var button = screen.Q<Button>(RecapScreenView.ContinueButtonElement)
                ?? throw new InvalidOperationException($"The recap screen has no '{RecapScreenView.ContinueButtonElement}' button.");
            button.text = _flow.Run.IsInProgress ? "Continue" : "End of the run";
            button.clicked += _flow.ContinueFromRecap;
        }

        private void ShowRunEnd()
        {
            var run = _flow.Run;
            var summary = $"Level {run.Level}, {run.FightsPlayed} fights played, {run.RegularFightsWon} regular fights won. Seed {run.Seed}.";
            EndScreenView.Bind(_host.Show(_config.EndScreen), run.Outcome, summary, _flow.ReturnToTitle);
        }
    }
}
