using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;
using NUnit.Framework;

namespace Game.Core.Tests.Combat
{
    /// <summary>
    /// Neighbour modifiers in the combat loop (ADR 0002 rules confirmed in ADR 0004, mechanism in ADR 0005).
    /// Every card here has a cast time of 1, so the hero resolves one card per tick: tick N is the hero's cast N.
    /// </summary>
    public class NeighbourModifierFightTests
    {
        // Placeholder ids and arbitrary test data, not real content or balance values.
        private const ulong Seed = 1UL;
        private const int Capacity = 5;
        private const int Sturdy = 1000;
        private const int Bonus = 10;

        private const int Hero = Fight.HeroIndex;
        private const int FirstEnemy = 1;
        private const int SecondEnemy = 2;

        // --- Helpers ---

        private static NeighbourModifier Next(BonusKind kind, int amount) =>
            new NeighbourModifier(kind, NeighbourDirection.Next, amount);

        private static NeighbourModifier Previous(BonusKind kind, int amount) =>
            new NeighbourModifier(kind, NeighbourDirection.Previous, amount);

        private static CardDefinition Card(string id, IEffect[] effects, params NeighbourModifier[] modifiers) =>
            new CardDefinition(id, 1, effects, modifiers);

        private static IEffect[] Damage(int amount) => new IEffect[] { new DealDamageEffect(amount) };

        private static IEffect[] NoEffect() => new IEffect[0];

        private static SpellLine<CardDefinition> Line(params CardDefinition[] cards)
        {
            var line = new SpellLine<CardDefinition>(Capacity);
            foreach (var card in cards)
            {
                line.Add(card);
            }

            return line;
        }

        private static FightParticipant Participant(params CardDefinition[] cards) =>
            new FightParticipant(new Combatant(Sturdy, 0), Line(cards));

        // An enemy that never resolves a card and survives everything the tests deal.
        private static FightParticipant Dummy(int maxTicks) =>
            Participant(new CardDefinition("test_card_99", maxTicks + 1, NoEffect()));

        private static FightResult Run(int maxTicks, FightParticipant hero, params FightParticipant[] enemies) =>
            new Fight(hero, enemies, maxTicks, new Pcg32Random(Seed)).Run();

        // The hero casts its line against a dummy for the given number of ticks (= casts).
        private static FightResult RunHero(int casts, params CardDefinition[] cards) =>
            Run(casts, Participant(cards), Dummy(casts));

        private static List<EffectOutcome> OutcomesOf(FightResult result, int caster)
        {
            var outcomes = new List<EffectOutcome>();
            foreach (var cast in result.Casts)
            {
                if (cast.CasterIndex == caster)
                {
                    outcomes.Add(cast.Outcome);
                }
            }

            return outcomes;
        }

        private static List<int> DamageDealt(FightResult result, int caster = Hero) =>
            OutcomesOf(result, caster).ConvertAll(outcome => outcome.Damage.Total);

        // --- Next and previous ---

        [Test]
        public void Next_ModifierOnCard_BoostsNextCardOnly()
        {
            var result = RunHero(
                3,
                Card("test_card_01", Damage(1), Next(BonusKind.Damage, Bonus)),
                Card("test_card_02", Damage(1)),
                Card("test_card_03", Damage(1)));

            CollectionAssert.AreEqual(new[] { 1, 1 + Bonus, 1 }, DamageDealt(result));
        }

        [Test]
        public void Previous_ModifierOnCard_BoostsPreviousCardOnNextLoop()
        {
            var result = RunHero(
                6,
                Card("test_card_01", Damage(1)),
                Card("test_card_02", Damage(1), Previous(BonusKind.Damage, Bonus)),
                Card("test_card_03", Damage(1)));

            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1 + Bonus, 1, 1 }, DamageDealt(result));
        }

        // --- Line ends (loop wrap) ---

        [Test]
        public void Next_ModifierOnLastCard_BoostsFirstCard()
        {
            var result = RunHero(
                4,
                Card("test_card_01", Damage(1)),
                Card("test_card_02", Damage(1)),
                Card("test_card_03", Damage(1), Next(BonusKind.Damage, Bonus)));

            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1 + Bonus }, DamageDealt(result));
        }

        [Test]
        public void Previous_ModifierOnFirstCard_BoostsLastCard()
        {
            var result = RunHero(
                3,
                Card("test_card_01", Damage(1), Previous(BonusKind.Damage, Bonus)),
                Card("test_card_02", Damage(1)),
                Card("test_card_03", Damage(1)));

            CollectionAssert.AreEqual(new[] { 1, 1, 1 + Bonus }, DamageDealt(result));
        }

        // --- Single-card line ---

        [TestCase(NeighbourDirection.Next)]
        [TestCase(NeighbourDirection.Previous)]
        public void SingleCardLine_Modifier_BoostsItsOwnNextCast(NeighbourDirection direction)
        {
            var modifier = new NeighbourModifier(BonusKind.Damage, direction, Bonus);

            var result = RunHero(3, Card("test_card_01", Damage(1), modifier));

            CollectionAssert.AreEqual(new[] { 1, 1 + Bonus, 1 + Bonus }, DamageDealt(result));
        }

        // --- Repeated looping: used up after one cast, granted again each time ---

        [Test]
        public void Looping_SourceResolvesEachLoop_NeighbourBoostedEveryLoopWithoutAccumulating()
        {
            var result = RunHero(
                6,
                Card("test_card_01", NoEffect(), Next(BonusKind.Damage, Bonus)),
                Card("test_card_02", Damage(1)));

            CollectionAssert.AreEqual(new[] { 0, 1 + Bonus, 0, 1 + Bonus, 0, 1 + Bonus }, DamageDealt(result));
        }

        [Test]
        public void Looping_BonusUsedUp_NotAppliedToFollowingCast()
        {
            // Position 1 is boosted once per loop: its bonus is used up by its cast and only comes back when
            // position 0 resolves again. Position 2 never gets it.
            var result = RunHero(
                6,
                Card("test_card_01", NoEffect(), Next(BonusKind.Damage, Bonus)),
                Card("test_card_02", Damage(1)),
                Card("test_card_03", Damage(1)));

            CollectionAssert.AreEqual(new[] { 0, 1 + Bonus, 1, 0, 1 + Bonus, 1 }, DamageDealt(result));
        }

        [Test]
        public void Looping_SameCardAtTwoPositions_BonusTrackedPerPosition()
        {
            var booster = Card("test_card_01", Damage(1), Next(BonusKind.Damage, Bonus));

            // Positions 0 and 2 hold the same card: 0 boosts 1, 2 boosts 0 (wrap). Position 2 is never boosted.
            var result = RunHero(6, booster, Card("test_card_02", Damage(1)), booster);

            CollectionAssert.AreEqual(new[] { 1, 1 + Bonus, 1, 1 + Bonus, 1 + Bonus, 1 }, DamageDealt(result));
        }

        // --- Additive stacking ---

        [Test]
        public void Stacking_SeveralModifiersOnOneCard_AddUp()
        {
            var result = RunHero(
                2,
                Card("test_card_01", NoEffect(), Next(BonusKind.Damage, 2), Next(BonusKind.Damage, 3)),
                Card("test_card_02", Damage(1)));

            CollectionAssert.AreEqual(new[] { 0, 1 + 2 + 3 }, DamageDealt(result));
        }

        [Test]
        public void Stacking_ModifiersFromBothNeighbours_AddUpOnSameCast()
        {
            // Position 1 gets +2 from position 0 (next) and +3 from position 2 (previous, from the first loop on).
            var result = RunHero(
                5,
                Card("test_card_01", NoEffect(), Next(BonusKind.Damage, 2)),
                Card("test_card_02", Damage(1)),
                Card("test_card_03", NoEffect(), Previous(BonusKind.Damage, 3)));

            CollectionAssert.AreEqual(new[] { 0, 1 + 2, 0, 0, 1 + 2 + 3 }, DamageDealt(result));
        }

        [Test]
        public void Stacking_TwoCardLineWithNextAndPrevious_BothReachTheOtherCard()
        {
            // In a two-card line, the next and the previous card are the same card.
            var result = RunHero(
                4,
                Card("test_card_01", NoEffect(), Next(BonusKind.Damage, 2), Previous(BonusKind.Damage, 3)),
                Card("test_card_02", Damage(1)));

            CollectionAssert.AreEqual(new[] { 0, 1 + 2 + 3, 0, 1 + 2 + 3 }, DamageDealt(result));
        }

        // --- Matching effect kind only ---

        [Test]
        public void Kind_DamageBonusOnCardWithoutDamageEffect_DoesNothing()
        {
            var result = RunHero(
                3,
                Card("test_card_01", NoEffect(), Next(BonusKind.Damage, Bonus)),
                Card("test_card_02", new IEffect[] { new GainShieldEffect(1) }),
                Card("test_card_03", Damage(1)));

            var outcomes = OutcomesOf(result, Hero);
            Assert.AreEqual((0, 1, 1), (outcomes[1].Damage.Total, outcomes[1].ShieldGained, outcomes[2].Damage.Total));
        }

        [Test]
        public void Kind_HealBonus_AddsToHealing()
        {
            var hero = Participant(
                Card("test_card_01", NoEffect(), Next(BonusKind.Heal, Bonus)),
                Card("test_card_02", new IEffect[] { new HealEffect(1) }));
            hero.Combatant.TakeDamage(Sturdy / 2);

            var result = Run(2, hero, Dummy(2));

            Assert.AreEqual(1 + Bonus, OutcomesOf(result, Hero)[1].Healed);
        }

        [Test]
        public void Kind_ShieldBonus_AddsToShield()
        {
            var result = RunHero(
                2,
                Card("test_card_01", NoEffect(), Next(BonusKind.Shield, Bonus)),
                Card("test_card_02", new IEffect[] { new GainShieldEffect(1) }));

            Assert.AreEqual(1 + Bonus, OutcomesOf(result, Hero)[1].ShieldGained);
        }

        [Test]
        public void Kind_OnlyMatchingBonusApplies()
        {
            var result = RunHero(
                2,
                Card("test_card_01", NoEffect(), Next(BonusKind.Heal, Bonus), Next(BonusKind.Shield, Bonus)),
                Card("test_card_02", Damage(1)));

            var outcome = OutcomesOf(result, Hero)[1];
            Assert.AreEqual((1, 0, 0), (outcome.Damage.Total, outcome.Healed, outcome.ShieldGained));
        }

        [Test]
        public void Kind_CardWithTwoDamageEffects_BonusAddedOnceToFirst()
        {
            var result = RunHero(
                2,
                Card("test_card_01", NoEffect(), Next(BonusKind.Damage, Bonus)),
                Card("test_card_02", new IEffect[] { new DealDamageEffect(1), new DealDamageEffect(1) }));

            var outcomes = result.Casts[1].EffectOutcomes;
            Assert.AreEqual((1 + Bonus, 1), (outcomes[0].Damage.Total, outcomes[1].Damage.Total));
        }

        [Test]
        public void Bonus_DoesNotChangeSharedEffectAmount()
        {
            var effect = new DealDamageEffect(1);

            RunHero(
                2,
                Card("test_card_01", NoEffect(), Next(BonusKind.Damage, Bonus)),
                Card("test_card_02", new IEffect[] { effect }));

            Assert.AreEqual(1, effect.Amount);
        }

        // --- Per combatant, dead caster ---

        [Test]
        public void Combatants_SameCardsInTwoLines_BonusesStayInTheirOwnLine()
        {
            var booster = Card("test_card_01", NoEffect(), Next(BonusKind.Damage, Bonus));
            var striker = Card("test_card_02", Damage(1));

            // The hero has the booster before the striker; the enemy only casts the striker.
            var result = Run(2, Participant(booster, striker), Participant(striker));

            CollectionAssert.AreEqual(new[] { 0, 1 + Bonus }, DamageDealt(result, Hero));
            CollectionAssert.AreEqual(new[] { 1, 1 }, DamageDealt(result, FirstEnemy));
        }

        [Test]
        public void DeadCaster_PendingBonusNeverUsedAndDoesNotReachOtherCombatants()
        {
            // Both enemies cast the same line. The first enemy grants its bonus on tick 1, then the hero kills it
            // on tick 2 before its boosted card resolves. The second enemy keeps getting only its own bonus.
            var booster = Card("test_card_01", Damage(1), Next(BonusKind.Damage, Bonus));
            var striker = Card("test_card_02", Damage(1));
            var hero = Participant(
                new CardDefinition("test_card_03", 2, Damage(Sturdy)),
                new CardDefinition("test_card_04", 100, NoEffect()));
            var doomed = Participant(booster, striker);
            var survivor = Participant(booster, striker);

            var result = Run(4, hero, doomed, survivor);

            Assert.AreEqual(
                (1, 4, true),
                (DamageDealt(result, FirstEnemy).Count, DamageDealt(result, SecondEnemy).Count, doomed.Combatant.IsDead));
            CollectionAssert.AreEqual(new[] { 1, 1 + Bonus, 1, 1 + Bonus }, DamageDealt(result, SecondEnemy));
        }
    }
}
