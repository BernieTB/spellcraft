using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Core.Combat;
using Game.Core.Combat.Log;
using Game.Core.Effects;

namespace Game.Unity.DebugTools
{
    /// <summary>
    /// Human-readable lines for the debug fight viewer. Debug wording only, not player-facing text.
    /// </summary>
    public static class CombatEventText
    {
        /// <summary>"Hero" for <see cref="Fight.HeroIndex"/>, otherwise "Enemy N".</summary>
        public static string CombatantName(int index)
        {
            return index == Fight.HeroIndex
                ? "Hero"
                : string.Format(CultureInfo.InvariantCulture, "Enemy {0}", index);
        }

        /// <summary>
        /// One line describing <paramref name="e"/>, prefixed with its tick. A cast that received neighbour bonuses
        /// lists them, marking the wasted ones (for example "casts at Enemy 1 with +3 damage bonus, +4 heal bonus
        /// (wasted)").
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="e"/> is null.</exception>
        public static string Describe(CombatEvent e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            var source = string.Format(
                CultureInfo.InvariantCulture,
                "t{0}  {1} [{2}] {3}",
                e.Tick,
                CombatantName(e.CasterIndex),
                e.Position,
                e.CardId);
            var target = CombatantName(e.TargetIndex);

            switch (e.Kind)
            {
                case CombatEventKind.CardCast:
                    return string.Format(CultureInfo.InvariantCulture, "{0} casts at {1}{2}", source, target, BonusText(e));
                case CombatEventKind.Damage:
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} hits {1} for {2} ({3} on shield, {4} on health) -> {5} HP, {6} shield",
                        source,
                        target,
                        e.Amount,
                        e.AbsorbedByShield,
                        e.HealthLost,
                        e.TargetHealth,
                        e.TargetShield);
                case CombatEventKind.Heal:
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} heals {1} for {2} -> {3} HP",
                        source,
                        target,
                        e.Amount,
                        e.TargetHealth);
                case CombatEventKind.ShieldGain:
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} gives {1} {2} shield -> {3} shield",
                        source,
                        target,
                        e.Amount,
                        e.TargetShield);
                case CombatEventKind.Death:
                    return string.Format(CultureInfo.InvariantCulture, "{0} kills {1}", source, target);
                case CombatEventKind.LineChanged:
                    return e.LineChange.Kind == LineChangeKind.Move
                        ? string.Format(CultureInfo.InvariantCulture, "{0} moves to [{1}]", source, e.LineChange.ToPosition)
                        : string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} swapped with reserve {1} ({2})",
                            source,
                            e.LineChange.ReserveIndex,
                            e.IncomingCardId);
                default:
                    return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", source, e.Kind, target);
            }
        }

        // " with +3 damage bonus, +4 heal bonus (wasted)" for a cast that received bonuses, empty otherwise.
        private static string BonusText(CombatEvent e)
        {
            if (e.Bonus.IsNone)
            {
                return string.Empty;
            }

            var parts = new List<string>(3);
            AddBonusPart(parts, e, BonusKind.Damage, "damage");
            AddBonusPart(parts, e, BonusKind.Heal, "heal");
            AddBonusPart(parts, e, BonusKind.Shield, "shield");
            return " with " + string.Join(", ", parts);
        }

        private static void AddBonusPart(List<string> parts, CombatEvent e, BonusKind kind, string name)
        {
            var received = e.Bonus.Get(kind);
            if (received == 0)
            {
                return;
            }

            var wasted = e.WastedBonus.Get(kind);
            var suffix = wasted == 0
                ? string.Empty
                : wasted == received
                    ? " (wasted)"
                    : string.Format(CultureInfo.InvariantCulture, " ({0} wasted)", wasted);
            parts.Add(string.Format(CultureInfo.InvariantCulture, "+{0} {1} bonus{2}", received, name, suffix));
        }

        /// <summary>"Hero wins", "Enemies win" or "Timeout (no winner)".</summary>
        public static string Outcome(FightWinner winner)
        {
            switch (winner)
            {
                case FightWinner.Hero:
                    return "Hero wins";
                case FightWinner.Enemies:
                    return "Enemies win";
                default:
                    return "Timeout (no winner)";
            }
        }
    }
}
