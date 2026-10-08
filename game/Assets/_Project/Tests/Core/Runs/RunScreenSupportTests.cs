using System.Linq;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Combat;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Runs;
using NUnit.Framework;

namespace Game.Core.Tests.Runs
{
    /// <summary>
    /// What the run screen (#73) reads from the run: XP into the current level, and the live view of a fight session.
    /// </summary>
    public class RunScreenSupportTests
    {
        // Placeholder ids and arbitrary test values, not game content or balance numbers.
        private static readonly LevelCurve Curve = new LevelCurve(new[] { 10, 20 }, 5);

        private static readonly CardDefinition Strike = new CardDefinition("test_card_strike", 1, new IEffect[] { new DealDamageEffect(10) });
        private static readonly CardDefinition Boost = new CardDefinition(
            "test_card_boost",
            1,
            new IEffect[0],
            new[] { new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, 3) });
        private static readonly CardDefinition Nibble = new CardDefinition("test_card_nibble", 3, new IEffect[] { new DealDamageEffect(1) });

        private static readonly EncounterDefinition Tank =
            new EncounterDefinition("test_encounter_tank", new[] { new EnemyDefinition("test_enemy_tank", 60, 0, new[] { Nibble }, 15) });

        private static Run CreateRun()
        {
            var heroClass = new ClassDefinition("test_class", 20, 0, 2, new[] { Boost, Strike }, new CardDefinition[0]);
            var biome = new BiomeDefinition("test_biome", new[] { Tank }, 1, Tank, Tank.Enemies[0]);
            return new Run(heroClass, biome, new RunRules(100, Curve), 1UL);
        }

        [Test]
        public void XpIntoLevel_AtTheStart_IsZeroOverTheFirstCost()
        {
            var run = CreateRun();

            Assert.AreEqual(0, run.XpIntoLevel);
            Assert.AreEqual(10, run.XpForNextLevel);
        }

        [Test]
        public void XpIntoLevel_AfterALevelUp_IsTheXpBeyondTheLevelStart()
        {
            var run = CreateRun();

            run.Play(RunStep.RegularFight);

            // 15 XP: level 2 reached at 10, then 5 into a level that costs 20.
            Assert.AreEqual(2, run.Level);
            Assert.AreEqual(5, run.XpIntoLevel);
            Assert.AreEqual(20, run.XpForNextLevel);
        }

        [Test]
        public void HeroCast_AfterABoostResolves_PointsAtTheBoostedSlot()
        {
            var run = CreateRun();
            var session = run.BeginFight(RunStep.RegularFight);

            session.Advance();

            Assert.IsFalse(session.HeroCast.IsCasting);
            Assert.AreEqual(1, session.HeroCast.Position);
            Assert.AreEqual(3, session.HeroPendingBonus(1).Damage);
            session.Cancel();
        }

        [Test]
        public void EnemyCast_DuringAFight_ShowsTheEnemyCardAndProgress()
        {
            var run = CreateRun();
            var session = run.BeginFight(RunStep.RegularFight);

            session.Advance();
            var cast = session.EnemyCast(0);

            Assert.IsTrue(cast.IsCasting);
            Assert.AreEqual("test_card_nibble", cast.CardId);
            Assert.AreEqual(1, cast.ElapsedTicks);
            Assert.AreEqual(3, cast.CastTime);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => session.EnemyCast(1));
            session.Cancel();
        }

        [Test]
        public void HeroPendingBonus_AfterASwapOfTheBoostedSlot_StaysOnTheSlot()
        {
            var run = CreateRun();
            run.AddCard(Nibble);
            Assert.AreEqual(1, run.Reserve.Count);
            var session = run.BeginFight(RunStep.RegularFight);
            session.Advance();

            session.SwapWithReserve(1, 0);

            Assert.AreEqual(3, session.HeroPendingBonus(1).Damage);
            Assert.AreEqual("test_card_nibble", session.HeroLine[1].Id);
            session.Cancel();
            Assert.IsNull(run.CurrentFight);
            Assert.AreEqual("test_card_nibble", run.Line.Last().Definition.Id);
        }
    }
}
