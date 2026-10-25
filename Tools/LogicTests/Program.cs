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
            Test("elimination cuts last place at exact intervals and declares one survivor", () => {
                var race=new EliminationRace();double[] progress={10.5,9.9,8.3,7.7};
                Check(race.EliminateDue(44.99f,progress)==-1 && race.ActiveCount==4);
                Check(race.EliminateDue(45,progress)==3 && race.ActiveCount==3 && !race.IsActive(3));
                Check(race.EliminateDue(45,progress)==-1);
                Check(race.EliminateDue(90,progress)==2 && race.ActiveCount==2);
                Check(race.EliminateDue(135,progress)==1 && race.Winner==0 && race.ActiveCount==1);
                Check(race.EliminateDue(1000,progress)==-1);
            });
            Test("elimination handles player losses, checkpoint ranking, ties and delayed frames", () => {
                var race=new EliminationRace();
                Check(race.EliminateDue(45,new double[]{1.99,2,3,4})==0);
                var tied=new EliminationRace();double[] progress={0,0,0,0};
                Check(tied.EliminateDue(135,progress)==3);
                Check(tied.EliminateDue(135,progress)==2);
                Check(tied.EliminateDue(135,progress)==1 && tied.Winner==0);
                Check(tied.EliminateDue(135,progress)==-1);
            });
            Test("elimination rejects invalid inputs without losing a driver", () => {
                Throws<ArgumentOutOfRangeException>(()=>new EliminationRace(1));
                Throws<ArgumentOutOfRangeException>(()=>new EliminationRace(4,float.NaN));
                var race=new EliminationRace();
                Throws<ArgumentException>(()=>race.EliminateDue(45,new double[]{1,2}));
                Throws<ArgumentException>(()=>race.EliminateDue(45,new double[]{1,double.NaN,2,3}));
                Throws<ArgumentOutOfRangeException>(()=>race.EliminateDue(-1,new double[4]));
                Check(race.ActiveCount==4 && race.NextCutoff==45);
            });
            Test("drafting requires a moving aligned lead car inside the slipstream corridor", () => {
                Check(RaceCraft.DraftStrength(12,0,0,1,100)> .99f);
                Check(RaceCraft.DraftStrength(12,2,0,1,100)<.3f);
                Check(RaceCraft.DraftStrength(-12,0,0,1,100)==0);
                Check(RaceCraft.DraftStrength(3,0,0,1,100)==0);
                Check(RaceCraft.DraftStrength(35,0,0,1,100)==0);
                Check(RaceCraft.DraftStrength(12,3,0,1,100)==0);
                Check(RaceCraft.DraftStrength(12,0,3,1,100)==0);
                Check(RaceCraft.DraftStrength(12,0,0,-1,100)==0);
                Check(RaceCraft.DraftStrength(12,0,0,1,30)==0);
                Check(RaceCraft.DraftStrength(float.NaN,0,0,1,100)==0);
            });
            Test("rivals choose open passing lanes and avoid blocked lane changes", () => {
                Check(RaceCraft.ChooseLane(0,30,4,30,-1)==-4);
                Check(RaceCraft.ChooseLane(0,30,4,30,1)==4);
                Check(RaceCraft.ChooseLane(0,5,4,6,1)==0);
                Check(RaceCraft.ChooseLane(-4,30,30,30,1)==-4);
                Check(RaceCraft.ChooseLane(4,30,25,2,-1)==0);
                Check(RaceCraft.ChooseLane(4,30,0,2,-1)==4);
                Check(RaceCraft.CornerSpeed(160,60,1)<RaceCraft.CornerSpeed(160,0,1));
                Check(RaceCraft.CornerSpeed(160,30,1)<RaceCraft.CornerSpeed(160,30,2));
            });
            Test("garage purchases spend once and stop at the last car", () => {
                var garage=new GarageProgress();Check(garage.BuyNextCar(1));Check(garage.credits==0 && garage.purchasedCars==2);
                Check(!garage.BuyNextCar(2) && garage.credits==0);
                garage.Credit(10000);Check(garage.BuyNextCar(5) && garage.purchasedCars==6);
                int balance=garage.credits;Check(!garage.BuyNextCar(6) && garage.credits==balance);
            });
            Test("upgrades enforce ownership, category, price and stage cap", () => {
                var garage=new GarageProgress();int original=garage.credits;
                Check(!garage.BuyUpgrade(1,0,1) && !garage.BuyUpgrade(0,3,1) && garage.credits==original);
                Check(garage.BuyUpgrade(0,0,1));Check(garage.credits==350 && garage.Level(0,0)==1);
                Check(garage.BuyUpgrade(0,0,1));Check(garage.credits==50 && garage.Level(0,0)==2);
                Check(!garage.BuyUpgrade(0,0,1));garage.Credit(400);Check(garage.BuyUpgrade(0,0,1));
                Check(garage.Level(0,0)==3 && !garage.BuyUpgrade(0,0,1));
                Check(garage.Signature(0)==3 && garage.Signature(1)==0);
            });
            Test("credits and medal bonuses reject repeats, failures and overflow", () => {
                Check(GarageProgress.EventReward(false,true,99999)==0);
                Check(GarageProgress.EventReward(true,true,99999)==400);
                Check(GarageProgress.EventReward(true,false,-500)==150);
                Check(GarageProgress.MedalReward(0,3)==300 && GarageProgress.MedalReward(3,3)==0 && GarageProgress.MedalReward(3,1)==0);
                var garage=new GarageProgress();garage.Credit(int.MaxValue);Check(garage.credits==GarageProgress.MaxCredits);
                Check(garage.Credit(1)==0 && garage.Credit(-100)==0);
            });
            Test("garage normalization and rollback copy retain valid progression", () => {
                var garage=new GarageProgress { credits=-20,purchasedCars=9,upgrades=new[]{-1,7,2} };garage.Normalize();
                Check(garage.credits==0 && garage.purchasedCars==6 && garage.upgrades.Length==18);
                Check(garage.Level(0,0)==0 && garage.Level(0,1)==3 && garage.Level(0,2)==2);
                var copy=garage.Copy();copy.upgrades[0]=3;Check(garage.upgrades[0]==0);
            });
            Test("driving school requires ordered actions and an upright landing", () => {
                var lesson=new DrivingLesson();
                for(int i=0;i<22;i++)lesson.Tick(.1f,30,false,true,true,false);
                Check(lesson.Step==1);
                while(lesson.Step==1)lesson.Tick(.1f,30,true,true,true,false);
                Check(lesson.Step==2);
                for(int i=0;i<5;i++)lesson.Tick(.1f,0,false,false,true,false);
                Check(lesson.Step==2); // Airborne without a prior grounded sample does not qualify.
                lesson.Tick(.1f,0,false,true,true,false);
                for(int i=0;i<4;i++)lesson.Tick(.1f,0,false,false,true,false);
                lesson.Tick(.1f,0,false,true,false,false);Check(lesson.Step==2);
                lesson.Tick(.1f,0,false,true,true,false);Check(lesson.Step==2);
                for(int i=0;i<4;i++)lesson.Tick(.1f,0,false,false,true,false);
                lesson.Tick(.1f,0,false,true,true,false);Check(lesson.Step==3);
                lesson.Tick(.1f,0,false,true,true,true);Check(lesson.Complete);
                lesson.Tick(.1f,100,true,true,true,true);Check(lesson.Step==4);
            });
            Test("driving school cannot complete by waiting or a frame time spike", () => {
                var lesson=new DrivingLesson();
                for(int i=0;i<100;i++)lesson.Tick(.1f,0,true,true,true,true);
                Check(lesson.Step==0);
                lesson.Tick(100,100,true,true,true,true);Check(lesson.Step==0);
                lesson.Tick(float.NaN,100,true,true,true,true);
                lesson.Tick(float.PositiveInfinity,100,true,true,true,true);
                lesson.Tick(-1,100,true,true,true,true);Check(lesson.Step==0);
            });
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
            Test("authored circuits close with evenly spaced finite gates inside recovery bounds", () => {
                for (int theme = 0; theme < 5; theme++) {
                    var route = CircuitLayout.Build(theme);
                    Check(route.Count > 40 && route.Count < 90);
                    for (int i = 0; i < route.Count; i++) {
                        var p = route[i]; float distance = CircuitLayout.Distance(p, route[(i + 1) % route.Count]);
                        Check(float.IsFinite(p.X) && float.IsFinite(p.Z) && Math.Abs(p.X) < 150 && Math.Abs(p.Z) < 150);
                        Check(distance > 8 && distance <= 10.1f);
                    }
                }
            });
            Test("circuits leave clearance between distant road sections", () => {
                for (int theme = 0; theme < 5; theme++) {
                    var route = CircuitLayout.Build(theme);
                    for (int i = 0; i < route.Count; i++) for (int j = i + 5; j < route.Count; j++) {
                        if (route.Count - (j - i) < 5) continue;
                        Check(CircuitLayout.Distance(route[i], route[j]) > 26);
                    }
                }
            });
            Test("circuits have smooth corners and individually authored layouts", () => {
                var signatures = new HashSet<string>();
                for (int theme = 0; theme < 5; theme++) {
                    var route = CircuitLayout.Build(theme); signatures.Add(string.Join(";", route.Select(p => p.X + "," + p.Z)));
                    for (int i = 0; i < route.Count; i++) {
                        var a = route[i]; var b = route[(i+1)%route.Count]; var c = route[(i+2)%route.Count];
                        float dot = ((b.X-a.X)*(c.X-b.X)+(b.Z-a.Z)*(c.Z-b.Z)) / (CircuitLayout.Distance(a,b)*CircuitLayout.Distance(b,c));
                        Check(dot > .65f);
                    }
                }
                Check(signatures.Count == 5);
            });
            Test("district labels cover all four sectors and invalid themes are rejected", () => {
                for (int theme = 0; theme < 5; theme++) {
                    var labels = new HashSet<string>();
                    for (int sector = 0; sector < 4; sector++) labels.Add(CircuitLayout.Section(theme, sector*20, 80));
                    Check(labels.Count == 4);
                }
                Throws<ArgumentOutOfRangeException>(() => CircuitLayout.Build(-1));
                Throws<ArgumentOutOfRangeException>(() => CircuitLayout.Build(5));
            });
            Test("Steam descriptors map the complete player without publishing a branch", () => WithBuildFixture((player,exe,zip) => {
                string folder=Path.Combine(Path.GetDirectoryName(zip), "Steam descriptors");
                string file=SteamDepotManifest.Write(exe,"12345","12346",folder,"0.6.0");
                string app=File.ReadAllText(file), depot=File.ReadAllText(Path.Combine(folder,"depot_build.vdf"));
                Check(app.Contains("12345") && app.Contains(player.Replace('\\','/')) && !app.Contains("SetLive"));
                Check(depot.Contains("12346") && depot.Contains("\"recursive\" \"1\""));
                Throws<IOException>(() => SteamDepotManifest.Write(exe,"12345","12346",folder,"0.6.0"));
            }));
            Test("Steam descriptors reject missing players and nonnumeric IDs", () => WithBuildFixture((player,exe,zip) => {
                string folder=Path.Combine(Path.GetDirectoryName(zip), "Steam");
                Throws<ArgumentException>(() => SteamDepotManifest.Write(exe,"YOUR_APP_ID","123",folder,"0.6.0"));
                Throws<ArgumentException>(() => SteamDepotManifest.Write(exe,"123","0",folder,"0.6.0"));
                File.Delete(exe);
                Throws<FileNotFoundException>(() => SteamDepotManifest.Write(exe,"123","456",folder,"0.6.0"));
                Check(!Directory.Exists(folder));
            }));
            Test("Steam configuration cannot be placed inside shipped content", () => WithBuildFixture((player,exe,zip) => {
                Throws<ArgumentException>(() => SteamDepotManifest.Write(exe,"123","456",Path.Combine(player,"Steam"),"0.6.0"));
                Check(!Directory.Exists(Path.Combine(player,"Steam")));
            }));
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
