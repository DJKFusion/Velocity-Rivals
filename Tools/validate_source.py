"""Static delivery checks. This is NOT a Unity compiler or playtest.

Optional C# parse: pip install tree-sitter tree-sitter-c-sharp
"""
from pathlib import Path
import json
import re
import sys

root = Path(__file__).resolve().parents[1]
errors = []
for path in list(root.glob('Packages/*.json')) + list(root.glob('Assets/**/*.asmdef')):
    try:
        json.loads(path.read_text())
    except ValueError as e:
        errors.append(f'{path}: {e}')

guids = {}
for meta in root.glob('Assets/**/*.meta'):
    match = re.search(r'^guid: ([0-9a-f]{32})$', meta.read_text(), re.M)
    if not match:
        errors.append(f'Missing valid GUID: {meta}')
        continue
    guid = match.group(1)
    if guid in guids:
        errors.append(f'Duplicate GUID: {meta}, {guids[guid]}')
    guids[guid] = meta
for path in root.glob('Assets/**/*'):
    if path.suffix in ('.unity', '.prefab', '.asset'):
        for guid in re.findall(r'guid: ([0-9a-f]{32})', path.read_text()):
            if guid not in guids:
                errors.append(f'Unresolved asset reference {guid}: {path}')
    if not path.name.endswith('.meta') and not Path(str(path) + '.meta').exists():
        errors.append(f'Missing metadata: {path}')
for name in ('Viper', 'Bulwark', 'Phantom', 'Apex', 'Nomad', 'Comet'):
    for path in (f'Assets/Resources/VehicleModels/{name}.fbx', f'Assets/Resources/VehicleStats/{name}.asset', f'Assets/Prefabs/{name}.prefab'):
        if not (root / path).is_file(): errors.append(f'Missing delivery asset: {path}')

parsed = 0
network_assembly = json.loads((root / 'Assets/Scripts/Multiplayer/VelocityRivals.Multiplayer.asmdef').read_text())
if network_assembly.get('defineConstraints') != ['VELOCITY_MULTIPLAYER'] or network_assembly.get('autoReferenced') is not False:
    errors.append('Multiplayer must be opt-in and excluded from the default player assembly.')
for path in (root / 'Assets/Scripts').rglob('*.cs'):
    if 'Multiplayer' not in path.parts and re.search(r'\b(LanRaceSession|RelayTransport|LanMatchRules)\b', path.read_text()):
        errors.append(f'Single-player code still directly references multiplayer: {path}')
if re.search(r'scriptingDefineSymbols:.*VELOCITY_MULTIPLAYER', (root / 'ProjectSettings/ProjectSettings.asset').read_text()):
    errors.append('Multiplayer scripting symbol is enabled in Player Settings.')
try:
    from tree_sitter import Language, Parser
    import tree_sitter_c_sharp
    parser = Parser(Language(tree_sitter_c_sharp.language()))
    for path in root.rglob('*.cs'):
        if 'obj' in path.parts or 'bin' in path.parts: continue
        tree = parser.parse(path.read_bytes())
        parsed += 1
        stack = [tree.root_node]
        while stack:
            node = stack.pop()
            if node.type == 'ERROR' or node.is_missing:
                errors.append(f'C# syntax: {path.relative_to(root)}:{node.start_point.row + 1} {node.type}')
            stack.extend(node.children)
except ImportError:
    print('SKIP C# syntax parsing: install tree-sitter and tree-sitter-c-sharp.')

report = json.loads((root / 'Tools/Blender/Generated/validation.json').read_text())
if len(report['assets']) != 8 or not all(a['passed'] for a in report['assets']):
    errors.append('Blender report does not contain eight passing exports.')
audio_report = json.loads((root / 'Tools/Audio/validation.json').read_text())
if len(audio_report['tracks']) != 9 or not all(track['passed'] for track in audio_report['tracks']):
    errors.append('Audio report does not contain nine passing generated tracks.')
for track in audio_report['tracks']:
    path = root / 'Assets/Resources/Music' / track['playlist'] / (track['track'] + '.ogg')
    if not path.is_file() or path.read_bytes()[:4] != b'OggS' or path.stat().st_size != track['bytes']:
        errors.append(f'Missing or changed supplied soundtrack file: {path}')
if errors:
    print('\n'.join(errors), file=sys.stderr)
    raise SystemExit(1)
print(f'PASS JSON manifests, {len(guids)} unique asset GUIDs, all asset references, six vehicle prefab/stat/model sets.')
print(f'PASS C# syntax parsing: {parsed} files.' if parsed else 'C# syntax was not checked.')
print('PASS nine supplied OGG files match the audio validation report.')
print('PASS default single-player source boundaries; Unity build guard verifies the actual player assemblies.')
print('Unity import, API compilation, tests, gameplay, multiplayer, builds and performance remain separate checks.')
