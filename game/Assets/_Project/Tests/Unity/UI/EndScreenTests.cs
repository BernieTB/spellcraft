using System;
using Game.Core.Runs;
using Game.Unity.EditorTools.UI;
using Game.Unity.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Game.Unity.Tests.UI
{
    /// <summary>The end-of-run screen (#88) without Play mode.</summary>
    public class EndScreenTests
    {
        private static VisualElement Clone()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BootstrapSceneBuilder.EndScreenPath);
            Assert.IsNotNull(layout, $"Missing {BootstrapSceneBuilder.EndScreenPath}.");
            return layout.CloneTree();
        }

        [Test]
        public void Bind_Victory_ShowsVictoryAndTheSummary()
        {
            var root = Clone();

            EndScreenView.Bind(root, RunOutcome.Victory, "Summary line", null);

            Assert.AreEqual("Victory", root.Q<Label>(EndScreenView.TitleElement).text);
            Assert.IsTrue(root.Q<Label>(EndScreenView.TitleElement).ClassListContains(EndScreenView.VictoryClass));
            Assert.AreEqual("Summary line", root.Q<Label>(EndScreenView.SummaryElement).text);
        }

        [Test]
        public void Bind_Defeat_ShowsDefeat()
        {
            var root = Clone();

            EndScreenView.Bind(root, RunOutcome.Defeat, "x", null);

            Assert.AreEqual("Defeat", root.Q<Label>(EndScreenView.TitleElement).text);
            Assert.IsTrue(root.Q<Label>(EndScreenView.TitleElement).ClassListContains(EndScreenView.DefeatClass));
            Assert.IsFalse(root.Q<Label>(EndScreenView.TitleElement).ClassListContains(EndScreenView.VictoryClass));
        }

        [Test]
        public void Bind_TreeWithoutElements_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => EndScreenView.Bind(new VisualElement(), RunOutcome.Victory, "x", null));
        }
    }
}
