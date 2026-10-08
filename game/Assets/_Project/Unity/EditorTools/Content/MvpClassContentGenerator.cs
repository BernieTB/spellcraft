using System.Collections.Generic;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Effects;
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
    /// thresholds and the hero's stats below are first-pass numbers, to be tuned with the simulation runner
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

        /// <summary>Hero's max health at the start of every fight (first-pass number).</summary>
        public const int MaxHealth = 30;

        /// <summary>Hero's shield at the start of every fight (first-pass number).</summary>
        public const int StartingShield = 0;

        /// <summary>Spell line slots at the start of a run (ADR 0009).</summary>
        public const int StartingLineCapacity = 4;

        // Casts needed for the two evolution stages of every card (first-pass numbers).
        private const int FirstStageCasts = 3;
        private const int SecondStageCasts = 7;

        private static readonly CardSpec[] StartingSpecs =
        {
            // Two damage cards.
            new CardSpec("CARD_A", 2, new[] { Damage(3) }, new Mod[0],
                At(FirstStageCasts, new[] { Damage(4) }), At(SecondStageCasts, new[] { Damage(5) })),
            new CardSpec("CARD_B", 3, new[] { Damage(5) }, new Mod[0],
                At(FirstStageCasts, new[] { Damage(7) }), At(SecondStageCasts, new[] { Damage(9) })),

            // The weaver card: a small hit that boosts the next card's damage.
            new CardSpec("CARD_C", 2, new[] { Damage(1) }, new[] { NextDamage(2) },
                At(FirstStageCasts, new[] { Damage(1) }, NextDamage(3)),
                At(SecondStageCasts, new[] { Damage(2) }, NextDamage(4))),

            // The defensive card gives shield, not healing (ADR 0009).
            new CardSpec("CARD_D", 2, new[] { Shield(4) }, new Mod[0],
                At(FirstStageCasts, new[] { Shield(6) }), At(SecondStageCasts, new[] { Shield(8) })),
        };

        private static readonly CardSpec[] PoolSpecs =
        {
            new CardSpec("CARD_E", 1, new[] { Damage(2) }, new Mod[0],
                At(FirstStageCasts, new[] { Damage(3) }), At(SecondStageCasts, new[] { Damage(4) })),
            new CardSpec("CARD_F", 4, new[] { Damage(8) }, new Mod[0],
                At(FirstStageCasts, new[] { Damage(11) }), At(SecondStageCasts, new[] { Damage(14) })),
            new CardSpec("CARD_G", 3, new[] { Damage(3), Shield(2) }, new Mod[0],
                At(FirstStageCasts, new[] { Damage(4), Shield(3) }),
                At(SecondStageCasts, new[] { Damage(5), Shield(4) })),
            new CardSpec("CARD_H", 2, new[] { Shield(2) }, new[] { NextShield(2) },
                At(FirstStageCasts, new[] { Shield(3) }, NextShield(3)),
                At(SecondStageCasts, new[] { Shield(4) }, NextShield(4))),
            new CardSpec("CARD_I", 3, new[] { Damage(2) }, new[] { PreviousDamage(3) },
                At(FirstStageCasts, new[] { Damage(2) }, PreviousDamage(4)),
                At(SecondStageCasts, new[] { Damage(3) }, PreviousDamage(5))),
            new CardSpec("CARD_J", 2, new[] { Heal(2) }, new Mod[0],
                At(FirstStageCasts, new[] { Heal(3) }), At(SecondStageCasts, new[] { Heal(4) })),
            new CardSpec("CARD_K", 3, new[] { Damage(2) }, new[] { NextDamage(1), PreviousDamage(1) },
                At(FirstStageCasts, new[] { Damage(2) }, NextDamage(2), PreviousDamage(2)),
                At(SecondStageCasts, new[] { Damage(3) }, NextDamage(3), PreviousDamage(3))),
            new CardSpec("CARD_L", 1, new[] { Damage(1) }, new[] { NextDamage(1) },
                At(FirstStageCasts, new[] { Damage(1) }, NextDamage(2)),
                At(SecondStageCasts, new[] { Damage(2) }, NextDamage(3))),
            new CardSpec("CARD_M", 5, new[] { Damage(6), Shield(4) }, new Mod[0],
                At(FirstStageCasts, new[] { Damage(8), Shield(6) }),
                At(SecondStageCasts, new[] { Damage(10), Shield(8) })),
            new CardSpec("CARD_N", 4, new[] { Shield(6) }, new[] { NextDamage(3) },
                At(FirstStageCasts, new[] { Shield(8) }, NextDamage(4)),
                At(SecondStageCasts, new[] { Shield(10) }, NextDamage(5))),
        };

        /// <summary>Ids of the starting deck cards, in spell line order.</summary>
        /// <remarks>
        /// The weaver card comes before the second damage card, so its bonus lands on that card.
        /// </remarks>
        public static IReadOnlyList<string> StartingDeckIds { get; } = new[] { "CARD_A", "CARD_C", "CARD_B", "CARD_D" };

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

        private static Eff Damage(int amount) => new Eff(EffectKind.DealDamage, amount);

        private static Eff Heal(int amount) => new Eff(EffectKind.Heal, amount);

        private static Eff Shield(int amount) => new Eff(EffectKind.GainShield, amount);

        private static Mod NextDamage(int amount) => new Mod(BonusKind.Damage, NeighbourDirection.Next, amount);

        private static Mod PreviousDamage(int amount) => new Mod(BonusKind.Damage, NeighbourDirection.Previous, amount);

        private static Mod NextShield(int amount) => new Mod(BonusKind.Shield, NeighbourDirection.Next, amount);

        private static Stage At(int casts, Eff[] effects, params Mod[] modifiers) => new Stage(casts, effects, modifiers);

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

        private readonly struct Eff
        {
            public Eff(EffectKind kind, int amount)
            {
                Kind = kind;
                Amount = amount;
            }

            public EffectKind Kind { get; }

            public int Amount { get; }
        }

        private readonly struct Mod
        {
            public Mod(BonusKind kind, NeighbourDirection direction, int amount)
            {
                Kind = kind;
                Direction = direction;
                Amount = amount;
            }

            public BonusKind Kind { get; }

            public NeighbourDirection Direction { get; }

            public int Amount { get; }
        }

        private readonly struct Stage
        {
            public Stage(int casts, Eff[] effects, Mod[] modifiers)
            {
                Casts = casts;
                Effects = effects;
                Modifiers = modifiers;
            }

            public int Casts { get; }

            public Eff[] Effects { get; }

            public Mod[] Modifiers { get; }
        }

        private readonly struct CardSpec
        {
            public CardSpec(string id, int castTime, Eff[] effects, Mod[] modifiers, params Stage[] stages)
            {
                Id = id;
                CastTime = castTime;
                Effects = effects;
                Modifiers = modifiers;
                Stages = stages;
            }

            public string Id { get; }

            public int CastTime { get; }

            public Eff[] Effects { get; }

            public Mod[] Modifiers { get; }

            public Stage[] Stages { get; }
        }
    }
}
