using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;
using NUnit.Framework;

namespace Game.Core.Tests.Combat
{
    public class FightTests
    {
        // Placeholder ids and arbitrary test data, not real content or balance values.
        private const ulong Seed = 1UL;
        private const int MaxTicks = 100;
        private const int Health = 10;
        private const int Capacity = 5;

        private const int Hero = Fight.HeroIndex;
        private const int FirstEnemy = 1;
        private const int SecondEnemy = 2;

        // --- Helpers ---

        private static CardDefinition Card(string id, int castTime, params IEffect[] effects) =>
            new CardDefinition(id, castTime, effects);

        // A card that does nothing: useful to observe timing only.
        private static CardDefinition Idle(string id, int castTime) => Card(id, castTime);

        private static FightParticipant Participant(int health, params CardDefinition[] cards) =>
            new FightParticipant(new Combatant(health, 0), Line(cards));

        private static SpellLine<CardDefinition> Line(params CardDefinition[] cards)
        {
            var line = new SpellLine<CardDefinition>(Capacity);
            foreach (var card in cards)
            {
                line.Add(card);
            }

            return line;
        }

        private static FightResult Run(FightParticipant hero, params FightParticipant[] enemies) =>
            new Fight(hero, enemies, MaxTicks, new Pcg32Random(Seed)).Run();

        private static FightResult RunFor(int maxTicks, FightParticipant hero, params FightParticipant[] enemies) =>
            new Fight(hero, enemies, maxTicks, new Pcg32Random(Seed)).Run();

        private static List<(int Tick, int Caster, int Position)> Timeline(FightResult result)
        {
            var timeline = new List<(int, int, int)>();
            foreach (var cast in result.Casts)
            {
                timeline.Add((cast.Tick, cast.CasterIndex, cast.Position));
            }

            return timeline;
        }

        private static List<(int Caster, int Target)> Targets(FightResult result)
        {
            var targets = new List<(int, int)>();
            foreach (var cast in result.Casts)
            {
                targets.Add((cast.CasterIndex, cast.TargetIndex));
            }

            return targets;
        }

        private static FightParticipant IdleEnemy() => Participant(Health, Idle("test_card_99", MaxTicks + 1));

        // --- Input validation ---

        [Test]
        public void FightParticipant_NullCombatant_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new FightParticipant(null, Line(Idle("test_card_01", 1))));
        }

        [Test]
        public void FightParticipant_NullSpellLine_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new FightParticipant(new Combatant(Health, 0), null));
        }

        [Test]
        public void Constructor_NullHero_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new Fight(null, new[] { IdleEnemy() }, MaxTicks, new Pcg32Random(Seed)));
        }

        [Test]
        public void Constructor_NullEnemies_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new Fight(IdleEnemy(), null, MaxTicks, new Pcg32Random(Seed)));
        }

        [Test]
        public void Constructor_NoEnemies_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => new Fight(IdleEnemy(), new FightParticipant[0], MaxTicks, new Pcg32Random(Seed)));
        }

        [Test]
        public void Constructor_NullEnemyItem_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => new Fight(IdleEnemy(), new FightParticipant[] { null }, MaxTicks, new Pcg32Random(Seed)));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_NonPositiveMaxTicks_Throws(int maxTicks)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new Fight(IdleEnemy(), new[] { IdleEnemy() }, maxTicks, new Pcg32Random(Seed)));
        }

        [Test]
        public void Constructor_NullRandom_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Fight(IdleEnemy(), new[] { IdleEnemy() }, MaxTicks, null));
        }

        [Test]
        public void Constructor_HeroWithEmptySpellLine_Throws()
        {
            var hero = new FightParticipant(new Combatant(Health, 0), Line());

            Assert.Throws<ArgumentException>(
                () => new Fight(hero, new[] { IdleEnemy() }, MaxTicks, new Pcg32Random(Seed)));
        }

        [Test]
        public void Constructor_EnemyWithEmptySpellLine_Throws()
        {
            var enemy = new FightParticipant(new Combatant(Health, 0), Line());

            Assert.Throws<ArgumentException>(
                () => new Fight(IdleEnemy(), new[] { enemy }, MaxTicks, new Pcg32Random(Seed)));
        }

        [Test]
        public void Constructor_DeadCombatant_Throws()
        {
            var enemy = IdleEnemy();
            enemy.Combatant.TakeDamage(Health);

            Assert.Throws<ArgumentException>(
                () => new Fight(IdleEnemy(), new[] { enemy }, MaxTicks, new Pcg32Random(Seed)));
        }

        [Test]
        public void Constructor_SameCombatantTwice_Throws()
        {
            var enemy = IdleEnemy();

            Assert.Throws<ArgumentException>(
                () => new Fight(IdleEnemy(), new[] { enemy, enemy }, MaxTicks, new Pcg32Random(Seed)));
        }

        [Test]
        public void Constructor_ValidInputs_ExposesInjectedRandom()
        {
            var random = new Pcg32Random(Seed);

            var fight = new Fight(IdleEnemy(), new[] { IdleEnemy() }, MaxTicks, random);

            Assert.AreSame(random, fight.Random);
        }

        [Test]
        public void Run_CalledTwice_Throws()
        {
            var fight = new Fight(IdleEnemy(), new[] { IdleEnemy() }, MaxTicks, new Pcg32Random(Seed));
            fight.Run();

            Assert.Throws<InvalidOperationException>(() => fight.Run());
        }

        // --- Loop order and cast timing ---

        [Test]
        public void Run_CardWithCastTime_ResolvesWhenCastTimeHasElapsed()
        {
            var hero = Participant(Health, Idle("test_card_01", 3));

            var result = RunFor(7, hero, IdleEnemy());

            CollectionAssert.AreEqual(new[] { (3, Hero, 0), (6, Hero, 0) }, Timeline(result));
        }

        [Test]
        public void Run_CastTimeOfOne_ResolvesEveryTick()
        {
            var hero = Participant(Health, Idle("test_card_01", 1));

            var result = RunFor(3, hero, IdleEnemy());

            CollectionAssert.AreEqual(new[] { (1, Hero, 0), (2, Hero, 0), (3, Hero, 0) }, Timeline(result));
        }

        [Test]
        public void Run_SeveralCards_CastsInOrderAndLoops()
        {
            var hero = Participant(Health, Idle("test_card_01", 1), Idle("test_card_02", 2), Idle("test_card_03", 1));

            var result = RunFor(9, hero, IdleEnemy());

            CollectionAssert.AreEqual(
                new[] { (1, Hero, 0), (3, Hero, 1), (4, Hero, 2), (5, Hero, 0), (7, Hero, 1), (8, Hero, 2), (9, Hero, 0) },
                Timeline(result));
        }

        [Test]
        public void Run_ResolvedCast_RecordsCard()
        {
            var card = Idle("test_card_01", 1);

            var result = RunFor(1, Participant(Health, card), IdleEnemy());

            Assert.AreSame(card, result.Casts[0].Card);
        }

        [Test]
        public void Run_SpellLineEditedAfterCreation_CastsOriginalLine()
        {
            var line = Line(Idle("test_card_01", 1));
            var hero = new FightParticipant(new Combatant(Health, 0), line);
            var fight = new Fight(hero, new[] { IdleEnemy() }, 2, new Pcg32Random(Seed));

            line.Add(Idle("test_card_02", 1));
            var result = fight.Run();

            Assert.AreEqual("test_card_01", result.Casts[1].Card.Id);
        }

        // --- Same-tick ordering ---

        [Test]
        public void Run_CastsCompletingOnSameTick_ResolveHeroFirstThenEnemiesInOrder()
        {
            var hero = Participant(Health, Idle("test_card_01", 2));
            var first = Participant(Health, Idle("test_card_02", 2));
            var second = Participant(Health, Idle("test_card_03", 2));

            var result = RunFor(2, hero, first, second);

            CollectionAssert.AreEqual(
                new[] { (2, Hero, 0), (2, FirstEnemy, 0), (2, SecondEnemy, 0) },
                Timeline(result));
        }

        [Test]
        public void Run_HeroKillsEnemyOnSharedTick_EnemyDoesNotResolve()
        {
            var hero = Participant(Health, Card("test_card_01", 2, new DealDamageEffect(Health)));
            var enemy = Participant(Health, Card("test_card_02", 2, new DealDamageEffect(Health)));

            var result = Run(hero, enemy);

            Assert.AreEqual((FightWinner.Hero, 1, Health), (result.Winner, result.Casts.Count, hero.Combatant.CurrentHealth));
        }

        // --- Death stops casting, end of fight ---

        [Test]
        public void Run_DeadEnemy_StopsCasting()
        {
            var hero = Participant(Health, Card("test_card_01", 3, new DealDamageEffect(Health)));
            var doomed = Participant(Health, Idle("test_card_02", 1));
            var survivor = Participant(Health, Idle("test_card_03", MaxTicks + 1));

            var result = Run(hero, doomed, survivor);

            var lastDoomedCast = 0;
            foreach (var cast in result.Casts)
            {
                if (cast.CasterIndex == FirstEnemy)
                {
                    lastDoomedCast = cast.Tick;
                }
            }

            Assert.AreEqual(2, lastDoomedCast);
        }

        [Test]
        public void Run_AllEnemiesDead_HeroWinsOnKillingTick()
        {
            var hero = Participant(Health, Card("test_card_01", 2, new DealDamageEffect(Health / 2)));

            var result = Run(hero, IdleEnemy());

            Assert.AreEqual((FightWinner.Hero, 4, false), (result.Winner, result.Ticks, result.TimedOut));
        }

        [Test]
        public void Run_HeroDead_EnemiesWinOnKillingTick()
        {
            var enemy = Participant(Health, Card("test_card_01", 3, new DealDamageEffect(Health)));

            var result = Run(IdleEnemy(), enemy);

            Assert.AreEqual((FightWinner.Enemies, 3), (result.Winner, result.Ticks));
        }

        [Test]
        public void Run_HeroKilledMidTick_LaterEnemiesDoNotResolve()
        {
            var killer = Participant(Health, Card("test_card_01", 2, new DealDamageEffect(Health)));
            var other = Participant(Health, Idle("test_card_02", 2));

            var result = Run(IdleEnemy(), killer, other);

            CollectionAssert.AreEqual(new[] { (2, FirstEnemy, 0) }, Timeline(result));
        }

        // --- Targeting (provisional rule) ---

        [Test]
        public void Run_HeroCast_TargetsFirstLivingEnemy()
        {
            var hero = Participant(Health, Card("test_card_01", 1, new DealDamageEffect(Health / 2)));
            var first = IdleEnemy();
            var second = IdleEnemy();

            var result = RunFor(3, hero, first, second);

            CollectionAssert.AreEqual(
                new[] { (Hero, FirstEnemy), (Hero, FirstEnemy), (Hero, SecondEnemy) },
                Targets(result));
        }

        [Test]
        public void Run_HeroCast_LeavesOtherEnemiesUntouched()
        {
            var hero = Participant(Health, Card("test_card_01", 1, new DealDamageEffect(1)));
            var second = IdleEnemy();

            RunFor(3, hero, IdleEnemy(), second);

            Assert.AreEqual(Health, second.Combatant.CurrentHealth);
        }

        [Test]
        public void Run_EnemyCasts_TargetHero()
        {
            var first = Participant(Health, Card("test_card_01", 1, new DealDamageEffect(1)));
            var second = Participant(Health, Card("test_card_02", 1, new DealDamageEffect(1)));
            var hero = Participant(Health, Idle("test_card_03", MaxTicks + 1));

            var result = RunFor(1, hero, first, second);

            CollectionAssert.AreEqual(new[] { (FirstEnemy, Hero), (SecondEnemy, Hero) }, Targets(result));
        }

        [Test]
        public void Run_EnemyCasts_DamageHero()
        {
            var first = Participant(Health, Card("test_card_01", 1, new DealDamageEffect(1)));
            var second = Participant(Health, Card("test_card_02", 1, new DealDamageEffect(1)));
            var hero = Participant(Health, Idle("test_card_03", MaxTicks + 1));

            RunFor(1, hero, first, second);

            Assert.AreEqual(Health - 2, hero.Combatant.CurrentHealth);
        }

        [Test]
        public void Run_SelfEffects_ApplyToCaster()
        {
            var hero = new FightParticipant(
                new Combatant(Health, 0),
                Line(Card("test_card_01", 1, new GainShieldEffect(2), new DealDamageEffect(1))));
            var enemy = IdleEnemy();

            RunFor(1, hero, enemy);

            Assert.AreEqual((2, 0, Health - 1), (hero.Combatant.Shield, enemy.Combatant.Shield, enemy.Combatant.CurrentHealth));
        }

        // --- Outcomes ---

        [Test]
        public void Run_ResolvedCast_RecordsSummedOutcome()
        {
            var enemy = new FightParticipant(new Combatant(Health, 1), Line(Idle("test_card_99", MaxTicks + 1)));
            var hero = Participant(Health, Card("test_card_01", 1, new DealDamageEffect(2), new DealDamageEffect(3)));

            var outcome = RunFor(1, hero, enemy).Casts[0].Outcome;

            Assert.AreEqual((1, 4), (outcome.Damage.AbsorbedByShield, outcome.Damage.HealthLost));
        }

        [Test]
        public void Run_ResolvedCast_RecordsEachEffectOutcome()
        {
            var hero = Participant(Health, Card("test_card_01", 1, new DealDamageEffect(2), new GainShieldEffect(3)));

            var outcomes = RunFor(1, hero, IdleEnemy()).Casts[0].EffectOutcomes;

            Assert.AreEqual((2, 0, 3), (outcomes[0].Damage.HealthLost, outcomes[0].ShieldGained, outcomes[1].ShieldGained));
        }

        // --- Timeout guard ---

        [Test]
        public void Run_NoSideCanWin_TimesOutAtMaxTicks()
        {
            var hero = Participant(Health, Idle("test_card_01", 1));
            var enemy = Participant(Health, Idle("test_card_02", 1));

            var result = RunFor(MaxTicks, hero, enemy);

            Assert.AreEqual((FightWinner.None, true, MaxTicks), (result.Winner, result.TimedOut, result.Ticks));
        }

        [Test]
        public void Run_NoSideCanWin_StopsCastingAfterMaxTicks()
        {
            var hero = Participant(Health, Idle("test_card_01", 1));

            var result = RunFor(MaxTicks, hero, IdleEnemy());

            Assert.AreEqual(MaxTicks, result.Casts[result.Casts.Count - 1].Tick);
        }

        [Test]
        public void Run_WinOnLastAllowedTick_IsNotATimeout()
        {
            var hero = Participant(Health, Card("test_card_01", 2, new DealDamageEffect(Health)));

            var result = RunFor(2, hero, IdleEnemy());

            Assert.AreEqual((FightWinner.Hero, 2), (result.Winner, result.Ticks));
        }

        // --- Determinism ---

        [Test]
        public void Run_SameSeedAndInputs_IdenticalTrace()
        {
            var first = FightTrace.Format(GoldenFightTests.CreateGoldenFight().Run());
            var second = FightTrace.Format(GoldenFightTests.CreateGoldenFight().Run());

            Assert.AreEqual(first, second);
        }
    }
}
