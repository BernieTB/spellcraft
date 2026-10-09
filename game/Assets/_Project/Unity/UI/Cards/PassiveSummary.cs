using System;
using System.Collections.Generic;
using Game.Core.Effects;
using Game.Core.Upgrades;

namespace Game.Unity.UI.Cards
{
    /// <summary>One distinct passive upgrade the hero owns, stacked: how many times taken and what they do together.</summary>
    public sealed class PassiveSummaryLine
    {
        public PassiveSummaryLine(string id, int count, string effectText)
        {
            Id = id;
            Count = count;
            EffectText = effectText;
        }

        public string Id { get; }

        /// <summary>How many times the upgrade was taken.</summary>
        public int Count { get; }

        /// <summary>The total effect ("+10 max health").</summary>
        public string EffectText { get; }

        /// <summary>One line for the run screen: "passive_id x2: +10 max health".</summary>
        public string Text => Count > 1 ? $"{Id} x{Count}: {EffectText}" : $"{Id}: {EffectText}";

        /// <summary>The hover details: id, how many times it was taken and the total effect.</summary>
        public string TooltipText => string.Join("\n", "Passive upgrade " + Id, $"Taken {Count} time(s)", EffectText);
    }

    /// <summary>
    /// Says what passive upgrades do (#123): a pure view-model shared by the level-up screen (one offered upgrade) and the
    /// run screen (the list of owned ones). No rule: the amounts come from Core.
    /// </summary>
    public static class PassiveSummary
    {
        /// <summary>What one upgrade changes ("+5 max health").</summary>
        /// <exception cref="ArgumentNullException"><paramref name="passive"/> is null.</exception>
        public static string Describe(PassiveUpgrade passive)
        {
            if (passive == null)
            {
                throw new ArgumentNullException(nameof(passive));
            }

            return Describe(passive.Kind, passive.EffectKind, passive.Amount);
        }

        /// <summary>The owned upgrades, same upgrades stacked, in the order first taken.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="upgrades"/> is null.</exception>
        public static IReadOnlyList<PassiveSummaryLine> DescribeOwned(PassiveUpgradeSet upgrades)
        {
            if (upgrades == null)
            {
                throw new ArgumentNullException(nameof(upgrades));
            }

            var order = new List<string>();
            var counts = new Dictionary<string, int>();
            var totals = new Dictionary<string, int>();
            var first = new Dictionary<string, PassiveUpgrade>();
            foreach (var upgrade in upgrades.Upgrades)
            {
                if (!counts.ContainsKey(upgrade.Id))
                {
                    order.Add(upgrade.Id);
                    counts[upgrade.Id] = 0;
                    totals[upgrade.Id] = 0;
                    first[upgrade.Id] = upgrade;
                }

                counts[upgrade.Id]++;
                totals[upgrade.Id] += upgrade.Amount;
            }

            var lines = new List<PassiveSummaryLine>(order.Count);
            foreach (var id in order)
            {
                var sample = first[id];
                lines.Add(new PassiveSummaryLine(id, counts[id], Describe(sample.Kind, sample.EffectKind, totals[id])));
            }

            return lines.AsReadOnly();
        }

        private static string Describe(PassiveUpgradeKind kind, BonusKind effectKind, int amount)
        {
            switch (kind)
            {
                case PassiveUpgradeKind.MaxHealth:
                    return $"+{amount} max health";
                case PassiveUpgradeKind.StartingShield:
                    return $"+{amount} starting shield each fight";
                case PassiveUpgradeKind.EffectAmount:
                    return $"+{amount} to every {CardSummary.KindWord(effectKind)} effect you cast";
                default:
                    return $"+{amount} to the neighbour bonuses your cards give";
            }
        }
    }
}
