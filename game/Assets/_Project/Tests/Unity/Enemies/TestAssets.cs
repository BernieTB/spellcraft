using System.Collections.Generic;
using Game.Unity.Cards;
using Game.Unity.Classes;
using Game.Unity.Enemies;
using Game.Unity.Runs;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Tests.Enemies
{
    /// <summary>
    /// Builds in-memory card, enemy, encounter and class assets through their serialized fields, as the inspector would,
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

        public ClassAsset Class(
            string id,
            int maxHealth,
            int startingShield,
            int startingLineCapacity,
            CardAsset[] startingDeck,
            CardAsset[] cardPool,
            bool isPlaceholder = false)
        {
            var asset = Create<ClassAsset>(string.IsNullOrEmpty(id) ? "unnamed_class" : id);
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_maxHealth").intValue = maxHealth;
            serialized.FindProperty("_startingShield").intValue = startingShield;
            serialized.FindProperty("_startingLineCapacity").intValue = startingLineCapacity;
            serialized.FindProperty("_isPlaceholder").boolValue = isPlaceholder;
            FillCards(serialized.FindProperty("_startingDeck"), startingDeck);
            FillCards(serialized.FindProperty("_cardPool"), cardPool);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        public SecretRoomAsset SecretRoom(
            string id,
            EnemyAsset objectiveEnemy,
            int objectiveCount,
            EncounterAsset miniBossEncounter,
            int bonusLineSlots,
            CardAsset uniqueCard,
            bool revealsHealth = false,
            bool revealsShield = false,
            params int[] revealedCardPositions)
        {
            var asset = Create<SecretRoomAsset>(string.IsNullOrEmpty(id) ? "unnamed_secret_room" : id);
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_objectiveEnemy").objectReferenceValue = objectiveEnemy;
            serialized.FindProperty("_objectiveCount").intValue = objectiveCount;
            serialized.FindProperty("_miniBossEncounter").objectReferenceValue = miniBossEncounter;
            serialized.FindProperty("_bonusLineSlots").intValue = bonusLineSlots;
            serialized.FindProperty("_uniqueCard").objectReferenceValue = uniqueCard;
            serialized.FindProperty("_revealsProfessorHealth").boolValue = revealsHealth;
            serialized.FindProperty("_revealsProfessorShield").boolValue = revealsShield;
            var positions = serialized.FindProperty("_revealedProfessorCardPositions");
            positions.arraySize = revealedCardPositions.Length;
            for (var i = 0; i < revealedCardPositions.Length; i++)
            {
                positions.GetArrayElementAtIndex(i).intValue = revealedCardPositions[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        public void DestroyAll()
        {
            foreach (var asset in _created)
            {
                Object.DestroyImmediate(asset);
            }

            _created.Clear();
        }

        private static void FillCards(SerializedProperty list, CardAsset[] cards)
        {
            list.arraySize = cards.Length;
            for (var i = 0; i < cards.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            }
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
