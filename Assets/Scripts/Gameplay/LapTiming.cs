using System;

namespace VelocityRivals
{
    /// <summary>Uses match time so countdown and pause never inflate lap records.</summary>
    public sealed class LapTiming
    {
        public int CompletedLaps { get; private set; }
        public float LastLap { get; private set; }
        public float BestLap { get; private set; }
        private float lapStart;

        public float CurrentLap(float elapsed) => Math.Max(0, elapsed - lapStart);

        public bool CompleteLap(int completedLaps, float elapsed)
        {
            if (completedLaps != CompletedLaps + 1 || float.IsNaN(elapsed) || float.IsInfinity(elapsed) || elapsed <= lapStart)
                return false;
            LastLap = elapsed - lapStart;
            if (BestLap <= 0 || LastLap < BestLap) BestLap = LastLap;
            lapStart = elapsed;
            CompletedLaps = completedLaps;
            return true;
        }
    }
}
