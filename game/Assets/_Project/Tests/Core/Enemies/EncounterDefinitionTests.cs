using System;
using Game.Core.Cards;
using Game.Core.Effects;
using Game.Core.Enemies;
using NUnit.Framework;

namespace Game.Core.Tests.Enemies
{
    public class EncounterDefinitionTests
    {
        // Placeholder ids and test values: not game content.
        private static readonly CardDefinition Attack = new CardDefinition("TestCard1", 1, new IEffect[] { new DealDamageEffect(2) });

        private static EnemyDefinition Enemy(string id = "TestEnemy1", int maxHealth = 10, int shield = 1)
        {
            return new EnemyDefinition(id, maxHealth, shield, new[] { Attack });
        }

        // --- EnemyDefinition ---

        [Test]
        public void Enemy_ValidData_KeepsValues()
        {
            var enemy = Enemy(maxHealth: 12, shield: 3);

            Assert.AreEqual("TestEnemy1", enemy.Id);
            Assert.AreEqual(12, enemy.MaxHealth);
            Assert.AreEqual(3, enemy.Shield);
            CollectionAssert.AreEqual(new[] { Attack }, enemy.SpellLine);
        }

        [TestCase(null)]
        [TestCase(" ")]
        public void Enemy_BlankId_Throws(string id)
        {
            Assert.Throws<ArgumentException>(() => Enemy(id: id));
        }

        [Test]
        public void Enemy_StatsOutOfRange_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Enemy(maxHealth: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Enemy(shield: -1));
        }

        [Test]
        public void Enemy_XpReward_DefaultsToZeroAndKeepsTheGivenValue()
        {
            Assert.AreEqual(0, Enemy().XpReward);
            Assert.AreEqual(7, new EnemyDefinition("TestEnemy1", 10, 0, new[] { Attack }, 7).XpReward);
        }

        [Test]
        public void Enemy_NegativeXpReward_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyDefinition("TestEnemy1", 10, 0, new[] { Attack }, -1));
        }

        [Test]
        public void Enemy_EmptySpellLine_Throws()
        {
            Assert.Throws<ArgumentException>(() => new EnemyDefinition("TestEnemy1", 10, 0, new CardDefinition[0]));
        }

        [Test]
        public void Enemy_NullCard_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new EnemyDefinition("TestEnemy1", 10, 0, new CardDefinition[] { null }));
            Assert.Throws<ArgumentNullException>(() => new EnemyDefinition("TestEnemy1", 10, 0, null));
        }

        [Test]
        public void CreateParticipant_StartsAtFullHealthWithItsShieldAndLine()
        {
            var participant = Enemy(maxHealth: 12, shield: 3).CreateParticipant();

            Assert.AreEqual(12, participant.Combatant.CurrentHealth);
            Assert.AreEqual(3, participant.Combatant.Shield);
            CollectionAssert.AreEqual(new[] { Attack }, participant.SpellLine.Cards);
        }

        [Test]
        public void CreateParticipant_EachCall_GivesANewCombatant()
        {
            var enemy = Enemy();
            var first = enemy.CreateParticipant();
            first.Combatant.TakeDamage(5);

            var second = enemy.CreateParticipant();

            Assert.AreNotSame(first.Combatant, second.Combatant);
            Assert.AreEqual(10, second.Combatant.CurrentHealth);
        }

        // --- EncounterDefinition ---

        [Test]
        public void Encounter_SameEnemyTwice_CreatesTwoCombatantsInOrder()
        {
            var enemy = Enemy();
            var encounter = new EncounterDefinition("TestEncounter1", new[] { enemy, enemy });

            var participants = encounter.CreateParticipants();

            Assert.AreEqual(2, participants.Count);
            Assert.AreNotSame(participants[0].Combatant, participants[1].Combatant);
        }

        [Test]
        public void Encounter_InvalidData_Throws()
        {
            Assert.Throws<ArgumentException>(() => new EncounterDefinition(" ", new[] { Enemy() }));
            Assert.Throws<ArgumentException>(() => new EncounterDefinition("TestEncounter1", new EnemyDefinition[0]));
            Assert.Throws<ArgumentNullException>(() => new EncounterDefinition("TestEncounter1", new EnemyDefinition[] { null }));
            Assert.Throws<ArgumentNullException>(() => new EncounterDefinition("TestEncounter1", null));
        }
    }
}
