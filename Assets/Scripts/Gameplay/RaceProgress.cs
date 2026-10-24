using System;

namespace VelocityRivals
{
    /// <summary>Ordered checkpoints: checkpoint zero is the finish line, never a free lap.</summary>
    [Serializable]
    public sealed class RaceProgress
    {
        public int CheckpointCount { get; private set; }
        public int NextCheckpoint { get; private set; } = 1;
        public int CompletedLaps { get; private set; }
        public int RequiredLaps { get; private set; }
        public bool Complete => CompletedLaps >= RequiredLaps;
        public int Progress => CompletedLaps * CheckpointCount + (NextCheckpoint == 0 ? CheckpointCount - 1 : NextCheckpoint - 1);

        public RaceProgress(int checkpoints, int laps)
        {
            if (checkpoints < 3 || laps < 1) throw new ArgumentOutOfRangeException();
            CheckpointCount = checkpoints;
            RequiredLaps = laps;
        }

        public bool Cross(int checkpoint, bool forward)
        {
            if (Complete || !forward || checkpoint != NextCheckpoint) return false;
            if (checkpoint == 0) CompletedLaps++;
            NextCheckpoint = (checkpoint + 1) % CheckpointCount;
            return true;
        }
    }
}
