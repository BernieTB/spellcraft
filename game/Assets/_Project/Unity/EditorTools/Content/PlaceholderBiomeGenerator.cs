using System;
using System.Collections.Generic;
using System.Linq;
using Game.Unity.Cards;
using Game.Unity.Enemies;
using Game.Unity.Runs;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.EditorTools.Content
{
    /// <summary>
    /// Creates or updates the placeholder biome of the Vertical slice (#72) in
    /// <see cref="PlaceholderGuard.PlaceholderBiomeFolder"/>: regular monsters with one attack card each and their
    /// encounter pool, two mini-bosses and a professor with worked spell lines, two secret rooms, the biome, the
    /// global fight time limit and a level curve (ADR 0009, 0010, 0011).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ids and numbers come from <see cref="PlaceholderBiomeSpecs"/>. They are working names and first-pass values
    /// that let the MVP class finish the biome, not game content or balance: final names, words and creatures are
    /// written by the project owner. Every generated asset is flagged as placeholder.
    /// </para>
    /// <para>
    /// Run from the menu <c>Tools &gt; Game &gt; Regenerate Placeholder Biome</c>, or headless with
    /// <c>-executeMethod Game.Unity.EditorTools.Content.PlaceholderBiomeGenerator.Generate</c>. Existing assets are
    /// updated in place, so their GUIDs and references survive a regeneration.
    /// </para>
    /// </remarks>
    public static class PlaceholderBiomeGenerator
    {
        /// <summary>Asset name of the level curve.</summary>
        public const string LevelCurveId = "LEVEL_CURVE_01";

        /// <summary>Asset name of the global fight time limit.</summary>
        public const string FightTimeLimitId = "FIGHT_TIME_LIMIT";

        /// <summary>Ids of the generated cards, in order. Each asset is named after its id.</summary>
        public static IReadOnlyList<string> CardIds { get; } = PlaceholderBiomeSpecs.Cards.Select(spec => spec.Id).ToArray();

        /// <summary>Ids of the generated enemies, in order.</summary>
        public static IReadOnlyList<string> EnemyIds { get; } = PlaceholderBiomeSpecs.Enemies.Select(spec => spec.Id).ToArray();

        /// <summary>Ids of the generated encounters, in order.</summary>
        public static IReadOnlyList<string> EncounterIds { get; } =
            PlaceholderBiomeSpecs.Encounters.Select(spec => spec.Id).ToArray();

        /// <summary>Ids of the generated secret rooms, in order.</summary>
        public static IReadOnlyList<string> RoomIds { get; } = PlaceholderBiomeSpecs.Rooms.Select(spec => spec.Id).ToArray();

        /// <summary>Path of the asset named <paramref name="id"/>.</summary>
        public static string AssetPath(string id) => $"{PlaceholderGuard.PlaceholderBiomeFolder}/{id}.asset";

        /// <summary>Path of the biome asset.</summary>
        public static string BiomePath => AssetPath(PlaceholderBiomeSpecs.BiomeId);

        /// <summary>Path of the level curve asset.</summary>
        public static string LevelCurvePath => AssetPath(LevelCurveId);

        /// <summary>Path of the fight time limit asset.</summary>
        public static string FightTimeLimitPath => AssetPath(FightTimeLimitId);

        [MenuItem("Tools/Game/Regenerate Placeholder Biome")]
        public static void Generate()
        {
            EnsureFolder(PlaceholderGuard.PlaceholderBiomeFolder);

            foreach (var spec in PlaceholderBiomeSpecs.Cards)
            {
                var asset = LoadOrCreate<CardAsset>(AssetPath(spec.Id));
                Write(asset, spec);
            }

            foreach (var spec in PlaceholderBiomeSpecs.Enemies)
            {
                var asset = LoadOrCreate<EnemyAsset>(AssetPath(spec.Id));
                Write(asset, spec);
            }

            foreach (var spec in PlaceholderBiomeSpecs.Encounters)
            {
                var asset = LoadOrCreate<EncounterAsset>(AssetPath(spec.Id));
                Write(asset, spec);
            }

            foreach (var spec in PlaceholderBiomeSpecs.Rooms)
            {
                var asset = LoadOrCreate<SecretRoomAsset>(AssetPath(spec.Id));
                Write(asset, spec);
            }

            WriteBiome(LoadOrCreate<BiomeAsset>(BiomePath));
            WriteLevelCurve(LoadOrCreate<LevelCurveAsset>(LevelCurvePath));
            WriteFightTimeLimit(LoadOrCreate<FightTimeLimitAsset>(FightTimeLimitPath));

            AssetDatabase.SaveAssets();
            Debug.Log($"Generated the placeholder biome in {PlaceholderGuard.PlaceholderBiomeFolder}: "
                + $"{PlaceholderBiomeSpecs.Cards.Length} cards, {PlaceholderBiomeSpecs.Enemies.Length} enemies, "
                + $"{PlaceholderBiomeSpecs.Encounters.Length} encounters, {PlaceholderBiomeSpecs.Rooms.Length} secret rooms.");
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

        private static T Load<T>(string id) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetPath(id));
            if (asset == null)
            {
                throw new InvalidOperationException($"Placeholder biome asset missing at {AssetPath(id)}.");
            }

            return asset;
        }

        private static void Write(CardAsset asset, PlaceholderBiomeSpecs.CardSpec spec)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").stringValue = spec.Id;
            serialized.FindProperty("_castTime").intValue = spec.CastTime;
            serialized.FindProperty("_isPlaceholder").boolValue = true;

            var effects = serialized.FindProperty("_effects");
            effects.arraySize = spec.Effects.Length;
            for (var i = 0; i < spec.Effects.Length; i++)
            {
                var entry = effects.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_kind").intValue = (int)spec.Effects[i].Kind;
                entry.FindPropertyRelative("_amount").intValue = spec.Effects[i].Amount;
            }

            var modifiers = serialized.FindProperty("_neighbourModifiers");
            modifiers.arraySize = spec.Modifiers.Length;
            for (var i = 0; i < spec.Modifiers.Length; i++)
            {
                var entry = modifiers.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_kind").intValue = (int)spec.Modifiers[i].Kind;
                entry.FindPropertyRelative("_direction").intValue = (int)spec.Modifiers[i].Direction;
                entry.FindPropertyRelative("_amount").intValue = spec.Modifiers[i].Amount;
            }

            serialized.FindProperty("_evolutions").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void Write(EnemyAsset asset, PlaceholderBiomeSpecs.EnemySpec spec)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").stringValue = spec.Id;
            serialized.FindProperty("_rank").intValue = (int)spec.Rank;
            serialized.FindProperty("_maxHealth").intValue = spec.MaxHealth;
            serialized.FindProperty("_startingShield").intValue = spec.Shield;
            serialized.FindProperty("_xpReward").intValue = spec.Xp;
            serialized.FindProperty("_isPlaceholder").boolValue = true;
            WriteReferences(serialized.FindProperty("_spellLine"), spec.CardIds, Load<CardAsset>);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void Write(EncounterAsset asset, PlaceholderBiomeSpecs.EncounterSpec spec)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").stringValue = spec.Id;
            serialized.FindProperty("_isPlaceholder").boolValue = true;
            WriteReferences(serialized.FindProperty("_enemies"), spec.EnemyIds, Load<EnemyAsset>);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void Write(SecretRoomAsset asset, PlaceholderBiomeSpecs.RoomSpec spec)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").stringValue = spec.Id;
            serialized.FindProperty("_objectiveEnemy").objectReferenceValue = Load<EnemyAsset>(spec.ObjectiveEnemyId);
            serialized.FindProperty("_objectiveCount").intValue = spec.ObjectiveCount;
            serialized.FindProperty("_miniBossEncounter").objectReferenceValue = Load<EncounterAsset>(spec.MiniBossEncounterId);
            serialized.FindProperty("_bonusLineSlots").intValue = 1;
            serialized.FindProperty("_uniqueCard").objectReferenceValue = Load<CardAsset>(spec.UniqueCardId);
            serialized.FindProperty("_revealsProfessorHealth").boolValue = spec.RevealsHealth;
            serialized.FindProperty("_revealsProfessorShield").boolValue = spec.RevealsShield;
            var positions = serialized.FindProperty("_revealedProfessorCardPositions");
            positions.arraySize = spec.RevealedPositions.Length;
            for (var i = 0; i < spec.RevealedPositions.Length; i++)
            {
                positions.GetArrayElementAtIndex(i).intValue = spec.RevealedPositions[i];
            }

            serialized.FindProperty("_isPlaceholder").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void WriteBiome(BiomeAsset asset)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").stringValue = PlaceholderBiomeSpecs.BiomeId;
            serialized.FindProperty("_minimumRegularFights").intValue = PlaceholderBiomeSpecs.MinimumRegularFights;
            serialized.FindProperty("_professorEncounter").objectReferenceValue =
                Load<EncounterAsset>(PlaceholderBiomeSpecs.ProfessorEncounterId);
            serialized.FindProperty("_professor").objectReferenceValue = Load<EnemyAsset>(PlaceholderBiomeSpecs.ProfessorId);
            serialized.FindProperty("_isPlaceholder").boolValue = true;
            WriteReferences(serialized.FindProperty("_regularEncounters"), PlaceholderBiomeSpecs.RegularPool, Load<EncounterAsset>);
            WriteReferences(serialized.FindProperty("_secretRooms"), RoomIds.ToArray(), Load<SecretRoomAsset>);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void WriteLevelCurve(LevelCurveAsset asset)
        {
            var serialized = new SerializedObject(asset);
            var costs = serialized.FindProperty("_levelCosts");
            costs.arraySize = PlaceholderBiomeSpecs.LevelCosts.Length;
            for (var i = 0; i < PlaceholderBiomeSpecs.LevelCosts.Length; i++)
            {
                costs.GetArrayElementAtIndex(i).intValue = PlaceholderBiomeSpecs.LevelCosts[i];
            }

            serialized.FindProperty("_costIncreaseAfterList").intValue = PlaceholderBiomeSpecs.CostIncreaseAfterList;
            serialized.FindProperty("_isPlaceholder").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void WriteFightTimeLimit(FightTimeLimitAsset asset)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_maxTicks").intValue = PlaceholderBiomeSpecs.FightTimeLimit;
            serialized.FindProperty("_isPlaceholder").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void WriteReferences<T>(SerializedProperty list, string[] ids, Func<string, T> load)
            where T : UnityEngine.Object
        {
            list.arraySize = ids.Length;
            for (var i = 0; i < ids.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = load(ids[i]);
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var separator = folder.LastIndexOf('/');
            var parent = folder.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder.Substring(separator + 1));
        }
    }
}
