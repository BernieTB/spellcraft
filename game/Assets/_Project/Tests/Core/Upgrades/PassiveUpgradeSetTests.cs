using System;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;
using Game.Core.Upgrades;
using NUnit.Framework;

namespace Game.Core.Tests.Upgrades
{
    /// <summary>
    /// Passive upgrades applied to the hero (ADR 0012): one test per kind, stacking, and fights showing that the
    /// upgraded hero really fights with the upgraded values.
    /// </summary>
    public class PassiveUpgradeSetTests
    {
        // Placeholder ids and arbitrary test data, not real content or balance values.
        private const ulong Seed = 1UL;
        private const int Capacity = 4;
        private const int Health = 20;
        private const int Shield = 2;
        private const int Damage = 5;
        private const int Heal = 4;
        private const int GainShield = 6;
        private const int ModifierAmount = 2;
        private const int X = 3;

        // --- Helpers ---

        private static PassiveUpgrade MaxHealth(int amount) =>
            new PassiveUpgrade("test_upgrade_health", PassiveUpgradeKind.MaxHealth, amount);

        private static PassiveUpgrade StartingShield(int amount) =>
            new PassiveUpgrade("test_upgrade_shield", PassiveUpgradeKind.StartingShield, amount);

        private static PassiveUpgrade EffectAmount(BonusKind kind, int amount) =>
            new PassiveUpgrade("test_upgrade_effect", PassiveUpgradeKind.EffectAmount, kind, amount);

        private static PassiveUpgrade NeighbourBonus(int amount) =>
            new PassiveUpgrade("test_upgrade_neighbour", PassiveUpgradeKind.NeighbourBonus, amount);

        private static PassiveUpgradeSet Set(params PassiveUpgrade[] upgrades)
        {
            var set = PassiveUpgradeSet.Empty;
            foreach (var upgrade in upgrades)
            {
                set = set.With(upgrade);
            }

            return set;
        }

        private static CardDefinition MixedCard() =>
            new CardDefinition(
                "test_card_01",
                2,
                new IEffect[] { new DealDamageEffect(Damage), new HealEffect(Heal), new GainShieldEffect(GainShield), new DealDamageEffect(Damage) },
                new[] { new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, ModifierAmount) });

        private static SpellLine<CardDefinition> Line(params CardDefinition[] cards)
        {
            var line = new SpellLine<CardDefinition>(Capacity);
            foreach (var card in cards)
            {
                line.Add(card);
            }

            return line;
        }

        private static FightParticipant Hero(params CardDefinition[] cards) =>
            new FightParticipant(new Combatant(Health, Shield), Line(cards));

        private static int AmountOf(IEffect effect) => ((IAmountEffect)effect).Amount;

        // --- Totals and stacking ---

        [Test]
        public void Empty_HasNoUpgrade()
        {
            var set = PassiveUpgradeSet.Empty;

            Assert.IsTrue(set.IsEmpty);
            Assert.AreEqual(0, set.MaxHealthBonus);
            Assert.AreEqual(0, set.StartingShieldBonus);
            Assert.IsTrue(set.EffectBonus.IsNone);
            Assert.AreEqual(0, set.NeighbourBonus);
        }

        [Test]
        public void With_SameUpgradeTwice_Stacks()
        {
            var upgrade = EffectAmount(BonusKind.Damage, X);

            var set = Set(upgrade, upgrade);

            Assert.AreEqual(2, set.Upgrades.Count);
            Assert.AreEqual(2 * X, set.EffectBonus.Damage);
        }

        [Test]
        public void With_EveryKind_AddsUpEachTotalSeparately()
        {
            var set = Set(
                MaxHealth(1),
                MaxHealth(2),
                StartingShield(3),
                EffectAmount(BonusKind.Heal, 4),
                EffectAmount(BonusKind.Shield, 5),
                NeighbourBonus(6),
                NeighbourBonus(7));

            Assert.AreEqual(3, set.MaxHealthBonus);
            Assert.AreEqual(3, set.StartingShieldBonus);
            Assert.AreEqual(new EffectBonus(0, 4, 5), set.EffectBonus);
            Assert.AreEqual(13, set.NeighbourBonus);
        }

        [Test]
        public void With_DoesNotChangeTheOriginalSet()
        {
            var original = Set(MaxHealth(X));

            original.With(MaxHealth(X));

            Assert.AreEqual(1, original.Upgrades.Count);
            Assert.AreEqual(X, original.MaxHealthBonus);
        }

        [Test]
        public void With_NullUpgrade_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => PassiveUpgradeSet.Empty.With(null));
        }

        // --- Hero stats ---

        [Test]
        public void ApplyTo_MaxHealth_RaisesMaxAndCurrentHealth()
        {
            var hero = Set(MaxHealth(X), MaxHealth(X)).ApplyTo(Hero(MixedCard()));

            Assert.AreEqual(Health + 2 * X, hero.Combatant.MaxHealth);
            Assert.AreEqual(Health + 2 * X, hero.Combatant.CurrentHealth);
            Assert.AreEqual(Shield, hero.Combatant.Shield);
        }

        [Test]
        public void ApplyTo_StartingShield_RaisesShield()
        {
            var hero = Set(StartingShield(X)).ApplyTo(Hero(MixedCard()));

            Assert.AreEqual(Shield + X, hero.Combatant.Shield);
            Assert.AreEqual(Health, hero.Combatant.MaxHealth);
        }

        [Test]
        public void ApplyTo_Participant_DoesNotChangeTheGivenHero()
        {
            var original = Hero(MixedCard());

            Set(MaxHealth(X), EffectAmount(BonusKind.Damage, X)).ApplyTo(original);

            Assert.AreEqual(Health, original.Combatant.MaxHealth);
            Assert.AreEqual(Damage, AmountOf(original.SpellLine[0].Effects[0]));
        }

        [Test]
        public void ApplyTo_EmptySet_ReturnsTheSameHero()
        {
            var hero = Hero(MixedCard());

            Assert.AreSame(hero, PassiveUpgradeSet.Empty.ApplyTo(hero));
        }

        [Test]
        public void ApplyTo_WoundedHero_Throws()
        {
            var hero = Hero(MixedCard());
            hero.Combatant.TakeDamage(Shield + 1);

            Assert.Throws<ArgumentException>(() => Set(MaxHealth(X)).ApplyTo(hero));
        }

        [Test]
        public void ApplyTo_EmptySetOnWoundedHero_Throws()
        {
            var hero = Hero(MixedCard());
            hero.Combatant.TakeDamage(Shield + 1);

            Assert.Throws<ArgumentException>(() => PassiveUpgradeSet.Empty.ApplyTo(hero));
        }

        [Test]
        public void ApplyTo_NullHero_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Set(MaxHealth(X)).ApplyTo((FightParticipant)null));
        }

        // --- Cards ---

        [Test]
        public void ApplyTo_EffectAmount_RaisesEveryEffectOfItsKindOnly()
        {
            var card = Set(EffectAmount(BonusKind.Damage, X)).ApplyTo(MixedCard());

            Assert.AreEqual(Damage + X, AmountOf(card.Effects[0]));
            Assert.AreEqual(Heal, AmountOf(card.Effects[1]));
            Assert.AreEqual(GainShield, AmountOf(card.Effects[2]));
            Assert.AreEqual(Damage + X, AmountOf(card.Effects[3]));
        }

        [TestCase(BonusKind.Damage)]
        [TestCase(BonusKind.Heal)]
        [TestCase(BonusKind.Shield)]
        public void ApplyTo_EffectAmount_LeavesEffectsOfOtherKindsUnchanged(BonusKind kind)
        {
            var original = MixedCard();

            var card = Set(EffectAmount(kind, X)).ApplyTo(original);

            for (var i = 0; i < original.Effects.Count; i++)
            {
                var effect = (IAmountEffect)original.Effects[i];
                if (effect.Kind != kind)
                {
                    Assert.AreEqual(effect.Amount, AmountOf(card.Effects[i]), $"Effect {i} ({effect.Kind}) changed.");
                }
            }
        }

        [Test]
        public void ApplyTo_ZeroAmounts_BecomeX()
        {
            var original = new CardDefinition(
                "test_card_03",
                1,
                new IEffect[] { new DealDamageEffect(0) },
                new[] { new NeighbourModifier(BonusKind.Heal, NeighbourDirection.Previous, 0) });

            var card = Set(EffectAmount(BonusKind.Damage, X), NeighbourBonus(X)).ApplyTo(original);

            Assert.AreEqual(X, AmountOf(card.Effects.Single()));
            Assert.AreEqual(X, card.NeighbourModifiers.Single().Amount);
        }

        [TestCase(BonusKind.Heal, 1)]
        [TestCase(BonusKind.Shield, 2)]
        public void ApplyTo_EffectAmount_RaisesTheMatchingEffect(BonusKind kind, int effectIndex)
        {
            var original = MixedCard();

            var card = Set(EffectAmount(kind, X)).ApplyTo(original);

            Assert.AreEqual(AmountOf(original.Effects[effectIndex]) + X, AmountOf(card.Effects[effectIndex]));
            Assert.IsInstanceOf(original.Effects[effectIndex].GetType(), card.Effects[effectIndex]);
        }

        [Test]
        public void ApplyTo_NeighbourBonus_RaisesEveryModifierAndKeepsItsKindAndDirection()
        {
            var card = Set(NeighbourBonus(X), NeighbourBonus(X)).ApplyTo(MixedCard());

            var modifier = card.NeighbourModifiers.Single();
            Assert.AreEqual(ModifierAmount + 2 * X, modifier.Amount);
            Assert.AreEqual(BonusKind.Damage, modifier.Kind);
            Assert.AreEqual(NeighbourDirection.Next, modifier.Direction);
        }

        [Test]
        public void ApplyTo_Card_KeepsIdCastTimeAndEffectOrder()
        {
            var original = MixedCard();

            var card = Set(EffectAmount(BonusKind.Heal, X)).ApplyTo(original);

            Assert.AreEqual(original.Id, card.Id);
            Assert.AreEqual(original.CastTime, card.CastTime);
            CollectionAssert.AreEqual(
                original.Effects.Select(effect => effect.GetType()),
                card.Effects.Select(effect => effect.GetType()));
        }

        [Test]
        public void ApplyTo_StatOnlyUpgrades_KeepTheSameCard()
        {
            var original = MixedCard();

            Assert.AreSame(original, Set(MaxHealth(X), StartingShield(X)).ApplyTo(original));
        }

        [Test]
        public void ApplyTo_EffectWithoutAmount_IsKeptUnchanged()
        {
            var other = new NoAmountEffect();
            var original = new CardDefinition("test_card_02", 1, new IEffect[] { other });

            var card = Set(EffectAmount(BonusKind.Damage, X)).ApplyTo(original);

            Assert.AreSame(other, card.Effects.Single());
        }

        [Test]
        public void ApplyTo_Line_KeepsCapacityAndOrder()
        {
            var first = MixedCard();
            var second = new CardDefinition("test_card_02", 1, new IEffect[] { new HealEffect(Heal) });

            var line = Set(EffectAmount(BonusKind.Heal, X)).ApplyTo(Line(first, second));

            Assert.AreEqual(Capacity, line.Capacity);
            CollectionAssert.AreEqual(new[] { first.Id, second.Id }, line.Cards.Select(card => card.Id));
            Assert.AreEqual(Heal + X, AmountOf(line[1].Effects[0]));
        }

        // --- In a fight ---

        [Test]
        public void Fight_EffectAmountUpgrade_DealsTheUpgradedDamage()
        {
            var strike = new CardDefinition("test_card_01", 1, new IEffect[] { new DealDamageEffect(Damage) });
            var hero = Set(EffectAmount(BonusKind.Damage, X)).ApplyTo(Hero(strike));
            var idle = new CardDefinition("test_card_99", 100, new IEffect[0]);
            var enemy = new FightParticipant(new Combatant(1000, 0), Line(idle));

            var result = new Fight(hero, new[] { enemy }, 1, new Pcg32Random(Seed)).Run();

            Assert.AreEqual(Damage + X, result.Casts.Single().Outcome.Damage.HealthLost);
        }

        [Test]
        public void Fight_NeighbourBonusUpgrade_RaisesTheBonusTheNextCardReceives()
        {
            var weaver = new CardDefinition(
                "test_card_01",
                1,
                new IEffect[0],
                new[] { new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, ModifierAmount) });
            var strike = new CardDefinition("test_card_02", 1, new IEffect[] { new DealDamageEffect(Damage) });
            var hero = Set(NeighbourBonus(X)).ApplyTo(Hero(weaver, strike));
            var idle = new CardDefinition("test_card_99", 100, new IEffect[0]);
            var enemy = new FightParticipant(new Combatant(1000, 0), Line(idle));

            var result = new Fight(hero, new[] { enemy }, 2, new Pcg32Random(Seed)).Run();

            var strikeCast = result.Casts[1];
            Assert.AreEqual(ModifierAmount + X, strikeCast.Bonus.Damage);
            Assert.AreEqual(Damage + ModifierAmount + X, strikeCast.Outcome.Damage.HealthLost);
        }

        [Test]
        public void Fight_StatUpgrades_HeroStartsWithUpgradedHealthAndShield()
        {
            var idle = new CardDefinition("test_card_99", 100, new IEffect[0]);
            var hit = new CardDefinition("test_card_98", 1, new IEffect[] { new DealDamageEffect(Shield + X + 1) });
            var hero = Set(MaxHealth(X), StartingShield(X)).ApplyTo(Hero(idle));
            var enemy = new FightParticipant(new Combatant(1000, 0), Line(hit));

            var result = new Fight(hero, new[] { enemy }, 1, new Pcg32Random(Seed)).Run();

            var damage = result.Casts.Single().Outcome.Damage;
            Assert.AreEqual(Shield + X, damage.AbsorbedByShield);
            Assert.AreEqual(Health + X - 1, hero.Combatant.CurrentHealth);
        }

        [Test]
        public void Fight_CardSharedWithEnemy_EnemyKeepsTheBaseAmount()
        {
            var strike = new CardDefinition("test_card_01", 1, new IEffect[] { new DealDamageEffect(Damage) });
            var hero = Set(EffectAmount(BonusKind.Damage, X)).ApplyTo(Hero(strike));
            var enemy = new FightParticipant(new Combatant(1000, 0), Line(strike));

            var result = new Fight(hero, new[] { enemy }, 1, new Pcg32Random(Seed)).Run();

            Assert.AreEqual(Damage, AmountOf(strike.Effects.Single()));
            var heroCast = result.Casts.Single(cast => cast.CasterIndex == Fight.HeroIndex);
            var enemyCast = result.Casts.Single(cast => cast.CasterIndex != Fight.HeroIndex);
            Assert.AreEqual(Damage + X, heroCast.Outcome.Damage.HealthLost);
            Assert.AreEqual(Damage, enemyCast.Outcome.Damage.AbsorbedByShield + enemyCast.Outcome.Damage.HealthLost);
        }

        // An effect with no amount, standing for future effect types that are not IAmountEffect.
        private sealed class NoAmountEffect : IEffect
        {
            public EffectOutcome Apply(EffectContext context) => EffectOutcome.None;
        }

        // --- Evolving cards (ADR 0013) ---

        private static CardDefinition EvolvingCard(int stage = 0)
        {
            var card = new CardDefinition(
                "test_card_01",
                2,
                new IEffect[] { new DealDamageEffect(Damage) },
                new[] { new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, ModifierAmount) },
                new[]
                {
                    new CardEvolution(
                        3,
                        new IEffect[] { new DealDamageEffect(Damage * 2) },
                        new[] { new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, ModifierAmount * 2) }),
                    new CardEvolution(
                        7,
                        new IEffect[] { new DealDamageEffect(Damage * 3) },
                        new[] { new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, ModifierAmount * 3) }),
                });
            return card.AtStage(stage);
        }

        [Test]
        public void ApplyTo_EvolvingCard_UpgradesEveryStageAndKeepsTheEvolutions()
        {
            var upgraded = Set(EffectAmount(BonusKind.Damage, X), NeighbourBonus(1)).ApplyTo(EvolvingCard());

            Assert.That(upgraded.Evolutions, Has.Count.EqualTo(2));
            Assert.That(upgraded.Evolutions[0].CastsRequired, Is.EqualTo(3));
            Assert.That(upgraded.Evolutions[1].CastsRequired, Is.EqualTo(7));
            Assert.That(((IAmountEffect)upgraded.Effects[0]).Amount, Is.EqualTo(Damage + X));
            Assert.That(((IAmountEffect)upgraded.AtStage(1).Effects[0]).Amount, Is.EqualTo(Damage * 2 + X));
            Assert.That(((IAmountEffect)upgraded.AtStage(2).Effects[0]).Amount, Is.EqualTo(Damage * 3 + X));
            Assert.That(upgraded.AtStage(2).NeighbourModifiers[0].Amount, Is.EqualTo(ModifierAmount * 3 + 1));
            Assert.That(upgraded.AtStage(1).CastTime, Is.EqualTo(2));
        }

        [Test]
        public void ApplyTo_EvolvingCardAtAnEvolvedStage_KeepsItsStage()
        {
            var upgraded = Set(EffectAmount(BonusKind.Damage, X)).ApplyTo(EvolvingCard(stage: 1));

            Assert.That(upgraded.Stage, Is.EqualTo(1));
            Assert.That(((IAmountEffect)upgraded.Effects[0]).Amount, Is.EqualTo(Damage * 2 + X));
        }

        [Test]
        public void ApplyTo_EvolvingCardWithoutUpgradesThatChangeCards_IsTheSameInstance()
        {
            var card = EvolvingCard();

            Assert.That(Set(MaxHealth(5)).ApplyTo(card), Is.SameAs(card));
        }

        [Test]
        public void ApplyTo_Hero_EvolvedFormsOfTheLineAreUpgradedToo()
        {
            var hero = Hero(EvolvingCard());

            var upgraded = Set(EffectAmount(BonusKind.Damage, X)).ApplyTo(hero);

            Assert.That(((IAmountEffect)upgraded.SpellLine[0].AtStage(2).Effects[0]).Amount, Is.EqualTo(Damage * 3 + X));
        }
    }
}
