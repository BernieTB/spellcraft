using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Combat.Log;
using Game.Core.Combat.Recap;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;
using NUnit.Framework;

namespace Game.Core.Tests.Combat
{
    /// <summary>
    /// Evolution of the hero's cards during a fight (ADR 0013, issue #79): casts are counted per card copy, a card
    /// evolves after the cast that reaches a stage and uses the new stage on its next cast.
    /// </summary>
    public class CardEvolutionFightTests
    {
        // Placeholder ids and arbitrary test data, not real content or balance values.
        private const ulong Seed = 1UL;
        private const int Capacity = 5;
        private const int Sturdy = 1000;
        private const int MaxTicks = 50;
        private const int BaseDamage = 1;
        private const int FirstCasts = 2;
        private const int FirstDamage = 5;
        private const int SecondCasts = 4;
        private const int SecondDamage = 20;

        private const int Hero = Fight.HeroIndex;

        // --- Helpers ---

        private static CardDefinition Idle(string id, int castTime) => new CardDefinition(id, castTime, new IEffect[0]);

        // A card that deals BaseDamage, then FirstDamage after 2 casts and SecondDamage after 4. Cast time 1.
        private static CardDefinition Evolving(string id = "test_card_01", int castTime = 1) => new CardDefinition(
            id,
            castTime,
            new IEffect[] { new DealDamageEffect(BaseDamage) },
            new NeighbourModifier[0],
            new[]
            {
                new CardEvolution(FirstCasts, new IEffect[] { new DealDamageEffect(FirstDamage) }, new NeighbourModifier[0]),
                new CardEvolution(SecondCasts, new IEffect[] { new DealDamageEffect(SecondDamage) }, new NeighbourModifier[0]),
            });

        private static SpellLine<CardDefinition> Line(params CardDefinition[] cards)
        {
            var line = new SpellLine<CardDefinition>(Capacity);
            foreach (var card in cards)
            {
                line.Add(card);
            }

            return line;
        }

        private static FightParticipant Participant(int health, params CardDefinition[] cards) =>
            new FightParticipant(new Combatant(health, 0), Line(cards));

        // An enemy that never resolves a card and survives everything the tests deal.
        private static FightParticipant Dummy() => Participant(Sturdy, Idle("test_card_99", MaxTicks + 1));

        private static Fight CountingFight(
            FightParticipant hero,
            int[] lineCasts,
            CardDefinition[] reserve = null,
            int[] reserveCasts = null,
            int maxTicks = MaxTicks)
        {
            reserve = reserve ?? new CardDefinition[0];
            return new Fight(
                hero,
                new[] { Dummy() },
                maxTicks,
                new Pcg32Random(Seed),
                reserve,
                true,
                lineCasts,
                reserveCasts ?? new int[reserve.Length]);
        }

        private static List<int> HeroDamages(FightResult result)
        {
            var damages = new List<int>();
            foreach (var cast in result.Casts)
            {
                if (cast.CasterIndex == Hero)
                {
                    damages.Add(cast.Outcome.Damage.Total);
                }
            }

            return damages;
        }

        private static List<int> HeroStages(FightResult result)
        {
            var stages = new List<int>();
            foreach (var cast in result.Casts)
            {
                if (cast.CasterIndex == Hero)
                {
                    stages.Add(cast.Card.Stage);
                }
            }

            return stages;
        }

        // --- Stages during a fight ---

        [Test]
        public void Run_CardReachingAStage_EvolvesAfterThatCastAndUsesItNext()
        {
            var fight = CountingFight(Participant(30, Evolving()), new[] { 0 }, maxTicks: 6);

            var result = fight.Run();

            Assert.That(HeroDamages(result), Is.EqualTo(new[] { 1, 1, 5, 5, 20, 20 }));
            Assert.That(HeroStages(result), Is.EqualTo(new[] { 0, 0, 1, 1, 2, 2 }));
            Assert.That(result.HeroCardCasts, Is.EqualTo(new[] { 6 }));
        }

        [Test]
        public void Run_Evolutions_AreRecordedOnTheCastThatReachedTheStage()
        {
            var fight = CountingFight(Participant(30, Evolving()), new[] { 0 }, maxTicks: 6);

            var result = fight.Run();

            Assert.That(result.Evolutions, Has.Count.EqualTo(2));
            Assert.That(
                (result.Evolutions[0].Tick, result.Evolutions[0].Position, result.Evolutions[0].Stage, result.Evolutions[0].Casts),
                Is.EqualTo((2, 0, 1, FirstCasts)));
            Assert.That(
                (result.Evolutions[1].Tick, result.Evolutions[1].Position, result.Evolutions[1].Stage, result.Evolutions[1].Casts),
                Is.EqualTo((4, 0, 2, SecondCasts)));
            Assert.That(result.Evolutions[0].Card.Id, Is.EqualTo("test_card_01"));
            Assert.That(result.Casts[result.Evolutions[0].CastIndex].Tick, Is.EqualTo(2));
            Assert.That(result.Casts[result.Evolutions[1].CastIndex].Tick, Is.EqualTo(4));
        }

        [Test]
        public void Run_Evolution_NeverChangesTheCastTime()
        {
            var fight = CountingFight(Participant(30, Evolving(castTime: 2)), new[] { 0 }, maxTicks: 14);

            var result = fight.Run();

            var ticks = new List<int>();
            foreach (var cast in result.Casts)
            {
                if (cast.CasterIndex == Hero)
                {
                    ticks.Add(cast.Tick);
                }
            }

            Assert.That(ticks, Is.EqualTo(new[] { 2, 4, 6, 8, 10, 12, 14 }));
            Assert.That(HeroStages(result), Is.EqualTo(new[] { 0, 0, 1, 1, 2, 2, 2 }));
        }

        [Test]
        public void Run_CardWithCastsFromEarlierFights_EvolvesSooner()
        {
            // One cast already counted: the first cast of this fight is the second one, so it reaches stage 1.
            var hero = Participant(30, Evolving());
            var fight = CountingFight(hero, new[] { FirstCasts - 1 }, maxTicks: 2);

            var result = fight.Run();

            Assert.That(HeroDamages(result), Is.EqualTo(new[] { 1, 5 }));
            Assert.That(result.Evolutions[0].Tick, Is.EqualTo(1));
            Assert.That(result.HeroCardCasts, Is.EqualTo(new[] { FirstCasts + 1 }));
        }

        [Test]
        public void Run_CardAlreadyAtItsLastStage_KeepsCountingWithoutEvolving()
        {
            var card = Evolving().AtStage(2);
            var fight = CountingFight(Participant(30, card), new[] { SecondCasts }, maxTicks: 3);

            var result = fight.Run();

            Assert.That(HeroDamages(result), Is.EqualTo(new[] { SecondDamage, SecondDamage, SecondDamage }));
            Assert.That(result.Evolutions, Is.Empty);
            Assert.That(result.HeroCardCasts, Is.EqualTo(new[] { SecondCasts + 3 }));
        }

        [Test]
        public void Run_CardWithoutEvolutions_CountsItsCasts()
        {
            var fight = CountingFight(Participant(30, Idle("test_card_02", 1)), new[] { 0 }, maxTicks: 4);

            var result = fight.Run();

            Assert.That(result.Evolutions, Is.Empty);
            Assert.That(result.HeroCardCasts, Is.EqualTo(new[] { 4 }));
        }

        [Test]
        public void Run_EnemiesCards_DoNotEvolveOrCount()
        {
            var enemy = Participant(Sturdy, Evolving("test_card_98"));
            var fight = new Fight(
                Participant(30, Idle("test_card_02", 1)),
                new[] { enemy },
                6,
                new Pcg32Random(Seed),
                new CardDefinition[0],
                true,
                new[] { 0 },
                new int[0]);

            var result = fight.Run();

            Assert.That(result.Evolutions, Is.Empty);
            Assert.That(result.HeroCardCasts, Is.EqualTo(new[] { 6 }));
            foreach (var cast in result.Casts)
            {
                Assert.That(cast.Card.Stage, Is.EqualTo(0));
            }
        }

        // --- Two copies, line changes, reserve ---

        [Test]
        public void Run_TwoCopiesOfTheSameCard_EvolveSeparately()
        {
            var card = Evolving();
            var fight = CountingFight(Participant(30, card, card), new[] { 0, 0 }, maxTicks: 6);

            var result = fight.Run();

            Assert.That(HeroDamages(result), Is.EqualTo(new[] { 1, 1, 1, 1, 5, 5 }));
            Assert.That(result.Evolutions, Has.Count.EqualTo(2));
            Assert.That((result.Evolutions[0].Tick, result.Evolutions[0].Position), Is.EqualTo((3, 0)));
            Assert.That((result.Evolutions[1].Tick, result.Evolutions[1].Position), Is.EqualTo((4, 1)));
            Assert.That(result.HeroCardCasts, Is.EqualTo(new[] { 3, 3 }));
        }

        [Test]
        public void Run_TwoCopiesWithDifferentCasts_EvolveAtDifferentTimes()
        {
            var card = Evolving();
            var fight = CountingFight(Participant(30, card, card), new[] { FirstCasts - 1, 0 }, maxTicks: 3);

            var result = fight.Run();

            // The first copy is one cast from stage 1; the second copy is not.
            Assert.That(HeroStages(result), Is.EqualTo(new[] { 0, 0, 1 }));
            Assert.That(result.Evolutions, Has.Count.EqualTo(1));
            Assert.That(result.Evolutions[0].Position, Is.EqualTo(0));
        }

        [Test]
        public void Run_MovedCard_KeepsItsCastsWhereverItGoes()
        {
            var hero = Participant(30, Evolving(), Idle("test_card_02", 1));
            var fight = CountingFight(hero, new[] { 0, 0 }, maxTicks: 2);

            // The evolving card casts on tick 1 (position 0), then moves to position 1 before its second cast.
            var result = fight.Run(new[] { LineChange.Move(2, 0, 1) });

            Assert.That(result.Evolutions, Has.Count.EqualTo(1));
            Assert.That((result.Evolutions[0].Tick, result.Evolutions[0].Position), Is.EqualTo((2, 1)));
            Assert.That(fight.HeroLine[1].Stage, Is.EqualTo(1));
            Assert.That(fight.HeroLine[0].Id, Is.EqualTo("test_card_02"));
            // Counts are listed by the copies' starting order: the evolving card first, then the other one.
            Assert.That(result.HeroCardCasts[0], Is.EqualTo(2));
        }

        [Test]
        public void Run_CardSwappedToTheReserve_KeepsItsCastsAndStage()
        {
            var hero = Participant(30, Evolving(), Idle("test_card_02", 1));
            var fight = CountingFight(hero, new[] { 1, 0 }, new[] { Idle("test_card_03", 1) }, new[] { 0 }, maxTicks: 3);

            // Tick 1: the evolving card (1 cast so far) resolves its second cast and evolves, then goes to the
            // reserve at the start of tick 2.
            var result = fight.Run(new[] { LineChange.SwapWithReserve(2, 0, 0) });

            Assert.That(result.Evolutions, Has.Count.EqualTo(1));
            Assert.That(fight.HeroReserve[0].Stage, Is.EqualTo(1));
            Assert.That(fight.HeroReserve[0].Id, Is.EqualTo("test_card_01"));
            // Tokens: line copies first (evolving card, other card), then the reserve copy.
            Assert.That(result.HeroCardCasts[0], Is.EqualTo(2));
        }

        [Test]
        public void Run_CardSwappedOutWhileCasting_EvolvesInTheReserve()
        {
            // The evolving card needs 3 ticks per cast: it starts on tick 1, goes to the reserve on tick 2, and its
            // cast still resolves on tick 3. That second cast brings it to stage 1, in the reserve.
            var hero = Participant(30, Evolving(castTime: 3));
            var fight = CountingFight(hero, new[] { FirstCasts - 1 }, new[] { Idle("test_card_03", 1) }, new[] { 0 });

            var result = fight.Run(new[] { LineChange.SwapWithReserve(2, 0, 0) });

            Assert.That(result.Evolutions, Has.Count.EqualTo(1));
            Assert.That((result.Evolutions[0].Tick, result.Evolutions[0].Position), Is.EqualTo((3, 0)));
            Assert.That(fight.HeroReserve[0].Id, Is.EqualTo("test_card_01"));
            Assert.That(fight.HeroReserve[0].Stage, Is.EqualTo(1));
            Assert.That(fight.HeroLine[0].Id, Is.EqualTo("test_card_03"));
            Assert.That(result.HeroCardCasts[0], Is.EqualTo(FirstCasts));
        }

        // --- Fights that do not count ---

        [Test]
        public void Run_WithoutCastCounts_DoesNotCountOrEvolve()
        {
            var fight = new Fight(
                Participant(30, Evolving()),
                new[] { Dummy() },
                6,
                new Pcg32Random(Seed),
                new CardDefinition[0],
                true);

            var result = fight.Run();

            Assert.That(HeroDamages(result), Is.EqualTo(new[] { 1, 1, 1, 1, 1, 1 }));
            Assert.That(result.Evolutions, Is.Empty);
            Assert.That(result.HeroCardCasts, Is.Empty);
        }

        // --- Validation ---

        [Test]
        public void Constructor_OnlyOneCastList_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Fight(
                Participant(30, Evolving()),
                new[] { Dummy() },
                MaxTicks,
                new Pcg32Random(Seed),
                new CardDefinition[0],
                true,
                new[] { 0 },
                null));
        }

        [Test]
        public void Constructor_CastListOfTheWrongLength_Throws()
        {
            Assert.Throws<ArgumentException>(() => CountingFight(Participant(30, Evolving()), new[] { 0, 0 }));
            Assert.Throws<ArgumentException>(() =>
                CountingFight(Participant(30, Evolving()), new[] { 0 }, new CardDefinition[0], new[] { 0 }));
        }

        [Test]
        public void Constructor_NegativeCasts_Throws()
        {
            Assert.Throws<ArgumentException>(() => CountingFight(Participant(30, Evolving()), new[] { -1 }));
        }

        [Test]
        public void Constructor_CardNotAtTheStageItsCastsHaveReached_Throws()
        {
            // 2 casts reach stage 1, but the card is given at stage 0.
            Assert.Throws<ArgumentException>(() => CountingFight(Participant(30, Evolving()), new[] { FirstCasts }));
            // The reserve is checked too.
            Assert.Throws<ArgumentException>(() => CountingFight(
                Participant(30, Idle("test_card_02", 1)),
                new[] { 0 },
                new[] { Evolving() },
                new[] { FirstCasts }));
        }

        // --- Determinism and the log ---

        private static CombatLog RecordGoldenFight()
        {
            // The enemy has 7 health: 1 + 1 on the first two casts, then 5 once the card has evolved.
            var hero = Participant(30, Evolving());
            var enemy = Participant(7, Idle("test_card_99", 99));
            return CombatLogRecorder.Record(
                hero,
                new[] { enemy },
                MaxTicks,
                new Pcg32Random(Seed),
                new CardDefinition[0],
                true,
                new LineChange[0],
                new[] { 0 },
                new int[0]);
        }

        [Test]
        public void Record_EvolutionDuringAFight_MatchesTheGoldenText()
        {
            var expected =
                "combatant=0 maxHealth=30 health=30 shield=0 line=test_card_01\n"
                + "combatant=1 maxHealth=7 health=7 shield=0 line=test_card_99\n"
                + "tick=1 event=cast caster=0 pos=0 card=test_card_01 target=1\n"
                + "tick=1 event=damage caster=0 pos=0 card=test_card_01 target=1 amount=1 absorbed=0 healthLost=1 health=6 shield=0\n"
                + "tick=2 event=cast caster=0 pos=0 card=test_card_01 target=1\n"
                + "tick=2 event=damage caster=0 pos=0 card=test_card_01 target=1 amount=1 absorbed=0 healthLost=1 health=5 shield=0\n"
                + "tick=2 event=evolution caster=0 pos=0 card=test_card_01 target=0 stage=1 casts=2 health=30 shield=0\n"
                + "tick=3 event=cast caster=0 pos=0 card=test_card_01 target=1\n"
                + "tick=3 event=damage caster=0 pos=0 card=test_card_01 target=1 amount=5 absorbed=0 healthLost=5 health=0 shield=0\n"
                + "tick=3 event=death caster=0 pos=0 card=test_card_01 target=1 health=0 shield=0\n"
                + "winner=hero ticks=3\n";

            Assert.That(RecordGoldenFight().ToText(), Is.EqualTo(expected));
        }

        [Test]
        public void Record_EvolutionEvent_HasAnEvolutionObjectInJson()
        {
            var json = RecordGoldenFight().ToJson();

            Assert.That(json, Does.Contain("\"kind\":\"evolution\""));
            Assert.That(json, Does.Contain("\"evolution\":{\"stage\":1,\"casts\":2}"));
            Assert.That(json.Split(new[] { "\"evolution\":{" }, StringSplitOptions.None), Has.Length.EqualTo(2));
        }

        [Test]
        public void Record_EvolutionEvent_CarriesStageCastsAndTheHeroState()
        {
            var log = RecordGoldenFight();

            var evolution = log.Events[4];
            Assert.That(evolution.Kind, Is.EqualTo(CombatEventKind.Evolved));
            Assert.That(evolution.EvolutionStage, Is.EqualTo(1));
            Assert.That(evolution.EvolutionCasts, Is.EqualTo(FirstCasts));
            Assert.That((evolution.CasterIndex, evolution.TargetIndex), Is.EqualTo((Hero, Hero)));
            Assert.That((evolution.TargetHealth, evolution.TargetShield), Is.EqualTo((30, 0)));
            Assert.That(log.HeroCardCasts, Is.EqualTo(new[] { 3 }));
        }

        [Test]
        public void Record_SameInputs_GiveTheSameLog()
        {
            Assert.That(RecordGoldenFight().ToText(), Is.EqualTo(RecordGoldenFight().ToText()));
            Assert.That(RecordGoldenFight().ToJson(), Is.EqualTo(RecordGoldenFight().ToJson()));
        }

        [Test]
        public void Build_EvolutionOfAnUnknownCast_Throws()
        {
            var card = Evolving();
            var snapshots = new[]
            {
                CombatantSnapshot.Of(0, Participant(30, card)),
                CombatantSnapshot.Of(1, Dummy()),
            };
            var evolved = card.AtStage(1);
            var result = new FightResult(
                FightWinner.None,
                3,
                new CastRecord[0],
                new LineChangeRecord[0],
                new[] { new EvolutionRecord(2, 0, evolved, 2, 0) },
                new[] { 2 });

            Assert.Throws<ArgumentException>(() => CombatLog.Build(snapshots, result));
        }

        [Test]
        public void EvolutionRecord_CardAtStageZero_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EvolutionRecord(1, 0, Evolving(), 2, 0));
        }

        [Test]
        public void Recap_FightWithAnEvolution_IgnoresTheEvolutionEvent()
        {
            var recap = FightRecapBuilder.Build(RecordGoldenFight());

            var heroCards = recap.Combatants[Hero].Cards;
            Assert.That(heroCards, Has.Count.EqualTo(1));
            Assert.That(heroCards[0].Casts, Is.EqualTo(3));
            Assert.That(heroCards[0].Damage, Is.EqualTo(BaseDamage + BaseDamage + FirstDamage));
        }
    }
}
