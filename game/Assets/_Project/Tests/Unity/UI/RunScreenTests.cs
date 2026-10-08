using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Runs;
using Game.Unity.EditorTools.UI;
using Game.Unity.UI;
using Game.Unity.UI.RunScreen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Game.Unity.Tests.UI
{
    /// <summary>
    /// The run screen (#73): pacing, flow, view models and the view, all without entering Play mode. The runs come
    /// from <see cref="ScreenPreviewRuns"/> (demo cards: boost, strike, ward in the line, bolt in the reserve).
    /// </summary>
    public class RunScreenTests
    {
        private const string RoomId = "demo_room";

        private static Run NewRun() => ScreenPreviewRuns.NewRun();

        private static RunScreenController NewController(Run run = null) =>
            new RunScreenController(run ?? NewRun(), new RunScreenSettings(4d, new[] { 1d, 2d, 4d }));

        private static void PlayToEnd(RunScreenController controller)
        {
            for (var i = 0; i < 1000 && controller.Phase == RunScreenPhase.Fighting; i++)
            {
                controller.StepOneTick();
            }
        }

        // A hero that cannot survive the first fight.
        private static Run DeadlyRun()
        {
            var weak = new ClassDefinition(
                "demo_weak",
                1,
                0,
                1,
                new[] { new CardDefinition("demo_idle", 5, new IEffect[0]) },
                new CardDefinition[0]);
            var smash = new CardDefinition("demo_smash", 1, new IEffect[] { new DealDamageEffect(50) });
            var ogre = new EnemyDefinition("demo_ogre", 500, 0, new[] { smash }, 5);
            var encounter = new EncounterDefinition("demo_ogre_encounter", new[] { ogre });
            var biome = new BiomeDefinition("demo_deadly_biome", new[] { encounter }, 1, encounter, ogre);
            return new Run(weak, biome, new RunRules(100, new LevelCurve(new[] { 10 }, 5)), 1UL);
        }

        // --- FightPacer ---

        [Test]
        public void Pacer_TurnsSecondsIntoWholeTicksAndKeepsTheRest()
        {
            var pacer = new FightPacer(4d, new[] { 1d, 2d });

            Assert.AreEqual(0, pacer.TicksDue(0.1d));
            Assert.AreEqual(1, pacer.TicksDue(0.2d));
            Assert.AreEqual(4, pacer.TicksDue(1d));
        }

        [Test]
        public void Pacer_SpeedMultipliesTicks_AndPauseGivesNone()
        {
            var pacer = new FightPacer(4d, new[] { 1d, 2d }) { SpeedIndex = 1 };

            Assert.AreEqual(8, pacer.TicksDue(1d));
            pacer.IsPaused = true;
            Assert.AreEqual(0, pacer.TicksDue(10d));
            pacer.IsPaused = false;
            Assert.AreEqual(0, pacer.TicksDue(0d));
        }

        [Test]
        public void Pacer_RejectsBadSettings()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FightPacer(0d, new[] { 1d }));
            Assert.Throws<ArgumentException>(() => new FightPacer(1d, new double[0]));
            Assert.Throws<ArgumentException>(() => new FightPacer(1d, new[] { 2d, 1d }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FightPacer(1d, new[] { 0d }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FightPacer(1d, new[] { 1d }).TicksDue(-1d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FightPacer(1d, new[] { 1d }).SpeedIndex = 1);
        }

        // --- Controller: choosing the next step ---

        [Test]
        public void NewScreen_OffersTheRegularFight_AndLocksTheProfessorWithItsReason()
        {
            var controller = NewController();

            var choices = controller.StepChoices;

            Assert.AreEqual(RunScreenPhase.ChoosingStep, controller.Phase);
            Assert.AreEqual(2, choices.Count);
            Assert.AreEqual(RunStep.RegularFight, choices[0].Step);
            Assert.IsTrue(choices[0].IsEnabled);
            Assert.AreEqual(RunStep.Professor, choices[1].Step);
            Assert.IsFalse(choices[1].IsEnabled);
            Assert.AreEqual("Win 2 more regular fights.", choices[1].DisabledReason);
        }

        [Test]
        public void StepChoices_OnceTheProfessorIsAvailable_AreEnabled()
        {
            var run = NewRun();
            run.Play(RunStep.RegularFight);
            run.Play(RunStep.RegularFight);
            while (run.HasPendingChoice)
            {
                run.ConsumePendingLevelUp();
            }

            var choices = new RunScreenController(run).StepChoices;

            Assert.IsTrue(choices.All(choice => choice.IsEnabled));
            Assert.AreEqual(RunStep.Professor, choices.Last().Step);
        }

        [Test]
        public void StepChoices_ListUnlockedSecretRooms()
        {
            var run = NewRun();
            run.UnlockSecretRoom(RoomId, run.Biome.SecretRooms[0].MiniBossEncounter);

            var steps = new RunScreenController(run).StepChoices.Select(choice => choice.Step).ToList();

            Assert.That(steps, Has.Member(RunStep.SecretRoom(RoomId)));
        }

        [Test]
        public void ChooseStep_RegularFight_StartsAFightEditableInTheSession()
        {
            var run = NewRun();
            var controller = NewController(run);

            Assert.IsTrue(controller.ChooseStep(RunStep.RegularFight));

            Assert.AreEqual(RunScreenPhase.Fighting, controller.Phase);
            Assert.AreSame(run.CurrentFight, controller.Session);
            Assert.IsTrue(controller.CanEditLine);
            Assert.AreEqual(0, controller.Session.Tick);
        }

        [Test]
        public void ChooseStep_AnUnavailableStep_IsRefused()
        {
            var controller = NewController();

            Assert.IsFalse(controller.ChooseStep(RunStep.Professor));
            Assert.AreEqual(RunScreenPhase.ChoosingStep, controller.Phase);
        }

        [Test]
        public void ChooseStep_SecretRoom_AsksForThePreparationInsteadOfStarting()
        {
            var run = NewRun();
            run.UnlockSecretRoom(RoomId, run.Biome.SecretRooms[0].MiniBossEncounter);
            var controller = NewController(run);
            var requested = new List<RunStep>();
            controller.PreparationRequested += requested.Add;

            Assert.IsTrue(controller.ChooseStep(RunStep.SecretRoom(RoomId)));

            Assert.AreEqual(new[] { RunStep.SecretRoom(RoomId) }, requested);
            Assert.AreEqual(RunScreenPhase.ChoosingStep, controller.Phase);
            Assert.IsNull(run.CurrentFight);
        }

        [Test]
        public void StartFight_AfterThePreparation_PlaysAMiniBossWithAFixedLine()
        {
            var run = NewRun();
            run.UnlockSecretRoom(RoomId, run.Biome.SecretRooms[0].MiniBossEncounter);
            var controller = NewController(run);

            controller.StartFight(RunStep.SecretRoom(RoomId));
            controller.ClickLineSlot(0);
            controller.ClickLineSlot(1);

            Assert.AreEqual(RunScreenPhase.Fighting, controller.Phase);
            Assert.IsFalse(controller.CanEditLine);
            Assert.AreEqual(-1, controller.SelectedLinePosition);
            Assert.AreEqual("demo_boost", controller.Session.HeroLine[0].Id);
        }

        // --- Controller: a pending level-up blocks the next fight ---

        [Test]
        public void PendingLevelUp_BlocksEveryStep_AndAsksForTheChoice()
        {
            var run = NewRun();
            run.Play(RunStep.RegularFight);
            var controller = NewController(run);
            var asked = 0;
            controller.PendingChoiceRequested += () => asked++;

            Assert.IsTrue(run.HasPendingChoice);
            Assert.IsFalse(controller.CanStartStep);
            Assert.IsTrue(controller.StepChoices.All(choice => !choice.IsEnabled));
            Assert.IsFalse(controller.ChooseStep(RunStep.RegularFight));
            Assert.AreEqual(1, asked);
            Assert.IsNull(run.CurrentFight);
            Assert.Throws<InvalidOperationException>(() => controller.StartFight(RunStep.RegularFight));
        }

        [Test]
        public void PendingLevelUp_OnceTaken_FreesTheSteps()
        {
            var run = NewRun();
            run.Play(RunStep.RegularFight);
            var controller = NewController(run);

            run.ConsumePendingLevelUp();
            controller.ChoiceResolved();

            Assert.IsTrue(controller.CanStartStep);
            Assert.IsTrue(controller.ChooseStep(RunStep.RegularFight));
            Assert.AreEqual(RunScreenPhase.Fighting, controller.Phase);
        }

        [Test]
        public void OpenPendingChoiceIfAny_RaisesTheEventOnlyWhenOneWaits()
        {
            var run = NewRun();
            var controller = NewController(run);
            var asked = 0;
            controller.PendingChoiceRequested += () => asked++;

            Assert.IsFalse(controller.OpenPendingChoiceIfAny());
            run.Play(RunStep.RegularFight);
            Assert.IsTrue(controller.OpenPendingChoiceIfAny());
            Assert.AreEqual(1, asked);
        }

        // --- Controller: the fight ---

        [Test]
        public void Advance_PlaysTheTicksDueAtTheCurrentSpeed()
        {
            var controller = NewController();
            controller.StartFight(RunStep.RegularFight);

            controller.Advance(1d);
            Assert.AreEqual(4, controller.Session.Tick);

            controller.CycleSpeed();
            controller.Advance(0.5d);
            Assert.AreEqual(8, controller.Session.Tick);
        }

        [Test]
        public void Advance_WhilePaused_PlaysNothing()
        {
            var controller = NewController();
            controller.StartFight(RunStep.RegularFight);

            controller.Pacer.IsPaused = true;
            controller.Advance(5d);

            Assert.AreEqual(0, controller.Session.Tick);
        }

        [Test]
        public void Advance_ACrazyLongFrame_IsCapped()
        {
            var controller = NewController();
            controller.StartFight(RunStep.RegularFight);

            controller.Advance(1e6);

            Assert.LessOrEqual(controller.Session?.Tick ?? RunScreenSettings.MaxTicksPerAdvance, RunScreenSettings.MaxTicksPerAdvance);
        }

        [Test]
        public void CycleSpeed_GoesThroughTheSpeedsAndWrapsAround()
        {
            var controller = NewController();

            controller.CycleSpeed();
            controller.CycleSpeed();
            Assert.AreEqual(4d, controller.Pacer.Speed);
            controller.CycleSpeed();
            Assert.AreEqual(1d, controller.Pacer.Speed);
        }

        [Test]
        public void FightEnd_IsCompletedByTheRun_AndShowsTheResult()
        {
            var run = NewRun();
            var controller = NewController(run);
            RunFightReport reported = null;
            controller.FightCompleted += report => reported = report;
            controller.StartFight(RunStep.RegularFight);

            PlayToEnd(controller);

            Assert.AreEqual(RunScreenPhase.FightResult, controller.Phase);
            Assert.IsNull(run.CurrentFight);
            Assert.IsNull(controller.Session);
            Assert.AreSame(reported, controller.LastReport);
            Assert.IsTrue(reported.HeroWon);
            Assert.AreEqual(1, run.FightsPlayed);
            Assert.AreEqual(10, run.TotalXp);
        }

        [Test]
        public void Continue_AfterAWin_OpensThePendingLevelUpChoice()
        {
            var run = NewRun();
            var controller = NewController(run);
            var asked = 0;
            controller.PendingChoiceRequested += () => asked++;
            controller.StartFight(RunStep.RegularFight);
            PlayToEnd(controller);

            controller.Continue();

            Assert.AreEqual(RunScreenPhase.ChoosingStep, controller.Phase);
            Assert.AreEqual(1, asked);
            Assert.IsFalse(controller.CanStartStep);
        }

        [Test]
        public void Continue_AfterADefeat_EndsTheRun()
        {
            var run = DeadlyRun();
            var controller = NewController(run);
            RunOutcome? ended = null;
            controller.RunEnded += outcome => ended = outcome;
            controller.StartFight(RunStep.RegularFight);
            PlayToEnd(controller);

            Assert.AreEqual(RunScreenPhase.FightResult, controller.Phase);
            Assert.IsNull(ended);
            controller.Continue();

            Assert.AreEqual(RunScreenPhase.RunEnded, controller.Phase);
            Assert.AreEqual(RunOutcome.Defeat, ended);
            Assert.IsFalse(controller.ChooseStep(RunStep.RegularFight));
            Assert.AreEqual(0, controller.StepChoices.Count);
        }

        [Test]
        public void Leave_CancelsTheSession_AndDoesNotCountTheFight()
        {
            var run = NewRun();
            var controller = NewController(run);
            controller.StartFight(RunStep.RegularFight);
            controller.Advance(1d);

            controller.Leave();

            Assert.AreEqual(RunScreenPhase.ChoosingStep, controller.Phase);
            Assert.IsNull(run.CurrentFight);
            Assert.AreEqual(0, run.FightsPlayed);
            Assert.IsTrue(controller.ChooseStep(RunStep.RegularFight));
        }

        [Test]
        public void Dispose_WhileFighting_CancelsTheSession()
        {
            var run = NewRun();
            var controller = NewController(run);
            controller.StartFight(RunStep.RegularFight);

            controller.Dispose();

            Assert.IsNull(run.CurrentFight);
            Assert.AreEqual(0, run.FightsPlayed);
        }

        [Test]
        public void NewController_TakesOverAnOpenSession()
        {
            var run = NewRun();
            var session = run.BeginFight(RunStep.RegularFight);

            var controller = new RunScreenController(run);

            Assert.AreEqual(RunScreenPhase.Fighting, controller.Phase);
            Assert.AreSame(session, controller.Session);
            controller.Dispose();
        }

        // --- Controller: editing the line ---

        [Test]
        public void ClickingTwoLineSlots_MovesTheCardAtOnce()
        {
            var run = NewRun();
            var controller = NewController(run);
            controller.StartFight(RunStep.RegularFight);

            controller.ClickLineSlot(0);
            Assert.AreEqual(0, controller.SelectedLinePosition);
            controller.ClickLineSlot(2);

            Assert.AreEqual(-1, controller.SelectedLinePosition);
            CollectionAssert.AreEqual(
                new[] { "demo_strike", "demo_ward", "demo_boost" },
                controller.Session.HeroLine.Select(card => card.Id));
            CollectionAssert.AreEqual(
                new[] { "demo_strike", "demo_ward", "demo_boost" },
                run.Line.Select(card => card.Definition.Id));
        }

        [Test]
        public void ClickingALineSlotThenAReserveCard_SwapsThemInAnyOrder()
        {
            var run = NewRun();
            var controller = NewController(run);
            controller.StartFight(RunStep.RegularFight);

            controller.ClickLineSlot(1);
            controller.ClickReserveCard(0);
            Assert.AreEqual("demo_bolt", controller.Session.HeroLine[1].Id);
            Assert.AreEqual("demo_strike", controller.Session.HeroReserve[0].Id);

            controller.ClickReserveCard(0);
            Assert.AreEqual(0, controller.SelectedReserveIndex);
            controller.ClickLineSlot(1);
            Assert.AreEqual("demo_strike", controller.Session.HeroLine[1].Id);
            Assert.AreEqual("demo_bolt", controller.Session.HeroReserve[0].Id);
        }

        [Test]
        public void ClickingTheSelectedSlotAgain_DropsTheSelection()
        {
            var controller = NewController();
            controller.StartFight(RunStep.RegularFight);

            controller.ClickLineSlot(1);
            controller.ClickLineSlot(1);

            Assert.AreEqual(-1, controller.SelectedLinePosition);
        }

        [Test]
        public void EditingBetweenFights_IsIgnored()
        {
            var run = NewRun();
            var controller = NewController(run);

            controller.ClickLineSlot(0);
            controller.ClickLineSlot(1);

            Assert.AreEqual("demo_boost", run.Line[0].Definition.Id);
        }

        [Test]
        public void ALineEditAndTheRestOfTheFight_StillGiveAFightThatEnds()
        {
            var controller = NewController();
            controller.StartFight(RunStep.RegularFight);
            controller.Advance(1d);

            controller.ClickLineSlot(0);
            controller.ClickLineSlot(2);
            PlayToEnd(controller);

            Assert.AreEqual(RunScreenPhase.FightResult, controller.Phase);
            Assert.IsTrue(controller.LastReport.Log.Events.Any(e => e.Kind == Game.Core.Combat.Log.CombatEventKind.LineChanged));
        }

        // --- View model ---

        [Test]
        public void ViewModel_BetweenFights_ShowsTheHudAndTheStepChoices()
        {
            var model = RunScreenViewModel.From(NewController());

            Assert.AreEqual("Level 1", model.Hud.LevelText);
            Assert.AreEqual("XP 0/10", model.Hud.XpText);
            Assert.AreEqual(0d, model.Hud.XpFraction);
            Assert.AreEqual("Line 3/3", model.Hud.LineCapacityText);
            Assert.AreEqual("Reserve 1", model.Hud.ReserveText);
            Assert.IsNull(model.Hud.PendingLevelUpText);
            Assert.AreEqual(1, model.Hud.Objectives.Count);
            Assert.AreEqual("Defeat demo_slime x2", model.Hud.Objectives[0].Text);
            Assert.AreEqual("0/2", model.Hud.Objectives[0].ProgressText);
            Assert.AreEqual(3, model.Line.Count);
            Assert.AreEqual(1, model.Reserve.Count);
            Assert.AreEqual(2, model.Steps.Count);
            Assert.IsNull(model.Fight);
            Assert.IsNull(model.Result);
        }

        [Test]
        public void ViewModel_ObjectiveProgress_FollowsTheFights()
        {
            var run = NewRun();
            while (run.SecretRooms[0].ObjectiveProgress == 0 && run.FightsPlayed < 20)
            {
                run.Play(RunStep.RegularFight);
            }

            var objective = RunScreenViewModel.From(NewController(run)).Hud.Objectives[0];

            Assert.AreEqual($"{run.SecretRooms[0].ObjectiveProgress}/2", objective.ProgressText);
            Assert.AreEqual(0.5d, objective.Fraction);
        }

        [Test]
        public void ViewModel_DuringAFight_ShowsBothSidesAndTheLine()
        {
            var controller = NewController();
            controller.StartFight(RunStep.RegularFight);
            controller.StepOneTick();
            controller.StepOneTick();

            var model = RunScreenViewModel.From(controller);

            Assert.IsNotNull(model.Fight);
            Assert.AreEqual("Hero", model.Fight.Hero.Name);
            Assert.AreEqual(40, model.Fight.Hero.MaxHealth);
            Assert.AreEqual(1, model.Fight.Enemies.Count);
            Assert.AreEqual("Tick 2", model.Fight.TickText);
            Assert.AreEqual("Speed x1", model.Fight.SpeedText);
            Assert.IsTrue(model.Fight.CanEditLine);
            Assert.IsNotEmpty(model.Fight.Enemies[0].CastText);

            // Tick 2 resolved the boost (cast time 2): the strike now waits with its damage bonus and is cast next.
            Assert.IsTrue(model.Line[1].IsNext);
            Assert.AreEqual("+3 damage", model.Line[1].PendingBonusText);
            Assert.IsTrue(model.Line[1].HasPendingBonus);
            Assert.IsFalse(model.Line[0].HasPendingBonus);
        }

        [Test]
        public void ViewModel_TheCardBeingCast_IsMarkedWithItsProgress()
        {
            var controller = NewController();
            controller.StartFight(RunStep.RegularFight);
            controller.StepOneTick();

            var line = RunScreenViewModel.From(controller).Line;

            Assert.IsTrue(line[0].IsCasting);
            Assert.AreEqual(0.5d, line[0].CastFraction);
            Assert.IsFalse(line[1].IsCasting);
        }

        [Test]
        public void ViewModel_APendingBonus_StaysVisibleOnItsSlotAfterAMove()
        {
            var controller = NewController();
            controller.StartFight(RunStep.RegularFight);
            controller.StepOneTick();
            controller.StepOneTick();

            controller.ClickLineSlot(1);
            controller.ClickLineSlot(2);
            var line = RunScreenViewModel.From(controller).Line;

            Assert.AreEqual("demo_ward", line[1].CardId);
            Assert.AreEqual("+3 damage", line[1].PendingBonusText);
        }

        [Test]
        public void ViewModel_Selection_IsShownOnTheSlotOrTheReserveCard()
        {
            var controller = NewController();
            controller.StartFight(RunStep.RegularFight);

            controller.ClickLineSlot(2);
            Assert.IsTrue(RunScreenViewModel.From(controller).Line[2].IsSelected);

            controller.ClearSelection();
            controller.ClickReserveCard(0);
            var model = RunScreenViewModel.From(controller);
            Assert.IsTrue(model.Reserve[0].IsSelected);
            Assert.IsFalse(model.Line[2].IsSelected);
        }

        [Test]
        public void ViewModel_PausedFight_SaysSo()
        {
            var controller = NewController();
            controller.StartFight(RunStep.RegularFight);
            controller.Pacer.IsPaused = true;

            Assert.IsTrue(RunScreenViewModel.From(controller).Fight.IsPaused);
        }

        [Test]
        public void ViewModel_FixedLine_ExplainsWhyTheLineCannotBeEdited()
        {
            var run = NewRun();
            run.UnlockSecretRoom(RoomId, run.Biome.SecretRooms[0].MiniBossEncounter);
            var controller = NewController(run);
            controller.StartFight(RunStep.SecretRoom(RoomId));

            var fight = RunScreenViewModel.From(controller).Fight;

            Assert.IsFalse(fight.CanEditLine);
            Assert.AreEqual("The line is fixed in this fight.", fight.EditHintText);
        }

        [Test]
        public void ViewModel_AfterAWin_ShowsXpAndTheLevelUp()
        {
            var controller = NewController();
            controller.StartFight(RunStep.RegularFight);
            PlayToEnd(controller);

            var model = RunScreenViewModel.From(controller);

            Assert.AreEqual("Victory", model.Result.Title);
            Assert.IsTrue(model.Result.IsVictory);
            Assert.That(model.Result.Lines, Has.Member("+10 XP"));
            Assert.That(model.Result.Lines, Has.Member("Level up: now level 2"));
            Assert.AreEqual("Choose level-up", model.Result.ContinueText);
            Assert.AreEqual("Level-up waiting (1)", model.Hud.PendingLevelUpText);
            Assert.IsTrue(model.HasPendingChoice);
            Assert.AreEqual(0, model.Steps.Count);
        }

        [Test]
        public void ViewModel_AfterADefeat_ShowsDefeat()
        {
            var controller = NewController(DeadlyRun());
            controller.StartFight(RunStep.RegularFight);
            PlayToEnd(controller);

            var result = RunScreenViewModel.From(controller).Result;

            Assert.AreEqual("Defeat", result.Title);
            Assert.IsFalse(result.IsVictory);
            Assert.AreEqual("Continue", result.ContinueText);
        }

        [Test]
        public void ViewModel_EmptyAndPassiveCards_AreDescribed()
        {
            var boost = new CardDefinition(
                "demo_boost",
                2,
                new IEffect[0],
                new[] { new NeighbourModifier(BonusKind.Shield, NeighbourDirection.Previous, 2) });
            var strike = new CardDefinition("demo_strike", 3, new IEffect[] { new DealDamageEffect(4), new HealEffect(1) });

            Assert.AreEqual("t2: no direct effect | previous +2 shield", RunScreenViewModel.DescribeCard(boost));
            Assert.AreEqual("t3: damage 4, heal 1", RunScreenViewModel.DescribeCard(strike));
            Assert.IsNull(RunScreenViewModel.DescribeBonus(EffectBonus.None));
            Assert.AreEqual("+2 damage, +1 shield", RunScreenViewModel.DescribeBonus(new EffectBonus(2, 0, 1)));
        }

        // --- View ---

        private static VisualElement CloneRunScreen()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BootstrapSceneBuilder.RunScreenPath);
            Assert.IsNotNull(layout, $"Missing {BootstrapSceneBuilder.RunScreenPath}.");
            return layout.CloneTree();
        }

        private static bool Hidden(VisualElement root, string name) =>
            root.Q(name).ClassListContains(RunScreenView.HiddenClass);

        [Test]
        public void Layout_HasEveryElementTheViewNeeds()
        {
            var root = CloneRunScreen();

            Assert.DoesNotThrow(() => RunScreenView.Bind(root, NewController()).Dispose());
        }

        [Test]
        public void Bind_MissingElement_NamesIt()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => RunScreenView.Bind(new VisualElement(), NewController()));

            StringAssert.Contains(RunScreenView.HudLevelElement, ex.Message);
        }

        [Test]
        public void View_BetweenFights_ShowsTheStepButtonsAndTheLine()
        {
            var root = CloneRunScreen();
            using (RunScreenView.Bind(root, NewController()))
            {
                Assert.IsFalse(Hidden(root, RunScreenView.StepsPanelElement));
                Assert.IsTrue(Hidden(root, RunScreenView.FightPanelElement));
                Assert.IsTrue(Hidden(root, RunScreenView.ResultPanelElement));
                Assert.IsTrue(Hidden(root, RunScreenView.ControlsElement));
                Assert.IsTrue(Hidden(root, RunScreenView.PendingChoiceButtonElement));
                var steps = root.Q(RunScreenView.StepsElement).Children().ToList();
                Assert.AreEqual(2, steps.Count);
                Assert.IsTrue(steps[0].Q<Button>().enabledSelf);
                Assert.IsFalse(steps[1].Q<Button>().enabledSelf);
                Assert.AreEqual(3, root.Q(RunScreenView.LineElement).childCount);
                Assert.AreEqual(1, root.Q(RunScreenView.ReserveElement).childCount);
                Assert.AreEqual("Level 1", root.Q<Label>(RunScreenView.HudLevelElement).text);
                Assert.AreEqual(1, root.Q(RunScreenView.ObjectivesElement).childCount);
            }
        }

        [Test]
        public void View_DuringAFight_ShowsShapesControlsAndLiveSlots()
        {
            var root = CloneRunScreen();
            var controller = NewController();
            using (var binding = RunScreenView.Bind(root, controller))
            {
                controller.StartFight(RunStep.RegularFight);
                controller.StepOneTick();
                controller.StepOneTick();
                binding.Refresh();

                Assert.IsTrue(Hidden(root, RunScreenView.StepsPanelElement));
                Assert.IsFalse(Hidden(root, RunScreenView.FightPanelElement));
                Assert.IsFalse(Hidden(root, RunScreenView.ControlsElement));
                Assert.AreEqual(1, root.Q(RunScreenView.HeroElement).childCount);
                Assert.AreEqual(1, root.Q(RunScreenView.EnemiesElement).childCount);
                Assert.AreEqual("Pause", root.Q<Button>(RunScreenView.PauseButtonElement).text);

                var line = root.Q(RunScreenView.LineElement);
                Assert.IsTrue(line[1].ClassListContains(RunScreenView.SlotNextClass));
                Assert.IsFalse(line[1].Q<Label>("slot-bonus").ClassListContains(RunScreenView.HiddenClass));
                StringAssert.Contains("+3 damage", line[1].Q<Label>("slot-bonus").text);
                StringAssert.Contains("Pick a line card", root.Q<Label>(RunScreenView.EditHintElement).text);
            }
        }

        [Test]
        public void View_SelectingAndMoving_UpdatesTheSlotsWithoutRebuildingThem()
        {
            var root = CloneRunScreen();
            var controller = NewController();
            using (var binding = RunScreenView.Bind(root, controller))
            {
                controller.StartFight(RunStep.RegularFight);
                binding.Refresh();
                var line = root.Q(RunScreenView.LineElement);
                var firstSlot = line[0];

                controller.ClickLineSlot(0);
                binding.Refresh();
                Assert.IsTrue(line[0].ClassListContains(RunScreenView.SlotSelectedClass));

                controller.ClickLineSlot(2);
                binding.Refresh();
                Assert.AreSame(firstSlot, line[0]);
                StringAssert.Contains("demo_strike", line[0].Q<Label>("slot-title").text);
                StringAssert.Contains("demo_boost", line[2].Q<Label>("slot-title").text);
                Assert.IsFalse(line[0].ClassListContains(RunScreenView.SlotSelectedClass));
            }
        }

        [Test]
        public void View_FightResult_ShowsTheResultAndTheContinueButton()
        {
            var root = CloneRunScreen();
            var controller = NewController();
            using (var binding = RunScreenView.Bind(root, controller))
            {
                controller.StartFight(RunStep.RegularFight);
                PlayToEnd(controller);
                binding.Refresh();

                Assert.IsFalse(Hidden(root, RunScreenView.ResultPanelElement));
                Assert.IsTrue(Hidden(root, RunScreenView.FightPanelElement));
                Assert.IsTrue(Hidden(root, RunScreenView.StepsPanelElement));
                Assert.AreEqual("Victory", root.Q<Label>(RunScreenView.ResultTitleElement).text);
                Assert.IsTrue(root.Q<Label>(RunScreenView.ResultTitleElement).ClassListContains(RunScreenView.VictoryTitleClass));
                Assert.AreEqual("Choose level-up", root.Q<Button>(RunScreenView.ContinueButtonElement).text);
                Assert.GreaterOrEqual(root.Q(RunScreenView.ResultLinesElement).childCount, 2);
            }
        }

        [Test]
        public void View_APendingLevelUp_DisablesTheStepsAndShowsTheChoiceButton()
        {
            var run = NewRun();
            run.Play(RunStep.RegularFight);
            var root = CloneRunScreen();
            using (RunScreenView.Bind(root, NewController(run)))
            {
                Assert.IsFalse(Hidden(root, RunScreenView.PendingChoiceButtonElement));
                Assert.IsFalse(Hidden(root, RunScreenView.HudPendingElement));
                var buttons = root.Q(RunScreenView.StepsElement).Query<Button>().ToList();
                Assert.IsTrue(buttons.All(button => !button.enabledSelf));
            }
        }

        [Test]
        public void View_Dispose_CancelsAnOpenFight_AndRefreshingAfterwardsDoesNothing()
        {
            var run = NewRun();
            var root = CloneRunScreen();
            var controller = NewController(run);
            var binding = RunScreenView.Bind(root, controller);
            controller.StartFight(RunStep.RegularFight);

            binding.Dispose();

            Assert.IsNull(run.CurrentFight);
            Assert.DoesNotThrow(binding.Refresh);
            Assert.DoesNotThrow(binding.Dispose);
        }

        [Test]
        public void ScreenHost_HidingTheScreen_CancelsTheFight()
        {
            var run = NewRun();
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BootstrapSceneBuilder.RunScreenPath);
            var host = new ScreenHost(new VisualElement());
            var controller = NewController(run);
            RunScreenView.RunScreenBinding binding = null;
            var screen = host.Show(layout, _ => binding.Dispose());
            binding = RunScreenView.Bind(screen, controller);
            controller.StartFight(RunStep.RegularFight);
            Assert.IsNotNull(run.CurrentFight);

            host.Clear();

            Assert.IsNull(run.CurrentFight);
        }

        // --- Preview ---

        [Test]
        public void Previews_IncludeTheRunScreenStates()
        {
            var names = ScreenPreviews.All.Select(preview => preview.Name).ToList();

            Assert.That(names, Has.Member("Run: next step"));
            Assert.That(names, Has.Member("Run: regular fight"));
            Assert.That(names, Has.Member("Run: level-up pending"));
            Assert.That(names, Has.Member("Run: fight result"));
        }

        [Test]
        public void Previews_RunFight_ShowsALiveFight()
        {
            var host = new ScreenHost(new VisualElement());

            var screen = ScreenPreviews.All.Single(p => p.Name == "Run: regular fight").ShowIn(host);

            Assert.IsFalse(screen.Q(RunScreenView.FightPanelElement).ClassListContains(RunScreenView.HiddenClass));
            host.Clear();
        }
    }
}
