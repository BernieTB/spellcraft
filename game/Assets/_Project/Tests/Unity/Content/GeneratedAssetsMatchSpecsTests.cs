using System.Collections.Generic;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Effects;
using Game.Unity.Cards;
using Game.Unity.Classes;
using Game.Unity.EditorTools.Content;
using Game.Unity.Enemies;
using Game.Unity.Runs;
using Game.Unity.Upgrades;
using NUnit.Framework;
using UnityEditor;

namespace Game.Unity.Tests.Content
{
    /// <summary>
    /// The balance lives in the generator specs (#125, ADR 0017); the committed assets must be what the generators would
    /// write, or the balance tests would play numbers the specs do not hold. Compares the numbers, not the YAML.
    /// </summary>
    public class GeneratedAssetsMatchSpecsTests
    {
        private static string Sig(int castTime, IEnumerable<int> effects, IEnumerable<int> modifiers) =>
            $"{castTime}|{string.Join(",", effects)}|{string.Join(",", modifiers)}";

        private static string Sig(CardDefinition card) =>
            Sig(card.CastTime, card.Effects.OfType<IAmountEffect>().Select(e => e.Amount), card.NeighbourModifiers.Select(m => m.Amount));

        private static string Sig(int castTime, int[] effects, int[] modifiers) => Sig(castTime, effects, (IEnumerable<int>)modifiers);

        [Test]
        public void ClassCards_AndHeroNumbers_MatchTheSpecs()
        {
            var definition = AssetDatabase.LoadAssetAtPath<ClassAsset>(MvpClassContentGenerator.ClassPath).ToDefinition();

            Assert.AreEqual(MvpClassSpecs.MaxHealth, definition.MaxHealth);
            Assert.AreEqual(MvpClassSpecs.StartingShield, definition.StartingShield);
            Assert.AreEqual(MvpClassSpecs.StartingLineCapacity, definition.StartingLineCapacity);
            CollectionAssert.AreEqual(MvpClassSpecs.StartingDeckIds, definition.StartingDeck.Select(c => c.Id).ToArray());

            var cards = definition.StartingDeck.Concat(definition.CardPool).GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First());
            foreach (var spec in MvpClassSpecs.StartingSpecs.Concat(MvpClassSpecs.PoolSpecs))
            {
                var card = cards[spec.Id];
                Assert.AreEqual(Sig(spec.CastTime, spec.Effects.Select(e => e.Amount), spec.Modifiers.Select(m => m.Amount)), Sig(card), spec.Id);
                Assert.AreEqual(spec.Stages.Length, card.Evolutions.Count, spec.Id);
                for (var i = 0; i < spec.Stages.Length; i++)
                {
                    var stage = spec.Stages[i];
                    var evolution = card.Evolutions[i];
                    Assert.AreEqual(stage.Casts, evolution.CastsRequired, spec.Id);
                    CollectionAssert.AreEqual(stage.Effects.Select(e => e.Amount), evolution.Effects.OfType<IAmountEffect>().Select(e => e.Amount), spec.Id);
                    CollectionAssert.AreEqual(stage.Modifiers.Select(m => m.Amount), evolution.NeighbourModifiers.Select(m => m.Amount), spec.Id);
                }
            }
        }

        [Test]
        public void BiomeCardsEnemiesAndRules_MatchTheSpecs()
        {
            foreach (var spec in PlaceholderBiomeSpecs.Cards)
            {
                var card = AssetDatabase.LoadAssetAtPath<CardAsset>(PlaceholderBiomeGenerator.AssetPath(spec.Id)).ToDefinition();
                Assert.AreEqual(Sig(spec.CastTime, spec.Effects.Select(e => e.Amount), spec.Modifiers.Select(m => m.Amount)), Sig(card), spec.Id);
            }

            foreach (var spec in PlaceholderBiomeSpecs.Enemies)
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyAsset>(PlaceholderBiomeGenerator.AssetPath(spec.Id)).ToDefinition();
                Assert.AreEqual(spec.MaxHealth, enemy.MaxHealth, spec.Id);
                Assert.AreEqual(spec.Shield, enemy.Shield, spec.Id);
                Assert.AreEqual(spec.Xp, enemy.XpReward, spec.Id);
                CollectionAssert.AreEqual(spec.CardIds, enemy.SpellLine.Select(c => c.Id).ToArray(), spec.Id);
            }

            var limit = AssetDatabase.LoadAssetAtPath<FightTimeLimitAsset>(PlaceholderBiomeGenerator.FightTimeLimitPath);
            Assert.AreEqual(PlaceholderBiomeSpecs.FightTimeLimit, limit.MaxTicks);
            var curve = AssetDatabase.LoadAssetAtPath<LevelCurveAsset>(PlaceholderBiomeGenerator.LevelCurvePath).ToDefinition();
            CollectionAssert.AreEqual(PlaceholderBiomeSpecs.LevelCosts, curve.LevelCosts);
            Assert.AreEqual(PlaceholderBiomeSpecs.CostIncreaseAfterList, curve.CostIncreaseAfterList);
        }

        [Test]
        public void PassivePool_MatchesTheSpecs()
        {
            var pool = AssetDatabase.LoadAssetAtPath<PassiveUpgradePoolAsset>(PassiveUpgradePoolGenerator.PoolPath).ToUpgrades();

            CollectionAssert.AreEqual(
                PassivePoolSpecs.All.Select(s => $"{s.Id}|{s.Kind}|{s.EffectKind}|{s.Amount}"),
                pool.Select(u => $"{u.Id}|{u.Kind}|{u.EffectKind}|{u.Amount}"));
        }
    }
}
