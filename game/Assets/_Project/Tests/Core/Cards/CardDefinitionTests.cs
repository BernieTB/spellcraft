using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Effects;
using NUnit.Framework;

namespace Game.Core.Tests.Cards
{
    public class CardDefinitionTests
    {
        // Placeholder id and arbitrary test data, not real content or balance values.
        private const string Id = "test_card_01";
        private const int CastTime = 2;
        private const int MaxHealth = 10;

        private sealed class RecordingEffect : IEffect
        {
            private readonly string _name;
            private readonly List<string> _log;

            public RecordingEffect(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public void Apply(EffectContext context) => _log.Add(_name);
        }

        private static EffectContext CreateContext() =>
            new EffectContext(new Combatant(MaxHealth, 0), new Combatant(MaxHealth, 0));

        [Test]
        public void Constructor_ValidValues_StoresId()
        {
            var card = new CardDefinition(Id, CastTime, new IEffect[0]);

            Assert.AreEqual(Id, card.Id);
        }

        [Test]
        public void Constructor_ValidValues_StoresCastTime()
        {
            var card = new CardDefinition(Id, CastTime, new IEffect[0]);

            Assert.AreEqual(CastTime, card.CastTime);
        }

        [Test]
        public void Constructor_ValidValues_StoresEffectsInOrder()
        {
            var first = new DealDamageEffect(1);
            var second = new HealEffect(1);

            var card = new CardDefinition(Id, CastTime, new IEffect[] { first, second });

            CollectionAssert.AreEqual(new IEffect[] { first, second }, card.Effects);
        }

        [Test]
        public void Constructor_SourceModifiedAfterwards_EffectsUnchanged()
        {
            var source = new List<IEffect> { new DealDamageEffect(1) };
            var card = new CardDefinition(Id, CastTime, source);

            source.Add(new HealEffect(1));

            Assert.AreEqual(1, card.Effects.Count);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_MissingId_Throws(string id)
        {
            Assert.Throws<ArgumentException>(() => new CardDefinition(id, CastTime, new IEffect[0]));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_NonPositiveCastTime_Throws(int castTime)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardDefinition(Id, castTime, new IEffect[0]));
        }

        [Test]
        public void Constructor_NullEffects_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CardDefinition(Id, CastTime, null));
        }

        [Test]
        public void Constructor_NullEffectItem_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CardDefinition(Id, CastTime, new IEffect[] { null }));
        }

        [Test]
        public void Resolve_SeveralEffects_AppliesThemInOrder()
        {
            var log = new List<string>();
            var card = new CardDefinition(Id, CastTime, new IEffect[]
            {
                new RecordingEffect("first", log),
                new RecordingEffect("second", log),
                new RecordingEffect("third", log),
            });

            card.Resolve(CreateContext());

            CollectionAssert.AreEqual(new[] { "first", "second", "third" }, log);
        }

        [Test]
        public void Resolve_DamageEffect_DamagesTarget()
        {
            var context = CreateContext();
            var card = new CardDefinition(Id, CastTime, new IEffect[] { new DealDamageEffect(3) });

            card.Resolve(context);

            Assert.AreEqual(MaxHealth - 3, context.Target.CurrentHealth);
        }

        [Test]
        public void Resolve_NoEffects_LeavesCombatantsUnchanged()
        {
            var context = CreateContext();
            var card = new CardDefinition(Id, CastTime, new IEffect[0]);

            card.Resolve(context);

            Assert.AreEqual((MaxHealth, MaxHealth), (context.Caster.CurrentHealth, context.Target.CurrentHealth));
        }

        [Test]
        public void Resolve_NullContext_Throws()
        {
            var card = new CardDefinition(Id, CastTime, new IEffect[0]);

            Assert.Throws<ArgumentNullException>(() => card.Resolve(null));
        }
    }
}
