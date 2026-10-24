# Velocity Rivals

An original Unity 6 arcade racing and vehicle-sports project for DJKFusion.

This delivery contains integrated development source, a bootstrap scene, six vehicle prefabs and tunable stat assets, six generated car meshes, procedural environments, six local modes, menus, saves, nine original synthwave tracks, and three two-player modes with direct LAN or private online-room transport. It is **not a tested Windows executable or a finished release**. The online relay is implemented but **not publicly deployed**. Unity import, compilation, physics tuning, multiplayer playtests and GPU profiling remain required.

## Open the game

1. Finish installing **Unity 6000.6.7f1**, the version selected in your Unity Hub screenshot.
2. Extract the source ZIP to a local folder. In Unity Hub choose **Projects → Add → Add project from disk** and select the folder containing `Assets`, `Packages`, and `ProjectSettings`. This is an existing project; do not create another project over it.
3. Open it using 6000.6.7f1. Allow Unity Package Manager to download the packages declared in `Packages/manifest.json`. The manifest pins Input System 1.20.1, URP 17.0.4, uGUI 2.0.0 and Test Framework 1.4.5; the installed Editor must verify compatibility. No unverified package lockfile is supplied. Commit the lockfile Unity generates after successful resolution.
4. The Editor setup automatically creates and assigns a URP pipeline and opens the game when the current scene is an untouched default scene. Use **Velocity Rivals → Play Game** to open the correct scene and start playing, or double-click `Assets/Scenes/VelocityRivals.unity`.
5. Press **Play**, select **PLAY**, choose an event/environment, and select **START EVENT**. Begin with Free Play in Cyber Dome to check the controls before racing.

If Unity reports a compile/import error, copy the first red Console error with its file and line. Do not ignore errors or switch Editor versions blindly. This project could not be imported in the cloud because that machine has no Unity Editor or license.

## Controls

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Accelerate / brake into reverse | W / S | Right / left trigger |
| Steer | A / D | Left stick |
| Boost | Left Shift | East / B |
| Jump / second jump | Space | South / A |
| Drift / handbrake | Left Ctrl | West / X |
| Air roll | Q / E | Left shoulder + stick |
| Recover car | R | North / Y |
| Pause local event | Escape | Start |
| Menu navigation | Mouse, arrows, Enter | D-pad, South / A |
| Multiplayer room controls | F8 | Use menu to host; address/code typing needs keyboard |

## What is connected

- **Velocity Championship:** three sequential rounds, three laps each, ordered checkpoints, three AI rivals, standings, points and an overall result.
- **Arena Clash:** player plus an AI teammate against two rivals; a physics energy core and two scoring zones; first to five or a timed result.
- **Boost Battle:** high-speed impacts, armor, knockout/respawn and knockback scoring; first to fifteen or a timed result. Cyan pickups grant six seconds of shielding, orange grants eight seconds of double damage, and purple immediately pulses damage to rivals within 18 meters. Pickups recharge in twelve seconds. Knockouts award three points and rebuild the car after three seconds with two seconds of spawn protection.
- **Stunt Rush:** timed jumps/air rotations with upright-landing points and a target score.
- **Time Attack:** three-lap personal records and a local fastest-run ghost. Online leaderboards are not connected.
- **Free Play:** practice with no mandatory win condition; pause to restart or leave.

Five named environments are generated at runtime from a shared world builder. Circuit shapes, scenery, ramps, palette and traction vary; arenas currently share a common layout. These are starter environments, not five production-quality authored levels.

The latest visual pass adds sculpted car shells, wheel arches and spoke wheels; distinct paint/glass/metal materials; marked roads, lit city scenery, natural formations and arena spectator structures. [Visual update details and required checks](docs/UPDATE-VISUALS.md). The Blender car contact sheet is an offline asset preview, not an in-game screenshot.

The garage saves paint, wheel size, trail colour, stripe/spoiler and finish choices. Event completion grants XP, levels, vehicle unlocks and basic achievements. The first car is available initially; each additional 500 XP unlocks another. Profile JSON and a backup are stored under Unity's `Application.persistentDataPath`. On Windows this is normally `%USERPROFILE%\AppData\LocalLow\DJKFusion\Velocity Rivals`. Time Attack ghosts are separate files in the same directory.

The garage includes a rotating preview of the selected model's paint and finish. Select Rookie, Rival or Elite difficulty before an event; it changes AI acceleration and maximum speed. Settings include steering sensitivity. Countdown, jump, drift, boost, impact, power-up and goal audio are synthesized placeholders; hit and pickup particles are generated at runtime. These new integrations require Unity playtesting.

Multiplayer now offers **Circuit Duel**, **Arena Duel** and **Gate Sprint**, with a lobby, synchronized countdown, selected cars and rematches. Choose **MULTIPLAYER → ONLINE ROOM** for room-code play or **LOCAL NETWORK / LAN** for direct connections. Friends in different homes need the included relay deployed once: [online setup guide](docs/ONLINE.md). No public matchmaking or production anti-cheat is connected. [Multiplayer scope and tests](docs/MULTIPLAYER.md).

The new visual pass adds themed skies/clouds/stars, textured and normal-mapped roads, sky reflections, headlights, tyre trails, drift smoke and color grading. Performance graphics disables smoke and headlights. [Update details and remaining validation](docs/UPDATE-ONLINE-VISUALS.md). These effects have not yet been rendered or profiled in Unity here.

The latest update adds a live map with the next checkpoint, current/last/best lap times, wrong-way warnings, a car-shaped Time Attack ghost, How to Play screens, and local-event auto-pause when the application loses focus. Lap bests are for the current round; event records and ghosts still persist separately. These HUD and gameplay integrations require Unity verification.

## Music

Nine original synthwave tracks are included, with three menu songs and six racing songs. New additions include **Skyline Dreams**, **Solar Drift**, **Hyperdrive**, **Chrome Horizon**, **Ion Storm** and **Final Overdrive**. Menu and event playlists change automatically with crossfades and reduced music volume during local pause. **Settings → Soundtrack** shows the current track and lets you skip or change volume.

To add your own songs, stop Play Mode and use **Velocity Rivals → Music → Import Race Track** or **Import Menu Track**, then choose an MP3, WAV or OGG file. Start Play Mode again to refresh the playlist. Songs are included in subsequent Windows builds. [Full music instructions](docs/MUSIC.md).

## Build and test

Inside Unity use **Window → General → Test Runner** and run EditMode and PlayMode tests. Then use **Velocity Rivals → Build Windows ZIP** to build the game and package all required files. The Editor needs Windows build support installed through Hub. [Step-by-step Windows build instructions](docs/BUILD-WINDOWS.md).

Alternatively, from PowerShell in this project:

```powershell
.\Tools\Build-Windows.ps1 -RunTests -Package
```

Close the project in Unity before running the script. Pass `-Unity 'C:\path\to\Editor\Unity.exe'` if your installation is elsewhere. Logs and test reports go to `Artifacts`. A successful build goes to `Builds/Windows/VelocityRivals.exe`; its full package goes to `Builds/Packages`. Extract the full game ZIP before launching the EXE. No EXE is included in this source delivery.

Standalone pure-logic tests, which do not require Unity:

```sh
dotnet run --project Tools/LogicTests/LogicTests.csproj --configuration Release
```

These compile the **same** checkpoint, combat, lap timing, playlist, network framing and archive classes the project uses. Packaging tests use synthetic fixtures, not an actual player build. They do not substitute for Unity tests. See [testing evidence and acceptance checks](docs/TESTING.md), [feature status and known issues](docs/STATUS.md), and [release requirements](docs/RELEASE.md).

## Assets and architecture

- `Assets/Scripts/Core`: entry point and runtime composition.
- `Vehicles`, `Cameras`: Rigidbody driving, four suspension rays, Input System actions and follow camera.
- `Gameplay`: generated world, match state, race progress, AI, arena scoring and ghost playback.
- `UI`, `Audio`, `SaveSystem`: menus/HUD, original soundtrack/synthesized effects and profile persistence.
- `Multiplayer`: host-authoritative duel rules, TCP framing and bounded WebSocket transport.
- `Tools/Relay`: private online room server, tests and Docker deployment files; `render.yaml` provides a hosting blueprint.
- `Assets/Resources/VehicleStats`: editable ScriptableObjects used by matches.
- `Assets/Prefabs`: six physics vehicle prefabs; visual meshes are instantiated on Play. **Velocity Rivals → Regenerate Entry Scene and Vehicle Assets** can bake visual prefabs inside Unity. That command replaces the generated entry scene and prefabs; preserve intentional hand edits before running it.
- `Tools/Blender`: repeatable original asset generators and an offline contact sheet. [Art documentation](docs/ART.md).

Development should continue in this project, preserving working systems. The next gate is successful import and tests in the specified Unity Editor, followed by controller/AI playtesting and a Windows build. Download the source from the GitHub repository using **Code → Download ZIP**.
