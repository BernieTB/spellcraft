using System;
using System.IO;
using Game.Unity.DebugTools;
using Game.Unity.EditorTools.DebugTools;
using Game.Unity.EditorTools.Simulation;
using NUnit.Framework;
using UnityEditor;

namespace Game.Unity.Tests.Simulation
{
    /// <summary>
    /// Checks the command-line options of the headless simulation runner and runs it on the debug fight setup.
    /// </summary>
    public class SimulationRunnerTests
    {
        private static DebugFightSetup LoadSetup()
        {
            var setup = AssetDatabase.LoadAssetAtPath<DebugFightSetup>(DebugFightSceneBuilder.SetupPath);
            Assert.IsNotNull(setup, $"Missing {DebugFightSceneBuilder.SetupPath}: run Tools > Game > Rebuild Debug Fight Scene.");
            return setup;
        }

        [Test]
        public void Parse_NoOption_UsesDefaults()
        {
            var arguments = SimulationArguments.Parse(new[] { "Unity.exe", "-batchmode", "-quit" });

            Assert.IsNull(arguments.SetupPath);
            Assert.AreEqual(SimulationArguments.DefaultFights, arguments.Fights);
            Assert.IsNull(arguments.SeedStart);
            Assert.IsNull(arguments.OutputPath);
        }

        [Test]
        public void Parse_EveryOption_ReadsValues()
        {
            var arguments = SimulationArguments.Parse(new[]
            {
                "-simSetup", "Assets/x.asset", "-simFights", "25", "-simSeedStart", "-7", "-simOutput", "C:/out/s.json",
            });

            Assert.AreEqual("Assets/x.asset", arguments.SetupPath);
            Assert.AreEqual(25, arguments.Fights);
            Assert.AreEqual(-7L, arguments.SeedStart);
            Assert.AreEqual("C:/out/s.json", arguments.OutputPath);
        }

        [TestCase("0")]
        [TestCase("ten")]
        public void Parse_InvalidFights_Throws(string value)
        {
            Assert.Throws<ArgumentException>(() => SimulationArguments.Parse(new[] { "-simFights", value }));
        }

        [Test]
        public void Parse_InvalidSeed_Throws()
        {
            Assert.Throws<ArgumentException>(() => SimulationArguments.Parse(new[] { "-simSeedStart", "1.5" }));
        }

        [Test]
        public void Parse_MissingValue_Throws()
        {
            Assert.Throws<ArgumentException>(() => SimulationArguments.Parse(new[] { "-simOutput" }));
        }

        [Test]
        public void Simulate_SetupAsset_SameSeedsGiveIdenticalSummary()
        {
            var setup = LoadSetup();

            var first = SimulationRunner.Simulate(setup, 1L, 20).ToJson();
            var second = SimulationRunner.Simulate(setup, 1L, 20).ToJson();

            Assert.AreEqual(first, second);
            StringAssert.Contains("\"fightCount\":20", first);
        }

        [Test]
        public void Simulate_SetupAsset_FirstFightMatchesTheDebugViewer()
        {
            var setup = LoadSetup();

            var summary = SimulationRunner.Simulate(setup, setup.Seed, 1);
            var log = setup.Simulate();

            Assert.AreEqual(log.Ticks, summary.MinTicks);
            Assert.AreEqual(log.Winner == Game.Core.Combat.FightWinner.Hero ? 1 : 0, summary.HeroWins);
        }

        [Test]
        public void Execute_WritesTheSummaryFile()
        {
            var output = Path.Combine(Path.GetTempPath(), $"spellcraft-sim-test-{Guid.NewGuid():N}", "summary.json");
            try
            {
                var arguments = SimulationArguments.Parse(new[] { "-simFights", "3", "-simSeedStart", "4", "-simOutput", output });

                var written = SimulationRunner.Execute(arguments);

                Assert.AreEqual(Path.GetFullPath(output), written);
                Assert.AreEqual(SimulationRunner.Simulate(LoadSetup(), 4L, 3).ToJson(), File.ReadAllText(written));
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(output), true);
            }
        }

        [Test]
        public void DefaultOutputPath_IsInTheIgnoredResultsFolder()
        {
            var expectedFolder = Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, SimulationRunner.DefaultOutputFolder);

            Assert.AreEqual(expectedFolder, Path.GetDirectoryName(SimulationRunner.DefaultOutputPath()));
        }
    }
}
