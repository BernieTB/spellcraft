using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Unity.Tests.Balance
{
    /// <summary>Plays the bots over a range of seeds and formats the results as a Markdown table (#125).</summary>
    public static class BalanceReport
    {
        public static List<BotRunResult> Play(BalanceContent content, BotKind kind, StepPolicy policy, int seeds, Action<BalanceBot> configure = null)
        {
            var bot = new BalanceBot(content, kind, policy, new Dictionary<string, double>());
            configure?.Invoke(bot);
            var results = new List<BotRunResult>();
            for (ulong seed = 1; seed <= (ulong)seeds; seed++)
            {
                results.Add(bot.Play(seed));
            }

            return results;
        }

        public static string Table(BalanceContent content, int seeds, IEnumerable<BotKind> kinds = null, IEnumerable<StepPolicy> policies = null, Action<BalanceBot> configure = null)
        {
            var text = new StringBuilder();
            text.AppendLine("| Bot | Path | Win % | Dies in regular | Dies in mini-boss 1 | Dies in mini-boss 2 | Dies to professor | Fights | Run ticks | Regular fight ticks | Mini-boss ticks | Professor ticks | Time-outs % | Longest fight | Minutes (est.) | Level | Rooms |");
            text.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var policy in policies ?? new[] { StepPolicy.FullBiome, StepPolicy.ShortPath })
            {
                foreach (var kind in kinds ?? new[] { BotKind.Naive, BotKind.Intermediate, BotKind.Expert })
                {
                    text.AppendLine(Row(kind, policy, Play(content, kind, policy, seeds, configure)));
                }
            }

            return text.ToString();
        }

        public static string Row(BotKind kind, StepPolicy policy, List<BotRunResult> results)
        {
            double Pct(int count) => 100d * count / results.Count;
            var regularTicks = results.Sum(r => r.RegularTicks) / (double)Math.Max(1, results.Sum(r => r.RegularFights));
            var miniTicks = results.Sum(r => r.MiniBossTicks) / (double)Math.Max(1, results.Sum(r => r.MiniBossFights));
            var reachedProfessor = results.Where(r => r.ProfessorTicks > 0).ToList();
            var professorTicks = reachedProfessor.Count == 0 ? 0 : reachedProfessor.Average(r => r.ProfessorTicks);
            return $"| {kind} | {policy} | {Pct(results.Count(r => r.Won)):F1} | {Pct(results.Count(r => r.DeathStage == "Regular")):F1} | "
                + $"{Pct(results.Count(r => r.DeathStage == "MiniBoss ROOM_01")):F1} | {Pct(results.Count(r => r.DeathStage == "MiniBoss ROOM_02")):F1} | {Pct(results.Count(r => r.DeathStage == "Professor")):F1} | "
                + $"{results.Average(r => r.Fights):F1} | {results.Average(r => r.Ticks):F0} | {regularTicks:F1} | {miniTicks:F1} | {professorTicks:F1} | "
                + $"{Pct(results.Count(r => r.TimedOut)):F1} | {results.Max(r => r.LongestFight)} | {results.Average(r => r.Seconds) / 60d:F1} | {results.Average(r => r.Level):F1} | {results.Average(r => r.RoomsCleared):F1} |";
        }
    }
}
