using System;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;
using NUnit.Framework;

namespace Game.Core.Tests.Combat
{
    /// <summary>
    /// The read-only view of where a combatant is in its line and which bonuses wait (used by the run screen, #73).
    /// </summary>
    public class FightCastProgressTests
    {
        // Placeholder ids and arbitrary test data, not real content or balance values.
        private const int Sturdy = 1000;
        private const int MaxTicks = 50;

        private static CardDefinition Hit(string id, int castTime, int damage, params NeighbourModifier[] modifiers) =>
            new CardDefinition(id, castTime, new IEffect[] { new DealDamageEffect(damage) }, modifiers);

        private static FightParticipant Participant(params CardDefinition[] cards)
        {
            var line = new SpellLine<CardDefinition>(5);
            foreach (var card in cards)
            {
                line.Add(card);
            }

            return new FightParticipant(new Combatant(Sturdy, 0), line);
        }

        private static Fight NewFight(FightParticipant hero, bool edits = true) =>
            new Fight(hero, new[] { Participant(Hit("test_card_enemy", 2, 1)) }, MaxTicks, new Pcg32Random(1UL), new CardDefinition[0], edits);

        [Test]
        public void GetCastProgress_BeforeTheFirstTick_PointsAtThePositionCastNext()
        {
            var fight = NewFight(Participant(Hit("test_card_a", 3, 1), Hit("test_card_b", 3, 1)));

            var progress = fight.GetCastProgress(Fight.HeroIndex);

            Assert.IsFalse(progress.IsCasting);
            Assert.AreEqual(0, progress.Position);
            Assert.IsNull(progress.CardId);
        }

        [Test]
        public void GetCastProgress_DuringACast_ReportsCardAndElapsedTicks()
        {
            var fight = NewFight(Participant(Hit("test_card_a", 3, 1), Hit("test_card_b", 3, 1)));

            fight.Step();
            fight.Step();
            var progress = fight.GetCastProgress(Fight.HeroIndex);

            Assert.IsTrue(progress.IsCasting);
            Assert.AreEqual("test_card_a", progress.CardId);
            Assert.AreEqual(0, progress.Position);
            Assert.AreEqual(2, progress.ElapsedTicks);
            Assert.AreEqual(3, progress.CastTime);
        }

        [Test]
        public void GetCastProgress_AfterAResolution_PointsAtTheNextPosition()
        {
            var fight = NewFight(Participant(Hit("test_card_a", 2, 1), Hit("test_card_b", 2, 1)));

            fight.Step();
            fight.Step();
            var progress = fight.GetCastProgress(Fight.HeroIndex);

            Assert.IsFalse(progress.IsCasting);
            Assert.AreEqual(1, progress.Position);
        }

        [Test]
        public void GetCastProgress_ForAnEnemy_ReadsItsOwnLine()
        {
            var fight = NewFight(Participant(Hit("test_card_a", 3, 1)));

            fight.Step();
            var progress = fight.GetCastProgress(1);

            Assert.IsTrue(progress.IsCasting);
            Assert.AreEqual("test_card_enemy", progress.CardId);
            Assert.AreEqual(1, progress.ElapsedTicks);
        }

        [Test]
        public void GetCastProgress_UnknownCombatant_Throws()
        {
            var fight = NewFight(Participant(Hit("test_card_a", 3, 1)));

            Assert.Throws<ArgumentOutOfRangeException>(() => fight.GetCastProgress(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => fight.GetCastProgress(-1));
        }

        [Test]
        public void GetPendingBonus_AfterAModifierResolves_ShowsTheBonusOnTheNeighbour()
        {
            var booster = Hit("test_card_boost", 1, 1, new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, 4));
            var fight = NewFight(Participant(booster, Hit("test_card_b", 5, 1)));

            Assert.IsTrue(fight.GetPendingBonus(Fight.HeroIndex, 1).IsNone);
            fight.Step();

            Assert.AreEqual(4, fight.GetPendingBonus(Fight.HeroIndex, 1).Damage);
            Assert.IsTrue(fight.GetPendingBonus(Fight.HeroIndex, 0).IsNone);
        }

        [Test]
        public void GetPendingBonus_AfterALineEdit_StaysAtItsPosition()
        {
            var booster = Hit("test_card_boost", 1, 1, new NeighbourModifier(BonusKind.Damage, NeighbourDirection.Next, 4));
            var fight = NewFight(Participant(booster, Hit("test_card_b", 5, 1), Hit("test_card_c", 5, 1)));
            fight.Step();

            fight.ApplyLineChange(LineChange.Move(2, 1, 2));

            Assert.AreEqual(4, fight.GetPendingBonus(Fight.HeroIndex, 1).Damage);
        }

        [Test]
        public void GetPendingBonus_PositionOutOfRange_Throws()
        {
            var fight = NewFight(Participant(Hit("test_card_a", 3, 1)));

            Assert.Throws<ArgumentOutOfRangeException>(() => fight.GetPendingBonus(Fight.HeroIndex, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => fight.GetPendingBonus(Fight.HeroIndex, -1));
        }
    }
}
