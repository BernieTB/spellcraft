using System.IO;
using System.Text;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests.Combat
{
    /// <summary>
    /// Golden tests of the combat loop: fixed fights (fixed inputs and seed) must produce exactly the traces stored
    /// in <c>Golden/</c> (format: <see cref="FightTrace"/>): <c>basic_fight.txt</c> for the basic rules and
    /// <c>neighbour_fight.txt</c> for neighbour modifiers. Any change to the combat rules, timing, targeting,
    /// effects or neighbour modifiers that changes an outcome makes them fail on purpose.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a change of outcome is intended (a rule changed on purpose), regenerate the file deliberately, review
    /// its diff line by line, and commit it with the rule change:
    /// </para>
    /// <list type="bullet">
    /// <item>Editor: Window &gt; General &gt; Test Runner &gt; EditMode, select
    /// <c>Run_GoldenFight_RegenerateStoredTrace</c> (or <c>Run_NeighbourGoldenFight_RegenerateStoredTrace</c>) and
    /// click Run Selected (they are <c>[Explicit]</c>, so Run All skips them).</item>
    /// <item>CLI: <c>unity test ./game --editor-version 6000.3.24f1 --mode EditMode
    /// --filter Run_GoldenFight_RegenerateStoredTrace --output &lt;scratch-dir&gt;/results.xml</c> (same with the
    /// neighbour test name).</item>
    /// </list>
    /// <para>
    /// The file is read from <c>Application.dataPath</c>, which points to <c>game/Assets</c> both in the editor and
    /// in batch mode (local CLI and CI).
    /// </para>
    /// </remarks>
    public class GoldenFightTests
    {
        // Placeholder ids and arbitrary test data, not real content or balance values.
        private const ulong Seed = 12345UL;
        private const int MaxTicks = 200;
        private const int Capacity = 5;

        private static string GoldenPath => GoldenFile("basic_fight.txt");

        private static string NeighbourGoldenPath => GoldenFile("neighbour_fight.txt");

        private static string GoldenFile(string name) =>
            Path.Combine(Application.dataPath, "_Project", "Tests", "Core", "Combat", "Golden", name);

        /// <summary>
        /// The golden fight: one hero against two enemies, using every placeholder effect, with shield absorption,
        /// overkill, an enemy dying mid-fight and a retarget. Changing it changes the golden file.
        /// </summary>
        public static Fight CreateGoldenFight()
        {
            var hero = new FightParticipant(
                new Combatant(30, 0),
                Line(
                    new CardDefinition("test_card_01", 2, new IEffect[] { new DealDamageEffect(4) }),
                    new CardDefinition("test_card_02", 1, new IEffect[] { new GainShieldEffect(3) }),
                    new CardDefinition("test_card_03", 3, new IEffect[] { new HealEffect(2), new DealDamageEffect(5) })));

            var firstEnemy = new FightParticipant(
                new Combatant(12, 2),
                Line(new CardDefinition("test_card_04", 3, new IEffect[] { new DealDamageEffect(5) })));

            var secondEnemy = new FightParticipant(
                new Combatant(10, 0),
                Line(
                    new CardDefinition("test_card_05", 2, new IEffect[] { new DealDamageEffect(3) }),
                    new CardDefinition("test_card_06", 1, new IEffect[] { new GainShieldEffect(2) })));

            return new Fight(hero, new[] { firstEnemy, secondEnemy }, MaxTicks, new Pcg32Random(Seed));
        }

        /// <summary>
        /// The neighbour golden fight: every modifier kind and direction, a wrap at both line ends, a single-card
        /// line, stacking, a bonus without a matching effect and an enemy dying with a bonus pending. Changing it
        /// changes its golden file.
        /// </summary>
        public static Fight CreateNeighbourGoldenFight()
        {
            var hero = new FightParticipant(
                new Combatant(40, 0),
                Line(
                    new CardDefinition(
                        "test_card_01",
                        2,
                        new IEffect[] { new DealDamageEffect(2) },
                        new[]
                        {
                            new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, 3),
                            new NeighbourModifier(BonusKind.Shield, NeighbourDirection.Previous, 2),
                        }),
                    new CardDefinition(
                        "test_card_02",
                        1,
                        new IEffect[] { new DealDamageEffect(1), new DealDamageEffect(1) },
                        new[] { new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, 1) }),
                    new CardDefinition(
                        "test_card_03",
                        3,
                        new IEffect[] { new GainShieldEffect(1), new HealEffect(1) },
                        new[] { new NeighbourModifier(BonusKind.Heal, NeighbourDirection.Next, 4) })));

            var firstEnemy = new FightParticipant(
                new Combatant(14, 1),
                Line(new CardDefinition(
                    "test_card_04",
                    2,
                    new IEffect[] { new DealDamageEffect(2) },
                    new[] { new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Previous, 1) })));

            var secondEnemy = new FightParticipant(
                new Combatant(16, 0),
                Line(
                    new CardDefinition(
                        "test_card_05",
                        1,
                        new IEffect[] { new GainShieldEffect(1) },
                        new[] { new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, 2) }),
                    new CardDefinition("test_card_06", 2, new IEffect[] { new DealDamageEffect(3) })));

            return new Fight(hero, new[] { firstEnemy, secondEnemy }, MaxTicks, new Pcg32Random(Seed));
        }

        [Test]
        public void Run_GoldenFight_MatchesStoredTrace()
        {
            Assert.IsTrue(
                File.Exists(GoldenPath),
                $"Golden file missing: {GoldenPath}. Generate it with Run_GoldenFight_RegenerateStoredTrace.");

            var expected = File.ReadAllText(GoldenPath).Replace("\r\n", "\n");
            var actual = FightTrace.Format(CreateGoldenFight().Run());

            Assert.AreEqual(
                expected,
                actual,
                "The fight no longer matches the golden trace. If the change is intended, regenerate it with " +
                "Run_GoldenFight_RegenerateStoredTrace (see GoldenFightTests) and review the diff.");
        }

        [Test]
        [Explicit("Rewrites the golden file. Run it on purpose only, then review the diff.")]
        public void Run_GoldenFight_RegenerateStoredTrace()
        {
            var trace = FightTrace.Format(CreateGoldenFight().Run());

            Directory.CreateDirectory(Path.GetDirectoryName(GoldenPath));
            File.WriteAllText(GoldenPath, trace, new UTF8Encoding(false));

            Assert.Pass($"Golden file written: {GoldenPath}");
        }

        [Test]
        public void Run_NeighbourGoldenFight_MatchesStoredTrace()
        {
            Assert.IsTrue(
                File.Exists(NeighbourGoldenPath),
                $"Golden file missing: {NeighbourGoldenPath}. Generate it with Run_NeighbourGoldenFight_RegenerateStoredTrace.");

            var expected = File.ReadAllText(NeighbourGoldenPath).Replace("\r\n", "\n");
            var actual = FightTrace.Format(CreateNeighbourGoldenFight().Run());

            Assert.AreEqual(
                expected,
                actual,
                "The neighbour fight no longer matches the golden trace. If the change is intended, regenerate it " +
                "with Run_NeighbourGoldenFight_RegenerateStoredTrace (see GoldenFightTests) and review the diff.");
        }

        [Test]
        [Explicit("Rewrites the golden file. Run it on purpose only, then review the diff.")]
        public void Run_NeighbourGoldenFight_RegenerateStoredTrace()
        {
            var trace = FightTrace.Format(CreateNeighbourGoldenFight().Run());

            Directory.CreateDirectory(Path.GetDirectoryName(NeighbourGoldenPath));
            File.WriteAllText(NeighbourGoldenPath, trace, new UTF8Encoding(false));

            Assert.Pass($"Golden file written: {NeighbourGoldenPath}");
        }

        private static SpellLine<CardDefinition> Line(params CardDefinition[] cards)
        {
            var line = new SpellLine<CardDefinition>(Capacity);
            foreach (var card in cards)
            {
                line.Add(card);
            }

            return line;
        }
    }
}
