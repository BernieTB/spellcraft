using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Meta;
using Game.Core.Runs;
using Game.Unity.UI.RunScreen;

namespace Game.Unity.EditorTools.UI
{
    /// <summary>
    /// Small runs built by code, so the screen preview (<see cref="ScreenPreviewWindow"/>) can show the run screen
    /// (#73) in each of its states without playing a game: choosing the next step, a regular fight, a level-up
    /// waiting and the result of a fight.
    /// </summary>
    /// <remarks>
    /// Demo data for looking at screens, not game content: ids are <c>demo_*</c> and the numbers are chosen only to
    /// make each state last long enough to look at. Nothing here reaches a player build (editor assembly).
    /// </remarks>
    public static class ScreenPreviewRuns
    {
        private const ulong Seed = 7;
        private const string Slime = "demo_slime";

        /// <summary>A new run: the regular fight is available, the professor is not yet.</summary>
        public static RunScreenController ChoosingStep()
        {
            return new RunScreenController(NewRun());
        }

        /// <summary>A regular fight in progress, a few ticks in, with a card in the reserve to swap in.</summary>
        public static RunScreenController Fighting()
        {
            var controller = new RunScreenController(NewRun());
            controller.StartFight(RunStep.RegularFight);
            for (var i = 0; i < 6; i++)
            {
                controller.StepOneTick();
            }

            return controller;
        }

        /// <summary>A fight just won that gave a level: the choice of the next step is blocked by the level-up.</summary>
        public static RunScreenController LevelUpPending()
        {
            var run = NewRun();
            run.Play(RunStep.RegularFight);
            return new RunScreenController(run);
        }

        /// <summary>The result screen of a fight that was just won.</summary>
        public static RunScreenController FightResult()
        {
            var controller = new RunScreenController(NewRun());
            controller.StartFight(RunStep.RegularFight);
            while (controller.Phase == RunScreenPhase.Fighting)
            {
                controller.StepOneTick();
            }

            return controller;
        }

        /// <summary>A demo run with a full line of three cards, a card in the reserve and one secret room.</summary>
        public static Run NewRun()
        {
            var boost = new CardDefinition(
                "demo_boost",
                2,
                new IEffect[0],
                new[] { new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, 3) });
            var strike = new CardDefinition("demo_strike", 3, new IEffect[] { new DealDamageEffect(4) });
            var ward = new CardDefinition("demo_ward", 2, new IEffect[] { new GainShieldEffect(3) });
            var bolt = new CardDefinition("demo_bolt", 4, new IEffect[] { new DealDamageEffect(8) });
            var hit = new CardDefinition("demo_hit", 5, new IEffect[] { new DealDamageEffect(2) });
            var crush = new CardDefinition("demo_crush", 5, new IEffect[] { new DealDamageEffect(3) });

            var slime = new EnemyDefinition(Slime, 24, 0, new[] { hit }, 10);
            var brute = new EnemyDefinition("demo_brute", 30, 0, new[] { crush }, 10);
            var miniBoss = new EnemyDefinition("demo_mini_boss", 40, 0, new[] { crush }, 25);
            var professor = new EnemyDefinition("demo_professor", 60, 0, new[] { crush }, 50);

            var heroClass = new ClassDefinition("demo_class", 40, 0, 3, new[] { boost, strike, ward }, new[] { bolt });
            var room = new SecretRoomDefinition(
                "demo_room",
                new ObjectiveDefinition(Slime, 2),
                new EncounterDefinition("demo_mini_boss_encounter", new[] { miniBoss }),
                1,
                bolt,
                new ProfessorRevelation(true, false, new[] { 0 }));
            var biome = new BiomeDefinition(
                "demo_biome",
                new List<EncounterDefinition>
                {
                    new EncounterDefinition("demo_slime_encounter", new[] { slime }),
                    new EncounterDefinition("demo_brute_encounter", new[] { brute }),
                },
                2,
                new EncounterDefinition("demo_professor_encounter", new[] { professor }),
                professor,
                new[] { room });

            var run = new Run(heroClass, biome, new RunRules(200, new LevelCurve(new[] { 10, 20 }, 5)), Seed);
            run.AddCard(bolt);
            return run;
        }
    }
}
