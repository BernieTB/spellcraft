using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Effects;
using Game.Core.Runs;
using Game.Core.Upgrades;
using Game.Unity.UI;

namespace Game.Unity.EditorTools.UI
{
    /// <summary>
    /// Demo level-up offers for the screen preview window: placeholder <c>demo_*</c> cards and passives, not game
    /// content. Editor assembly only.
    /// </summary>
    public static class ScreenPreviewLevelUps
    {
        /// <summary>An offer with a free slot in the spell line.</summary>
        public static LevelUpScreenModel FreeSlot() => Model(2, 4);

        /// <summary>An offer while the spell line is full: reserve or replacement.</summary>
        public static LevelUpScreenModel FullLine() => Model(4, 4);

        public static LevelUpOffer Offer()
        {
            return new LevelUpOffer(new[]
            {
                new LevelUpPackage(
                    new CardDefinition("demo_strike", 3, new IEffect[] { new DealDamageEffect(6) }),
                    new PassiveUpgrade("demo_health", PassiveUpgradeKind.MaxHealth, 5)),
                new LevelUpPackage(
                    new CardDefinition(
                        "demo_boost",
                        2,
                        new IEffect[] { new GainShieldEffect(3) },
                        new[] { new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, 2) }),
                    new PassiveUpgrade("demo_damage", PassiveUpgradeKind.EffectAmount, BonusKind.Damage, 1)),
                new LevelUpPackage(
                    new CardDefinition("demo_heal", 4, new IEffect[] { new HealEffect(5) }),
                    new PassiveUpgrade("demo_shield", PassiveUpgradeKind.StartingShield, 3)),
            });
        }

        private static LevelUpScreenModel Model(int lineCount, int capacity)
        {
            var line = new List<CardInstance>();
            for (var i = 0; i < lineCount; i++)
            {
                line.Add(new CardInstance(
                    i + 1, new CardDefinition("demo_line_" + (i + 1), 2, new IEffect[] { new DealDamageEffect(2) })));
            }

            return new LevelUpScreenModel(Offer(), line, capacity);
        }
    }
}
