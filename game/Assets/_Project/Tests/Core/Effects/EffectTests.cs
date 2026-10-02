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

        [Test]
        public void DealDamageEffect_Apply_ReturnsDamageSplitBetweenShieldAndHealth()
        {
            _target.GainShield(1);

            var outcome = new DealDamageEffect(Amount).Apply(_context);

            Assert.AreEqual((1, Amount - 1, 0, 0), ToTuple(outcome));
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

        [Test]
        public void HealEffect_Apply_ReturnsHealthActuallyRestored()
        {
            _caster.TakeDamage(1);

            var outcome = new HealEffect(Amount).Apply(_context);

            Assert.AreEqual((0, 0, 1, 0), ToTuple(outcome));
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

        [Test]
        public void GainShieldEffect_Apply_ReturnsShieldGained()
        {
            var outcome = new GainShieldEffect(Amount).Apply(_context);

            Assert.AreEqual((0, 0, 0, Amount), ToTuple(outcome));
        }

        // --- Neighbour bonus through the context ---

        private EffectContext ContextWithBonus(int damage, int heal, int shield) =>
            new EffectContext(_caster, _target, new EffectBonus(damage, heal, shield));

        [Test]
        public void EffectContext_WithoutBonus_HasNoBonus()
        {
            Assert.AreEqual(EffectBonus.None, _context.Bonus);
        }

        [Test]
        public void EffectContext_ConsumeBonus_ReturnsBonusOfThatKind()
        {
            var context = ContextWithBonus(1, 2, 3);

            Assert.AreEqual(2, context.ConsumeBonus(BonusKind.Heal));
        }

        [Test]
        public void EffectContext_ConsumeBonusTwice_SecondCallReturnsZero()
        {
            var context = ContextWithBonus(1, 2, 3);
            context.ConsumeBonus(BonusKind.Damage);

            Assert.AreEqual(0, context.ConsumeBonus(BonusKind.Damage));
        }

        [Test]
        public void EffectContext_ConsumeBonus_LeavesOtherKindsAndBonusUnchanged()
        {
            var context = ContextWithBonus(1, 2, 3);
            context.ConsumeBonus(BonusKind.Damage);

            Assert.AreEqual((3, new EffectBonus(1, 2, 3)), (context.ConsumeBonus(BonusKind.Shield), context.Bonus));
        }

        [Test]
        public void DealDamageEffect_ApplyWithDamageBonus_AddsBonusToDamage()
        {
            var outcome = new DealDamageEffect(Amount).Apply(ContextWithBonus(2, 0, 0));

            Assert.AreEqual((Amount + 2, MaxHealth - Amount - 2), (outcome.Damage.HealthLost, _target.CurrentHealth));
        }

        [Test]
        public void DealDamageEffect_ApplyWithOtherBonuses_IgnoresThem()
        {
            var outcome = new DealDamageEffect(Amount).Apply(ContextWithBonus(0, 2, 2));

            Assert.AreEqual((0, Amount, 0, 0), ToTuple(outcome));
        }

        [Test]
        public void DealDamageEffect_ApplyWithDamageBonus_KeepsAmount()
        {
            var effect = new DealDamageEffect(Amount);

            effect.Apply(ContextWithBonus(2, 0, 0));

            Assert.AreEqual(Amount, effect.Amount);
        }

        [Test]
        public void HealEffect_ApplyWithHealBonus_AddsBonusToHealing()
        {
            _caster.TakeDamage(MaxHealth - 1);

            var outcome = new HealEffect(Amount).Apply(ContextWithBonus(0, 2, 0));

            Assert.AreEqual(Amount + 2, outcome.Healed);
        }

        [Test]
        public void HealEffect_ApplyWithOtherBonuses_IgnoresThem()
        {
            _caster.TakeDamage(MaxHealth - 1);

            var outcome = new HealEffect(Amount).Apply(ContextWithBonus(2, 0, 2));

            Assert.AreEqual((0, 0, Amount, 0), ToTuple(outcome));
        }

        [Test]
        public void GainShieldEffect_ApplyWithShieldBonus_AddsBonusToShield()
        {
            var outcome = new GainShieldEffect(Amount).Apply(ContextWithBonus(0, 0, 2));

            Assert.AreEqual((Amount + 2, Amount + 2), (outcome.ShieldGained, _caster.Shield));
        }

        [Test]
        public void GainShieldEffect_ApplyWithOtherBonuses_IgnoresThem()
        {
            var outcome = new GainShieldEffect(Amount).Apply(ContextWithBonus(2, 2, 0));

            Assert.AreEqual((0, 0, 0, Amount), ToTuple(outcome));
        }

        // --- EffectOutcome ---

        [Test]
        public void EffectOutcome_None_IsAllZero()
        {
            Assert.AreEqual((0, 0, 0, 0), ToTuple(EffectOutcome.None));
        }

        [Test]
        public void EffectOutcome_Plus_SumsEachField()
        {
            var first = new EffectOutcome(new DamageResult(1, 2), 3, 4);
            var second = new EffectOutcome(new DamageResult(5, 6), 7, 8);

            Assert.AreEqual((6, 8, 10, 12), ToTuple(first.Plus(second)));
        }

        private static (int, int, int, int) ToTuple(EffectOutcome outcome) =>
            (outcome.Damage.AbsorbedByShield, outcome.Damage.HealthLost, outcome.Healed, outcome.ShieldGained);
    }
}
