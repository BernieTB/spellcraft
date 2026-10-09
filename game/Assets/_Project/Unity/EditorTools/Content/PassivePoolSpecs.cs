using Game.Core.Effects;
using Game.Core.Upgrades;

namespace Game.Unity.EditorTools.Content
{
    /// <summary>
    /// The passive upgrades of the MVP pool (#119) as plain data with no Unity type, so
    /// <see cref="PassiveUpgradePoolGenerator"/> writes them to assets and a tuning harness (#125) can read them too.
    /// Working amounts, tuned with the simulation (ADR 0006).
    /// </summary>
    public static class PassivePoolSpecs
    {
        public static readonly Spec[] All =
        {
            // Base stats.
            new Spec("PASSIVE_A", PassiveUpgradeKind.MaxHealth, BonusKind.Damage, 5),
            new Spec("PASSIVE_B", PassiveUpgradeKind.MaxHealth, BonusKind.Damage, 10),
            new Spec("PASSIVE_C", PassiveUpgradeKind.StartingShield, BonusKind.Damage, 3),

            // +X to every effect of one kind.
            new Spec("PASSIVE_D", PassiveUpgradeKind.EffectAmount, BonusKind.Damage, 1),
            new Spec("PASSIVE_E", PassiveUpgradeKind.EffectAmount, BonusKind.Damage, 2),
            new Spec("PASSIVE_F", PassiveUpgradeKind.EffectAmount, BonusKind.Heal, 1),
            new Spec("PASSIVE_G", PassiveUpgradeKind.EffectAmount, BonusKind.Shield, 1),
            new Spec("PASSIVE_H", PassiveUpgradeKind.EffectAmount, BonusKind.Shield, 2),

            // Stronger neighbour bonuses.
            new Spec("PASSIVE_I", PassiveUpgradeKind.NeighbourBonus, BonusKind.Damage, 1),
            new Spec("PASSIVE_J", PassiveUpgradeKind.NeighbourBonus, BonusKind.Damage, 2),
        };

        public readonly struct Spec
        {
            public Spec(string id, PassiveUpgradeKind kind, BonusKind effectKind, int amount)
            {
                Id = id;
                Kind = kind;
                EffectKind = effectKind;
                Amount = amount;
            }

            public string Id { get; }

            public PassiveUpgradeKind Kind { get; }

            public BonusKind EffectKind { get; }

            public int Amount { get; }
        }
    }
}
