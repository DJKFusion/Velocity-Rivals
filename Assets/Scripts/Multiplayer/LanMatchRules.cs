using System;

namespace VelocityRivals
{
    public enum LanMode { GateSprint, CircuitDuel, ArenaDuel }
    public enum LanPhase { Waiting, Countdown, Racing, Complete }

    /// <summary>Host-owned match rules, independent of rendering, sockets and Unity time.</summary>
    public sealed class LanMatchRules
    {
        public LanMode Mode { get; }
        public LanPhase Phase { get; private set; }
        public float Countdown { get; private set; } = 3;
        public float Remaining { get; private set; }
        public int Winner { get; private set; } = -1;
        public const int Laps = 3;
        public const int GoalTarget = 5;
        public int Checkpoints { get; }
        private readonly int[] scores = new int[2];
        private readonly int[] gates = new int[2];
        private readonly RaceProgress[] races = new RaceProgress[2];
        public int Score(int player) { CheckPlayer(player); return scores[player]; }
        public int NextGate(int player) { CheckPlayer(player); return Mode == LanMode.CircuitDuel ? races[player].NextCheckpoint : gates[player]; }
        public int Lap(int player) { CheckPlayer(player); return Mode == LanMode.CircuitDuel ? Math.Min(Laps, races[player].CompletedLaps + 1) : 0; }

        public LanMatchRules(LanMode mode, int checkpoints)
        {
            if (!Enum.IsDefined(typeof(LanMode), mode) || checkpoints < 3) throw new ArgumentOutOfRangeException();
            Mode = mode; Checkpoints = checkpoints;
            Remaining = mode == LanMode.CircuitDuel ? 600 : mode == LanMode.ArenaDuel ? 180 : 120;
            for (int i = 0; i < 2; i++) races[i] = new RaceProgress(checkpoints, Laps);
        }
        public bool Start()
        {
            if (Phase != LanPhase.Waiting) return false;
            Phase = LanPhase.Countdown; return true;
        }
        public void Tick(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (Phase == LanPhase.Countdown)
            {
                float used = Math.Min(Countdown, seconds);
                Countdown -= used; seconds -= used;
                if (Countdown > 0) return;
                Phase = LanPhase.Racing;
            }
            if (Phase != LanPhase.Racing) return;
            Remaining = Math.Max(0, Remaining - seconds);
            if (Remaining <= 0) Finish(scores[0] == scores[1] ? -1 : scores[0] > scores[1] ? 0 : 1);
        }
        public bool CrossGate(int player, int checkpoint, bool forward)
        {
            CheckPlayer(player);
            if (Phase != LanPhase.Racing || Mode == LanMode.ArenaDuel) return false;
            if (Mode == LanMode.CircuitDuel)
            {
                if (!races[player].Cross(checkpoint, forward)) return false;
                scores[player] = races[player].Progress;
                if (races[player].Complete) Finish(player);
            }
            else
            {
                if (!forward || checkpoint != gates[player]) return false;
                scores[player]++; gates[player] = (gates[player] + 1) % Checkpoints;
            }
            return true;
        }
        public bool Goal(int player)
        {
            CheckPlayer(player);
            if (Mode != LanMode.ArenaDuel || Phase != LanPhase.Racing) return false;
            scores[player]++;
            if (scores[player] >= GoalTarget) Finish(player);
            return true;
        }
        private void Finish(int winner) { Phase = LanPhase.Complete; Winner = winner; }
        private static void CheckPlayer(int player) { if (player < 0 || player > 1) throw new ArgumentOutOfRangeException(nameof(player)); }
    }
}
