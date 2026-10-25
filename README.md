# Velocity Rivals — Single Player

Latest update: [menu, HUD and soundtrack refresh](docs/PRESENTATION-AND-RADIO.md). Two-column home tiles, clearer racing instruments and results, and twelve newly arranged songs across ambient, house, breakbeat, drum-and-bass and synth styles. The source checks pass; the new Unity UI tests and Windows presentation review remain pending.

An original Unity 6 arcade racing and vehicle-sports project for DJKFusion. The current edition plays offline and has no multiplayer menu, room setup or networking component. Multiplayer source is retained behind a disabled assembly constraint for future work.

**Status: version 0.6.0 development/release candidate source.** No Windows EXE has been produced in this cloud workspace. Unity compilation, gameplay, shader appearance and target-PC performance still need verification before release. [Complete scope, build instructions and acceptance checklist](docs/SINGLE-PLAYER-RELEASE.md).

## Open and play in Unity

1. [Download the project ZIP](https://github.com/DJKFusion/Velocity-Rivals/archive/refs/heads/main.zip) and extract into a **new folder**. Preserve your old project and any custom music/assets.
2. In **Unity Hub → Projects → Add → Add project from disk**, select the folder containing `Assets`, `Packages` and `ProjectSettings`.
3. Open with **Unity 6000.6.7f1**. Wait for packages/assets to import. The project pins Input System 1.20.1, URP 17.0.4, uGUI 2.0.0 and Test Framework 1.4.5. Resolve any red Console errors before continuing.
4. Choose **Velocity Rivals → Play Game**. Use **CAREER TOUR** for the campaign, **PLAY** for individual events, or **HOW TO PLAY** for controls/rules.

## Build the standalone Windows game

After importing successfully, **close the project in Unity**, then double-click **`Build-SinglePlayer.cmd`**. It uses your installed Editor, runs both Unity test suites, checks single-player build assets/configuration, and builds/packages Windows. Unity must be activated through Hub and have the Windows build module installed.

On success, the ZIP and SHA-256 checksum are in **`Builds/Packages`**. Extract the entire ZIP and run **`VelocityRivals.exe`**. That built game works without Unity open. Logs and XML test reports go to `Artifacts`. The build stops on failed/missing/empty reports or packaging failure.

Alternative PowerShell workflow:

```powershell
.\Tools\Build-Windows.ps1 -RunTests -Package
```

Pass `-Unity 'C:\path\to\Editor\Unity.exe'` for a different installation, or set `UNITY_EDITOR_PATH` for the batch file. The unpacked player defaults to `Builds/SinglePlayer/Windows`. Inside Unity, **Velocity Rivals → Build Windows ZIP** builds directly; it does not run tests first. [Build guide](docs/BUILD-WINDOWS.md).

**The source ZIP is not the game ZIP.** Share the generated Windows package only after completing the [release playtests](docs/SINGLE-PLAYER-RELEASE.md).

## Included single-player content

- **Career Tour:** 12 events, four cups and a Grand Final, with saved bronze/silver/gold medals and replay. Bronze unlocks the next event; finishing the tour unlocks a completion achievement.
- **Seven quick-play modes:** three-round Championship, Arena Clash, Boost Battle, Stunt Rush, Time Attack, Free Play and Elimination. Circuit rivals choose passing lanes; Championship and Elimination include slipstreaming.
- **Five themes:** Neon City Circuit, Skyfall Speedway, Desert Velocity, Cyber Dome and Glacier Rush. Layouts are generated; arenas share a common base layout.
- **Six original cars:** Viper, Bulwark, Phantom, Apex, Nomad and Comet. Earn XP to unlock the garage, with an additional car each 500 XP.
- **Garage:** a lit rotating car preview, vehicle selection, paint, wheel scale, trail color, decals, spoilers and finishes.
- **Presentation:** twelve distinct original electronic arrangements, skies/clouds/stars, road textures/normals, sky reflections, headlights, drift smoke, tyre trails, bloom, ACES and color grading. Graphics quality and reduced-effects options are available.
- **Progress:** local profile backup, achievements, career medals, per-event/environment/vehicle records and Time Attack ghosts. Existing version-1 profiles migrate to schema 2.

The original car models and generated surface/audio files have been checked with independent tools. The integrated game still requires Unity review; the visual features are not a measured claim of photorealism or 60 FPS.

## Controls

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Accelerate / reverse | W / S or arrows | Right / left trigger |
| Steer | A / D or arrows | Left stick |
| Boost | Left Shift | B / East |
| Jump / second jump | Space | A / South |
| Drift | Left Ctrl | X / West |
| Air roll | Q / E | Left shoulder + stick |
| Recover | R | Y / North |
| Pause | Escape | Start |
| Menus | Mouse, arrows, Enter | D-pad, A / South |

Start in **Free Play → Cyber Dome** to learn the car. Career time/score targets and AI difficulty are initial tuning values; playtest them before release.

## Music and saves

Twelve songs provide about 15.7 minutes of original synthwave. In **Settings → Soundtrack**, choose **Open Custom Music Folder**, put OGG/WAV/MP3 songs into **Menu** or **Race**, then **Reload Custom Music**. No rebuild is required for personal songs. To bundle songs for distribution, import them through **Velocity Rivals → Music → Import Menu Track / Import Race Track** in Unity and rebuild. [Music guide](docs/MUSIC.md).

Profiles and ghosts normally live in `%USERPROFILE%\AppData\LocalLow\DJKFusion\Velocity Rivals`. Keep backups before manually editing saves. Version-1 records with no track/car identity remain visible as earlier-version records; they are not assigned an invented track.

## Development checks

```sh
dotnet run --project Tools/LogicTests/LogicTests.csproj --configuration Release
python -m pip install tree-sitter tree-sitter-c-sharp
python Tools/validate_source.py
```

41 standalone C# checks passed here. They test shared logic and archive fixtures, including preserved networking regression sources; they are not Unity gameplay tests. [Testing evidence](docs/TESTING.md), [feature ledger](docs/STATUS.md), [asset provenance](docs/ART.md), and [release preparation](docs/RELEASE.md) describe the remaining work.
