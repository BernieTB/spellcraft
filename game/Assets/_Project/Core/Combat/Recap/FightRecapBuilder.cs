using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Game.Core.Combat.Log;
using Game.Core.Effects;

namespace Game.Core.Combat.Recap
{
    /// <summary>
    /// Builds the <see cref="FightRecap"/> of a fight from its <see cref="CombatLog"/> only (ADR 0014). Pure and
    /// deterministic: the same log always gives the same recap.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Per card.</b> Every event is credited to the card whose cast caused it, identified by its owner, its
    /// position and its id: casts and the bonus they received or wasted from <see cref="CombatEventKind.CardCast"/>,
    /// damage from <see cref="CombatEventKind.Damage"/> (shield absorbed plus health lost), healing and shield from
    /// <see cref="CombatEventKind.Heal"/> and <see cref="CombatEventKind.ShieldGain"/>.
    /// <see cref="CombatEventKind.LineChanged"/> events (live line editing in regular fights, ADR 0012 and
    /// ADR 0015) are not output and are skipped; after a change, the cards seen at a new position get their own
    /// entries, after those of the starting line.
    /// </para>
    /// <para>
    /// <b>Turning point</b> (fights not won). The state of the fight is read after the last event of every tick
    /// that has events, plus the starting state (tick 0). The hero is <i>behind</i> when the share of health it has
    /// left is lower than the enemies' share: hero health / hero max health &lt; sum of the enemies' health / sum
    /// of their max health (dead enemies count with zero health). Shares are compared, not raw health, because a
    /// professor can have far more health than the hero, and shield is not counted. A tie is not behind. Confirmed
    /// by the owner on 2026-10-04. The turning point is the first tick from which the hero
    /// stays behind until the end. A defeat always ends behind; a fight lost on the time limit may not, and then
    /// has no turning point and no <see cref="DefeatAnalysis"/>.
    /// </para>
    /// <para>
    /// <b>Analysed loop.</b> The causes are looked for in the hero's last loop of casts before the turning point:
    /// the last N casts of the hero resolved on or before it, N being the length of the hero's starting spell
    /// line. If fewer casts happened by then, the first N casts of the fight are used.
    /// </para>
    /// <para>
    /// <b>Causes</b>, by priority, the first one found being the main cause:
    /// <list type="number">
    /// <item><see cref="DefeatCause.WastedBonuses"/>: a hero cast in the loop wasted part of its bonus;</item>
    /// <item><see cref="DefeatCause.ShieldBroken"/>: damage brought the hero's shield from above zero to zero
    /// between the start of the loop and the turning point, both included (the latest such hit is
    /// reported);</item>
    /// <item><see cref="DefeatCause.WeakestCard"/>: always present, the hero card with the lowest output (damage
    /// plus healing plus shield) over the loop; ties go to the lowest position.</item>
    /// </list>
    /// The priority puts first what the player controls in the spell line (confirmed by the owner on 2026-10-04).
    /// No threshold is involved.
    /// </para>
    /// </remarks>
    public static class FightRecapBuilder
    {
        /// <summary>Builds the recap of <paramref name="log"/>.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="log"/> is null.</exception>
        /// <exception cref="ArgumentException">The log has no combatant or no enemy.</exception>
        public static FightRecap Build(CombatLog log)
        {
            if (log == null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            if (log.Combatants.Count < 2)
            {
                throw new ArgumentException("A combat log needs the hero and at least one enemy.", nameof(log));
            }

            var outcome = OutcomeOf(log.Winner);
            var combatants = BuildCombatants(log);
            var defeat = outcome == FightRecapOutcome.Victory ? null : AnalyseDefeat(log);
            return new FightRecap(outcome, log.Ticks, combatants, defeat);
        }

        private static FightRecapOutcome OutcomeOf(FightWinner winner)
        {
            switch (winner)
            {
                case FightWinner.Hero:
                    return FightRecapOutcome.Victory;
                case FightWinner.Enemies:
                    return FightRecapOutcome.Defeat;
                case FightWinner.None:
                    return FightRecapOutcome.TimeLimit;
                default:
                    throw new ArgumentOutOfRangeException(nameof(winner), winner, "Unknown fight winner.");
            }
        }

        // --- Per card ---

        private sealed class CardTotals
        {
            public CardTotals(int position, string cardId, bool inStartingLine)
            {
                Position = position;
                CardId = cardId;
                InStartingLine = inStartingLine;
            }

            public int Position { get; }
            public string CardId { get; }
            public bool InStartingLine { get; }
            public int Casts;
            public int Damage;
            public int Healing;
            public int Shield;
            public EffectBonus Bonus;
            public EffectBonus Wasted;

            public CardRecap ToRecap(int combatantIndex) =>
                new CardRecap(combatantIndex, Position, CardId, Casts, Damage, Healing, Shield, Bonus, Wasted);
        }

        private static IReadOnlyList<CombatantRecap> BuildCombatants(CombatLog log)
        {
            var count = log.Combatants.Count;
            var totals = new List<CardTotals>[count];
            for (var i = 0; i < count; i++)
            {
                var ids = log.Combatants[i].SpellLineCardIds;
                totals[i] = new List<CardTotals>(ids.Count);
                for (var position = 0; position < ids.Count; position++)
                {
                    totals[i].Add(new CardTotals(position, ids[position], true));
                }
            }

            foreach (var e in log.Events)
            {
                if (e.Kind == CombatEventKind.LineChanged || e.Kind == CombatEventKind.Evolved)
                {
                    // A change or an evolution is not output of a card; the casts around it show which card played
                    // where, and an evolved card keeps its id.
                    continue;
                }

                var card = Find(totals[e.CasterIndex], e.Position, e.CardId);
                switch (e.Kind)
                {
                    case CombatEventKind.CardCast:
                        card.Casts++;
                        card.Bonus = card.Bonus.Plus(e.Bonus);
                        card.Wasted = card.Wasted.Plus(e.WastedBonus);
                        break;
                    case CombatEventKind.Damage:
                        card.Damage = checked(card.Damage + e.Amount);
                        break;
                    case CombatEventKind.Heal:
                        card.Healing = checked(card.Healing + e.Amount);
                        break;
                    case CombatEventKind.ShieldGain:
                        card.Shield = checked(card.Shield + e.Amount);
                        break;
                }
            }

            var recaps = new CombatantRecap[count];
            for (var i = 0; i < count; i++)
            {
                var ordered = Ordered(totals[i]);
                var cards = new List<CardRecap>(ordered.Count);
                var wasted = new List<BonusWaste>();
                var received = EffectBonus.None;
                var wastedTotal = EffectBonus.None;
                foreach (var card in ordered)
                {
                    cards.Add(card.ToRecap(i));
                    received = received.Plus(card.Bonus);
                    wastedTotal = wastedTotal.Plus(card.Wasted);
                    foreach (var kind in Kinds)
                    {
                        var amount = card.Wasted.Get(kind);
                        if (amount > 0)
                        {
                            wasted.Add(new BonusWaste(i, card.Position, card.CardId, kind, amount, WastedBonusReason.NoEffectOfKind));
                        }
                    }
                }

                wasted.Sort((a, b) =>
                {
                    var byPosition = a.Position.CompareTo(b.Position);
                    if (byPosition != 0)
                    {
                        return byPosition;
                    }

                    var byKind = a.Kind.CompareTo(b.Kind);
                    return byKind != 0 ? byKind : string.CompareOrdinal(a.CardId, b.CardId);
                });

                recaps[i] = new CombatantRecap(
                    i,
                    new ReadOnlyCollection<CardRecap>(cards),
                    new ReadOnlyCollection<BonusWaste>(wasted),
                    received,
                    wastedTotal);
            }

            return new ReadOnlyCollection<CombatantRecap>(recaps);
        }

        private static readonly BonusKind[] Kinds = { BonusKind.Damage, BonusKind.Heal, BonusKind.Shield };

        // The entry for this card, created after the others when the log shows a card the starting line did not
        // have at that position (a line changed during the fight).
        private static CardTotals Find(List<CardTotals> cards, int position, string cardId)
        {
            foreach (var card in cards)
            {
                if (card.Position == position && string.Equals(card.CardId, cardId, StringComparison.Ordinal))
                {
                    return card;
                }
            }

            var added = new CardTotals(position, cardId, false);
            cards.Add(added);
            return added;
        }

        // Starting line by position, then cards found only in the log, by position then id.
        private static List<CardTotals> Ordered(List<CardTotals> cards)
        {
            var starting = new List<CardTotals>();
            var extra = new List<CardTotals>();
            foreach (var card in cards)
            {
                (card.InStartingLine ? starting : extra).Add(card);
            }

            extra.Sort((a, b) =>
            {
                var byPosition = a.Position.CompareTo(b.Position);
                return byPosition != 0 ? byPosition : string.CompareOrdinal(a.CardId, b.CardId);
            });
            starting.AddRange(extra);
            return starting;
        }

        // --- Where the chain broke ---

        // One cast of the hero and what its effects produced.
        private sealed class HeroCast
        {
            public HeroCast(CombatEvent cast)
            {
                Cast = cast;
            }

            public CombatEvent Cast { get; }
            public long Output;
        }

        private static DefeatAnalysis AnalyseDefeat(CombatLog log)
        {
            var turningPoint = FindTurningPoint(log);
            if (turningPoint < 0)
            {
                return null;
            }

            var heroCasts = HeroCasts(log);
            var window = Window(heroCasts, log.Combatants[Fight.HeroIndex].SpellLineCardIds.Count, turningPoint);
            var windowStart = window.Count > 0 ? window[0].Cast.Tick : turningPoint;
            var windowEnd = window.Count > 0 ? window[window.Count - 1].Cast.Tick : turningPoint;

            var causes = new List<DefeatCause>(3);

            var wasted = EffectBonus.None;
            foreach (var cast in window)
            {
                wasted = wasted.Plus(cast.Cast.WastedBonus);
            }

            if (!wasted.IsNone)
            {
                causes.Add(DefeatCause.WastedBonuses);
            }

            var breaker = FindShieldBreak(log, Math.Min(windowStart, turningPoint), turningPoint);
            if (breaker != null)
            {
                causes.Add(DefeatCause.ShieldBroken);
            }

            Weakest(log, window, out var weakestPosition, out var weakestId, out var weakestOutput);
            causes.Add(DefeatCause.WeakestCard);

            return new DefeatAnalysis(
                turningPoint,
                windowStart,
                windowEnd,
                causes[0],
                new ReadOnlyCollection<DefeatCause>(causes),
                wasted,
                breaker?.Tick ?? -1,
                breaker?.CasterIndex ?? -1,
                breaker?.Position ?? -1,
                breaker?.CardId,
                weakestPosition,
                weakestId,
                weakestOutput);
        }

        // The first tick from which the hero stays behind until the end, or -1 if the hero is not behind at the end.
        private static int FindTurningPoint(CombatLog log)
        {
            var count = log.Combatants.Count;
            var health = new long[count];
            long enemyMax = 0;
            for (var i = 0; i < count; i++)
            {
                health[i] = log.Combatants[i].Health;
                if (i != Fight.HeroIndex)
                {
                    enemyMax += log.Combatants[i].MaxHealth;
                }
            }

            long heroMax = log.Combatants[Fight.HeroIndex].MaxHealth;

            bool Behind()
            {
                long enemyHealth = 0;
                for (var i = 0; i < count; i++)
                {
                    if (i != Fight.HeroIndex)
                    {
                        enemyHealth += health[i];
                    }
                }

                // heroHealth / heroMax < enemyHealth / enemyMax, without division.
                return checked(health[Fight.HeroIndex] * enemyMax) < checked(enemyHealth * heroMax);
            }

            // The tick of the latest state where the hero was not behind is followed by the turning point.
            var behind = Behind();
            var turningPoint = behind ? 0 : -1;
            var events = log.Events;
            for (var index = 0; index < events.Count; index++)
            {
                var e = events[index];
                health[e.TargetIndex] = e.TargetHealth;

                var lastOfTick = index == events.Count - 1 || events[index + 1].Tick != e.Tick;
                if (!lastOfTick)
                {
                    continue;
                }

                var now = Behind();
                if (now && !behind)
                {
                    turningPoint = e.Tick;
                }
                else if (!now)
                {
                    turningPoint = -1;
                }

                behind = now;
            }

            return behind ? turningPoint : -1;
        }

        private static List<HeroCast> HeroCasts(CombatLog log)
        {
            var casts = new List<HeroCast>();
            HeroCast current = null;
            foreach (var e in log.Events)
            {
                if (e.Kind == CombatEventKind.CardCast)
                {
                    current = e.CasterIndex == Fight.HeroIndex ? new HeroCast(e) : null;
                    if (current != null)
                    {
                        casts.Add(current);
                    }

                    continue;
                }

                if (current != null && e.Kind != CombatEventKind.Death)
                {
                    current.Output += e.Amount;
                }
            }

            return casts;
        }

        private static List<HeroCast> Window(List<HeroCast> casts, int lineLength, int turningPoint)
        {
            var size = Math.Max(1, lineLength);
            var byTurningPoint = 0;
            while (byTurningPoint < casts.Count && casts[byTurningPoint].Cast.Tick <= turningPoint)
            {
                byTurningPoint++;
            }

            if (byTurningPoint >= size)
            {
                return casts.GetRange(byTurningPoint - size, size);
            }

            return casts.GetRange(0, Math.Min(size, casts.Count));
        }

        // The latest damage event in [fromTick, toTick] that brought the hero's shield from above zero to zero.
        private static CombatEvent FindShieldBreak(CombatLog log, int fromTick, int toTick)
        {
            CombatEvent found = null;
            foreach (var e in log.Events)
            {
                if (e.Kind == CombatEventKind.Damage
                    && e.TargetIndex == Fight.HeroIndex
                    && e.AbsorbedByShield > 0
                    && e.TargetShield == 0
                    && e.Tick >= fromTick
                    && e.Tick <= toTick)
                {
                    found = e;
                }
            }

            return found;
        }

        // The hero card with the lowest output over the loop; ties go to the lowest position, then the card id. With
        // no cast at all, the first card of the starting line, with zero output.
        private static void Weakest(
            CombatLog log,
            List<HeroCast> window,
            out int position,
            out string cardId,
            out long output)
        {
            if (window.Count == 0)
            {
                var line = log.Combatants[Fight.HeroIndex].SpellLineCardIds;
                position = 0;
                cardId = line.Count > 0 ? line[0] : null;
                output = 0;
                return;
            }

            var totals = new List<(int Position, string CardId, long Output)>();
            foreach (var cast in window)
            {
                var index = totals.FindIndex(t =>
                    t.Position == cast.Cast.Position && string.Equals(t.CardId, cast.Cast.CardId, StringComparison.Ordinal));
                if (index < 0)
                {
                    totals.Add((cast.Cast.Position, cast.Cast.CardId, cast.Output));
                }
                else
                {
                    totals[index] = (totals[index].Position, totals[index].CardId, totals[index].Output + cast.Output);
                }
            }

            var best = totals[0];
            for (var i = 1; i < totals.Count; i++)
            {
                var candidate = totals[i];
                var compare = candidate.Output.CompareTo(best.Output);
                if (compare == 0)
                {
                    compare = candidate.Position.CompareTo(best.Position);
                }

                if (compare == 0)
                {
                    compare = string.CompareOrdinal(candidate.CardId, best.CardId);
                }

                if (compare < 0)
                {
                    best = candidate;
                }
            }

            position = best.Position;
            cardId = best.CardId;
            output = best.Output;
        }
    }
}
