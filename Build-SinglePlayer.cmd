@echo off
setlocal
title Velocity Rivals - Single Player Build
set "VR_PROJECT=%~dp0."
set "VR_UNITY=%ProgramFiles%\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe"
if defined UNITY_EDITOR_PATH set "VR_UNITY=%UNITY_EDITOR_PATH%"
if not exist "%VR_UNITY%" (
  echo Unity 6000.6.7f1 was not found at "%VR_UNITY%".
  echo Install it through Unity Hub, or set UNITY_EDITOR_PATH to your Unity.exe.
  goto failed
)
echo Close this project in Unity before continuing. Keep Unity Hub installed and signed in.
echo This runs EditMode and PlayMode tests, then builds and packages the single-player game.
echo It can take several minutes. Logs are written to Artifacts.
pause
if not exist "%VR_PROJECT%\Artifacts" mkdir "%VR_PROJECT%\Artifacts"
call :suite EditMode
if errorlevel 1 goto failed
call :suite PlayMode
if errorlevel 1 goto failed
if exist "%VR_PROJECT%\Artifacts\latest-singleplayer-package.json" del "%VR_PROJECT%\Artifacts\latest-singleplayer-package.json"
start "" /wait "%VR_UNITY%" -batchmode -nographics -quit -projectPath "%VR_PROJECT%" -buildTarget Win64 -executeMethod VelocityRivals.Editor.ProjectBuilder.BuildWindowsPackage -logFile "%VR_PROJECT%\Artifacts\build.log"
if errorlevel 1 goto failed
powershell -NoProfile -Command "try { $a = Get-Content -LiteralPath ($env:VR_PROJECT + '\Artifacts\latest-singleplayer-package.json') -Raw -ErrorAction Stop | ConvertFrom-Json; if (!(Test-Path -LiteralPath $a.package) -or !(Test-Path -LiteralPath $a.executable) -or !(Test-Path -LiteralPath ($a.package + '.sha256'))) { exit 1 }; exit 0 } catch { Write-Host $_; exit 1 }"
if errorlevel 1 goto failed
echo.
echo BUILD COMMAND SUCCEEDED. Your game ZIP and checksum are in Builds\Packages.
echo Extract the entire ZIP, launch VelocityRivals.exe, and complete the release playtest checklist.
start "" explorer "%VR_PROJECT%\Builds\Packages"
pause
exit /b 0

:suite
set "VR_TEST_REPORT=%VR_PROJECT%\Artifacts\%~1.xml"
if exist "%VR_TEST_REPORT%" del "%VR_TEST_REPORT%"
echo Running %~1 tests...
start "" /wait "%VR_UNITY%" -batchmode -projectPath "%VR_PROJECT%" -runTests -testPlatform %~1 -testResults "%VR_TEST_REPORT%" -logFile "%VR_PROJECT%\Artifacts\%~1.log"
if errorlevel 1 exit /b 1
powershell -NoProfile -Command "try { [xml]$r = Get-Content -LiteralPath $env:VR_TEST_REPORT -ErrorAction Stop; if ($r.'test-run'.result -ne 'Passed' -or [int]$r.'test-run'.total -le 0 -or [int]$r.'test-run'.failed -gt 0) { exit 1 }; exit 0 } catch { Write-Host $_; exit 1 }"
exit /b %errorlevel%

:failed
echo.
echo BUILD STOPPED. No successful release is being claimed.
echo Read the newest Artifacts log or XML report. Check Unity activation, Windows build support,
echo compile errors, failing tests, and that this project is not already open in Unity.
pause
exit /b 1
