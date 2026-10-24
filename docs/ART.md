# Original starter art

`Tools/Blender/generate_assets.py` creates six original stylized vehicle meshes and two environment props. These are usable procedural starter assets, not final production art. They use no third-party geometry, textures, or sounds.

| Vehicle | Design |
| --- | --- |
| Viper | Sports coupe, twin wing pylons and split lighting |
| Bulwark | Heavy armored body, protected wheel pods and impact bars |
| Phantom | Narrow lightweight cockpit, exposed wheels and aero fins |
| Apex | Wide hypercar body, side intakes and rear diffuser |
| Nomad | Rally muscle body, raised intake and auxiliary lights |
| Comet | Experimental twin rocket pods and vertical tail |

Car files are in `Assets/Resources/VehicleModels`. Load a model with `Resources.Load<GameObject>("VehicleModels/Viper")`. Models contain separate body, glass, wheel, hub, trim, and exhaust meshes, named for material assignment. The models have no physics colliders; use the game's vehicle physics root. Tires touch the local ground plane. One unit is one meter. Cars face Blender -Y, exported with -Z forward and Y up for Unity. Verify the final Unity prefab orientation before shipping; Blender round-trip verification is not a Unity import test.

Generated checkpoint and goal props are in `Assets/Models/Generated`. Their colliders and scoring/checkpoint triggers must be attached by scene generation or gameplay code; the FBX meshes provide visual geometry only.

Run from the project folder:

```sh
blender --background --factory-startup --python-exit-code 1 --python Tools/Blender/generate_assets.py
```

An optional project location may be supplied after `-- --project-root /path/to/Velocity-Rivals`. The command replaces its eight generated FBX files and its validation report. Do not put hand-edited art at those generated paths.

Each export is imported back into Blender. The script asserts that mesh and vertex counts match and bounds match to within 0.001 meters. `Tools/Blender/Generated/validation.json` records actual measurements from that run. No external textures are needed. Blender material properties may require conversion to URP materials in Unity, especially emission and bloom. Runtime vehicle paint should be assigned to `Body*` meshes, and cyan emissive materials to `Trim*` meshes.

All eight generated files passed these round-trip checks in Blender 4.3.2. Combined FBX size is 3,333,120 bytes after the sculpted-car update. A contact sheet, rendered from the exported FBX files and visually inspected, is included at `Tools/Blender/Generated/vehicle_preview.png`. This is an offline Blender render, not an in-game screenshot. Regenerate it with:

```sh
blender --background --factory-startup --threads 4 --python-exit-code 1 --python Tools/Blender/render_preview.py
```

The preview uses CPU Cycles with 32 samples and no denoiser, so it also works with Blender builds that omit OpenImageDenoise.

The revised cars use subdivided shells, cut wheel arches, rounded tires, spoke wheels, mirrors, vents, glass canopies and rear lamps. Runtime materials distinguish paint, glass, alloy, rubber/carbon and emissive lighting. Wheel meshes are grouped by axle before rotation, and readable body/wheel parts are combined by material. The Editor model importer enables readable meshes for this purpose; CPU mesh memory and draw-call savings need profiling.

Remaining art work includes suspension travel animation, production UV maps, texture detail, LOD meshes, collision mesh optimization, custom decals, final exhaust/VFX, and Unity visual review. No frame-rate target has been validated from these assets alone.
