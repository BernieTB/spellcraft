namespace Game.Core.Simulation
{
    /// <summary>
    /// What one card did for one side over a batch of fights, summed from the effect outcomes of its casts
    /// (<see cref="Combat.CastRecord.EffectOutcomes"/>, neighbour bonuses included).
    /// </summary>
    /// <remarks>
    /// Damage is the damage actually applied (<see cref="Combat.DamageResult.Total"/>): health lost plus damage
    /// absorbed by the target's shield. Overkill is not counted. Both parts are also given separately.
    /// </remarks>
    public sealed class CardStatistics
    {
        public CardStatistics(
            SimulationSide side,
            string cardId,
            int fightCount,
            long casts,
            long healthLost,
            long shieldAbsorbed,
            long healed,
            long shieldGained)
        {
            Side = side;
            CardId = cardId;
            FightCount = fightCount;
            Casts = casts;
            HealthLost = healthLost;
            ShieldAbsorbed = shieldAbsorbed;
            Healed = healed;
            ShieldGained = shieldGained;
        }

        /// <summary>The side that cast the card.</summary>
        public SimulationSide Side { get; }

        /// <summary>The card's id (<see cref="Cards.CardDefinition.Id"/>).</summary>
        public string CardId { get; }

        /// <summary>Number of fights in the batch, the divisor of the per-fight averages.</summary>
        public int FightCount { get; }

        /// <summary>Number of resolved casts of the card by this side.</summary>
        public long Casts { get; }

        /// <summary>Health removed from targets.</summary>
        public long HealthLost { get; }

        /// <summary>Damage absorbed by targets' shields.</summary>
        public long ShieldAbsorbed { get; }

        /// <summary>Total damage applied: <see cref="HealthLost"/> plus <see cref="ShieldAbsorbed"/>.</summary>
        public long Damage => HealthLost + ShieldAbsorbed;

        /// <summary>Health actually restored to the caster.</summary>
        public long Healed { get; }

        /// <summary>Shield actually added to the caster.</summary>
        public long ShieldGained { get; }

        /// <summary><see cref="Damage"/> divided by the number of fights in the batch (all fights, not only those
        /// where the card was cast).</summary>
        public double AverageDamagePerFight => (double)Damage / FightCount;
    }
}
