using System.Linq;
using Game.Core.Cards;
using Game.Core.Combat.Recap;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Runs;
using Game.Core.Upgrades;
using Game.Unity.EditorTools.UI;
using Game.Unity.UI;
using Game.Unity.UI.Cards;
using Game.Unity.UI.RunScreen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Game.Unity.Tests.UI
{
    /// <summary>
    /// The card summary shared by every screen (#123): the view-models say what a card does at its current stage, and
    /// the run, preparation and recap screens show it on every card, with the full details as a tooltip.
    /// </summary>
    public class CardSummaryTests
    {
        private static CardDefinition Evolving()
        {
            return new CardDefinition(
                "demo_grow",
                3,
                new IEffect[] { new DealDamageEffect(2) },
                new[] { new NeighbourModifier(BonusKind.Shield, NeighbourDirection.Previous, 1) },
                new[]
                {
                    new CardEvolution(
                        2,
                        new IEffect[] { new DealDamageEffect(5), new GainShieldEffect(2) },
                        new[] { new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, 4) }),
                });
        }

        // --- CardSummary ---

        [Test]
        public void CardSummary_BaseStage_ListsCastTimeEffectsAndNeighbourBonus()
        {
            var summary = CardSummary.From(Evolving());

            Assert.AreEqual(3, summary.CastTime);
            CollectionAssert.AreEqual(new[] { "Deal 2 damage" }, summary.EffectLines);
            CollectionAssert.AreEqual(new[] { "Prev card +1 shield" }, summary.NeighbourShortLines);
            CollectionAssert.AreEqual(new[] { "Cast 3", "Deal 2 damage", "Prev card +1 shield" }, summary.CompactLines);
            Assert.IsFalse(summary.HasStageMark);
            Assert.AreEqual(string.Empty, summary.StageMarkText);
        }

        [Test]
        public void CardSummary_EvolvedStage_ShowsTheAmountsOfThatStageAndTheMark()
        {
            var summary = CardSummary.From(Evolving().AtStage(1));

            CollectionAssert.AreEqual(new[] { "Deal 5 damage", "Gain 2 shield" }, summary.EffectLines);
            CollectionAssert.AreEqual(new[] { "Next card +4 damage" }, summary.NeighbourShortLines);
            Assert.AreEqual("*", summary.StageMarkText);
            StringAssert.Contains("Evolved (stage 1)", summary.TooltipText);
        }

        [Test]
        public void CardSummary_Tooltip_HasEverythingTheSlotShowsAndMore()
        {
            var tooltip = CardSummary.From(Evolving()).TooltipText;

            StringAssert.Contains("demo_grow", tooltip);
            StringAssert.Contains("Base form", tooltip);
            StringAssert.Contains("Cast time: 3", tooltip);
            StringAssert.Contains("Deal 2 damage", tooltip);
            StringAssert.Contains("Gives the previous card +1 shield", tooltip);
        }

        [Test]
        public void CardSummary_CardWithNothing_SaysSo()
        {
            var summary = CardSummary.From(new CardDefinition("demo_idle", 2, new IEffect[0]));

            Assert.IsTrue(summary.HasNothing);
            CollectionAssert.AreEqual(new[] { "Cast 2", "No direct effect" }, summary.CompactLines);
        }

        [Test]
        public void CardSummary_DetailedText_IsTheLevelUpCardText()
        {
            Assert.AreEqual(
                "Cast time: 3\nDeal 2 damage\nGives the previous card +1 shield",
                CardSummary.From(Evolving()).DetailedText);
        }

        // --- PassiveSummary ---

        [Test]
        public void PassiveSummary_OwnedUpgrades_AreStackedWithTheirTotalEffect()
        {
            var set = PassiveUpgradeSet.Empty
                .With(new PassiveUpgrade("passive_health", PassiveUpgradeKind.MaxHealth, 5))
                .With(new PassiveUpgrade("passive_power", PassiveUpgradeKind.EffectAmount, BonusKind.Damage, 2))
                .With(new PassiveUpgrade("passive_health", PassiveUpgradeKind.MaxHealth, 5));

            var lines = PassiveSummary.DescribeOwned(set);

            Assert.AreEqual(2, lines.Count);
            Assert.AreEqual("passive_health x2: +10 max health", lines[0].Text);
            Assert.AreEqual("passive_power: +2 to every damage effect you cast", lines[1].Text);
            StringAssert.Contains("Taken 2 time(s)", lines[0].TooltipText);
        }

        [Test]
        public void PassiveSummary_NoUpgrade_IsEmpty()
        {
            Assert.IsEmpty(PassiveSummary.DescribeOwned(PassiveUpgradeSet.Empty));
        }

        // --- Run screen ---

        [Test]
        public void RunViewModel_EverySlotOfTheLineAndTheReserve_HasASummary()
        {
            var model = RunScreenViewModel.From(new RunScreenController(ScreenPreviewRuns.NewCrowdedRun()));

            Assert.AreEqual(6, model.Line.Count);
            Assert.IsNotEmpty(model.Reserve);
            Assert.That(model.Line.Concat(model.Reserve).Select(slot => slot.Summary), Has.All.Not.Null);
            Assert.AreEqual("demo_grow", model.Line[0].Summary.CardId);
        }

        [Test]
        public void RunViewModel_ListsThePassivesWithTheirEffect()
        {
            var model = RunScreenViewModel.From(new RunScreenController(ScreenPreviewRuns.NewCrowdedRun()));

            Assert.AreEqual(2, model.Passives.Count);
            StringAssert.Contains("+10 max health", model.Passives[0].Text);
        }

        [Test]
        public void RunView_ShowsTheSummaryOnEverySlotAndTheDetailsAsTooltip_AndThePassives()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BootstrapSceneBuilder.RunScreenPath);
            var root = layout.CloneTree();
            using (RunScreenView.Bind(root, new RunScreenController(ScreenPreviewRuns.NewCrowdedRun())))
            {
                var slots = root.Query<VisualElement>(className: RunScreenView.SlotClass).ToList();

                Assert.AreEqual(9, slots.Count);
                foreach (var slot in slots)
                {
                    StringAssert.StartsWith("Cast ", slot.Q<Label>("slot-detail").text);
                    StringAssert.Contains("Cast time:", slot.tooltip);
                }

                StringAssert.Contains("Deal 2 damage", slots[0].Q<Label>("slot-detail").text);
                var passives = root.Query<Label>(className: RunScreenView.PassiveClass).ToList();
                Assert.AreEqual(2, passives.Count);
                Assert.IsFalse(root.Q(RunScreenView.PassivesRowElement).ClassListContains(RunScreenView.HiddenClass));
            }
        }

        [Test]
        public void RunView_WithoutPassives_HidesThePassivesRow()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BootstrapSceneBuilder.RunScreenPath);
            var root = layout.CloneTree();
            using (RunScreenView.Bind(root, new RunScreenController(ScreenPreviewRuns.NewRun())))
            {
                Assert.IsTrue(root.Q(RunScreenView.PassivesRowElement).ClassListContains(RunScreenView.HiddenClass));
            }
        }

        // --- Preparation ---

        [Test]
        public void PreparationViewModel_CardsAndKnownEnemyCards_CarryASummary()
        {
            var model = new PreparationViewModel(ScreenPreviewPreparations.MiniBoss());

            Assert.That(model.Line.Concat(model.Reserve).Select(card => card.Summary), Has.All.Not.Null);
            var enemy = model.Enemies.Single();
            Assert.That(enemy.CardSummaries, Has.All.Not.Null);
            Assert.AreEqual(enemy.CardIds.Count, enemy.CardSummaries.Count);
        }

        [Test]
        public void PreparationViewModel_UnknownProfessorCards_HaveNoSummary()
        {
            var model = new PreparationViewModel(ScreenPreviewPreparations.ProfessorUnknown());

            Assert.That(model.Enemies.Single().CardSummaries, Has.All.Null);
        }

        [Test]
        public void PreparationView_EveryCardShowsItsSummary_AndTheDetailsAsTooltip()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PreparationScreenView.LayoutPath);
            var root = layout.CloneTree();
            var model = new PreparationViewModel(ScreenPreviewPreparations.MiniBoss());
            PreparationScreenView.Bind(root, model);

            var cards = root.Query<Button>(className: PreparationScreenView.CardClass).ToList();

            Assert.AreEqual(model.Line.Count + model.Reserve.Count, cards.Count);
            foreach (var card in cards)
            {
                StringAssert.StartsWith("Cast ", card.Q<Label>("card-detail").text);
                StringAssert.Contains("Cast time:", card.tooltip);
            }
        }

        // --- Recap ---

        [Test]
        public void RecapView_WithSummaries_ShowsWhatEachCardDoesUnderItsId()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BootstrapSceneBuilder.RecapScreenPath);
            var root = layout.CloneTree();

            RecapScreenView.Bind(root, ScreenPreviewFights.Victory(), ScreenPreviewFights.Summaries);

            var cells = root.Query<Label>(className: RecapScreenView.CardCellClass).ToList();
            Assert.That(cells.Select(cell => cell.text), Has.Some.Contains("demo_strike\nCast 3, Deal 4 damage"));
            var row = root.Query<VisualElement>(className: RecapScreenView.CardRowClass).ToList().First(r => !string.IsNullOrEmpty(r.tooltip));
            StringAssert.Contains("Cast time:", row.tooltip);
        }

        [Test]
        public void RecapView_WithoutSummaries_ShowsOnlyIds()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BootstrapSceneBuilder.RecapScreenPath);
            var root = layout.CloneTree();

            RecapScreenView.Bind(root, ScreenPreviewFights.Victory());

            Assert.That(
                root.Query<Label>(className: RecapScreenView.CardCellClass).ToList().Select(cell => cell.text),
                Has.None.Contains("Cast "));
        }

        [Test]
        public void RecapCardSummaries_HeroAndEnemyCards_ComeFromTheReport_AndAMovedCardGetsNone()
        {
            var run = ScreenPreviewRuns.NewRun();
            var enemy = new EnemyDefinition(
                "demo_slime", 10, 0, new[] { new CardDefinition("demo_hit", 5, new IEffect[] { new DealDamageEffect(2) }) }, 5);
            var report = new RunFightReport(
                RunStep.RegularFight,
                new EncounterDefinition("demo_slime_encounter", new[] { enemy }),
                run.Line,
                ScreenPreviewFights.VictoryLog());
            var lookup = RecapCardSummaries.For(report);
            var empty = default(EffectBonus);

            var hero = lookup(new CardRecap(0, 1, "demo_strike", 1, 0, 0, 0, empty, empty));
            var foe = lookup(new CardRecap(1, 0, "demo_hit", 1, 0, 0, 0, empty, empty));
            var moved = lookup(new CardRecap(0, 0, "demo_strike", 1, 0, 0, 0, empty, empty));

            CollectionAssert.AreEqual(new[] { "Deal 4 damage" }, hero.EffectLines);
            CollectionAssert.AreEqual(new[] { "Deal 2 damage" }, foe.EffectLines);
            Assert.IsNull(moved);
        }
    }
}
