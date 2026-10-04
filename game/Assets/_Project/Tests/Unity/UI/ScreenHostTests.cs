using System;
using Game.Unity.EditorTools.UI;
using Game.Unity.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Game.Unity.Tests.UI
{
    /// <summary>
    /// Checks that the screen host swaps screens in a plain container, without entering Play mode.
    /// </summary>
    public class ScreenHostTests
    {
        private static VisualTreeAsset Layout(string path)
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            Assert.IsNotNull(layout, $"Missing {path}.");
            return layout;
        }

        [Test]
        public void Show_Layout_AddsTheClonedScreenToTheContainer()
        {
            var container = new VisualElement();
            var host = new ScreenHost(container);

            var screen = host.Show(Layout(BootstrapSceneBuilder.TitleScreenPath));

            Assert.AreSame(screen, host.Current);
            Assert.AreEqual(1, container.childCount);
            Assert.AreSame(screen, container[0]);
            Assert.IsNotNull(screen.Q<Label>(TitleScreenView.TitleElement));
            Assert.IsTrue(screen.ClassListContains(ScreenHost.SlotClass));
        }

        [Test]
        public void Show_AnotherLayout_ReplacesTheCurrentScreen()
        {
            var container = new VisualElement();
            var host = new ScreenHost(container);
            var title = host.Show(Layout(BootstrapSceneBuilder.TitleScreenPath));

            var recap = host.Show(Layout(BootstrapSceneBuilder.RecapScreenPath));

            Assert.AreNotSame(title, recap);
            Assert.AreEqual(1, container.childCount, "Only one screen at a time.");
            Assert.IsNull(title.parent, "The previous screen is removed.");
            Assert.AreSame(recap, host.Current);
            Assert.IsNotNull(recap.Q(RecapScreenView.HeroElement));
        }

        [Test]
        public void Show_SameLayoutTwice_GivesAFreshTree()
        {
            var host = new ScreenHost(new VisualElement());
            var layout = Layout(BootstrapSceneBuilder.TitleScreenPath);

            var first = host.Show(layout);
            var second = host.Show(layout);

            Assert.AreNotSame(first, second);
        }

        [Test]
        public void Show_LeavesTheOtherChildrenOfTheContainerAlone()
        {
            var container = new VisualElement();
            var other = new VisualElement();
            container.Add(other);
            var host = new ScreenHost(container);

            host.Show(Layout(BootstrapSceneBuilder.TitleScreenPath));
            host.Show(Layout(BootstrapSceneBuilder.RecapScreenPath));

            Assert.AreSame(container, other.parent);
            Assert.AreEqual(2, container.childCount);
        }

        [Test]
        public void Clear_RemovesTheCurrentScreen()
        {
            var container = new VisualElement();
            var host = new ScreenHost(container);
            host.Show(Layout(BootstrapSceneBuilder.TitleScreenPath));

            host.Clear();

            Assert.IsNull(host.Current);
            Assert.AreEqual(0, container.childCount);
        }

        [Test]
        public void Clear_WithoutScreen_DoesNothing()
        {
            var host = new ScreenHost(new VisualElement());

            Assert.DoesNotThrow(host.Clear);
            Assert.IsNull(host.Current);
        }

        [Test]
        public void Constructor_NullContainer_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ScreenHost(null));
        }

        [Test]
        public void Show_NullLayout_Throws()
        {
            var host = new ScreenHost(new VisualElement());

            Assert.Throws<ArgumentNullException>(() => host.Show(null));
        }
    }
}
