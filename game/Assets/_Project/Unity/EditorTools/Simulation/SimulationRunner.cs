using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Core.Simulation;
using Game.Unity.DebugTools;
using Game.Unity.EditorTools.DebugTools;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Game.Unity.EditorTools.Simulation
{
    /// <summary>
    /// Headless simulation runner: plays the fight of a <see cref="DebugFightSetup"/> asset over a range of seeds and
    /// writes a JSON <see cref="SimulationSummary"/> (<c>docs/adr/0006-headless-simulation-runner.md</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Run headless with <c>-batchmode -quit -executeMethod Game.Unity.EditorTools.Simulation.SimulationRunner.Run</c>
    /// and the options of <see cref="SimulationArguments"/>, or from the menu
    /// <c>Tools &gt; Game &gt; Run Simulation</c> (default options). All the work is in Core
    /// (<see cref="FightBatch"/>); this class only loads the asset, times the run and writes the file.
    /// </para>
    /// <para>
    /// Durations are logged, never written to the summary, so the summary stays reproducible.
    /// In batch mode the editor exits with code 0 on success and 1 on any error.
    /// </para>
    /// </remarks>
    public static class SimulationRunner
    {
        /// <summary>Folder of the default output file, relative to the Unity project folder. Ignored by Git.</summary>
        public const string DefaultOutputFolder = "SimulationResults";

        /// <summary>Name of the default output file.</summary>
        public const string DefaultOutputFile = "simulation-summary.json";

        [MenuItem("Tools/Game/Run Simulation")]
        public static void Run()
        {
            try
            {
                var arguments = SimulationArguments.Parse(Environment.GetCommandLineArgs());
                var outputPath = Execute(arguments);
                Debug.Log($"[Simulation] Summary written to {outputPath}");
                ExitIfBatchMode(0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Simulation] Failed: {exception}");
                ExitIfBatchMode(1);
                if (!Application.isBatchMode)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Runs the simulation described by <paramref name="arguments"/>, writes the summary and returns the full path
        /// of the file written.
        /// </summary>
        /// <exception cref="InvalidOperationException">The setup asset is missing or invalid.</exception>
        public static string Execute(SimulationArguments arguments)
        {
            var total = Stopwatch.StartNew();
            var setupPath = arguments.SetupPath ?? DebugFightSceneBuilder.SetupPath;
            var setup = AssetDatabase.LoadAssetAtPath<DebugFightSetup>(setupPath);
            if (setup == null)
            {
                throw new InvalidOperationException($"No DebugFightSetup asset at '{setupPath}'.");
            }

            var firstSeed = arguments.SeedStart ?? setup.Seed;
            var simulation = Stopwatch.StartNew();
            var summary = Simulate(setup, firstSeed, arguments.Fights);
            simulation.Stop();

            var outputPath = Path.GetFullPath(arguments.OutputPath ?? DefaultOutputPath());
            var folder = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(outputPath, summary.ToJson(), new UTF8Encoding(false));
            total.Stop();

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "[Simulation] {0} fights of '{1}', seeds {2} to {3}: hero {4}, enemies {5}, timeout {6}, "
                + "average {7:0.##} ticks. Simulation loop {8} ms ({9:0.###} ms per fight); runner total {10} ms; "
                + "editor up for {11:0.0} s.",
                summary.FightCount,
                setupPath,
                summary.FirstSeed,
                summary.LastSeed,
                summary.HeroWins,
                summary.EnemyWins,
                summary.Timeouts,
                summary.AverageTicks,
                simulation.ElapsedMilliseconds,
                simulation.Elapsed.TotalMilliseconds / summary.FightCount,
                total.ElapsedMilliseconds,
                EditorApplication.timeSinceStartup));
            return outputPath;
        }

        /// <summary>Runs <paramref name="fights"/> fights of <paramref name="setup"/> from <paramref name="firstSeed"/>.</summary>
        public static SimulationSummary Simulate(DebugFightSetup setup, long firstSeed, int fights)
        {
            if (setup == null)
            {
                throw new ArgumentNullException(nameof(setup));
            }

            return FightBatch.Run(firstSeed, fights, setup.CreateFight);
        }

        /// <summary>Full path of the default output file, in the ignored <see cref="DefaultOutputFolder"/>.</summary>
        public static string DefaultOutputPath()
        {
            var projectFolder = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectFolder, DefaultOutputFolder, DefaultOutputFile);
        }

        private static void ExitIfBatchMode(int code)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
