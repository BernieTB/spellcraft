using System.Collections.Generic;
using Game.Core.Effects;

namespace Game.Core.Combat.Recap
{
    /// <summary>A possible cause of a defeat. Values are explicit and must not be renumbered.</summary>
    public enum DefeatCause
    {
        /// <summary>The hero's cards wasted neighbour bonuses in the analysed loop.</summary>
        WastedBonuses = 0,

        /// <summary>An enemy broke the hero's shield (brought it to zero) in the analysed loop.</summary>
        ShieldBroken = 1,

        /// <summary>A card of the hero produced less than the others in the analysed loop.</summary>
        WeakestCard = 2,
    }

    /// <summary>
    /// "Where the chain broke" (ADR 0014): the turning point of a lost fight and the causes found around it.
    /// Immutable. Built by <see cref="FightRecapBuilder"/>; see there for the exact rules.
    /// </summary>
    public sealed class DefeatAnalysis
    {
        public DefeatAnalysis(
            int turningPointTick,
            int windowStartTick,
            int windowEndTick,
            DefeatCause mainCause,
            IReadOnlyList<DefeatCause> causes,
            EffectBonus wastedBonus,
            int shieldBrokenTick,
            int shieldBreakerIndex,
            int shieldBreakerPosition,
            string shieldBreakerCardId,
            int weakestCardPosition,
            string weakestCardId,
            long weakestCardOutput)
        {
            TurningPointTick = turningPointTick;
            WindowStartTick = windowStartTick;
            WindowEndTick = windowEndTick;
            MainCause = mainCause;
            Causes = causes;
            WastedBonus = wastedBonus;
            ShieldBrokenTick = shieldBrokenTick;
            ShieldBreakerIndex = shieldBreakerIndex;
            ShieldBreakerPosition = shieldBreakerPosition;
            ShieldBreakerCardId = shieldBreakerCardId;
            WeakestCardPosition = weakestCardPosition;
            WeakestCardId = weakestCardId;
            WeakestCardOutput = weakestCardOutput;
        }

        /// <summary>
        /// The first tick from which the hero stayed behind until the end. Zero when the hero was behind from the start.
        /// </summary>
        public int TurningPointTick { get; }

        /// <summary>First tick of the analysed loop (the hero's casts that led to the turning point).</summary>
        public int WindowStartTick { get; }

        /// <summary>Last tick of the analysed loop.</summary>
        public int WindowEndTick { get; }

        /// <summary>The cause to show first: the first of <see cref="Causes"/>.</summary>
        public DefeatCause MainCause { get; }

        /// <summary>
        /// Every cause found, by priority: <see cref="DefeatCause.WastedBonuses"/>,
        /// <see cref="DefeatCause.ShieldBroken"/>, then <see cref="DefeatCause.WeakestCard"/>, which is always
        /// present.
        /// </summary>
        public IReadOnlyList<DefeatCause> Causes { get; }

        /// <summary>Neighbour bonus the hero's casts wasted in the analysed loop.</summary>
        public EffectBonus WastedBonus { get; }

        /// <summary>Tick on which the hero's shield was broken in the analysed loop, or -1.</summary>
        public int ShieldBrokenTick { get; }

        /// <summary>Fight index of the enemy whose card broke the shield, or -1.</summary>
        public int ShieldBreakerIndex { get; }

        /// <summary>Position of that card in the enemy's line, or -1.</summary>
        public int ShieldBreakerPosition { get; }

        /// <summary>Id of that card, or null.</summary>
        public string ShieldBreakerCardId { get; }

        /// <summary>Position of the hero's card with the lowest output in the analysed loop.</summary>
        public int WeakestCardPosition { get; }

        /// <summary>Id of that card.</summary>
        public string WeakestCardId { get; }

        /// <summary>Its output in the analysed loop: damage plus healing plus shield gained.</summary>
        public long WeakestCardOutput { get; }
    }
}
