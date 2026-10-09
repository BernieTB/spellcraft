using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Combat.Recap;
using Game.Core.Effects;
using Game.Unity.UI.Cards;
using UnityEngine.UIElements;

namespace Game.Unity.UI
{
    /// <summary>
    /// The recap screen (<c>Screens/RecapScreen.uxml</c>, ADR 0014): what each card of each side produced, the
    /// neighbour bonuses used and wasted, and for a lost fight where the chain broke. The view only displays a
    /// <see cref="FightRecap"/> built by Core; it holds no rule. Like the other screens it is a static binder, so
    /// it can fill a cloned tree without entering Play mode.
    /// </summary>
    /// <remarks>
    /// Texts are placeholders in English (localisation is not decided). Card slots are shown from 1, the way a
    /// player counts, while Core positions start at zero. Colours and sizes live in <c>Styles/Recap.uss</c>.
    /// </remarks>
    public static class RecapScreenView
    {
        /// <summary>Name of the label that shows the outcome ("Victory", "Defeat", "Out of time").</summary>
        public const string OutcomeTitleElement = "outcome-title";

        /// <summary>Name of the label that explains the outcome and the length of the fight.</summary>
        public const string OutcomeSubtitleElement = "outcome-subtitle";

        /// <summary>Name of the panel shown for a fight that was not won.</summary>
        public const string DefeatPanelElement = "defeat-panel";

        /// <summary>Name of the label that shows the turning point.</summary>
        public const string TurningPointElement = "turning-point";

        /// <summary>Name of the label that shows the analysed loop.</summary>
        public const string AnalysedLoopElement = "analysed-loop";

        /// <summary>Name of the container of the causes, the main one first.</summary>
        public const string CausesElement = "causes";

        /// <summary>Name of the container of the hero's section.</summary>
        public const string HeroElement = "hero";

        /// <summary>Name of the container of the enemies' sections.</summary>
        public const string EnemiesElement = "enemies";

        /// <summary>Class that hides an element.</summary>
        public const string HiddenClass = "hidden";

        /// <summary>Class of the outcome label for a victory.</summary>
        public const string VictoryClass = "outcome--victory";

        /// <summary>Class of the outcome label for a defeat.</summary>
        public const string DefeatClass = "outcome--defeat";

        /// <summary>Class of the outcome label for a fight that ran out of time.</summary>
        public const string TimeLimitClass = "outcome--time-limit";

        /// <summary>Class of one combatant's section.</summary>
        public const string CombatantClass = "combatant";

        /// <summary>Class of a table row, header included.</summary>
        public const string CardRowClass = "card-row";

        /// <summary>Class of the header row of a table.</summary>
        public const string HeaderRowClass = "card-row--header";

        /// <summary>Class of the row of the card blamed for the defeat.</summary>
        public const string CulpritRowClass = "card-row--culprit";

        /// <summary>Class of a cause row.</summary>
        public const string CauseClass = "cause";

        /// <summary>Class added to the main cause row.</summary>
        public const string MainCauseClass = "cause--main";

        /// <summary>Class of a wasted bonus line.</summary>
        public const string WastedClass = "wasted";

        /// <summary>Class added to the wasted bonus lines when wasted bonuses are the main cause.</summary>
        public const string WastedCulpritClass = "wasted--culprit";

        /// <summary>Class of a combatant's heading ("Hero", "Enemy 1").</summary>
        public const string CombatantHeadingClass = "combatant-heading";

        /// <summary>Class of the line that totals a combatant's neighbour bonuses.</summary>
        public const string CombatantSummaryClass = "combatant-summary";

        /// <summary>Class of every table cell.</summary>
        public const string CellClass = "cell";

        /// <summary>Class of the slot column.</summary>
        public const string SlotCellClass = "cell--slot";

        /// <summary>Class of the card column.</summary>
        public const string CardCellClass = "cell--card";

        /// <summary>Class of the numeric columns (casts, damage, healing, shield).</summary>
        public const string NumberCellClass = "cell--number";

        /// <summary>Class of the bonus columns, which share the remaining width equally.</summary>
        public const string BonusCellClass = "cell--bonus";

        private static readonly string[] OutcomeClasses = { VictoryClass, DefeatClass, TimeLimitClass };

        /// <summary>Name of the button that leaves the recap (hooked up by the game flow, #88).</summary>
        public const string ContinueButtonElement = "continue-button";

        /// <summary>The table columns, in order: title and class of every cell of the column.</summary>
        private static readonly (string Title, string Class)[] Columns =
        {
            ("Slot", SlotCellClass),
            ("Card", CardCellClass),
            ("Casts", NumberCellClass),
            ("Damage", NumberCellClass),
            ("Healing", NumberCellClass),
            ("Shield", NumberCellClass),
            ("Bonus used", BonusCellClass),
            ("Bonus wasted", BonusCellClass),
        };

        /// <summary>
        /// Fills a cloned recap screen tree. Binding the same tree again replaces what was shown.
        /// </summary>
        /// <param name="summaries">
        /// Tells what a card of the table does (shown under its id, details on hover), or null for a card it does not
        /// know (#123). Null shows ids only. See <see cref="RecapCardSummaries"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="root"/> or <paramref name="recap"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The tree lacks one of the named elements.</exception>
        public static void Bind(VisualElement root, FightRecap recap, Func<CardRecap, CardSummary> summaries = null)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (recap == null)
            {
                throw new ArgumentNullException(nameof(recap));
            }

            var title = Find<Label>(root, OutcomeTitleElement);
            var subtitle = Find<Label>(root, OutcomeSubtitleElement);
            var defeatPanel = Find<VisualElement>(root, DefeatPanelElement);
            var turningPoint = Find<Label>(root, TurningPointElement);
            var analysedLoop = Find<Label>(root, AnalysedLoopElement);
            var causes = Find<VisualElement>(root, CausesElement);
            var hero = Find<VisualElement>(root, HeroElement);
            var enemies = Find<VisualElement>(root, EnemiesElement);

            ShowOutcome(title, subtitle, recap);
            ShowDefeat(defeatPanel, turningPoint, analysedLoop, causes, recap);

            hero.Clear();
            enemies.Clear();
            foreach (var combatant in recap.Combatants)
            {
                var section = BuildCombatant(combatant, recap.Defeat, summaries);
                (combatant.IsHero ? hero : enemies).Add(section);
            }
        }

        private static T Find<T>(VisualElement root, string name)
            where T : VisualElement
        {
            return root.Q<T>(name)
                ?? throw new InvalidOperationException($"The recap screen has no '{name}' element.");
        }

        private static void ShowOutcome(Label title, Label subtitle, FightRecap recap)
        {
            foreach (var outcomeClass in OutcomeClasses)
            {
                title.RemoveFromClassList(outcomeClass);
            }

            switch (recap.Outcome)
            {
                case FightRecapOutcome.Victory:
                    title.text = "Victory";
                    title.AddToClassList(VictoryClass);
                    subtitle.text = $"Every enemy fell after {recap.Ticks} ticks.";
                    break;
                case FightRecapOutcome.Defeat:
                    title.text = "Defeat";
                    title.AddToClassList(DefeatClass);
                    subtitle.text = $"The hero fell after {recap.Ticks} ticks.";
                    break;
                case FightRecapOutcome.TimeLimit:
                    title.text = "Out of time";
                    title.AddToClassList(TimeLimitClass);
                    subtitle.text =
                        $"The fight reached its time limit after {recap.Ticks} ticks. It counts as a defeat.";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(recap), recap.Outcome, "Unknown fight outcome.");
            }
        }

        private static void ShowDefeat(
            VisualElement panel,
            Label turningPoint,
            Label analysedLoop,
            VisualElement causes,
            FightRecap recap)
        {
            causes.Clear();

            // A victory has nothing to explain.
            var explained = recap.Outcome != FightRecapOutcome.Victory;
            panel.EnableInClassList(HiddenClass, !explained);
            if (!explained)
            {
                return;
            }

            var defeat = recap.Defeat;
            if (defeat == null)
            {
                // Nothing to point at. Only a fight that ran out of time can be explained this way (the hero was
                // not behind when time ran out); a lost fight always comes with an analysis, so say so neutrally.
                turningPoint.text = recap.Outcome == FightRecapOutcome.TimeLimit
                    ? "The hero was not behind when time ran out, so there is no turning point."
                    : "No analysis of this fight is available.";
                analysedLoop.AddToClassList(HiddenClass);
                return;
            }

            turningPoint.text = defeat.TurningPointTick == 0
                ? "The hero was behind from the first tick."
                : $"Turning point: tick {defeat.TurningPointTick}. The hero stayed behind from then on.";
            analysedLoop.RemoveFromClassList(HiddenClass);
            analysedLoop.text = $"Analysed loop: ticks {defeat.WindowStartTick} to {defeat.WindowEndTick}.";

            for (var i = 0; i < defeat.Causes.Count; i++)
            {
                var isMain = defeat.Causes[i] == defeat.MainCause;
                var row = new Label((isMain ? "Main cause: " : "Also: ") + DescribeCause(defeat.Causes[i], defeat));
                row.AddToClassList(CauseClass);
                if (isMain)
                {
                    row.AddToClassList(MainCauseClass);
                }

                causes.Add(row);
            }
        }

        private static string DescribeCause(DefeatCause cause, DefeatAnalysis defeat)
        {
            switch (cause)
            {
                case DefeatCause.WastedBonuses:
                    return $"neighbour bonuses were wasted ({FormatBonus(defeat.WastedBonus)}): "
                        + "the receiving cards had no effect of that kind.";
                case DefeatCause.ShieldBroken:
                    return $"the shield was broken on tick {defeat.ShieldBrokenTick} by {defeat.ShieldBreakerCardId} "
                        + $"(enemy {defeat.ShieldBreakerIndex}, slot {defeat.ShieldBreakerPosition + 1}).";
                case DefeatCause.WeakestCard:
                    return $"{defeat.WeakestCardId} (slot {defeat.WeakestCardPosition + 1}) was the weakest card: "
                        + $"it produced {defeat.WeakestCardOutput} in the analysed loop.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(cause), cause, "Unknown defeat cause.");
            }
        }

        private static VisualElement BuildCombatant(
            CombatantRecap combatant, DefeatAnalysis defeat, Func<CardRecap, CardSummary> summaries)
        {
            var section = new VisualElement();
            section.AddToClassList(CombatantClass);

            var heading = new Label(combatant.IsHero ? "Hero" : $"Enemy {combatant.Index}");
            heading.AddToClassList(CombatantHeadingClass);
            section.Add(heading);

            section.Add(BuildRow(Columns.Select(column => column.Title).ToArray(), HeaderRowClass));
            foreach (var card in combatant.Cards)
            {
                var summary = summaries?.Invoke(card);
                var cells = new[]
                {
                    (card.Position + 1).ToString(),
                    CardCellText(card, summary),
                    card.Casts.ToString(),
                    card.Damage.ToString(),
                    card.Healing.ToString(),
                    card.ShieldGained.ToString(),
                    FormatBonus(card.BonusUsed),
                    FormatBonus(card.BonusWasted),
                };
                var row = BuildRow(cells, null);
                if (summary != null)
                {
                    row.tooltip = summary.TooltipText;
                }

                if (IsCulprit(card, defeat))
                {
                    row.AddToClassList(CulpritRowClass);
                }

                section.Add(row);
            }

            var summary = new Label(
                $"Neighbour bonuses: {FormatBonus(combatant.BonusReceived)} received, "
                + $"{FormatBonus(combatant.BonusUsed)} used, {FormatBonus(combatant.BonusWasted)} wasted.");
            summary.AddToClassList(CombatantSummaryClass);
            section.Add(summary);

            var wastedIsMainCause = combatant.IsHero && defeat != null && defeat.MainCause == DefeatCause.WastedBonuses;
            foreach (var waste in combatant.WastedBonuses)
            {
                var line = new Label(
                    $"Slot {waste.Position + 1} ({waste.CardId}): +{waste.Amount} {KindName(waste.Kind)} wasted, "
                    + DescribeReason(waste));
                line.AddToClassList(WastedClass);
                if (wastedIsMainCause)
                {
                    line.AddToClassList(WastedCulpritClass);
                }

                section.Add(line);
            }

            return section;
        }

        /// <summary>
        /// True for the cards the defeat analysis blames first: the hero's cards that wasted a neighbour bonus, the
        /// hero's weakest card, or the enemy card that broke the hero's shield, depending on the main cause.
        /// </summary>
        private static bool IsCulprit(CardRecap card, DefeatAnalysis defeat)
        {
            if (defeat == null)
            {
                return false;
            }

            switch (defeat.MainCause)
            {
                case DefeatCause.WastedBonuses:
                    return card.CombatantIndex == Core.Combat.Fight.HeroIndex && !card.BonusWasted.IsNone;
                case DefeatCause.WeakestCard:
                    return card.CombatantIndex == Core.Combat.Fight.HeroIndex
                        && card.Position == defeat.WeakestCardPosition
                        && card.CardId == defeat.WeakestCardId;
                case DefeatCause.ShieldBroken:
                    return card.CombatantIndex == defeat.ShieldBreakerIndex
                        && card.Position == defeat.ShieldBreakerPosition
                        && card.CardId == defeat.ShieldBreakerCardId;
                default:
                    return false;
            }
        }

        /// <summary>The card column: the id (with the stage mark), then the compact summary when it is known.</summary>
        private static string CardCellText(CardRecap card, CardSummary summary)
        {
            if (summary == null)
            {
                return card.CardId;
            }

            var mark = summary.HasStageMark ? " " + summary.StageMarkText : string.Empty;
            return card.CardId + mark + "\n" + string.Join(", ", summary.CompactLines);
        }

        private static VisualElement BuildRow(IReadOnlyList<string> cells, string rowClass)
        {
            var row = new VisualElement();
            row.AddToClassList(CardRowClass);
            if (rowClass != null)
            {
                row.AddToClassList(rowClass);
            }

            for (var i = 0; i < cells.Count; i++)
            {
                var cell = new Label(cells[i]);
                cell.AddToClassList(CellClass);
                cell.AddToClassList(Columns[i].Class);
                row.Add(cell);
            }

            return row;
        }

        private static string DescribeReason(BonusWaste waste)
        {
            switch (waste.Reason)
            {
                case WastedBonusReason.NoEffectOfKind:
                    return $"the card has no {KindName(waste.Kind)} effect.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(waste), waste.Reason, "Unknown wasted bonus reason.");
            }
        }

        private static string KindName(BonusKind kind)
        {
            switch (kind)
            {
                case BonusKind.Damage:
                    return "damage";
                case BonusKind.Heal:
                    return "heal";
                case BonusKind.Shield:
                    return "shield";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown bonus kind.");
            }
        }

        /// <summary>Formats a bonus as "+3 damage, +1 shield", or "none" when it is empty.</summary>
        private static string FormatBonus(EffectBonus bonus)
        {
            if (bonus.IsNone)
            {
                return "none";
            }

            var parts = new List<string>();
            if (bonus.Damage > 0)
            {
                parts.Add($"+{bonus.Damage} damage");
            }

            if (bonus.Heal > 0)
            {
                parts.Add($"+{bonus.Heal} heal");
            }

            if (bonus.Shield > 0)
            {
                parts.Add($"+{bonus.Shield} shield");
            }

            return string.Join(", ", parts);
        }
    }
}
