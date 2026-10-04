using System;
using System.Collections.Generic;

namespace Game.Core.Combat
{
    /// <summary>
    /// How a fight ended: the winner, how long it took, every cast that resolved, every change the player made
    /// to the hero's line and every evolution of the hero's cards.
    /// </summary>
    public sealed class FightResult
    {
        /// <summary>Creates the result of a fight without line changes.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="casts"/> is null.</exception>
        public FightResult(FightWinner winner, int ticks, IReadOnlyList<CastRecord> casts)
            : this(winner, ticks, casts, Array.Empty<LineChangeRecord>())
        {
        }

        /// <exception cref="ArgumentNullException">
        /// <paramref name="casts"/> or <paramref name="lineChanges"/> is null.
        /// </exception>
        public FightResult(
            FightWinner winner,
            int ticks,
            IReadOnlyList<CastRecord> casts,
            IReadOnlyList<LineChangeRecord> lineChanges)
            : this(winner, ticks, casts, lineChanges, Array.Empty<EvolutionRecord>(), Array.Empty<int>())
        {
        }

        /// <param name="evolutions">Every evolution of the hero's cards, in the order they happened.</param>
        /// <param name="heroCardCasts">
        /// The final casts of each card copy of the hero (see <see cref="HeroCardCasts"/>). Empty when the fight did
        /// not count the casts of the hero's cards.
        /// </param>
        /// <exception cref="ArgumentNullException">An argument is null.</exception>
        public FightResult(
            FightWinner winner,
            int ticks,
            IReadOnlyList<CastRecord> casts,
            IReadOnlyList<LineChangeRecord> lineChanges,
            IReadOnlyList<EvolutionRecord> evolutions,
            IReadOnlyList<int> heroCardCasts)
        {
            Winner = winner;
            Ticks = ticks;
            Casts = casts ?? throw new ArgumentNullException(nameof(casts));
            LineChanges = lineChanges ?? throw new ArgumentNullException(nameof(lineChanges));
            Evolutions = evolutions ?? throw new ArgumentNullException(nameof(evolutions));
            HeroCardCasts = heroCardCasts ?? throw new ArgumentNullException(nameof(heroCardCasts));
        }

        /// <summary>The winning side, or <see cref="FightWinner.None"/> on timeout.</summary>
        public FightWinner Winner { get; }

        /// <summary>True when the fight hit its maximum number of ticks before a side won.</summary>
        public bool TimedOut => Winner == FightWinner.None;

        /// <summary>Number of ticks simulated: the tick on which the fight ended, or the maximum on timeout.</summary>
        public int Ticks { get; }

        /// <summary>Every resolved cast, in resolution order.</summary>
        public IReadOnlyList<CastRecord> Casts { get; }

        /// <summary>
        /// Every change applied to the hero's line, in the order applied (each at the start of its tick, before the
        /// casts of that tick). Empty when the line never changed.
        /// </summary>
        public IReadOnlyList<LineChangeRecord> LineChanges { get; }

        /// <summary>
        /// Every time a card of the hero's reached an evolution stage (<c>docs/adr/0013-card-evolution.md</c>), in
        /// order. Empty when no card evolved, or when the fight did not count the hero's casts.
        /// </summary>
        public IReadOnlyList<EvolutionRecord> Evolutions { get; }

        /// <summary>
        /// The total casts of every card copy the hero had at the start, after the fight: first the copies of the
        /// hero's line in its starting order, then the copies of the reserve in its starting order. A copy keeps
        /// its place in this list wherever it was moved during the fight. Empty when the fight did not count the
        /// hero's casts (it was not given the casts the copies already had).
        /// </summary>
        public IReadOnlyList<int> HeroCardCasts { get; }
    }
}
