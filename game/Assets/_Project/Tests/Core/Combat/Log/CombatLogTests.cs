using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Combat.Log;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;
using NUnit.Framework;

namespace Game.Core.Tests.Combat.Log
{
    public class CombatLogTests
    {
        // Placeholder ids and arbitrary test data, not real content or balance values.
        private const ulong Seed = 7UL;
        private const int MaxTicks = 50;
        private const int Capacity = 5;

        private const int Hero = Fight.HeroIndex;
        private const int FirstEnemy = 1;

        // The bonus fields every JSON event ends with when there is no neighbour bonus.
        private const string NoBonusJson =
            ",\"bonus\":{\"damage\":0,\"heal\":0,\"shield\":0},\"wasted\":{\"damage\":0,\"heal\":0,\"shield\":0}";

        // --- Helpers ---

        private static CardDefinition Card(string id, int castTime, params IEffect[] effects) =>
            new CardDefinition(id, castTime, effects);

        private static CardDefinition Idle() => Card("test_idle", MaxTicks + 1);

        // A card with no effect that gives each modifier to the next card.
        private static CardDefinition Booster(params NeighbourModifier[] modifiers) =>
            new CardDefinition("test_boost", 1, new IEffect[0], modifiers);

        private static NeighbourModifier Next(BonusKind kind, int amount) =>
            new NeighbourModifier(kind, NeighbourDirection.Next, amount);

        // The hero casts the booster on tick 1, then the receiver on tick 2, against an idle enemy.
        private static CombatLog RecordBoosted(CardDefinition booster, CardDefinition receiver) =>
            RecordFor(2, Participant(10, 0, booster, receiver), Participant(10, 0, Idle()));

        private static CombatEvent BoostedCast(CombatLog log) =>
            log.Events.Where(e => e.Kind == CombatEventKind.CardCast).ElementAt(1);

        private static CombatEvent Event(CombatEventKind kind, EffectBonus bonus, EffectBonus wastedBonus) =>
            new CombatEvent(0, 1, kind, "test_hit", 0, Hero, FirstEnemy, 0, 0, 0, 10, 0, bonus, wastedBonus);

        private static FightParticipant Participant(int health, int shield, params CardDefinition[] cards)
        {
            var line = new SpellLine<CardDefinition>(Capacity);
            foreach (var card in cards)
            {
                line.Add(card);
            }

            return new FightParticipant(new Combatant(health, shield), line);
        }

        private static CombatLog Record(FightParticipant hero, params FightParticipant[] enemies) =>
            CombatLogRecorder.Record(hero, enemies, MaxTicks, new Pcg32Random(Seed));

        private static CombatLog RecordFor(int maxTicks, FightParticipant hero, params FightParticipant[] enemies) =>
            CombatLogRecorder.Record(hero, enemies, maxTicks, new Pcg32Random(Seed));

        private static List<CombatEventKind> Kinds(CombatLog log) => log.Events.Select(e => e.Kind).ToList();

        // --- Content of events ---

        [Test]
        public void Record_DamageOnShield_SplitsAbsorbedAndHealthLost()
        {
            var hero = Participant(10, 0, Card("test_hit", 1, new DealDamageEffect(5)));
            var log = RecordFor(1, hero, Participant(10, 3, Idle()));

            var damage = log.Events.Single(e => e.Kind == CombatEventKind.Damage);
            Assert.AreEqual((5, 3, 2, 8, 0), (damage.Amount, damage.AbsorbedByShield, damage.HealthLost, damage.TargetHealth, damage.TargetShield));
        }

        [Test]
        public void Record_DamageFullyAbsorbed_LogsDamageWithNoHealthLost()
        {
            var hero = Participant(10, 0, Card("test_hit", 1, new DealDamageEffect(2)));
            var log = RecordFor(1, hero, Participant(10, 5, Idle()));

            var damage = log.Events.Single(e => e.Kind == CombatEventKind.Damage);
            Assert.AreEqual((2, 2, 0, 10, 3), (damage.Amount, damage.AbsorbedByShield, damage.HealthLost, damage.TargetHealth, damage.TargetShield));
        }

        [Test]
        public void Record_Heal_LogsHealthRestoredOnCaster()
        {
            var hero = Participant(10, 0, Card("test_heal", 3, new HealEffect(4)));
            var enemy = Participant(10, 0, Card("test_hit", 2, new DealDamageEffect(6)));
            var log = RecordFor(3, hero, enemy);

            var heal = log.Events.Single(e => e.Kind == CombatEventKind.Heal);
            Assert.AreEqual((Hero, Hero, 4, 8), (heal.CasterIndex, heal.TargetIndex, heal.Amount, heal.TargetHealth));
        }

        [Test]
        public void Record_HealCappedAtMaxHealth_LogsOnlyHealthActuallyRestored()
        {
            var hero = Participant(10, 0, Card("test_heal", 3, new HealEffect(9)));
            var enemy = Participant(10, 0, Card("test_hit", 2, new DealDamageEffect(2)));
            var log = RecordFor(3, hero, enemy);

            var heal = log.Events.Single(e => e.Kind == CombatEventKind.Heal);
            Assert.AreEqual((2, 10), (heal.Amount, heal.TargetHealth));
        }

        [Test]
        public void Record_HealAtFullHealth_LogsNoHealEvent()
        {
            var hero = Participant(10, 0, Card("test_heal", 1, new HealEffect(3)));
            var log = RecordFor(1, hero, Participant(10, 0, Idle()));

            CollectionAssert.AreEqual(new[] { CombatEventKind.CardCast }, Kinds(log));
        }

        [Test]
        public void Record_ShieldGain_LogsShieldAddedToCaster()
        {
            var hero = Participant(10, 2, Card("test_guard", 1, new GainShieldEffect(3)));
            var log = RecordFor(1, hero, Participant(10, 0, Idle()));

            var gain = log.Events.Single(e => e.Kind == CombatEventKind.ShieldGain);
            Assert.AreEqual((Hero, 3, 5), (gain.TargetIndex, gain.Amount, gain.TargetShield));
        }

        [Test]
        public void Record_LethalDamage_LogsDeathRightAfterDamage()
        {
            var hero = Participant(10, 0, Card("test_hit", 1, new DealDamageEffect(20)));
            var log = Record(hero, Participant(5, 0, Idle()));

            CollectionAssert.AreEqual(
                new[] { CombatEventKind.CardCast, CombatEventKind.Damage, CombatEventKind.Death },
                Kinds(log));
            var death = log.Events.Last();
            Assert.AreEqual((FirstEnemy, Hero, "test_hit", 0, 0), (death.TargetIndex, death.CasterIndex, death.CardId, death.TargetHealth, death.Position));
        }

        [Test]
        public void Record_LethalDamage_DamageAmountExcludesOverkill()
        {
            var hero = Participant(10, 0, Card("test_hit", 1, new DealDamageEffect(20)));
            var log = Record(hero, Participant(5, 1, Idle()));

            var damage = log.Events.Single(e => e.Kind == CombatEventKind.Damage);
            Assert.AreEqual((6, 1, 5), (damage.Amount, damage.AbsorbedByShield, damage.HealthLost));
        }

        [Test]
        public void Record_SeveralEffects_LogsThemInCardOrder()
        {
            var hero = Participant(10, 0, Card("test_combo", 1, new GainShieldEffect(1), new DealDamageEffect(2), new HealEffect(0)));
            var log = RecordFor(1, hero, Participant(10, 0, Idle()));

            CollectionAssert.AreEqual(
                new[] { CombatEventKind.CardCast, CombatEventKind.ShieldGain, CombatEventKind.Damage },
                Kinds(log));
        }

        [Test]
        public void Record_EffectlessCard_LogsCastOnly()
        {
            var hero = Participant(10, 0, Card("test_nothing", 1));
            var log = RecordFor(1, hero, Participant(10, 0, Idle()));

            CollectionAssert.AreEqual(new[] { CombatEventKind.CardCast }, Kinds(log));
        }

        // --- Neighbour bonuses ---

        [Test]
        public void Record_BonusUsed_CastEventCarriesBonusWithNothingWasted()
        {
            var log = RecordBoosted(Booster(Next(BonusKind.Damage, 3)), Card("test_hit", 1, new DealDamageEffect(1)));

            var cast = BoostedCast(log);
            Assert.AreEqual((new EffectBonus(3, 0, 0), EffectBonus.None), (cast.Bonus, cast.WastedBonus));
            Assert.AreEqual(4, log.Events.Single(e => e.Kind == CombatEventKind.Damage).Amount);
        }

        [Test]
        public void Record_BonusOfKindTheCardLacks_CastEventMarksItWasted()
        {
            var log = RecordBoosted(Booster(Next(BonusKind.Damage, 3)), Card("test_guard", 1, new GainShieldEffect(1)));

            var cast = BoostedCast(log);
            Assert.AreEqual((new EffectBonus(3, 0, 0), new EffectBonus(3, 0, 0)), (cast.Bonus, cast.WastedBonus));
            Assert.AreEqual(1, log.Events.Single(e => e.Kind == CombatEventKind.ShieldGain).Amount);
        }

        [Test]
        public void Record_StackedBonuses_CastEventCarriesTheirSum()
        {
            var booster = Booster(Next(BonusKind.Damage, 2), Next(BonusKind.Damage, 3), Next(BonusKind.Heal, 4));
            var log = RecordBoosted(booster, Card("test_hit", 1, new DealDamageEffect(1)));

            var cast = BoostedCast(log);
            Assert.AreEqual((new EffectBonus(5, 4, 0), new EffectBonus(0, 4, 0)), (cast.Bonus, cast.WastedBonus));
        }

        [Test]
        public void Record_EventsOtherThanTheBoostedCast_HaveNoBonus()
        {
            var log = RecordBoosted(Booster(Next(BonusKind.Damage, 3)), Card("test_hit", 1, new DealDamageEffect(1)));

            var withoutBonus = log.Events.Where(e => e != BoostedCast(log)).ToList();
            Assert.That(withoutBonus.Select(e => e.Bonus), Is.All.EqualTo(EffectBonus.None));
            Assert.That(withoutBonus.Select(e => e.WastedBonus), Is.All.EqualTo(EffectBonus.None));
        }

        [Test]
        public void CombatEvent_WastedBonusExceedsBonus_Throws()
        {
            Assert.Throws<ArgumentException>(() => Event(
                CombatEventKind.CardCast,
                EffectBonus.Of(BonusKind.Damage, 1),
                EffectBonus.Of(BonusKind.Heal, 1)));
        }

        [Test]
        public void CombatEvent_BonusOnEventOtherThanCast_Throws()
        {
            Assert.Throws<ArgumentException>(() => Event(
                CombatEventKind.Damage,
                EffectBonus.Of(BonusKind.Damage, 1),
                EffectBonus.None));
        }

        // --- Tick, card, position, caster and target ---

        [Test]
        public void Record_LoopingLine_EventsCarryCardAndPosition()
        {
            var hero = Participant(
                10,
                0,
                Card("test_a", 1, new DealDamageEffect(1)),
                Card("test_b", 2, new DealDamageEffect(1)));
            var log = RecordFor(4, hero, Participant(10, 0, Idle()));

            var casts = log.Events.Where(e => e.Kind == CombatEventKind.CardCast)
                .Select(e => (e.Tick, e.CardId, e.Position));
            CollectionAssert.AreEqual(new[] { (1, "test_a", 0), (3, "test_b", 1), (4, "test_a", 0) }, casts);
        }

        [Test]
        public void Record_EffectEvents_CopyTickCardAndPositionOfTheirCast()
        {
            var hero = Participant(10, 0, Card("test_idle_first", 1), Card("test_hit", 2, new DealDamageEffect(1)));
            var log = RecordFor(3, hero, Participant(10, 0, Idle()));

            var damage = log.Events.Single(e => e.Kind == CombatEventKind.Damage);
            Assert.AreEqual((3, "test_hit", 1, Hero), (damage.Tick, damage.CardId, damage.Position, damage.CasterIndex));
        }

        [Test]
        public void Record_SameTick_HeroEventsComeBeforeEnemyEvents()
        {
            var card = Card("test_hit", 1, new DealDamageEffect(1));
            var log = RecordFor(1, Participant(10, 0, card), Participant(10, 0, card));

            var casters = log.Events.Select(e => e.CasterIndex);
            CollectionAssert.AreEqual(new[] { Hero, Hero, FirstEnemy, FirstEnemy }, casters);
        }

        [Test]
        public void Record_Events_SequenceIsTheirIndex()
        {
            var card = Card("test_hit", 1, new DealDamageEffect(1), new GainShieldEffect(1));
            var log = RecordFor(5, Participant(10, 0, card), Participant(10, 0, card));

            CollectionAssert.AreEqual(Enumerable.Range(0, log.Events.Count), log.Events.Select(e => e.Sequence));
        }

        // --- Fight summary ---

        [Test]
        public void Record_Combatants_SnapshotStartingStatsAndLine()
        {
            var hero = Participant(10, 2, Card("test_a", 1, new DealDamageEffect(9)), Card("test_b", 1));
            var log = Record(hero, Participant(4, 0, Idle()));

            var snapshot = log.Combatants[Hero];
            Assert.AreEqual((Hero, 10, 10, 2), (snapshot.Index, snapshot.MaxHealth, snapshot.Health, snapshot.Shield));
            CollectionAssert.AreEqual(new[] { "test_a", "test_b" }, snapshot.SpellLineCardIds);
        }

        [Test]
        public void Record_Timeout_HasNoWinnerAndMaxTicks()
        {
            var log = RecordFor(4, Participant(10, 0, Idle()), Participant(10, 0, Idle()));

            Assert.AreEqual((FightWinner.None, 4), (log.Winner, log.Ticks));
            Assert.IsEmpty(log.Events);
        }

        // --- Serialisation ---

        [Test]
        public void ToText_SmallFight_MatchesExpectedText()
        {
            var hero = Participant(10, 0, Card("test_hit", 1, new DealDamageEffect(6)));
            var log = Record(hero, Participant(5, 1, Card("test_heal", 1, new HealEffect(1))));

            const string expected =
                "combatant=0 maxHealth=10 health=10 shield=0 line=test_hit\n"
                + "combatant=1 maxHealth=5 health=5 shield=1 line=test_heal\n"
                + "tick=1 event=cast caster=0 pos=0 card=test_hit target=1\n"
                + "tick=1 event=damage caster=0 pos=0 card=test_hit target=1 amount=6 absorbed=1 healthLost=5 health=0 shield=0\n"
                + "tick=1 event=death caster=0 pos=0 card=test_hit target=1 health=0 shield=0\n"
                + "winner=hero ticks=1\n";
            Assert.AreEqual(expected, log.ToText());
        }

        [Test]
        public void ToJson_SmallFight_MatchesExpectedJson()
        {
            var hero = Participant(10, 0, Card("test_hit", 1, new DealDamageEffect(6)));
            var log = Record(hero, Participant(5, 1, Card("test_heal", 1, new HealEffect(1))));

            const string expected =
                "{\n"
                + "  \"winner\":\"hero\",\n"
                + "  \"ticks\":1,\n"
                + "  \"combatants\":[\n"
                + "    {\"index\":0,\"maxHealth\":10,\"health\":10,\"shield\":0,\"spellLine\":[\"test_hit\"]},\n"
                + "    {\"index\":1,\"maxHealth\":5,\"health\":5,\"shield\":1,\"spellLine\":[\"test_heal\"]}\n"
                + "  ],\n"
                + "  \"events\":[\n"
                + "    {\"sequence\":0,\"tick\":1,\"kind\":\"cast\",\"card\":\"test_hit\",\"position\":0,\"caster\":0,\"target\":1,\"amount\":0,\"absorbed\":0,\"healthLost\":0,\"targetHealth\":5,\"targetShield\":1" + NoBonusJson + "},\n"
                + "    {\"sequence\":1,\"tick\":1,\"kind\":\"damage\",\"card\":\"test_hit\",\"position\":0,\"caster\":0,\"target\":1,\"amount\":6,\"absorbed\":1,\"healthLost\":5,\"targetHealth\":0,\"targetShield\":0" + NoBonusJson + "},\n"
                + "    {\"sequence\":2,\"tick\":1,\"kind\":\"death\",\"card\":\"test_hit\",\"position\":0,\"caster\":0,\"target\":1,\"amount\":0,\"absorbed\":0,\"healthLost\":0,\"targetHealth\":0,\"targetShield\":0" + NoBonusJson + "}\n"
                + "  ]\n"
                + "}\n";
            Assert.AreEqual(expected, log.ToJson());
        }

        [Test]
        public void ToText_CastWithBonus_WritesReceivedAndWastedBonusOnCastLineOnly()
        {
            var booster = Booster(Next(BonusKind.Damage, 3), Next(BonusKind.Heal, 4));
            var log = RecordBoosted(booster, Card("test_hit", 1, new DealDamageEffect(1)));

            const string expected =
                "combatant=0 maxHealth=10 health=10 shield=0 line=test_boost,test_hit\n"
                + "combatant=1 maxHealth=10 health=10 shield=0 line=test_idle\n"
                + "tick=1 event=cast caster=0 pos=0 card=test_boost target=1\n"
                + "tick=2 event=cast caster=0 pos=1 card=test_hit target=1 bonusDamage=3 bonusHeal=4 wastedHeal=4\n"
                + "tick=2 event=damage caster=0 pos=1 card=test_hit target=1 amount=4 absorbed=0 healthLost=4 health=6 shield=0\n"
                + "winner=none ticks=2\n";
            Assert.AreEqual(expected, log.ToText());
        }

        [Test]
        public void ToJson_CastWithBonus_WritesReceivedAndWastedBonusOnThatCast()
        {
            var booster = Booster(Next(BonusKind.Damage, 3), Next(BonusKind.Heal, 4));
            var json = RecordBoosted(booster, Card("test_hit", 1, new DealDamageEffect(1))).ToJson();

            StringAssert.Contains(
                "{\"sequence\":1,\"tick\":2,\"kind\":\"cast\",\"card\":\"test_hit\",\"position\":1,\"caster\":0,"
                + "\"target\":1,\"amount\":0,\"absorbed\":0,\"healthLost\":0,\"targetHealth\":10,\"targetShield\":0,"
                + "\"bonus\":{\"damage\":3,\"heal\":4,\"shield\":0},\"wasted\":{\"damage\":0,\"heal\":4,\"shield\":0}},\n",
                json);
        }

        [Test]
        public void ToJson_NoEvents_WritesEmptyArray()
        {
            var log = RecordFor(1, Participant(10, 0, Idle()), Participant(10, 0, Idle()));

            StringAssert.Contains("\"events\":[]\n}", log.ToJson());
        }

        [Test]
        public void ToJson_CardIdWithSpecialCharacters_IsEscaped()
        {
            var hero = Participant(10, 0, Card("q\"b\\n\nt\tc\u0001", 1));
            var log = RecordFor(1, hero, Participant(10, 0, Idle()));

            StringAssert.Contains("\"card\":\"q\\\"b\\\\n\\nt\\tc\\u0001\"", log.ToJson());
        }

        [Test]
        public void KindName_EveryKind_IsDistinct()
        {
            var names = Enum.GetValues(typeof(CombatEventKind)).Cast<CombatEventKind>().Select(CombatLog.KindName).ToList();

            CollectionAssert.AllItemsAreUnique(names);
        }

        // --- Consistency guard ---

        [Test]
        public void Record_EffectChangesStatsWithoutReportingIt_Throws()
        {
            var hero = Participant(10, 0, Card("test_hidden", 1, new UnreportedDamageEffect(2)));

            Assert.Throws<InvalidOperationException>(() => RecordFor(1, hero, Participant(10, 0, Idle())));
        }

        // --- Build validation ---

        [Test]
        public void Build_NullArguments_Throw()
        {
            var result = new FightResult(FightWinner.None, 0, Array.Empty<CastRecord>());

            Assert.Throws<ArgumentNullException>(() => CombatLog.Build(null, result));
            Assert.Throws<ArgumentNullException>(() => CombatLog.Build(Array.Empty<CombatantSnapshot>(), null));
        }

        [Test]
        public void Build_SnapshotIndexDoesNotMatchPosition_Throws()
        {
            var result = new FightResult(FightWinner.None, 0, Array.Empty<CastRecord>());
            var snapshots = new[] { new CombatantSnapshot(1, 10, 10, 0, new[] { "test_a" }) };

            Assert.Throws<ArgumentException>(() => CombatLog.Build(snapshots, result));
        }

        [Test]
        public void Build_CastOnUnknownCombatant_Throws()
        {
            var cast = new CastRecord(1, 0, 0, Card("test_a", 1), 3, Array.Empty<EffectOutcome>());
            var result = new FightResult(FightWinner.None, 1, new[] { cast });
            var snapshots = new[] { new CombatantSnapshot(0, 10, 10, 0, new[] { "test_a" }) };

            Assert.Throws<ArgumentException>(() => CombatLog.Build(snapshots, result));
        }

        [Test]
        public void Build_OutcomeExceedsSnapshotHealth_Throws()
        {
            var outcome = EffectOutcome.FromDamage(new DamageResult(0, 20));
            var cast = new CastRecord(1, 0, 0, Card("test_a", 1), 1, new[] { outcome });
            var result = new FightResult(FightWinner.Hero, 1, new[] { cast });
            var snapshots = new[]
            {
                new CombatantSnapshot(0, 10, 10, 0, new[] { "test_a" }),
                new CombatantSnapshot(1, 10, 10, 0, new[] { "test_a" }),
            };

            Assert.Throws<InvalidOperationException>(() => CombatLog.Build(snapshots, result));
        }

        // Test double: damages the target but reports no outcome, as a faulty future effect could.
        private sealed class UnreportedDamageEffect : IEffect
        {
            private readonly int _amount;

            public UnreportedDamageEffect(int amount)
            {
                _amount = amount;
            }

            public EffectOutcome Apply(EffectContext context)
            {
                context.Target.TakeDamage(_amount);
                return EffectOutcome.None;
            }
        }
    }
}
