"""Build the separate DRM-free itch.io download. Never bundle it in the base player."""
from pathlib import Path
import json
import zipfile

root = Path(__file__).resolve().parents[1]
output = root / 'Builds' / 'Store' / 'VelocityRivals-Aurora-Paints.zip'
output.parent.mkdir(parents=True, exist_ok=True)
with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED) as archive:
    archive.writestr('aurora-paints.json', json.dumps({'schema': 1, 'packId': 'aurora-paints'}, indent=2))
    archive.writestr('INSTALL.txt', '''Velocity Rivals - Aurora Paint Collection
Requires the Velocity Rivals build with the Cosmetic Store.
Contains four paint colours for all six cars. Cosmetic only.

1. Extract this ZIP.
2. In the game, open Garage > Cosmetic Store > Open Pack Folder.
3. Copy aurora-paints.json into that folder.
4. Select Reload Installed Packs, then Equip beside a paint.
5. Return to the main menu to see it on your car.

You can switch back to an included paint using Garage > Paint.
To reinstall on another PC, download the pack from your itch.io purchase page.
This is a DRM-free content pack. It does not contain the base game.
''')
with zipfile.ZipFile(output) as archive:
    assert archive.testzip() is None
    assert json.loads(archive.read('aurora-paints.json'))['packId'] == 'aurora-paints'
print(output)
