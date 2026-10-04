using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Effects;
using Game.Core.Enemies;
using Game.Core.Meta;
using NUnit.Framework;

namespace Game.Core.Tests.Meta
{
    public class BestiaryJsonTests
    {
        // Placeholder ids and test values: not game content.
        private static readonly CardDefinition Strike = new CardDefinition("TestCard1", 1, new IEffect[] { new DealDamageEffect(2) });
        private static readonly CardDefinition Guard = new CardDefinition("TestCard2", 1, new IEffect[] { new GainShieldEffect(2) });

        private static EnemyDefinition Professor(string id)
        {
            return new EnemyDefinition(id, 40, 5, new[] { Strike, Guard, Strike });
        }

        private static Bestiary Sample()
        {
            var bestiary = new Bestiary();
            bestiary.Reveal(Professor("TestProfessorB"), new ProfessorRevelation(false, true, new[] { 2, 0 }));
            bestiary.Reveal(Professor("TestProfessorA"), new ProfessorRevelation(true, false, new int[0]));
            return bestiary;
        }

        [Test]
        public void Serialize_GivesTheVersionedFormat()
        {
            var expected =
                "{\n"
                + "  \"version\":1,\n"
                + "  \"professors\":[\n"
                + "    {\"id\":\"TestProfessorA\",\"healthKnown\":true,\"shieldKnown\":false,\"cards\":[]},\n"
                + "    {\"id\":\"TestProfessorB\",\"healthKnown\":false,\"shieldKnown\":true,\"cards\":"
                + "[{\"position\":0,\"cardId\":\"TestCard1\"},{\"position\":2,\"cardId\":\"TestCard1\"}]}\n"
                + "  ]\n"
                + "}\n";

            Assert.AreEqual(expected, BestiaryJson.Serialize(Sample()));
        }

        [Test]
        public void Serialize_Empty_GivesAnEmptyList()
        {
            Assert.AreEqual("{\n  \"version\":1,\n  \"professors\":[]\n}\n", BestiaryJson.Serialize(new Bestiary()));
        }

        [Test]
        public void RoundTrip_KeepsEverything()
        {
            var original = Sample();

            var restored = BestiaryJson.Deserialize(BestiaryJson.Serialize(original));

            Assert.AreEqual(BestiaryJson.Serialize(original), BestiaryJson.Serialize(restored));
            var knowledge = restored.GetKnowledge(Professor("TestProfessorB"));
            Assert.IsNull(knowledge.MaxHealth);
            Assert.AreEqual(5, knowledge.Shield);
            CollectionAssert.AreEqual(new[] { Strike, null, Strike }, knowledge.SpellLine);
        }

        [Test]
        public void Deserialize_AcceptsWhitespaceAndFieldOrder()
        {
            var json = "{ \"professors\": [ { \"cards\": [ { \"cardId\": \"TestCard2\", \"position\": 1 } ],"
                + " \"shieldKnown\": false, \"healthKnown\": false, \"id\": \"TestProfessorA\" } ], \"version\": 1 }";

            var bestiary = BestiaryJson.Deserialize(json);

            CollectionAssert.AreEqual(new[] { null, Guard, null }, bestiary.GetKnowledge(Professor("TestProfessorA")).SpellLine);
        }

        [Test]
        public void Deserialize_EscapedId_IsRead()
        {
            var json = "{\"version\":1,\"professors\":[{\"id\":\"Test\\\"Prof\\u0041\",\"healthKnown\":true,\"shieldKnown\":false,\"cards\":[]}]}";

            Assert.AreEqual("Test\"ProfA", BestiaryJson.Deserialize(json).Entries[0].ProfessorId);
        }

        [Test]
        public void Deserialize_UnknownVersion_Throws()
        {
            var exception = Assert.Throws<FormatException>(() => BestiaryJson.Deserialize("{\"version\":2,\"professors\":[]}"));

            StringAssert.Contains("version 2", exception.Message);
        }

        private static IEnumerable<string> InvalidSaves()
        {
            yield return string.Empty;
            yield return "not json";
            yield return "[]";
            yield return "{\"version\":1,\"professors\":[]} trailing";
            yield return "{\"professors\":[]}";
            yield return "{\"version\":\"1\",\"professors\":[]}";
            yield return "{\"version\":1}";
            yield return "{\"version\":1,\"professors\":[{\"id\":\"A\",\"healthKnown\":true,\"shieldKnown\":false}]}";
            yield return "{\"version\":1,\"professors\":[{\"id\":\"\",\"healthKnown\":true,\"shieldKnown\":false,\"cards\":[]}]}";
            yield return "{\"version\":1,\"professors\":[{\"id\":\"A\",\"healthKnown\":1,\"shieldKnown\":false,\"cards\":[]}]}";
            yield return "{\"version\":1,\"professors\":[{\"id\":\"A\",\"healthKnown\":true,\"shieldKnown\":false,\"cards\":[{\"position\":-1,\"cardId\":\"C\"}]}]}";
            yield return "{\"version\":1,\"professors\":[{\"id\":\"A\",\"healthKnown\":true,\"shieldKnown\":false,\"cards\":[{\"position\":1.5,\"cardId\":\"C\"}]}]}";
            yield return "{\"version\":1,\"version\":1,\"professors\":[]}";
            yield return "{\"version\":1,\"professors\":[";
        }

        [TestCaseSource(nameof(InvalidSaves))]
        public void Deserialize_InvalidSave_ThrowsFormatException(string json)
        {
            Assert.Throws<FormatException>(() => BestiaryJson.Deserialize(json));
        }

        [Test]
        public void Deserialize_Null_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => BestiaryJson.Deserialize(null));
        }

        [Test]
        public void Serialize_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => BestiaryJson.Serialize(null));
        }
    }
}
