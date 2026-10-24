"""Render generated original car FBX files as an offline art contact sheet.

blender --background --factory-startup --python-exit-code 1 --python Tools/Blender/render_preview.py
"""
from pathlib import Path
import math
import bpy
from mathutils import Vector

root = Path(__file__).resolve().parents[2]
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
names = ["Viper", "Bulwark", "Phantom", "Apex", "Nomad", "Comet"]
for index, name in enumerate(names):
    bpy.ops.import_scene.fbx(filepath=str(root / "Assets/Resources/VehicleModels" / (name + ".fbx")))
    offset = Vector(((index % 3 - 1) * 5.2, (index // 3 - 0.5) * 6.5, 0))
    for obj in list(bpy.context.selected_objects):
        if obj.parent is None:
            obj.location += offset
    bpy.ops.object.text_add(location=(offset.x-1.0,offset.y-2.7,0.02))
    label = bpy.context.object
    label.data.body = name.upper()
    label.data.size = 0.35
    label.data.extrude = 0.003
    label.data.align_x = "LEFT"

bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,-0.015))
floor = bpy.context.object
mat = bpy.data.materials.new("ContactSheetFloor")
mat.diffuse_color = (0.025,0.035,0.055,1)
floor.data.materials.append(mat)
bpy.ops.object.camera_add(location=(12,-19,22))
camera = bpy.context.object
camera.rotation_euler = (Vector((0,0,0))-camera.location).to_track_quat("-Z","Y").to_euler()
camera.data.type = "ORTHO"
camera.data.ortho_scale = 22
bpy.context.scene.camera = camera
for position, power, size in [((0,-4,15),2600,10),((-8,5,9),1900,8),((8,7,8),2000,6)]:
    bpy.ops.object.light_add(type="AREA",location=position)
    lamp=bpy.context.object
    lamp.data.energy=power
    lamp.data.shape="DISK"
    lamp.data.size=size
    lamp.rotation_euler=(Vector((0,0,0))-lamp.location).to_track_quat("-Z","Y").to_euler()
scene=bpy.context.scene
scene.world.color=(0.15,0.15,0.15)
scene.render.engine="CYCLES"
scene.cycles.device="CPU"
scene.cycles.samples=32
scene.cycles.use_denoising=False
scene.render.resolution_x=1200
scene.render.resolution_y=900
scene.render.resolution_percentage=100
scene.render.image_settings.file_format="PNG"
scene.render.filepath=str(root / "Tools/Blender/Generated/vehicle_preview.png")
bpy.ops.render.render(write_still=True)
