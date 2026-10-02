using System;
using Game.Core.Effects;

namespace Game.Core.Cards
{
    /// <summary>
    /// A bonus a card gives to a neighbour in its own spell line each time it resolves, for example "the next card
    /// deals +X damage". Immutable data; the combat loop applies it (<see cref="Game.Core.Combat.Fight"/>).
    /// </summary>
    /// <remarks>
    /// Rules (<c>docs/adr/0002-first-pass-combat-rules.md</c>, confirmed in
    /// <c>docs/adr/0004-confirm-first-pass-combat-rules.md</c>; mechanism in
    /// <c>docs/adr/0005-neighbour-modifier-resolution.md</c>): the bonus waits on the neighbour's position and is
    /// used up by that position's next cast; bonuses waiting on the same cast add up; it only adds to effects of
    /// the matching kind, so a damage bonus on a card without a damage effect does nothing.
    /// </remarks>
    public sealed class NeighbourModifier
    {
        /// <param name="kind">Which effect kind of the neighbour gets the bonus.</param>
        /// <param name="direction">Which neighbour gets it: the next or the previous card.</param>
        /// <param name="amount">Bonus amount, from card data. Zero or more.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="kind"/> or <paramref name="direction"/> is not a defined value, or
        /// <paramref name="amount"/> is negative.
        /// </exception>
        public NeighbourModifier(BonusKind kind, NeighbourDirection direction, int amount)
        {
            if (!Enum.IsDefined(typeof(BonusKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown neighbour bonus kind.");
            }

            if (!Enum.IsDefined(typeof(NeighbourDirection), direction))
            {
                throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown neighbour direction.");
            }

            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Neighbour bonus amount cannot be negative.");
            }

            Kind = kind;
            Direction = direction;
            Amount = amount;
        }

        /// <summary>Which effect kind of the neighbour gets the bonus.</summary>
        public BonusKind Kind { get; }

        /// <summary>Which neighbour gets the bonus.</summary>
        public NeighbourDirection Direction { get; }

        /// <summary>Bonus added to one cast of the neighbour.</summary>
        public int Amount { get; }

        /// <summary>The bonus this modifier grants, as an <see cref="EffectBonus"/>.</summary>
        public EffectBonus ToBonus() => EffectBonus.Of(Kind, Amount);
    }
}
