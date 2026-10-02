using System.Collections.Generic;
using System.Linq;
using Game.Unity.Cards;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.EditorTools.Content
{
    /// <summary>
    /// Creates or updates the placeholder card assets used by tests, in <see cref="PlaceholderGuard.PlaceholderFolder"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ids, cast times and amounts below are arbitrary test data, not game content or balance values. Final
    /// words and cards are written by the project owner. Every generated card is flagged as placeholder.
    /// </para>
    /// <para>
    /// Run from the menu <c>Tools &gt; Game &gt; Regenerate Placeholder Cards</c>, or headless with
    /// <c>-executeMethod Game.Unity.EditorTools.Content.PlaceholderCardGenerator.Generate</c>. Existing assets are
    /// updated in place, so their GUIDs and references survive a regeneration.
    /// </para>
    /// </remarks>
    public static class PlaceholderCardGenerator
    {
        private static readonly CardSpec[] Specs =
        {
            new CardSpec("test_card_01", 1, (EffectKind.DealDamage, 1)),
            new CardSpec("test_card_02", 2, (EffectKind.Heal, 2)),
            new CardSpec("test_card_03", 2, (EffectKind.GainShield, 3)),
            new CardSpec("test_card_04", 3, (EffectKind.DealDamage, 2), (EffectKind.GainShield, 1)),
            new CardSpec("test_card_05", 4, (EffectKind.DealDamage, 3), (EffectKind.Heal, 1), (EffectKind.GainShield, 1)),
        };

        /// <summary>Ids of the generated cards, in order. Each asset is named after its id.</summary>
        public static IReadOnlyList<string> CardIds { get; } = Specs.Select(spec => spec.Id).ToArray();

        /// <summary>Path of the asset holding the card <paramref name="id"/>.</summary>
        public static string AssetPath(string id)
        {
            return $"{PlaceholderGuard.PlaceholderFolder}/{id}.asset";
        }

        [MenuItem("Tools/Game/Regenerate Placeholder Cards")]
        public static void Generate()
        {
            EnsureFolder(PlaceholderGuard.PlaceholderFolder);

            foreach (var spec in Specs)
            {
                var path = AssetPath(spec.Id);
                var asset = AssetDatabase.LoadAssetAtPath<CardAsset>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<CardAsset>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                Write(asset, spec);
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Generated {Specs.Length} placeholder cards in {PlaceholderGuard.PlaceholderFolder}.");
        }

        private static void Write(CardAsset asset, CardSpec spec)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("_id").stringValue = spec.Id;
            serialized.FindProperty("_castTime").intValue = spec.CastTime;
            serialized.FindProperty("_isPlaceholder").boolValue = true;

            var list = serialized.FindProperty("_effects");
            list.arraySize = spec.Effects.Length;
            for (var i = 0; i < spec.Effects.Length; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_kind").intValue = (int)spec.Effects[i].Kind;
                entry.FindPropertyRelative("_amount").intValue = spec.Effects[i].Amount;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private readonly struct CardSpec
        {
            public CardSpec(string id, int castTime, params (EffectKind Kind, int Amount)[] effects)
            {
                Id = id;
                CastTime = castTime;
                Effects = effects;
            }

            public string Id { get; }

            public int CastTime { get; }

            public (EffectKind Kind, int Amount)[] Effects { get; }
        }
    }
}
