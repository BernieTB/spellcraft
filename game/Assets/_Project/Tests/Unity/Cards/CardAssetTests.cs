using System;
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
    }
}
