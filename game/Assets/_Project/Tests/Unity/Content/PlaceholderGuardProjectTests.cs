using Game.Unity.EditorTools.Content;
using Game.Unity.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Tests.Content
{
    /// <summary>
    /// Writes a shipped-looking asset outside the placeholder area that references a placeholder enemy, and checks
    /// the project scan reports it. The temporary folder is deleted after each test.
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
