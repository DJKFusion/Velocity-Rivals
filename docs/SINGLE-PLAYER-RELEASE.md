# Single-player edition — 0.6.0 candidate

This update focuses on an offline Windows game. See [the UI redesign](UPDATE-UI-RELEASE.md) for 0.6 and [structured worlds](UPDATE-STRUCTURED-WORLDS.md) for 0.5. It adds a 12-event career with a defined ending and excludes multiplayer from the default player assembly. It is **not yet a verified finished release**: this cloud workspace has no Unity Editor/license or Windows player, so compilation, shader rendering, gameplay QA and an executable build have not run here.

## What is in the edition

- **Career Tour:** 12 events across Rookie Cup, Challenger Cup, Rival Cup and Velocity Finals. Time trials, combat, stunts, arena sport and four three-round championships lead to the Velocity Grand Final. Bronze unlocks the next event; silver/gold encourage replay. Career medals and completion persist locally. Time/score targets are initial tuning values and need playtesting.
- **Quick play:** seven modes including timed Elimination, five environment themes, three AI difficulty settings and six unlockable original cars.
- **Garage:** a lit, rotating showroom using the actual car/cosmetic construction. Vehicle selection, paint, wheels, trail choice, decal, spoiler and finish are saved. Static previews show cosmetic geometry/paint; a boost trail is visible when driving.
- **Presentation:** twelve original synthwave tracks, themed skies, textured/normal-mapped roads, reflections, headlights, drift smoke, tyre trails, bloom/ACES/color grading, a large countdown, map and race HUD. Actual appearance and performance remain unverified in Unity.
- **Options:** audio levels, three quality presets, borderless/windowed display, sensitivity, and reduced effects. Reduced effects removes bloom, vignette, impact bursts, smoke, showroom rotation and speed/boost FOV changes.
- **Progress:** XP/car unlocks, achievements, career medals, records separated by mode/environment/car and Time Attack ghosts. Version-1 profiles migrate to schema 2 without discarding old XP, customization or records. Old records are explicitly labeled as belonging to an earlier version.

## Fixes included

AI circuit targets extend beyond checkpoint planes so cars aim through the gate. Circuit recovery is owned by the match and resets the crossing sample to prevent teleport crossings. Cars stop on result screens. Unfinished races cannot set fastest-time records or count as finishes. Records are paginated. Save tests are isolated from the player's real profile.

## Multiplayer scope

The bootstrap creates no networking component; menus, music and HUD no longer reference it. `VelocityRivals.Multiplayer.asmdef` requires the opt-in `VELOCITY_MULTIPLAYER` symbol and is not auto-referenced. The release build guard rejects that symbol or an included multiplayer assembly. Existing network/server code is retained for possible future work, but it is not required to play or release this edition. No hosting setup is needed.

## Produce the actual Windows build

1. Download the latest project ZIP and extract it into a new folder, preserving your old project and custom files.
2. Open the folder through Unity Hub using **6000.6.7f1**. Wait for package import and fix any red Console errors. Ensure Windows build support is installed for your scripting backend.
3. Close the project in Unity.
4. Double-click **`Build-SinglePlayer.cmd`** in the extracted project folder. Press a key when prompted.
5. The script runs EditMode tests, PlayMode tests, build checks and the Windows build. It rejects failed, missing or empty test reports. It only reports packaging success when a new artifact report points to the EXE, ZIP and checksum.
6. On success, File Explorer opens **`Builds/Packages`**. Extract the new `VelocityRivals-SinglePlayer-0.6.0-….zip` and launch **`VelocityRivals.exe`**. Keep the entire extracted folder together.

If Unity is installed elsewhere, set the `UNITY_EDITOR_PATH` environment variable to its `Unity.exe`, or use `Tools/Build-Windows.ps1 -Unity 'C:\your\Unity.exe' -RunTests -Package`. The default is `%ProgramFiles%\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`.

Logs/reports are in `Artifacts`. Build files are in `Builds/SinglePlayer/Windows`; packages have a SHA-256 sidecar. `Artifacts/latest-singleplayer-package.json` identifies the successful package. The batch file does not install/activate Unity, resolve failing tests or certify gameplay quality. Its Windows execution has not been tested in this Linux cloud workspace.

Inside Unity, **Velocity Rivals → Check Single Player Build** checks the build configuration/assets/shaders. **Build Windows ZIP** builds/packages directly and does not run test suites first. Use the batch file or PowerShell `-RunTests` option for the automated test gate.

## Release acceptance — all still require your actual player

- [ ] Import and both Unity test suites pass without errors.
- [ ] The extracted Windows EXE launches on a second PC without Unity installed.
- [ ] Complete all 12 career events, advance every championship round, reach the final tour result and replay for a better medal.
- [ ] Finish every quick-play mode; test loss, draw, timeout, pause/restart/menu return and recovery.
- [ ] AI completes all five circuits and arena opponents behave consistently; tune unfair or stuck situations.
- [ ] Earn all car unlocks, preview/change cosmetics, close/reopen, and confirm save migration/records/medals survive.
- [ ] Keyboard, mouse and controller work; test disconnect/reconnect and alt-tab pause.
- [ ] No missing/pink shaders, clipped menus, broken tyres or silent tracks at 720p, 1080p and 1440p. Test each quality/effect setting.
- [ ] Measure frame time and memory on intended hardware over complete events and repeated restarts. No minimum/recommended hardware specification has been established yet.
- [ ] Inspect credits/asset notices and your Unity licensing/distribution terms; include any notices needed for custom assets you add.

Upload the **built game ZIP** only after these checks. The GitHub source ZIP is not a playable standalone game. A successful build is evidence of compilation and packaging, not of a fully tested release.

## Verification performed here

41 standalone C# checks pass, including six career checks and four authored-layout checks. The runner retains networking regression checks for preserved source, but that code is excluded from the default Unity player. Static C# syntax, Unity metadata/reference checks, audio-file validation and source ZIP checks pass. New Unity tests cover legacy save migration, track/car records, unfinished runs, career persistence, shader imports, assembly exclusion and career-menu flow; those tests are authored but unrun here.
