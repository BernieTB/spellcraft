using System;
using Game.Core.Runs;
using UnityEngine.UIElements;

namespace Game.Unity.UI
{
    /// <summary>
    /// The end of a run (<c>Screens/EndScreen.uxml</c>, #88): the outcome, a one-line summary and the button back to
    /// the title screen. It only displays what the caller gives it.
    /// </summary>
    public static class EndScreenView
    {
        /// <summary>Name of the label that shows the outcome.</summary>
        public const string TitleElement = "end-title";

        /// <summary>Name of the label that shows the summary.</summary>
        public const string SummaryElement = "end-summary";

        /// <summary>Name of the button back to the title screen.</summary>
        public const string BackButtonElement = "back-button";

        /// <summary>Class of the title after a victory.</summary>
        public const string VictoryClass = "title--victory";

        /// <summary>Class of the title after a defeat.</summary>
        public const string DefeatClass = "title--defeat";

        /// <summary>Fills a cloned end screen tree.</summary>
        /// <param name="root">The cloned screen.</param>
        /// <param name="outcome">How the run ended.</param>
        /// <param name="summary">One line about the run.</param>
        /// <param name="onBack">Called when the player goes back to the title screen. May be null.</param>
        /// <exception cref="InvalidOperationException">The tree lacks one of the named elements.</exception>
        public static void Bind(VisualElement root, RunOutcome outcome, string summary, Action onBack)
        {
            var title = root.Q<Label>(TitleElement)
                ?? throw new InvalidOperationException($"The end screen has no '{TitleElement}' label.");
            var summaryLabel = root.Q<Label>(SummaryElement)
                ?? throw new InvalidOperationException($"The end screen has no '{SummaryElement}' label.");
            var back = root.Q<Button>(BackButtonElement)
                ?? throw new InvalidOperationException($"The end screen has no '{BackButtonElement}' button.");

            title.text = outcome == RunOutcome.Victory ? "Victory" : outcome == RunOutcome.Defeat ? "Defeat" : "Run in progress";
            title.EnableInClassList(VictoryClass, outcome == RunOutcome.Victory);
            title.EnableInClassList(DefeatClass, outcome == RunOutcome.Defeat);
            summaryLabel.text = summary ?? string.Empty;
            if (onBack != null)
            {
                back.clicked += onBack;
            }
        }
    }
}
