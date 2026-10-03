using System;
using Game.Unity.Cards;
using Game.Unity.Combat;
using Game.Unity.Tests.Enemies;
using NUnit.Framework;

namespace Game.Unity.Tests.Combat
{
    public class FightParticipantBuilderTests
    {
        private TestAssets _assets;

        [SetUp]
        public void SetUp()
        {
            _assets = new TestAssets();
        }

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        [Test]
        public void Create_EmptyCardSlot_ThrowsNamingLabelAndPosition()
        {
            var line = new[] { _assets.Card("test_card_a"), null };

            var exception = Assert.Throws<InvalidOperationException>(
                () => FightParticipantBuilder.Create(5, 0, line, "enemy 2"));

            StringAssert.Contains("enemy 2", exception.Message);
            StringAssert.Contains("position 1", exception.Message);
        }

        [Test]
        public void Create_NullSpellLine_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => FightParticipantBuilder.Create(5, 0, null, "hero"));
        }

        [Test]
        public void Create_ZeroHealth_ThrowsArgumentException()
        {
            var line = new CardAsset[] { _assets.Card("test_card_a") };

            Assert.Throws(Is.InstanceOf<ArgumentException>(), () => FightParticipantBuilder.Create(0, 0, line, "hero"));
        }
    }
}
