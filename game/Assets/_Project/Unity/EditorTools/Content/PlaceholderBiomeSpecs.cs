using Game.Core.Cards;
using Game.Core.Effects;

namespace Game.Unity.EditorTools.Content
{
    /// <summary>
    /// The numbers and ids of the placeholder biome of the Vertical slice (#72), as plain data with no Unity type, so
    /// <see cref="PlaceholderBiomeGenerator"/> writes them to assets and a tuning harness can read them too.
    /// </summary>
    /// <remarks>
    /// Ids are working names (<c>CARD_*</c>, <c>ENEMY_*</c>...) with no invented words: final names, words and
    /// creatures are written by the project owner. The numbers were tuned in #125 with the balance bots of
    /// <c>Tests/Unity/Balance</c> (a bot that never touches the line, one that orders it and one that optimises it) so
    /// that ordering the line matters: the starting line is deliberately disordered, regular fights are safe for an
    /// ordered line, the mini-bosses are spikes and the professor is a wall without the mini-bosses' rewards and
    /// revelations (ADR 0009, 0010, 0011). They remain working values, not final balance decisions.
    /// </remarks>
    public static class PlaceholderBiomeSpecs
    {
        /// <summary>Id of the biome.</summary>
        public const string BiomeId = "BIOME_01";

        /// <summary>Regular fights to win before the professor is available (ADR 0009).</summary>
        public const int MinimumRegularFights = 8;

        /// <summary>
        /// Global fight time limit in ticks (ADR 0011): above the longest fight of the balance bots (about 1500 ticks), so it is only a
        /// safety net.
        /// </summary>
        public const int FightTimeLimit = 2000;

        /// <summary>Extra cost of each level past <see cref="LevelCosts"/>.</summary>
        public const int CostIncreaseAfterList = 3;

        /// <summary>Id of the professor's encounter.</summary>
        public const string ProfessorEncounterId = "ENCOUNTER_PROFESSOR_01";

        /// <summary>Id of the professor.</summary>
        public const string ProfessorId = "ENEMY_PROFESSOR_01";

        /// <summary>Hero level-up costs from level 1 (about 5 level-ups on the 8 regular fights of the shortest path).</summary>
        public static readonly int[] LevelCosts = { 6, 7, 9, 11, 13 };

        /// <summary>The cards of the enemies (one attack card per regular monster) and the two mini-boss unique cards.</summary>
        public static readonly CardSpec[] Cards =
        {
            // Regular monsters: one attack card each (ADR 0011).
            new CardSpec("CARD_ENEMY_01", 12, new[] { Damage(2) }),
            new CardSpec("CARD_ENEMY_02", 16, new[] { Damage(3) }),
            new CardSpec("CARD_ENEMY_03", 8, new[] { Damage(1) }),
            new CardSpec("CARD_ENEMY_04", 13, new[] { Damage(4) }),

            // First mini-boss: a small hit that boosts the next hit, a strong hit, then a shield.
            new CardSpec("CARD_ENEMY_05", 8, new[] { Damage(4) }, NextDamage(2)),
            new CardSpec("CARD_ENEMY_06", 12, new[] { Damage(6) }),
            new CardSpec("CARD_ENEMY_07", 16, new[] { Shield(11) }),

            // Second mini-boss: a heavy hit, a shield that boosts the next hit, a quick hit.
            new CardSpec("CARD_ENEMY_08", 16, new[] { Damage(8) }),
            new CardSpec("CARD_ENEMY_09", 8, new[] { Shield(6) }, NextDamage(2)),
            new CardSpec("CARD_ENEMY_10", 8, new[] { Damage(4) }),

            // Professor: five cards, with boosts and a shield.
            new CardSpec("CARD_ENEMY_11", 12, new[] { Damage(8) }, NextDamage(2)),
            new CardSpec("CARD_ENEMY_12", 12, new[] { Damage(8) }),
            new CardSpec("CARD_ENEMY_13", 16, new[] { Shield(15) }),
            new CardSpec("CARD_ENEMY_14", 8, new[] { Damage(5) }, PreviousDamage(1)),
            new CardSpec("CARD_ENEMY_15", 16, new[] { Damage(13) }),

            // Unique cards given by the mini-bosses (ADR 0010).
            new CardSpec("CARD_UNIQUE_01", 12, new[] { Damage(10), Shield(4) }),
            new CardSpec("CARD_UNIQUE_02", 8, new[] { Damage(5) }, NextDamage(6)),
        };

        /// <summary>Regular monsters, mini-bosses and the professor. XP: mini-bosses above any regular fight.</summary>
        public static readonly EnemySpec[] Enemies =
        {
            new EnemySpec("ENEMY_01", Rank.Regular, 48, 0, 5, "CARD_ENEMY_01"),
            new EnemySpec("ENEMY_02", Rank.Regular, 66, 0, 7, "CARD_ENEMY_02"),
            new EnemySpec("ENEMY_03", Rank.Regular, 38, 0, 3, "CARD_ENEMY_03"),
            new EnemySpec("ENEMY_04", Rank.Regular, 58, 10, 6, "CARD_ENEMY_04"),
            new EnemySpec("ENEMY_MINIBOSS_01", Rank.MiniBoss, 165, 0, 20, "CARD_ENEMY_05", "CARD_ENEMY_06", "CARD_ENEMY_07"),
            new EnemySpec("ENEMY_MINIBOSS_02", Rank.MiniBoss, 220, 20, 25, "CARD_ENEMY_08", "CARD_ENEMY_09", "CARD_ENEMY_10"),
            new EnemySpec(
                "ENEMY_PROFESSOR_01", Rank.Professor, 470, 34, 40,
                "CARD_ENEMY_11", "CARD_ENEMY_12", "CARD_ENEMY_13", "CARD_ENEMY_14", "CARD_ENEMY_15"),
        };

        /// <summary>All the encounters: the regular ones, one per mini-boss and the professor's.</summary>
        public static readonly EncounterSpec[] Encounters =
        {
            new EncounterSpec("ENCOUNTER_01", "ENEMY_01"),
            new EncounterSpec("ENCOUNTER_02", "ENEMY_02"),
            new EncounterSpec("ENCOUNTER_03", "ENEMY_03"),
            new EncounterSpec("ENCOUNTER_04", "ENEMY_01", "ENEMY_03"),
            new EncounterSpec("ENCOUNTER_05", "ENEMY_04"),
            new EncounterSpec("ENCOUNTER_MINIBOSS_01", "ENEMY_MINIBOSS_01"),
            new EncounterSpec("ENCOUNTER_MINIBOSS_02", "ENEMY_MINIBOSS_02"),
            new EncounterSpec(ProfessorEncounterId, ProfessorId),
        };

        /// <summary>
        /// Ids of the encounters drawn for regular fights, each draw taking one entry with equal chances. ENCOUNTER_01
        /// is listed twice, so the objective monsters ENEMY_01 and ENEMY_03 come up often.
        /// </summary>
        public static readonly string[] RegularPool =
        {
            "ENCOUNTER_01", "ENCOUNTER_01", "ENCOUNTER_02", "ENCOUNTER_03", "ENCOUNTER_04", "ENCOUNTER_05",
        };

        /// <summary>The two secret rooms. Each mini-boss reveals different cards of the professor's line.</summary>
        public static readonly RoomSpec[] Rooms =
        {
            new RoomSpec("ROOM_01", "ENEMY_01", 3, "ENCOUNTER_MINIBOSS_01", "CARD_UNIQUE_01", false, true, 0, 1),
            new RoomSpec("ROOM_02", "ENEMY_03", 3, "ENCOUNTER_MINIBOSS_02", "CARD_UNIQUE_02", true, false, 2, 3),
        };

        /// <summary>Kind of a card effect; the values match <c>EffectKind</c>.</summary>
        public enum Kind
        {
            Damage = 0,
            Heal = 1,
            Shield = 2,
        }

        /// <summary>Rank of an enemy; the values match <c>EnemyRank</c>.</summary>
        public enum Rank
        {
            Regular = 0,
            MiniBoss = 1,
            Professor = 2,
        }

        private static Eff Damage(int amount) => new Eff(Kind.Damage, amount);

        private static Eff Shield(int amount) => new Eff(Kind.Shield, amount);

        private static Mod NextDamage(int amount) => new Mod(BonusKind.Damage, NeighbourDirection.Next, amount);

        private static Mod PreviousDamage(int amount) => new Mod(BonusKind.Damage, NeighbourDirection.Previous, amount);

        public readonly struct Eff
        {
            public Eff(Kind kind, int amount)
            {
                Kind = kind;
                Amount = amount;
            }

            public Kind Kind { get; }

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

        public readonly struct CardSpec
        {
            public CardSpec(string id, int castTime, Eff[] effects, params Mod[] modifiers)
            {
                Id = id;
                CastTime = castTime;
                Effects = effects;
                Modifiers = modifiers;
            }

            public string Id { get; }

            public int CastTime { get; }

            public Eff[] Effects { get; }

            public Mod[] Modifiers { get; }
        }

        public readonly struct EnemySpec
        {
            public EnemySpec(string id, Rank rank, int maxHealth, int shield, int xp, params string[] cardIds)
            {
                Id = id;
                Rank = rank;
                MaxHealth = maxHealth;
                Shield = shield;
                Xp = xp;
                CardIds = cardIds;
            }

            public string Id { get; }

            public Rank Rank { get; }

            public int MaxHealth { get; }

            public int Shield { get; }

            public int Xp { get; }

            public string[] CardIds { get; }
        }

        public readonly struct EncounterSpec
        {
            public EncounterSpec(string id, params string[] enemyIds)
            {
                Id = id;
                EnemyIds = enemyIds;
            }

            public string Id { get; }

            public string[] EnemyIds { get; }
        }

        public readonly struct RoomSpec
        {
            public RoomSpec(
                string id,
                string objectiveEnemyId,
                int objectiveCount,
                string miniBossEncounterId,
                string uniqueCardId,
                bool revealsHealth,
                bool revealsShield,
                params int[] revealedPositions)
            {
                Id = id;
                ObjectiveEnemyId = objectiveEnemyId;
                ObjectiveCount = objectiveCount;
                MiniBossEncounterId = miniBossEncounterId;
                UniqueCardId = uniqueCardId;
                RevealsHealth = revealsHealth;
                RevealsShield = revealsShield;
                RevealedPositions = revealedPositions;
            }

            public string Id { get; }

            public string ObjectiveEnemyId { get; }

            public int ObjectiveCount { get; }

            public string MiniBossEncounterId { get; }

            public string UniqueCardId { get; }

            public bool RevealsHealth { get; }

            public bool RevealsShield { get; }

            public int[] RevealedPositions { get; }
        }
    }
}
