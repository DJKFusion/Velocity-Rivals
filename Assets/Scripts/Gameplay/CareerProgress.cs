using System;

namespace VelocityRivals
{
    public sealed class CareerEvent
    {
        public readonly string Name, Cup;
        public readonly GameMode Mode;
        public readonly int Environment, Difficulty;
        public readonly float GoldSeconds;
        public CareerEvent(string name, string cup, GameMode mode, int environment, int difficulty, float goldSeconds = 150)
        { Name = name; Cup = cup; Mode = mode; Environment = environment; Difficulty = difficulty; GoldSeconds = goldSeconds; }
        public string Goal => Mode == GameMode.TimeAttack ? "Finish three laps. Gold: " + GoldSeconds + "s or faster." :
            Mode == GameMode.Championship ? "Complete all three rounds. Gold: win the cup with at least 26 points." :
            Mode == GameMode.StuntRush ? "Land stunts before time runs out. Silver: 1,000 points. Gold: 2,000 points." :
            "Complete the event. Silver: win. Gold: win within " + GoldSeconds + "s.";
        public int Medal(bool completed, bool won, float seconds, int score)
        {
            if (!completed || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return 0;
            if (Mode == GameMode.TimeAttack) return seconds <= GoldSeconds ? 3 : seconds <= GoldSeconds * 1.3f ? 2 : 1;
            if (Mode == GameMode.StuntRush) return score >= 2000 ? 3 : score >= 1000 ? 2 : 1;
            if (Mode == GameMode.Championship) return won && score >= 26 ? 3 : won ? 2 : 1;
            return won ? seconds <= GoldSeconds ? 3 : 2 : 1;
        }
    }
    public static class CareerTour
    {
        private static readonly CareerEvent[] events = {
            new CareerEvent("Neon Debut", "Rookie Cup", GameMode.TimeAttack, 0, 0, 270),
            new CareerEvent("Desert Contact", "Rookie Cup", GameMode.BoostBattle, 2, 0, 130),
            new CareerEvent("City Championship", "Rookie Cup", GameMode.Championship, 0, 0),
            new CareerEvent("Above the Clouds", "Challenger Cup", GameMode.TimeAttack, 1, 1, 297),
            new CareerEvent("Glacier Acrobat", "Challenger Cup", GameMode.StuntRush, 4, 1),
            new CareerEvent("Northern Championship", "Challenger Cup", GameMode.Championship, 4, 1),
            new CareerEvent("Dome Showdown", "Rival Cup", GameMode.ArenaClash, 3, 1, 150),
            new CareerEvent("Desert Record", "Rival Cup", GameMode.TimeAttack, 2, 1, 315),
            new CareerEvent("Sky Championship", "Rival Cup", GameMode.Championship, 1, 1),
            new CareerEvent("Icebreaker", "Velocity Finals", GameMode.TimeAttack, 4, 2, 342),
            new CareerEvent("Neon Knockout", "Velocity Finals", GameMode.BoostBattle, 0, 2, 130),
            new CareerEvent("Velocity Grand Final", "Velocity Finals", GameMode.Championship, 3, 2)
        };
        public static int Count => events.Length;
        public static CareerEvent Get(int index)
        { if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index)); return events[index]; }
        public static string MedalName(int value) => value == 3 ? "GOLD" : value == 2 ? "SILVER" : value == 1 ? "BRONZE" : "NO MEDAL";
    }
    [Serializable] public sealed class CareerProgress
    {
        public int[] medals = new int[CareerTour.Count];
        public void Normalize()
        {
            var corrected = new int[CareerTour.Count];
            if (medals != null) for (int i = 0; i < Math.Min(medals.Length, corrected.Length); i++) corrected[i] = Math.Max(0, Math.Min(3, medals[i]));
            medals = corrected;
        }
        public int NextEvent
        { get { for (int i = 0; i < medals.Length; i++) if (medals[i] == 0) return i; return CareerTour.Count - 1; } }
        public bool Complete
        { get { for (int i = 0; i < medals.Length; i++) if (medals[i] == 0) return false; return medals.Length == CareerTour.Count; } }
        public int TotalMedals
        { get { int sum = 0; foreach (int value in medals) sum += value; return sum; } }
        public bool IsUnlocked(int index) => index >= 0 && index < CareerTour.Count && index <= NextEvent;
        public bool Record(int index, int medal)
        {
            if (!IsUnlocked(index) || medal < 1 || medal > 3) return false;
            medals[index] = Math.Max(medals[index], medal); return true;
        }
    }
}
