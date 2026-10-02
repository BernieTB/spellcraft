using System;
using Game.Core.Cards;
using Game.Core.Effects;
using UnityEngine;

namespace Game.Unity.Cards
{
    /// <summary>
    /// Authoring data for one neighbour modifier of a card: which effect kind gets the bonus, which neighbour
    /// (next or previous card in the spell line) and how much. Converted to a Core <see cref="NeighbourModifier"/>
    /// by <see cref="ToModifier"/>; Core validates the values.
    /// </summary>
    /// <remarks>
    /// The enums are the Core ones, serialized as integers; their values are explicit and never renumbered.
    /// </remarks>
    [Serializable]
    public struct NeighbourModifierEntry
    {
        [SerializeField]
        [Tooltip("Effect kind of the neighbour that gets the bonus. It does nothing if the neighbour has no effect of that kind.")]
        private BonusKind _kind;

        [SerializeField]
        [Tooltip("Next: the card after this one (the first after the last). Previous: the card before it, boosted on the next loop.")]
        private NeighbourDirection _direction;

        [SerializeField]
        [Min(0)]
        [Tooltip("Amount added to one cast of the neighbour.")]
        private int _amount;

        /// <summary>
        /// Creates the Core modifier described by this entry.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Core rejects a value: unknown kind or direction, or negative amount.
        /// </exception>
        public NeighbourModifier ToModifier() => new NeighbourModifier(_kind, _direction, _amount);
    }
}
