using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Combat.Log;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Runs;
using NUnit.Framework;

namespace Game.Core.Tests.Runs
{
    /// <summary>
    /// Card evolution across the fights of a run (ADR 0013, issue #79): each card copy keeps its own casts.
    /// </summary>
    public class RunEvolutionTests
    {
        // Placeholder ids and test values: not game content or balance numbers.
        private const int HeroHealth = 20;
        private const int TimeLimit = 50;
        private const int EnemyHealth = 3;

        // Deals 1; 5 after 2 casts; 20 after 4 casts. Cast time 1.
        private static readonly CardDefinition Evolving = new CardDefinition(
            "TestEvolving",
            1,
            new IEffect[] { new DealDamageEffect(1) },
            new NeighbourModifier[0],
            new[]
            {
                new CardEvolution(2, new IEffect[] { new DealDamageEffect(5) }, new NeighbourModifier[0]),
                new CardEvolution(4, new IEffect[] { new DealDamageEffect(20) }, new NeighbourModifier[0]),
            });

        private static readonly CardDefinition Filler = new CardDefinition("TestFiller", 1, new IEffect[0]);
        private static readonly CardDefinition EnemyHit = new CardDefinition("TestEnemyHit", 1, new IEffect[] { new DealDamageEffect(1) });

        private static Run CreateRun(params CardDefinition[] deck)
        {
            var heroClass = new ClassDefinition("TestClass", HeroHealth, 0, 4, deck, new CardDefinition[0]);
            var enemy = new EnemyDefinition("TestEnemy", EnemyHealth, 0, new[] { EnemyHit });
            var encounter = new EncounterDefinition("TestEncounter", new[] { enemy });
            var biome = new BiomeDefinition("TestBiome", new[] { encounter }, 0, encounter);
            return new Run(heroClass, biome, new RunRules(TimeLimit, new LevelCurve(new[] { 10 }, 5)), 1);
        }

        private static List<CombatEvent> EvolutionEvents(RunFightReport report)
        {
            var events = new List<CombatEvent>();
            foreach (var e in report.Log.Events)
            {
                if (e.Kind == CombatEventKind.Evolved)
                {
                    events.Add(e);
                }
            }

            return events;
        }

        [Test]
        public void NewInstance_HasNoCastsAndIsAtStageZero()
        {
            var run = CreateRun(Evolving);

            Assert.AreEqual(0, run.Line[0].Casts);
            Assert.AreEqual(0, run.Line[0].Stage);
            Assert.AreSame(Evolving, run.Line[0].Definition);
            Assert.AreSame(Evolving, run.Line[0].CurrentDefinition);
        }

        [Test]
        public void Play_CountsTheCastsOfEveryCardCopyOfTheLine()
        {
            var run = CreateRun(Evolving, Filler);

            run.Play(RunStep.RegularFight);

            // Casts on ticks 1 (evolving), 2 (filler), 3 (evolving)... until the enemy dies on tick 5.
            Assert.AreEqual(3, run.Line[0].Casts);
            Assert.AreEqual(2, run.Line[1].Casts);
        }

        [Test]
        public void Play_CastsAddUpAcrossFights_AndTheStageFollows()
        {
            var run = CreateRun(Evolving);

            var first = run.Play(RunStep.RegularFight);
            Assert.AreEqual(3, run.Line[0].Casts);
            Assert.AreEqual(1, run.Line[0].Stage);
            Assert.AreEqual(1, EvolutionEvents(first).Count);

            var second = run.Play(RunStep.RegularFight);
            Assert.AreEqual(4, run.Line[0].Casts);
            Assert.AreEqual(2, run.Line[0].Stage);
            Assert.AreEqual(1, EvolutionEvents(second).Count);
            Assert.AreEqual(2, EvolutionEvents(second)[0].EvolutionStage);
        }

        [Test]
        public void Play_NextFight_StartsWithTheEvolvedCard()
        {
            var run = CreateRun(Evolving);
            run.Play(RunStep.RegularFight);

            var report = run.Play(RunStep.RegularFight);

            // The card is at stage 1 when the second fight starts: its first cast deals 5 and kills the enemy
            // (3 health) on tick 1. At stage 0 it would deal 1 per tick.
            Assert.IsTrue(report.HeroWon);
            Assert.AreEqual(1, report.Log.Ticks);
            Assert.AreSame(Evolving.AtStage(2), run.Line[0].CurrentDefinition);
        }

        [Test]
        public void Play_TwoCopiesOfTheSameCard_CountAndEvolveSeparately()
        {
            var run = CreateRun(Evolving, Evolving);

            var report = run.Play(RunStep.RegularFight);

            // Casts on tick 1 (first copy), 2 (second copy) and 3 (first copy again, which evolves and kills the
            // enemy): the copies share a definition but not their casts.
            Assert.AreEqual(2, run.Line[0].Casts);
            Assert.AreEqual(1, run.Line[1].Casts);
            Assert.AreEqual(1, run.Line[0].Stage);
            Assert.AreEqual(0, run.Line[1].Stage);
            Assert.AreNotSame(run.Line[0], run.Line[1]);
            Assert.AreEqual(1, EvolutionEvents(report).Count);
            Assert.AreEqual(0, EvolutionEvents(report)[0].Position);
        }

        [Test]
        public void Reserve_KeepsTheCastsAndTheStageOfACard()
        {
            var run = CreateRun(Evolving, Filler);
            run.Play(RunStep.RegularFight);

            run.MoveToReserve(0);

            Assert.AreEqual(3, run.Reserve[0].Casts);
            Assert.AreEqual(1, run.Reserve[0].Stage);

            run.MoveFromReserve(0);
            var report = run.Play(RunStep.RegularFight);

            // Back in the line (after the filler), the card casts at stage 1 on tick 2 and kills the enemy. At stage 0
            // it would need three casts.
            Assert.IsTrue(report.HeroWon);
            Assert.AreEqual(2, report.Log.Ticks);
            Assert.AreEqual(4, run.Line[1].Casts);
            Assert.AreEqual(2, run.Line[1].Stage);
        }

        [Test]
        public void Play_CardWithoutEvolutions_JustCounts()
        {
            var run = CreateRun(Filler);

            run.Play(RunStep.RegularFight);

            Assert.Greater(run.Line[0].Casts, 0);
            Assert.AreEqual(0, run.Line[0].Stage);
            Assert.AreSame(Filler, run.Line[0].CurrentDefinition);
        }

        [Test]
        public void Play_SameSeedAndSteps_GiveTheSameRun()
        {
            var first = CreateRun(Evolving, Filler);
            var second = CreateRun(Evolving, Filler);
            var logs = new List<string>();

            foreach (var run in new[] { first, second })
            {
                run.Play(RunStep.RegularFight);
                logs.Add(run.Play(RunStep.RegularFight).Log.ToText());
            }

            Assert.AreEqual(logs[0], logs[1]);
            Assert.AreEqual(first.Line[0].Casts, second.Line[0].Casts);
            Assert.AreEqual(first.Line[1].Casts, second.Line[1].Casts);
        }
    }
}
