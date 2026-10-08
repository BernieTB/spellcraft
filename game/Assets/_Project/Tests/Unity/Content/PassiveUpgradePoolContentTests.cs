using System.Collections.Generic;
using System.Linq;
using Game.Core.Effects;
using Game.Core.Runs;
using Game.Core.Upgrades;
using Game.Unity.Classes;
using Game.Unity.EditorTools.Content;
using Game.Unity.Runs;
using Game.Unity.Upgrades;
using NUnit.Framework;
using UnityEditor;

namespace Game.Unity.Tests.Content
{
    /// <summary>
    /// Loads the MVP passive pool (#119, ADR 0012) from disk, checks its shape and plays the placeholder biome
    /// headless, taking every level-up offer from it. Numbers are first-pass values, not balance.
    /// </summary>
    public class PassiveUpgradePoolContentTests
    {
        private const int SeedCount = 25;

        private static PassiveUpgradePoolAsset LoadPool()
        {
            var pool = AssetDatabase.LoadAssetAtPath<PassiveUpgradePoolAsset>(PassiveUpgradePoolGenerator.PoolPath);
            Assert.IsNotNull(pool, PassiveUpgradePoolGenerator.PoolPath);
            return pool;
        }

        [Test]
        public void Pool_HoldsTheGeneratedPassivesWithUniqueIdsAndIsNotPlaceholder()
        {
            var pool = LoadPool();

            CollectionAssert.AreEqual(PassiveUpgradePoolGenerator.PassiveIds, pool.ToUpgrades().Select(upgrade => upgrade.Id));
            CollectionAssert.AllItemsAreUnique(PassiveUpgradePoolGenerator.PassiveIds);
            Assert.AreEqual(10, pool.Upgrades.Count);
            Assert.That(pool.Upgrades, Has.None.Matches<PassiveUpgradeAsset>(asset => asset.IsPlaceholder));
            Assert.AreEqual(PassiveUpgradePoolGenerator.PoolId, pool.Id);
        }

        [Test]
        public void Pool_CoversTheThreeKindsOfPassive()
        {
            var upgrades = LoadPool().ToUpgrades();

            var kinds = upgrades.Select(upgrade => upgrade.Kind).ToList();
            Assert.That(kinds, Does.Contain(PassiveUpgradeKind.MaxHealth));
            Assert.That(kinds, Does.Contain(PassiveUpgradeKind.StartingShield));
            Assert.That(kinds, Does.Contain(PassiveUpgradeKind.NeighbourBonus));
            var effectKinds = upgrades.Where(upgrade => upgrade.Kind == PassiveUpgradeKind.EffectAmount)
                .Select(upgrade => upgrade.EffectKind)
                .Distinct()
                .ToList();
            CollectionAssert.AreEquivalent(new[] { BonusKind.Damage, BonusKind.Heal, BonusKind.Shield }, effectKinds);
            Assert.That(upgrades.Select(upgrade => upgrade.Amount), Has.All.GreaterThan(0));
        }

        [Test]
        public void PlaceholderBiome_PlayedTakingOffersFromThePool_EndsInVictory()
        {
            var classDefinition = AssetDatabase.LoadAssetAtPath<ClassAsset>(MvpClassContentGenerator.ClassPath).ToDefinition();
            var biome = AssetDatabase.LoadAssetAtPath<BiomeAsset>(PlaceholderBiomeGenerator.AssetPath(PlaceholderBiomeSpecs.BiomeId))
                .ToDefinition();
            var limit = AssetDatabase.LoadAssetAtPath<FightTimeLimitAsset>(
                PlaceholderBiomeGenerator.AssetPath(PlaceholderBiomeGenerator.FightTimeLimitId));
            var curve = AssetDatabase.LoadAssetAtPath<LevelCurveAsset>(
                PlaceholderBiomeGenerator.AssetPath(PlaceholderBiomeGenerator.LevelCurveId));
            var rules = new RunRules(limit.MaxTicks, curve.ToDefinition());
            var upgrades = LoadPool().ToUpgrades();
            var poolIds = new HashSet<string>(upgrades.Select(upgrade => upgrade.Id));

            for (ulong seed = 1; seed <= SeedCount; seed++)
            {
                var run = new Run(classDefinition, biome, rules, seed);
                var taken = 0;
                while (run.IsInProgress)
                {
                    while (run.HasPendingChoice)
                    {
                        var offer = run.GetLevelUpOffer(upgrades);
                        Assert.AreEqual(3, offer.Packages.Count, $"seed {seed}");
                        Assert.That(offer.Packages.Select(package => package.Passive.Id), Has.All.Matches<string>(poolIds.Contains));
                        run.TakeLevelUpPackage(0);
                        taken++;
                    }

                    var step = run.AvailableSteps.FirstOrDefault(
                        candidate => candidate.Kind == RunStepKind.SecretRoom
                            && !run.SecretRooms.Single(room => room.Definition.Id == candidate.SecretRoomId).IsCleared);
                    if (step == null)
                    {
                        step = run.SecretRooms.All(room => room.IsCleared) && run.IsProfessorAvailable
                            ? RunStep.Professor
                            : RunStep.RegularFight;
                    }

                    run.Play(step);
                    Assert.Less(run.FightsPlayed, 200, $"seed {seed}: the run does not end.");
                }

                Assert.AreEqual(RunOutcome.Victory, run.Outcome, $"seed {seed}");
                Assert.Greater(taken, 0, $"seed {seed}: no level-up was taken.");
                Assert.AreEqual(taken, run.Upgrades.Upgrades.Count, $"seed {seed}");
            }
        }
    }
}
