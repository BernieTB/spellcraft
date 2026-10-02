using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game.Unity.EditorTools.Simulation
{
    /// <summary>
    /// Options of the headless simulation runner, read from the editor command line. Every option is optional.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>-simSetup &lt;asset path&gt;</c>: the <c>DebugFightSetup</c> asset to simulate, relative to the
    /// project (default <see cref="DebugTools.DebugFightSceneBuilder.SetupPath"/>).</item>
    /// <item><c>-simFights &lt;n&gt;</c>: number of fights, at least 1 (default <see cref="DefaultFights"/>).</item>
    /// <item><c>-simSeedStart &lt;seed&gt;</c>: seed of the first fight (default: the setup asset's seed).</item>
    /// <item><c>-simOutput &lt;file&gt;</c>: where to write the JSON summary (default
    /// <c>game/SimulationResults/simulation-summary.json</c>, ignored by Git).</item>
    /// </list>
    /// </remarks>
    public sealed class SimulationArguments
    {
        /// <summary>Number of fights when <c>-simFights</c> is not given. A tool default, not a balance value.</summary>
        public const int DefaultFights = 1000;

        public const string SetupOption = "-simSetup";
        public const string FightsOption = "-simFights";
        public const string SeedStartOption = "-simSeedStart";
        public const string OutputOption = "-simOutput";

        private SimulationArguments(string setupPath, int fights, long? seedStart, string outputPath)
        {
            SetupPath = setupPath;
            Fights = fights;
            SeedStart = seedStart;
            OutputPath = outputPath;
        }

        /// <summary>Project-relative path of the setup asset, or null for the default one.</summary>
        public string SetupPath { get; }

        /// <summary>Number of fights to run.</summary>
        public int Fights { get; }

        /// <summary>Seed of the first fight, or null to use the setup asset's seed.</summary>
        public long? SeedStart { get; }

        /// <summary>Output file path, or null for the default one.</summary>
        public string OutputPath { get; }

        /// <summary>Reads the options from a command line (for example <c>Environment.GetCommandLineArgs()</c>).
        /// Unknown arguments are ignored: the editor's own options share the same command line.</summary>
        /// <exception cref="ArgumentException">An option has no value or an invalid one.</exception>
        public static SimulationArguments Parse(IReadOnlyList<string> args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            string setupPath = null;
            string outputPath = null;
            var fights = DefaultFights;
            long? seedStart = null;

            for (var i = 0; i < args.Count; i++)
            {
                switch (args[i])
                {
                    case SetupOption:
                        setupPath = ValueAfter(args, ref i);
                        break;
                    case OutputOption:
                        outputPath = ValueAfter(args, ref i);
                        break;
                    case FightsOption:
                        var fightsText = ValueAfter(args, ref i);
                        if (!int.TryParse(fightsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out fights) || fights < 1)
                        {
                            throw new ArgumentException($"{FightsOption} needs a whole number of at least 1, got '{fightsText}'.");
                        }

                        break;
                    case SeedStartOption:
                        var seedText = ValueAfter(args, ref i);
                        if (!long.TryParse(seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed))
                        {
                            throw new ArgumentException($"{SeedStartOption} needs a whole number, got '{seedText}'.");
                        }

                        seedStart = seed;
                        break;
                }
            }

            return new SimulationArguments(setupPath, fights, seedStart, outputPath);
        }

        private static string ValueAfter(IReadOnlyList<string> args, ref int index)
        {
            var option = args[index];
            if (index + 1 >= args.Count || string.IsNullOrWhiteSpace(args[index + 1]))
            {
                throw new ArgumentException($"{option} needs a value.");
            }

            index++;
            return args[index];
        }
    }
}
