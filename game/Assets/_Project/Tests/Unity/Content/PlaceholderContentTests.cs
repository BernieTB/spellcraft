using System.Collections.Generic;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Effects;
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

        // TODO(#12): add PlaceholderLine_RunsFight here once the combat loop exists: run a fight between a
        // combatant casting BuildPlaceholderLine() and a test enemy, and assert it ends deterministically.
    }
}
