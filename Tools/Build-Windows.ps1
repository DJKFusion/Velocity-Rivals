param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe",
    [switch]$RunTests,
    [switch]$Package
)
$ErrorActionPreference = 'Stop'
$Project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (!(Test-Path $Unity)) { throw "Unity Editor not found at $Unity. Pass -Unity with your Editor path." }
$Artifacts = Join-Path $Project 'Artifacts'
New-Item -ItemType Directory -Force -Path $Artifacts | Out-Null
if ($RunTests) {
    foreach ($Platform in @('EditMode', 'PlayMode')) {
        $ResultPath = Join-Path $Artifacts "$Platform.xml"
        if (Test-Path $ResultPath) { Remove-Item $ResultPath }
        & $Unity -batchmode -nographics -projectPath $Project -runTests -testPlatform $Platform -testResults $ResultPath -logFile (Join-Path $Artifacts "$Platform.log")
        if ($LASTEXITCODE -ne 0) { throw "$Platform tests failed. See Artifacts logs." }
        if (!(Test-Path $ResultPath)) { throw "$Platform runner produced no result file." }
        [xml]$Results = Get-Content $ResultPath
        if ([int]$Results.'test-run'.total -le 0 -or [int]$Results.'test-run'.failed -gt 0 -or $Results.'test-run'.result -ne 'Passed') {
            throw "$Platform tests did not complete successfully. Inspect $ResultPath."
        }
    }
}
$Method = if ($Package) { 'VelocityRivals.Editor.ProjectBuilder.BuildWindowsPackage' } else { 'VelocityRivals.Editor.ProjectBuilder.BuildWindows' }
& $Unity -batchmode -nographics -quit -projectPath $Project -buildTarget Win64 -executeMethod $Method -logFile (Join-Path $Artifacts 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'Unity build failed. See Artifacts/build.log.' }
$Executable = if ($env:VELOCITY_BUILD_PATH) {
    if ([IO.Path]::IsPathRooted($env:VELOCITY_BUILD_PATH)) { $env:VELOCITY_BUILD_PATH } else { Join-Path $Project $env:VELOCITY_BUILD_PATH }
} else { Join-Path $Project 'Builds/Windows/VelocityRivals.exe' }
if (!(Test-Path $Executable)) { throw 'No executable was produced.' }
Write-Host "Build ready at $Executable. Ship the entire folder, including the Data directory and UnityPlayer.dll."
if ($Package) { Write-Host "Game ZIP is in $Project\Builds\Packages. Extract it fully before launching the game." }
