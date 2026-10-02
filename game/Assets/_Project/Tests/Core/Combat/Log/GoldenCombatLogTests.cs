using System.IO;
using System.Text;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Combat.Log;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests.Combat.Log
{
    /// <summary>
    /// Golden test of the combat event log: a fixed fight must produce exactly the text log stored in
    /// <c>Combat/Golden/basic_fight_log.txt</c> (format: <see cref="CombatLog.ToText"/>). It fails on purpose when
    /// the combat rules or the log format change an event.
    /// </summary>
    /// <remarks>
    /// When the change is intended, regenerate the file with the <c>[Explicit]</c> test
    /// <c>Record_GoldenFight_RegenerateStoredLog</c> (Test Runner: select it and Run Selected, or
    /// <c>--filter Record_GoldenFight_RegenerateStoredLog</c> on the command line), review the diff line by line and
    /// commit it with the change. See also <see cref="GoldenFightTests"/>.
    /// </remarks>
    public class GoldenCombatLogTests
    {
        // Placeholder ids and arbitrary test data, not real content or balance values.
        private const ulong Seed = 2024UL;
        private const int MaxTicks = 200;
        private const int Capacity = 5;

        private static string GoldenPath =>
            Path.Combine(Application.dataPath, "_Project", "Tests", "Core", "Combat", "Golden", "basic_fight_log.txt");

        /// <summary>
        /// One hero against two enemies, using every effect and every event kind: shield absorption, capped
        /// healing, overkill, an enemy death mid-tick, a retarget and the hero's victory.
        /// </summary>
        private static CombatLog RecordGoldenFight()
        {
            var hero = new FightParticipant(
                new Combatant(24, 1),
                Line(
                    new CardDefinition("test_card_01", 2, new IEffect[] { new DealDamageEffect(4) }),
                    new CardDefinition("test_card_02", 1, new IEffect[] { new GainShieldEffect(2), new HealEffect(3) }),
                    new CardDefinition("test_card_03", 3, new IEffect[] { new DealDamageEffect(7) })));

            var firstEnemy = new FightParticipant(
                new Combatant(9, 3),
                Line(new CardDefinition("test_card_04", 2, new IEffect[] { new DealDamageEffect(3) })));

            var secondEnemy = new FightParticipant(
                new Combatant(8, 0),
                Line(
                    new CardDefinition("test_card_05", 3, new IEffect[] { new DealDamageEffect(4), new HealEffect(2) }),
                    new CardDefinition("test_card_06", 1, new IEffect[] { new GainShieldEffect(1) })));

            return CombatLogRecorder.Record(hero, new[] { firstEnemy, secondEnemy }, MaxTicks, new Pcg32Random(Seed));
        }

        [Test]
        public void Record_GoldenFight_MatchesStoredLog()
        {
            Assert.IsTrue(
                File.Exists(GoldenPath),
                $"Golden file missing: {GoldenPath}. Generate it with Record_GoldenFight_RegenerateStoredLog.");

            var expected = File.ReadAllText(GoldenPath).Replace("\r\n", "\n");
            var actual = RecordGoldenFight().ToText();

            Assert.AreEqual(
                expected,
                actual,
                "The combat log no longer matches the golden file. If the change is intended, regenerate it with "
                + "Record_GoldenFight_RegenerateStoredLog (see GoldenCombatLogTests) and review the diff.");
        }

        [Test]
        [Explicit("Rewrites the golden file. Run it on purpose only, then review the diff.")]
        public void Record_GoldenFight_RegenerateStoredLog()
        {
            var text = RecordGoldenFight().ToText();

            Directory.CreateDirectory(Path.GetDirectoryName(GoldenPath));
            File.WriteAllText(GoldenPath, text, new UTF8Encoding(false));

            Assert.Pass($"Golden file written: {GoldenPath}");
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
