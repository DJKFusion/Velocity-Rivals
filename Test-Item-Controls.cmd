@echo off
setlocal
title Velocity Rivals - Item Control Diagnostics
set "VR_PROJECT=%~dp0."
set "VR_UNITY=%ProgramFiles%\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe"
if defined UNITY_EDITOR_PATH set "VR_UNITY=%UNITY_EDITOR_PATH%"
echo Close this project in Unity first. This only runs the two item-control tests.
echo It does not build or package the game, and does not replace the full build tests.
pause
if not exist "%VR_UNITY%" (
  echo Unity was not found at "%VR_UNITY%".
  goto end
)
if not exist "%VR_PROJECT%\Artifacts" mkdir "%VR_PROJECT%\Artifacts"
if exist "%VR_PROJECT%\Artifacts\ItemControls.xml" del "%VR_PROJECT%\Artifacts\ItemControls.xml"
start "" /wait "%VR_UNITY%" -batchmode -projectPath "%VR_PROJECT%" -runTests -testPlatform PlayMode -testFilter "VelocityRivals.Tests.SessionTests.*ActivatesHeldRaceItem" -testResults "%VR_PROJECT%\Artifacts\ItemControls.xml" -logFile "%VR_PROJECT%\Artifacts\ItemControls.log"
if errorlevel 1 (
  echo Item-control tests stopped. Send Artifacts\ItemControls.xml and ItemControls.log.
  goto end
)
powershell -NoProfile -Command "try { [xml]$r=Get-Content -LiteralPath ($env:VR_PROJECT+'\Artifacts\ItemControls.xml') -ErrorAction Stop; if ($r.'test-run'.result -ne 'Passed' -or [int]$r.'test-run'.passed -ne 2) { exit 1 }; exit 0 } catch { Write-Host $_; exit 1 }"
if errorlevel 1 (
  echo Could not confirm both tests passed. Send Artifacts\ItemControls.xml and ItemControls.log.
  goto end
)
echo BOTH ITEM-CONTROL TESTS PASSED. Now run Build-SinglePlayer.cmd for the full checks and game build.
:end
pause
