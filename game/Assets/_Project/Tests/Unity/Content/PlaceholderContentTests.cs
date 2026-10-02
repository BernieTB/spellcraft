using System.Collections.Generic;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;
using Game.Unity.Cards;
using Game.Unity.Editor.Content;
using NUnit.Framework;
using UnityEditor;

namespace Game.Unity.Tests.Content
{
    /// <summary>
    /// Loads the placeholder card set from disk and checks it goes through the runtime conversion.
    /// </summary>
    public class PlaceholderContentTests
    {
        private static List<CardAsset> LoadPlaceholderAssets()
        {
            return AssetDatabase.FindAssets("t:" + nameof(CardAsset), new[] { PlaceholderGuard.PlaceholderFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, System.StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<CardAsset>)
                .ToList();
        }

        private static List<CardDefinition> LoadPlaceholderDefinitions()
        {
            return LoadPlaceholderAssets().Select(asset => asset.ToDefinition()).ToList();
        }

        /// <summary>
        /// Builds a spell line holding every placeholder card, in id order. Capacity is test data.
        /// </summary>
        private static SpellLine<CardDefinition> BuildPlaceholderLine()
        {
            var definitions = LoadPlaceholderDefinitions();
            var line = new SpellLine<CardDefinition>(definitions.Count);
            foreach (var definition in definitions)
            {
                line.Add(definition);
            }

            return line;
        }

        [Test]
        public void PlaceholderSet_MatchesGenerator()
        {
            var ids = LoadPlaceholderAssets().Select(asset => asset.ToDefinition().Id);

            CollectionAssert.AreEqual(PlaceholderCardGenerator.CardIds, ids);
        }

        [Test]
        public void PlaceholderSet_EveryCardIsFlaggedAsPlaceholder()
        {
            var assets = LoadPlaceholderAssets();

            Assert.IsNotEmpty(assets);
            Assert.That(assets.Select(asset => asset.IsPlaceholder), Is.All.True);
        }

        [Test]
        public void PlaceholderSet_IdsAreTestIds()
        {
            var definitions = LoadPlaceholderDefinitions();

            Assert.That(definitions.Select(definition => definition.Id), Is.All.StartsWith("test_card_"));
        }

        [Test]
        public void PlaceholderSet_CoversEveryEffect()
        {
            var effectTypes = LoadPlaceholderDefinitions()
                .SelectMany(definition => definition.Effects)
                .Select(effect => effect.GetType())
                .ToList();

            CollectionAssert.IsSubsetOf(
                new[] { typeof(DealDamageEffect), typeof(HealEffect), typeof(GainShieldEffect) },
                effectTypes);
        }

        [Test]
        public void PlaceholderSet_HasACardWithSeveralEffects()
        {
            var definitions = LoadPlaceholderDefinitions();

            Assert.That(definitions.Any(definition => definition.Effects.Count > 1));
        }

        [Test]
        public void PlaceholderSet_HasVariedCastTimes()
        {
            var castTimes = LoadPlaceholderDefinitions().Select(definition => definition.CastTime).Distinct();

            Assert.Greater(castTimes.Count(), 1);
        }

        [Test]
        public void PlaceholderLine_HoldsEveryCardInOrder()
        {
            var line = BuildPlaceholderLine();

            CollectionAssert.AreEqual(
                PlaceholderCardGenerator.CardIds,
                line.Cards.Select(card => card.Id));
        }

        [Test]
        public void PlaceholderLine_RunsFight_EndsWithAWinner()
        {
            var result = RunPlaceholderFight();

            Assert.AreNotEqual(FightWinner.None, result.Winner);
            Assert.IsNotEmpty(result.Casts);
        }

        [Test]
        public void PlaceholderLine_RunsFightTwice_GivesIdenticalOutcome()
        {
            var first = RunPlaceholderFight();
            var second = RunPlaceholderFight();

            Assert.AreEqual(first.Winner, second.Winner);
            Assert.AreEqual(first.Ticks, second.Ticks);
            CollectionAssert.AreEqual(
                first.Casts.Select(cast => (cast.Tick, cast.CasterIndex, cast.Position, cast.Card.Id)),
                second.Casts.Select(cast => (cast.Tick, cast.CasterIndex, cast.Position, cast.Card.Id)));
        }

        /// <summary>
        /// The hero casts the full placeholder line against an enemy casting only the first placeholder card.
        /// All numbers are test data.
        /// </summary>
        private static FightResult RunPlaceholderFight()
        {
            var enemyLine = new SpellLine<CardDefinition>(1);
            enemyLine.Add(LoadPlaceholderDefinitions()[0]);

            var hero = new FightParticipant(new Combatant(30, 0), BuildPlaceholderLine());
            var enemy = new FightParticipant(new Combatant(20, 0), enemyLine);
            var fight = new Fight(hero, new[] { enemy }, 1000, new Pcg32Random(1));

            return fight.Run();
        }
    }
}
