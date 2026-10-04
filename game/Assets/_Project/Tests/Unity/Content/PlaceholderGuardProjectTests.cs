using Game.Unity.Cards;
using Game.Unity.Classes;
using Game.Unity.EditorTools.Content;
using Game.Unity.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Tests.Content
{
    /// <summary>
    /// Writes shipped-looking assets outside the placeholder area that reference placeholder content, and checks
    /// the project scan reports them. The temporary folder is deleted after each test.
    /// </summary>
    public class PlaceholderGuardProjectTests
    {
        private const string TempFolder = "Assets/_Project/Unity/Content/PlaceholderGuardTestTemp";

        [SetUp]
        public void SetUp()
        {
            DeleteTempFolder();
            AssetDatabase.CreateFolder("Assets/_Project/Unity/Content", "PlaceholderGuardTestTemp");
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTempFolder();
        }

        [Test]
        public void FindProjectViolations_EncounterOutsideAreaReferencingPlaceholderEnemy_ReportsIt()
        {
            var enemyPath = PlaceholderEnemyGenerator.AssetPath(PlaceholderEnemyGenerator.EnemyIds[0]);
            var encounterPath = $"{TempFolder}/shipped_encounter.asset";
            CreateEncounter(encounterPath, AssetDatabase.LoadAssetAtPath<EnemyAsset>(enemyPath));

            var violations = PlaceholderGuard.FindProjectViolations();

            CollectionAssert.Contains(violations, $"{encounterPath} -> {enemyPath}");
        }

        [Test]
        public void FindProjectViolations_ClassOutsideAreaReferencingPlaceholderCard_ReportsIt()
        {
            var cardPath = PlaceholderCardGenerator.AssetPath(PlaceholderCardGenerator.CardIds[0]);
            var classPath = $"{TempFolder}/shipped_class.asset";
            CreateClass(classPath, AssetDatabase.LoadAssetAtPath<CardAsset>(cardPath));

            var violations = PlaceholderGuard.FindProjectViolations();

            CollectionAssert.Contains(violations, $"{classPath} -> {cardPath}");
        }

        private static void CreateClass(string path, CardAsset card)
        {
            Assert.IsNotNull(card, "Placeholder card missing; run the placeholder card generator.");

            var classAsset = ScriptableObject.CreateInstance<ClassAsset>();
            AssetDatabase.CreateAsset(classAsset, path);
            var serialized = new SerializedObject(classAsset);
            serialized.FindProperty("_id").stringValue = "shipped_class";
            var deck = serialized.FindProperty("_startingDeck");
            deck.arraySize = 1;
            deck.GetArrayElementAtIndex(0).objectReferenceValue = card;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static void CreateEncounter(string path, EnemyAsset enemy)
        {
            Assert.IsNotNull(enemy, "Placeholder enemy missing; run the placeholder enemy generator.");

            var encounter = ScriptableObject.CreateInstance<EncounterAsset>();
            AssetDatabase.CreateAsset(encounter, path);
            var serialized = new SerializedObject(encounter);
            serialized.FindProperty("_id").stringValue = "shipped_encounter";
            var enemies = serialized.FindProperty("_enemies");
            enemies.arraySize = 1;
            enemies.GetArrayElementAtIndex(0).objectReferenceValue = enemy;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        // Deletes the folder and its .meta file.
        private static void DeleteTempFolder()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
            }
        }
    }
}
