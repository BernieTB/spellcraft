using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Classes;
using Game.Core.Runs;
using Game.Unity.Cards;
using Game.Unity.Classes;
using Game.Unity.EditorTools.Content;
using Game.Unity.Enemies;
using Game.Unity.Runs;
using Game.Unity.Tests.Balance;
using NUnit.Framework;
using UnityEditor;

namespace Game.Unity.Tests.Content
{
    /// <summary>
    /// Loads the placeholder biome (#72, ADR 0009, 0010, 0011) from disk, checks its shape and plays it headless
    /// through the run model with the MVP class. The difficulty targets are checked in <c>BalanceTargetsTests</c>.
    /// </summary>
    public class PlaceholderBiomeContentTests
    {
        private const int SeedCount = 25;

        private static T Load<T>(string id) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(PlaceholderBiomeGenerator.AssetPath(id));
            Assert.IsNotNull(asset, PlaceholderBiomeGenerator.AssetPath(id));
            return asset;
        }

        private static List<T> LoadAll<T>() where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { PlaceholderBiomeGenerator.Folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .ToList();
        }

        private static BiomeAsset LoadBiome() => Load<BiomeAsset>(PlaceholderBiomeSpecs.BiomeId);

        private static ClassDefinition LoadClass()
        {
            return AssetDatabase.LoadAssetAtPath<ClassAsset>(MvpClassContentGenerator.ClassPath).ToDefinition();
        }

        private static RunRules LoadRules()
        {
            var limit = Load<FightTimeLimitAsset>(PlaceholderBiomeGenerator.FightTimeLimitId);
            return new RunRules(limit.MaxTicks, Load<LevelCurveAsset>(PlaceholderBiomeGenerator.LevelCurveId).ToDefinition());
        }

        // A simple player: opens each unlocked secret room before anything else, farms regular fights until both
        // rooms are cleared and the professor is available, then faces the professor. Never edits the line.
        private static Run PlayToTheEnd(ulong seed, List<RunFightReport> reports)
        {
            var run = new Run(LoadClass(), LoadBiome().ToDefinition(), LoadRules(), seed);
            while (run.IsInProgress)
            {
                var step = run.AvailableSteps.FirstOrDefault(
                    candidate => candidate.Kind == RunStepKind.SecretRoom
                        && !run.SecretRooms.Single(room => room.Definition.Id == candidate.SecretRoomId).IsCleared);
                if (step == null)
                {
                    step = run.SecretRooms.All(room => room.IsCleared) && run.IsProfessorAvailable
                        ? RunStep.Professor
                        : RunStep.RegularFight;
                }

                reports.Add(run.Play(step));
                Assert.Less(run.FightsPlayed, 200, "The run does not end.");
            }

            return run;
        }

        [Test]
        public void Folder_HoldsExactlyTheGeneratedAssets()
        {
            CollectionAssert.AreEquivalent(PlaceholderBiomeGenerator.CardIds, LoadAll<CardAsset>().Select(card => card.ToDefinition().Id));
            CollectionAssert.AreEquivalent(PlaceholderBiomeGenerator.EnemyIds, LoadAll<EnemyAsset>().Select(enemy => enemy.Id));
            CollectionAssert.AreEquivalent(PlaceholderBiomeGenerator.EncounterIds, LoadAll<EncounterAsset>().Select(encounter => encounter.Id));
            CollectionAssert.AreEquivalent(PlaceholderBiomeGenerator.RoomIds, LoadAll<SecretRoomAsset>().Select(room => room.Id));
            Assert.AreEqual(1, LoadAll<BiomeAsset>().Count);
            Assert.AreEqual(1, LoadAll<LevelCurveAsset>().Count);
            Assert.AreEqual(1, LoadAll<FightTimeLimitAsset>().Count);
        }

        [Test]
        public void Assets_AreSliceContentNotFlaggedAsPlaceholderWithWorkingNames()
        {
            foreach (var card in LoadAll<CardAsset>())
            {
                Assert.IsFalse(card.IsPlaceholder, card.name);
                StringAssert.IsMatch("^CARD_(ENEMY|UNIQUE)_[0-9]{2}$", card.ToDefinition().Id);
            }

            foreach (var enemy in LoadAll<EnemyAsset>())
            {
                Assert.IsFalse(enemy.IsPlaceholder, enemy.name);
                StringAssert.StartsWith("ENEMY_", enemy.Id);
            }

            foreach (var encounter in LoadAll<EncounterAsset>())
            {
                Assert.IsFalse(encounter.IsPlaceholder, encounter.name);
                StringAssert.StartsWith("ENCOUNTER_", encounter.Id);
            }

            Assert.IsFalse(LoadBiome().IsPlaceholder);
            Assert.IsFalse(Load<LevelCurveAsset>(PlaceholderBiomeGenerator.LevelCurveId).IsPlaceholder);
            Assert.IsFalse(Load<FightTimeLimitAsset>(PlaceholderBiomeGenerator.FightTimeLimitId).IsPlaceholder);
            Assert.That(LoadAll<SecretRoomAsset>(), Has.None.Matches<SecretRoomAsset>(room => room.IsPlaceholder));
        }

        [Test]
        public void RegularMonsters_HaveASingleAttackCard()
        {
            var regulars = LoadAll<EnemyAsset>().Where(enemy => enemy.Rank == EnemyRank.Regular).ToList();

            Assert.IsNotEmpty(regulars);
            foreach (var enemy in regulars)
            {
                var line = enemy.ToDefinition().SpellLine;
                Assert.AreEqual(1, line.Count, enemy.Id);
                Assert.That(line[0].Effects, Has.All.InstanceOf<Game.Core.Effects.DealDamageEffect>(), enemy.Id);
            }
        }

        [Test]
        public void Bosses_HaveWorkedSpellLines()
        {
            var enemies = LoadAll<EnemyAsset>();

            var miniBosses = enemies.Where(enemy => enemy.Rank == EnemyRank.MiniBoss).ToList();
            var professors = enemies.Where(enemy => enemy.Rank == EnemyRank.Professor).ToList();
            Assert.AreEqual(2, miniBosses.Count);
            Assert.AreEqual(1, professors.Count);
            Assert.That(miniBosses, Has.All.Matches<EnemyAsset>(enemy => enemy.ToDefinition().SpellLine.Count >= 3));
            Assert.That(professors[0].ToDefinition().SpellLine.Count, Is.GreaterThan(miniBosses.Max(enemy => enemy.ToDefinition().SpellLine.Count)));
        }

        [Test]
        public void MiniBossXp_IsAboveEveryRegularEncounter()
        {
            var biome = LoadBiome().ToDefinition();
            var bestRegular = biome.RegularEncounters.Max(encounter => encounter.Enemies.Sum(enemy => enemy.XpReward));

            foreach (var room in biome.SecretRooms)
            {
                Assert.Greater(room.MiniBossEncounter.Enemies.Sum(enemy => enemy.XpReward), bestRegular, room.Id);
            }
        }

        [Test]
        public void Biome_ConvertsWithTheDataOfADR0009And0010()
        {
            var biome = LoadBiome().ToDefinition();

            Assert.AreEqual(8, biome.MinimumRegularFights);
            Assert.AreEqual(2, biome.SecretRooms.Count);
            Assert.AreEqual("ENEMY_PROFESSOR_01", biome.Professor.Id);
            Assert.That(biome.SecretRooms.Select(room => room.BonusLineSlots), Has.All.EqualTo(1));
            Assert.That(biome.SecretRooms.Select(room => room.MiniBossEncounter.Id).Distinct().Count(), Is.EqualTo(2));
            Assert.That(biome.SecretRooms.Select(room => room.UniqueCard.Id).Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void Objectives_AskForMonstersThatAreCommonInThePool()
        {
            var biome = LoadBiome().ToDefinition();

            foreach (var room in biome.SecretRooms)
            {
                var share = biome.RegularEncounters.Count(
                        encounter => encounter.Enemies.Any(enemy => enemy.Id == room.Objective.EnemyId))
                    / (double)biome.RegularEncounters.Count;
                Assert.GreaterOrEqual(share, 0.3, room.Id);
                Assert.GreaterOrEqual(room.Objective.Count, 2, room.Id);
            }

            Assert.AreEqual(2, biome.SecretRooms.Select(room => room.Objective.EnemyId).Distinct().Count());
        }

        [Test]
        public void MiniBosses_RevealDifferentCardsOfTheProfessor()
        {
            var biome = LoadBiome().ToDefinition();

            var revealed = biome.SecretRooms.SelectMany(room => room.Revelation.CardPositions).ToList();
            CollectionAssert.AllItemsAreUnique(revealed);
            Assert.That(biome.SecretRooms.Select(room => room.Revelation.CardPositions.Count), Has.All.GreaterThan(0));
            Assert.Less(revealed.Count, biome.Professor.SpellLine.Count, "The professor keeps a hidden card.");
        }

        [Test]
        public void FightTimeLimit_IsOnlyASafetyNet()
        {
            var rules = LoadRules();
            var reports = new List<RunFightReport>();

            PlayToTheEnd(1, reports);

            Assert.That(rules.FightTimeLimit, Is.GreaterThanOrEqualTo(2 * reports.Max(report => report.Log.Ticks)));
        }

        [Test]
        public void LevelCurve_GivesAboutFiveLevelUpsOnTheShortestPath()
        {
            var levels = new List<int>();
            var bot = new BalanceBot(AssetBalanceContent.Load(), BotKind.Intermediate, StepPolicy.ShortPath);
            for (ulong seed = 1; seed <= SeedCount; seed++)
            {
                var run = new Run(LoadClass(), LoadBiome().ToDefinition(), LoadRules(), seed);
                for (var i = 0; i < run.Biome.MinimumRegularFights; i++)
                {
                    // The regular fights of a disordered line can be lost (#125): order it first, as a player would.
                    bot.ArrangeLine(run, RunStep.RegularFight);
                    run.Play(RunStep.RegularFight);
                }

                levels.Add(run.Level - 1);
            }

            Assert.That(levels, Has.All.InRange(4, 6), string.Join(",", levels));
            Assert.AreEqual(5, (int)Math.Round(levels.Average()), "level-ups on average");
        }

        [Test]
        public void WholeBiome_PlayedWithoutEditingTheLine_EndsInAVictoryOrADefeatButAlwaysEnds()
        {
            var outcomes = new List<RunOutcome>();
            for (ulong seed = 1; seed <= SeedCount; seed++)
            {
                var reports = new List<RunFightReport>();

                var run = PlayToTheEnd(seed, reports);

                Assert.AreNotEqual(RunOutcome.InProgress, run.Outcome, $"seed {seed}");
                outcomes.Add(run.Outcome);
            }

            // Balance (#125): a player who never edits the line does not walk through the biome.
            Assert.That(outcomes.Count(outcome => outcome == RunOutcome.Victory), Is.LessThanOrEqualTo(SeedCount / 4));
        }

        [Test]
        public void WholeBiome_SameSeed_GivesTheSameRun()
        {
            var first = PlayToTheEnd(7, new List<RunFightReport>());
            var second = PlayToTheEnd(7, new List<RunFightReport>());

            Assert.AreEqual(first.FightsPlayed, second.FightsPlayed);
            Assert.AreEqual(first.TotalXp, second.TotalXp);
        }
    }
}
