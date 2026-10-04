using System;

namespace Game.Core.Runs
{
    /// <summary>
    /// A snapshot of one of the biome's secret rooms during a run, for a screen to show the objective, whether the
    /// room is open and whether its mini-boss was already beaten. Taken from <see cref="Run.SecretRooms"/>.
    /// </summary>
    public sealed class SecretRoomStatus
    {
        internal SecretRoomStatus(SecretRoomDefinition definition, int objectiveProgress, bool isUnlocked, bool isCleared)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            ObjectiveProgress = objectiveProgress;
            IsUnlocked = isUnlocked;
            IsCleared = isCleared;
        }

        /// <summary>The room's data.</summary>
        public SecretRoomDefinition Definition { get; }

        /// <summary>
        /// Enemies of the objective's kind defeated in regular fights so far, never above the objective's count. It
        /// stops once the room is unlocked.
        /// </summary>
        public int ObjectiveProgress { get; }

        /// <summary>True once the room can be entered (it is among <see cref="Run.AvailableSteps"/>).</summary>
        public bool IsUnlocked { get; }

        /// <summary>True once the mini-boss was beaten for the first time, so the room's rewards were given.</summary>
        public bool IsCleared { get; }
    }
}
