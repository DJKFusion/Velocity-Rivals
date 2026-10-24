"""Original Velocity Rivals starter art. Run with Blender, not system Python.

blender --background --factory-startup --python-exit-code 1 --python Tools/Blender/generate_assets.py
Optional arguments after --: --project-root /path/to/Velocity-Rivals
"""
import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector


def arguments():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2])
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])


def clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def material(name, color, metal=0.0, rough=0.4, glow=0.0):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Metallic"].default_value = metal
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Emission Color"].default_value = (*color, 1)
    bsdf.inputs["Emission Strength"].default_value = glow
    return mat


def finish(obj, name, mat, bevel=0):
    obj.name = name
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if bevel:
        modifier = obj.modifiers.new("Manufactured edge bevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.data.materials.append(mat)
    return obj


def box(name, position, scale, mat, bevel=0.05):
    bpy.ops.mesh.primitive_cube_add(size=1, location=position)
    obj = bpy.context.object
    obj.scale = scale
    return finish(obj, name, mat, bevel)


def wedge(name, width, length, bottom, front_height, rear_height, mat, center_y=0):
    # Front is Blender -Y; FBX -Z-forward/Y-up export presents forward as Unity +Z.
    w, l = width / 2, length / 2
    vertices = [(-w,-l,bottom),(w,-l,bottom),(w,l,bottom),(-w,l,bottom),
                (-w,-l,front_height),(w,-l,front_height),(w,l,rear_height),(-w,l,rear_height)]
    faces = [(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)]
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location.y = center_y
    obj.select_set(True)
    return finish(obj, name, mat, 0.07)


def cylinder(name, position, radius, depth, mat, axis="X", vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=position)
    obj = bpy.context.object
    if axis == "X":
        obj.rotation_euler.y = math.pi / 2
    elif axis == "Y":
        obj.rotation_euler.x = math.pi / 2
    return finish(obj, name, mat, 0.025)


def wheel(x, y, radius, rubber, alloy, trim):
    side = "L" if x < 0 else "R"
    end = "Front" if y < 0 else "Rear"
    bpy.ops.mesh.primitive_torus_add(major_segments=40, minor_segments=12,
        location=(x,y,radius), rotation=(0,math.pi/2,0), major_radius=radius*.76, minor_radius=radius*.24)
    tire = finish(bpy.context.object, "Wheel_"+side+end, rubber)
    for polygon in tire.data.polygons: polygon.use_smooth = True
    outer_x = x + (0.155 if x > 0 else -0.155)
    cylinder("BrakeDisc_"+side+end, (x,y,radius), radius*.60,.06,alloy, vertices=32)
    bpy.ops.mesh.primitive_torus_add(major_segments=32, minor_segments=8,
        location=(outer_x,y,radius), rotation=(0,math.pi/2,0), major_radius=radius*.65, minor_radius=.025)
    rim = finish(bpy.context.object,"WheelRim_"+side+end,alloy)
    for polygon in rim.data.polygons: polygon.use_smooth=True
    for i in range(6):
        angle=i*math.pi/3
        spoke=box("WheelSpoke_"+side+end,(outer_x,y+math.sin(angle)*radius*.31,radius+math.cos(angle)*radius*.31),(.045,.045,radius*.64),alloy,.012)
        spoke.rotation_euler.x=-angle
    cylinder("WheelHub_" + side + end, (outer_x,y,radius), radius * 0.17, 0.055, trim, vertices=24)


def loft(name, rings, bottom, mat, smooth=2):
    """A continuous sculpted shell from longitudinal cross-sections (Y, half-width, height)."""
    vertices=[]
    for y,w,z in rings:
        vertices.extend([(x,y,h) for x,h in [(-w*.78,bottom),(-w,bottom+.10),(-w,z-.10),
            (-w*.72,z),(w*.72,z),(w,z-.10),(w,bottom+.10),(w*.78,bottom)]])
    faces=[]
    for r in range(len(rings)-1):
        for j in range(8): faces.append((r*8+j,r*8+(j+1)%8,(r+1)*8+(j+1)%8,(r+1)*8+j))
    faces.extend([tuple(reversed(range(8))),tuple((len(rings)-1)*8+j for j in range(8))])
    mesh=bpy.data.meshes.new(name+"Mesh");mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(mesh);bm.free()
    mesh.materials.append(mat)
    if smooth:
        modifier=obj.modifiers.new("Sculpted body surfaces","SUBSURF");modifier.levels=smooth
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    for polygon in mesh.polygons: polygon.use_smooth=True
    for polygon in obj.data.polygons: polygon.use_smooth=True
    return obj


def wheel_arch(body,x,y,r):
    bpy.ops.mesh.primitive_cylinder_add(vertices=40,radius=r+0.045,depth=.60,
        location=(x,y,r),rotation=(0,math.pi/2,0))
    cutter=bpy.context.object
    bpy.context.view_layer.objects.active=body
    modifier=body.modifiers.new("Wheel clearance","BOOLEAN");modifier.operation='DIFFERENCE';modifier.object=cutter
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.data.objects.remove(cutter,do_unlink=True)


def car(name, kind, color):
    paint = material(name + "_BodyPaint", color, 0.65, 0.28)
    rubber = material("Rubber", (0.018,0.023,0.035), 0, 0.8)
    alloy = material("Alloy", (0.28,0.34,0.43), 0.9, 0.23)
    glass = material("GlassOpaque", (0.035,0.11,0.17), 0.65, 0.16)
    trim = material(name + "_Neon", (0.06,0.9,1.0), 0.2, 0.25, 4)
    exhaust = material("Exhaust", (0.025,0.03,0.045), 0.5, 0.6)
    widths = [1.76,1.95,1.52,1.85,1.88,1.65]
    lengths = [3.65,3.75,3.35,3.95,3.7,4.0]
    radii = [0.34,0.43,0.30,0.34,0.42,0.33]
    w,l,r = widths[kind],lengths[kind],radii[kind]
    body=loft("Body_"+name,[(y*l,w*.5*width,r+height) for y,width,height in [
        (-.50,.74,.12),(-.47,.92,.19),(-.34,1,.38),(-.17,.91,.32),
        (.04,.87,.31),(.26,1,.41),(.43,.98,.32),(.49,.81,.24)]],r*.55,paint)
    for x in [-w*.49,w*.49]:
        for y in [-l*.31,l*.31]: wheel_arch(body,x,y,r)
    box("Body_Undertray",(0,0,r*0.7),(w*0.88,l*0.88,0.14),alloy)
    roof_w = w * (0.45 if kind == 2 else 0.60)
    roof_l = l * (0.24 if kind == 5 else 0.34)
    loft("Glass_Cockpit",[(y*l,roof_w*.5*width,r+height) for y,width,height in [
        (-.24,1,.40),(-.15,.9,.70),(-.05,.8,.83),(.10,.78,.84),(.23,.96,.48)]],r+.28,glass)
    loft("Body_Roof",[(-.045*l,roof_w*.38,r+.86),(.03*l,roof_w*.39,r+.89),(.10*l,roof_w*.37,r+.86)],r+.78,paint,1)
    for side in [-1,1]:
        box("Carbon_SideSkirt",(side*w*.48,0,r*.55),(.10,l*.5,.10),exhaust,.025)
        mirror=box("Body_Mirror",(side*w*.53,-l*.12,r+.63),(.22,.24,.10),paint,.04)
        box("Carbon_MirrorMount",(side*w*.46,-l*.12,r+.55),(.24,.07,.12),exhaust,.02)
        box("Glass_Mirror",(side*w*.54,-l*.09,r+.64),(.18,.025,.07),glass,.01)
        for vent in range(4):
            box("Carbon_HoodVent",(side*w*.27,-l*.31+vent*.06,r+.355),(.16,.025,.018),exhaust,.004)
    box("Carbon_FrontSplitter",(0,-l*.46,r*.52),(w*.89,.25,.07),exhaust,.025)
    for side in [-1,1]:
        box("Carbon_FrontIntake",(side*w*.27,-l*.485,r+.02),(.42,.06,.12),exhaust,.04)
        box("Tail_RearLamp",(side*w*.30,l*.478,r+.18),(.42,.04,.05),material("RearLamp",(1,.015,.03),0,.2,4),.015)
    for x in [-w*0.49,w*0.49]:
        wheel(x,-l*0.31,r,rubber,alloy,trim)
        wheel(x,l*0.31,r,rubber,alloy,trim)
        box("Trim_Sill",(x,0,r*0.97),(0.055,l*0.60,0.05),trim,0.015)
    for x in [-w*0.31,w*0.31]:
        box("Trim_Headlight",(x,-l*0.5-0.01,r+0.07),(w*0.22,0.035,0.06),trim,0.015)
        cylinder("Exhaust_Nozzle",(x,l*0.5+0.04,r+0.12),0.12,0.18,exhaust,"Y")
    if kind == 0: # Viper: split front nose, twin wing pylons, broad spoiler.
        for x in [-0.5,0.5]:
            box("Body_WingPylon",(x,l*0.38,r+0.57),(0.07,0.12,0.38),alloy)
        box("Body_Spoiler",(0,l*0.39,r+0.78),(w*1.05,0.27,0.10),paint)
        box("Trim_HoodStripe",(0,-l*0.28,r+0.32),(0.10,l*0.31,0.035),trim,0.01)
    elif kind == 1: # Bulwark: armored cabin, impact bars, exposed armored wheel pods.
        for x in [-w*0.51,w*0.51]:
            for y in [-l*0.31,l*0.31]:
                box("Body_ArmorPod",(x,y,r*1.8),(0.42,0.95,0.18),paint)
        for y in [-l*0.5,l*0.5]:
            box("Body_ImpactBar",(0,y,r*0.95),(w*0.94,0.20,0.20),alloy)
        box("Body_RoofArmor",(0,0.15,r+0.87),(1.12,0.80,0.12),paint)
    elif kind == 2: # Phantom: lightweight open-wheel dart with central canopy.
        for x in [-w*0.60,w*0.60]:
            box("Body_AeroFin",(x,0.78,r+0.35),(0.06,0.72,0.56),paint)
        box("Body_FrontWing",(0,-l*0.48,r*0.7),(w*1.1,0.30,0.08),paint)
    elif kind == 3: # Apex: wide hypercar, long low rear deck and diffuse fins.
        for x in [-0.62,-0.31,0,0.31,0.62]:
            box("Body_Diffuser",(x,l*0.45,r*0.65),(0.045,0.38,0.24),alloy,0.01)
        for x in [-w*0.43,w*0.43]:
            box("Glass_SideIntake",(x,0.32,r+0.22),(0.12,0.75,0.17),glass)
        box("Trim_RearLightBar",(0,l*0.5,r+0.28),(w*0.87,0.04,0.045),trim,0.01)
    elif kind == 4: # Nomad: rally muscle silhouette and raised intake.
        box("Body_HoodIntake",(0,-0.78,r+0.48),(0.57,0.65,0.25),alloy)
        for x in [-0.4,0,0.4]:
            cylinder("Trim_RallyLight",(x,-l*0.47,r+0.43),0.095,0.06,trim,"Y")
        box("Body_RearRollBar",(0,0.65,r+0.94),(1.30,0.10,0.10),alloy)
    else: # Comet: paired long rocket nacelles and single vertical tail.
        for x in [-0.56,0.56]:
            cylinder("Body_RocketPod",(x,0.75,r+0.55),0.23,1.55,paint,"Y")
            cylinder("Trim_RocketMouth",(x,1.56,r+0.55),0.15,0.06,trim,"Y")
        box("Body_TailFin",(0,1.25,r+0.88),(0.075,0.65,0.75),paint)
    bpy.ops.object.select_all(action="SELECT")


def counts():
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    corners = [o.matrix_world @ Vector(v) for o in meshes for v in o.bound_box]
    return {"meshes":len(meshes),"vertices":sum(len(o.data.vertices) for o in meshes),
            "polygons":sum(len(o.data.polygons) for o in meshes),
            "dimensions":[round(max(p[i] for p in corners)-min(p[i] for p in corners),5) for i in range(3)]}


def export_validate(path):
    bpy.context.view_layer.update()
    before = counts()
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={"MESH"},
        axis_forward="-Z", axis_up="Y", global_scale=1.0, apply_unit_scale=True,
        bake_anim=False, add_leaf_bones=False, use_mesh_modifiers=True)
    clear()
    bpy.ops.import_scene.fbx(filepath=str(path), use_anim=False)
    bpy.context.view_layer.update()
    after = counts()
    assert before["meshes"] == after["meshes"], (path,before,after)
    assert before["vertices"] == after["vertices"], (path,before,after)
    assert all(abs(a-b)<0.001 for a,b in zip(before["dimensions"],after["dimensions"])), (path,before,after)
    return {"file":path.name,"source":before,"roundtrip":after,"bytes":path.stat().st_size,"passed":True}


def prop(name):
    alloy = material("PropAlloy",(0.035,0.055,0.095),0.75,0.35)
    glow = material("PropNeon",(0.05,0.75,1),0.1,0.25,6)
    if name == "CheckpointGate":
        for x in [-6,6]:
            box("Gate_Pillar",(x,0,2.5),(0.55,0.65,5),alloy)
            box("Gate_Neon",(x,-0.34,2.5),(0.15,0.04,4.5),glow,0.01)
        box("Gate_Header",(0,0,5),(12.5,0.65,0.55),alloy)
        box("Gate_Light",(0,-0.34,5),(11.8,0.04,0.15),glow,0.01)
    else:
        for x in [-4,4]:
            box("Goal_Pillar",(x,0,2),(0.5,0.5,4),alloy)
            box("Goal_Neon",(x,-0.26,2),(0.18,0.04,3.8),glow,0.01)
        box("Goal_Top",(0,0,4),(8.5,0.5,0.5),alloy)
        box("Goal_Light",(0,-0.26,4),(8.3,0.04,0.16),glow,0.01)
    bpy.ops.object.select_all(action="SELECT")


def main():
    root = arguments().project_root.resolve()
    cars = root / "Assets/Resources/VehicleModels"
    props = root / "Assets/Models/Generated"
    report = root / "Tools/Blender/Generated"
    for folder in [cars,props,report]: folder.mkdir(parents=True,exist_ok=True)
    bpy.context.scene.unit_settings.system="METRIC"
    bpy.context.scene.unit_settings.scale_length=1
    models=[("Viper",(0.04,0.45,0.70)),("Bulwark",(0.75,0.16,0.055)),
            ("Phantom",(0.42,0.08,0.68)),("Apex",(0.72,0.76,0.86)),
            ("Nomad",(0.60,0.48,0.10)),("Comet",(0.05,0.61,0.35))]
    results=[]
    for index,(name,color) in enumerate(models):
        clear()
        car(name,index,color)
        results.append(export_validate(cars / (name+".fbx")))
    for name in ["CheckpointGate","EnergyGoal"]:
        clear()
        prop(name)
        results.append(export_validate(props / (name+".fbx")))
    data={"generator":"Velocity Rivals original procedural starter art","blender":bpy.app.version_string,
          "units":"meters","source_forward":"-Y","export_forward":"-Z","export_up":"Y","assets":results}
    (report / "validation.json").write_text(json.dumps(data,indent=2)+"\n",encoding="utf-8")
    print("PASS: generated and round-trip validated",len(results),"FBX assets;",sum(r["bytes"] for r in results),"bytes")


if __name__ == "__main__":
    main()
