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
            Console.WriteLine(passed + " tests passed. These cover shared C# logic, not Unity runtime behavior.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
