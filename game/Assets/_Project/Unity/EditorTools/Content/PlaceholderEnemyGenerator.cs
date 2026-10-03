using System;
using System.Collections.Generic;
using System.Linq;
using Game.Unity.Cards;
using Game.Unity.Enemies;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.EditorTools.Content
{
    /// <summary>
    /// Creates or updates the placeholder enemy and encounter assets used by tests, in
    /// <see cref="PlaceholderGuard.PlaceholderFolder"/>, after regenerating the placeholder cards they use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ids, stats and spell lines below are arbitrary test data, not game content or balance values. Final
    /// enemies, mini-bosses and professors are written by the project owner. Every generated asset is flagged as
    /// placeholder.
    /// </para>
    /// <para>
    /// Run from the menu <c>Tools &gt; Game &gt; Regenerate Placeholder Enemies</c>, or headless with
    /// <c>-executeMethod Game.Unity.EditorTools.Content.PlaceholderEnemyGenerator.Generate</c>. Existing assets are
    /// updated in place, so their GUIDs and references survive a regeneration.
    /// </para>
    /// </remarks>
    public static class PlaceholderEnemyGenerator
    {
        private static readonly EnemySpec[] EnemySpecs =
        {
            new EnemySpec("test_enemy_01", EnemyRank.Regular, 10, 0, "test_card_01"),
            new EnemySpec("test_enemy_02", EnemyRank.Regular, 8, 2, "test_card_03", "test_card_01"),
            new EnemySpec("test_miniboss_01", EnemyRank.MiniBoss, 25, 3, "test_card_04", "test_card_02"),
            new EnemySpec("test_professor_01", EnemyRank.Professor, 40, 5, "test_card_05", "test_card_01", "test_card_03"),
        };

        private static readonly EncounterSpec[] EncounterSpecs =
        {
            new EncounterSpec("test_encounter_01", "test_enemy_01"),
            new EncounterSpec("test_encounter_02", "test_enemy_01", "test_enemy_02", "test_enemy_01"),
            new EncounterSpec("test_encounter_03", "test_miniboss_01"),
            new EncounterSpec("test_encounter_04", "test_professor_01"),
        };

        /// <summary>Ids of the generated enemies, in order. Each asset is named after its id.</summary>
        public static IReadOnlyList<string> EnemyIds { get; } = EnemySpecs.Select(spec => spec.Id).ToArray();

        /// <summary>Ids of the generated encounters, in order. Each asset is named after its id.</summary>
        public static IReadOnlyList<string> EncounterIds { get; } = EncounterSpecs.Select(spec => spec.Id).ToArray();

        /// <summary>Path of the asset holding the enemy or encounter <paramref name="id"/>.</summary>
        public static string AssetPath(string id)
        {
            return $"{PlaceholderGuard.PlaceholderFolder}/{id}.asset";
        }

        [MenuItem("Tools/Game/Regenerate Placeholder Enemies")]
        public static void Generate()
        {
            PlaceholderCardGenerator.Generate();

            foreach (var spec in EnemySpecs)
            {
                var asset = LoadOrCreate<EnemyAsset>(AssetPath(spec.Id));
                Write(asset, spec);
            }

            foreach (var spec in EncounterSpecs)
            {
                var asset = LoadOrCreate<EncounterAsset>(AssetPath(spec.Id));
                Write(asset, spec);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Generated {EnemySpecs.Length} placeholder enemies and {EncounterSpecs.Length} placeholder "
                + $"encounters in {PlaceholderGuard.PlaceholderFolder}.");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        private static void Write(EnemyAsset asset, EnemySpec spec)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").stringValue = spec.Id;
            serialized.FindProperty("_rank").intValue = (int)spec.Rank;
            serialized.FindProperty("_maxHealth").intValue = spec.MaxHealth;
            serialized.FindProperty("_startingShield").intValue = spec.StartingShield;
            serialized.FindProperty("_isPlaceholder").boolValue = true;

            var line = serialized.FindProperty("_spellLine");
            line.arraySize = spec.CardIds.Length;
            for (var i = 0; i < spec.CardIds.Length; i++)
            {
                line.GetArrayElementAtIndex(i).objectReferenceValue =
                    Load<CardAsset>(PlaceholderCardGenerator.AssetPath(spec.CardIds[i]));
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void Write(EncounterAsset asset, EncounterSpec spec)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").stringValue = spec.Id;
            serialized.FindProperty("_isPlaceholder").boolValue = true;

            var enemies = serialized.FindProperty("_enemies");
            enemies.arraySize = spec.EnemyIds.Length;
            for (var i = 0; i < spec.EnemyIds.Length; i++)
            {
                enemies.GetArrayElementAtIndex(i).objectReferenceValue = Load<EnemyAsset>(AssetPath(spec.EnemyIds[i]));
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Placeholder asset missing at {path}.");
            }

            return asset;
        }

        private readonly struct EnemySpec
        {
            public EnemySpec(string id, EnemyRank rank, int maxHealth, int startingShield, params string[] cardIds)
            {
                Id = id;
                Rank = rank;
                MaxHealth = maxHealth;
                StartingShield = startingShield;
                CardIds = cardIds;
            }

            public string Id { get; }

            public EnemyRank Rank { get; }

            public int MaxHealth { get; }

            public int StartingShield { get; }

            public string[] CardIds { get; }
        }

        private readonly struct EncounterSpec
        {
            public EncounterSpec(string id, params string[] enemyIds)
            {
                Id = id;
                EnemyIds = enemyIds;
            }

            public string Id { get; }

            public string[] EnemyIds { get; }
        }
    }
}
