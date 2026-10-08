using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Meta;
using Game.Core.Runs;

namespace Game.Unity.EditorTools.UI
{
    /// <summary>
    /// Runs ready for a preparation phase, built by code, so the screen preview can show the preparation screen:
    /// one before a mini-boss (everything known) and one before a professor (only some of it revealed).
    /// </summary>
    /// <remarks>Demo data, not game content: <c>demo_*</c> placeholders, numbers chosen only to fill the screen.</remarks>
    public static class ScreenPreviewPreparations
    {
        private const string RoomId = "demo_room";

        private static readonly CardDefinition Strike = Card("demo_strike", 3, new DealDamageEffect(4));
        private static readonly CardDefinition Guard = Card("demo_guard", 2, new GainShieldEffect(3));
        private static readonly CardDefinition Mend = Card("demo_mend", 2, new HealEffect(2));
        private static readonly CardDefinition Bolt = Card("demo_bolt", 4, new DealDamageEffect(6));
        private static readonly CardDefinition Unique = Card("demo_unique", 3, new DealDamageEffect(9));
        private static readonly CardDefinition EnemyHit = Card("demo_enemy_hit", 4, new DealDamageEffect(3));
        private static readonly CardDefinition EnemyWard = Card("demo_enemy_ward", 3, new GainShieldEffect(2));
        private static readonly CardDefinition EnemyBurst = Card("demo_enemy_burst", 5, new DealDamageEffect(8));

        private static readonly EnemyDefinition Grunt = new EnemyDefinition("demo_grunt", 12, 0, new[] { EnemyHit }, 1);
        private static readonly EnemyDefinition MiniBossEnemy =
            new EnemyDefinition("demo_miniboss", 20, 2, new[] { EnemyHit, EnemyWard }, 5);
        private static readonly EnemyDefinition Professor =
            new EnemyDefinition("demo_professor", 40, 5, new[] { EnemyHit, EnemyBurst, EnemyWard }, 20);

        private static readonly EncounterDefinition GruntFight = new EncounterDefinition("demo_grunt_fight", new[] { Grunt });
        private static readonly EncounterDefinition MiniBossFight = new EncounterDefinition("demo_miniboss_fight", new[] { MiniBossEnemy });
        private static readonly EncounterDefinition ProfessorFight = new EncounterDefinition("demo_professor_fight", new[] { Professor });

        /// <summary>A preparation before a mini-boss: its health, shield and cards are all shown.</summary>
        public static BossPreparation MiniBoss()
        {
            var run = CreateRun();
            run.UnlockSecretRoom(RoomId, MiniBossFight);
            return run.BeginPreparation(RunStep.SecretRoom(RoomId));
        }

        /// <summary>A preparation before the professor with nothing revealed.</summary>
        public static BossPreparation ProfessorUnknown()
        {
            return CreateRun().BeginPreparation(RunStep.Professor);
        }

        /// <summary>A preparation before the professor with the health and the first card revealed.</summary>
        public static BossPreparation ProfessorPartlyKnown()
        {
            var bestiary = new Bestiary();
            bestiary.Reveal(Professor, new ProfessorRevelation(true, false, new[] { 0 }));
            return CreateRun().BeginPreparation(RunStep.Professor, bestiary);
        }

        private static Run CreateRun()
        {
            var heroClass = new ClassDefinition("demo_class", 100, 0, 3, new[] { Strike, Guard }, new CardDefinition[0]);
            var room = new SecretRoomDefinition(
                RoomId, new ObjectiveDefinition(Grunt.Id, 1), MiniBossFight, 1, Unique, new ProfessorRevelation(false, false, new int[0]));
            var biome = new BiomeDefinition("demo_biome", new[] { GruntFight }, 1, ProfessorFight, Professor, new[] { room });
            var run = new Run(heroClass, biome, new RunRules(50, new LevelCurve(new[] { 1000 }, 5)), 1);
            run.Play(RunStep.RegularFight);
            run.AddCard(Mend);
            run.AddCard(Bolt);
            run.AddCard(Unique);
            return run;
        }

        private static CardDefinition Card(string id, int castTime, IEffect effect)
        {
            return new CardDefinition(id, castTime, new[] { effect });
        }
    }
}
