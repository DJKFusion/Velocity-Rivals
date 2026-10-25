# Visual refresh — existing 0.6.0 project

## Cars and presentation

All six car FBXs were regenerated with brake calipers, flush door handles, panel
seams and rear ventilation details. Comet now has low sculpted rear buttresses,
integrated turbine outlets and an aero bridge in place of the oversized pods and
vertical tail. Runtime materials distinguish calipers, metal handles, white LED
headlights and alloy hubs. Metallic paint now retains a glossy surface instead of
using the matte finish's roughness.

The main-menu car view is taller and its lower information is arranged in two
columns. The showroom has a floor, backdrop, light strips and softbox panels, plus
a local reflection probe (64 pixels on Performance, 128 otherwise). Car framing
is closer and lower. Studio objects remain on the isolated showroom layer.

## Environments

City architecture mixes conventional towers, cylindrical towers and stepped
buildings. Desert and glacier blocks use eight cached, irregular rock meshes;
glacier formations receive snow caps and evergreen clusters. Existing placement
clearance checks still keep scenery outside the road. Geometry remains combined
by material and generated meshes are released with their world.

## Verification

- Blender exported and re-imported all six cars and two existing props, checking
  mesh/vertex counts and dimensions. All eight assets passed.
- The regenerated car contact sheet was visually inspected. It is a Blender render,
  not a screenshot or a guarantee of the same appearance in Unity:
  `Tools/Blender/Generated/vehicle_preview.png`.
- Source syntax, metadata, resource references and the twelve audio files pass
  validation. Existing Unity tests cover finite scenery geometry in every theme,
  vehicle import scale, menu scrolling and showroom cleanup.
- Unity is unavailable in this cloud environment. Shader appearance, reflection
  probe rendering, runtime asset import, camera framing and performance need a local
  Unity run and rebuilt Windows player. The build's test gates remain enabled.

No application version, race rules, circuit layout revision or saved progression
changes are made in this visual refresh.
