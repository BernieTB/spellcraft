using Game.Core.Combat.Log;
using Game.Core.Effects;
using Game.Unity.DebugTools;
using NUnit.Framework;

namespace Game.Unity.Tests.DebugTools
{
    public class CombatEventTextTests
    {
        // Arbitrary test data, not real content or balance values.
        private static CombatEvent Cast(EffectBonus bonus, EffectBonus wastedBonus) =>
            new CombatEvent(1, 2, CombatEventKind.CardCast, "test_hit", 1, 0, 1, 0, 0, 0, 10, 0, bonus, wastedBonus);

        [Test]
        public void Describe_CastWithoutBonus_HasNoBonusText()
        {
            Assert.AreEqual("t2  Hero [1] test_hit casts at Enemy 1", CombatEventText.Describe(Cast(EffectBonus.None, EffectBonus.None)));
        }

        [Test]
        public void Describe_CastWithUsedBonus_ListsIt()
        {
            var line = CombatEventText.Describe(Cast(new EffectBonus(3, 0, 0), EffectBonus.None));

            Assert.AreEqual("t2  Hero [1] test_hit casts at Enemy 1 with +3 damage bonus", line);
        }

        [Test]
        public void Describe_CastWithWastedBonus_MarksItWasted()
        {
            var line = CombatEventText.Describe(Cast(new EffectBonus(3, 4, 0), new EffectBonus(0, 4, 0)));

            Assert.AreEqual("t2  Hero [1] test_hit casts at Enemy 1 with +3 damage bonus, +4 heal bonus (wasted)", line);
        }

        [Test]
        public void Describe_CastWithPartlyWastedBonus_ShowsWastedAmount()
        {
            var line = CombatEventText.Describe(Cast(new EffectBonus(0, 0, 5), new EffectBonus(0, 0, 2)));

            Assert.AreEqual("t2  Hero [1] test_hit casts at Enemy 1 with +5 shield bonus (2 wasted)", line);
        }
    }
}
