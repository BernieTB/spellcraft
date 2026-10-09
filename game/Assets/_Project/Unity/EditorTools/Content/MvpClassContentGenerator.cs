using System.Collections.Generic;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Effects;
using static Game.Unity.EditorTools.Content.MvpClassSpecs;
using Game.Unity.Cards;
using Game.Unity.Classes;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.EditorTools.Content
{
    /// <summary>
    /// Creates or updates the MVP class content (<c>CLASS_A</c>, the combo weaver of ADR 0007): its starting deck,
    /// its pool of level-up offers and their two evolution stages each (ADR 0013), in <see cref="ContentFolder"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Card words are written by the project owner: until then the cards are named by the placeholders
    /// <c>CARD_A</c>, <c>CARD_B</c>... (ADR 0007), and replacing them is a data change only. Cast times, amounts,
    /// thresholds and the hero's stats are the working numbers of <see cref="MvpClassSpecs"/>, tuned with the balance bots (#125)
    /// (ADR 0006); they are not balance decisions. Unlike the test placeholders, these assets are not flagged as
    /// placeholder content, so they may be referenced from outside the placeholder folder.
    /// </para>
    /// <para>
    /// Run from the menu <c>Tools &gt; Game &gt; Regenerate MVP Class Content</c>, or headless with
    /// <c>-executeMethod Game.Unity.EditorTools.Content.MvpClassContentGenerator.Generate</c>. Existing assets are
    /// updated in place, so their GUIDs and references survive a regeneration.
    /// </para>
    /// </remarks>
    public static class MvpClassContentGenerator
    {
        /// <summary>Folder holding the MVP class and its cards.</summary>
        public const string ContentFolder = "Assets/_Project/Unity/Content/Classes";

        /// <summary>Working name of the MVP class (ADR 0007).</summary>
        public const string ClassId = "CLASS_A";

        /// <summary>Hero's max health at the start of every fight (see <see cref="MvpClassSpecs"/>).</summary>
        public const int MaxHealth = MvpClassSpecs.MaxHealth;

        /// <summary>Hero's shield at the start of every fight.</summary>
        public const int StartingShield = MvpClassSpecs.StartingShield;

        /// <summary>Spell line slots at the start of a run (ADR 0009).</summary>
        public const int StartingLineCapacity = MvpClassSpecs.StartingLineCapacity;

        /// <summary>Ids of the starting deck cards, in spell line order.</summary>
        public static IReadOnlyList<string> StartingDeckIds => MvpClassSpecs.StartingDeckIds;

        /// <summary>Ids of the cards of the level-up pool, in order.</summary>
        public static IReadOnlyList<string> PoolIds { get; } = PoolSpecs.Select(spec => spec.Id).ToArray();

        /// <summary>Ids of every generated card.</summary>
        public static IReadOnlyList<string> CardIds { get; } =
            StartingSpecs.Concat(PoolSpecs).Select(spec => spec.Id).ToArray();

        /// <summary>Path of the class asset.</summary>
        public static string ClassPath => $"{ContentFolder}/{ClassId}.asset";

        /// <summary>Path of the asset holding the card <paramref name="id"/>.</summary>
        public static string CardPath(string id) => $"{ContentFolder}/{id}.asset";

        [MenuItem("Tools/Game/Regenerate MVP Class Content")]
        public static void Generate()
        {
            EnsureFolder(ContentFolder);

            var cards = new Dictionary<string, CardAsset>();
            foreach (var spec in StartingSpecs.Concat(PoolSpecs))
            {
                var asset = LoadOrCreate<CardAsset>(CardPath(spec.Id));
                WriteCard(asset, spec);
                EditorUtility.SetDirty(asset);
                cards[spec.Id] = asset;
            }

            var classAsset = LoadOrCreate<ClassAsset>(ClassPath);
            WriteClass(classAsset, cards);
            EditorUtility.SetDirty(classAsset);

            AssetDatabase.SaveAssets();
            Debug.Log($"Generated the MVP class and {cards.Count} cards in {ContentFolder}.");
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

        private static void WriteClass(ClassAsset asset, IReadOnlyDictionary<string, CardAsset> cards)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").stringValue = ClassId;
            serialized.FindProperty("_maxHealth").intValue = MaxHealth;
            serialized.FindProperty("_startingShield").intValue = StartingShield;
            serialized.FindProperty("_startingLineCapacity").intValue = StartingLineCapacity;
            serialized.FindProperty("_isPlaceholder").boolValue = false;
            WriteCards(serialized.FindProperty("_startingDeck"), StartingDeckIds, cards);
            WriteCards(serialized.FindProperty("_cardPool"), PoolIds, cards);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteCards(
            SerializedProperty list, IReadOnlyList<string> ids, IReadOnlyDictionary<string, CardAsset> cards)
        {
            list.arraySize = ids.Count;
            for (var i = 0; i < ids.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = cards[ids[i]];
            }
        }

        private static void WriteCard(CardAsset asset, CardSpec spec)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").stringValue = spec.Id;
            serialized.FindProperty("_castTime").intValue = spec.CastTime;
            serialized.FindProperty("_isPlaceholder").boolValue = false;
            WriteEffects(serialized.FindProperty("_effects"), spec.Effects);
            WriteModifiers(serialized.FindProperty("_neighbourModifiers"), spec.Modifiers);

            var stages = serialized.FindProperty("_evolutions");
            stages.arraySize = spec.Stages.Length;
            for (var i = 0; i < spec.Stages.Length; i++)
            {
                var entry = stages.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_castsRequired").intValue = spec.Stages[i].Casts;
                WriteEffects(entry.FindPropertyRelative("_effects"), spec.Stages[i].Effects);
                WriteModifiers(entry.FindPropertyRelative("_neighbourModifiers"), spec.Stages[i].Modifiers);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteEffects(SerializedProperty list, Eff[] effects)
        {
            list.arraySize = effects.Length;
            for (var i = 0; i < effects.Length; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_kind").intValue = (int)effects[i].Kind;
                entry.FindPropertyRelative("_amount").intValue = effects[i].Amount;
            }
        }

        private static void WriteModifiers(SerializedProperty list, Mod[] modifiers)
        {
            list.arraySize = modifiers.Length;
            for (var i = 0; i < modifiers.Length; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_kind").intValue = (int)modifiers[i].Kind;
                entry.FindPropertyRelative("_direction").intValue = (int)modifiers[i].Direction;
                entry.FindPropertyRelative("_amount").intValue = modifiers[i].Amount;
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
