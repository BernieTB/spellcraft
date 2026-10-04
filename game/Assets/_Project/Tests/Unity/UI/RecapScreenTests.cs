using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Combat.Recap;
using Game.Core.Effects;
using Game.Unity.EditorTools.UI;
using Game.Unity.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Game.Unity.Tests.UI
{
    /// <summary>
    /// Checks the recap screen layout and its view without entering Play mode. The recaps are built by hand: the
    /// builder itself is tested in <c>Game.Core.Tests</c>.
    /// </summary>
    public class RecapScreenTests
    {
        private const int Hero = 0;

        private static VisualElement CloneRecapScreen()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BootstrapSceneBuilder.RecapScreenPath);
            Assert.IsNotNull(layout, $"Missing {BootstrapSceneBuilder.RecapScreenPath}.");
            return layout.CloneTree();
        }

        private static VisualElement Bound(FightRecap recap)
        {
            var root = CloneRecapScreen();
            RecapScreenView.Bind(root, recap);
            return root;
        }

        private static CardRecap Card(
            int owner,
            int position,
            string id,
            int casts = 0,
            int damage = 0,
            int healing = 0,
            int shield = 0,
            EffectBonus received = default,
            EffectBonus wasted = default)
        {
            return new CardRecap(owner, position, id, casts, damage, healing, shield, received, wasted);
        }

        private static EffectBonus Sum(IEnumerable<EffectBonus> bonuses)
        {
            var list = bonuses.ToList();
            return new EffectBonus(list.Sum(b => b.Damage), list.Sum(b => b.Heal), list.Sum(b => b.Shield));
        }

        private static CombatantRecap Combatant(int index, CardRecap[] cards, params BonusWaste[] wastes)
        {
            return new CombatantRecap(
                index,
                cards,
                wastes,
                Sum(cards.Select(card => card.BonusReceived)),
                Sum(cards.Select(card => card.BonusWasted)));
        }

        private static FightRecap Recap(
            FightRecapOutcome outcome,
            DefeatAnalysis defeat,
            params CombatantRecap[] combatants)
        {
            return new FightRecap(outcome, 240, combatants, defeat);
        }

        /// <summary>
        /// A defeat analysis whose main cause is <paramref name="main"/>; the weakest card, always present in the
        /// analysis rules, follows it as the other cause.
        /// </summary>
        private static DefeatAnalysis Defeat(
            DefeatCause main,
            int turningPoint = 60,
            EffectBonus wastedBonus = default,
            int shieldBreakerIndex = 1,
            int shieldBreakerPosition = 0,
            string shieldBreakerCardId = "enemy_card",
            int weakestPosition = 1,
            string weakestId = "hero_b")
        {
            var causes = main == DefeatCause.WeakestCard
                ? new List<DefeatCause> { DefeatCause.WeakestCard }
                : new List<DefeatCause> { main, DefeatCause.WeakestCard };

            return new DefeatAnalysis(
                turningPoint,
                20,
                turningPoint,
                main,
                causes,
                wastedBonus,
                main == DefeatCause.ShieldBroken ? 45 : -1,
                shieldBreakerIndex,
                shieldBreakerPosition,
                shieldBreakerCardId,
                weakestPosition,
                weakestId,
                3);
        }

        private static FightRecap SimpleRecap(FightRecapOutcome outcome, DefeatAnalysis defeat)
        {
            return Recap(
                outcome,
                defeat,
                Combatant(
                    Hero,
                    new[]
                    {
                        Card(Hero, 0, "hero_a", casts: 4, damage: 12),
                        Card(Hero, 1, "hero_b", casts: 4, healing: 3),
                    }),
                Combatant(1, new[] { Card(1, 0, "enemy_card", casts: 5, damage: 20) }));
        }

        private static List<string> Texts(VisualElement element)
        {
            return element.Query<Label>().ToList().Select(label => label.text).ToList();
        }

        private static List<VisualElement> Rows(VisualElement scope)
        {
            return scope.Query(className: RecapScreenView.CardRowClass).ToList();
        }

        private static List<Label> Causes(VisualElement root)
        {
            return root.Q(RecapScreenView.CausesElement).Children().OfType<Label>().ToList();
        }

        [Test]
        public void Layout_Clone_HasEveryNamedElement()
        {
            var root = CloneRecapScreen();

            foreach (var name in new[]
            {
                RecapScreenView.OutcomeTitleElement,
                RecapScreenView.OutcomeSubtitleElement,
                RecapScreenView.DefeatPanelElement,
                RecapScreenView.TurningPointElement,
                RecapScreenView.AnalysedLoopElement,
                RecapScreenView.CausesElement,
                RecapScreenView.HeroElement,
                RecapScreenView.EnemiesElement,
            })
            {
                Assert.IsNotNull(root.Q(name), $"The layout has no '{name}' element.");
            }
        }

        [Test]
        public void Layout_Clone_UsesTheCommonAndRecapStyleSheets()
        {
            var common = AssetDatabase.LoadAssetAtPath<StyleSheet>(BootstrapSceneBuilder.CommonStylePath);
            var recap = AssetDatabase.LoadAssetAtPath<StyleSheet>(BootstrapSceneBuilder.RecapStylePath);
            Assert.IsNotNull(common, $"Missing {BootstrapSceneBuilder.CommonStylePath}.");
            Assert.IsNotNull(recap, $"Missing {BootstrapSceneBuilder.RecapStylePath}.");

            var root = CloneRecapScreen();
            var attached = Enumerable.Range(0, root.styleSheets.count).Select(i => root.styleSheets[i]).ToList();

            Assert.That(attached, Has.Member(common));
            Assert.That(attached, Has.Member(recap));
        }

        [Test]
        public void Bind_Victory_ShowsTheOutcomeAndHidesTheDefeatPanel()
        {
            var root = Bound(SimpleRecap(FightRecapOutcome.Victory, null));

            var title = root.Q<Label>(RecapScreenView.OutcomeTitleElement);
            Assert.AreEqual("Victory", title.text);
            Assert.IsTrue(title.ClassListContains(RecapScreenView.VictoryClass));
            StringAssert.Contains("240 ticks", root.Q<Label>(RecapScreenView.OutcomeSubtitleElement).text);
            Assert.IsTrue(root.Q(RecapScreenView.DefeatPanelElement).ClassListContains(RecapScreenView.HiddenClass));
            Assert.IsEmpty(Causes(root));
        }

        [Test]
        public void Bind_Defeat_ShowsTheTurningPointAndTheMainCause()
        {
            var defeat = Defeat(DefeatCause.WeakestCard, turningPoint: 60, weakestPosition: 1, weakestId: "hero_b");
            var root = Bound(SimpleRecap(FightRecapOutcome.Defeat, defeat));

            var title = root.Q<Label>(RecapScreenView.OutcomeTitleElement);
            Assert.AreEqual("Defeat", title.text);
            Assert.IsTrue(title.ClassListContains(RecapScreenView.DefeatClass));
            Assert.IsFalse(root.Q(RecapScreenView.DefeatPanelElement).ClassListContains(RecapScreenView.HiddenClass));
            StringAssert.Contains("tick 60", root.Q<Label>(RecapScreenView.TurningPointElement).text);
            StringAssert.Contains("ticks 20 to 60", root.Q<Label>(RecapScreenView.AnalysedLoopElement).text);

            var causes = Causes(root);
            Assert.AreEqual(1, causes.Count);
            Assert.IsTrue(causes[0].ClassListContains(RecapScreenView.MainCauseClass));
            StringAssert.StartsWith("Main cause:", causes[0].text);
            StringAssert.Contains("hero_b (slot 2) was the weakest card", causes[0].text);
        }

        [Test]
        public void Bind_DefeatFromTheFirstTick_SaysTheHeroWasBehindFromTheStart()
        {
            var root = Bound(SimpleRecap(FightRecapOutcome.Defeat, Defeat(DefeatCause.WeakestCard, turningPoint: 0)));

            StringAssert.Contains("behind from the first tick", root.Q<Label>(RecapScreenView.TurningPointElement).text);
        }

        [Test]
        public void Bind_TimeLimitWithoutAnalysis_SaysSoAndHasNoTurningPoint()
        {
            var root = Bound(SimpleRecap(FightRecapOutcome.TimeLimit, null));

            var title = root.Q<Label>(RecapScreenView.OutcomeTitleElement);
            Assert.AreEqual("Out of time", title.text);
            Assert.IsTrue(title.ClassListContains(RecapScreenView.TimeLimitClass));
            var subtitle = root.Q<Label>(RecapScreenView.OutcomeSubtitleElement).text;
            StringAssert.Contains("time limit", subtitle);
            StringAssert.Contains("counts as a defeat", subtitle);
            Assert.IsFalse(root.Q(RecapScreenView.DefeatPanelElement).ClassListContains(RecapScreenView.HiddenClass));
            StringAssert.Contains("no turning point", root.Q<Label>(RecapScreenView.TurningPointElement).text);
            Assert.IsTrue(root.Q(RecapScreenView.AnalysedLoopElement).ClassListContains(RecapScreenView.HiddenClass));
            Assert.IsEmpty(Causes(root));
        }

        [Test]
        public void Bind_TimeLimitWithAnalysis_ShowsTheTurningPointToo()
        {
            var root = Bound(SimpleRecap(FightRecapOutcome.TimeLimit, Defeat(DefeatCause.WeakestCard, turningPoint: 90)));

            Assert.AreEqual("Out of time", root.Q<Label>(RecapScreenView.OutcomeTitleElement).text);
            StringAssert.Contains("tick 90", root.Q<Label>(RecapScreenView.TurningPointElement).text);
            Assert.IsFalse(root.Q(RecapScreenView.AnalysedLoopElement).ClassListContains(RecapScreenView.HiddenClass));
        }

        [Test]
        public void Bind_Defeat_ListsEveryCauseWithTheMainOneFirst()
        {
            var defeat = Defeat(DefeatCause.WastedBonuses, wastedBonus: new EffectBonus(3, 0, 2));
            var root = Bound(SimpleRecap(FightRecapOutcome.Defeat, defeat));

            var causes = Causes(root);
            Assert.AreEqual(2, causes.Count);
            Assert.IsTrue(causes[0].ClassListContains(RecapScreenView.MainCauseClass));
            StringAssert.Contains("+3 damage, +2 shield", causes[0].text);
            Assert.IsFalse(causes[1].ClassListContains(RecapScreenView.MainCauseClass));
            StringAssert.StartsWith("Also:", causes[1].text);
        }

        [Test]
        public void Bind_ShieldBroken_NamesTheEnemyCardAndMarksItsRow()
        {
            var defeat = Defeat(
                DefeatCause.ShieldBroken,
                shieldBreakerIndex: 1,
                shieldBreakerPosition: 0,
                shieldBreakerCardId: "enemy_card");
            var root = Bound(SimpleRecap(FightRecapOutcome.Defeat, defeat));

            StringAssert.Contains(
                "shield was broken on tick 45 by enemy_card (enemy 1, slot 1)",
                Causes(root)[0].text);

            var culprits = root.Query(className: RecapScreenView.CulpritRowClass).ToList();
            Assert.AreEqual(1, culprits.Count);
            Assert.That(Texts(culprits[0]), Has.Member("enemy_card"));
            Assert.IsTrue(root.Q(RecapScreenView.EnemiesElement).Children().Any(section => section.Contains(culprits[0])));
        }

        [Test]
        public void Bind_WeakestCard_MarksTheHeroRowOfThatCardOnly()
        {
            var defeat = Defeat(DefeatCause.WeakestCard, weakestPosition: 1, weakestId: "hero_b");
            var root = Bound(SimpleRecap(FightRecapOutcome.Defeat, defeat));

            var culprits = root.Query(className: RecapScreenView.CulpritRowClass).ToList();
            Assert.AreEqual(1, culprits.Count);
            Assert.That(Texts(culprits[0]), Has.Member("hero_b"));
            Assert.IsTrue(root.Q(RecapScreenView.HeroElement).Children().Any(section => section.Contains(culprits[0])));
        }

        [Test]
        public void Bind_Victory_MarksNoCulprit()
        {
            var root = Bound(SimpleRecap(FightRecapOutcome.Victory, null));

            Assert.IsEmpty(root.Query(className: RecapScreenView.CulpritRowClass).ToList());
        }

        [Test]
        public void Bind_Cards_ShowTheirOutputForBothSides()
        {
            var root = Bound(SimpleRecap(FightRecapOutcome.Victory, null));

            var heroRows = Rows(root.Q(RecapScreenView.HeroElement));
            Assert.AreEqual(3, heroRows.Count, "A header row and one row per card.");
            Assert.IsTrue(heroRows[0].ClassListContains(RecapScreenView.HeaderRowClass));
            Assert.That(Texts(heroRows[1]).Take(6), Is.EqualTo(new[] { "1", "hero_a", "4", "12", "0", "0" }));
            Assert.That(Texts(heroRows[2]).Take(6), Is.EqualTo(new[] { "2", "hero_b", "4", "0", "3", "0" }));

            var enemyRows = Rows(root.Q(RecapScreenView.EnemiesElement));
            Assert.AreEqual(2, enemyRows.Count);
            Assert.That(Texts(enemyRows[1]).Take(6), Is.EqualTo(new[] { "1", "enemy_card", "5", "20", "0", "0" }));
        }

        [Test]
        public void Bind_WastedBonus_ShowsTheAmountAndTheReason()
        {
            var received = new EffectBonus(3, 0, 0);
            var wasted = new BonusWaste(Hero, 1, "hero_b", BonusKind.Damage, 3, WastedBonusReason.NoEffectOfKind);
            var recap = Recap(
                FightRecapOutcome.Defeat,
                Defeat(DefeatCause.WastedBonuses, wastedBonus: received),
                Combatant(
                    Hero,
                    new[]
                    {
                        Card(Hero, 0, "hero_a", casts: 4, damage: 12),
                        Card(Hero, 1, "hero_b", casts: 4, healing: 3, received: received, wasted: received),
                    },
                    wasted),
                Combatant(1, new[] { Card(1, 0, "enemy_card", casts: 5, damage: 20) }));

            var root = Bound(recap);

            var lines = root.Query<Label>(className: RecapScreenView.WastedClass).ToList();
            Assert.AreEqual(1, lines.Count);
            StringAssert.Contains("Slot 2 (hero_b): +3 damage wasted", lines[0].text);
            StringAssert.Contains("no damage effect", lines[0].text);
            Assert.IsTrue(
                lines[0].ClassListContains(RecapScreenView.WastedCulpritClass),
                "Wasted bonuses are the main cause.");

            var cardRow = Rows(root.Q(RecapScreenView.HeroElement))[2];
            Assert.IsTrue(cardRow.ClassListContains(RecapScreenView.CulpritRowClass), "The card that wasted the bonus.");
            var cells = Texts(cardRow);
            Assert.AreEqual("none", cells[6], "Bonus used by the card.");
            Assert.AreEqual("+3 damage", cells[7], "Bonus wasted by the card.");
        }

        [Test]
        public void Bind_WastedBonusOutsideTheMainCause_IsNotHighlighted()
        {
            var wasted = new BonusWaste(Hero, 0, "hero_a", BonusKind.Shield, 2, WastedBonusReason.NoEffectOfKind);
            var recap = Recap(
                FightRecapOutcome.Victory,
                null,
                Combatant(Hero, new[] { Card(Hero, 0, "hero_a", casts: 1, damage: 5) }, wasted),
                Combatant(1, new[] { Card(1, 0, "enemy_card", casts: 1) }));

            var root = Bound(recap);

            var line = root.Query<Label>(className: RecapScreenView.WastedClass).First();
            Assert.IsNotNull(line);
            Assert.IsFalse(line.ClassListContains(RecapScreenView.WastedCulpritClass));
        }

        [Test]
        public void Bind_SeveralEnemies_EachGetsItsOwnSection()
        {
            var recap = Recap(
                FightRecapOutcome.Victory,
                null,
                Combatant(Hero, new[] { Card(Hero, 0, "hero_a", casts: 1, damage: 5) }),
                Combatant(1, new[] { Card(1, 0, "enemy_a", casts: 1) }),
                Combatant(2, new[] { Card(2, 0, "enemy_b", casts: 1) }));

            var root = Bound(recap);

            var sections = root.Q(RecapScreenView.EnemiesElement).Query(className: RecapScreenView.CombatantClass).ToList();
            Assert.AreEqual(2, sections.Count);
            Assert.That(Texts(sections[0]), Has.Member("Enemy 1"));
            Assert.That(Texts(sections[1]), Has.Member("Enemy 2"));
            Assert.That(Texts(root.Q(RecapScreenView.HeroElement)), Has.Member("Hero"));
        }

        [Test]
        public void Bind_Twice_ReplacesWhatWasShown()
        {
            var root = CloneRecapScreen();
            RecapScreenView.Bind(root, SimpleRecap(FightRecapOutcome.Defeat, Defeat(DefeatCause.WeakestCard)));

            RecapScreenView.Bind(root, SimpleRecap(FightRecapOutcome.Victory, null));

            var title = root.Q<Label>(RecapScreenView.OutcomeTitleElement);
            Assert.AreEqual("Victory", title.text);
            Assert.IsFalse(title.ClassListContains(RecapScreenView.DefeatClass));
            Assert.AreEqual(1, root.Q(RecapScreenView.HeroElement).Query(className: RecapScreenView.CombatantClass).ToList().Count);
            Assert.AreEqual(1, root.Q(RecapScreenView.EnemiesElement).Query(className: RecapScreenView.CombatantClass).ToList().Count);
            Assert.IsEmpty(Causes(root));
            Assert.IsTrue(root.Q(RecapScreenView.DefeatPanelElement).ClassListContains(RecapScreenView.HiddenClass));
            Assert.IsEmpty(root.Query(className: RecapScreenView.CulpritRowClass).ToList());
        }

        [Test]
        public void Bind_TreeWithoutNamedElements_Throws()
        {
            Assert.Throws<InvalidOperationException>(
                () => RecapScreenView.Bind(new VisualElement(), SimpleRecap(FightRecapOutcome.Victory, null)));
        }

        [Test]
        public void Bind_LostFightWithoutAnalysis_DoesNotClaimTimeRanOut()
        {
            var root = Bound(SimpleRecap(FightRecapOutcome.Defeat, null));

            var text = root.Q<Label>(RecapScreenView.TurningPointElement).text;
            StringAssert.DoesNotContain("time ran out", text);
            StringAssert.Contains("No analysis", text);
            Assert.IsEmpty(Causes(root));
        }

        [Test]
        public void Bind_WastedBonusesAreTheMainCause_MarksTheHeroCardsThatWastedThem()
        {
            var received = new EffectBonus(3, 0, 0);
            var recap = Recap(
                FightRecapOutcome.Defeat,
                Defeat(DefeatCause.WastedBonuses, wastedBonus: received),
                Combatant(
                    Hero,
                    new[]
                    {
                        Card(Hero, 0, "hero_a", casts: 4, damage: 12),
                        Card(Hero, 1, "hero_b", casts: 4, healing: 3, received: received, wasted: received),
                    }),
                Combatant(1, new[] { Card(1, 0, "enemy_card", casts: 5, damage: 20, received: received, wasted: received) }));

            var root = Bound(recap);

            var culprits = root.Query(className: RecapScreenView.CulpritRowClass).ToList();
            Assert.AreEqual(1, culprits.Count, "Only the hero's card that wasted a bonus, not the enemy's.");
            Assert.That(Texts(culprits[0]), Has.Member("hero_b"));
        }

        [Test]
        public void Bind_MainCause_IsTheOneNamedByTheAnalysisNotTheFirstOfTheList()
        {
            var defeat = new DefeatAnalysis(
                60,
                20,
                60,
                DefeatCause.WastedBonuses,
                new[] { DefeatCause.WeakestCard, DefeatCause.WastedBonuses },
                new EffectBonus(2, 0, 0),
                -1,
                -1,
                -1,
                null,
                1,
                "hero_b",
                3);

            var root = Bound(SimpleRecap(FightRecapOutcome.Defeat, defeat));

            var causes = Causes(root);
            Assert.AreEqual(2, causes.Count);
            Assert.IsFalse(causes[0].ClassListContains(RecapScreenView.MainCauseClass));
            Assert.IsTrue(causes[1].ClassListContains(RecapScreenView.MainCauseClass));
            StringAssert.StartsWith("Main cause:", causes[1].text);
            StringAssert.StartsWith("Also:", causes[0].text);
        }

        [Test]
        public void Bind_Cells_UseOneClassPerColumnInTheHeaderAndInTheRows()
        {
            var root = Bound(SimpleRecap(FightRecapOutcome.Victory, null));

            var rows = Rows(root.Q(RecapScreenView.HeroElement));
            var headerClasses = rows[0].Children().Select(cell => cell.GetClasses().ToList()).ToList();
            var rowClasses = rows[1].Children().Select(cell => cell.GetClasses().ToList()).ToList();

            Assert.AreEqual(8, headerClasses.Count);
            Assert.AreEqual(headerClasses.Count, rowClasses.Count);
            for (var i = 0; i < headerClasses.Count; i++)
            {
                CollectionAssert.AreEquivalent(headerClasses[i], rowClasses[i], $"Column {i}.");
                Assert.That(headerClasses[i], Has.Member(RecapScreenView.CellClass));
            }

            Assert.That(headerClasses[0], Has.Member(RecapScreenView.SlotCellClass));
            Assert.That(headerClasses[1], Has.Member(RecapScreenView.CardCellClass));
            Assert.That(headerClasses[2], Has.Member(RecapScreenView.NumberCellClass));
            Assert.That(headerClasses[5], Has.Member(RecapScreenView.NumberCellClass));
            Assert.That(headerClasses[6], Has.Member(RecapScreenView.BonusCellClass));
            Assert.That(headerClasses[7], Has.Member(RecapScreenView.BonusCellClass));
        }

        [Test]
        public void Bind_CombatantWithoutCards_ShowsOnlyItsHeaderAndSummary()
        {
            var recap = Recap(
                FightRecapOutcome.Victory,
                null,
                Combatant(Hero, new CardRecap[0]),
                Combatant(1, new CardRecap[0]));

            var root = Bound(recap);

            Assert.AreEqual(1, Rows(root.Q(RecapScreenView.HeroElement)).Count, "The header row only.");
            Assert.AreEqual(1, Rows(root.Q(RecapScreenView.EnemiesElement)).Count);
            Assert.IsEmpty(root.Query<Label>(className: RecapScreenView.WastedClass).ToList());
            Assert.That(Texts(root.Q(RecapScreenView.HeroElement)), Has.Member("Hero"));
        }

        [Test]
        public void Bind_RecapWithoutCombatants_ShowsTheOutcomeAndNoSections()
        {
            var root = Bound(Recap(FightRecapOutcome.Victory, null));

            Assert.AreEqual("Victory", root.Q<Label>(RecapScreenView.OutcomeTitleElement).text);
            Assert.IsEmpty(root.Q(RecapScreenView.HeroElement).Children());
            Assert.IsEmpty(root.Q(RecapScreenView.EnemiesElement).Children());
        }

        [Test]
        public void Bind_DefeatWithoutAnyCause_ShowsTheTurningPointOnly()
        {
            var defeat = new DefeatAnalysis(
                60,
                20,
                60,
                DefeatCause.WeakestCard,
                new DefeatCause[0],
                default,
                -1,
                -1,
                -1,
                null,
                0,
                "hero_a",
                0);

            var root = Bound(SimpleRecap(FightRecapOutcome.Defeat, defeat));

            StringAssert.Contains("tick 60", root.Q<Label>(RecapScreenView.TurningPointElement).text);
            Assert.IsEmpty(Causes(root));
        }

        [Test]
        public void Bind_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(
                () => RecapScreenView.Bind(null, SimpleRecap(FightRecapOutcome.Victory, null)));
            Assert.Throws<ArgumentNullException>(() => RecapScreenView.Bind(CloneRecapScreen(), null));
        }
    }
}
