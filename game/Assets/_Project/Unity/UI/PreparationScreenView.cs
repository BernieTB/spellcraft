using System;
using System.Collections.Generic;
using Game.Unity.UI.Cards;
using UnityEngine.UIElements;

namespace Game.Unity.UI
{
    /// <summary>
    /// The preparation screen (<c>Screens/PreparationScreen.uxml</c>, #84, ADR 0014). It only displays a
    /// <see cref="PreparationViewModel"/> and forwards clicks to it; every rule lives in Core. Like the other
    /// screens it is a static binder, so it can fill a cloned tree without entering Play mode.
    /// </summary>
    /// <remarks>
    /// Texts are placeholders in English. Card slots are shown from 1. Colours and sizes live in
    /// <c>Styles/Preparation.uss</c>. Controls are simple buttons: select a card, then act on it.
    /// </remarks>
    public static class PreparationScreenView
    {
        /// <summary>Path of the screen layout.</summary>
        public const string LayoutPath = "Assets/_Project/Unity/UI/Screens/PreparationScreen.uxml";

        public const string TitleElement = "title";
        public const string BossInfoElement = "boss-info";
        public const string LineElement = "line";
        public const string ReserveElement = "reserve";
        public const string MoveLeftButton = "move-left";
        public const string MoveRightButton = "move-right";
        public const string SwapButton = "swap";
        public const string ToReserveButton = "to-reserve";
        public const string ToLineButton = "to-line";
        public const string StartButton = "start";

        /// <summary>Class of a card button.</summary>
        public const string CardClass = "prep-card";

        /// <summary>Class added to a selected card.</summary>
        public const string SelectedClass = "prep-card--selected";

        /// <summary>Class of a label that shows a piece of information the player does not have.</summary>
        public const string UnknownClass = "unknown";

        /// <summary>Text shown for hidden information.</summary>
        public const string UnknownText = "Unknown";

        /// <summary>
        /// Fills a cloned preparation screen tree and keeps it in step with the view-model. Returns an action that
        /// stops following the view-model (call it when the screen is hidden).
        /// </summary>
        /// <param name="onStart">Called after the fight has begun, so the caller can show it.</param>
        /// <exception cref="ArgumentNullException"><paramref name="root"/> or <paramref name="model"/> is null.</exception>
        /// <exception cref="InvalidOperationException">An expected element is missing.</exception>
        public static Action Bind(VisualElement root, PreparationViewModel model, Action onStart = null)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var title = Find<Label>(root, TitleElement);
            var bossInfo = Find<VisualElement>(root, BossInfoElement);
            var line = Find<VisualElement>(root, LineElement);
            var reserve = Find<VisualElement>(root, ReserveElement);
            var moveLeft = Find<Button>(root, MoveLeftButton);
            var moveRight = Find<Button>(root, MoveRightButton);
            var swap = Find<Button>(root, SwapButton);
            var toReserve = Find<Button>(root, ToReserveButton);
            var toLine = Find<Button>(root, ToLineButton);
            var start = Find<Button>(root, StartButton);

            moveLeft.clicked += model.MoveLeft;
            moveRight.clicked += model.MoveRight;
            swap.clicked += model.Swap;
            toReserve.clicked += model.MoveToReserve;
            toLine.clicked += model.MoveToLine;
            start.clicked += () =>
            {
                if (model.Start() != null)
                {
                    onStart?.Invoke();
                }
            };

            void Refresh()
            {
                title.text = model.IsProfessor ? "Prepare for the professor" : "Prepare for the mini-boss";
                ShowBoss(bossInfo, model);
                ShowCards(line, model.Line, model.SelectedLine, model.SelectLine);
                ShowCards(reserve, model.Reserve, model.SelectedReserve, model.SelectReserve);
                moveLeft.SetEnabled(model.CanMoveLeft);
                moveRight.SetEnabled(model.CanMoveRight);
                swap.SetEnabled(model.CanSwap);
                toReserve.SetEnabled(model.CanMoveToReserve);
                toLine.SetEnabled(model.CanMoveToLine);
                start.SetEnabled(model.CanStart);
            }

            model.Changed += Refresh;
            Refresh();
            return () => model.Changed -= Refresh;
        }

        private static T Find<T>(VisualElement root, string name)
            where T : VisualElement
        {
            return root.Q<T>(name)
                ?? throw new InvalidOperationException($"The preparation screen has no '{name}' element.");
        }

        private static void ShowBoss(VisualElement container, PreparationViewModel model)
        {
            container.Clear();
            foreach (var enemy in model.Enemies)
            {
                var section = new VisualElement();
                section.AddToClassList("prep-enemy");
                section.Add(new Label(enemy.Id) { name = "enemy-name" });
                section.Add(InfoLabel("Health", enemy.MaxHealth));
                section.Add(InfoLabel("Shield", enemy.Shield));
                for (var i = 0; i < enemy.CardIds.Count; i++)
                {
                    var id = enemy.CardIds[i];
                    var summary = enemy.CardSummaries != null && i < enemy.CardSummaries.Count ? enemy.CardSummaries[i] : null;
                    var label = new Label(summary == null
                        ? $"{i + 1}. {id ?? UnknownText}"
                        : $"{i + 1}. {id}: {string.Join(", ", summary.CompactLines)}");
                    label.AddToClassList("prep-enemy-card");
                    if (id == null)
                    {
                        label.AddToClassList(UnknownClass);
                    }

                    if (summary != null)
                    {
                        label.tooltip = summary.TooltipText;
                    }

                    section.Add(label);
                }

                container.Add(section);
            }
        }

        private static Label InfoLabel(string name, int? value)
        {
            var label = new Label($"{name}: {(value.HasValue ? value.Value.ToString() : UnknownText)}");
            label.AddToClassList("prep-enemy-stat");
            if (!value.HasValue)
            {
                label.AddToClassList(UnknownClass);
            }

            return label;
        }

        private static void ShowCards(
            VisualElement container,
            IReadOnlyList<PreparationCard> cards,
            int? selected,
            Action<int> select)
        {
            container.Clear();
            for (var i = 0; i < cards.Count; i++)
            {
                var index = i;
                var card = cards[i];
                var button = new Button(() => select(index));
                button.AddToClassList(CardClass);
                CardFace.Fill(button, $"{i + 1}. {card.Id}", card.Summary);
                if (selected == i)
                {
                    button.AddToClassList(SelectedClass);
                }

                container.Add(button);
            }
        }
    }
}
