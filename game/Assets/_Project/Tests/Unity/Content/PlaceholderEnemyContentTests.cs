using System.Collections.Generic;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Randomness;
using Game.Core.SpellLines;
using Game.Unity.Cards;
using Game.Unity.EditorTools.Content;
using Game.Unity.Enemies;
using NUnit.Framework;
using UnityEditor;

namespace Game.Unity.Tests.Content
{
    /// <summary>
    /// Loads the placeholder enemies and encounters from disk and checks they go through the runtime conversion.
    /// </summary>
    public class PlaceholderEnemyContentTests
    {
        private static List<T> Load<T>() where T : UnityEngine.Object
        {
            return AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { PlaceholderGuard.PlaceholderFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, System.StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .ToList();
        }

        [Test]
        public void PlaceholderEnemies_MatchGenerator()
        {
            var ids = Load<EnemyAsset>().Select(enemy => enemy.Id).OrderBy(id => id, System.StringComparer.Ordinal);

            CollectionAssert.AreEqual(PlaceholderEnemyGenerator.EnemyIds.OrderBy(id => id, System.StringComparer.Ordinal), ids);
        }

        [Test]
        public void PlaceholderEncounters_MatchGenerator()
        {
            var ids = Load<EncounterAsset>().Select(encounter => encounter.Id);

            CollectionAssert.AreEqual(PlaceholderEnemyGenerator.EncounterIds, ids);
        }

        [Test]
        public void PlaceholderEnemiesAndEncounters_AreFlaggedAsPlaceholder()
        {
            var flags = Load<EnemyAsset>().Select(enemy => enemy.IsPlaceholder)
                .Concat(Load<EncounterAsset>().Select(encounter => encounter.IsPlaceholder))
                .ToList();

            Assert.IsNotEmpty(flags);
            Assert.That(flags, Is.All.True);
        }

        [Test]
        public void PlaceholderEnemies_CoverEveryRank()
        {
            var ranks = Load<EnemyAsset>().Select(enemy => enemy.Rank).Distinct();

            CollectionAssert.AreEquivalent(new[] { EnemyRank.Regular, EnemyRank.MiniBoss, EnemyRank.Professor }, ranks);
        }

        [Test]
        public void PlaceholderEncounters_HaveOneWithSeveralEnemies()
        {
            Assert.That(Load<EncounterAsset>().Any(encounter => encounter.Enemies.Count > 1));
        }

        [Test]
        public void PlaceholderEncounters_EachRunsAFightToTheEnd()
        {
            foreach (var encounter in Load<EncounterAsset>())
            {
                var fight = new Fight(CreateHero(), encounter.ToParticipants(), 10000, new Pcg32Random(1));

                var result = fight.Run();

                Assert.AreNotEqual(FightWinner.None, result.Winner, encounter.Id);
            }
        }

        /// <summary>A hero casting the first placeholder card. Numbers are test data.</summary>
        private static FightParticipant CreateHero()
        {
            var card = AssetDatabase.LoadAssetAtPath<CardAsset>(
                PlaceholderCardGenerator.AssetPath(PlaceholderCardGenerator.CardIds[0]));
            var line = new SpellLine<CardDefinition>(1);
            line.Add(card.ToDefinition());
            return new FightParticipant(new Combatant(30, 0), line);
        }
    }
}
