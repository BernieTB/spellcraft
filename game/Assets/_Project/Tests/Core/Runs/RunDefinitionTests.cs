using System;
using Game.Core.Cards;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Runs;
using NUnit.Framework;

namespace Game.Core.Tests.Runs
{
    public class RunDefinitionTests
    {
        // Placeholder ids and test values: not game content.
        private static readonly EncounterDefinition Encounter = new EncounterDefinition(
            "TestEncounter1",
            new[] { new EnemyDefinition("TestEnemy1", 10, 0, new[] { new CardDefinition("TestCard1", 1, new IEffect[] { new DealDamageEffect(1) }) }) });

        // --- BiomeDefinition ---

        [Test]
        public void Biome_ValidData_KeepsValues()
        {
            var biome = new BiomeDefinition("TestBiome", new[] { Encounter, Encounter }, 3, Encounter);

            Assert.AreEqual("TestBiome", biome.Id);
            Assert.AreEqual(2, biome.RegularEncounters.Count);
            Assert.AreEqual(3, biome.MinimumRegularFights);
            Assert.AreSame(Encounter, biome.ProfessorEncounter);
        }

        [Test]
        public void Biome_InvalidData_Throws()
        {
            Assert.Throws<ArgumentException>(() => new BiomeDefinition(" ", new[] { Encounter }, 0, Encounter));
            Assert.Throws<ArgumentException>(() => new BiomeDefinition("TestBiome", new EncounterDefinition[0], 0, Encounter));
            Assert.Throws<ArgumentNullException>(() => new BiomeDefinition("TestBiome", new EncounterDefinition[] { null }, 0, Encounter));
            Assert.Throws<ArgumentNullException>(() => new BiomeDefinition("TestBiome", null, 0, Encounter));
            Assert.Throws<ArgumentNullException>(() => new BiomeDefinition("TestBiome", new[] { Encounter }, 0, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BiomeDefinition("TestBiome", new[] { Encounter }, -1, Encounter));
        }

        // --- RunRules ---

        [Test]
        public void Rules_ValidTimeLimit_KeepsIt()
        {
            Assert.AreEqual(500, new RunRules(500).FightTimeLimit);
        }

        [Test]
        public void Rules_TimeLimitBelowOne_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunRules(0));
        }

        // --- RunStep ---

        [Test]
        public void Step_EqualKindAndRoom_AreEqual()
        {
            Assert.AreEqual(RunStep.SecretRoom("TestRoom"), RunStep.SecretRoom("TestRoom"));
            Assert.AreEqual(RunStep.SecretRoom("TestRoom").GetHashCode(), RunStep.SecretRoom("TestRoom").GetHashCode());
            Assert.AreNotEqual(RunStep.SecretRoom("TestRoom"), RunStep.SecretRoom("TestOtherRoom"));
            Assert.AreNotEqual(RunStep.RegularFight, RunStep.Professor);
        }

        [Test]
        public void Step_OnlyMiniBossAndProfessorFights_RequirePreparation()
        {
            Assert.IsFalse(RunStep.RegularFight.RequiresPreparation);
            Assert.IsTrue(RunStep.SecretRoom("TestRoom").RequiresPreparation);
            Assert.IsTrue(RunStep.Professor.RequiresPreparation);
        }

        [Test]
        public void Step_BlankRoomId_Throws()
        {
            Assert.Throws<ArgumentException>(() => RunStep.SecretRoom(" "));
        }

        // --- CardInstance ---

        [Test]
        public void CardInstance_InvalidArguments_Throw()
        {
            var card = Encounter.Enemies[0].SpellLine[0];
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardInstance(0, card));
            Assert.Throws<ArgumentNullException>(() => new CardInstance(1, null));
        }
    }
}
