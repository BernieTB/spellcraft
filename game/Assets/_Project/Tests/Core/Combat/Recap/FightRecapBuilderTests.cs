using System;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Combat.Log;
using Game.Core.Combat.Recap;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;
using NUnit.Framework;

namespace Game.Core.Tests.Combat.Recap
{
    public class FightRecapBuilderTests
    {
        // Placeholder ids and arbitrary test data, not real content or balance values. Each fight's timeline is
        // worked out in the test's comment: casts resolve on tick = sum of cast times, the hero before enemies.
        private const ulong Seed = 11UL;
        private const int MaxTicks = 50;
        private const int Capacity = 5;

        private const int Hero = Fight.HeroIndex;
        private const int FirstEnemy = 1;

        // --- Helpers ---

        private static CardDefinition Hit(string id, int amount, int castTime = 1) =>
            new CardDefinition(id, castTime, new IEffect[] { new DealDamageEffect(amount) });

        private static CardDefinition Guard(int amount) =>
            new CardDefinition("test_guard", 1, new IEffect[] { new GainShieldEffect(amount) });

        private static CardDefinition Idle(int castTime = MaxTicks + 1) =>
            new CardDefinition("test_idle", castTime, new IEffect[0]);

        // A card with no effect that gives +amount damage to the next card.
        private static CardDefinition Booster(int amount) =>
            new CardDefinition(
                "test_boost",
                1,
                new IEffect[0],
                new[] { new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, amount) });

        private static SpellLine<CardDefinition> Line(params CardDefinition[] cards)
        {
            var line = new SpellLine<CardDefinition>(Capacity);
            foreach (var card in cards)
            {
                line.Add(card);
            }

            return line;
        }

        private static FightParticipant Participant(int health, int shield, params CardDefinition[] cards) =>
            new FightParticipant(new Combatant(health, shield), Line(cards));

        private static FightRecap Recap(int maxTicks, FightParticipant hero, params FightParticipant[] enemies) =>
            FightRecapBuilder.Build(CombatLogRecorder.Record(hero, enemies, maxTicks, new Pcg32Random(Seed)));

        private static FightRecap Recap(FightParticipant hero, params FightParticipant[] enemies) =>
            Recap(MaxTicks, hero, enemies);

        // --- Arguments and outcome ---

        [Test]
        public void Build_NullLog_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => FightRecapBuilder.Build(null));
        }

        [Test]
        public void Build_HeroWins_VictoryWithoutDefeatAnalysis()
        {
            // Tick 1: the hero kills the enemy.
            var recap = Recap(Participant(10, 0, Hit("test_hit", 5)), Participant(5, 0, Idle()));

            Assert.AreEqual(FightRecapOutcome.Victory, recap.Outcome);
            Assert.IsNull(recap.Defeat);
        }

        [Test]
        public void Build_TimeLimitWithHeroAhead_SaysOutOfTimeWithoutTurningPoint()
        {
            // Hero 10/10 against an enemy at 4/10 when the 4 ticks run out.
            var recap = Recap(4, Participant(10, 0, Hit("test_hit", 3), Guard(2)), Participant(10, 0, Idle()));

            Assert.AreEqual(FightRecapOutcome.TimeLimit, recap.Outcome);
            Assert.IsTrue(recap.RanOutOfTime);
            Assert.IsNull(recap.Defeat);
        }

        [Test]
        public void Build_TimeLimitWithHeroBehind_HasDefeatAnalysis()
        {
            // Tick 1: hero hits 1 (enemy 9/10), enemy hits 4 (hero 6/10): behind until the limit on tick 2.
            var recap = Recap(2, Participant(10, 0, Hit("test_hit", 1)), Participant(10, 0, Hit("test_enemy_hit", 4)));

            Assert.AreEqual(FightRecapOutcome.TimeLimit, recap.Outcome);
            Assert.AreEqual(1, recap.Defeat.TurningPointTick);
        }

        // --- Per card ---

        [Test]
        public void Build_CardTotals_CountCastsAndOutputByPosition()
        {
            // Ticks 1 and 3: hit 3; ticks 2 and 4: shield 2. The enemy's card never resolves.
            var recap = Recap(4, Participant(10, 0, Hit("test_hit", 3), Guard(2)), Participant(10, 0, Idle()));

            var hit = recap.Hero.Cards[0];
            var guard = recap.Hero.Cards[1];
            Assert.AreEqual((0, "test_hit", 2, 6, 0, 0), (hit.Position, hit.CardId, hit.Casts, hit.Damage, hit.Healing, hit.ShieldGained));
            Assert.AreEqual((1, "test_guard", 2, 0, 4), (guard.Position, guard.CardId, guard.Casts, guard.Damage, guard.ShieldGained));

            var idle = recap.Combatants[FirstEnemy].Cards.Single();
            Assert.AreEqual(("test_idle", 0), (idle.CardId, idle.Casts));
        }

        [Test]
        public void Build_CombatantsInFightOrder_HeroFirst()
        {
            var recap = Recap(4, Participant(10, 0, Hit("test_hit", 3)), Participant(10, 0, Idle()), Participant(10, 0, Idle()));

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, recap.Combatants.Select(c => c.Index).ToArray());
            Assert.IsTrue(recap.Hero.IsHero);
        }

        [Test]
        public void Build_CardOnlyInLog_AddedAfterStartingLine()
        {
            // The log's starting line says test_other at position 0, but the cast shows test_hit there (as when the
            // line is changed during a fight).
            var fight = new Fight(
                Participant(10, 0, Hit("test_hit", 1)),
                new[] { Participant(10, 0, Idle()) },
                1,
                new Pcg32Random(Seed));
            var snapshots = new[]
            {
                new CombatantSnapshot(Hero, 10, 10, 0, new[] { "test_other" }),
                new CombatantSnapshot(FirstEnemy, 10, 10, 0, new[] { "test_idle" }),
            };

            var recap = FightRecapBuilder.Build(CombatLog.Build(snapshots, fight.Run()));

            CollectionAssert.AreEqual(
                new[] { ("test_other", 0), ("test_hit", 1) },
                recap.Hero.Cards.Select(c => (c.CardId, c.Casts)).ToArray());
        }

        [Test]
        public void Build_UsedBonus_CountedOnReceivingCard()
        {
            // Tick 1: booster; tick 2: hit 3 + 2 bonus.
            var recap = Recap(2, Participant(10, 0, Booster(2), Hit("test_hit", 3)), Participant(20, 0, Idle()));

            var hit = recap.Hero.Cards[1];
            Assert.AreEqual(5, hit.Damage);
            Assert.AreEqual(new EffectBonus(2, 0, 0), hit.BonusUsed);
            Assert.IsTrue(hit.BonusWasted.IsNone);
            CollectionAssert.IsEmpty(recap.Hero.WastedBonuses);
        }

        [Test]
        public void Build_WastedBonus_ListedWithReason()
        {
            // Tick 1: booster; tick 2: the guard has no damage effect and wastes the +2 damage.
            var recap = Recap(2, Participant(10, 0, Booster(2), Guard(1)), Participant(20, 0, Idle()));

            var guard = recap.Hero.Cards[1];
            Assert.AreEqual(new EffectBonus(2, 0, 0), guard.BonusReceived);
            Assert.AreEqual(new EffectBonus(2, 0, 0), guard.BonusWasted);
            Assert.IsTrue(guard.BonusUsed.IsNone);

            var waste = recap.Hero.WastedBonuses.Single();
            Assert.AreEqual(
                (1, "test_guard", BonusKind.Damage, 2, WastedBonusReason.NoEffectOfKind),
                (waste.Position, waste.CardId, waste.Kind, waste.Amount, waste.Reason));
            Assert.AreEqual(new EffectBonus(2, 0, 0), recap.Hero.BonusWasted);
        }

        [Test]
        public void Build_GoldenFight_CardTotalsMatchLogEvents()
        {
            var log = GoldenFightLog();
            var recap = FightRecapBuilder.Build(log);

            foreach (var combatant in recap.Combatants)
            {
                var events = log.Events.Where(e => e.CasterIndex == combatant.Index).ToList();
                Assert.AreEqual(events.Count(e => e.Kind == CombatEventKind.CardCast), combatant.Cards.Sum(c => c.Casts));
                Assert.AreEqual(Sum(events, CombatEventKind.Damage), combatant.Cards.Sum(c => c.Damage));
                Assert.AreEqual(Sum(events, CombatEventKind.Heal), combatant.Cards.Sum(c => c.Healing));
                Assert.AreEqual(Sum(events, CombatEventKind.ShieldGain), combatant.Cards.Sum(c => c.ShieldGained));
            }

            Assert.AreEqual(FightRecapOutcome.Victory, recap.Outcome);
        }

        private static int Sum(System.Collections.Generic.IEnumerable<CombatEvent> events, CombatEventKind kind) =>
            events.Where(e => e.Kind == kind).Sum(e => e.Amount);

        // The fight of GoldenCombatLogTests: one hero against two enemies, every event kind, hero wins.
        private static CombatLog GoldenFightLog()
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

            return CombatLogRecorder.Record(hero, new[] { firstEnemy, secondEnemy }, 200, new Pcg32Random(2024UL));
        }

        // --- Turning point ---

        [Test]
        public void Build_FallsBehindOnTick1AndStays_TurningPointIs1()
        {
            // Tick 1: enemy 9/10, hero 6/10. Tick 2: 8 and 2. Tick 3: the hero dies.
            var recap = Recap(Participant(10, 0, Hit("test_hit", 1)), Participant(10, 0, Hit("test_enemy_hit", 4)));

            Assert.AreEqual(FightRecapOutcome.Defeat, recap.Outcome);
            Assert.AreEqual(1, recap.Defeat.TurningPointTick);
        }

        [Test]
        public void Build_HeroAheadThenBehind_TurningPointIsWhenItFellBehind()
        {
            // Tick 1: enemy 8/10, hero 10/10 (ahead). Tick 3: hero 4/10 (behind). Tick 6: the hero dies.
            var recap = Recap(
                Participant(10, 0, Hit("test_hit", 2), Idle(40)),
                Participant(10, 0, Hit("test_enemy_hit", 6, 3)));

            Assert.AreEqual(3, recap.Defeat.TurningPointTick);
        }

        [Test]
        public void Build_StrongerEnemy_ComparesHealthSharesNotRawHealth()
        {
            // Tick 1: enemy 70/100, hero 9/10. The hero loses 1 per tick and falls below 70% on tick 4 (6/10);
            // tick 3 (7/10 against 70/100) is a tie, not behind.
            var recap = Recap(
                Participant(10, 0, Hit("test_hit", 30), Idle(40)),
                Participant(100, 0, Hit("test_enemy_hit", 1)));

            Assert.AreEqual(4, recap.Defeat.TurningPointTick);
        }

        [Test]
        public void Build_DeadEnemy_CountsWithZeroHealthAgainstItsMaxHealth()
        {
            // Tick 1: the hero kills the first enemy and the second deals 4 (hero 6/10). Enemies 10/20: the hero is
            // ahead (counting only living enemies, 10/10, it would already be behind). Tick 2: the hero hits the
            // second enemy for 1 and takes 4: enemies 9/20, hero 2/10, behind. Tick 3: the hero dies.
            var recap = Recap(
                Participant(10, 0, Hit("test_kill", 10), Hit("test_hit", 1, 1), Idle(40)),
                Participant(10, 0, Idle()),
                Participant(10, 0, Hit("test_enemy_hit", 4)));

            Assert.AreEqual(2, recap.Defeat.TurningPointTick);
        }

        // --- Causes ---

        [Test]
        public void Build_NoWasteNoShield_MainCauseIsWeakestCard()
        {
            var recap = Recap(Participant(10, 0, Hit("test_hit", 1)), Participant(10, 0, Hit("test_enemy_hit", 4)));

            var defeat = recap.Defeat;
            Assert.AreEqual(DefeatCause.WeakestCard, defeat.MainCause);
            CollectionAssert.AreEqual(new[] { DefeatCause.WeakestCard }, defeat.Causes);
            Assert.AreEqual((0, "test_hit", 1L), (defeat.WeakestCardPosition, defeat.WeakestCardId, defeat.WeakestCardOutput));
            Assert.AreEqual((1, 1), (defeat.WindowStartTick, defeat.WindowEndTick));
            Assert.AreEqual(-1, defeat.ShieldBrokenTick);
        }

        [Test]
        public void Build_ShieldBrokenAtTurningPoint_MainCauseIsShieldBroken()
        {
            // Tick 1: the enemy's 5 damage takes the hero's 3 shield and 2 health (8/10 against 9/10).
            var recap = Recap(Participant(10, 3, Hit("test_hit", 1)), Participant(10, 0, Hit("test_enemy_hit", 5)));

            var defeat = recap.Defeat;
            Assert.AreEqual(1, defeat.TurningPointTick);
            CollectionAssert.AreEqual(new[] { DefeatCause.ShieldBroken, DefeatCause.WeakestCard }, defeat.Causes);
            Assert.AreEqual(
                (1, FirstEnemy, 0, "test_enemy_hit"),
                (defeat.ShieldBrokenTick, defeat.ShieldBreakerIndex, defeat.ShieldBreakerPosition, defeat.ShieldBreakerCardId));
        }

        [Test]
        public void Build_WastedBonusInLoop_IsMainCause_ShieldBrokenAfterTurningPointIgnored()
        {
            // Tick 1: booster; the enemy deals 4 (hero 6/10, behind). Tick 2: the guard wastes +2 damage and gains 1
            // shield, broken by the enemy's 4 after the turning point, so not a cause. Tick 3: the hero dies. The
            // loop is the hero's first two casts.
            var recap = Recap(Participant(10, 0, Booster(2), Guard(1)), Participant(10, 0, Hit("test_enemy_hit", 4)));

            var defeat = recap.Defeat;
            Assert.AreEqual(1, defeat.TurningPointTick);
            Assert.AreEqual((1, 2), (defeat.WindowStartTick, defeat.WindowEndTick));
            Assert.AreEqual(DefeatCause.WastedBonuses, defeat.MainCause);
            CollectionAssert.AreEqual(new[] { DefeatCause.WastedBonuses, DefeatCause.WeakestCard }, defeat.Causes);
            Assert.AreEqual(new EffectBonus(2, 0, 0), defeat.WastedBonus);
            Assert.AreEqual(-1, defeat.ShieldBrokenTick);
            Assert.AreEqual((0, "test_boost", 0L), (defeat.WeakestCardPosition, defeat.WeakestCardId, defeat.WeakestCardOutput));
        }

        // --- Golden defeat against two enemies ---

        [Test]
        public void Build_GoldenDefeatAgainstTwoEnemies_FullRecap()
        {
            // Hero 12/12: booster (+2 damage to next) / guard (shield 3) / hit 2, each 1 tick. Enemy 1 (6): hits 1
            // every 2 ticks. Enemy 2 (10): hits 4 every 2 ticks.
            // T1 booster. T2 guard wastes +2, shield 3; enemy 1 takes 1 shield; enemy 2 takes 2 shield and 2 health
            // (hero 10/12 against 16/16: behind, turning point) and breaks the shield. T3 hit: enemy 1 at 4.
            // T4 hero 5. T5 guard. T6 hit (enemy 1 at 2), hero 3. T7 booster. T8 guard, hero 1. T9 hit kills enemy 1.
            // T10 enemy 2 deals the last 1 health: defeat.
            var log = CombatLogRecorder.Record(
                Participant(12, 0, Booster(2), Guard(3), Hit("test_hit", 2)),
                new[]
                {
                    Participant(6, 0, Hit("test_e1", 1, 2)),
                    Participant(10, 0, Hit("test_e2", 4, 2)),
                },
                MaxTicks,
                new Pcg32Random(Seed));

            var recap = FightRecapBuilder.Build(log);

            Assert.AreEqual((FightRecapOutcome.Defeat, 10), (recap.Outcome, recap.Ticks));

            var hero = recap.Hero.Cards
                .Select(c => (c.Position, c.CardId, c.Casts, c.Damage, c.Healing, c.ShieldGained, c.BonusReceived, c.BonusWasted))
                .ToArray();
            CollectionAssert.AreEqual(
                new[]
                {
                    (0, "test_boost", 4, 0, 0, 0, EffectBonus.None, EffectBonus.None),
                    (1, "test_guard", 3, 0, 0, 9, new EffectBonus(6, 0, 0), new EffectBonus(6, 0, 0)),
                    (2, "test_hit", 3, 6, 0, 0, EffectBonus.None, EffectBonus.None),
                },
                hero);
            var heroWaste = recap.Hero.WastedBonuses.Single();
            Assert.AreEqual(
                (1, "test_guard", BonusKind.Damage, 6),
                (heroWaste.Position, heroWaste.CardId, heroWaste.Kind, heroWaste.Amount));

            var e1 = recap.Combatants[1].Cards.Single();
            var e2 = recap.Combatants[2].Cards.Single();
            Assert.AreEqual(("test_e1", 4, 4), (e1.CardId, e1.Casts, e1.Damage));
            Assert.AreEqual(("test_e2", 5, 17), (e2.CardId, e2.Casts, e2.Damage));
            CollectionAssert.IsEmpty(recap.Combatants[1].WastedBonuses);
            CollectionAssert.IsEmpty(recap.Combatants[2].WastedBonuses);

            var defeat = recap.Defeat;
            Assert.AreEqual((2, 1, 3), (defeat.TurningPointTick, defeat.WindowStartTick, defeat.WindowEndTick));
            CollectionAssert.AreEqual(
                new[] { DefeatCause.WastedBonuses, DefeatCause.ShieldBroken, DefeatCause.WeakestCard },
                defeat.Causes);
            Assert.AreEqual(DefeatCause.WastedBonuses, defeat.MainCause);
            Assert.AreEqual(new EffectBonus(2, 0, 0), defeat.WastedBonus);
            Assert.AreEqual(
                (2, 2, 0, "test_e2"),
                (defeat.ShieldBrokenTick, defeat.ShieldBreakerIndex, defeat.ShieldBreakerPosition, defeat.ShieldBreakerCardId));
            Assert.AreEqual(
                (0, "test_boost", 0L),
                (defeat.WeakestCardPosition, defeat.WeakestCardId, defeat.WeakestCardOutput));
        }

        // --- Edge cases ---

        [Test]
        public void Build_HeroWoundedAtStart_TurningPointIsTick0()
        {
            // The log's starting state has the hero at 3/10: behind before any event, and it stays behind.
            var fight = new Fight(
                Participant(10, 0, Hit("test_hit", 1)),
                new[] { Participant(10, 0, Hit("test_enemy_hit", 1)) },
                2,
                new Pcg32Random(Seed));
            var snapshots = new[]
            {
                new CombatantSnapshot(Hero, 10, 3, 0, new[] { "test_hit" }),
                new CombatantSnapshot(FirstEnemy, 10, 10, 0, new[] { "test_enemy_hit" }),
            };

            var recap = FightRecapBuilder.Build(CombatLog.Build(snapshots, fight.Run()));

            Assert.AreEqual(0, recap.Defeat.TurningPointTick);
        }

        // Hero 10: hit 1 (3 ticks) / heal 8 / idle. Enemy 10: hits 8 every 3 ticks.
        // T3: enemy 9, hero 2 (behind). T4: healed to 10 (ahead again). T6: hero 2 (behind). T9: the hero dies.
        private static FightParticipant[] RecoveringFight() => new[]
        {
            Participant(
                10,
                0,
                Hit("test_hit", 1, 3),
                new CardDefinition("test_heal", 1, new IEffect[] { new HealEffect(8) }),
                Idle(40)),
            Participant(10, 0, Hit("test_enemy_hit", 8, 3)),
        };

        [Test]
        public void Build_HeroRecoversThenFallsBehindAgain_TurningPointIsTheLastFall()
        {
            var fight = RecoveringFight();
            var recap = Recap(fight[0], fight[1]);

            Assert.AreEqual(FightRecapOutcome.Defeat, recap.Outcome);
            Assert.AreEqual(6, recap.Defeat.TurningPointTick);
        }

        [Test]
        public void Build_TimeLimitAfterHeroCameBack_NoDefeatAnalysis()
        {
            // Same fight stopped on tick 5: behind on tick 3, ahead again from tick 4.
            var fight = RecoveringFight();
            var recap = Recap(5, fight[0], fight[1]);

            Assert.AreEqual(FightRecapOutcome.TimeLimit, recap.Outcome);
            Assert.IsNull(recap.Defeat);
        }

        [Test]
        public void Build_WeakestCardTie_LowestPositionWins()
        {
            // Both hero cards deal 1 in the loop (ticks 1 and 2); the card at position 0 wins the tie although its
            // id sorts after the other.
            var recap = Recap(
                Participant(10, 0, Hit("test_hit_b", 1), Hit("test_hit_a", 1)),
                Participant(10, 0, Hit("test_enemy_hit", 4)));

            var defeat = recap.Defeat;
            Assert.AreEqual(
                (0, "test_hit_b", 1L),
                (defeat.WeakestCardPosition, defeat.WeakestCardId, defeat.WeakestCardOutput));
        }

        [Test]
        public void Build_LogWithoutEvents_TimeLimitWithZeroCasts()
        {
            var snapshots = new[]
            {
                new CombatantSnapshot(Hero, 10, 10, 0, new[] { "test_hit" }),
                new CombatantSnapshot(FirstEnemy, 10, 10, 0, new[] { "test_enemy_hit" }),
            };
            var log = CombatLog.Build(snapshots, new FightResult(FightWinner.None, 0, new CastRecord[0]));

            var recap = FightRecapBuilder.Build(log);

            Assert.AreEqual(FightRecapOutcome.TimeLimit, recap.Outcome);
            Assert.AreEqual(0, recap.Hero.Cards.Single().Casts);
            Assert.IsNull(recap.Defeat);
        }

        [Test]
        public void Build_LessThanTwoCombatants_Throws()
        {
            var snapshots = new[] { new CombatantSnapshot(Hero, 10, 10, 0, new[] { "test_hit" }) };
            var log = CombatLog.Build(snapshots, new FightResult(FightWinner.None, 0, new CastRecord[0]));

            Assert.Throws<ArgumentException>(() => FightRecapBuilder.Build(log));
        }

        [Test]
        public void Build_SameLog_SameRecap()
        {
            var log = CombatLogRecorder.Record(
                Participant(10, 0, Booster(2), Guard(1)),
                new[] { Participant(10, 0, Hit("test_enemy_hit", 4)) },
                MaxTicks,
                new Pcg32Random(Seed));

            var first = FightRecapBuilder.Build(log);
            var second = FightRecapBuilder.Build(log);

            Assert.AreEqual(first.Defeat.TurningPointTick, second.Defeat.TurningPointTick);
            CollectionAssert.AreEqual(first.Defeat.Causes, second.Defeat.Causes);
            CollectionAssert.AreEqual(
                first.Hero.Cards.Select(c => (c.CardId, c.Casts, c.Output)).ToArray(),
                second.Hero.Cards.Select(c => (c.CardId, c.Casts, c.Output)).ToArray());
        }
    }
}
