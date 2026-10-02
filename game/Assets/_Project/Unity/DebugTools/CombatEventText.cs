using System;
using System.Globalization;
using Game.Core.Combat;
using Game.Core.Combat.Log;

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

        /// <summary>One line describing <paramref name="e"/>, prefixed with its tick.</summary>
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
                    return string.Format(CultureInfo.InvariantCulture, "{0} casts at {1}", source, target);
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
                default:
                    return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", source, e.Kind, target);
            }
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
