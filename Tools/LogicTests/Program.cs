using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using VelocityRivals;
using VelocityRivals.Editor;

static class Program
{
    static int passed;
    static void Check(bool value) { if (!value) throw new Exception("Assertion failed"); }
    static void Test(string name, Action test) { test(); passed++; Console.WriteLine("PASS " + name); }
    static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    static int Main(string[] args)
    {
        try
        {
            if (args.Length == 2 && args[0] == "--relay") { RelayIntegration.Run(args[1]).GetAwaiter().GetResult(); return 0; }
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
            Test("playlist handles no tracks, one track and invalid counts", () => {
                Check(new ShufflePlaylist(0, 1).Next() == -1);
                var single = new ShufflePlaylist(1, 1);
                for (int i = 0; i < 10; i++) Check(single.Next() == 0);
                Throws<ArgumentOutOfRangeException>(() => new ShufflePlaylist(-1, 1));
            });
            Test("playlist visits every track exactly once without boundary repeats", () => {
                for (int count = 2; count <= 12; count++) {
                    var playlist = new ShufflePlaylist(count, count); int previous = -1;
                    for (int cycle = 0; cycle < 100; cycle++) {
                        var seen = new HashSet<int>();
                        for (int i = 0; i < count; i++) {
                            int next = playlist.Next();
                            Check(next >= 0 && next < count && next != previous && seen.Add(next)); previous = next;
                        }
                        Check(seen.Count == count);
                    }
                }
            });
            Test("lap timing records splits and fastest completed lap", () => {
                var laps = new LapTiming();
                Check(laps.CurrentLap(10) == 10 && laps.CompleteLap(1, 30));
                Check(laps.LastLap == 30 && laps.BestLap == 30 && laps.CurrentLap(35) == 5);
                Check(laps.CompleteLap(2, 58) && laps.LastLap == 28 && laps.BestLap == 28);
                Check(laps.CompleteLap(3, 91) && laps.LastLap == 33 && laps.BestLap == 28);
            });
            Test("lap timing rejects duplicate skipped backwards and invalid samples", () => {
                var laps = new LapTiming(); Check(!laps.CompleteLap(2, 30));
                Check(!laps.CompleteLap(1, float.NaN) && !laps.CompleteLap(1, float.PositiveInfinity));
                Check(laps.CompleteLap(1, 30) && !laps.CompleteLap(1, 31));
                Check(!laps.CompleteLap(2, 29) && !laps.CompleteLap(2, 30));
                Check(laps.CompletedLaps == 1 && laps.LastLap == 30);
            });
            Test("lap timing stays still when match time is paused", () => {
                var laps = new LapTiming(); laps.CompleteLap(1, 30);
                for (int i = 0; i < 100; i++) Check(laps.CurrentLap(35) == 5);
                Check(laps.CompleteLap(2, 60) && laps.LastLap == 30);
            });
            Test("Windows archive preserves executable, nested data and player dependencies", () => {
                WithBuildFixture((dir, exe, zip) => {
                    WindowsArchive.Create(exe, zip);
                    using var archive = ZipFile.OpenRead(zip);
                    Check(archive.GetEntry("VelocityRivals.exe") != null && archive.GetEntry("UnityPlayer.dll") != null);
                    using var reader = new StreamReader(archive.GetEntry("VelocityRivals_Data/Managed/fixture.txt").Open());
                    Check(reader.ReadToEnd() == "test data, not a playable build");
                });
            });
            Test("Windows archive refuses incomplete builds", () => {
                WithBuildFixture((dir, exe, zip) => {
                    string player = Path.Combine(dir, "UnityPlayer.dll"); File.Delete(player);
                    Throws<FileNotFoundException>(() => WindowsArchive.Create(exe, zip)); Check(!File.Exists(zip));
                    File.WriteAllText(player, "fixture"); Directory.Delete(Path.Combine(dir, "VelocityRivals_Data"), true);
                    Throws<DirectoryNotFoundException>(() => WindowsArchive.Create(exe, zip)); Check(!File.Exists(zip));
                });
            });
            Test("Windows archive avoids self-inclusion and preserves existing packages", () => {
                WithBuildFixture((dir, exe, zip) => {
                    Throws<ArgumentException>(() => WindowsArchive.Create(exe, Path.Combine(dir, "self.zip")));
                    File.WriteAllText(zip, "previous package");
                    Throws<IOException>(() => WindowsArchive.Create(exe, zip));
                    Check(File.ReadAllText(zip) == "previous package");
                });
            });
            Test("network match waits for loaded peers and full countdown", () => {
                var match = new LanMatchRules(LanMode.CircuitDuel, 4);
                match.Tick(100); Check(match.Remaining == 600 && !match.CrossGate(0, 1, true));
                Check(match.Start() && !match.Start()); match.Tick(2);
                Check(match.Phase == LanPhase.Countdown && !match.CrossGate(0, 1, true));
                match.Tick(2); Check(match.Phase == LanPhase.Racing && match.Remaining == 599);
            });
            Test("network circuit requires three ordered laps and locks first winner", () => {
                var match = new LanMatchRules(LanMode.CircuitDuel, 4); match.Start(); match.Tick(3);
                Check(!match.CrossGate(1, 0, true) && !match.CrossGate(1, 2, true) && !match.CrossGate(1, 1, false));
                for (int lap = 0; lap < 3; lap++) foreach (int gate in new[] { 1, 2, 3, 0 }) Check(match.CrossGate(1, gate, true));
                Check(match.Phase == LanPhase.Complete && match.Winner == 1 && match.Score(1) == 12);
                Check(!match.CrossGate(0, 1, true) && !match.Goal(0)); match.Tick(999); Check(match.Winner == 1);
            });
            Test("network arena ends at five goals and ignores race gates", () => {
                var match = new LanMatchRules(LanMode.ArenaDuel, 4); Check(!match.Goal(0)); match.Start(); match.Tick(3);
                Check(!match.CrossGate(0, 0, true)); for (int i = 0; i < 5; i++) Check(match.Goal(0));
                Check(match.Winner == 0 && !match.Goal(1) && match.Score(0) == 5);
            });
            Test("network timeout resolves scores and draws, new rematch resets progress", () => {
                var match = new LanMatchRules(LanMode.GateSprint, 4); match.Start(); match.Tick(3); match.CrossGate(1, 0, true);
                Check(!match.CrossGate(1, 0, true)); match.Tick(120); Check(match.Winner == 1);
                var next = new LanMatchRules(LanMode.GateSprint, 4); Check(next.Score(1) == 0 && next.NextGate(1) == 0);
                next.Start(); next.Tick(123); Check(next.Phase == LanPhase.Complete && next.Winner == -1);
            });
            Test("network rules reject invalid configuration and clocks", () => {
                Throws<ArgumentOutOfRangeException>(() => new LanMatchRules((LanMode)9, 4));
                Throws<ArgumentOutOfRangeException>(() => new LanMatchRules(LanMode.CircuitDuel, 2));
                var match = new LanMatchRules(LanMode.ArenaDuel, 4);
                Throws<ArgumentOutOfRangeException>(() => match.Goal(2));
                Throws<ArgumentOutOfRangeException>(() => match.Tick(float.NaN));
                Throws<ArgumentOutOfRangeException>(() => match.Tick(-1));
            });
            Test("relay endpoints require TLS outside loopback", () => {
                Check(RelayTransport.ValidateEndpoint("wss://example.com/rooms").Scheme == "wss");
                Check(RelayTransport.ValidateEndpoint("ws://127.0.0.1:10000").IsLoopback);
                Throws<ArgumentException>(() => RelayTransport.ValidateEndpoint("ws://example.com"));
                Throws<ArgumentException>(() => RelayTransport.ValidateEndpoint("https://example.com"));
                Throws<ArgumentException>(() => RelayTransport.ValidateEndpoint("wss://user:secret@example.com"));
            });
            Test("career unlocks each event in order and reaches an explicit ending", () => {
                var career = new CareerProgress(); Check(career.NextEvent == 0 && !career.Complete);
                Check(!career.Record(1, 3) && !career.IsUnlocked(-1) && !career.IsUnlocked(CareerTour.Count));
                for (int i = 0; i < CareerTour.Count; i++) { Check(career.IsUnlocked(i)); Check(career.Record(i, 1)); }
                Check(career.Complete && career.TotalMedals == CareerTour.Count && career.NextEvent == CareerTour.Count - 1);
            });
            Test("career replays improve medals without reducing progress", () => {
                var career = new CareerProgress(); career.Record(0, 3); career.Record(1, 2);
                Check(career.Record(0, 1) && career.medals[0] == 3 && career.NextEvent == 2);
                Check(!career.Record(2, 0) && !career.Record(2, 4) && !career.Complete);
            });
            Test("career normalization repairs missing and invalid save arrays", () => {
                var career = new CareerProgress { medals = null }; career.Normalize(); Check(career.medals.Length == CareerTour.Count);
                career.medals = new[] { 99, -1, 2 }; career.Normalize();
                Check(career.medals[0] == 3 && career.medals[1] == 0 && career.medals[2] == 2 && career.NextEvent == 1);
            });
            Test("career medals require completion and finite times", () => {
                var entry = CareerTour.Get(0);
                Check(entry.Medal(false, true, 50, 5000) == 0 && entry.Medal(true, true, float.NaN, 5000) == 0);
                Check(entry.Medal(true, true, entry.GoldSeconds, 1) == 3);
                Check(entry.Medal(true, true, entry.GoldSeconds * 1.2f, 1) == 2);
                Check(entry.Medal(true, true, entry.GoldSeconds * 2, 1) == 1);
            });
            Test("career championship and stunt medals enforce their distinct goals", () => {
                Check(CareerTour.Get(2).Medal(true, true, 400, 26) == 3);
                Check(CareerTour.Get(2).Medal(true, true, 400, 24) == 2);
                Check(CareerTour.Get(2).Medal(true, false, 400, 20) == 1);
                Check(CareerTour.Get(4).Medal(true, true, 120, 2000) == 3);
                Check(CareerTour.Get(4).Medal(true, true, 120, 1000) == 2);
            });
            Test("career catalog covers five themes with valid modes and a grand final", () => {
                var themes = new HashSet<int>();
                for (int i = 0; i < CareerTour.Count; i++) {
                    var entry = CareerTour.Get(i); themes.Add(entry.Environment);
                    Check(entry.Environment >= 0 && entry.Environment < 5 && entry.Difficulty >= 0 && entry.Difficulty <= 2);
                    Check(entry.Mode != GameMode.FreePlay && !string.IsNullOrEmpty(entry.Goal));
                }
                Check(themes.Count == 5 && CareerTour.Get(CareerTour.Count - 1).Mode == GameMode.Championship);
                Throws<ArgumentOutOfRangeException>(() => CareerTour.Get(CareerTour.Count));
            });
            Console.WriteLine(passed + " tests passed. These cover shared C# logic and archive fixtures, not Unity runtime behavior or a real Windows build. Networking regression sources are tested separately from their excluded player assembly.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }

    static void WithBuildFixture(Action<string, string, string> test)
    {
        string root = Path.Combine(Path.GetTempPath(), "velocity-package-test-" + Guid.NewGuid().ToString("N"));
        string player = Path.Combine(root, "Player");
        Directory.CreateDirectory(Path.Combine(player, "VelocityRivals_Data", "Managed"));
        string executable = Path.Combine(player, "VelocityRivals.exe");
        File.WriteAllText(executable, "test fixture, not an executable");
        File.WriteAllText(Path.Combine(player, "UnityPlayer.dll"), "test fixture");
        File.WriteAllText(Path.Combine(player, "VelocityRivals_Data", "Managed", "fixture.txt"), "test data, not a playable build");
        try { test(player, executable, Path.Combine(root, "game.zip")); }
        finally { Directory.Delete(root, true); }
    }
}
