using System.Globalization;
using System.Text;
using Game.Core.Combat;

namespace Game.Core.Tests.Combat
{
    /// <summary>
    /// Compact textual trace of a fight, for determinism and golden tests: one line per resolved cast, then one
    /// line with the result. Test-only; the real combat event log is separate work (#14).
    /// </summary>
    /// <remarks>
    /// Cast line: <c>tick=T caster=C pos=P card=ID target=X absorbed=A healthLost=H healed=E shieldGained=S</c>,
    /// with combatant indices as in <see cref="Fight.HeroIndex"/> (0 = hero, 1..N = enemies) and the outcome summed
    /// over the card's effects. Last line: <c>winner=W ticks=N</c>. Lines end with <c>\n</c>.
    /// </remarks>
    public static class FightTrace
    {
        public static string Format(FightResult result)
        {
            var builder = new StringBuilder();
            foreach (var cast in result.Casts)
            {
                var outcome = cast.Outcome;
                builder.Append(string.Format(
                    CultureInfo.InvariantCulture,
                    "tick={0} caster={1} pos={2} card={3} target={4} absorbed={5} healthLost={6} healed={7} shieldGained={8}\n",
                    cast.Tick,
                    cast.CasterIndex,
                    cast.Position,
                    cast.Card.Id,
                    cast.TargetIndex,
                    outcome.Damage.AbsorbedByShield,
                    outcome.Damage.HealthLost,
                    outcome.Healed,
                    outcome.ShieldGained));
            }

            builder.Append(string.Format(CultureInfo.InvariantCulture, "winner={0} ticks={1}\n", result.Winner, result.Ticks));
            return builder.ToString();
        }
    }
}
