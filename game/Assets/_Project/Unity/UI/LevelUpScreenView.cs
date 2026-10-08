using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Runs;
using Game.Unity.Upgrades;
using UnityEngine.UIElements;

namespace Game.Unity.UI
{
    /// <summary>
    /// The level-up choice screen (<c>Screens/LevelUpScreen.uxml</c>, #77, ADR 0012). A static binder like the
    /// other screens: it draws a <see cref="LevelUpScreenModel"/> and forwards the player's clicks to it; the
    /// choice is applied to the run by <see cref="Show"/> through Core. It holds no rule.
    /// </summary>
    public static class LevelUpScreenView
    {
        public const string TitleElement = "levelup-title";
        public const string PackagesElement = "packages";
        public const string DestinationPanelElement = "destination-panel";
        public const string DestinationTextElement = "destination-text";
        public const string ReserveButtonElement = "reserve-button";
        public const string LineCardsElement = "line-cards";
        public const string ConfirmButtonElement = "confirm-button";

        public const string PackageClass = "package";
        public const string PackageSelectedClass = "package--selected";
        public const string LineCardClass = "line-card";
        public const string LineCardSelectedClass = "line-card--selected";
        public const string ReserveSelectedClass = "reserve-button--selected";

        /// <summary>Shows the level-up screen for the run's pending level-up and applies the choice with Core.</summary>
        /// <param name="host">The screen host.</param>
        /// <param name="layout">The screen's layout.</param>
        /// <param name="run">The run, with a pending level-up.</param>
        /// <param name="passivePool">The passive upgrade assets the offer draws from (#75).</param>
        /// <param name="onChosen">Called after the package was taken.</param>
        public static VisualElement Show(
            ScreenHost host,
            VisualTreeAsset layout,
            Run run,
            IEnumerable<PassiveUpgradeAsset> passivePool,
            Action onChosen = null)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            if (passivePool == null)
            {
                throw new ArgumentNullException(nameof(passivePool));
            }

            var pool = passivePool.Select(asset => asset.ToUpgrade()).ToList();
            var offer = run.GetLevelUpOffer(pool);
            var model = new LevelUpScreenModel(offer, run.Line, run.LineCapacity);
            var screen = host.Show(layout);
            Bind(screen, model, ApplyChoice(run, onChosen));
            return screen;
        }

        /// <summary>The callback that gives the player's choice to Core, then tells the caller.</summary>
        public static Action<LevelUpChoice> ApplyChoice(Run run, Action onChosen = null)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            return choice =>
            {
                run.TakeLevelUpPackage(choice.PackageIndex, choice.ReplacedLinePosition);
                onChosen?.Invoke();
            };
        }

        /// <summary>Fills a cloned level-up screen tree; redraws when the model changes.</summary>
        /// <exception cref="InvalidOperationException">The tree lacks one of the named elements.</exception>
        public static void Bind(VisualElement root, LevelUpScreenModel model, Action<LevelUpChoice> onChosen)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            Find<Label>(root, TitleElement);
            var packages = Find<VisualElement>(root, PackagesElement);
            var panel = Find<VisualElement>(root, DestinationPanelElement);
            var destination = Find<Label>(root, DestinationTextElement);
            var reserve = Find<Button>(root, ReserveButtonElement);
            var lineCards = Find<VisualElement>(root, LineCardsElement);
            var confirm = Find<Button>(root, ConfirmButtonElement);

            reserve.RegisterCallback<ClickEvent>(_ => model.SelectReserve());
            confirm.RegisterCallback<ClickEvent>(_ =>
            {
                if (model.CanConfirm)
                {
                    onChosen?.Invoke(model.Confirm());
                }
            });

            void Redraw()
            {
                packages.Clear();
                foreach (var package in model.Packages)
                {
                    var index = package.Index;
                    var card = new VisualElement { name = "package-" + index };
                    card.AddToClassList(PackageClass);
                    card.EnableInClassList(PackageSelectedClass, model.SelectedPackage == index);
                    card.Add(Text("package-card-name", package.CardName));
                    card.Add(Text("package-card-summary", package.CardSummary));
                    card.Add(Text("package-passive", package.PassiveText));
                    card.RegisterCallback<ClickEvent>(_ => model.SelectPackage(index));
                    packages.Add(card);
                }

                panel.EnableInClassList("hidden", !model.LineIsFull || !model.SelectedPackage.HasValue);
                destination.text = model.DestinationText;
                reserve.EnableInClassList(ReserveSelectedClass, !model.SelectedReplacement.HasValue);
                lineCards.Clear();
                foreach (var line in model.LineCards)
                {
                    var position = line.Position;
                    var entry = new Label($"Slot {position + 1}: {line.CardName}");
                    entry.AddToClassList(LineCardClass);
                    entry.EnableInClassList(LineCardSelectedClass, model.SelectedReplacement == position);
                    entry.RegisterCallback<ClickEvent>(_ => model.SelectReplacement(position));
                    lineCards.Add(entry);
                }

                confirm.SetEnabled(model.CanConfirm);
            }

            model.Changed += Redraw;
            Redraw();
        }

        private static Label Text(string cssClass, string text)
        {
            var label = new Label(text);
            label.AddToClassList(cssClass);
            return label;
        }

        private static T Find<T>(VisualElement root, string name)
            where T : VisualElement
        {
            return root.Q<T>(name)
                ?? throw new InvalidOperationException($"The level-up screen has no '{name}' element.");
        }
    }
}
