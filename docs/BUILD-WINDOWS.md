# Create a Windows game you can launch without Unity

The GitHub download is a Unity source project. This cloud workspace has no licensed Unity Editor and has not produced an executable. The following steps use your installed Unity 6000.6.7f1 to produce one. A successful compile/build still needs gameplay testing before release.

## Download and open this update

1. Download https://github.com/DJKFusion/Velocity-Rivals/archive/refs/heads/main.zip.
2. Extract it into a **new folder**. Keep your old project, custom music and other local changes until you have transferred anything you want to preserve.
3. In Unity Hub choose **Projects → Add → Add project from disk**. Select the extracted folder containing `Assets`, `Packages` and `ProjectSettings`.
4. Open with **6000.6.7f1** and wait for importing to finish.
5. Choose **Velocity Rivals → Play Game**. Check the menu, Free Play, the cars and music. Stop Play Mode before building.

## Recommended: double-click build with tests

Close the project in Unity after importing it. Double-click **Build-SinglePlayer.cmd** in the extracted project folder. It runs EditMode and PlayMode tests, checks single-player assets/configuration, then builds and packages Windows. Any failed test or build stops the script. On success it opens **Builds/Packages**, containing a versioned single-player game ZIP and SHA-256 checksum. The batch script itself has not been executed in this Linux workspace.

If Unity is installed in a custom location, use the PowerShell command below, or set UNITY_EDITOR_PATH to the full Unity.exe path before running the batch file.

## Build through the menu

1. Open **Window → General → Test Runner** and run the EditMode and PlayMode suites. Resolve any failing tests and red Console errors.
2. Choose **Velocity Rivals → Build Windows ZIP**.
3. Wait for Unity to finish. On success the ZIP is revealed in File Explorer, under the project's `Builds/Packages` folder.
4. Extract that ZIP into a new folder and launch **VelocityRivals.exe**. You do not need Unity open to play the resulting Windows game.

The menu checks build support, saves your scenes if requested, builds the actual entry scene, rejects a failed BuildReport, checks the EXE/Data/UnityPlayer files and packages the **whole** player directory. It also includes a `START HERE.txt` controls guide. Each package receives a new timestamped filename. It never labels a source-only ZIP as a Windows game.

The menu build does not run tests automatically. If you want a build that stops when either test suite fails, use the PowerShell workflow below.

If Windows support is missing, go to **Unity Hub → Installs → the gear/menu beside 6000.6.7f1 → Add modules**, and install the Windows build support module required by your selected scripting backend. Reopen the project afterward. The build preserves your chosen Mono/IL2CPP backend; IL2CPP additionally needs its Windows compiler prerequisites.

## Build with tests from PowerShell

Close this project in Unity first, then run from its folder:

```powershell
.\Tools\Build-Windows.ps1 -RunTests -Package
```

If your Editor is installed elsewhere:

```powershell
.\Tools\Build-Windows.ps1 -Unity 'D:\Unity\6000.6.7f1\Editor\Unity.exe' -RunTests -Package
```

Reports/logs are in `Artifacts`. Test failure, missing reports and zero executed tests prevent the build. Omitting `-Package` leaves the unpacked game in `Builds/SinglePlayer/Windows`. `VELOCITY_BUILD_PATH` can override the executable path; the package still goes in `Builds/Packages`.

## Check the actual player before sharing

Launch the extracted EXE, hear menu/race music, complete all modes and a three-round championship, test controller input, pause/resume and alt-tab, quit/reopen to verify progress saves, and run on a second Windows PC. Check frame rate and memory during repeated events. Complete the entire 12-event career and verify the game works with the network disconnected. This edition has no multiplayer or server requirement. Full acceptance checks are in [TESTING.md](TESTING.md).

Building does not finish the outstanding game design, graphics tuning or platform release work. Follow [the single-player checklist](SINGLE-PLAYER-RELEASE.md). See [STATUS.md](STATUS.md) for the current scope and remaining work.
