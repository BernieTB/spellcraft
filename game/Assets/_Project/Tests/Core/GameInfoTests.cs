using NUnit.Framework;

namespace Game.Core.Tests
{
    public class GameInfoTests
    {
        [Test]
        public void Name_IsWorkingTitle()
        {
            Assert.AreEqual("Spellcraft", GameInfo.Name);
        }
    }
}
