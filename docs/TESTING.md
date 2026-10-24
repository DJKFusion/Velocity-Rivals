# Validation and test instructions

## Executed in the cloud

- Twenty-eight C# console tests compiled and passed using .NET SDK 8.0.426. They use the actual checkpoint, network framing, combat, lap timing, shuffle playlist, duel rules, relay transport URL validation and Windows archive source files. New checks cover loaded-player countdown, three-lap first finish, arena goals, timeout/draw/reset, invalid clocks/configuration and TLS endpoint requirements. Archive tests use synthetic fixtures, not actual Unity binaries.
- Eight Python WebSocket server tests passed: create/join/forwarding, wrong protocol, missing room, third client, role restrictions, disconnect, payload bounds and capacity.
- The actual C# `RelayTransport` connected through the local Python server, created/joined a room, exchanged both directions and received a disconnect notice. This is not Unity gameplay or a public TLS deployment test.
- Nine original OGG music files were generated and decoded with FFmpeg; stereo duration, finite samples, signal level and non-clipping peaks passed. Details are in `Tools/Audio/validation.json`. Audible listening and Unity playback remain unverified.
- Three original road/normal/smoke PNGs decoded at their expected sizes. The shader code and final Unity appearance are unverified.
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

Relay tests:

```sh
python -m pip install -r Tools/Relay/requirements.txt
cd Tools/Relay
python -m unittest -v test_relay.py
```

To repeat the actual game transport integration, start `python Tools/Relay/server.py` from the repository root in one terminal, then in another run:

```sh
dotnet run --project Tools/LogicTests/LogicTests.csproj --configuration Release -- --relay ws://127.0.0.1:10000
```

## Authored Unity tests (unrun)

Use Window → General → Test Runner, then run both EditMode and PlayMode suites.

EditMode covers ordered checkpoint progress, exact lap completion, invalid configuration, profile save/load and customization, record selection/XP, and recovery from a damaged primary save. Save tests use unique temporary directories and restore the original storage setting.

PlayMode covers vehicle spawning, pause/resume/menu state, restart cleanup, all 30 mode/environment combinations, AI movement after countdown, battle impact scoring/cooldown, starting an event through actual menu buttons, music import/streaming/transitions/asset lifetime, isolated network-world setup without local AI/ghosts, and proxy speed/boost presentation without dynamic physics. These are smoke tests; they do not demonstrate AI race completion or game balance.

`Tools/Build-Windows.ps1 -RunTests -Package` runs both suites, rejects failed/zero-test/missing reports and then builds and packages the entire Windows directory. Reports must belong to the current run; the script removes only its previous report files before running. Read `Artifacts/EditMode.log`, `PlayMode.log`, `build.log` and XML results if anything fails.

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
14. **Music update:** hear all nine supplied tracks; add an MP3 to each playlist through the import menus, restart Play Mode and verify selection/track names. Listen through full tracks and menu/race/result crossfades; pause to check ducking, skip a track, mute/unmute and verify saved levels. Test an empty playlist's fallback and verify the built EXE includes custom songs.
15. **Race HUD update:** compare the map's car/next gate with the world, complete laps and verify last/best timing, pause to verify timers stop, drive backward to trigger the warning and turn around to clear it. Check the map in arenas and at each supported resolution. Alt-tab a local event and confirm it remains paused until Resume.
16. **Online:** deploy a WSS relay and check `/health`, create/join with two PCs in different homes, start only when both worlds load, and finish all three modes. Compare core/vehicle states, laps, scores, winner and time; rematch twice, disconnect each peer, restart the relay, test bad codes and verify return to menu. Measure input delay and bandwidth. No part of this Internet gameplay check has run here.
17. **Visual pass:** verify no shader errors or pink materials, check skies/fog/reflections across five environments, drift and jump to inspect trails/smoke, recover without a long teleport streak, switch quality to disable smoke/headlights, and check memory after repeated matches.
