# Validation and test instructions

## Executed in the cloud

- Fourteen C# console tests compiled and passed using .NET SDK 8.0.426. They use the actual `RaceProgress.cs`, `LanFrameBuffer.cs` and `CombatVitals.cs` source files: finish-line/skip prevention, reverse rejection, three-lap completion, duplicate/configuration guards, network fragmentation/coalescing/bounds, shield expiry, knockout damage, delayed protected respawn and overdrive expiry/reset.
- Eight Blender FBX exports imported successfully back into Blender 4.3.2. Assertions compared mesh counts, vertices and bounds. Measurements are in `Tools/Blender/Generated/validation.json`.
- Six generated car models were visually inspected in the offline Blender contact sheet. This is not a Unity screenshot.
- `Tools/validate_source.py` validated manifest/assembly JSON, unique Unity GUIDs, source scene/prefab/stat GUID references, six vehicle asset sets and C# syntax using tree-sitter. Syntax parsing is not Unity API compilation.

No Unity Editor was available in the cloud. Unity Test Runner suites, gameplay, controller hardware, LAN end-to-end sessions, Windows builds and performance profiling have **not** run.

## Repeat independent checks

```sh
dotnet run --project Tools/LogicTests/LogicTests.csproj --configuration Release
python -m pip install tree-sitter tree-sitter-c-sharp
python Tools/validate_source.py
```

The .NET console program exits nonzero on any failed assertion. The Python validator reports a skipped C# parser if its dependencies are absent. Blender regeneration commands and destructive scope of regeneration are documented in `ART.md`.

## Authored Unity tests (unrun)

Use Window → General → Test Runner, then run both EditMode and PlayMode suites.

EditMode covers ordered checkpoint progress, exact lap completion, invalid configuration, profile save/load and customization, record selection/XP, and recovery from a damaged primary save. Save tests use unique temporary directories and restore the original storage setting.

PlayMode covers vehicle spawning, pause/resume/menu state, restart cleanup, all 30 mode/environment combinations, AI movement after countdown, battle impact scoring/cooldown, and starting an event through actual menu buttons. These are smoke tests; they do not demonstrate AI race completion or game balance.

`Tools/Build-Windows.ps1 -RunTests` runs both suites, rejects failed/zero-test/missing reports and then builds. Reports must belong to the current run; the script removes only its previous report files before running. Read `Artifacts/EditMode.log`, `PlayMode.log`, `build.log` and XML results if anything fails.

## Manual acceptance checks (unrun)

1. **First boot:** no red Console errors; main menu renders, music/clicks are audible, keyboard/mouse/controller select controls.
2. **Driving:** Free Play, Cyber Dome. Accelerate/reverse/steer/brake/drift; drain and recharge boost; perform first and second jump, directional dodge, roll and upright landing; recover after flipping; confirm HUD and FOV.
3. **Racing:** complete every circuit in both directions. Wrong-way crossings, skipped gates and repeated finish crossings must not grant laps. Race each AI to completion; record stuck or off-track cases and tune speed/steering before claiming readiness.
4. **Championship:** finish three rounds, advance only from results, verify points and overall trophy outcome; restart mid-championship clears standings.
5. **Arena Clash:** score at each end; verify appropriate team gets points and players/core reset; exercise first-to-five, time expiry and draw.
6. **Boost Battle:** low-speed contact gives no points; fast contact increments the correct score once; confirm armor loss, shield protection, overdrive damage, pulse range, knockout scores, delayed respawn, pause-safe timers and first-to-fifteen/time result. Follow UPDATE-COMBAT.md.
7. **Stunts:** rotation followed by an upright landing scores; flipped landings do not grant the same reward; timer/results/restart work.
8. **Time Attack:** complete a full run; restart to see ghost; slower runs do not replace fastest ghost; records survive closing and reopening the game.
9. **Garage/save:** earn XP and unlock a second vehicle; change all cosmetic fields, quit normally, reopen and check selections and appearance. Test backup recovery using a copied test profile, not your only save.
10. **Menus:** pause local event, change volume/quality, resume, restart and return; verify gamepad focus throughout; test 1280×720, 1920×1080 and 2560×1440.
11. **LAN:** follow `MULTIPLAYER.md`; run two player instances, compare positions/timer/scores/results, disconnect each side, reconnect and test third-client rejection. Protocol unit tests alone do not pass this check.
12. **Windows:** run the built EXE on a clean Windows account, verify its entire data directory is present, controller reconnects, alt-tab works, saves persist, and exit is clean.
13. **Performance:** Unity Profiler plus a Development Build on target hardware. Measure CPU/GPU frame times over complete races and arenas, memory after 20 restarts, GC spikes, draw calls and physics cost. Target 16.67 ms total frame budget for 60 FPS; record hardware and quality settings. No target has yet been measured.
