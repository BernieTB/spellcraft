using System;

namespace Game.Core.Runs
{
    /// <summary>
    /// What the player must do to unlock a secret room: defeat <see cref="Count"/> monsters of one kind
    /// (<c>docs/adr/0010-secret-rooms-and-mini-boss-rewards.md</c>). Only regular fights won count. Built from data.
    /// </summary>
    public sealed class ObjectiveDefinition
    {
        /// <param name="enemyId">Id of the enemy to defeat, as in <see cref="Enemies.EnemyDefinition.Id"/>.</param>
        /// <param name="count">How many of them to defeat. At least one.</param>
        /// <exception cref="ArgumentException"><paramref name="enemyId"/> is null, empty or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 1.</exception>
        public ObjectiveDefinition(string enemyId, int count)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                throw new ArgumentException("Objective enemy id cannot be null, empty or whitespace.", nameof(enemyId));
            }

            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "An objective needs at least one enemy to defeat.");
            }

            EnemyId = enemyId;
            Count = count;
        }

        /// <summary>Id of the enemy to defeat.</summary>
        public string EnemyId { get; }

        /// <summary>How many of that enemy to defeat. At least one.</summary>
        public int Count { get; }
    }
}
