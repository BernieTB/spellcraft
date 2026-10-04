using System;
using Game.Core.Cards;
using Game.Core.Effects;
using Game.Unity.Cards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Tests.Cards
{
    public class CardAssetTests
    {
        // Placeholder id and arbitrary test data, not real content or balance values.
        private const string Id = "test_card_01";
        private const int CastTime = 2;

        private CardAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<CardAsset>();
            _asset.name = "TestCardAsset";
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_asset);
        }

        /// <summary>
        /// Writes the asset through its serialized fields, as the inspector would.
        /// </summary>
        private void Author(string id, int castTime, params (int kind, int amount)[] effects)
        {
            var serialized = new SerializedObject(_asset);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_castTime").intValue = castTime;

            var list = serialized.FindProperty("_effects");
            list.arraySize = effects.Length;
            for (var i = 0; i < effects.Length; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_kind").intValue = effects[i].kind;
                entry.FindPropertyRelative("_amount").intValue = effects[i].amount;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Writes the neighbour modifier list through its serialized fields, as the inspector would.
        /// </summary>
        private void AuthorModifiers(params (int kind, int direction, int amount)[] modifiers)
        {
            var serialized = new SerializedObject(_asset);
            var list = serialized.FindProperty("_neighbourModifiers");
            list.arraySize = modifiers.Length;
            for (var i = 0; i < modifiers.Length; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_kind").intValue = modifiers[i].kind;
                entry.FindPropertyRelative("_direction").intValue = modifiers[i].direction;
                entry.FindPropertyRelative("_amount").intValue = modifiers[i].amount;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void ToDefinition_ValidAsset_CopiesId()
        {
            Author(Id, CastTime);

            Assert.AreEqual(Id, _asset.ToDefinition().Id);
        }

        [Test]
        public void ToDefinition_ValidAsset_CopiesCastTime()
        {
            Author(Id, CastTime);

            Assert.AreEqual(CastTime, _asset.ToDefinition().CastTime);
        }

        [Test]
        public void ToDefinition_NoEffects_HasEmptyEffects()
        {
            Author(Id, CastTime);

            Assert.IsEmpty(_asset.ToDefinition().Effects);
        }

        [Test]
        public void ToDefinition_DealDamageEntry_CreatesDealDamageEffectWithAmount()
        {
            Author(Id, CastTime, ((int)EffectKind.DealDamage, 3));

            var effect = (DealDamageEffect)_asset.ToDefinition().Effects[0];

            Assert.AreEqual(3, effect.Amount);
        }

        [Test]
        public void ToDefinition_HealEntry_CreatesHealEffectWithAmount()
        {
            Author(Id, CastTime, ((int)EffectKind.Heal, 4));

            var effect = (HealEffect)_asset.ToDefinition().Effects[0];

            Assert.AreEqual(4, effect.Amount);
        }

        [Test]
        public void ToDefinition_GainShieldEntry_CreatesGainShieldEffectWithAmount()
        {
            Author(Id, CastTime, ((int)EffectKind.GainShield, 5));

            var effect = (GainShieldEffect)_asset.ToDefinition().Effects[0];

            Assert.AreEqual(5, effect.Amount);
        }

        [Test]
        public void ToDefinition_SeveralEntries_KeepsOrder()
        {
            Author(Id, CastTime,
                ((int)EffectKind.Heal, 1),
                ((int)EffectKind.DealDamage, 1),
                ((int)EffectKind.GainShield, 1));

            var effects = _asset.ToDefinition().Effects;

            CollectionAssert.AreEqual(
                new[] { typeof(HealEffect), typeof(DealDamageEffect), typeof(GainShieldEffect) },
                new[] { effects[0].GetType(), effects[1].GetType(), effects[2].GetType() });
        }

        [Test]
        public void ToDefinition_EmptyId_Throws()
        {
            Author(string.Empty, CastTime);

            Assert.Throws<InvalidOperationException>(() => _asset.ToDefinition());
        }

        [Test]
        public void ToDefinition_ZeroCastTime_Throws()
        {
            Author(Id, 0);

            Assert.Throws<InvalidOperationException>(() => _asset.ToDefinition());
        }

        [Test]
        public void ToDefinition_NegativeAmount_Throws()
        {
            Author(Id, CastTime, ((int)EffectKind.DealDamage, -1));

            Assert.Throws<InvalidOperationException>(() => _asset.ToDefinition());
        }

        [Test]
        public void ToDefinition_UnknownEffectKind_Throws()
        {
            Author(Id, CastTime, (999, 1));

            Assert.Throws<InvalidOperationException>(() => _asset.ToDefinition());
        }

        [Test]
        public void ToDefinition_InvalidAsset_MessageNamesAsset()
        {
            Author(string.Empty, CastTime);

            var exception = Assert.Throws<InvalidOperationException>(() => _asset.ToDefinition());

            StringAssert.Contains(_asset.name, exception.Message);
        }

        [Test]
        public void ToDefinition_NoModifiers_HasEmptyNeighbourModifiers()
        {
            Author(Id, CastTime);

            Assert.IsEmpty(_asset.ToDefinition().NeighbourModifiers);
        }

        [TestCase(BonusKind.Damage, NeighbourDirection.Next, 2)]
        [TestCase(BonusKind.Heal, NeighbourDirection.Previous, 3)]
        [TestCase(BonusKind.Shield, NeighbourDirection.Next, 4)]
        public void ToDefinition_ModifierEntry_CreatesModifierWithKindDirectionAndAmount(
            BonusKind kind, NeighbourDirection direction, int amount)
        {
            Author(Id, CastTime);
            AuthorModifiers(((int)kind, (int)direction, amount));

            var modifier = _asset.ToDefinition().NeighbourModifiers[0];

            Assert.AreEqual((kind, direction, amount), (modifier.Kind, modifier.Direction, modifier.Amount));
        }

        [Test]
        public void ToDefinition_SeveralModifierEntries_KeepsOrder()
        {
            Author(Id, CastTime);
            AuthorModifiers(
                ((int)BonusKind.Shield, (int)NeighbourDirection.Previous, 1),
                ((int)BonusKind.Damage, (int)NeighbourDirection.Next, 2));

            var modifiers = _asset.ToDefinition().NeighbourModifiers;

            CollectionAssert.AreEqual(
                new[] { (BonusKind.Shield, 1), (BonusKind.Damage, 2) },
                new[] { (modifiers[0].Kind, modifiers[0].Amount), (modifiers[1].Kind, modifiers[1].Amount) });
        }

        [Test]
        public void ToDefinition_NegativeModifierAmount_Throws()
        {
            Author(Id, CastTime);
            AuthorModifiers(((int)BonusKind.Damage, (int)NeighbourDirection.Next, -1));

            Assert.Throws<InvalidOperationException>(() => _asset.ToDefinition());
        }

        [Test]
        public void ToDefinition_UnknownModifierKind_Throws()
        {
            Author(Id, CastTime);
            AuthorModifiers((999, (int)NeighbourDirection.Next, 1));

            Assert.Throws<InvalidOperationException>(() => _asset.ToDefinition());
        }

        [Test]
        public void ToDefinition_UnknownModifierDirection_Throws()
        {
            Author(Id, CastTime);
            AuthorModifiers(((int)BonusKind.Damage, 999, 1));

            Assert.Throws<InvalidOperationException>(() => _asset.ToDefinition());
        }

        [Test]
        public void IsPlaceholder_NewAsset_IsFalse()
        {
            Assert.IsFalse(_asset.IsPlaceholder);
        }

        [Test]
        public void IsPlaceholder_FlagSet_IsTrue()
        {
            var serialized = new SerializedObject(_asset);
            serialized.FindProperty("_isPlaceholder").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsTrue(_asset.IsPlaceholder);
        }

        [Test]
        public void ToDefinition_NewAsset_IsInvalidUntilAuthored()
        {
            Assert.Throws<InvalidOperationException>(() => _asset.ToDefinition());
        }

        // --- Evolutions (ADR 0013) ---

        /// <summary>
        /// Writes the evolution list through its serialized fields, as the inspector would. Each stage has its casts
        /// required and the damage of its single effect.
        /// </summary>
        private void AuthorEvolutions(params (int casts, int damage)[] stages)
        {
            var serialized = new SerializedObject(_asset);
            var list = serialized.FindProperty("_evolutions");
            list.arraySize = stages.Length;
            for (var i = 0; i < stages.Length; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_castsRequired").intValue = stages[i].casts;
                var effects = entry.FindPropertyRelative("_effects");
                effects.arraySize = 1;
                effects.GetArrayElementAtIndex(0).FindPropertyRelative("_kind").intValue = (int)EffectKind.DealDamage;
                effects.GetArrayElementAtIndex(0).FindPropertyRelative("_amount").intValue = stages[i].damage;
                var modifiers = entry.FindPropertyRelative("_neighbourModifiers");
                modifiers.arraySize = 1;
                modifiers.GetArrayElementAtIndex(0).FindPropertyRelative("_kind").intValue = (int)BonusKind.Heal;
                modifiers.GetArrayElementAtIndex(0).FindPropertyRelative("_direction").intValue = (int)NeighbourDirection.Previous;
                modifiers.GetArrayElementAtIndex(0).FindPropertyRelative("_amount").intValue = stages[i].damage + 1;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void ToDefinition_NoEvolutions_HasNone()
        {
            Author(Id, CastTime, ((int)EffectKind.DealDamage, 3));

            var card = _asset.ToDefinition();

            Assert.That(card.Evolutions, Is.Empty);
            Assert.That(card.Stage, Is.EqualTo(0));
        }

        [Test]
        public void ToDefinition_TwoEvolutions_ConvertsEachStage()
        {
            Author(Id, CastTime, ((int)EffectKind.DealDamage, 3));
            AuthorEvolutions((2, 6), (5, 9));

            var card = _asset.ToDefinition();

            Assert.That(card.Evolutions, Has.Count.EqualTo(2));
            Assert.That(card.Evolutions[0].CastsRequired, Is.EqualTo(2));
            Assert.That(card.Evolutions[1].CastsRequired, Is.EqualTo(5));
            Assert.That(((IAmountEffect)card.AtStage(1).Effects[0]).Amount, Is.EqualTo(6));
            Assert.That(((IAmountEffect)card.AtStage(2).Effects[0]).Amount, Is.EqualTo(9));
            Assert.That(card.AtStage(2).NeighbourModifiers[0].Kind, Is.EqualTo(BonusKind.Heal));
            Assert.That(card.AtStage(2).NeighbourModifiers[0].Direction, Is.EqualTo(NeighbourDirection.Previous));
            Assert.That(card.AtStage(2).NeighbourModifiers[0].Amount, Is.EqualTo(10));
        }

        [Test]
        public void ToDefinition_Evolutions_NeverChangeTheIdOrTheCastTime()
        {
            Author(Id, CastTime, ((int)EffectKind.DealDamage, 3));
            AuthorEvolutions((2, 6), (5, 9));

            var card = _asset.ToDefinition();

            Assert.That(card.AtStage(1).Id, Is.EqualTo(Id));
            Assert.That(card.AtStage(2).CastTime, Is.EqualTo(CastTime));
        }

        [Test]
        public void ToDefinition_EvolutionsNotIncreasing_ThrowsNamingTheAsset()
        {
            Author(Id, CastTime, ((int)EffectKind.DealDamage, 3));
            AuthorEvolutions((5, 6), (5, 9));

            var exception = Assert.Throws<InvalidOperationException>(() => _asset.ToDefinition());

            StringAssert.Contains("TestCardAsset", exception.Message);
        }

        [Test]
        public void ToDefinition_ThreeEvolutions_ThrowsNamingTheAsset()
        {
            Author(Id, CastTime, ((int)EffectKind.DealDamage, 3));
            AuthorEvolutions((1, 4), (2, 5), (3, 6));

            var exception = Assert.Throws<InvalidOperationException>(() => _asset.ToDefinition());

            StringAssert.Contains("TestCardAsset", exception.Message);
        }

        [Test]
        public void ToDefinition_EvolutionWithZeroCasts_ThrowsNamingTheAsset()
        {
            Author(Id, CastTime, ((int)EffectKind.DealDamage, 3));
            AuthorEvolutions((0, 6));

            var exception = Assert.Throws<InvalidOperationException>(() => _asset.ToDefinition());

            StringAssert.Contains("TestCardAsset", exception.Message);
        }
    }
}
