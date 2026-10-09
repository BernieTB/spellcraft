using Game.Core.Cards;
using Game.Core.Effects;
using Game.Unity.Cards;

namespace Game.Unity.EditorTools.Content
{
    /// <summary>
    /// The numbers and ids of the MVP class (<c>CLASS_A</c>, ADR 0007) as plain data with no Unity type, so
    /// <see cref="MvpClassContentGenerator"/> writes them to assets and a tuning harness (#125) can read them too.
    /// Working values, tuned with the balance bots (#125, ADR 0017); not final balance decisions. Cast times are in ticks
    /// (4 per second on the run screen); the starting line is disordered on purpose, so the player has to move the weaver.
    /// </summary>
    public static class MvpClassSpecs
    {
        /// <summary>Hero's max health at the start of every fight.</summary>
        public const int MaxHealth = 40;

        /// <summary>Hero's shield at the start of every fight.</summary>
        public const int StartingShield = 0;

        /// <summary>Spell line slots at the start of a run (ADR 0009).</summary>
        public const int StartingLineCapacity = 4;

        // Casts needed for the two evolution stages of every card (first-pass numbers).
        public const int FirstStageCasts = 3;
        public const int SecondStageCasts = 7;

        public static readonly CardSpec[] StartingSpecs =
        {
            // Two damage cards.
            new CardSpec("CARD_A", 8, new[] { Damage(3) }, new Mod[0],
                At(FirstStageCasts, new[] { Damage(4) }), At(SecondStageCasts, new[] { Damage(5) })),
            new CardSpec("CARD_B", 12, new[] { Damage(5) }, new Mod[0],
                At(FirstStageCasts, new[] { Damage(7) }), At(SecondStageCasts, new[] { Damage(9) })),

            // The weaver card: a small hit that boosts the next card's damage.
            new CardSpec("CARD_C", 8, new[] { Damage(1) }, new[] { NextDamage(4) },
                At(FirstStageCasts, new[] { Damage(1) }, NextDamage(6)),
                At(SecondStageCasts, new[] { Damage(2) }, NextDamage(8))),

            // The defensive card gives shield, not healing (ADR 0009).
            new CardSpec("CARD_D", 8, new[] { Shield(4) }, new Mod[0],
                At(FirstStageCasts, new[] { Shield(6) }), At(SecondStageCasts, new[] { Shield(8) })),
        };

        public static readonly CardSpec[] PoolSpecs =
        {
            new CardSpec("CARD_E", 4, new[] { Damage(2) }, new Mod[0],
                At(FirstStageCasts, new[] { Damage(3) }), At(SecondStageCasts, new[] { Damage(4) })),
            new CardSpec("CARD_F", 16, new[] { Damage(8) }, new Mod[0],
                At(FirstStageCasts, new[] { Damage(11) }), At(SecondStageCasts, new[] { Damage(14) })),
            new CardSpec("CARD_G", 12, new[] { Damage(3), Shield(2) }, new Mod[0],
                At(FirstStageCasts, new[] { Damage(4), Shield(3) }),
                At(SecondStageCasts, new[] { Damage(5), Shield(4) })),
            new CardSpec("CARD_H", 8, new[] { Shield(2) }, new[] { NextShield(4) },
                At(FirstStageCasts, new[] { Shield(3) }, NextShield(6)),
                At(SecondStageCasts, new[] { Shield(4) }, NextShield(8))),
            new CardSpec("CARD_I", 12, new[] { Damage(2) }, new[] { PreviousDamage(6) },
                At(FirstStageCasts, new[] { Damage(2) }, PreviousDamage(8)),
                At(SecondStageCasts, new[] { Damage(3) }, PreviousDamage(10))),
            new CardSpec("CARD_J", 8, new[] { Heal(2) }, new Mod[0],
                At(FirstStageCasts, new[] { Heal(3) }), At(SecondStageCasts, new[] { Heal(4) })),
            new CardSpec("CARD_K", 12, new[] { Damage(2) }, new[] { NextDamage(2), PreviousDamage(2) },
                At(FirstStageCasts, new[] { Damage(2) }, NextDamage(4), PreviousDamage(4)),
                At(SecondStageCasts, new[] { Damage(3) }, NextDamage(6), PreviousDamage(6))),
            new CardSpec("CARD_L", 4, new[] { Damage(1) }, new[] { NextDamage(2) },
                At(FirstStageCasts, new[] { Damage(1) }, NextDamage(4)),
                At(SecondStageCasts, new[] { Damage(2) }, NextDamage(6))),
            new CardSpec("CARD_M", 20, new[] { Damage(6), Shield(4) }, new Mod[0],
                At(FirstStageCasts, new[] { Damage(8), Shield(6) }),
                At(SecondStageCasts, new[] { Damage(10), Shield(8) })),
            new CardSpec("CARD_N", 16, new[] { Shield(6) }, new[] { NextDamage(6) },
                At(FirstStageCasts, new[] { Shield(8) }, NextDamage(8)),
                At(SecondStageCasts, new[] { Shield(10) }, NextDamage(10))),
        };

        /// <summary>Ids of the starting deck cards, in spell line order.</summary>
        /// <remarks>
        /// The weaver card comes before the second damage card, so its bonus lands on that card.
        /// </remarks>
        public static readonly string[] StartingDeckIds = { "CARD_A", "CARD_B", "CARD_C", "CARD_D" };

        private static Eff Damage(int amount) => new Eff(EffectKind.DealDamage, amount);

        private static Eff Heal(int amount) => new Eff(EffectKind.Heal, amount);

        private static Eff Shield(int amount) => new Eff(EffectKind.GainShield, amount);

        private static Mod NextDamage(int amount) => new Mod(BonusKind.Damage, NeighbourDirection.Next, amount);

        private static Mod PreviousDamage(int amount) => new Mod(BonusKind.Damage, NeighbourDirection.Previous, amount);

        private static Mod NextShield(int amount) => new Mod(BonusKind.Shield, NeighbourDirection.Next, amount);

        private static Stage At(int casts, Eff[] effects, params Mod[] modifiers) => new Stage(casts, effects, modifiers);

        public readonly struct Eff
        {
            public Eff(EffectKind kind, int amount)
            {
                Kind = kind;
                Amount = amount;
            }

            public EffectKind Kind { get; }

            public int Amount { get; }
        }

        public readonly struct Mod
        {
            public Mod(BonusKind kind, NeighbourDirection direction, int amount)
            {
                Kind = kind;
                Direction = direction;
                Amount = amount;
            }

            public BonusKind Kind { get; }

            public NeighbourDirection Direction { get; }

            public int Amount { get; }
        }

        public readonly struct Stage
        {
            public Stage(int casts, Eff[] effects, Mod[] modifiers)
            {
                Casts = casts;
                Effects = effects;
                Modifiers = modifiers;
            }

            public int Casts { get; }

            public Eff[] Effects { get; }

            public Mod[] Modifiers { get; }
        }

        public readonly struct CardSpec
        {
            public CardSpec(string id, int castTime, Eff[] effects, Mod[] modifiers, params Stage[] stages)
            {
                Id = id;
                CastTime = castTime;
                Effects = effects;
                Modifiers = modifiers;
                Stages = stages;
            }

            public string Id { get; }

            public int CastTime { get; }

            public Eff[] Effects { get; }

            public Mod[] Modifiers { get; }

            public Stage[] Stages { get; }
        }
    }
}
