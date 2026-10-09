using System;
using Game.Core.Combat.Recap;
using Game.Core.Runs;

namespace Game.Unity.UI.Cards
{
    /// <summary>
    /// Finds what to show under each card of the recap (#123). The recap itself only knows card ids, so the summaries
    /// come from the fight report: the hero's line as it was when the fight began (each card at the stage it had
    /// then) and the lines of the enemies. A card that is not where the report says it was (the line was edited during
    /// the fight) gets no summary rather than a wrong one.
    /// </summary>
    public static class RecapCardSummaries
    {
        /// <summary>The lookup for the cards of <paramref name="report"/>'s fight.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="report"/> is null.</exception>
        public static Func<CardRecap, CardSummary> For(RunFightReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            return card =>
            {
                if (card.CombatantIndex == Game.Core.Combat.Fight.HeroIndex)
                {
                    if (card.Position < report.HeroLine.Count && report.HeroLine[card.Position].Definition.Id == card.CardId)
                    {
                        return CardSummary.From(report.HeroLine[card.Position].CurrentDefinition);
                    }

                    return null;
                }

                var enemyIndex = card.CombatantIndex - 1;
                if (enemyIndex < 0 || enemyIndex >= report.Encounter.Enemies.Count)
                {
                    return null;
                }

                var line = report.Encounter.Enemies[enemyIndex].SpellLine;
                return card.Position < line.Count && line[card.Position].Id == card.CardId ? CardSummary.From(line[card.Position]) : null;
            };
        }
    }
}
