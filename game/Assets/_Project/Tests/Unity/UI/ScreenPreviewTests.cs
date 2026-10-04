using System;
using System.Linq;
using Game.Core.Combat.Recap;
using Game.Unity.EditorTools.UI;
using Game.Unity.UI;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Game.Unity.Tests.UI
{
    /// <summary>
    /// Checks the data and the registry behind the screen preview window, without opening the window.
    /// </summary>
    public class ScreenPreviewTests
    {
        [Test]
        public void Victory_IsAWinWithoutAnalysis()
        {
            var recap = ScreenPreviewFights.Victory();

            Assert.AreEqual(FightRecapOutcome.Victory, recap.Outcome);
            Assert.IsNull(recap.Defeat);
            Assert.AreEqual(2, recap.Combatants.Count);
        }

        [Test]
        public void Defeat_IsALossWithAnAnalysisBlamingWastedBonuses()
        {
            var recap = ScreenPreviewFights.Defeat();

            Assert.AreEqual(FightRecapOutcome.Defeat, recap.Outcome);
            Assert.IsNotNull(recap.Defeat);
            Assert.AreEqual(DefeatCause.WastedBonuses, recap.Defeat.MainCause);
            Assert.IsFalse(recap.Hero.BonusWasted.IsNone);
        }

        [Test]
        public void TimeLimit_RunsOutOfTimeWithoutAnAnalysis()
        {
            var recap = ScreenPreviewFights.TimeLimit();

            Assert.AreEqual(FightRecapOutcome.TimeLimit, recap.Outcome);
            Assert.IsTrue(recap.RanOutOfTime);
            Assert.IsNull(recap.Defeat);
        }

        [Test]
        public void Fights_AreTheSameEveryTime()
        {
            Assert.AreEqual(ScreenPreviewFights.DefeatLog().ToText(), ScreenPreviewFights.DefeatLog().ToText());
            Assert.AreEqual(ScreenPreviewFights.VictoryLog().ToJson(), ScreenPreviewFights.VictoryLog().ToJson());
        }

        [Test]
        public void All_HasTheRecapVariantsAndUniqueNames()
        {
            var names = ScreenPreviews.All.Select(preview => preview.Name).ToList();

            Assert.That(names, Is.Unique);
            Assert.That(names, Has.Member("Title"));
            Assert.That(names.Count(name => name.StartsWith("Recap", StringComparison.Ordinal)), Is.EqualTo(3));
        }

        [Test]
        public void All_EveryPreviewShowsInAScreenHost()
        {
            var host = new ScreenHost(new VisualElement());

            foreach (var preview in ScreenPreviews.All)
            {
                VisualElement screen = null;
                Assert.DoesNotThrow(() => screen = preview.ShowIn(host), preview.Name);
                Assert.AreSame(screen, host.Current, preview.Name);
            }
        }

        [Test]
        public void ShowIn_RecapDefeat_FillsTheScreenWithTheDemoFight()
        {
            var host = new ScreenHost(new VisualElement());
            var preview = ScreenPreviews.All.Single(p => p.Name == "Recap: defeat");

            var screen = preview.ShowIn(host);

            Assert.AreEqual("Defeat", screen.Q<Label>(RecapScreenView.OutcomeTitleElement).text);
            Assert.IsFalse(screen.Q(RecapScreenView.DefeatPanelElement).ClassListContains(RecapScreenView.HiddenClass));
        }

        [Test]
        public void ShowIn_MissingLayout_NamesThePreview()
        {
            var host = new ScreenHost(new VisualElement());
            var preview = new ScreenPreview("Ghost", "Assets/_Project/Unity/UI/Screens/Missing.uxml", _ => { });

            var exception = Assert.Throws<InvalidOperationException>(() => preview.ShowIn(host));

            StringAssert.Contains("Ghost", exception.Message);
        }

        [Test]
        public void Constructor_InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentException>(() => new ScreenPreview(" ", "path", _ => { }));
            Assert.Throws<ArgumentException>(() => new ScreenPreview("Name", "", _ => { }));
            Assert.Throws<ArgumentNullException>(() => new ScreenPreview("Name", "path", null));
        }
    }
}
