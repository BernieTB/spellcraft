using System;
using System.Collections.Generic;
using Game.Core.Meta;
using Game.Unity.EditorTools.UI;
using Game.Unity.Flow;
using Game.Unity.UI;
using Game.Unity.UI.RunScreen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Game.Unity.Tests.Flow
{
    /// <summary>
    /// The presenter shows the right layout, filled by the right view, for every screen of the flow (#88), checked on a
    /// plain container with the real config of the game.
    /// </summary>
    public class GameFlowPresenterTests
    {
        private static GameConfigAsset Config()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfigAsset>(BootstrapSceneBuilder.GameConfigPath);
            Assert.IsNotNull(config, $"Missing {BootstrapSceneBuilder.GameConfigPath}.");
            return config;
        }

        private static GameFlow NewFlow(GameConfigAsset config, ulong seed = 2)
        {
            return new GameFlow(config.CreateRun, config.ToPassivePool(), new Bestiary(), new MemoryBestiaryStore(), () => seed);
        }

        private static string ProbeFor(GameScreen screen)
        {
            switch (screen)
            {
                case GameScreen.Title:
                    return TitleScreenView.StartButtonElement;
                case GameScreen.Run:
                    return RunScreenView.HudLevelElement;
                case GameScreen.LevelUp:
                    return LevelUpScreenView.PackagesElement;
                case GameScreen.Preparation:
                    return PreparationScreenView.StartButton;
                case GameScreen.Recap:
                    return RecapScreenView.ContinueButtonElement;
                default:
                    return EndScreenView.BackButtonElement;
            }
        }

        [Test]
        public void Config_HasEveryReference()
        {
            Assert.DoesNotThrow(Config().Validate);
        }

        [Test]
        public void New_ShowsTheTitleScreenWithItsButtons()
        {
            var container = new VisualElement();
            using (var flow = NewFlow(Config()))
            using (new GameFlowPresenter(flow, new ScreenHost(container), Config()))
            {
                Assert.IsNotNull(container.Q<Button>(TitleScreenView.StartButtonElement));
                Assert.IsNotNull(container.Q<Button>(TitleScreenView.QuitButtonElement));
            }
        }

        [Test]
        public void FullRun_EveryScreenIsShownWithItsOwnLayout()
        {
            var config = Config();
            var container = new VisualElement();
            var seen = new HashSet<GameScreen>();
            for (ulong seed = 1; seed <= 4; seed++)
            {
                using (var flow = NewFlow(config, seed))
                using (new GameFlowPresenter(flow, new ScreenHost(container), config))
                {
                    FlowBot.PlayRun(
                        flow,
                        onScreen: () =>
                        {
                            Assert.AreEqual(1, container.childCount, flow.Screen.ToString());
                            Assert.IsNotNull(container.Q(ProbeFor(flow.Screen)), $"{flow.Screen} screen shows no '{ProbeFor(flow.Screen)}'.");
                            seen.Add(flow.Screen);
                        });
                }
            }

            foreach (GameScreen screen in Enum.GetValues(typeof(GameScreen)))
            {
                Assert.IsTrue(seen.Contains(screen), $"The {screen} screen was never shown.");
            }
        }

        [Test]
        public void RunEnd_ShowsTheOutcomeAndTheSeed()
        {
            var config = Config();
            var container = new VisualElement();
            using (var flow = NewFlow(config, 5))
            using (new GameFlowPresenter(flow, new ScreenHost(container), config))
            {
                string title = null;
                string summary = null;
                flow.ScreenChanged += () =>
                {
                    if (flow.Screen == GameScreen.RunEnd)
                    {
                        title = container.Q<Label>(EndScreenView.TitleElement).text;
                        summary = container.Q<Label>(EndScreenView.SummaryElement).text;
                    }
                };

                FlowBot.PlayRun(flow);

                Assert.That(title, Is.EqualTo("Victory").Or.EqualTo("Defeat"));
                StringAssert.Contains("Seed 5", summary);
            }
        }

        [Test]
        public void Dispose_RemovesTheScreenAndStopsFollowingTheFlow()
        {
            var config = Config();
            var container = new VisualElement();
            var flow = NewFlow(config);
            var presenter = new GameFlowPresenter(flow, new ScreenHost(container), config);

            presenter.Dispose();
            flow.StartRun();

            Assert.AreEqual(0, container.childCount);
            flow.Dispose();
        }
    }
}
