using System.Linq;
using Game.Core.Runs;
using Game.Unity.EditorTools.UI;
using Game.Unity.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Game.Unity.Tests.UI
{
    /// <summary>
    /// Checks the preparation view-model (selection and actions, rules stay in Core) and the screen layout
    /// without entering Play mode. Demo runs come from <see cref="ScreenPreviewPreparations"/>: the hero line is
    /// demo_strike, demo_guard, demo_mend (capacity 3) and the reserve is demo_bolt, demo_unique.
    /// </summary>
    public class PreparationScreenTests
    {
        private static string[] Ids(System.Collections.Generic.IEnumerable<PreparationCard> cards)
        {
            return cards.Select(card => card.Id).ToArray();
        }

        private static PreparationViewModel Model(BossPreparation preparation = null)
        {
            return new PreparationViewModel(preparation ?? ScreenPreviewPreparations.MiniBoss());
        }

        private static VisualElement Bound(PreparationViewModel model)
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PreparationScreenView.LayoutPath);
            Assert.IsNotNull(layout, $"Missing {PreparationScreenView.LayoutPath}.");
            var root = layout.CloneTree();
            PreparationScreenView.Bind(root, model);
            return root;
        }

        // --- Contents ---

        [Test]
        public void Lists_LineAndReserveFromTheRun()
        {
            var model = Model();

            Assert.AreEqual(3, model.LineCapacity);
            CollectionAssert.AreEqual(new[] { "demo_strike", "demo_guard", "demo_mend" }, Ids(model.Line));
            CollectionAssert.AreEquivalent(new[] { "demo_bolt", "demo_unique" }, Ids(model.Reserve));
            Assert.AreEqual(3, model.Line.Count);
            Assert.AreEqual(2, model.Reserve.Count);
        }

        [Test]
        public void Enemies_MiniBoss_EverythingKnown()
        {
            var model = Model();

            var enemy = model.Enemies.Single();
            Assert.IsFalse(model.IsProfessor);
            Assert.IsTrue(enemy.MaxHealth.HasValue);
            Assert.IsTrue(enemy.Shield.HasValue);
            Assert.That(enemy.CardIds, Has.All.Not.Null);
        }

        [Test]
        public void Enemies_ProfessorWithoutRevelations_EverythingUnknownButTheLineLength()
        {
            var model = Model(ScreenPreviewPreparations.ProfessorUnknown());

            var professor = model.Enemies.Single();
            Assert.IsTrue(model.IsProfessor);
            Assert.IsNull(professor.MaxHealth);
            Assert.IsNull(professor.Shield);
            Assert.AreEqual(3, professor.CardIds.Count);
            Assert.That(professor.CardIds, Has.All.Null);
        }

        [Test]
        public void Enemies_ProfessorPartlyRevealed_ShowsOnlyWhatWasRevealed()
        {
            var model = Model(ScreenPreviewPreparations.ProfessorPartlyKnown());

            var professor = model.Enemies.Single();
            Assert.IsNotNull(professor.MaxHealth);
            Assert.IsNull(professor.Shield);
            Assert.IsNotNull(professor.CardIds[0]);
            Assert.IsNull(professor.CardIds[1]);
            Assert.IsNull(professor.CardIds[2]);
        }

        // --- Actions ---

        [Test]
        public void MoveLeftRight_MovesTheSelectedCardAndKeepsItSelected()
        {
            var model = Model();
            model.SelectLine(0);

            Assert.IsFalse(model.CanMoveLeft);
            model.MoveRight();

            Assert.AreEqual(1, model.SelectedLine);
            Assert.AreEqual("demo_guard", model.Line[0].Id);
            Assert.AreEqual("demo_strike", model.Line[1].Id);
            model.MoveLeft();
            Assert.AreEqual(0, model.SelectedLine);
            Assert.AreEqual("demo_strike", model.Line[0].Id);
        }

        [Test]
        public void MoveRight_LastPosition_IsNotAllowedAndDoesNothing()
        {
            var model = Model();
            model.SelectLine(2);

            Assert.IsFalse(model.CanMoveRight);
            model.MoveRight();

            Assert.AreEqual(2, model.SelectedLine);
            Assert.AreEqual("demo_mend", model.Line[2].Id);
        }

        [Test]
        public void Swap_NeedsBothSelections_ThenExchangesTheCards()
        {
            var model = Model();
            model.SelectLine(0);
            Assert.IsFalse(model.CanSwap);

            model.SelectReserve(0);
            Assert.IsTrue(model.CanSwap);
            var lineCard = model.Line[0].Id;
            var reserveCard = model.Reserve[0].Id;
            model.Swap();

            Assert.AreEqual(reserveCard, model.Line[0].Id);
            Assert.Contains(lineCard, Ids(model.Reserve));
            Assert.AreEqual(3, model.Line.Count);
            Assert.AreEqual(2, model.Reserve.Count);
        }

        [Test]
        public void MoveToReserve_SendsTheCardToTheEndOfTheReserveAndClearsTheSelection()
        {
            var model = Model();
            model.SelectLine(1);

            model.MoveToReserve();

            Assert.AreEqual(2, model.Line.Count);
            Assert.AreEqual("demo_guard", model.Reserve.Last().Id);
            Assert.IsNull(model.SelectedLine);
        }

        [Test]
        public void MoveToLine_FullLine_IsNotAllowed()
        {
            var model = Model();
            model.SelectReserve(0);

            Assert.IsFalse(model.CanMoveToLine);
            model.MoveToLine();

            Assert.AreEqual(3, model.Line.Count);
            Assert.AreEqual(2, model.Reserve.Count);
        }

        [Test]
        public void MoveToLine_FreeSlot_AddsTheReserveCardAtTheEnd()
        {
            var model = Model();
            model.SelectLine(0);
            model.MoveToReserve();
            var reserveCard = model.Reserve[0].Id;
            model.SelectReserve(0);

            Assert.IsTrue(model.CanMoveToLine);
            model.MoveToLine();

            Assert.AreEqual(reserveCard, model.Line.Last().Id);
            Assert.IsNull(model.SelectedReserve);
        }

        [Test]
        public void MoveToReserve_LastLineCard_IsNotAllowed()
        {
            var model = Model();
            while (model.Line.Count > 1)
            {
                model.SelectLine(0);
                model.MoveToReserve();
            }

            model.SelectLine(0);

            Assert.IsFalse(model.CanMoveToReserve);
            model.MoveToReserve();
            Assert.AreEqual(1, model.Line.Count);
        }

        [Test]
        public void Select_SameCardTwice_Unselects_AndOutOfRangeIsIgnored()
        {
            var model = Model();

            model.SelectLine(1);
            model.SelectLine(1);
            model.SelectLine(9);
            model.SelectReserve(-1);

            Assert.IsNull(model.SelectedLine);
            Assert.IsNull(model.SelectedReserve);
        }

        [Test]
        public void Changed_IsRaisedByEveryChange()
        {
            var model = Model();
            var count = 0;
            model.Changed += () => count++;

            model.SelectLine(0);
            model.MoveRight();
            model.SelectReserve(0);
            model.Swap();

            Assert.AreEqual(4, count);
        }

        // --- Start ---

        [Test]
        public void Start_BeginsTheFight_AndFixesTheLine()
        {
            var model = Model();
            model.SelectLine(0);
            model.SelectReserve(0);
            var before = Ids(model.Line);

            var session = model.Start();

            Assert.IsNotNull(session);
            Assert.IsTrue(model.IsStarted);
            Assert.IsFalse(model.CanStart);
            Assert.IsFalse(model.CanMoveRight);
            Assert.IsFalse(model.CanSwap);
            model.MoveRight();
            model.Swap();
            model.SelectLine(2);
            CollectionAssert.AreEqual(before, Ids(model.Line));
            Assert.IsNull(model.SelectedLine);
        }

        [Test]
        public void Start_Twice_ReturnsNullTheSecondTime()
        {
            var model = Model();

            Assert.IsNotNull(model.Start());
            Assert.IsNull(model.Start());
        }

        [Test]
        public void Start_UsesTheLineAsArranged()
        {
            var model = Model(ScreenPreviewPreparations.ProfessorUnknown());
            model.SelectLine(0);
            model.MoveRight();

            var session = model.Start();

            Assert.AreEqual("demo_guard", session.HeroLine[0].Id);
        }

        // --- Screen ---

        [Test]
        public void Layout_HasEveryElementTheViewQueries()
        {
            var root = Bound(Model());

            foreach (var name in new[]
                     {
                         PreparationScreenView.MoveLeftButton, PreparationScreenView.MoveRightButton,
                         PreparationScreenView.SwapButton, PreparationScreenView.ToReserveButton,
                         PreparationScreenView.ToLineButton, PreparationScreenView.StartButton,
                     })
            {
                Assert.IsNotNull(root.Q<Button>(name), name);
            }

            Assert.IsNotNull(root.Q(PreparationScreenView.LineElement));
            Assert.IsNotNull(root.Q(PreparationScreenView.ReserveElement));
            Assert.IsNotNull(root.Q(PreparationScreenView.BossInfoElement));
        }

        [Test]
        public void Bind_ShowsCardsOfTheLineAndTheReserve()
        {
            var root = Bound(Model());

            Assert.AreEqual(3, root.Q(PreparationScreenView.LineElement).childCount);
            Assert.AreEqual(2, root.Q(PreparationScreenView.ReserveElement).childCount);
        }

        [Test]
        public void Bind_Professor_MarksHiddenInformationAsUnknown()
        {
            var root = Bound(Model(ScreenPreviewPreparations.ProfessorUnknown()));

            var unknown = root.Q(PreparationScreenView.BossInfoElement).Query<Label>(className: PreparationScreenView.UnknownClass).ToList();

            Assert.AreEqual(5, unknown.Count); // health, shield and three cards
            Assert.That(unknown.Select(label => label.text), Has.All.Contains(PreparationScreenView.UnknownText));
        }

        [Test]
        public void Bind_MiniBoss_HasNoUnknownInformation()
        {
            var root = Bound(Model());

            Assert.AreEqual(0, root.Q(PreparationScreenView.BossInfoElement).Query<Label>(className: PreparationScreenView.UnknownClass).ToList().Count);
        }

        [Test]
        public void Bind_FollowsTheViewModel_AndMarksTheSelection()
        {
            var model = Model();
            var root = Bound(model);

            model.SelectLine(1);

            var line = root.Q(PreparationScreenView.LineElement);
            Assert.IsTrue(line[1].ClassListContains(PreparationScreenView.SelectedClass));
            Assert.IsFalse(line[0].ClassListContains(PreparationScreenView.SelectedClass));
            Assert.IsTrue(root.Q<Button>(PreparationScreenView.MoveLeftButton).enabledSelf);
            Assert.IsFalse(root.Q<Button>(PreparationScreenView.SwapButton).enabledSelf);
        }

        [Test]
        public void Bind_Detach_StopsFollowingTheViewModel()
        {
            var model = Model();
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PreparationScreenView.LayoutPath);
            var root = layout.CloneTree();
            var detach = PreparationScreenView.Bind(root, model);

            detach();
            model.SelectLine(1);

            Assert.IsFalse(root.Q(PreparationScreenView.LineElement)[1].ClassListContains(PreparationScreenView.SelectedClass));
        }

        [Test]
        public void Bind_AfterStart_DisablesTheControls()
        {
            var model = Model();
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PreparationScreenView.LayoutPath);
            var root = layout.CloneTree();
            PreparationScreenView.Bind(root, model);

            model.SelectLine(0);
            model.Start();
            Assert.IsFalse(root.Q<Button>(PreparationScreenView.StartButton).enabledSelf);
            Assert.IsFalse(root.Q<Button>(PreparationScreenView.MoveRightButton).enabledSelf);
        }

        [Test]
        public void Preview_ThreeVariantsAreRegistered()
        {
            var names = ScreenPreviews.All.Select(preview => preview.Name).Where(name => name.StartsWith("Preparation")).ToList();

            Assert.AreEqual(3, names.Count);
        }
    }
}
