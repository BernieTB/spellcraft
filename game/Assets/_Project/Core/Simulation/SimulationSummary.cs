using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Core.Combat.Log;

namespace Game.Core.Simulation
{
    /// <summary>
    /// Aggregated results of a batch of seeded fights: outcomes, fight length and what each card did. Built by
    /// <see cref="SimulationSummaryBuilder"/>, usually through <see cref="FightBatch.Run"/>. Immutable. Serialises to
    /// JSON with <see cref="ToJson"/>.
    /// </summary>
    /// <remarks>
    /// The summary only holds simulation results, never wall-clock measurements, so the same setup and seeds always
    /// give the same JSON, byte for byte.
    /// </remarks>
    public sealed class SimulationSummary
    {
        internal SimulationSummary(
            long firstSeed,
            int fightCount,
            int heroWins,
            int enemyWins,
            int timeouts,
            long totalTicks,
            int minTicks,
            int maxTicks,
            IReadOnlyList<CardStatistics> cards)
        {
            FirstSeed = firstSeed;
            FightCount = fightCount;
            HeroWins = heroWins;
            EnemyWins = enemyWins;
            Timeouts = timeouts;
            TotalTicks = totalTicks;
            MinTicks = minTicks;
            MaxTicks = maxTicks;
            Cards = cards;
        }

        /// <summary>Seed of the first fight. Fight <c>i</c> (from 0) used seed <c>FirstSeed + i</c>.</summary>
        public long FirstSeed { get; }

        /// <summary>Seed of the last fight.</summary>
        public long LastSeed => FirstSeed + FightCount - 1;

        /// <summary>Number of fights, at least 1.</summary>
        public int FightCount { get; }

        /// <summary>Fights won by the hero.</summary>
        public int HeroWins { get; }

        /// <summary>Fights won by the enemies.</summary>
        public int EnemyWins { get; }

        /// <summary>Fights that hit their maximum number of ticks with no winner.</summary>
        public int Timeouts { get; }

        /// <summary>Share of fights won by the hero, from 0 to 1.</summary>
        public double HeroWinRate => (double)HeroWins / FightCount;

        /// <summary>Share of fights won by the enemies, from 0 to 1.</summary>
        public double EnemyWinRate => (double)EnemyWins / FightCount;

        /// <summary>Share of fights that timed out, from 0 to 1.</summary>
        public double TimeoutRate => (double)Timeouts / FightCount;

        /// <summary>Sum of the ticks of every fight (<see cref="Combat.FightResult.Ticks"/>).</summary>
        public long TotalTicks { get; }

        /// <summary>Average fight length, in ticks.</summary>
        public double AverageTicks => (double)TotalTicks / FightCount;

        /// <summary>Shortest fight, in ticks.</summary>
        public int MinTicks { get; }

        /// <summary>Longest fight, in ticks.</summary>
        public int MaxTicks { get; }

        /// <summary>
        /// Statistics per side and card id, for every card cast at least once, ordered by side (hero first) then by
        /// card id (ordinal).
        /// </summary>
        public IReadOnlyList<CardStatistics> Cards { get; }

        /// <summary>
        /// Writes the summary as indented JSON with a fixed field order and invariant culture. Rates and averages are
        /// rounded to 4 decimals.
        /// </summary>
        public string ToJson()
        {
            var builder = new StringBuilder();
            builder.Append("{\n");
            Field(builder, 1, "fightCount", Integer(FightCount), true);
            Field(builder, 1, "firstSeed", Integer(FirstSeed), true);
            Field(builder, 1, "lastSeed", Integer(LastSeed), true);

            Open(builder, 1, "outcomes");
            Outcome(builder, "hero", HeroWins, HeroWinRate, true);
            Outcome(builder, "enemies", EnemyWins, EnemyWinRate, true);
            Outcome(builder, "timeout", Timeouts, TimeoutRate, false);
            Close(builder, 1, true);

            Open(builder, 1, "ticks");
            Field(builder, 2, "average", Fraction(AverageTicks), true);
            Field(builder, 2, "min", Integer(MinTicks), true);
            Field(builder, 2, "max", Integer(MaxTicks), true);
            Field(builder, 2, "total", Integer(TotalTicks), false);
            Close(builder, 1, true);

            Indent(builder, 1);
            JsonWriter.AppendName(builder, "cards");
            builder.Append(Cards.Count == 0 ? "[" : "[\n");
            for (var i = 0; i < Cards.Count; i++)
            {
                var card = Cards[i];
                Indent(builder, 2);
                builder.Append('{');
                JsonWriter.AppendField(builder, "side", SideName(card.Side));
                builder.Append(',');
                JsonWriter.AppendField(builder, "cardId", card.CardId);
                InlineField(builder, "casts", Integer(card.Casts));
                InlineField(builder, "damage", Integer(card.Damage));
                InlineField(builder, "healthLost", Integer(card.HealthLost));
                InlineField(builder, "shieldAbsorbed", Integer(card.ShieldAbsorbed));
                InlineField(builder, "averageDamagePerFight", Fraction(card.AverageDamagePerFight));
                InlineField(builder, "healed", Integer(card.Healed));
                InlineField(builder, "shieldGained", Integer(card.ShieldGained));
                builder.Append(i < Cards.Count - 1 ? "},\n" : "}\n");
            }

            if (Cards.Count > 0)
            {
                Indent(builder, 1);
            }

            builder.Append("]\n}\n");
            return builder.ToString();
        }

        /// <summary>The JSON name of a side: <c>hero</c> or <c>enemies</c>.</summary>
        public static string SideName(SimulationSide side)
        {
            switch (side)
            {
                case SimulationSide.Hero:
                    return "hero";
                case SimulationSide.Enemies:
                    return "enemies";
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown side.");
            }
        }

        private static void Outcome(StringBuilder builder, string name, int count, double rate, bool comma)
        {
            Indent(builder, 2);
            JsonWriter.AppendName(builder, name);
            builder.Append('{');
            JsonWriter.AppendName(builder, "count");
            builder.Append(Integer(count));
            InlineField(builder, "rate", Fraction(rate));
            builder.Append(comma ? "},\n" : "}\n");
        }

        private static void Field(StringBuilder builder, int depth, string name, string rawValue, bool comma)
        {
            Indent(builder, depth);
            JsonWriter.AppendName(builder, name);
            builder.Append(rawValue).Append(comma ? ",\n" : "\n");
        }

        private static void InlineField(StringBuilder builder, string name, string rawValue)
        {
            builder.Append(',');
            JsonWriter.AppendName(builder, name);
            builder.Append(rawValue);
        }

        private static void Open(StringBuilder builder, int depth, string name)
        {
            Indent(builder, depth);
            JsonWriter.AppendName(builder, name);
            builder.Append("{\n");
        }

        private static void Close(StringBuilder builder, int depth, bool comma)
        {
            Indent(builder, depth);
            builder.Append(comma ? "},\n" : "}\n");
        }

        private static void Indent(StringBuilder builder, int depth) => builder.Append(' ', depth * 2);

        private static string Integer(long value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Fraction(double value) =>
            Math.Round(value, 4, MidpointRounding.AwayFromZero).ToString("0.0###", CultureInfo.InvariantCulture);
    }
}
