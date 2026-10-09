using System.Collections.Generic;
using System.Linq;
using Game.Core.Effects;
using Game.Core.Upgrades;
using Game.Unity.Upgrades;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.EditorTools.Content
{
    /// <summary>
    /// Creates or updates the MVP pool of passive upgrades offered at level-up (#119, ADR 0012): ten passives
    /// <c>PASSIVE_A</c>...<c>PASSIVE_J</c> and the pool <c>PASSIVE_POOL_A</c>, in <see cref="ContentFolder"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Passive words are written by the project owner: until then they are named by the placeholders
    /// <c>PASSIVE_A</c>... like the cards (ADR 0007). Amounts are working numbers to tune with the simulation
    /// runner (ADR 0006), not balance decisions. The assets are not flagged as placeholder content.
    /// </para>
    /// <para>
    /// Run from the menu <c>Tools &gt; Game &gt; Regenerate MVP Passive Pool</c>, or headless with
    /// <c>-executeMethod Game.Unity.EditorTools.Content.PassiveUpgradePoolGenerator.Generate</c>. Existing assets are
    /// updated in place, so their GUIDs and references survive a regeneration.
    /// </para>
    /// </remarks>
    public static class PassiveUpgradePoolGenerator
    {
        /// <summary>Folder holding the MVP passives and their pool (next to the MVP class).</summary>
        public const string ContentFolder = MvpClassContentGenerator.ContentFolder;

        /// <summary>Identifier of the pool of the MVP class.</summary>
        public const string PoolId = "PASSIVE_POOL_A";

        /// <summary>Ids of the generated passives, in pool order.</summary>
        public static IReadOnlyList<string> PassiveIds { get; } = PassivePoolSpecs.All.Select(spec => spec.Id).ToArray();

        /// <summary>Path of the pool asset.</summary>
        public static string PoolPath => $"{ContentFolder}/{PoolId}.asset";

        /// <summary>Path of the asset holding the passive <paramref name="id"/>.</summary>
        public static string PassivePath(string id) => $"{ContentFolder}/{id}.asset";

        [MenuItem("Tools/Game/Regenerate MVP Passive Pool")]
        public static void Generate()
        {
            var assets = new List<PassiveUpgradeAsset>();
            foreach (var spec in PassivePoolSpecs.All)
            {
                var asset = LoadOrCreate<PassiveUpgradeAsset>(PassivePath(spec.Id));
                var serialized = new SerializedObject(asset);
                serialized.FindProperty("_id").stringValue = spec.Id;
                serialized.FindProperty("_kind").intValue = (int)spec.Kind;
                serialized.FindProperty("_effectKind").intValue = (int)spec.EffectKind;
                serialized.FindProperty("_amount").intValue = spec.Amount;
                serialized.FindProperty("_isPlaceholder").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                assets.Add(asset);
            }

            var pool = LoadOrCreate<PassiveUpgradePoolAsset>(PoolPath);
            var poolSerialized = new SerializedObject(pool);
            poolSerialized.FindProperty("_id").stringValue = PoolId;
            var list = poolSerialized.FindProperty("_upgrades");
            list.arraySize = assets.Count;
            for (var i = 0; i < assets.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
            }

            poolSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pool);

            AssetDatabase.SaveAssets();
            Debug.Log($"Generated the MVP passive pool and {assets.Count} passives in {ContentFolder}.");
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
    }
}
