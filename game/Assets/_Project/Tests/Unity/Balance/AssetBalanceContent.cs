using Game.Unity.Classes;
using Game.Unity.EditorTools.Content;
using Game.Unity.Runs;
using Game.Unity.Upgrades;
using Game.Core.Runs;
using UnityEditor;

namespace Game.Unity.Tests.Balance
{
    /// <summary>Reads the content of a balance run from the generated assets, as the game does.</summary>
    public static class AssetBalanceContent
    {
        public static BalanceContent Load()
        {
            var limit = AssetDatabase.LoadAssetAtPath<FightTimeLimitAsset>(PlaceholderBiomeGenerator.FightTimeLimitPath);
            var curve = AssetDatabase.LoadAssetAtPath<LevelCurveAsset>(PlaceholderBiomeGenerator.LevelCurvePath);
            return new BalanceContent
            {
                HeroClass = AssetDatabase.LoadAssetAtPath<ClassAsset>(MvpClassContentGenerator.ClassPath).ToDefinition(),
                Biome = AssetDatabase.LoadAssetAtPath<BiomeAsset>(PlaceholderBiomeGenerator.BiomePath).ToDefinition(),
                Rules = new RunRules(limit.MaxTicks, curve.ToDefinition()),
                Passives = AssetDatabase.LoadAssetAtPath<PassiveUpgradePoolAsset>(PassiveUpgradePoolGenerator.PoolPath).ToUpgrades(),
            };
        }
    }
}
