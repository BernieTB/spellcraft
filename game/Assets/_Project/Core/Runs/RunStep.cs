using System;

namespace Game.Core.Runs
{
    /// <summary>
    /// A step the player can choose between fights (<c>docs/adr/0009-vertical-slice-run-pacing.md</c>). Two steps
    /// are equal when they have the same kind and secret room id.
    /// </summary>
    public sealed class RunStep : IEquatable<RunStep>
    {
        private RunStep(RunStepKind kind, string secretRoomId)
        {
            Kind = kind;
            SecretRoomId = secretRoomId;
        }

        /// <summary>A regular fight, drawn from the biome's pool when it is played.</summary>
        public static RunStep RegularFight { get; } = new RunStep(RunStepKind.RegularFight, null);

        /// <summary>The professor's fight.</summary>
        public static RunStep Professor { get; } = new RunStep(RunStepKind.Professor, null);

        /// <summary>What kind of step this is.</summary>
        public RunStepKind Kind { get; }

        /// <summary>The secret room's id for <see cref="RunStepKind.SecretRoom"/>, otherwise null.</summary>
        public string SecretRoomId { get; }

        /// <summary>
        /// True for mini-boss and professor fights, which a preparation phase comes before and whose spell line is
        /// fixed once they start (<c>docs/adr/0012-linked-choices-and-spell-line-editing.md</c>).
        /// </summary>
        public bool RequiresPreparation => Kind == RunStepKind.SecretRoom || Kind == RunStepKind.Professor;

        /// <summary>The fight of an unlocked secret room.</summary>
        /// <exception cref="ArgumentException"><paramref name="roomId"/> is null, empty or whitespace.</exception>
        public static RunStep SecretRoom(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                throw new ArgumentException("Secret room id cannot be null, empty or whitespace.", nameof(roomId));
            }

            return new RunStep(RunStepKind.SecretRoom, roomId);
        }

        /// <inheritdoc />
        public bool Equals(RunStep other)
        {
            return other != null && Kind == other.Kind && string.Equals(SecretRoomId, other.SecretRoomId, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return Equals(obj as RunStep);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ (SecretRoomId == null ? 0 : StringComparer.Ordinal.GetHashCode(SecretRoomId));
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Kind == RunStepKind.SecretRoom ? $"{Kind}({SecretRoomId})" : Kind.ToString();
        }
    }
}
