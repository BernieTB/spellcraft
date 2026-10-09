using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Combat;
using Game.Core.Combat.Log;
using Game.Core.Combat.Recap;
using Game.Core.Effects;
using Game.Core.Randomness;
using Game.Core.SpellLines;
using Game.Unity.UI.Cards;

namespace Game.Unity.EditorTools.UI
{
    /// <summary>
    /// Small fights built by code and played by Core, so the screen preview (<see cref="ScreenPreviewWindow"/>) can
    /// show real recaps: one victory, one defeat and one fight that runs out of time.
    /// </summary>
    /// <remarks>
    /// Demo data for looking at screens, not game content: the cards are obvious placeholders (<c>demo_*</c>) and
    /// the numbers are chosen only to give each outcome. Nothing here reaches a player build (editor assembly).
    /// </remarks>
    public static class ScreenPreviewFights
    {
        private const int Seed = 1;

        // Every demo card built so far, by id, so the recap preview can show what each card does.
        private static readonly Dictionary<string, CardDefinition> Cards = new Dictionary<string, CardDefinition>();

        /// <summary>What the recap preview shows under a card: its summary, as the demo cards define it.</summary>
        public static CardSummary Summaries(CardRecap card)
        {
            return Cards.TryGetValue(card.CardId, out var definition) ? CardSummary.From(definition) : null;
        }

        /// <summary>The hero beats one enemy; one card boosts the next card's damage.</summary>
        public static CombatLog VictoryLog()
        {
            var hero = Participant(
                40,
                0,
                Card("demo_boost", 2, new IEffect[0], Next(BonusKind.Damage, 3)),
                Card("demo_strike", 3, new IEffect[] { new DealDamageEffect(4) }));
            var enemy = Participant(12, 0, Card("demo_enemy_hit", 5, new IEffect[] { new DealDamageEffect(2) }));
            return Record(hero, enemy, 200);
        }

        /// <summary>
        /// The hero loses: its first card gives a shield bonus to a card that only deals damage (wasted), and the
        /// enemy hits harder and faster than the hero can answer.
        /// </summary>
        public static CombatLog DefeatLog()
        {
            var hero = Participant(
                20,
                6,
                Card("demo_weaver", 2, new IEffect[] { new DealDamageEffect(2) }, Next(BonusKind.Shield, 3)),
                Card("demo_bolt", 3, new IEffect[] { new DealDamageEffect(3) }));
            var enemy = Participant(60, 0, Card("demo_crush", 4, new IEffect[] { new DealDamageEffect(6) }));
            return Record(hero, enemy, 200);
        }

        /// <summary>Both sides only gain shield, so nobody can win and the fight reaches its time limit.</summary>
        public static CombatLog TimeLimitLog()
        {
            var hero = Participant(10, 0, Card("demo_ward", 2, new IEffect[] { new GainShieldEffect(2) }));
            var enemy = Participant(10, 0, Card("demo_enemy_ward", 2, new IEffect[] { new GainShieldEffect(2) }));
            return Record(hero, enemy, 40);
        }

        /// <summary>The recap of <see cref="VictoryLog"/>.</summary>
        public static FightRecap Victory() => FightRecapBuilder.Build(VictoryLog());

        /// <summary>The recap of <see cref="DefeatLog"/>.</summary>
        public static FightRecap Defeat() => FightRecapBuilder.Build(DefeatLog());

        /// <summary>The recap of <see cref="TimeLimitLog"/>.</summary>
        public static FightRecap TimeLimit() => FightRecapBuilder.Build(TimeLimitLog());

        private static CombatLog Record(FightParticipant hero, FightParticipant enemy, int maxTicks)
        {
            return CombatLogRecorder.Record(hero, new List<FightParticipant> { enemy }, maxTicks, new Pcg32Random(Seed));
        }

        private static NeighbourModifier Next(BonusKind kind, int amount)
        {
            return new NeighbourModifier(kind, NeighbourDirection.Next, amount);
        }

        private static CardDefinition Card(
            string id,
            int castTime,
            IEffect[] effects,
            params NeighbourModifier[] modifiers)
        {
            var card = new CardDefinition(id, castTime, effects, modifiers);
            Cards[id] = card;
            return card;
        }

        private static FightParticipant Participant(int maxHealth, int shield, params CardDefinition[] cards)
        {
            var line = new SpellLine<CardDefinition>(cards.Length);
            foreach (var card in cards)
            {
                line.Add(card);
            }

            return new FightParticipant(new Combatant(maxHealth, shield), line);
        }
    }
}
