using System;
using Game.Core.Combat;
using NUnit.Framework;

namespace Game.Core.Tests.Combat
{
    public class CombatantTests
    {
        // Arbitrary test data, not balance values.
        private const int MaxHealth = 10;
        private const int StartingShield = 4;

        private static Combatant CreateUnshielded() => new Combatant(MaxHealth, 0);

        private static Combatant CreateShielded() => new Combatant(MaxHealth, StartingShield);

        private static Combatant CreateDead()
        {
            var combatant = CreateUnshielded();
            combatant.TakeDamage(MaxHealth);
            return combatant;
        }

        // --- Constructor ---

        [Test]
        public void Constructor_ValidValues_StartsAtFullHealth()
        {
            var combatant = CreateShielded();

            Assert.AreEqual(MaxHealth, combatant.CurrentHealth);
        }

        [Test]
        public void Constructor_ValidValues_SetsMaxHealth()
        {
            var combatant = CreateShielded();

            Assert.AreEqual(MaxHealth, combatant.MaxHealth);
        }

        [Test]
        public void Constructor_ValidValues_SetsShield()
        {
            var combatant = CreateShielded();

            Assert.AreEqual(StartingShield, combatant.Shield);
        }

        [Test]
        public void Constructor_ValidValues_IsAlive()
        {
            var combatant = CreateShielded();

            Assert.IsFalse(combatant.IsDead);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_NonPositiveMaxHealth_Throws(int maxHealth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Combatant(maxHealth, 0));
        }

        [Test]
        public void Constructor_NegativeShield_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Combatant(MaxHealth, -1));
        }

        // --- TakeDamage without shield ---

        [Test]
        public void TakeDamage_LessThanHealth_ReducesHealth()
        {
            var combatant = CreateUnshielded();

            combatant.TakeDamage(3);

            Assert.AreEqual(MaxHealth - 3, combatant.CurrentHealth);
        }

        [Test]
        public void TakeDamage_LessThanHealth_ReportsHealthLost()
        {
            var combatant = CreateUnshielded();

            Assert.AreEqual(3, combatant.TakeDamage(3).HealthLost);
        }

        [Test]
        public void TakeDamage_LessThanHealth_StaysAlive()
        {
            var combatant = CreateUnshielded();

            combatant.TakeDamage(MaxHealth - 1);

            Assert.IsFalse(combatant.IsDead);
        }

        [Test]
        public void TakeDamage_ExactlyHealth_Dies()
        {
            var combatant = CreateUnshielded();

            combatant.TakeDamage(MaxHealth);

            Assert.IsTrue(combatant.IsDead);
        }

        [Test]
        public void TakeDamage_MoreThanHealth_ClampsHealthAtZero()
        {
            var combatant = CreateUnshielded();

            combatant.TakeDamage(MaxHealth + 5);

            Assert.AreEqual(0, combatant.CurrentHealth);
        }

        [Test]
        public void TakeDamage_MoreThanHealth_ReportsOnlyHealthRemoved()
        {
            var combatant = CreateUnshielded();

            Assert.AreEqual(MaxHealth, combatant.TakeDamage(MaxHealth + 5).Total);
        }

        [Test]
        public void TakeDamage_Zero_LeavesHealthUnchanged()
        {
            var combatant = CreateUnshielded();

            combatant.TakeDamage(0);

            Assert.AreEqual(MaxHealth, combatant.CurrentHealth);
        }

        [Test]
        public void TakeDamage_Negative_Throws()
        {
            var combatant = CreateUnshielded();

            Assert.Throws<ArgumentOutOfRangeException>(() => combatant.TakeDamage(-1));
        }

        [Test]
        public void TakeDamage_WhenDead_ReportsNoDamage()
        {
            var combatant = CreateDead();

            Assert.AreEqual(0, combatant.TakeDamage(1).Total);
        }

        // --- TakeDamage with shield ---

        [Test]
        public void TakeDamage_LessThanShield_ReducesShield()
        {
            var combatant = CreateShielded();

            combatant.TakeDamage(StartingShield - 1);

            Assert.AreEqual(1, combatant.Shield);
        }

        [Test]
        public void TakeDamage_LessThanShield_LeavesHealthUnchanged()
        {
            var combatant = CreateShielded();

            combatant.TakeDamage(StartingShield - 1);

            Assert.AreEqual(MaxHealth, combatant.CurrentHealth);
        }

        [Test]
        public void TakeDamage_LessThanShield_ReportsAllAbsorbed()
        {
            var combatant = CreateShielded();

            Assert.AreEqual(StartingShield - 1, combatant.TakeDamage(StartingShield - 1).AbsorbedByShield);
        }

        [Test]
        public void TakeDamage_ExactlyShield_EmptiesShieldAndKeepsHealth()
        {
            var combatant = CreateShielded();

            combatant.TakeDamage(StartingShield);

            Assert.AreEqual((0, MaxHealth), (combatant.Shield, combatant.CurrentHealth));
        }

        [Test]
        public void TakeDamage_MoreThanShield_EmptiesShield()
        {
            var combatant = CreateShielded();

            combatant.TakeDamage(StartingShield + 3);

            Assert.AreEqual(0, combatant.Shield);
        }

        [Test]
        public void TakeDamage_MoreThanShield_RemainderReducesHealth()
        {
            var combatant = CreateShielded();

            combatant.TakeDamage(StartingShield + 3);

            Assert.AreEqual(MaxHealth - 3, combatant.CurrentHealth);
        }

        [Test]
        public void TakeDamage_MoreThanShield_ReportsSplit()
        {
            var combatant = CreateShielded();

            var result = combatant.TakeDamage(StartingShield + 3);

            Assert.AreEqual((StartingShield, 3), (result.AbsorbedByShield, result.HealthLost));
        }

        [Test]
        public void TakeDamage_MoreThanShieldAndHealth_Dies()
        {
            var combatant = CreateShielded();

            combatant.TakeDamage(StartingShield + MaxHealth);

            Assert.IsTrue(combatant.IsDead);
        }

        // --- Heal ---

        [Test]
        public void Heal_WhenDamaged_RestoresHealth()
        {
            var combatant = CreateUnshielded();
            combatant.TakeDamage(5);

            combatant.Heal(2);

            Assert.AreEqual(MaxHealth - 3, combatant.CurrentHealth);
        }

        [Test]
        public void Heal_WhenDamaged_ReturnsAmount()
        {
            var combatant = CreateUnshielded();
            combatant.TakeDamage(5);

            Assert.AreEqual(2, combatant.Heal(2));
        }

        [Test]
        public void Heal_MoreThanMissingHealth_CapsAtMaxHealth()
        {
            var combatant = CreateUnshielded();
            combatant.TakeDamage(2);

            combatant.Heal(5);

            Assert.AreEqual(MaxHealth, combatant.CurrentHealth);
        }

        [Test]
        public void Heal_MoreThanMissingHealth_ReturnsHealthRestored()
        {
            var combatant = CreateUnshielded();
            combatant.TakeDamage(2);

            Assert.AreEqual(2, combatant.Heal(5));
        }

        [Test]
        public void Heal_AtFullHealth_ReturnsZero()
        {
            var combatant = CreateUnshielded();

            Assert.AreEqual(0, combatant.Heal(3));
        }

        [Test]
        public void Heal_AfterShieldLoss_DoesNotRestoreShield()
        {
            var combatant = CreateShielded();
            combatant.TakeDamage(StartingShield);

            combatant.Heal(StartingShield);

            Assert.AreEqual(0, combatant.Shield);
        }

        [Test]
        public void Heal_Zero_LeavesHealthUnchanged()
        {
            var combatant = CreateUnshielded();
            combatant.TakeDamage(5);

            combatant.Heal(0);

            Assert.AreEqual(MaxHealth - 5, combatant.CurrentHealth);
        }

        [Test]
        public void Heal_Negative_Throws()
        {
            var combatant = CreateUnshielded();

            Assert.Throws<ArgumentOutOfRangeException>(() => combatant.Heal(-1));
        }

        [Test]
        public void Heal_WhenDead_StaysDead()
        {
            var combatant = CreateDead();

            combatant.Heal(5);

            Assert.IsTrue(combatant.IsDead);
        }

        [Test]
        public void Heal_WhenDead_ReturnsZero()
        {
            var combatant = CreateDead();

            Assert.AreEqual(0, combatant.Heal(5));
        }

        // --- GainShield ---

        [Test]
        public void GainShield_WithExistingShield_Stacks()
        {
            var combatant = CreateShielded();

            combatant.GainShield(3);

            Assert.AreEqual(StartingShield + 3, combatant.Shield);
        }

        [Test]
        public void GainShield_AboveMaxHealth_IsNotCapped()
        {
            var combatant = CreateUnshielded();

            combatant.GainShield(MaxHealth * 2);

            Assert.AreEqual(MaxHealth * 2, combatant.Shield);
        }

        [Test]
        public void GainShield_Valid_ReturnsAmount()
        {
            var combatant = CreateUnshielded();

            Assert.AreEqual(3, combatant.GainShield(3));
        }

        [Test]
        public void GainShield_Valid_LeavesHealthUnchanged()
        {
            var combatant = CreateUnshielded();

            combatant.GainShield(3);

            Assert.AreEqual(MaxHealth, combatant.CurrentHealth);
        }

        [Test]
        public void GainShield_Negative_Throws()
        {
            var combatant = CreateUnshielded();

            Assert.Throws<ArgumentOutOfRangeException>(() => combatant.GainShield(-1));
        }

        [Test]
        public void GainShield_WhenDead_LeavesShieldAtZero()
        {
            var combatant = CreateDead();

            combatant.GainShield(3);

            Assert.AreEqual(0, combatant.Shield);
        }

        [Test]
        public void GainShield_Overflow_Throws()
        {
            var combatant = new Combatant(MaxHealth, int.MaxValue);

            Assert.Throws<OverflowException>(() => combatant.GainShield(1));
        }
    }
}
