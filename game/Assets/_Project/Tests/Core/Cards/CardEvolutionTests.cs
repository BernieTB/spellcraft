using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Effects;
using NUnit.Framework;

namespace Game.Core.Tests.Cards
{
    /// <summary>
    /// Evolution stages of a card definition (ADR 0013, issue #79).
    /// </summary>
    public class CardEvolutionTests
    {
        // Placeholder ids and arbitrary test data, not real content or balance values.
        private const string Id = "test_card_01";
        private const int CastTime = 3;
        private const int BaseDamage = 2;
        private const int FirstCasts = 4;
        private const int FirstDamage = 6;
        private const int SecondCasts = 9;
        private const int SecondDamage = 15;

        private static NeighbourModifier NextDamage(int amount) =>
            new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, amount);

        private static CardEvolution Stage(int casts, int damage, params NeighbourModifier[] modifiers) =>
            new CardEvolution(casts, new IEffect[] { new DealDamageEffect(damage) }, modifiers);

        private static CardDefinition Evolving() => new CardDefinition(
            Id,
            CastTime,
            new IEffect[] { new DealDamageEffect(BaseDamage) },
            new NeighbourModifier[0],
            new[] { Stage(FirstCasts, FirstDamage, NextDamage(1)), Stage(SecondCasts, SecondDamage, NextDamage(3)) });

        private static int DamageOf(CardDefinition card) => ((IAmountEffect)card.Effects[0]).Amount;

        // --- A card without evolutions ---

        [Test]
        public void Constructor_WithoutEvolutions_IsAtStageZeroAndNeverEvolves()
        {
            var card = new CardDefinition(Id, CastTime, new IEffect[] { new DealDamageEffect(BaseDamage) });

            Assert.That(card.Stage, Is.EqualTo(0));
            Assert.That(card.Evolutions, Is.Empty);
            Assert.That(card.AtStage(0), Is.SameAs(card));
            Assert.That(card.StageForCasts(1000), Is.EqualTo(0));
        }

        [Test]
        public void AtStage_NoEvolutions_StageOneThrows()
        {
            var card = new CardDefinition(Id, CastTime, new IEffect[0]);

            Assert.Throws<ArgumentOutOfRangeException>(() => card.AtStage(1));
        }

        // --- Stages ---

        [Test]
        public void AtStage_EvolvedStages_ChangeEffectsAndModifiersButNotIdOrCastTime()
        {
            var card = Evolving();

            var first = card.AtStage(1);
            var second = card.AtStage(2);

            Assert.That(card.Stage, Is.EqualTo(0));
            Assert.That(DamageOf(card), Is.EqualTo(BaseDamage));
            Assert.That(card.NeighbourModifiers, Is.Empty);
            Assert.That(first.Stage, Is.EqualTo(1));
            Assert.That(DamageOf(first), Is.EqualTo(FirstDamage));
            Assert.That(first.NeighbourModifiers[0].Amount, Is.EqualTo(1));
            Assert.That(second.Stage, Is.EqualTo(2));
            Assert.That(DamageOf(second), Is.EqualTo(SecondDamage));
            Assert.That(second.NeighbourModifiers[0].Amount, Is.EqualTo(3));
            Assert.That(new[] { first.Id, second.Id }, Is.All.EqualTo(Id));
            Assert.That(new[] { first.CastTime, second.CastTime }, Is.All.EqualTo(CastTime));
        }

        [Test]
        public void AtStage_FromAnyStage_ReachesEveryStage()
        {
            var card = Evolving();

            for (var from = 0; from <= 2; from++)
            {
                for (var to = 0; to <= 2; to++)
                {
                    Assert.That(card.AtStage(from).AtStage(to), Is.SameAs(card.AtStage(to)), $"{from} to {to}");
                }
            }
        }

        [Test]
        public void AtStage_EveryStage_SharesTheSameEvolutions()
        {
            var card = Evolving();

            Assert.That(card.Evolutions, Has.Count.EqualTo(2));
            Assert.That(card.AtStage(1).Evolutions, Is.SameAs(card.Evolutions));
            Assert.That(card.AtStage(2).Evolutions, Is.SameAs(card.Evolutions));
        }

        [Test]
        public void AtStage_OutOfRange_Throws()
        {
            var card = Evolving();

            Assert.Throws<ArgumentOutOfRangeException>(() => card.AtStage(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => card.AtStage(3));
        }

        [TestCase(0, 0)]
        [TestCase(FirstCasts - 1, 0)]
        [TestCase(FirstCasts, 1)]
        [TestCase(SecondCasts - 1, 1)]
        [TestCase(SecondCasts, 2)]
        [TestCase(SecondCasts + 100, 2)]
        public void StageForCasts_GivesTheHighestStageReached(int casts, int expected)
        {
            Assert.That(Evolving().StageForCasts(casts), Is.EqualTo(expected));
            Assert.That(Evolving().AtStage(2).StageForCasts(casts), Is.EqualTo(expected));
        }

        [Test]
        public void StageForCasts_NegativeCasts_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Evolving().StageForCasts(-1));
        }

        // --- Validation ---

        [Test]
        public void Constructor_TooManyEvolutions_Throws()
        {
            var stages = new List<CardEvolution>();
            for (var i = 1; i <= CardDefinition.MaxEvolutions + 1; i++)
            {
                stages.Add(Stage(i, i));
            }

            Assert.Throws<ArgumentException>(() =>
                new CardDefinition(Id, CastTime, new IEffect[0], new NeighbourModifier[0], stages));
        }

        [Test]
        public void MaxEvolutions_IsTwo()
        {
            Assert.That(CardDefinition.MaxEvolutions, Is.EqualTo(2));
        }

        [TestCase(5, 5)]
        [TestCase(5, 3)]
        public void Constructor_CastsRequiredNotIncreasing_Throws(int first, int second)
        {
            Assert.Throws<ArgumentException>(() => new CardDefinition(
                Id,
                CastTime,
                new IEffect[0],
                new NeighbourModifier[0],
                new[] { Stage(first, 1), Stage(second, 1) }));
        }

        [Test]
        public void Constructor_NullEvolutions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CardDefinition(Id, CastTime, new IEffect[0], new NeighbourModifier[0], null));
            Assert.Throws<ArgumentNullException>(() =>
                new CardDefinition(Id, CastTime, new IEffect[0], new NeighbourModifier[0], new CardEvolution[] { null }));
        }

        [Test]
        public void CardEvolution_ZeroOrNegativeCasts_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Stage(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Stage(-1, 1));
        }

        [Test]
        public void CardEvolution_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new CardEvolution(1, null, new NeighbourModifier[0]));
            Assert.Throws<ArgumentNullException>(() => new CardEvolution(1, new IEffect[0], null));
            Assert.Throws<ArgumentNullException>(() =>
                new CardEvolution(1, new IEffect[] { null }, new NeighbourModifier[0]));
            Assert.Throws<ArgumentNullException>(() =>
                new CardEvolution(1, new IEffect[0], new NeighbourModifier[] { null }));
        }

        [Test]
        public void CardEvolution_CopiesItsLists()
        {
            var effects = new List<IEffect> { new DealDamageEffect(1) };
            var evolution = new CardEvolution(2, effects, new NeighbourModifier[0]);

            effects.Clear();

            Assert.That(evolution.Effects, Has.Count.EqualTo(1));
        }

        // --- MapForms ---

        [Test]
        public void MapForms_MapsEveryStageAndKeepsStageAndThresholds()
        {
            const int bonus = 10;
            var card = Evolving();

            CardDefinition AddBonus(CardDefinition form) => new CardDefinition(
                form.Id,
                form.CastTime,
                new IEffect[] { new DealDamageEffect(DamageOf(form) + bonus) },
                form.NeighbourModifiers);

            var mapped = card.MapForms(AddBonus);
            var mappedSecond = card.AtStage(2).MapForms(AddBonus);

            Assert.That(mapped.Stage, Is.EqualTo(0));
            Assert.That(DamageOf(mapped), Is.EqualTo(BaseDamage + bonus));
            Assert.That(DamageOf(mapped.AtStage(1)), Is.EqualTo(FirstDamage + bonus));
            Assert.That(DamageOf(mapped.AtStage(2)), Is.EqualTo(SecondDamage + bonus));
            Assert.That(mapped.Evolutions[0].CastsRequired, Is.EqualTo(FirstCasts));
            Assert.That(mapped.Evolutions[1].CastsRequired, Is.EqualTo(SecondCasts));
            Assert.That(mapped.AtStage(1).NeighbourModifiers[0].Amount, Is.EqualTo(1));
            Assert.That(mappedSecond.Stage, Is.EqualTo(2));
            Assert.That(DamageOf(mappedSecond), Is.EqualTo(SecondDamage + bonus));
        }

        [Test]
        public void MapForms_GivesEachStageAsACardWithoutEvolutions()
        {
            var seen = new List<int>();

            Evolving().MapForms(form =>
            {
                seen.Add(form.Evolutions.Count);
                return form;
            });

            Assert.That(seen, Is.EqualTo(new[] { 0, 0, 0 }));
        }

        [Test]
        public void MapForms_ChangingTheCastTime_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => Evolving().MapForms(form =>
                new CardDefinition(form.Id, form.CastTime + 1, form.Effects, form.NeighbourModifiers)));
        }

        [Test]
        public void MapForms_ChangingTheId_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => Evolving().MapForms(form =>
                new CardDefinition("test_card_02", form.CastTime, form.Effects, form.NeighbourModifiers)));
        }

        [Test]
        public void MapForms_NullMapOrNullResult_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Evolving().MapForms(null));
            Assert.Throws<ArgumentNullException>(() => Evolving().MapForms(form => null));
        }
    }
}
