using System;
using System.Collections.Generic;

namespace VelocityRivals
{
    /// <summary>Authored control points, rounded into evenly spaced closed circuits.</summary>
    public static class CircuitLayout
    {
        public const int Revision = 3;
        public readonly struct Point
        {
            public readonly float X, Z;
            public Point(float x, float z) { X = x; Z = z; }
        }
        static readonly float[][] controls = {
            new float[] { -55,70, 15,70, 78,48, 92,-12, 48,-65, -22,-72, -85,-30, -90,25 },
            new float[] { -65,64, 10,76, 85,52, 100,0, 65,-60, 10,-72, -30,-38, -90,-48, -105,10 },
            new float[] { -80,62, 0,76, 90,48, 114,-4, 75,-65, 15,-80, -25,-46, -94,-65, -115,0 },
            new float[] { -62,68, 15,68, 78,46, 92,-8, 55,-65, -20,-68, -82,-40, -92,15 },
            new float[] { -72,64, -10,82, 55,62, 98,20, 80,-40, 25,-78, -30,-55, -90,-68, -112,-10 }
        };
        static readonly string[][] districts = {
            new[] { "Central boulevard", "Neon financial district", "Dockside tunnel", "Festival straight" },
            new[] { "Cloud terminal", "Skybridge", "Observation district", "Launch promenade" },
            new[] { "Frontier paddock", "Canyon pass", "Solar refinery", "Mesa straight" },
            new[] { "Grandstand straight", "Champion bend", "Reactor tunnel", "Victory boulevard" },
            new[] { "Alpine village", "Ice gallery", "Research station", "Glacier pass" }
        };
        public static string Section(int theme, int checkpoint, int count)
        {
            Validate(theme);
            return districts[theme][Math.Min(3, Math.Max(0, checkpoint) * 4 / Math.Max(1, count))];
        }
        public static List<Point> Build(int theme)
        {
            Validate(theme);
            var source = controls[theme]; int count = source.Length / 2;
            Point At(int i) { i = (i + count) % count; return new Point(source[i * 2], source[i * 2 + 1]); }
            var dense = new List<Point>();
            for (int i = 0; i < count; i++)
                for (int j = 0; j < 16; j++)
                {
                    var a = At(i - 1); var b = At(i); var c = At(i + 1); var d = At(i + 2);
                    float t = j / 16f;
                    dense.Add(new Point(Curve(a.X,b.X,c.X,d.X,t), Curve(a.Z,b.Z,c.Z,d.Z,t)));
                }
            dense.Add(dense[0]);
            var distances = new float[dense.Count];
            for (int i = 1; i < dense.Count; i++) distances[i] = distances[i - 1] + Distance(dense[i - 1], dense[i]);
            int samples = (int)Math.Ceiling(distances[distances.Length - 1] / 10f);
            var result = new List<Point>(samples); int segment = 1;
            for (int i = 0; i < samples; i++)
            {
                float target = distances[distances.Length - 1] * i / samples;
                while (segment < distances.Length - 1 && distances[segment] < target) segment++;
                float t = (target - distances[segment - 1]) / (distances[segment] - distances[segment - 1]);
                var a = dense[segment - 1]; var b = dense[segment];
                result.Add(new Point(a.X + (b.X - a.X) * t, a.Z + (b.Z - a.Z) * t));
            }
            return result;
        }
        static float Curve(float a, float b, float c, float d, float t)
            => .5f * (2*b + (-a+c)*t + (2*a-5*b+4*c-d)*t*t + (-a+3*b-3*c+d)*t*t*t);
        public static float Distance(Point a, Point b) => (float)Math.Sqrt((a.X-b.X)*(a.X-b.X) + (a.Z-b.Z)*(a.Z-b.Z));
        static void Validate(int theme) { if (theme < 0 || theme >= controls.Length) throw new ArgumentOutOfRangeException(nameof(theme)); }
    }
}
