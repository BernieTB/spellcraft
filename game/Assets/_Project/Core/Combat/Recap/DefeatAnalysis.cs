using System.Collections.Generic;
using Game.Core.Effects;

namespace Game.Core.Combat.Recap
{
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

        /// <summary>
        /// First tick of the analysed loop (the hero's casts that led to the turning point). When the hero cast
        /// nothing before dying, the loop is empty and this equals <see cref="TurningPointTick"/>.
        /// </summary>
        public int WindowStartTick { get; }

        /// <summary>Last tick of the analysed loop; <see cref="TurningPointTick"/> when the loop is empty.</summary>
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

        /// <summary>
        /// Tick on which the hero's shield was broken, between the start of the analysed loop and the turning
        /// point, or -1.
        /// </summary>
        public int ShieldBrokenTick { get; }

        /// <summary>Fight index of the enemy whose card broke the shield, or -1.</summary>
        public int ShieldBreakerIndex { get; }

        /// <summary>Position of that card in the enemy's line, or -1.</summary>
        public int ShieldBreakerPosition { get; }

        /// <summary>Id of that card, or null.</summary>
        public string ShieldBreakerCardId { get; }

        /// <summary>
        /// Position of the hero's card with the lowest output in the analysed loop (ties: lowest position, then card
        /// id in ordinal order). With an empty loop, the first card of the starting line, with zero output.
        /// </summary>
        public int WeakestCardPosition { get; }

        /// <summary>Id of that card.</summary>
        public string WeakestCardId { get; }

        /// <summary>Its output in the analysed loop: damage plus healing plus shield gained.</summary>
        public long WeakestCardOutput { get; }
    }
}
