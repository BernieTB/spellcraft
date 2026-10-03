using System;
using System.Linq;
using Game.Core;
using Game.Unity.EditorTools.UI;
using Game.Unity.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Game.Unity.Tests.UI
{
    /// <summary>
    /// Checks the title screen layout and its view without entering Play mode.
    /// </summary>
    public class TitleScreenTests
    {
        private static VisualElement CloneTitleScreen()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BootstrapSceneBuilder.TitleScreenPath);
            Assert.IsNotNull(layout, $"Missing {BootstrapSceneBuilder.TitleScreenPath}.");
            return layout.CloneTree();
        }

        [Test]
        public void Layout_Clone_HasTitleLabel()
        {
            var root = CloneTitleScreen();

            Assert.IsNotNull(root.Q<Label>(TitleScreenView.TitleElement));
        }

        [Test]
        public void Layout_Clone_UsesTheCommonStyleSheet()
        {
            var common = AssetDatabase.LoadAssetAtPath<StyleSheet>(BootstrapSceneBuilder.CommonStylePath);
            Assert.IsNotNull(common, $"Missing {BootstrapSceneBuilder.CommonStylePath}.");

            var root = CloneTitleScreen();
            var attached = Enumerable.Range(0, root.styleSheets.count).Select(i => root.styleSheets[i]);

            Assert.That(attached, Has.Member(common));
        }

        [Test]
        public void Bind_TitleScreen_ShowsTheGameName()
        {
            var root = CloneTitleScreen();

            TitleScreenView.Bind(root);

            Assert.AreEqual(GameInfo.Name, root.Q<Label>(TitleScreenView.TitleElement).text);
        }

        [Test]
        public void Bind_TreeWithoutTitle_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => TitleScreenView.Bind(new VisualElement()));
        }
    }
}
