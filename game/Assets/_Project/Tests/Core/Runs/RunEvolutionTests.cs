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

        // Cast time 3, evolves after one cast: lets a test swap it out of the line while it is casting.
        private static readonly CardDefinition SlowEvolver = new CardDefinition(
            "TestSlowEvolver",
            3,
            new IEffect[] { new DealDamageEffect(1) },
            new NeighbourModifier[0],
            new[] { new CardEvolution(1, new IEffect[] { new DealDamageEffect(9) }, new NeighbourModifier[0]) });

        private static readonly CardDefinition Smite = new CardDefinition("TestSmite", 1, new IEffect[] { new DealDamageEffect(50) });

        private static Run CreateRun(params CardDefinition[] deck) => CreateRun(4, EnemyHealth, deck);

        private static Run CreateRun(int capacity, int enemyHealth, params CardDefinition[] deck)
        {
            var heroClass = new ClassDefinition("TestClass", HeroHealth, 0, capacity, deck, new CardDefinition[0]);
            var enemy = new EnemyDefinition("TestEnemy", enemyHealth, 0, new[] { EnemyHit });
            var encounter = new EncounterDefinition("TestEncounter", new[] { enemy });
            var biome = new BiomeDefinition("TestBiome", new[] { encounter }, 0, encounter, encounter.Enemies[0]);
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

        // --- Fight sessions (RunFightSession) ---

        [Test]
        public void Session_StepByStep_EvolvesTheCardInTheFightAndCommitsItsCasts()
        {
            var run = CreateRun(4, 30, Evolving);
            var session = run.BeginFight(RunStep.RegularFight);

            session.Advance();
            Assert.AreEqual(0, session.HeroLine[0].Stage);
            session.Advance();
            // The second cast reached stage 1: the fight's card is evolved before the next cast.
            Assert.AreEqual(1, session.HeroLine[0].Stage);
            Assert.AreEqual(0, run.Line[0].Casts, "the run's instance is updated when the session completes");

            while (!session.IsOver)
            {
                session.Advance();
            }

            var report = session.Complete();

            Assert.IsTrue(report.HeroWon);
            Assert.AreEqual(2, EvolutionEvents(report).Count);
            Assert.AreEqual(run.Line[0].Casts, report.Log.HeroCardCasts[0]);
            Assert.AreEqual(2, run.Line[0].Stage);
        }

        [Test]
        public void Session_ReserveCardSwappedIn_EvolvesAndKeepsItsCountOnItsInstance()
        {
            var run = CreateRun(1, EnemyHealth, Filler);
            var evolving = run.AddCard(Evolving);
            Assert.AreSame(evolving, run.Reserve[0]);
            var filler = run.Line[0];

            var session = run.BeginFight(RunStep.RegularFight);
            session.SwapWithReserve(0, 0);
            while (!session.IsOver)
            {
                session.Advance();
            }

            session.Complete();

            // The evolving card played from the first tick: casts on ticks 1, 2 (evolves) and 3 (kills the enemy).
            Assert.AreSame(evolving, run.Line[0]);
            Assert.AreSame(filler, run.Reserve[0]);
            Assert.AreEqual(3, evolving.Casts);
            Assert.AreEqual(1, evolving.Stage);
            Assert.AreEqual(0, filler.Casts);
        }

        [Test]
        public void Session_CardSwappedToTheReserveWhileCasting_KeepsItsCastAndEvolvesThere()
        {
            var run = CreateRun(1, EnemyHealth, SlowEvolver);
            run.AddCard(Smite);
            var slow = run.Line[0];
            var smite = run.Reserve[0];

            var session = run.BeginFight(RunStep.RegularFight);
            session.Advance();
            // The slow card is mid-cast: it goes to the reserve but its cast still resolves on tick 3.
            session.SwapWithReserve(0, 0);
            while (!session.IsOver)
            {
                session.Advance();
            }

            var report = session.Complete();

            Assert.IsTrue(report.HeroWon);
            Assert.AreSame(smite, run.Line[0]);
            Assert.AreSame(slow, run.Reserve[0]);
            Assert.AreEqual(1, slow.Casts);
            Assert.AreEqual(1, slow.Stage);
            Assert.AreEqual(1, EvolutionEvents(report).Count);
            Assert.Greater(smite.Casts, 0);
        }

        [Test]
        public void Session_Cancel_KeepsTheLineChangesButNotTheCasts()
        {
            var run = CreateRun(1, 30, Filler);
            var evolving = run.AddCard(Evolving);
            var filler = run.Line[0];

            var session = run.BeginFight(RunStep.RegularFight);
            session.SwapWithReserve(0, 0);
            session.Advance();
            session.Advance();
            Assert.AreEqual(1, session.HeroLine[0].Stage);
            session.Cancel();

            Assert.AreSame(evolving, run.Line[0]);
            Assert.AreSame(filler, run.Reserve[0]);
            Assert.AreEqual(0, evolving.Casts);
            Assert.AreEqual(0, evolving.Stage);
            Assert.AreEqual(0, run.FightsPlayed);

            // Beginning again starts from the counts the instances had: the card has to evolve again.
            var again = run.BeginFight(RunStep.RegularFight);
            again.Advance();
            Assert.AreEqual(0, again.HeroLine[0].Stage);
            again.Advance();
            Assert.AreEqual(1, again.HeroLine[0].Stage);
        }

        [Test]
        public void Play_IsTheSameAsASessionPlayedToTheEnd()
        {
            var played = CreateRun(Evolving, Filler);
            var session = CreateRun(Evolving, Filler);
            var stepped = CreateRun(Evolving, Filler);

            var playedLogs = new List<string>();
            var sessionLogs = new List<string>();
            var steppedLogs = new List<string>();
            for (var fight = 0; fight < 3; fight++)
            {
                playedLogs.Add(played.Play(RunStep.RegularFight).Log.ToJson());

                var toEnd = session.BeginFight(RunStep.RegularFight);
                toEnd.RunToEnd();
                sessionLogs.Add(toEnd.Complete().Log.ToJson());

                var stepping = stepped.BeginFight(RunStep.RegularFight);
                while (!stepping.IsOver)
                {
                    stepping.Advance();
                }

                steppedLogs.Add(stepping.Complete().Log.ToJson());
            }

            Assert.AreEqual(playedLogs, sessionLogs);
            Assert.AreEqual(playedLogs, steppedLogs);
            foreach (var other in new[] { session, stepped })
            {
                Assert.AreEqual(played.Line[0].Casts, other.Line[0].Casts);
                Assert.AreEqual(played.Line[1].Casts, other.Line[1].Casts);
                Assert.AreEqual(played.Line[0].Stage, other.Line[0].Stage);
            }
        }

        [Test]
        public void BeginFight_CardsEvolvedInEarlierFights_StartAtTheirStageWithPassivesApplied()
        {
            var run = CreateRun(Evolving);
            run.Play(RunStep.RegularFight);
            run.Play(RunStep.RegularFight);
            Assert.AreEqual(2, run.Line[0].Stage);

            run.TakeUpgrade(new Game.Core.Upgrades.PassiveUpgrade(
                "test_upgrade_damage", Game.Core.Upgrades.PassiveUpgradeKind.EffectAmount, BonusKind.Damage, 3));
            var session = run.BeginFight(RunStep.RegularFight);

            // Stage 2 deals 20, plus the passive: the upgrade reaches every stage of the card.
            Assert.AreEqual(2, session.HeroLine[0].Stage);
            Assert.AreEqual(23, ((IAmountEffect)session.HeroLine[0].Effects[0]).Amount);
            session.Cancel();
        }
    }
}
