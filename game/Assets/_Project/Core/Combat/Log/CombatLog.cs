using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Game.Core.Effects;

namespace Game.Core.Combat.Log
{
    /// <summary>
    /// The ordered record of a fight: the combatants as they started, every event in the order it happened, and
    /// how the fight ended. Read by tests, the headless runner, the debug fight viewer and, later, the recap.
    /// Immutable. Serialises to text (<see cref="ToText"/>) and JSON (<see cref="ToJson"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built from a <see cref="FightResult"/> and the starting snapshots (<see cref="Build"/>), usually through
    /// <see cref="CombatLogRecorder.Record"/>. The log reads the outcomes the fight computed; it applies no rule of
    /// its own, it only keeps a running total of health and shield to report the state after each event.
    /// </para>
    /// <para>
    /// Order (deterministic): casts in resolution order (<see cref="FightResult.Casts"/>); for each cast, a
    /// <see cref="CombatEventKind.CardCast"/> event, then the effects in the card's order. For each effect: its
    /// damage, then a <see cref="CombatEventKind.Death"/> if that damage brought the target to zero health, then
    /// its healing, then its shield gain. Parts that changed nothing (zero amount) produce no event.
    /// </para>
    /// <para>
    /// Neighbour bonuses: the <see cref="CombatEventKind.CardCast"/> event carries the bonus the cast received and
    /// the part it wasted (<see cref="CombatEvent.Bonus"/>, <see cref="CombatEvent.WastedBonus"/>, from
    /// <see cref="CastRecord"/>). The used part is already in the amounts of the effect events.
    /// </para>
    /// </remarks>
    public sealed class CombatLog
    {
        private CombatLog(
            IReadOnlyList<CombatantSnapshot> combatants,
            IReadOnlyList<CombatEvent> events,
            FightWinner winner,
            int ticks)
        {
            Combatants = combatants;
            Events = events;
            Winner = winner;
            Ticks = ticks;
        }

        /// <summary>The combatants as they started the fight, by fight index.</summary>
        public IReadOnlyList<CombatantSnapshot> Combatants { get; }

        /// <summary>Every event, in order. <see cref="CombatEvent.Sequence"/> equals the index in this list.</summary>
        public IReadOnlyList<CombatEvent> Events { get; }

        /// <summary>The winning side, or <see cref="FightWinner.None"/> on timeout.</summary>
        public FightWinner Winner { get; }

        /// <summary>Number of ticks simulated.</summary>
        public int Ticks { get; }

        /// <summary>
        /// Builds the log of a fight that has run.
        /// </summary>
        /// <param name="combatants">
        /// Every combatant of the fight as it was before the fight ran, by fight index (index <c>i</c> at position
        /// <c>i</c>). See <see cref="CombatantSnapshot.Of"/>.
        /// </param>
        /// <param name="result">The result of <see cref="Fight.Run"/>.</param>
        /// <exception cref="ArgumentNullException">An argument or a snapshot is null.</exception>
        /// <exception cref="ArgumentException">
        /// A snapshot index does not match its position, or a cast refers to an unknown combatant.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The outcomes do not fit the snapshots (health or shield would go out of range).
        /// </exception>
        public static CombatLog Build(IReadOnlyList<CombatantSnapshot> combatants, FightResult result)
        {
            if (combatants == null)
            {
                throw new ArgumentNullException(nameof(combatants));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var count = combatants.Count;
            var health = new int[count];
            var shield = new int[count];
            var maxHealth = new int[count];
            var snapshots = new CombatantSnapshot[count];
            for (var i = 0; i < count; i++)
            {
                var snapshot = combatants[i] ?? throw new ArgumentNullException(nameof(combatants), $"Snapshot {i} is null.");
                if (snapshot.Index != i)
                {
                    throw new ArgumentException($"Snapshot at position {i} has index {snapshot.Index}.", nameof(combatants));
                }

                snapshots[i] = snapshot;
                health[i] = snapshot.Health;
                shield[i] = snapshot.Shield;
                maxHealth[i] = snapshot.MaxHealth;
            }

            var events = new List<CombatEvent>();

            foreach (var cast in result.Casts)
            {
                var caster = cast.CasterIndex;
                var target = cast.TargetIndex;
                if (caster < 0 || caster >= count || target < 0 || target >= count)
                {
                    throw new ArgumentException(
                        $"Cast on tick {cast.Tick} refers to combatant {caster} or {target}, but the log has {count}.",
                        nameof(result));
                }

                void Add(
                    CombatEventKind kind,
                    int subject,
                    int amount,
                    int absorbed,
                    int lost,
                    EffectBonus bonus = default,
                    EffectBonus wastedBonus = default)
                {
                    events.Add(new CombatEvent(
                        events.Count,
                        cast.Tick,
                        kind,
                        cast.Card.Id,
                        cast.Position,
                        caster,
                        subject,
                        amount,
                        absorbed,
                        lost,
                        health[subject],
                        shield[subject],
                        bonus,
                        wastedBonus));
                }

                Add(CombatEventKind.CardCast, target, 0, 0, 0, cast.Bonus, cast.WastedBonus);

                foreach (var outcome in cast.EffectOutcomes)
                {
                    var damage = outcome.Damage;
                    if (damage.Total > 0)
                    {
                        shield[target] -= damage.AbsorbedByShield;
                        health[target] -= damage.HealthLost;
                        Check(shield[target] >= 0 && health[target] >= 0, cast, target);
                        Add(CombatEventKind.Damage, target, damage.Total, damage.AbsorbedByShield, damage.HealthLost);

                        if (damage.HealthLost > 0 && health[target] == 0)
                        {
                            Add(CombatEventKind.Death, target, 0, 0, 0);
                        }
                    }

                    if (outcome.Healed > 0)
                    {
                        health[caster] += outcome.Healed;
                        Check(health[caster] <= maxHealth[caster], cast, caster);
                        Add(CombatEventKind.Heal, caster, outcome.Healed, 0, 0);
                    }

                    if (outcome.ShieldGained > 0)
                    {
                        shield[caster] = checked(shield[caster] + outcome.ShieldGained);
                        Add(CombatEventKind.ShieldGain, caster, outcome.ShieldGained, 0, 0);
                    }
                }
            }

            return new CombatLog(
                new ReadOnlyCollection<CombatantSnapshot>(snapshots),
                new ReadOnlyCollection<CombatEvent>(events),
                result.Winner,
                result.Ticks);
        }

        /// <summary>
        /// Writes the log as plain text, one line per item, each ending with <c>\n</c>:
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>one line per combatant: <c>combatant=I maxHealth=M health=H shield=S line=ID,ID,...</c>;</item>
        /// <item>one line per event: <c>tick=T event=K caster=C pos=P card=ID target=X</c>, followed for damage by
        /// <c>amount=A absorbed=B healthLost=L</c>, for heal and shield by <c>amount=A</c>, and for every kind but
        /// cast by the target's state after the event, <c>health=H shield=S</c>;</item>
        /// <item>a cast that received a neighbour bonus also has, for each kind with a bonus, in the order damage,
        /// heal, shield: <c>bonusK=N</c> (received), followed by <c>wastedK=W</c> when part of it was wasted, with
        /// <c>K</c> one of <c>Damage</c>, <c>Heal</c>, <c>Shield</c> (for example
        /// <c>bonusDamage=3 bonusHeal=4 wastedHeal=4</c>). A cast without bonus has none of these fields;</item>
        /// <item>a last line <c>winner=W ticks=N</c>.</item>
        /// </list>
        /// <para>Event names: <c>cast</c>, <c>damage</c>, <c>heal</c>, <c>shield</c>, <c>death</c>. Winner names:
        /// <c>none</c>, <c>hero</c>, <c>enemies</c>. Numbers use the invariant culture. Card ids are written as
        /// they are, unescaped: the text is for reading and golden diffs; parse <see cref="ToJson"/> instead.</para>
        /// </remarks>
        public string ToText()
        {
            var builder = new StringBuilder();
            foreach (var combatant in Combatants)
            {
                builder.Append("combatant=").Append(Number(combatant.Index))
                    .Append(" maxHealth=").Append(Number(combatant.MaxHealth))
                    .Append(" health=").Append(Number(combatant.Health))
                    .Append(" shield=").Append(Number(combatant.Shield))
                    .Append(" line=").Append(string.Join(",", combatant.SpellLineCardIds))
                    .Append('\n');
            }

            foreach (var e in Events)
            {
                builder.Append("tick=").Append(Number(e.Tick))
                    .Append(" event=").Append(KindName(e.Kind))
                    .Append(" caster=").Append(Number(e.CasterIndex))
                    .Append(" pos=").Append(Number(e.Position))
                    .Append(" card=").Append(e.CardId)
                    .Append(" target=").Append(Number(e.TargetIndex));

                switch (e.Kind)
                {
                    case CombatEventKind.CardCast:
                        AppendBonusText(builder, e, BonusKind.Damage, "Damage");
                        AppendBonusText(builder, e, BonusKind.Heal, "Heal");
                        AppendBonusText(builder, e, BonusKind.Shield, "Shield");
                        break;
                    case CombatEventKind.Damage:
                        builder.Append(" amount=").Append(Number(e.Amount))
                            .Append(" absorbed=").Append(Number(e.AbsorbedByShield))
                            .Append(" healthLost=").Append(Number(e.HealthLost));
                        break;
                    case CombatEventKind.Heal:
                    case CombatEventKind.ShieldGain:
                        builder.Append(" amount=").Append(Number(e.Amount));
                        break;
                }

                if (e.Kind != CombatEventKind.CardCast)
                {
                    builder.Append(" health=").Append(Number(e.TargetHealth))
                        .Append(" shield=").Append(Number(e.TargetShield));
                }

                builder.Append('\n');
            }

            builder.Append("winner=").Append(WinnerName(Winner))
                .Append(" ticks=").Append(Number(Ticks))
                .Append('\n');
            return builder.ToString();
        }

        /// <summary>
        /// Writes the log as JSON: an object with <c>winner</c>, <c>ticks</c>, <c>combatants</c> and <c>events</c>.
        /// Every event has all its fields (<c>sequence</c>, <c>tick</c>, <c>kind</c>, <c>card</c>, <c>position</c>,
        /// <c>caster</c>, <c>target</c>, <c>amount</c>, <c>absorbed</c>, <c>healthLost</c>, <c>targetHealth</c>,
        /// <c>targetShield</c>, <c>bonus</c>, <c>wasted</c>), with the same names as <see cref="ToText"/> for kinds and
        /// winner. <c>bonus</c> and <c>wasted</c> are objects with <c>damage</c>, <c>heal</c> and <c>shield</c>, in
        /// that order: the neighbour bonus a cast received and the part of it wasted, all zero on other events and on
        /// casts without bonus, so every event has the same shape. One combatant or event per line, lines end with
        /// <c>\n</c>; the output is deterministic.
        /// </summary>
        public string ToJson()
        {
            var builder = new StringBuilder();
            builder.Append("{\n");
            builder.Append("  ");
            JsonWriter.AppendField(builder, "winner", WinnerName(Winner));
            builder.Append(",\n  ");
            JsonWriter.AppendField(builder, "ticks", Ticks);
            builder.Append(",\n  ");
            JsonWriter.AppendName(builder, "combatants");
            builder.Append('[');
            for (var i = 0; i < Combatants.Count; i++)
            {
                var c = Combatants[i];
                builder.Append(i == 0 ? "\n    {" : ",\n    {");
                JsonWriter.AppendField(builder, "index", c.Index);
                builder.Append(',');
                JsonWriter.AppendField(builder, "maxHealth", c.MaxHealth);
                builder.Append(',');
                JsonWriter.AppendField(builder, "health", c.Health);
                builder.Append(',');
                JsonWriter.AppendField(builder, "shield", c.Shield);
                builder.Append(',');
                JsonWriter.AppendName(builder, "spellLine");
                builder.Append('[');
                for (var j = 0; j < c.SpellLineCardIds.Count; j++)
                {
                    if (j > 0)
                    {
                        builder.Append(',');
                    }

                    JsonWriter.AppendString(builder, c.SpellLineCardIds[j]);
                }

                builder.Append("]}");
            }

            builder.Append(Combatants.Count == 0 ? "]" : "\n  ]");
            builder.Append(",\n  ");
            JsonWriter.AppendName(builder, "events");
            builder.Append('[');
            for (var i = 0; i < Events.Count; i++)
            {
                var e = Events[i];
                builder.Append(i == 0 ? "\n    {" : ",\n    {");
                JsonWriter.AppendField(builder, "sequence", e.Sequence);
                builder.Append(',');
                JsonWriter.AppendField(builder, "tick", e.Tick);
                builder.Append(',');
                JsonWriter.AppendField(builder, "kind", KindName(e.Kind));
                builder.Append(',');
                JsonWriter.AppendField(builder, "card", e.CardId);
                builder.Append(',');
                JsonWriter.AppendField(builder, "position", e.Position);
                builder.Append(',');
                JsonWriter.AppendField(builder, "caster", e.CasterIndex);
                builder.Append(',');
                JsonWriter.AppendField(builder, "target", e.TargetIndex);
                builder.Append(',');
                JsonWriter.AppendField(builder, "amount", e.Amount);
                builder.Append(',');
                JsonWriter.AppendField(builder, "absorbed", e.AbsorbedByShield);
                builder.Append(',');
                JsonWriter.AppendField(builder, "healthLost", e.HealthLost);
                builder.Append(',');
                JsonWriter.AppendField(builder, "targetHealth", e.TargetHealth);
                builder.Append(',');
                JsonWriter.AppendField(builder, "targetShield", e.TargetShield);
                builder.Append(',');
                AppendBonusJson(builder, "bonus", e.Bonus);
                builder.Append(',');
                AppendBonusJson(builder, "wasted", e.WastedBonus);
                builder.Append('}');
            }

            builder.Append(Events.Count == 0 ? "]" : "\n  ]");
            builder.Append("\n}\n");
            return builder.ToString();
        }

        /// <summary>Stable lower-case name of an event kind, as used by <see cref="ToText"/> and <see cref="ToJson"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a known kind.</exception>
        public static string KindName(CombatEventKind kind)
        {
            switch (kind)
            {
                case CombatEventKind.CardCast:
                    return "cast";
                case CombatEventKind.Damage:
                    return "damage";
                case CombatEventKind.Heal:
                    return "heal";
                case CombatEventKind.ShieldGain:
                    return "shield";
                case CombatEventKind.Death:
                    return "death";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown combat event kind.");
            }
        }

        /// <summary>Stable lower-case name of a winner, as used by <see cref="ToText"/> and <see cref="ToJson"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="winner"/> is not a known value.</exception>
        public static string WinnerName(FightWinner winner)
        {
            switch (winner)
            {
                case FightWinner.None:
                    return "none";
                case FightWinner.Hero:
                    return "hero";
                case FightWinner.Enemies:
                    return "enemies";
                default:
                    throw new ArgumentOutOfRangeException(nameof(winner), winner, "Unknown fight winner.");
            }
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static void AppendBonusText(StringBuilder builder, CombatEvent e, BonusKind kind, string name)
        {
            var received = e.Bonus.Get(kind);
            if (received == 0)
            {
                return;
            }

            builder.Append(" bonus").Append(name).Append('=').Append(Number(received));
            var wasted = e.WastedBonus.Get(kind);
            if (wasted > 0)
            {
                builder.Append(" wasted").Append(name).Append('=').Append(Number(wasted));
            }
        }

        private static void AppendBonusJson(StringBuilder builder, string name, EffectBonus bonus)
        {
            JsonWriter.AppendName(builder, name);
            builder.Append('{');
            JsonWriter.AppendField(builder, "damage", bonus.Damage);
            builder.Append(',');
            JsonWriter.AppendField(builder, "heal", bonus.Heal);
            builder.Append(',');
            JsonWriter.AppendField(builder, "shield", bonus.Shield);
            builder.Append('}');
        }

        private static void Check(bool consistent, CastRecord cast, int combatant)
        {
            if (!consistent)
            {
                throw new InvalidOperationException(
                    $"Cast of '{cast.Card.Id}' on tick {cast.Tick} puts combatant {combatant} out of range: "
                    + "the snapshots do not match the fight.");
            }
        }
    }
}
