using System;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Combat.Log;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;
using Game.Unity.DebugTools;
using NUnit.Framework;

namespace Game.Unity.Tests.DebugTools
{
    public class FightPlaybackTests
    {
        // Arbitrary test data, not real content or balance values.
        private const double TicksPerSecond = 2d;

        /// <summary>
        /// Hero hits for 3 every 2 ticks; the enemy (7 health, 1 shield) never casts. Events: tick 2 (cast, damage),
        /// tick 4 (cast, damage), tick 6 (cast, damage, death). The fight lasts 6 ticks.
        /// </summary>
        private static CombatLog Log()
        {
            var heroLine = new SpellLine<CardDefinition>(1);
            heroLine.Add(new CardDefinition("test_hit", 2, new IEffect[] { new DealDamageEffect(3) }));
            var enemyLine = new SpellLine<CardDefinition>(1);
            enemyLine.Add(new CardDefinition("test_idle", 100, new IEffect[0]));

            return CombatLogRecorder.Record(
                new FightParticipant(new Combatant(10, 0), heroLine),
                new[] { new FightParticipant(new Combatant(7, 1), enemyLine) },
                50,
                new Pcg32Random(1));
        }

        private static FightPlayback Playback() => new FightPlayback(Log(), TicksPerSecond);

        [Test]
        public void Log_TestFight_HasExpectedShape()
        {
            var log = Log();

            Assert.AreEqual((7, 6, FightWinner.Hero), (log.Events.Count, log.Ticks, log.Winner));
        }

        [Test]
        public void Constructor_NonPositiveRate_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FightPlayback(Log(), 0d));
        }

        [Test]
        public void Constructor_NullLog_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new FightPlayback(null, TicksPerSecond));
        }

        [Test]
        public void New_StartsAtZeroWithNothingRevealed()
        {
            var playback = Playback();

            Assert.AreEqual((0d, 0, false), (playback.Position, playback.RevealedCount, playback.IsFinished));
        }

        [Test]
        public void Advance_BeforeFirstEventTick_RevealsNothing()
        {
            var playback = Playback();

            var revealed = playback.Advance(0.9d); // 1.8 ticks

            Assert.AreEqual((0, 0), (revealed, playback.RevealedCount));
        }

        [Test]
        public void Advance_ReachingEventTick_RevealsEveryEventOfThatTick()
        {
            var playback = Playback();

            var revealed = playback.Advance(1d); // 2 ticks

            Assert.AreEqual((2, 2d), (revealed, playback.Position));
        }

        [Test]
        public void Advance_ScalesWithSpeed()
        {
            var playback = Playback();
            playback.Speed = 2d;

            playback.Advance(1d); // 4 ticks

            Assert.AreEqual((4d, 4), (playback.Position, playback.RevealedCount));
        }

        [Test]
        public void Advance_SmallSteps_AddUp()
        {
            var playback = Playback();

            for (var i = 0; i < 8; i++)
            {
                playback.Advance(0.125d); // exact in binary floating point: 8 steps make 2 ticks
            }

            Assert.AreEqual(2, playback.RevealedCount);
        }

        [Test]
        public void Advance_WhilePaused_DoesNothing()
        {
            var playback = Playback();
            playback.IsPaused = true;

            var revealed = playback.Advance(10d);

            Assert.AreEqual((0, 0d), (revealed, playback.Position));
        }

        [Test]
        public void Advance_PastTheEnd_StopsAtLastTickAndFinishes()
        {
            var playback = Playback();

            playback.Advance(100d);

            Assert.AreEqual((6d, 7, true), (playback.Position, playback.RevealedCount, playback.IsFinished));
        }

        [Test]
        public void Advance_NegativeTime_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Playback().Advance(-1d));
        }

        [Test]
        public void Speed_NonPositive_Throws()
        {
            var playback = Playback();

            Assert.Throws<ArgumentOutOfRangeException>(() => playback.Speed = 0d);
        }

        [Test]
        public void StepToNextEventTick_WhilePaused_RevealsNextTickOnly()
        {
            var playback = Playback();
            playback.IsPaused = true;

            playback.StepToNextEventTick();
            var revealed = playback.StepToNextEventTick();

            Assert.AreEqual((2, 4, 4d), (revealed, playback.RevealedCount, playback.Position));
        }

        [Test]
        public void StepToNextEventTick_AfterLastEvent_GoesToEnd()
        {
            var playback = Playback();
            playback.Advance(100d);

            var revealed = playback.StepToNextEventTick();

            Assert.AreEqual((0, true), (revealed, playback.IsFinished));
        }

        [Test]
        public void Restart_ResetsPositionEventsAndCombatants()
        {
            var playback = Playback();
            playback.Advance(100d);

            playback.Restart();

            var enemy = playback.GetCombatant(1);
            Assert.AreEqual((0d, 0), (playback.Position, playback.RevealedCount));
            Assert.AreEqual((7, 1, false, -1), (enemy.Health, enemy.Shield, enemy.IsDead, enemy.LastCastPosition));
        }

        [Test]
        public void Restart_KeepsSpeedAndPause()
        {
            var playback = Playback();
            playback.Speed = 4d;
            playback.IsPaused = true;

            playback.Restart();

            Assert.AreEqual((4d, true), (playback.Speed, playback.IsPaused));
        }

        [Test]
        public void GetCombatant_AfterDamage_ShowsStateFromTheLog()
        {
            var playback = Playback();

            playback.Advance(1d); // tick 2: 3 damage, 1 on shield

            var enemy = playback.GetCombatant(1);
            Assert.AreEqual((5, 0, false), (enemy.Health, enemy.Shield, enemy.IsDead));
        }

        [Test]
        public void GetCombatant_AfterDeathEvent_IsDead()
        {
            var playback = Playback();

            playback.Advance(100d);

            var enemy = playback.GetCombatant(1);
            Assert.AreEqual((0, true), (enemy.Health, enemy.IsDead));
        }

        [Test]
        public void GetCombatant_AfterCast_ShowsLastCastPosition()
        {
            var playback = Playback();

            playback.Advance(1d);

            Assert.AreEqual((0, -1), (playback.GetCombatant(0).LastCastPosition, playback.GetCombatant(1).LastCastPosition));
        }

        [Test]
        public void GetCombatant_UnknownIndex_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Playback().GetCombatant(2));
        }

        [Test]
        public void Describe_EveryEventOfTheLog_GivesANonEmptyLine()
        {
            var lines = Log().Events.Select(CombatEventText.Describe).ToList();

            Assert.That(lines, Is.All.Not.Empty);
            StringAssert.Contains("Hero [0] test_hit hits Enemy 1 for 3", lines[1]);
        }
    }
}
