using System.Collections.Generic;
using Game.Unity.Cards;
using Game.Unity.Enemies;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Tests.Enemies
{
    /// <summary>
    /// Builds in-memory card, enemy and encounter assets through their serialized fields, as the inspector would,
    /// and destroys them after the test. Ids and numbers are arbitrary test data.
    /// </summary>
    internal sealed class TestAssets
    {
        private readonly List<Object> _created = new List<Object>();

        public CardAsset Card(string id, int castTime = 1, int damage = 1)
        {
            var card = Create<CardAsset>(id);
            var serialized = new SerializedObject(card);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_castTime").intValue = castTime;
            var effects = serialized.FindProperty("_effects");
            effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).FindPropertyRelative("_kind").intValue = (int)EffectKind.DealDamage;
            effects.GetArrayElementAtIndex(0).FindPropertyRelative("_amount").intValue = damage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return card;
        }

        public EnemyAsset Enemy(string id, EnemyRank rank, int maxHealth, int startingShield, params CardAsset[] spellLine)
        {
            var enemy = Create<EnemyAsset>(id ?? "unnamed_enemy");
            var serialized = new SerializedObject(enemy);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_rank").intValue = (int)rank;
            serialized.FindProperty("_maxHealth").intValue = maxHealth;
            serialized.FindProperty("_startingShield").intValue = startingShield;
            var line = serialized.FindProperty("_spellLine");
            line.arraySize = spellLine.Length;
            for (var i = 0; i < spellLine.Length; i++)
            {
                line.GetArrayElementAtIndex(i).objectReferenceValue = spellLine[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return enemy;
        }

        public EncounterAsset Encounter(string id, params EnemyAsset[] enemies)
        {
            var encounter = Create<EncounterAsset>(id ?? "unnamed_encounter");
            var serialized = new SerializedObject(encounter);
            serialized.FindProperty("_id").stringValue = id;
            var list = serialized.FindProperty("_enemies");
            list.arraySize = enemies.Length;
            for (var i = 0; i < enemies.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return encounter;
        }

        public void DestroyAll()
        {
            foreach (var asset in _created)
            {
                Object.DestroyImmediate(asset);
            }

            _created.Clear();
        }

        private T Create<T>(string assetName) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = assetName;
            _created.Add(asset);
            return asset;
        }
    }
}
