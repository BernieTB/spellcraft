using System;

namespace Game.Core.Combat
{
    /// <summary>
    /// A change the player makes to the hero's spell line during a regular fight, stamped with the tick it applies
    /// to (<c>docs/adr/0012-linked-choices-and-spell-line-editing.md</c>). Immutable; create it with
    /// <see cref="Move"/> or <see cref="SwapWithReserve"/>.
    /// </summary>
    /// <remarks>
    /// A change stamped with tick <c>T</c> applies at the start of tick <c>T</c>, before any combatant acts on that
    /// tick, so it takes effect immediately. Tick 1 is the first tick of the fight. Positions are zero-based
    /// positions of the hero's spell line; reserve indexes are zero-based indexes of the hero's reserve, as given to
    /// the <see cref="Fight"/>.
    /// </remarks>
    public sealed class LineChange
    {
        /// <summary>Value of <see cref="ToPosition"/> or <see cref="ReserveIndex"/> when the kind does not use it.</summary>
        public const int Unused = -1;

        private LineChange(LineChangeKind kind, int tick, int position, int toPosition, int reserveIndex)
        {
            if (tick < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(tick), tick, "A line change applies to a tick from 1.");
            }

            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), position, "Position cannot be negative.");
            }

            Kind = kind;
            Tick = tick;
            Position = position;
            ToPosition = toPosition;
            ReserveIndex = reserveIndex;
        }

        /// <summary>What the change does.</summary>
        public LineChangeKind Kind { get; }

        /// <summary>The tick the change applies to: at its start, before any combatant acts. From 1.</summary>
        public int Tick { get; }

        /// <summary>Position of the line card that moves (<see cref="LineChangeKind.Move"/>) or leaves for the reserve
        /// (<see cref="LineChangeKind.SwapWithReserve"/>).</summary>
        public int Position { get; }

        /// <summary>Where the card goes, for <see cref="LineChangeKind.Move"/>; <see cref="Unused"/> otherwise.</summary>
        public int ToPosition { get; }

        /// <summary>Index of the reserve card that takes <see cref="Position"/>, for
        /// <see cref="LineChangeKind.SwapWithReserve"/>; <see cref="Unused"/> otherwise.</summary>
        public int ReserveIndex { get; }

        /// <summary>Moves the card at <paramref name="fromPosition"/> to <paramref name="toPosition"/> on
        /// <paramref name="tick"/> (<see cref="SpellLines.SpellLine{TCard}.Move"/>).</summary>
        /// <exception cref="ArgumentOutOfRangeException">The tick is less than 1 or a position is negative.</exception>
        public static LineChange Move(int tick, int fromPosition, int toPosition)
        {
            if (toPosition < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(toPosition), toPosition, "Position cannot be negative.");
            }

            return new LineChange(LineChangeKind.Move, tick, fromPosition, toPosition, Unused);
        }

        /// <summary>Exchanges the line card at <paramref name="linePosition"/> with the reserve card at
        /// <paramref name="reserveIndex"/> on <paramref name="tick"/>; the line card takes the reserve card's index.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The tick is less than 1 or an index is negative.</exception>
        public static LineChange SwapWithReserve(int tick, int linePosition, int reserveIndex)
        {
            if (reserveIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(reserveIndex), reserveIndex, "Reserve index cannot be negative.");
            }

            return new LineChange(LineChangeKind.SwapWithReserve, tick, linePosition, Unused, reserveIndex);
        }

        /// <summary>For logs and test messages: <c>tick 3 move 0->2</c> or <c>tick 3 swap 1&lt;->reserve 0</c>.</summary>
        public override string ToString() =>
            Kind == LineChangeKind.Move
                ? $"tick {Tick} move {Position}->{ToPosition}"
                : $"tick {Tick} swap {Position}<->reserve {ReserveIndex}";
    }
}
