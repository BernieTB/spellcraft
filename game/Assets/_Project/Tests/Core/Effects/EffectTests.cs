using System;
using Game.Core.Combat;
using Game.Core.Effects;
using NUnit.Framework;

namespace Game.Core.Tests.Effects
{
    public class EffectTests
    {
        // Arbitrary test data, not balance values.
        private const int MaxHealth = 10;
        private const int Amount = 3;

        private Combatant _caster;
        private Combatant _target;
        private EffectContext _context;

        [SetUp]
        public void SetUp()
        {
            _caster = new Combatant(MaxHealth, 0);
            _target = new Combatant(MaxHealth, 0);
            _context = new EffectContext(_caster, _target);
        }

        // --- EffectContext ---

        [Test]
        public void EffectContext_NullCaster_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new EffectContext(null, _target));
        }

        [Test]
        public void EffectContext_NullTarget_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new EffectContext(_caster, null));
        }

        // --- DealDamageEffect ---

        [Test]
        public void DealDamageEffect_Constructor_StoresAmount()
        {
            Assert.AreEqual(Amount, new DealDamageEffect(Amount).Amount);
        }

        [Test]
        public void DealDamageEffect_NegativeAmount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DealDamageEffect(-1));
        }

        [Test]
        public void DealDamageEffect_Apply_DamagesTarget()
        {
            new DealDamageEffect(Amount).Apply(_context);

            Assert.AreEqual(MaxHealth - Amount, _target.CurrentHealth);
        }

        [Test]
        public void DealDamageEffect_Apply_LeavesCasterUnchanged()
        {
            new DealDamageEffect(Amount).Apply(_context);

            Assert.AreEqual(MaxHealth, _caster.CurrentHealth);
        }

        [Test]
        public void DealDamageEffect_NullContext_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DealDamageEffect(Amount).Apply(null));
        }

        // --- HealEffect ---

        [Test]
        public void HealEffect_Constructor_StoresAmount()
        {
            Assert.AreEqual(Amount, new HealEffect(Amount).Amount);
        }

        [Test]
        public void HealEffect_NegativeAmount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HealEffect(-1));
        }

        [Test]
        public void HealEffect_Apply_HealsCaster()
        {
            _caster.TakeDamage(Amount + 1);

            new HealEffect(Amount).Apply(_context);

            Assert.AreEqual(MaxHealth - 1, _caster.CurrentHealth);
        }

        [Test]
        public void HealEffect_Apply_LeavesTargetUnchanged()
        {
            _target.TakeDamage(Amount + 1);

            new HealEffect(Amount).Apply(_context);

            Assert.AreEqual(MaxHealth - Amount - 1, _target.CurrentHealth);
        }

        [Test]
        public void HealEffect_NullContext_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new HealEffect(Amount).Apply(null));
        }

        // --- GainShieldEffect ---

        [Test]
        public void GainShieldEffect_Constructor_StoresAmount()
        {
            Assert.AreEqual(Amount, new GainShieldEffect(Amount).Amount);
        }

        [Test]
        public void GainShieldEffect_NegativeAmount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GainShieldEffect(-1));
        }

        [Test]
        public void GainShieldEffect_Apply_ShieldsCaster()
        {
            new GainShieldEffect(Amount).Apply(_context);

            Assert.AreEqual(Amount, _caster.Shield);
        }

        [Test]
        public void GainShieldEffect_Apply_LeavesTargetUnchanged()
        {
            new GainShieldEffect(Amount).Apply(_context);

            Assert.AreEqual(0, _target.Shield);
        }

        [Test]
        public void GainShieldEffect_NullContext_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GainShieldEffect(Amount).Apply(null));
        }
    }
}
