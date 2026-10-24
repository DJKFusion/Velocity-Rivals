"""Package source and original assets, excluding Git, Unity and build caches."""
from pathlib import Path
import zipfile

root = Path(__file__).resolve().parents[1]
output = root.parent / 'Velocity-Rivals-source.zip'
excluded = {'.git', 'Library', 'Temp', 'Logs', 'UserSettings', 'Builds', 'Artifacts', 'obj', 'bin', '__pycache__'}
paths = [p for p in root.rglob('*') if p.is_file() and not any(part in excluded for part in p.relative_to(root).parts)]
with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted(paths):
        archive.write(path, Path('Velocity-Rivals') / path.relative_to(root))
with zipfile.ZipFile(output) as archive:
    assert archive.testzip() is None, 'Archive integrity check failed'
    names = set(archive.namelist())
    for required in ('Assets/Scenes/VelocityRivals.unity', 'ProjectSettings/ProjectVersion.txt', 'Packages/manifest.json', 'README.md', 'Tools/LogicTests/LogicTests.csproj',
                     'Assets/Resources/Music/Menu/01 - Midnight Grid.ogg', 'Assets/Resources/Music/Race/01 - Neon Pursuit.ogg',
                     'Assets/Resources/Music/Race/02 - Afterburn.ogg', 'Assets/Resources/Music/Race/06 - Final Overdrive.ogg',
                     'docs/PLAYER-GUIDE.txt', 'docs/UPDATE-STRUCTURED-WORLDS.md', 'Assets/Scripts/Gameplay/CircuitLayout.cs',
                     'Assets/Scripts/Gameplay/WorldDistricts.cs', 'Build-SinglePlayer.cmd', 'docs/SINGLE-PLAYER-RELEASE.md', 'Assets/Scripts/Gameplay/CareerProgress.cs',
                     'Assets/Scripts/Multiplayer/VelocityRivals.Multiplayer.asmdef', 'Assets/Resources/VelocitySky.shader', 'Tools/Relay/server.py', 'render.yaml', 'docs/ONLINE.md', 'docs/MUSIC.md', 'docs/BUILD-WINDOWS.md'):
        assert 'Velocity-Rivals/' + required in names, required
print(f'{output}: {len(paths)} files, {output.stat().st_size} bytes; ZIP integrity and required entries verified.')
