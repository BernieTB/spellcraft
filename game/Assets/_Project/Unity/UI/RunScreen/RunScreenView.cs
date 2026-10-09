using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UIElements;

namespace Game.Unity.UI.RunScreen
{
    /// <summary>
    /// The run screen (<c>Screens/RunScreen.uxml</c>, #73, ADR 0008): fills a cloned tree from a
    /// <see cref="RunScreenViewModel"/>, forwards clicks to the <see cref="RunScreenController"/> and advances the fight
    /// with the panel's clock. It holds no rule: what is shown comes from the view model, what happens comes from Core
    /// through the controller. Placeholder shapes only; colours and sizes live in <c>Styles/Run.uss</c>.
    /// </summary>
    public static class RunScreenView
    {
        /// <summary>Names of the elements the layout must have.</summary>
        public const string HudLevelElement = "hud-level";
        public const string XpFillElement = "xp-fill";
        public const string HudXpElement = "hud-xp";
        public const string HudLineElement = "hud-line";
        public const string HudReserveElement = "hud-reserve";
        public const string HudPendingElement = "hud-pending";
        public const string ObjectivesElement = "objectives";
        public const string StatusElement = "status";
        public const string StepsPanelElement = "steps-panel";
        public const string StepsElement = "steps";
        public const string PendingChoiceButtonElement = "pending-choice-button";
        public const string FightPanelElement = "fight-panel";
        public const string HeroElement = "hero";
        public const string EnemiesElement = "enemies";
        public const string ResultPanelElement = "result-panel";
        public const string ResultTitleElement = "result-title";
        public const string ResultLinesElement = "result-lines";
        public const string ContinueButtonElement = "continue-button";
        public const string ControlsElement = "controls";
        public const string FightInfoElement = "fight-info";
        public const string PauseButtonElement = "pause-button";
        public const string SpeedButtonElement = "speed-button";
        public const string LeaveButtonElement = "leave-button";
        public const string EditHintElement = "edit-hint";
        public const string LineElement = "line";
        public const string ReserveElement = "reserve";

        /// <summary>Style classes the view toggles (defined in <c>Styles/Run.uss</c> and <c>Common.uss</c>).</summary>
        public const string HiddenClass = "hidden";
        public const string SlotClass = "slot";
        public const string SlotCastingClass = "slot--casting";
        public const string SlotNextClass = "slot--next";
        public const string SlotSelectedClass = "slot--selected";
        public const string SlotEvolvedClass = "slot--evolved";
        public const string StepClass = "step";
        public const string ObjectiveClass = "objective";
        public const string CombatantBoxClass = "combatant-box";
        public const string CombatantDeadClass = "combatant-box--dead";
        public const string VictoryTitleClass = "result-title--victory";

        /// <summary>Real milliseconds between two refreshes while the screen is shown.</summary>
        private const long RefreshIntervalMs = 33;

        /// <summary>
        /// Fills a cloned run screen tree and keeps it up to date. Disposing the returned binding stops the refresh
        /// and cancels an open fight (<see cref="RunScreenController.Dispose"/>): pass <c>binding.Dispose</c> to the
        /// screen host as the hide callback.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="root"/> or <paramref name="controller"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The tree lacks one of the named elements.</exception>
        public static RunScreenBinding Bind(VisualElement root, RunScreenController controller)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            var binding = new RunScreenBinding(root, controller);
            binding.Refresh();
            return binding;
        }

        /// <summary>A bound run screen: refreshes the tree from the controller and releases what it registered.</summary>
        public sealed class RunScreenBinding : IDisposable
        {
            private readonly RunScreenController _controller;
            private readonly Label _hudLevel;
            private readonly VisualElement _xpFill;
            private readonly Label _hudXp;
            private readonly Label _hudLine;
            private readonly Label _hudReserve;
            private readonly Label _hudPending;
            private readonly VisualElement _objectives;
            private readonly Label _status;
            private readonly VisualElement _stepsPanel;
            private readonly VisualElement _steps;
            private readonly Button _pendingChoiceButton;
            private readonly VisualElement _fightPanel;
            private readonly VisualElement _hero;
            private readonly VisualElement _enemies;
            private readonly VisualElement _resultPanel;
            private readonly Label _resultTitle;
            private readonly VisualElement _resultLines;
            private readonly Button _continueButton;
            private readonly VisualElement _controls;
            private readonly Label _fightInfo;
            private readonly Button _pauseButton;
            private readonly Button _speedButton;
            private readonly Label _editHint;
            private readonly VisualElement _line;
            private readonly VisualElement _reserve;
            private IVisualElementScheduledItem _schedule;
            private string _stepsKey;
            private bool _disposed;

            internal RunScreenBinding(VisualElement root, RunScreenController controller)
            {
                _controller = controller;
                _hudLevel = Find<Label>(root, HudLevelElement);
                _xpFill = Find<VisualElement>(root, XpFillElement);
                _hudXp = Find<Label>(root, HudXpElement);
                _hudLine = Find<Label>(root, HudLineElement);
                _hudReserve = Find<Label>(root, HudReserveElement);
                _hudPending = Find<Label>(root, HudPendingElement);
                _objectives = Find<VisualElement>(root, ObjectivesElement);
                _status = Find<Label>(root, StatusElement);
                _stepsPanel = Find<VisualElement>(root, StepsPanelElement);
                _steps = Find<VisualElement>(root, StepsElement);
                _pendingChoiceButton = Find<Button>(root, PendingChoiceButtonElement);
                _fightPanel = Find<VisualElement>(root, FightPanelElement);
                _hero = Find<VisualElement>(root, HeroElement);
                _enemies = Find<VisualElement>(root, EnemiesElement);
                _resultPanel = Find<VisualElement>(root, ResultPanelElement);
                _resultTitle = Find<Label>(root, ResultTitleElement);
                _resultLines = Find<VisualElement>(root, ResultLinesElement);
                _continueButton = Find<Button>(root, ContinueButtonElement);
                _controls = Find<VisualElement>(root, ControlsElement);
                _fightInfo = Find<Label>(root, FightInfoElement);
                _pauseButton = Find<Button>(root, PauseButtonElement);
                _speedButton = Find<Button>(root, SpeedButtonElement);
                var leaveButton = Find<Button>(root, LeaveButtonElement);
                _editHint = Find<Label>(root, EditHintElement);
                _line = Find<VisualElement>(root, LineElement);
                _reserve = Find<VisualElement>(root, ReserveElement);

                // Plain clicked callbacks: the controller decides what they mean.
                _pendingChoiceButton.clicked += () => Act(() => _controller.OpenPendingChoiceIfAny());
                _continueButton.clicked += () => Act(_controller.Continue);
                _pauseButton.clicked += () => Act(() => _controller.Pacer.IsPaused = !_controller.Pacer.IsPaused);
                _speedButton.clicked += () => Act(_controller.CycleSpeed);
                leaveButton.clicked += () => Act(_controller.Leave);

                // The panel's clock drives the fight. Nothing runs until the tree is attached to a panel, so tests
                // and the preview window advance the controller by hand.
                _schedule = root.schedule.Execute(timer =>
                {
                    _controller.Advance(timer.deltaTime / 1000d);
                    Refresh();
                }).Every(RefreshIntervalMs);
            }

            /// <summary>Redraws the screen from the controller's current state.</summary>
            public void Refresh()
            {
                if (_disposed)
                {
                    return;
                }

                var model = RunScreenViewModel.From(_controller);
                ShowHud(model.Hud);
                _status.text = model.StatusText;

                var choosing = model.Phase == RunScreenPhase.BetweenFights;
                var hasSideSteps = (choosing || model.Phase == RunScreenPhase.Fighting) && model.Steps.Any(IsSideStep);
                var fighting = model.Fight != null;
                var hasResult = model.Result != null;
                SetVisible(_stepsPanel, hasSideSteps || (choosing && model.HasPendingChoice));
                SetVisible(_fightPanel, fighting);
                SetVisible(_resultPanel, hasResult);
                SetVisible(_controls, fighting);
                SetVisible(_pendingChoiceButton, choosing && model.HasPendingChoice);

                ShowSteps(model.Steps);

                if (fighting)
                {
                    ShowFight(model.Fight);
                }

                if (hasResult)
                {
                    ShowResult(model.Result);
                }

                _editHint.text = fighting ? model.Fight.EditHintText : string.Empty;
                Action<int> lineClick = null;
                Action<int> reserveClick = null;
                if (_controller.CanEditLine)
                {
                    lineClick = index => Act(() => _controller.ClickLineSlot(index));
                    reserveClick = index => Act(() => _controller.ClickReserveCard(index));
                }

                ShowSlots(_line, model.Line, lineClick);
                ShowSlots(_reserve, model.Reserve, reserveClick);
            }

            /// <summary>Stops the refresh and cancels an open fight. Safe to call twice.</summary>
            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _schedule?.Pause();
                _schedule = null;
                _controller.Dispose();
            }

            private void Act(Action action)
            {
                if (_disposed)
                {
                    return;
                }

                action();
                Refresh();
            }

            private void ShowHud(RunHudViewModel hud)
            {
                _hudLevel.text = hud.LevelText;
                _hudXp.text = hud.XpText;
                _xpFill.style.width = Length.Percent((float)(hud.XpFraction * 100d));
                _hudLine.text = hud.LineCapacityText;
                _hudReserve.text = hud.ReserveText;
                _hudPending.text = hud.PendingLevelUpText ?? string.Empty;
                SetVisible(_hudPending, hud.PendingLevelUpText != null);

                Sync(_objectives, hud.Objectives.Count, CreateObjective);
                for (var i = 0; i < hud.Objectives.Count; i++)
                {
                    var objective = hud.Objectives[i];
                    var row = _objectives[i];
                    row.Q<Label>("objective-text").text = objective.Text;
                    row.Q<Label>("objective-progress").text = objective.ProgressText;
                    row.EnableInClassList("objective--unlocked", objective.IsUnlocked && !objective.IsCleared);
                    row.EnableInClassList("objective--cleared", objective.IsCleared);
                    row.Q<VisualElement>("objective-fill").style.width = Length.Percent((float)(objective.Fraction * 100d));
                }
            }

            private static VisualElement CreateObjective()
            {
                var row = new VisualElement();
                row.AddToClassList(ObjectiveClass);
                var text = new Label { name = "objective-text" };
                text.AddToClassList("objective-text");
                var progress = new Label { name = "objective-progress" };
                progress.AddToClassList("objective-progress");
                row.Add(text);
                row.Add(progress);
                var bar = new VisualElement();
                bar.AddToClassList("bar");
                var fill = new VisualElement { name = "objective-fill" };
                fill.AddToClassList("bar-fill");
                bar.Add(fill);
                var wrapper = new VisualElement();
                wrapper.Add(row);
                wrapper.Add(bar);
                // Query by name reaches the labels through the wrapper.
                return wrapper;
            }

            // The regular fight has no button: the loop chains them (ADR 0016). Rooms and the professor are asked for.
            private static bool IsSideStep(StepChoice choice) => choice.Step.RequiresPreparation;

            private void ShowSteps(IReadOnlyList<StepChoice> steps)
            {
                var key = new StringBuilder();
                foreach (var step in steps.Where(IsSideStep))
                {
                    key.Append(step.Label).Append('|').Append(step.IsEnabled).Append('|').Append(step.DisabledReason)
                        .Append('|').Append(step.IsRequested).Append(';');
                }

                if (key.ToString() == _stepsKey)
                {
                    return;
                }

                _stepsKey = key.ToString();
                _steps.Clear();
                foreach (var choice in steps.Where(IsSideStep))
                {
                    var holder = new VisualElement();
                    holder.AddToClassList(StepClass);
                    var captured = choice;
                    var button = new Button(() => Act(() => _controller.RequestStep(captured.Step)))
                    {
                        text = choice.IsRequested ? choice.Label + " (next, click to cancel)" : choice.Label,
                    };
                    button.AddToClassList("button");
                    button.SetEnabled(choice.IsEnabled);
                    holder.Add(button);
                    if (choice.DisabledReason != null)
                    {
                        var reason = new Label(choice.DisabledReason);
                        reason.AddToClassList("step-reason");
                        holder.Add(reason);
                    }

                    _steps.Add(holder);
                }
            }

            private void ShowFight(FightViewModel fight)
            {
                _fightInfo.text = $"{fight.TickText}   {fight.SpeedText}";
                _pauseButton.text = fight.IsPaused ? "Resume" : "Pause";
                _speedButton.text = fight.SpeedText;
                ShowCombatants(_hero, new[] { fight.Hero }, false);
                ShowCombatants(_enemies, fight.Enemies, true);
            }

            private static void ShowCombatants(VisualElement container, IReadOnlyList<CombatantViewModel> combatants, bool enemy)
            {
                Sync(container, combatants.Count, () => CreateCombatant(enemy));
                for (var i = 0; i < combatants.Count; i++)
                {
                    var combatant = combatants[i];
                    var box = container[i];
                    box.EnableInClassList(CombatantDeadClass, combatant.IsDead);
                    box.Q<Label>("combatant-name").text = combatant.Name;
                    box.Q<VisualElement>("health-fill").style.width = Length.Percent((float)(combatant.HealthFraction * 100d));
                    box.Q<Label>("combatant-health").text = combatant.HealthText;
                    box.Q<VisualElement>("cast-fill").style.width = Length.Percent((float)(combatant.CastFraction * 100d));
                    box.Q<Label>("combatant-cast").text = combatant.CastText;
                }
            }

            private static VisualElement CreateCombatant(bool enemy)
            {
                var box = new VisualElement();
                box.AddToClassList(CombatantBoxClass);
                var shape = new VisualElement();
                shape.AddToClassList("shape");
                if (enemy)
                {
                    shape.AddToClassList("shape--enemy");
                }

                box.Add(shape);
                box.Add(new Label { name = "combatant-name" });
                box.Q<Label>("combatant-name").AddToClassList("combatant-name");
                box.Add(Bar("health-fill", "health-fill"));
                var health = new Label { name = "combatant-health" };
                health.AddToClassList("combatant-text");
                box.Add(health);
                var castBar = Bar("cast-fill", "cast-fill");
                castBar.AddToClassList("cast-bar");
                box.Add(castBar);
                var cast = new Label { name = "combatant-cast" };
                cast.AddToClassList("combatant-text");
                box.Add(cast);
                return box;
            }

            private static VisualElement Bar(string fillName, string fillClass)
            {
                var bar = new VisualElement();
                bar.AddToClassList("bar");
                var fill = new VisualElement { name = fillName };
                fill.AddToClassList("bar-fill");
                fill.AddToClassList(fillClass);
                bar.Add(fill);
                return bar;
            }

            private void ShowResult(FightResultViewModel result)
            {
                _resultTitle.text = result.Title;
                _resultTitle.EnableInClassList(VictoryTitleClass, result.IsVictory);
                _resultLines.Clear();
                foreach (var line in result.Lines)
                {
                    var label = new Label(line);
                    label.AddToClassList("result-line");
                    _resultLines.Add(label);
                }

                _continueButton.text = result.ContinueText;
                SetVisible(_continueButton, _controller.Phase == RunScreenPhase.FightResult);
            }

            private static void ShowSlots(VisualElement container, IReadOnlyList<CardSlotViewModel> slots, Action<int> onClick)
            {
                var count = slots.Count;
                // Handlers are registered once per element and read the click action from userData, so the same
                // elements serve every refresh.
                Sync(container, count, () => CreateSlot());
                for (var i = 0; i < count; i++)
                {
                    var slot = slots[i];
                    var element = container[i];
                    element.userData = onClick;
                    element.Q<Label>("slot-title").text = $"{slot.Index + 1}. {slot.CardId}";
                    element.Q<Label>("slot-detail").text = slot.DetailText;
                    var bonus = element.Q<Label>("slot-bonus");
                    bonus.text = slot.PendingBonusText == null ? string.Empty : $"Waiting: {slot.PendingBonusText}";
                    SetVisible(bonus, slot.HasPendingBonus);
                    var mark = element.Q<Label>("slot-stage");
                    mark.text = slot.StageMarkText;
                    SetVisible(mark, slot.HasStageMark);
                    element.EnableInClassList(SlotEvolvedClass, slot.JustEvolved);
                    element.EnableInClassList(SlotCastingClass, slot.IsCasting);
                    element.EnableInClassList(SlotNextClass, slot.IsNext);
                    element.EnableInClassList(SlotSelectedClass, slot.IsSelected);
                    element.Q<VisualElement>("slot-cast-fill").style.width = Length.Percent((float)(slot.CastFraction * 100d));
                    element.Q<VisualElement>("slot-cast-fill").parent.style.display = slot.IsCasting ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            private static VisualElement CreateSlot()
            {
                var slot = new VisualElement();
                slot.AddToClassList(SlotClass);
                var title = new Label { name = "slot-title" };
                title.AddToClassList("slot-title");
                var detail = new Label { name = "slot-detail" };
                detail.AddToClassList("slot-detail");
                var stage = new Label { name = "slot-stage" };
                stage.AddToClassList("slot-stage");
                var bonus = new Label { name = "slot-bonus" };
                bonus.AddToClassList("slot-bonus");
                slot.Add(title);
                slot.Add(stage);
                slot.Add(detail);
                slot.Add(bonus);
                var castBar = Bar("slot-cast-fill", "cast-fill");
                castBar.AddToClassList("cast-bar");
                slot.Add(castBar);
                slot.RegisterCallback<ClickEvent>(evt =>
                {
                    if (slot.userData is Action<int> action)
                    {
                        action(slot.parent.IndexOf(slot));
                    }
                });
                return slot;
            }

            private static void Sync(VisualElement container, int count, Func<VisualElement> create)
            {
                while (container.childCount > count)
                {
                    container.RemoveAt(container.childCount - 1);
                }

                while (container.childCount < count)
                {
                    container.Add(create());
                }
            }

            private static void SetVisible(VisualElement element, bool visible)
            {
                element.EnableInClassList(HiddenClass, !visible);
            }

            private static T Find<T>(VisualElement root, string name)
                where T : VisualElement
            {
                return root.Q<T>(name)
                    ?? throw new InvalidOperationException($"The run screen has no '{name}' element.");
            }
        }
    }
}
