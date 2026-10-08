using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Meta;
using Game.Core.Runs;
using Game.Unity.EditorTools.UI;
using Game.Unity.Flow;
using Game.Unity.UI;
using Game.Unity.UI.RunScreen;
using NUnit.Framework;
using UnityEditor;

namespace Game.Unity.Tests.Flow
{
    /// <summary>
    /// The screen flow of the game (#88) without Play mode: the state machine alone, with the game's real content
    /// (class, biome, passive pool of the game config) and with a small demo run where a secret room is quick to reach.
    /// </summary>
    public class GameFlowTests
    {
        private static GameConfigAsset Config()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfigAsset>(BootstrapSceneBuilder.GameConfigPath);
            Assert.IsNotNull(config, $"Missing {BootstrapSceneBuilder.GameConfigPath}: run Tools > Game > Rebuild Bootstrap Scene.");
            return config;
        }

        private static GameFlow NewFlow(MemoryBestiaryStore store, ulong seed = 1, Bestiary bestiary = null, List<string> warnings = null)
        {
            var config = Config();
            return new GameFlow(
                config.CreateRun,
                config.ToPassivePool(),
                bestiary ?? new Bestiary(),
                store,
                () => seed,
                warnings == null ? (Action<string>)null : warnings.Add);
        }

        private static GameFlow NewDemoFlow(MemoryBestiaryStore store, Bestiary bestiary = null)
        {
            return new GameFlow(
                _ => ScreenPreviewRuns.NewRun(),
                Config().ToPassivePool(),
                bestiary ?? new Bestiary(),
                store,
                () => 7);
        }

        [Test]
        public void New_StartsOnTheTitleScreenWithNoRun()
        {
            using (var flow = NewFlow(new MemoryBestiaryStore()))
            {
                Assert.AreEqual(GameScreen.Title, flow.Screen);
                Assert.IsNull(flow.Run);
                Assert.IsNull(flow.RunController);
            }
        }

        [Test]
        public void StartRun_CreatesTheRunFromTheSeedSourceAndShowsTheRunScreen()
        {
            using (var flow = NewFlow(new MemoryBestiaryStore(), seed: 12345))
            {
                var changes = 0;
                flow.ScreenChanged += () => changes++;

                flow.StartRun();

                Assert.AreEqual(GameScreen.Run, flow.Screen);
                Assert.AreEqual(12345UL, flow.Run.Seed);
                Assert.AreEqual(1, changes);
                Assert.AreSame(flow.Run, flow.RunController.Run);
            }
        }

        [Test]
        public void StartRun_UsesTheClassAndBiomeOfTheGameConfig()
        {
            using (var flow = NewFlow(new MemoryBestiaryStore()))
            {
                flow.StartRun();

                Assert.AreEqual("BIOME_01", flow.Run.Biome.Id);
                Assert.AreEqual("CLASS_A", flow.Run.HeroClass.Id);
            }
        }

        [Test]
        public void Methods_OnTheWrongScreen_Throw()
        {
            using (var flow = NewFlow(new MemoryBestiaryStore()))
            {
                Assert.Throws<InvalidOperationException>(flow.ContinueFromRecap);
                Assert.Throws<InvalidOperationException>(flow.ReturnToTitle);
                Assert.Throws<InvalidOperationException>(flow.PreparationStarted);
                flow.StartRun();
                Assert.Throws<InvalidOperationException>(flow.StartRun);
                Assert.Throws<InvalidOperationException>(flow.Quit);
            }
        }

        [Test]
        public void Quit_OnTheTitleScreen_RaisesQuitRequested()
        {
            using (var flow = NewFlow(new MemoryBestiaryStore()))
            {
                var quit = false;
                flow.QuitRequested += () => quit = true;

                flow.Quit();

                Assert.IsTrue(quit);
            }
        }

        [Test]
        public void RegularFight_Won_ShowsTheRecapThenTheLevelUpChoiceBeforeAnyNextFight()
        {
            using (var flow = NewFlow(new MemoryBestiaryStore()))
            {
                flow.StartRun();
                PlayRegularFightsUntilALevelIsPending(flow);

                Assert.AreEqual(GameScreen.Recap, flow.Screen);
                Assert.IsNotNull(flow.Recap);
                Assert.IsTrue(flow.LastReport.HeroWon);
                Assert.IsTrue(flow.Run.HasPendingChoice);

                flow.ContinueFromRecap();

                Assert.AreEqual(GameScreen.LevelUp, flow.Screen);
                Assert.IsNotNull(flow.LevelUp);
                Assert.IsNull(flow.Recap);
            }
        }

        [Test]
        public void RunScreen_WhileALevelUpWaits_StartingAFightOpensTheChoiceInstead()
        {
            using (var flow = NewFlow(new MemoryBestiaryStore()))
            {
                flow.StartRun();
                PlayRegularFightsUntilALevelIsPending(flow);

                flow.ContinueFromRecap();
                Assert.AreEqual(GameScreen.LevelUp, flow.Screen);
                flow.LevelUp.SelectPackage(0);
                if (flow.LevelUp.LineIsFull)
                {
                    flow.LevelUp.SelectReserve();
                }

                flow.TakeLevelUp(flow.LevelUp.Confirm());

                Assert.AreEqual(GameScreen.Run, flow.Screen);
                Assert.IsFalse(flow.Run.HasPendingChoice);
                Assert.IsTrue(flow.RunController.CanStartStep);
            }
        }

        [Test]
        public void SecretRoom_Chosen_OpensThePreparationAndStartingItGivesTheFightToTheRunScreen()
        {
            var store = new MemoryBestiaryStore();
            using (var flow = NewDemoFlow(store))
            {
                flow.StartRun();
                PlayUntilARoomIsUnlocked(flow);
                var room = flow.Run.AvailableSteps.First(step => step.Kind == RunStepKind.SecretRoom);

                flow.RunController.ChooseStep(room);

                Assert.AreEqual(GameScreen.Preparation, flow.Screen);
                Assert.IsNotNull(flow.Preparation);
                flow.Preparation.Start();
                flow.PreparationStarted();
                Assert.AreEqual(GameScreen.Run, flow.Screen);
                Assert.AreEqual(RunScreenPhase.Fighting, flow.RunController.Phase);
                Assert.AreSame(flow.Run.CurrentFight, flow.RunController.Session);
                Assert.IsNull(flow.Preparation);
            }
        }

        [Test]
        public void MiniBossVictory_RevealsToTheBestiaryAndSavesIt()
        {
            var store = new MemoryBestiaryStore();
            using (var flow = NewDemoFlow(store))
            {
                FlowBot.PlayRun(flow);

                Assert.That(flow.Bestiary.Entries, Is.Not.Empty);
                Assert.GreaterOrEqual(store.Writes, 1);
                Assert.AreEqual(flow.Bestiary.Entries.Count, BestiaryJson.Deserialize(store.Text).Entries.Count);
                Assert.IsFalse(flow.LastSaveFailed);
            }
        }

        [Test]
        public void Bestiary_IsKeptBetweenTwoRuns()
        {
            var store = new MemoryBestiaryStore();
            using (var flow = NewDemoFlow(store))
            {
                var bestiary = flow.Bestiary;
                FlowBot.PlayRun(flow);
                var known = bestiary.Entries.Count;
                Assert.Greater(known, 0);

                FlowBot.PlayRun(flow);

                Assert.AreSame(bestiary, flow.Bestiary);
                Assert.GreaterOrEqual(flow.Bestiary.Entries.Count, known);
            }
        }

        [Test]
        public void Bestiary_FromTheSave_IsKnownAtTheNextLaunch()
        {
            var store = new MemoryBestiaryStore();
            using (var first = NewDemoFlow(store))
            {
                FlowBot.PlayRun(first);
            }

            var loaded = BestiaryStorage.Load(store);
            Assert.AreEqual(BestiaryLoadStatus.Loaded, loaded.Status);
            using (var second = NewDemoFlow(store, loaded.Bestiary))
            {
                second.StartRun();
                Assert.That(second.Bestiary.Entries, Is.Not.Empty);
            }
        }

        [Test]
        public void Bestiary_WhenTheSaveFails_WarnsAndKeepsPlaying()
        {
            var warnings = new List<string>();
            var failing = new FailingStore();
            using (var flow = new GameFlow(
                _ => ScreenPreviewRuns.NewRun(), Config().ToPassivePool(), new Bestiary(), failing, () => 7, warnings.Add))
            {
                var visited = FlowBot.PlayRun(flow);

                Assert.AreEqual(GameScreen.Title, visited[visited.Count - 1]);
                Assert.That(warnings, Is.Not.Empty);
                Assert.IsTrue(flow.LastSaveFailed);
            }
        }

        [Test]
        public void FullRun_OnTheGameContent_GoesThroughEveryScreenAndBackToTheTitle()
        {
            var screens = new HashSet<GameScreen>();
            var outcomes = new List<RunOutcome>();
            for (ulong seed = 1; seed <= 6; seed++)
            {
                var store = new MemoryBestiaryStore();
                using (var flow = NewFlow(store, seed))
                {
                    var visited = FlowBot.PlayRun(flow);
                    foreach (var screen in visited)
                    {
                        screens.Add(screen);
                    }

                    Assert.AreEqual(GameScreen.Title, flow.Screen, $"seed {seed}");
                    Assert.IsNull(flow.Run);
                    Assert.IsNull(flow.RunController);
                    Assert.Contains(GameScreen.RunEnd, visited, $"seed {seed}");
                }
            }

            foreach (GameScreen screen in Enum.GetValues(typeof(GameScreen)))
            {
                Assert.IsTrue(screens.Contains(screen), $"No run went through the {screen} screen.");
            }
        }

        [Test]
        public void FullRun_SameSeedSameChoices_SameOutcome()
        {
            RunOutcome Play(ulong seed)
            {
                using (var flow = NewFlow(new MemoryBestiaryStore(), seed))
                {
                    var outcome = RunOutcome.InProgress;
                    flow.ScreenChanged += () =>
                    {
                        if (flow.Screen == GameScreen.RunEnd)
                        {
                            outcome = flow.Run.Outcome;
                        }
                    };
                    FlowBot.PlayRun(flow);
                    return outcome;
                }
            }

            Assert.AreEqual(Play(3), Play(3));
        }

        [Test]
        public void Dispose_DuringAFight_CancelsTheSession()
        {
            var flow = NewFlow(new MemoryBestiaryStore());
            flow.StartRun();
            flow.RunController.ChooseStep(RunStep.RegularFight);
            var run = flow.Run;
            Assert.IsNotNull(run.CurrentFight);

            flow.Dispose();

            Assert.IsNull(run.CurrentFight);
        }

        // Plays regular fights, taking the level-ups on the way, until a fight ends with a level-up waiting (the recap is shown).
        private static void PlayRegularFightsUntilALevelIsPending(GameFlow flow)
        {
            for (var i = 0; i < 50; i++)
            {
                flow.RunController.ChooseStep(RunStep.RegularFight);
                while (flow.Screen == GameScreen.Run)
                {
                    flow.RunController.StepOneTick();
                }

                if (flow.Run.HasPendingChoice)
                {
                    return;
                }

                flow.ContinueFromRecap();
            }

            Assert.Fail("No regular fight gave a level.");
        }

        private static void PlayUntilARoomIsUnlocked(GameFlow flow)
        {
            for (var i = 0; i < 4000 && !(flow.Screen == GameScreen.Run && flow.Run.AvailableSteps.Any(step => step.Kind == RunStepKind.SecretRoom)); i++)
            {
                switch (flow.Screen)
                {
                    case GameScreen.Run:
                        if (flow.RunController.Phase == RunScreenPhase.Fighting)
                        {
                            flow.RunController.StepOneTick();
                        }
                        else
                        {
                            flow.RunController.ChooseStep(RunStep.RegularFight);
                        }

                        break;
                    case GameScreen.LevelUp:
                        flow.LevelUp.SelectPackage(0);
                        if (flow.LevelUp.LineIsFull)
                        {
                            flow.LevelUp.SelectReserve();
                        }

                        flow.TakeLevelUp(flow.LevelUp.Confirm());
                        break;
                    case GameScreen.Recap:
                        flow.ContinueFromRecap();
                        break;
                    default:
                        Assert.Fail("Unexpected screen " + flow.Screen);
                        break;
                }
            }

            Assert.AreEqual(GameScreen.Run, flow.Screen);
            Assert.That(flow.Run.AvailableSteps.Any(step => step.Kind == RunStepKind.SecretRoom), "The demo room never unlocked.");
        }

        private sealed class FailingStore : IBestiaryStore
        {
            public bool TryRead(out string text)
            {
                text = null;
                return false;
            }

            public void Write(string text)
            {
                throw new System.IO.IOException("disk full");
            }
        }
    }
}
