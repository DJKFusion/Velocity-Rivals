using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VelocityRivals;

static class Program
{
    static int passed;
    static void Check(bool value) { if (!value) throw new Exception("Assertion failed"); }
    static void Test(string name, Action test) { test(); passed++; Console.WriteLine("PASS " + name); }
    static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    static int Main()
    {
        try
        {
            Test("finish gate and skipped checkpoint cannot award lap", () => {
                var p = new RaceProgress(32, 3);
                Check(!p.Cross(0, true) && !p.Cross(4, true) && p.CompletedLaps == 0);
            });
            Test("reverse checkpoint rejected", () => {
                var p = new RaceProgress(4, 1); Check(!p.Cross(1, false) && p.NextCheckpoint == 1);
            });
            Test("exact three-lap finish and monotonic rank progress", () => {
                var p = new RaceProgress(32, 3); int previous = p.Progress;
                for (int lap = 0; lap < 3; lap++)
                    for (int gate = 1; gate <= 32; gate++) {
                        Check(p.Cross(gate % 32, true)); Check(p.Progress > previous); previous = p.Progress;
                        Check(p.Complete == (lap == 2 && gate == 32));
                    }
                Check(p.CompletedLaps == 3 && !p.Cross(1, true));
            });
            Test("duplicate checkpoint rejected", () => {
                var p = new RaceProgress(4, 1); Check(p.Cross(1, true)); Check(!p.Cross(1, true)); Check(p.NextCheckpoint == 2);
            });
            Test("invalid race configuration rejected", () => {
                Throws<ArgumentOutOfRangeException>(() => new RaceProgress(2, 1));
                Throws<ArgumentOutOfRangeException>(() => new RaceProgress(4, 0));
            });
            Test("network frame survives one-byte fragmentation", () => {
                var payload = Encoding.UTF8.GetBytes("{\"kind\":\"input\",\"throttle\":1}");
                var frame = LanFrameBuffer.Encode(payload); var reader = new LanFrameBuffer();
                for (int i = 0; i < frame.Length - 1; i++) { reader.Feed(new[] { frame[i] }, 1); Check(!reader.TryRead(out _)); }
                reader.Feed(new[] { frame[frame.Length - 1] }, 1); Check(reader.TryRead(out var received));
                Check(payload.SequenceEqual(received)); Check(!reader.TryRead(out _));
            });
            Test("network reader separates coalesced frames", () => {
                var a = LanFrameBuffer.Encode(new byte[] { 1, 2, 3 }); var b = LanFrameBuffer.Encode(new byte[] { 4, 5 });
                var reader = new LanFrameBuffer(); var stream = a.Concat(b).ToArray(); reader.Feed(stream, stream.Length);
                Check(reader.TryRead(out var first) && first.SequenceEqual(new byte[] { 1, 2, 3 }));
                Check(reader.TryRead(out var second) && second.SequenceEqual(new byte[] { 4, 5 })); Check(!reader.TryRead(out _));
            });
            Test("invalid and oversized network frames rejected", () => {
                foreach (var header in new[] { new byte[] { 0, 0, 0, 0 }, new byte[] { 0, 0, 16, 1 }, new byte[] { 255, 255, 255, 255 } }) {
                    var reader = new LanFrameBuffer(); reader.Feed(header, 4); Throws<InvalidOperationException>(() => reader.TryRead(out _));
                }
                Throws<ArgumentOutOfRangeException>(() => LanFrameBuffer.Encode(new byte[4097]));
            });
            Test("network receive buffer bounded", () => {
                var reader = new LanFrameBuffer(); Throws<InvalidOperationException>(() => reader.Feed(new byte[65537], 65537));
            });
            Test("shield blocks damage and expires on match time", () => {
                var v = new CombatVitals(); v.Shield(10);
                Check(v.Damage(50, 15) == 0 && v.Health == 100);
                Check(v.Damage(50, 16) == 50 && v.Health == 50);
            });
            Test("knockout clamps damage and cannot be scored twice", () => {
                var v = new CombatVitals(); Check(v.Damage(150, 4) == 100);
                Check(v.KnockedOut && v.Damage(10, 5) == 0 && v.RespawnAt == 7);
            });
            Test("respawn waits and protects against spawn camping", () => {
                var v = new CombatVitals(); v.Damage(100, 4);
                Check(!v.TryRespawn(6.99f)); Check(v.TryRespawn(7));
                Check(v.Health == 100 && !v.KnockedOut && v.Damage(100, 8.9f) == 0);
                Check(v.Damage(10, 9) == 10 && !v.TryRespawn(10));
            });
            Test("overdrive expires and clears on knockout", () => {
                var v = new CombatVitals(); v.Overdrive(5);
                Check(v.DamageMultiplier(12.9f) == 2 && v.DamageMultiplier(13) == 1);
                v.Overdrive(20); v.Damage(100, 21); Check(v.DamageMultiplier(22) == 1);
            });
            Test("knocked out cars cannot collect buffs; negative damage cannot heal", () => {
                var v = new CombatVitals(); Check(v.Damage(-10, 0) == 0 && v.Health == 100);
                v.Damage(100, 1); v.Shield(2); v.Overdrive(2);
                Check(!v.Shielded(2) && v.DamageMultiplier(2) == 1);
            });
            Console.WriteLine(passed + " tests passed. These cover shared C# logic, not Unity runtime behavior.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
