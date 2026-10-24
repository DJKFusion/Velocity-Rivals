"""Create deterministic Unity metadata for new files, preserving all existing GUIDs."""
from pathlib import Path
import uuid

root = Path(__file__).resolve().parents[1]
assets = root / 'Assets'
for path in sorted(assets.rglob('*')):
    if path.name.endswith('.meta') or path.name.startswith('.'):
        continue
    meta = Path(str(path) + '.meta')
    if meta.exists():
        continue
    guid = uuid.uuid5(uuid.NAMESPACE_URL, 'velocity-rivals/' + path.relative_to(root).as_posix()).hex
    prefix = f'fileFormatVersion: 2\nguid: {guid}\n'
    if path.is_dir():
        body = 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
    elif path.suffix == '.cs':
        body = 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n'
    elif path.suffix == '.asmdef':
        body = 'AssemblyDefinitionImporter:\n  externalObjects: {}\n'
    elif path.suffix == '.asset':
        body = 'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n'
    elif path.suffix == '.prefab':
        body = 'PrefabImporter:\n  externalObjects: {}\n'
    elif path.suffix.lower() == '.fbx':
        # Leave importer settings at Editor defaults; preserve a stable identity.
        body = 'ModelImporter:\n  serializedVersion: 22200\n  internalIDToNameTable: []\n  externalObjects: {}\n  materials:\n    materialImportMode: 0\n'
    elif path.suffix.lower() in ('.ogg', '.wav', '.mp3'):
        body = ('AudioImporter:\n  externalObjects: {}\n  serializedVersion: 7\n'
                '  defaultSettings:\n    serializedVersion: 2\n    loadType: 2\n'
                '    sampleRateSetting: 0\n    sampleRateOverride: 44100\n'
                '    compressionFormat: 1\n    quality: 0.8\n    conversionMode: 0\n'
                '  forceToMono: 0\n  normalize: 1\n  preloadAudioData: 0\n'
                '  loadInBackground: 1\n  ambisonic: 0\n')
    else:
        body = 'DefaultImporter:\n  externalObjects: {}\n'
    meta.write_text(prefix + body + '  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
print('Metadata generated without changing existing GUIDs.')
