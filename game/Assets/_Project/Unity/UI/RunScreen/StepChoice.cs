using Game.Core.Runs;

namespace Game.Unity.UI.RunScreen
{
    /// <summary>One entry of the next-step choice: a step, whether it can be picked now, and why not.</summary>
    public sealed class StepChoice
    {
        public StepChoice(RunStep step, string label, bool isEnabled, string disabledReason)
        {
            Step = step;
            Label = label;
            IsEnabled = isEnabled;
            DisabledReason = disabledReason;
        }

        /// <summary>The step.</summary>
        public RunStep Step { get; }

        /// <summary>Text of the choice (a placeholder, English).</summary>
        public string Label { get; }

        /// <summary>True when the player can pick it now.</summary>
        public bool IsEnabled { get; }

        /// <summary>Why it cannot be picked, or null when it can.</summary>
        public string DisabledReason { get; }
    }
}
