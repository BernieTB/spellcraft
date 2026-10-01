using NUnit.Framework;

namespace Game.Core.Tests
{
    public class CoreInfoTests
    {
        [Test]
        public void ModuleName_IsGameCore()
        {
            Assert.AreEqual("Game.Core", CoreInfo.ModuleName);
        }
    }
}
