using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Unity.EditorTools.Content;
using NUnit.Framework;
using UnityEditor;

namespace Game.Unity.Tests.Balance
{
    /// <summary>
    /// Tuning tool of the balance (#125): plays complete runs of the biome with the three bots and writes a table, outside
    /// the repository. Does nothing unless the environment variable <c>SPELLCRAFT_BALANCE_REPORT</c> holds the output
    /// path. With <c>SPELLCRAFT_BALANCE_REGEN=1</c> it first regenerates the class, passive and biome assets from their
    /// specs, so one Unity run tunes the numbers. <c>SPELLCRAFT_BALANCE_SEEDS</c> sets the seed count (default 300).
    /// </summary>
    public class BalanceReportTests
    {
        [Test, Timeout(3600000)]
        public void WriteReport_WhenAskedByTheEnvironment()
        {
            var path = Environment.GetEnvironmentVariable("SPELLCRAFT_BALANCE_REPORT");
            if (string.IsNullOrEmpty(path))
            {
                Assert.Ignore("Set SPELLCRAFT_BALANCE_REPORT to write the balance report.");
            }

            if (Environment.GetEnvironmentVariable("SPELLCRAFT_BALANCE_REGEN") == "1")
            {
                MvpClassContentGenerator.Generate();
                PassiveUpgradePoolGenerator.Generate();
                PlaceholderBiomeGenerator.Generate();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var seeds = int.TryParse(Environment.GetEnvironmentVariable("SPELLCRAFT_BALANCE_SEEDS"), out var parsed) ? parsed : 300;
            var content = AssetBalanceContent.Load();
            var text = BalanceReport.Table(content, seeds);
            File.WriteAllText(path, text);
            TestContext.Out.WriteLine(text);
        }
    }
}
