"""Create missing serializable vehicle stats and usable runtime-built vehicle prefabs.

Existing assets are preserved. Unity's ProjectBuilder can subsequently bake visual
hierarchies and persistent materials into prefabs from inside the actual Editor.
"""
from pathlib import Path
import uuid

root = Path(__file__).resolve().parents[1]
def guid(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'velocity-rivals/' + path).hex

def create(path, content):
    target = root / path
    target.parent.mkdir(parents=True, exist_ok=True)
    if not target.exists():
        target.write_text(content)

header = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'
vehicles = [
    ('Viper', 40, 25, 95, 8, 24), ('Bulwark', 34, 22, 75, 7, 24),
    ('Phantom', 43, 29, 115, 10, 24), ('Apex', 46, 27, 92, 9, 24),
    ('Nomad', 38, 26, 82, 8, 24), ('Comet', 42, 24, 105, 6, 34)
]
for name, speed, acceleration, steering, grip, boost in vehicles:
    stats_path = f'Assets/Resources/VehicleStats/{name}.asset'
    create(stats_path, header + f'''--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid('Assets/Scripts/Vehicles/VehicleStats.cs')}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier:
  DisplayName: {name}
  MaxSpeed: {speed}
  Acceleration: {acceleration}
  Steering: {steering}
  Grip: {grip}
  BoostForce: {boost}
''')
    create(f'Assets/Prefabs/{name}.prefab', header + f'''--- !u!1 &1000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 1001}}
  - component: {{fileID: 1002}}
  - component: {{fileID: 1003}}
  - component: {{fileID: 1004}}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_IsActive: 1
--- !u!4 &1001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_Children: []
  m_Father: {{fileID: 0}}
--- !u!54 &1002
Rigidbody:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1000}}
  serializedVersion: 5
  m_Mass: 1100
  m_LinearDamping: 0.12
  m_AngularDamping: 2
  m_CenterOfMass: {{x: 0, y: 0.18, z: 0}}
  m_InertiaTensor: {{x: 1, y: 1, z: 1}}
  m_InertiaRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_UseGravity: 1
  m_IsKinematic: 0
  m_Interpolate: 1
  m_Constraints: 0
  m_CollisionDetection: 2
--- !u!65 &1003
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1000}}
  m_Material: {{fileID: 0}}
  m_IsTrigger: 0
  m_Enabled: 1
  serializedVersion: 3
  m_Size: {{x: 1.8, y: 0.65, z: 3.4}}
  m_Center: {{x: 0, y: 0.55, z: 0}}
--- !u!114 &1004
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 1000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid('Assets/Scripts/Vehicles/VehicleController.cs')}, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
  Stats: {{fileID: 11400000, guid: {guid(stats_path)}, type: 2}}
  IsPlayer: 1
  ControlsEnabled: 1
''')
print('Six vehicle stat assets and prefabs created; existing files preserved.')
