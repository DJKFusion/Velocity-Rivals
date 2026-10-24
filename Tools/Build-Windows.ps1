param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe",
    [switch]$RunTests
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
& $Unity -batchmode -nographics -quit -projectPath $Project -buildTarget Win64 -executeMethod VelocityRivals.Editor.ProjectBuilder.BuildWindows -logFile (Join-Path $Artifacts 'build.log')
if ($LASTEXITCODE -ne 0) { throw 'Unity build failed. See Artifacts/build.log.' }
if (!(Test-Path (Join-Path $Project 'Builds/Windows/VelocityRivals.exe'))) { throw 'No executable was produced.' }
Write-Host "Build ready at $Project\Builds\Windows. Ship the entire folder, including the Data directory and UnityPlayer.dll."
