using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Runs;
using Game.Core.Upgrades;
using Game.Unity.EditorTools.UI;
using Game.Unity.UI;
using Game.Unity.Upgrades;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Unity.Tests.UI
{
    /// <summary>
    /// Checks the level-up screen: the view-model (plain C#), the layout and view, and the wiring to a real run.
    /// </summary>
    public class LevelUpScreenTests
    {
        // Placeholder ids and test values: not game content or balance numbers.
        private static readonly CardDefinition Strike = new CardDefinition("TestStrike", 1, new IEffect[] { new DealDamageEffect(10) });
        private static readonly CardDefinition PoolA = new CardDefinition("TestPoolA", 1, new IEffect[] { new DealDamageEffect(1) });
        private static readonly CardDefinition PoolB = new CardDefinition("TestPoolB", 1, new IEffect[] { new DealDamageEffect(2) });
        private static readonly CardDefinition EnemyHit = new CardDefinition("TestEnemyHit", 1, new IEffect[] { new DealDamageEffect(3) });

        private static readonly EncounterDefinition Rich = new EncounterDefinition(
            "TestRich", new[] { new EnemyDefinition("TestEnemy1", 15, 0, new[] { EnemyHit }, 30) });

        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                UnityEngine.Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        private static LevelUpScreenModel FreeSlotModel() => ScreenPreviewLevelUps.FreeSlot();

        private static LevelUpScreenModel FullLineModel() => ScreenPreviewLevelUps.FullLine();

        private static VisualTreeAsset Layout()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BootstrapSceneBuilder.LevelUpScreenPath);
            Assert.IsNotNull(layout, $"Missing {BootstrapSceneBuilder.LevelUpScreenPath}.");
            return layout;
        }

        private PassiveUpgradeAsset PassiveAsset(string id, PassiveUpgradeKind kind, int amount)
        {
            var asset = ScriptableObject.CreateInstance<PassiveUpgradeAsset>();
            _created.Add(asset);
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_kind").intValue = (int)kind;
            serialized.FindProperty("_amount").intValue = amount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static Run RunWithPendingLevelUp(int capacity)
        {
            var heroClass = new ClassDefinition(
                "TestClass", 20, 0, capacity, new[] { Strike }, new[] { PoolA, PoolB });
            var professor = new EncounterDefinition(
                "TestProfessor", new[] { new EnemyDefinition("TestProfessor1", 15, 0, new[] { EnemyHit }) });
            var biome = new BiomeDefinition("TestBiome", new[] { Rich }, 2, professor, professor.Enemies[0]);
            var run = new Run(heroClass, biome, new RunRules(50, new LevelCurve(new[] { 10, 20 }, 5)), 1);
            run.Play(RunStep.RegularFight);
            Assert.IsTrue(run.HasPendingChoice);
            return run;
        }

        // --- view-model ---

        [Test]
        public void Model_ShowsCardAndPassiveOfEveryPackage()
        {
            var model = FreeSlotModel();

            Assert.AreEqual(3, model.Packages.Count);
            Assert.AreEqual("demo_strike", model.Packages[0].CardName);
            StringAssert.Contains("Deal 6 damage", model.Packages[0].CardSummary);
            Assert.AreEqual("+5 max health", model.Packages[0].PassiveText);
            StringAssert.Contains("Gain 3 shield", model.Packages[1].CardSummary);
            StringAssert.Contains("next card +2 damage", model.Packages[1].CardSummary);
            Assert.AreEqual("+1 to every damage effect you cast", model.Packages[1].PassiveText);
            Assert.AreEqual("+3 starting shield each fight", model.Packages[2].PassiveText);
        }

        [Test]
        public void Model_FreeSlot_AddsAtEndAndRefusesReserveAndReplacement()
        {
            var model = FreeSlotModel();

            Assert.IsFalse(model.LineIsFull);
            StringAssert.Contains("end of your spell line", model.DestinationText);
            Assert.Throws<InvalidOperationException>(() => model.SelectReserve());
            Assert.Throws<InvalidOperationException>(() => model.SelectReplacement(0));
        }

        [Test]
        public void Model_FullLine_DefaultsToReserveThenReplacesTheChosenLineCard()
        {
            var model = FullLineModel();
            model.SelectPackage(1);

            Assert.IsTrue(model.LineIsFull);
            Assert.IsNull(model.Confirm().ReplacedLinePosition);
            StringAssert.Contains("reserve", model.DestinationText);

            model.SelectReplacement(2);
            var choice = model.Confirm();

            Assert.AreEqual(1, choice.PackageIndex);
            Assert.AreEqual(2, choice.ReplacedLinePosition);
            StringAssert.Contains("slot 3", model.DestinationText);

            model.SelectReserve();
            Assert.IsNull(model.Confirm().ReplacedLinePosition);
        }

        [Test]
        public void Model_ConfirmWithoutPackage_Throws()
        {
            var model = FullLineModel();

            Assert.IsFalse(model.CanConfirm);
            Assert.Throws<InvalidOperationException>(() => model.Confirm());
        }

        [Test]
        public void Model_OutOfRangeSelections_Throw()
        {
            var model = FullLineModel();

            Assert.Throws<ArgumentOutOfRangeException>(() => model.SelectPackage(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => model.SelectReplacement(4));
        }

        [Test]
        public void Model_SelectionRaisesChanged()
        {
            var model = FullLineModel();
            var count = 0;
            model.Changed += () => count++;

            model.SelectPackage(0);
            model.SelectReplacement(1);
            model.SelectReserve();

            Assert.AreEqual(3, count);
        }

        // --- layout and view ---

        [Test]
        public void Layout_HasTheNamedElements()
        {
            var root = Layout().CloneTree();

            Assert.IsNotNull(root.Q<Label>(LevelUpScreenView.TitleElement));
            Assert.IsNotNull(root.Q(LevelUpScreenView.PackagesElement));
            Assert.IsNotNull(root.Q(LevelUpScreenView.DestinationPanelElement));
            Assert.IsNotNull(root.Q<Label>(LevelUpScreenView.DestinationTextElement));
            Assert.IsNotNull(root.Q<Button>(LevelUpScreenView.ReserveButtonElement));
            Assert.IsNotNull(root.Q(LevelUpScreenView.LineCardsElement));
            Assert.IsNotNull(root.Q<Button>(LevelUpScreenView.ConfirmButtonElement));
        }

        [Test]
        public void Bind_DrawsOnePanelPerPackageAndNoRuleInTheView()
        {
            var root = Layout().CloneTree();

            LevelUpScreenView.Bind(root, FullLineModel(), _ => { });

            var packages = root.Q(LevelUpScreenView.PackagesElement).Children().ToList();
            Assert.AreEqual(3, packages.Count);
            Assert.AreEqual("demo_strike", packages[0].Q<Label>(className: "package-card-name").text);
            Assert.AreEqual("+5 max health", packages[0].Q<Label>(className: "package-passive").text);
            Assert.IsFalse(root.Q<Button>(LevelUpScreenView.ConfirmButtonElement).enabledSelf);
            Assert.IsTrue(root.Q(LevelUpScreenView.DestinationPanelElement).ClassListContains("hidden"));
        }

        [Test]
        public void Bind_SelectionShowsPanelOnFullLineAndEnablesConfirm()
        {
            var root = Layout().CloneTree();
            var model = FullLineModel();
            LevelUpScreenView.Bind(root, model, _ => { });

            model.SelectPackage(2);

            var packages = root.Q(LevelUpScreenView.PackagesElement).Children().ToList();
            Assert.IsTrue(packages[2].ClassListContains(LevelUpScreenView.PackageSelectedClass));
            Assert.IsFalse(root.Q(LevelUpScreenView.DestinationPanelElement).ClassListContains("hidden"));
            Assert.AreEqual(4, root.Q(LevelUpScreenView.LineCardsElement).childCount);
            Assert.IsTrue(root.Q<Button>(LevelUpScreenView.ConfirmButtonElement).enabledSelf);
        }

        [Test]
        public void Bind_FreeSlot_NeverShowsTheDestinationPanel()
        {
            var root = Layout().CloneTree();
            var model = FreeSlotModel();
            LevelUpScreenView.Bind(root, model, _ => { });

            model.SelectPackage(0);

            Assert.IsTrue(root.Q(LevelUpScreenView.DestinationPanelElement).ClassListContains("hidden"));
        }

        // --- wiring to a run ---

        [Test]
        public void Show_ThenModelChoice_TakesThePackageInTheRun()
        {
            var run = RunWithPendingLevelUp(capacity: 1);
            var pool = new[]
            {
                PassiveAsset("test_health", PassiveUpgradeKind.MaxHealth, 5),
                PassiveAsset("test_shield", PassiveUpgradeKind.StartingShield, 2),
            };
            var container = new VisualElement();
            var host = new ScreenHost(container);
            var pending = run.PendingLevelUps;
            var done = false;

            var screen = LevelUpScreenView.Show(host, Layout(), run, pool, () => done = true);

            var offer = run.GetLevelUpOffer(pool.Select(asset => asset.ToUpgrade()).ToList());
            var packages = screen.Q(LevelUpScreenView.PackagesElement).Children().ToList();
            Assert.AreEqual(offer.Packages.Count, packages.Count);
            Assert.AreEqual(offer.Packages[0].Card.Id, packages[0].Q<Label>(className: "package-card-name").text);

            // The line (capacity 1) is full: the player replaces slot 1. Events need a panel, so the clicks are
            // replaced by the model calls they make.
            var model = new LevelUpScreenModel(offer, run.Line, run.LineCapacity);
            Assert.AreEqual(1, screen.Q(LevelUpScreenView.LineCardsElement).childCount);
            model.SelectPackage(0);
            model.SelectReplacement(0);
            LevelUpScreenView.ApplyChoice(run, () => done = true)(model.Confirm());

            Assert.IsTrue(done);
            Assert.AreEqual(pending - 1, run.PendingLevelUps);
            Assert.AreEqual(offer.Packages[0].Card.Id, run.Line[0].Definition.Id);
            Assert.AreEqual(1, run.Reserve.Count);
            Assert.AreEqual("TestStrike", run.Reserve[0].Definition.Id);
        }

        // --- preview ---

        [Test]
        public void Previews_ListTheLevelUpScreens()
        {
            var names = ScreenPreviews.All.Select(preview => preview.Name).ToList();

            CollectionAssert.Contains(names, "Level-up: free slot");
            CollectionAssert.Contains(names, "Level-up: full line");
        }
    }
}
