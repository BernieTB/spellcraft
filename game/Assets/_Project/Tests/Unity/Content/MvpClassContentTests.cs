using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Classes;
using Game.Core.Combat;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;
using Game.Unity.Cards;
using Game.Unity.Classes;
using Game.Unity.EditorTools.Content;
using Game.Unity.Enemies;
using NUnit.Framework;
using UnityEditor;

namespace Game.Unity.Tests.Content
{
    /// <summary>
    /// Loads the MVP class (CLASS_A, ADR 0007) from disk and checks its starting deck, its card pool with the two
    /// evolution stages of each card (ADR 0013) and a headless fight with the starting deck.
    /// </summary>
    public class MvpClassContentTests
    {
        private static ClassDefinition LoadClass()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ClassAsset>(MvpClassContentGenerator.ClassPath);
            Assert.IsNotNull(asset, MvpClassContentGenerator.ClassPath);
            return asset.ToDefinition();
        }

        private static EncounterAsset LoadEncounter(string id)
        {
            return AssetDatabase.LoadAssetAtPath<EncounterAsset>($"{PlaceholderGuard.PlaceholderFolder}/{id}.asset");
        }

        private static FightParticipant CreateHero(ClassDefinition definition)
        {
            var line = new SpellLine<CardDefinition>(definition.StartingLineCapacity);
            foreach (var card in definition.StartingDeck)
            {
                line.Add(card);
            }

            return new FightParticipant(new Combatant(definition.MaxHealth, definition.StartingShield), line);
        }

        private static int Total(IReadOnlyList<IEffect> effects, IReadOnlyList<NeighbourModifier> modifiers)
        {
            return effects.OfType<IAmountEffect>().Sum(effect => effect.Amount) + modifiers.Sum(modifier => modifier.Amount);
        }

        [Test]
        public void Class_HasWorkingNameAndIsNotPlaceholderContent()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ClassAsset>(MvpClassContentGenerator.ClassPath);

            Assert.AreEqual("CLASS_A", asset.Id);
            Assert.IsFalse(asset.IsPlaceholder);
        }

        [Test]
        public void StartingDeck_HasFourCardsFillingTheStartingLine()
        {
            var definition = LoadClass();

            Assert.AreEqual(4, definition.StartingDeck.Count);
            Assert.AreEqual(4, definition.StartingLineCapacity);
        }

        [Test]
        public void StartingDeck_HasTwoDamageCardsOneWeaverAndOneShieldCard()
        {
            var deck = LoadClass().StartingDeck;

            var weavers = deck.Where(card => card.NeighbourModifiers.Count > 0).ToList();
            var plain = deck.Where(card => card.NeighbourModifiers.Count == 0).ToList();
            Assert.AreEqual(1, weavers.Count, "weaver cards");
            Assert.AreEqual(1, plain.Count(card => card.Effects.All(effect => effect is GainShieldEffect)), "shield cards");
            Assert.AreEqual(2, plain.Count(card => card.Effects.All(effect => effect is DealDamageEffect)), "damage cards");
        }

        [Test]
        public void StartingDeck_DefensiveCardGivesShieldNotHealing()
        {
            var effects = LoadClass().StartingDeck.SelectMany(card => card.Effects).ToList();

            Assert.That(effects, Has.Some.InstanceOf<GainShieldEffect>());
            Assert.That(effects, Has.None.InstanceOf<HealEffect>());
        }

        [Test]
        public void StartingDeck_WeaverBoostsADamageCardInTheLine()
        {
            var deck = LoadClass().StartingDeck.ToList();
            var weaver = deck.Single(card => card.NeighbourModifiers.Count > 0);
            var modifier = weaver.NeighbourModifiers.Single();

            Assert.AreEqual(NeighbourDirection.Next, modifier.Direction);
            Assert.AreEqual(BonusKind.Damage, modifier.Kind);
            var target = deck[(deck.IndexOf(weaver) + 1) % deck.Count];
            Assert.That(target.Effects, Has.Some.InstanceOf<DealDamageEffect>());
        }

        [Test]
        public void CardPool_HasTenDistinctCardsOutsideTheStartingDeck()
        {
            var definition = LoadClass();

            var ids = definition.CardPool.Select(card => card.Id).ToList();
            Assert.AreEqual(10, ids.Count);
            CollectionAssert.AllItemsAreUnique(ids);
            CollectionAssert.IsEmpty(ids.Intersect(definition.StartingDeck.Select(card => card.Id)));
        }

        [Test]
        public void EveryCard_HasExactlyTwoEvolutionStagesWithRisingCastThresholds()
        {
            var definition = LoadClass();

            foreach (var card in definition.StartingDeck.Concat(definition.CardPool))
            {
                Assert.AreEqual(2, card.Evolutions.Count, card.Id);
                Assert.Less(card.Evolutions[0].CastsRequired, card.Evolutions[1].CastsRequired, card.Id);
            }
        }

        [Test]
        public void EveryEvolutionStage_IsStrongerThanThePreviousOneForTheSameCard()
        {
            var definition = LoadClass();

            foreach (var card in definition.StartingDeck.Concat(definition.CardPool))
            {
                var totals = new[] { Total(card.Effects, card.NeighbourModifiers) }
                    .Concat(card.Evolutions.Select(stage => Total(stage.Effects, stage.NeighbourModifiers)))
                    .ToList();
                Assert.Less(totals[0], totals[1], card.Id);
                Assert.Less(totals[1], totals[2], card.Id);
            }
        }

        [Test]
        public void EveryCard_UsesPlaceholderWordsAndIsNotPlaceholderContent()
        {
            var definition = LoadClass();

            foreach (var card in definition.StartingDeck.Concat(definition.CardPool))
            {
                StringAssert.IsMatch("^CARD_[A-Z]$", card.Id);
                var asset = AssetDatabase.LoadAssetAtPath<CardAsset>(MvpClassContentGenerator.CardPath(card.Id));
                Assert.IsFalse(asset.IsPlaceholder, card.Id);
            }
        }

        [Test]
        public void ContentFolder_HoldsExactlyTheCardsOfTheClass()
        {
            var definition = LoadClass();
            var used = definition.StartingDeck.Concat(definition.CardPool)
                .Select(card => card.Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            var onDisk = AssetDatabase.FindAssets("t:" + nameof(CardAsset), new[] { MvpClassContentGenerator.ContentFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<CardAsset>(AssetDatabase.GUIDToAssetPath(guid)).ToDefinition().Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();

            CollectionAssert.AreEqual(MvpClassContentGenerator.CardIds.OrderBy(id => id, StringComparer.Ordinal), used);
            CollectionAssert.AreEqual(used, onDisk);
        }

        [Test]
        public void StartingDeckFight_AgainstASingleRegularEnemy_HeroWins()
        {
            var definition = LoadClass();

            var result = new Fight(
                CreateHero(definition), LoadEncounter("test_encounter_01").ToParticipants(), 10000, new Pcg32Random(1)).Run();

            Assert.AreEqual(FightWinner.Hero, result.Winner);
        }

        [Test]
        public void StartingDeckFight_AgainstEveryPlaceholderEncounter_EndsWithAWinner()
        {
            var definition = LoadClass();
            foreach (var id in new[] { "test_encounter_01", "test_encounter_02", "test_encounter_03", "test_encounter_04" })
            {
                var result = new Fight(
                    CreateHero(definition), LoadEncounter(id).ToParticipants(), 10000, new Pcg32Random(1)).Run();

                Assert.AreNotEqual(FightWinner.None, result.Winner, id);
            }
        }

        [Test]
        public void StartingDeckFight_WeaverBonusReachesTheDamageCard()
        {
            var definition = LoadClass();
            var weaverBonus = definition.StartingDeck.Single(card => card.NeighbourModifiers.Count > 0).NeighbourModifiers[0].Amount;

            var result = new Fight(
                CreateHero(definition), LoadEncounter("test_encounter_02").ToParticipants(), 10000, new Pcg32Random(1)).Run();

            Assert.That(
                result.Casts.Where(cast => cast.CasterIndex == Fight.HeroIndex),
                Has.Some.Matches<CastRecord>(cast => cast.Bonus.Damage == weaverBonus));
        }

        [Test]
        public void StartingDeckFight_SameSeed_GivesTheSameResult()
        {
            var definition = LoadClass();

            var first = new Fight(
                CreateHero(definition), LoadEncounter("test_encounter_02").ToParticipants(), 10000, new Pcg32Random(7)).Run();
            var second = new Fight(
                CreateHero(definition), LoadEncounter("test_encounter_02").ToParticipants(), 10000, new Pcg32Random(7)).Run();

            Assert.AreEqual(first.Ticks, second.Ticks);
            Assert.AreEqual(first.Casts.Count, second.Casts.Count);
        }
    }
}
