# Velocity Rivals

An original Unity 6 arcade racing and vehicle-sports project for DJKFusion.

This delivery contains integrated development source, a bootstrap scene, six vehicle prefabs and tunable stat assets, six generated car meshes, procedural environments, six local modes, menus, saves, audio placeholders, and an experimental two-player LAN gate sprint. It is **not a tested Windows executable or a finished release**. Unity import, compilation, physics tuning, multiplayer playtests and GPU profiling remain required.

## Open the game

1. Finish installing **Unity 6000.6.7f1**, the version selected in your Unity Hub screenshot.
2. Extract the source ZIP to a local folder. In Unity Hub choose **Projects → Add → Add project from disk** and select the folder containing `Assets`, `Packages`, and `ProjectSettings`. This is an existing project; do not create another project over it.
3. Open it using 6000.6.7f1. Allow Unity Package Manager to download the packages declared in `Packages/manifest.json`. The manifest pins Input System 1.20.1, URP 17.0.4, uGUI 2.0.0 and Test Framework 1.4.5; the installed Editor must verify compatibility. No unverified package lockfile is supplied. Commit the lockfile Unity generates after successful resolution.
4. The Editor setup automatically creates and assigns a URP pipeline. Open **Velocity Rivals → Open Game**, or double-click `Assets/Scenes/VelocityRivals.unity`.
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
| LAN development controls | F8 | Use menu to host; IP typing needs keyboard |

## What is connected

- **Velocity Championship:** three sequential rounds, three laps each, ordered checkpoints, three AI rivals, standings, points and an overall result.
- **Arena Clash:** player plus an AI teammate against two rivals; a physics energy core and two scoring zones; first to five or a timed result.
- **Boost Battle:** high-speed vehicle impacts and knockback scoring, first to eight or a timed result. Power-up pickups are not implemented.
- **Stunt Rush:** timed jumps/air rotations with upright-landing points and a target score.
- **Time Attack:** three-lap personal records and a local fastest-run ghost. Online leaderboards are not connected.
- **Free Play:** practice with no mandatory win condition; pause to restart or leave.

Five named environments are generated at runtime from a shared world builder. Circuit shapes, scenery, ramps, palette and traction vary; arenas currently share a common layout. These are starter environments, not five production-quality authored levels.

The garage saves paint, wheel size, trail colour, stripe/spoiler and finish choices. Event completion grants XP, levels, vehicle unlocks and basic achievements. The first car is available initially; each additional 500 XP unlocks another. Profile JSON and a backup are stored under Unity's `Application.persistentDataPath`. On Windows this is normally `%USERPROFILE%\AppData\LocalLow\DJKFusion\Velocity Rivals`. Time Attack ghosts are separate files in the same directory.

Multiplayer is a separate **LAN gate sprint**, not networked championship or Arena Clash. See [multiplayer scope and test steps](docs/MULTIPLAYER.md).

## Build and test

Inside Unity use **Window → General → Test Runner** and run EditMode and PlayMode tests. Then use **Velocity Rivals → Build Windows**. The Editor needs Windows build support installed through Hub.

Alternatively, from PowerShell in this project:

```powershell
.\Tools\Build-Windows.ps1 -RunTests
```

Pass `-Unity 'C:\path\to\Editor\Unity.exe'` if your installation is elsewhere. Logs and test reports go to `Artifacts`. A successful build goes to `Builds/Windows/VelocityRivals.exe`; distribute the entire Windows directory, not just the EXE. No EXE is included in this source delivery.

Standalone pure-logic tests, which do not require Unity:

```sh
dotnet run --project Tools/LogicTests/LogicTests.csproj --configuration Release
```

These compile the **same** checkpoint and network framing classes the game uses. They do not substitute for Unity tests. See [testing evidence and acceptance checks](docs/TESTING.md), [feature status and known issues](docs/STATUS.md), and [release requirements](docs/RELEASE.md).

## Assets and architecture

- `Assets/Scripts/Core`: entry point and runtime composition.
- `Vehicles`, `Cameras`: Rigidbody driving, four suspension rays, Input System actions and follow camera.
- `Gameplay`: generated world, match state, race progress, AI, arena scoring and ghost playback.
- `UI`, `Audio`, `SaveSystem`: menus/HUD, original synthesized placeholder audio and profile persistence.
- `Multiplayer`: independent host-authoritative LAN sprint and bounded message framing.
- `Assets/Resources/VehicleStats`: editable ScriptableObjects used by matches.
- `Assets/Prefabs`: six physics vehicle prefabs; visual meshes are instantiated on Play. **Velocity Rivals → Regenerate Entry Scene and Vehicle Assets** can bake visual prefabs inside Unity. That command replaces the generated entry scene and prefabs; preserve intentional hand edits before running it.
- `Tools/Blender`: repeatable original asset generators and an offline contact sheet. [Art documentation](docs/ART.md).

Development should continue in this project, preserving working systems. The next gate is successful import and tests in the specified Unity Editor, followed by controller/AI playtesting and a Windows build. Download the source from the GitHub repository using **Code → Download ZIP**.
