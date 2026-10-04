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
    /// Live editing of the hero's spell line during a regular fight (ADR 0012, issue #97).
    /// </summary>
    public class LineEditingFightTests
    {
        // Placeholder ids and arbitrary test data, not real content or balance values.
        private const ulong Seed = 1UL;
        private const int Capacity = 5;
        private const int Sturdy = 1000;
        private const int MaxTicks = 50;
        private const int Bonus = 10;

        private const int Hero = Fight.HeroIndex;

        // --- Helpers ---

        private static CardDefinition Idle(string id, int castTime) => new CardDefinition(id, castTime, new IEffect[0]);

        private static CardDefinition Hit(string id, int castTime, int damage, params NeighbourModifier[] modifiers) =>
            new CardDefinition(id, castTime, new IEffect[] { new DealDamageEffect(damage) }, modifiers);

        private static NeighbourModifier NextDamage(int amount) =>
            new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, amount);

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

        private static Fight EditableFight(FightParticipant hero, params CardDefinition[] reserve) =>
            new Fight(hero, new[] { Dummy() }, MaxTicks, new Pcg32Random(Seed), reserve, true);

        private static List<(int Tick, int Position, string Card)> HeroCasts(FightResult result)
        {
            var casts = new List<(int, int, string)>();
            foreach (var cast in result.Casts)
            {
                if (cast.CasterIndex == Hero)
                {
                    casts.Add((cast.Tick, cast.Position, cast.Card.Id));
                }
            }

            return casts;
        }

        private static IReadOnlyList<string> Ids(IReadOnlyList<CardDefinition> cards)
        {
            var ids = new List<string>();
            foreach (var card in cards)
            {
                ids.Add(card.Id);
            }

            return ids;
        }

        private static CastRecord HeroCastOnTick(FightResult result, int tick)
        {
            foreach (var cast in result.Casts)
            {
                if (cast.CasterIndex == Hero && cast.Tick == tick)
                {
                    return cast;
                }
            }

            Assert.Fail($"No hero cast on tick {tick}.");
            return null;
        }

        // --- Fights without line edits ---

        [Test]
        public void Constructor_WithoutReserve_DoesNotAllowLineEdits()
        {
            var fight = new Fight(Participant(Sturdy, Idle("test_card_01", 1)), new[] { Dummy() }, MaxTicks, new Pcg32Random(Seed));

            Assert.That(fight.LineEditsAllowed, Is.False);
        }

        [Test]
        public void ApplyLineChange_FixedLine_Throws()
        {
            var hero = Participant(Sturdy, Idle("test_card_01", 1), Idle("test_card_02", 1));
            var fight = new Fight(hero, new[] { Dummy() }, MaxTicks, new Pcg32Random(Seed), new CardDefinition[0], false);

            Assert.Throws<InvalidOperationException>(() => fight.ApplyLineChange(LineChange.Move(1, 0, 1)));
        }

        [Test]
        public void Run_FixedLineWithChanges_Throws()
        {
            var hero = Participant(Sturdy, Idle("test_card_01", 1), Idle("test_card_02", 1));
            var fight = new Fight(hero, new[] { Dummy() }, MaxTicks, new Pcg32Random(Seed), new CardDefinition[0], false);

            Assert.Throws<InvalidOperationException>(() => fight.Run(new[] { LineChange.Move(1, 0, 1) }));
        }

        [Test]
        public void Run_EditableFightWithoutChanges_MatchesFixedFight()
        {
            var cards = new[] { Hit("test_card_01", 1, 1, NextDamage(Bonus)), Hit("test_card_02", 2, 1) };
            var fixedResult = new Fight(Participant(Sturdy, cards), new[] { Dummy() }, MaxTicks, new Pcg32Random(Seed)).Run();
            var editableResult = EditableFight(Participant(Sturdy, cards), Idle("test_card_03", 1)).Run();

            Assert.That(HeroCasts(editableResult), Is.EqualTo(HeroCasts(fixedResult)));
            Assert.That(editableResult.LineChanges, Is.Empty);
        }

        // --- Rules ---

        [Test]
        public void Move_BeforeFirstTick_ChangesTheFirstCast()
        {
            var fight = EditableFight(Participant(Sturdy, Idle("test_card_01", 1), Idle("test_card_02", 1), Idle("test_card_03", 1)));

            var result = fight.Run(new[] { LineChange.Move(1, 2, 0) });

            Assert.That(HeroCasts(result).GetRange(0, 3), Is.EqualTo(new[]
            {
                (1, 0, "test_card_03"),
                (2, 1, "test_card_01"),
                (3, 2, "test_card_02"),
            }));
        }

        [Test]
        public void SwapWithReserve_CardBeingCast_FinishesItsCast()
        {
            var fight = EditableFight(
                Participant(Sturdy, Idle("test_card_01", 3), Idle("test_card_02", 1)),
                Idle("test_card_03", 1));

            var result = fight.Run(new[] { LineChange.SwapWithReserve(2, 0, 0) });

            Assert.That(HeroCasts(result).GetRange(0, 3), Is.EqualTo(new[]
            {
                (3, 0, "test_card_01"),
                (4, 1, "test_card_02"),
                (5, 0, "test_card_03"),
            }));
        }

        [Test]
        public void SwapWithReserve_LineCardTakesTheReserveIndex()
        {
            var fight = EditableFight(
                Participant(Sturdy, Idle("test_card_01", 1), Idle("test_card_02", 1)),
                Idle("test_card_03", 1),
                Idle("test_card_04", 1));

            fight.ApplyLineChange(LineChange.SwapWithReserve(1, 1, 1));

            Assert.That(Ids(fight.HeroLine), Is.EqualTo(new[] { "test_card_01", "test_card_04" }));
            Assert.That(Ids(fight.HeroReserve), Is.EqualTo(new[] { "test_card_03", "test_card_02" }));
        }

        [Test]
        public void Move_CardBeingCast_ResolvesAtItsNewPositionThenContinuesAfterIt()
        {
            var fight = EditableFight(Participant(
                Sturdy,
                Idle("test_card_01", 3),
                Idle("test_card_02", 1),
                Idle("test_card_03", 1),
                Idle("test_card_04", 1)));

            var result = fight.Run(new[] { LineChange.Move(2, 0, 2) });

            // Line after the move: 02, 03, 01, 04. Card 01 keeps casting and resolves at position 2.
            Assert.That(HeroCasts(result).GetRange(0, 3), Is.EqualTo(new[]
            {
                (3, 2, "test_card_01"),
                (4, 3, "test_card_04"),
                (5, 0, "test_card_02"),
            }));
        }

        [Test]
        public void Move_OtherCardAcrossTheCardBeingCast_KeepsTheCastWithItsCard()
        {
            var fight = EditableFight(Participant(
                Sturdy,
                Idle("test_card_01", 1),
                Idle("test_card_02", 3),
                Idle("test_card_03", 1)));

            // Card 02 starts on tick 2; on tick 3 card 01 moves behind it, so card 02 is now at position 0.
            var result = fight.Run(new[] { LineChange.Move(3, 0, 2) });

            Assert.That(HeroCasts(result).GetRange(1, 2), Is.EqualTo(new[]
            {
                (4, 0, "test_card_02"),
                (5, 1, "test_card_03"),
            }));
        }

        [Test]
        public void PendingBonus_StaysAtItsPosition_AndGoesToTheCardNowThere()
        {
            var fight = EditableFight(Participant(
                Sturdy,
                Hit("test_card_01", 1, 1, NextDamage(Bonus)),
                Hit("test_card_02", 1, 1),
                Hit("test_card_03", 1, 2)));

            // Card 01 resolves on tick 1 and leaves a bonus on position 1; on tick 2 card 03 is moved there.
            var result = fight.Run(new[] { LineChange.Move(2, 2, 1) });

            var cast = HeroCastOnTick(result, 2);
            Assert.That(cast.Card.Id, Is.EqualTo("test_card_03"));
            Assert.That(cast.Bonus.Damage, Is.EqualTo(Bonus));
            Assert.That(cast.Outcome.Damage.Total, Is.EqualTo(2 + Bonus));
        }

        [Test]
        public void PendingBonus_TakenByTheCardBeingCast_StaysWithItWhenItLeaves()
        {
            var fight = EditableFight(
                Participant(Sturdy, Hit("test_card_01", 1, 1, NextDamage(Bonus)), Hit("test_card_02", 3, 1)),
                Hit("test_card_03", 1, 1));

            // Card 02 starts on tick 2 with the bonus; it is swapped out on tick 3 and still resolves with it.
            var result = fight.Run(new[] { LineChange.SwapWithReserve(3, 1, 0) });

            var cast = HeroCastOnTick(result, 4);
            Assert.That(cast.Card.Id, Is.EqualTo("test_card_02"));
            Assert.That(cast.Bonus.Damage, Is.EqualTo(Bonus));
            Assert.That(HeroCastOnTick(result, 6).Bonus.Damage, Is.EqualTo(Bonus), "Card 01 boosts the card now at position 1.");
            Assert.That(HeroCastOnTick(result, 6).Card.Id, Is.EqualTo("test_card_03"));
        }

        [Test]
        public void SeveralChangesOnOneTick_ApplyInOrder()
        {
            var fight = EditableFight(Participant(Sturdy, Idle("test_card_01", 1), Idle("test_card_02", 1), Idle("test_card_03", 1)));

            var result = fight.Run(new[] { LineChange.Move(1, 0, 2), LineChange.Move(1, 0, 1) });

            // 01,02,03 -> 02,03,01 -> 03,02,01
            Assert.That(HeroCasts(result)[0], Is.EqualTo((1, 0, "test_card_03")));
            Assert.That(result.LineChanges.Count, Is.EqualTo(2));
        }

        [Test]
        public void Run_ChangeAfterTheFightEnds_IsNotApplied()
        {
            var hero = Participant(Sturdy, Hit("test_card_01", 1, 5), Idle("test_card_02", 1));
            var enemy = Participant(5, Idle("test_card_99", MaxTicks + 1));
            var fight = new Fight(hero, new[] { enemy }, MaxTicks, new Pcg32Random(Seed), new CardDefinition[0], true);

            var result = fight.Run(new[] { LineChange.Move(MaxTicks, 0, 1) });

            Assert.That(result.Winner, Is.EqualTo(FightWinner.Hero));
            Assert.That(result.LineChanges, Is.Empty);
        }

        // --- Step by step ---

        [Test]
        public void StepWithChanges_GivesTheSameFightAsRunWithTheSchedule()
        {
            CardDefinition[] Cards() => new[]
            {
                Hit("test_card_01", 1, 1, NextDamage(Bonus)),
                Hit("test_card_02", 2, 1),
                Hit("test_card_03", 1, 3),
            };

            var schedule = new[] { LineChange.Move(3, 2, 0), LineChange.SwapWithReserve(6, 1, 0) };
            var scheduled = EditableFight(Participant(Sturdy, Cards()), Idle("test_card_04", 1)).Run(schedule);

            var stepped = EditableFight(Participant(Sturdy, Cards()), Idle("test_card_04", 1));
            var next = 0;
            while (!stepped.IsOver)
            {
                while (next < schedule.Length && schedule[next].Tick == stepped.Tick + 1)
                {
                    stepped.ApplyLineChange(schedule[next++]);
                }

                stepped.Step();
            }

            var result = stepped.Run();
            Assert.That(HeroCasts(result), Is.EqualTo(HeroCasts(scheduled)));
            Assert.That(result.LineChanges.Count, Is.EqualTo(2));
        }

        [Test]
        public void ApplyLineChange_NotStampedWithTheNextTick_Throws()
        {
            var fight = EditableFight(Participant(Sturdy, Idle("test_card_01", 1), Idle("test_card_02", 1)));
            fight.Step();

            Assert.Throws<ArgumentException>(() => fight.ApplyLineChange(LineChange.Move(1, 0, 1)));
            Assert.Throws<ArgumentException>(() => fight.ApplyLineChange(LineChange.Move(3, 0, 1)));
        }

        [Test]
        public void ApplyLineChange_AfterTheFight_Throws()
        {
            var fight = EditableFight(Participant(Sturdy, Idle("test_card_01", 1), Idle("test_card_02", 1)));
            fight.Run();

            Assert.Throws<InvalidOperationException>(() => fight.ApplyLineChange(LineChange.Move(MaxTicks + 1, 0, 1)));
            Assert.Throws<InvalidOperationException>(() => fight.Step());
        }

        // --- Validation ---

        [Test]
        public void LineChange_InvalidTickOrIndex_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LineChange.Move(0, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => LineChange.Move(1, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => LineChange.Move(1, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => LineChange.SwapWithReserve(1, 0, -1));
        }

        [Test]
        public void Run_ChangeOutsideTheLineOrReserve_ThrowsBeforeRunning()
        {
            CardDefinition[] Cards() => new[] { Idle("test_card_01", 1), Idle("test_card_02", 1) };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                EditableFight(Participant(Sturdy, Cards())).Run(new[] { LineChange.Move(1, 2, 0) }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                EditableFight(Participant(Sturdy, Cards())).Run(new[] { LineChange.Move(1, 0, 2) }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                EditableFight(Participant(Sturdy, Cards()), Idle("test_card_03", 1)).Run(new[] { LineChange.SwapWithReserve(1, 0, 1) }));

            var fight = EditableFight(Participant(Sturdy, Cards()));
            Assert.Throws<ArgumentOutOfRangeException>(() => fight.Run(new[] { LineChange.Move(1, 0, 1), LineChange.Move(2, 5, 0) }));
            Assert.That(fight.Tick, Is.EqualTo(0), "A bad schedule is rejected before any tick runs.");
        }

        [Test]
        public void Run_ChangesOutOfTickOrderOrAfterTheLastTick_Throws()
        {
            CardDefinition[] Cards() => new[] { Idle("test_card_01", 1), Idle("test_card_02", 1) };

            Assert.Throws<ArgumentException>(() =>
                EditableFight(Participant(Sturdy, Cards())).Run(new[] { LineChange.Move(3, 0, 1), LineChange.Move(2, 0, 1) }));
            Assert.Throws<ArgumentException>(() =>
                EditableFight(Participant(Sturdy, Cards())).Run(new[] { LineChange.Move(MaxTicks + 1, 0, 1) }));
        }

        [Test]
        public void Constructor_NullReserveOrReserveCard_Throws()
        {
            var hero = Participant(Sturdy, Idle("test_card_01", 1));

            Assert.Throws<ArgumentNullException>(() =>
                new Fight(hero, new[] { Dummy() }, MaxTicks, new Pcg32Random(Seed), null, true));
            Assert.Throws<ArgumentNullException>(() =>
                new Fight(hero, new[] { Dummy() }, MaxTicks, new Pcg32Random(Seed), new CardDefinition[] { null }, true));
        }

        // --- Log ---

        private static CombatLog GoldenLog()
        {
            var hero = Participant(20, Hit("test_card_01", 1, 2, NextDamage(3)), Hit("test_card_02", 2, 1));
            var enemy = Participant(100, Hit("test_card_09", 3, 1));
            return CombatLogRecorder.Record(
                hero,
                new[] { enemy },
                6,
                new Pcg32Random(Seed),
                new[] { Hit("test_card_03", 1, 5) },
                true,
                new[] { LineChange.SwapWithReserve(3, 1, 0) });
        }

        [Test]
        public void Record_WithLineChange_GoldenText()
        {
            const string expected =
                "combatant=0 maxHealth=20 health=20 shield=0 line=test_card_01,test_card_02\n"
                + "combatant=1 maxHealth=100 health=100 shield=0 line=test_card_09\n"
                + "tick=1 event=cast caster=0 pos=0 card=test_card_01 target=1\n"
                + "tick=1 event=damage caster=0 pos=0 card=test_card_01 target=1 amount=2 absorbed=0 healthLost=2 health=98 shield=0\n"
                + "tick=3 event=line caster=0 pos=1 card=test_card_02 target=0 change=swap reserve=0 incoming=test_card_03 health=20 shield=0\n"
                + "tick=3 event=cast caster=0 pos=1 card=test_card_02 target=1 bonusDamage=3\n"
                + "tick=3 event=damage caster=0 pos=1 card=test_card_02 target=1 amount=4 absorbed=0 healthLost=4 health=94 shield=0\n"
                + "tick=3 event=cast caster=1 pos=0 card=test_card_09 target=0\n"
                + "tick=3 event=damage caster=1 pos=0 card=test_card_09 target=0 amount=1 absorbed=0 healthLost=1 health=19 shield=0\n"
                + "tick=4 event=cast caster=0 pos=0 card=test_card_01 target=1\n"
                + "tick=4 event=damage caster=0 pos=0 card=test_card_01 target=1 amount=2 absorbed=0 healthLost=2 health=92 shield=0\n"
                + "tick=5 event=cast caster=0 pos=1 card=test_card_03 target=1 bonusDamage=3\n"
                + "tick=5 event=damage caster=0 pos=1 card=test_card_03 target=1 amount=8 absorbed=0 healthLost=8 health=84 shield=0\n"
                + "tick=6 event=cast caster=0 pos=0 card=test_card_01 target=1\n"
                + "tick=6 event=damage caster=0 pos=0 card=test_card_01 target=1 amount=2 absorbed=0 healthLost=2 health=82 shield=0\n"
                + "tick=6 event=cast caster=1 pos=0 card=test_card_09 target=0\n"
                + "tick=6 event=damage caster=1 pos=0 card=test_card_09 target=0 amount=1 absorbed=0 healthLost=1 health=18 shield=0\n"
                + "winner=none ticks=6\n";

            Assert.That(GoldenLog().ToText(), Is.EqualTo(expected));
        }

        [Test]
        public void Record_WithLineChange_JsonHasTheChangeOnlyOnLineEvents()
        {
            var json = GoldenLog().ToJson();

            StringAssert.Contains(
                "\"kind\":\"line\",\"card\":\"test_card_02\",\"position\":1,\"caster\":0,\"target\":0",
                json);
            StringAssert.Contains(
                "\"change\":{\"kind\":\"swap\",\"to\":-1,\"reserve\":0,\"incoming\":\"test_card_03\"}",
                json);
            Assert.That(json.Split(new[] { "\"change\"" }, StringSplitOptions.None).Length - 1, Is.EqualTo(1));
        }

        [Test]
        public void Record_SameSeedAndChanges_SameLog()
        {
            Assert.That(GoldenLog().ToJson(), Is.EqualTo(GoldenLog().ToJson()));
        }

        [Test]
        public void Recap_LogWithLineChange_CountsTheIncomingCard()
        {
            var recap = FightRecapBuilder.Build(GoldenLog());

            CardRecap incoming = null;
            foreach (var card in recap.Hero.Cards)
            {
                if (card.CardId == "test_card_03")
                {
                    incoming = card;
                }
            }

            Assert.That(incoming, Is.Not.Null);
            Assert.That(incoming.Position, Is.EqualTo(1));
            Assert.That(incoming.Casts, Is.EqualTo(1));
            Assert.That(incoming.Damage, Is.EqualTo(8));
            Assert.That(recap.Hero.Cards.Count, Is.EqualTo(3), "The two starting cards plus the incoming one; the change itself adds none.");
        }
    }
}
