using System;
using System.Collections.Generic;
using Game.Core.Combat;

namespace Game.Core.Simulation
{
    /// <summary>
    /// Accumulates fight results one at a time into a <see cref="SimulationSummary"/>, without keeping the results,
    /// so large batches use constant memory (plus one entry per side and card).
    /// </summary>
    public sealed class SimulationSummaryBuilder
    {
        private readonly Dictionary<(SimulationSide Side, string CardId), CardTotals> _cards =
            new Dictionary<(SimulationSide, string), CardTotals>();

        private int _fightCount;
        private int _heroWins;
        private int _enemyWins;
        private int _timeouts;
        private long _totalTicks;
        private int _minTicks = int.MaxValue;
        private int _maxTicks = int.MinValue;

        /// <summary>Number of fights added so far.</summary>
        public int FightCount => _fightCount;

        /// <summary>Adds the result of one fight.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="result"/> is null.</exception>
        public void Add(FightResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            _fightCount++;
            switch (result.Winner)
            {
                case FightWinner.Hero:
                    _heroWins++;
                    break;
                case FightWinner.Enemies:
                    _enemyWins++;
                    break;
                default:
                    _timeouts++;
                    break;
            }

            _totalTicks += result.Ticks;
            _minTicks = Math.Min(_minTicks, result.Ticks);
            _maxTicks = Math.Max(_maxTicks, result.Ticks);

            foreach (var cast in result.Casts)
            {
                var side = cast.CasterIndex == Fight.HeroIndex ? SimulationSide.Hero : SimulationSide.Enemies;
                var key = (side, cast.Card.Id);
                if (!_cards.TryGetValue(key, out var totals))
                {
                    totals = new CardTotals();
                    _cards.Add(key, totals);
                }

                totals.Casts++;
                foreach (var outcome in cast.EffectOutcomes)
                {
                    totals.HealthLost += outcome.Damage.HealthLost;
                    totals.ShieldAbsorbed += outcome.Damage.AbsorbedByShield;
                    totals.Healed += outcome.Healed;
                    totals.ShieldGained += outcome.ShieldGained;
                }
            }
        }

        /// <summary>
        /// Builds the summary of every fight added so far.
        /// </summary>
        /// <param name="firstSeed">Seed of the first fight added; the others are assumed to follow, one apart.</param>
        /// <exception cref="InvalidOperationException">No fight was added.</exception>
        public SimulationSummary Build(long firstSeed)
        {
            if (_fightCount == 0)
            {
                throw new InvalidOperationException("A simulation summary needs at least one fight.");
            }

            // Sorted explicitly: dictionary order is unspecified, and the JSON must be identical run after run.
            var keys = new List<(SimulationSide Side, string CardId)>(_cards.Keys);
            keys.Sort((a, b) => a.Side != b.Side ? a.Side.CompareTo(b.Side) : string.CompareOrdinal(a.CardId, b.CardId));

            var cards = new List<CardStatistics>(keys.Count);
            foreach (var key in keys)
            {
                var totals = _cards[key];
                cards.Add(new CardStatistics(
                    key.Side,
                    key.CardId,
                    _fightCount,
                    totals.Casts,
                    totals.HealthLost,
                    totals.ShieldAbsorbed,
                    totals.Healed,
                    totals.ShieldGained));
            }

            return new SimulationSummary(
                firstSeed,
                _fightCount,
                _heroWins,
                _enemyWins,
                _timeouts,
                _totalTicks,
                _minTicks,
                _maxTicks,
                cards.AsReadOnly());
        }

        private sealed class CardTotals
        {
            public long Casts;
            public long HealthLost;
            public long ShieldAbsorbed;
            public long Healed;
            public long ShieldGained;
        }
    }
}
