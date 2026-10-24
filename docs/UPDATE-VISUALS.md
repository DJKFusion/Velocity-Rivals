# Cars and environment visual update

This update continues the existing project and all six gameplay modes. It is not a finished release or proof of in-game visual quality.

## Implemented

- Regenerated all six original cars with subdivided, sculpted shells, cut wheel arches, rounded tires, multi-spoke wheels, brake discs, mirrors/mounts, vents, splitters, curved glass and red rear lamps. Existing vehicle identities, resource paths and GUIDs remain stable.
- Added distinct paint, glass, alloy, rubber/carbon and light materials to runtime cars and garage preview. Corrected wheel rotation to turn a whole wheel assembly around its axle.
- Added optional runtime car mesh combining by pivot and material, plus an Editor importer that retains readable meshes. Combining skips unreadable meshes safely; removed source renderers cannot reappear during knockout/respawn.
- Added road centre dashes, edge lines, alternating curb stripes, checkpoint beams and a chequered finish line.
- Added city glass towers with lit windows, desert/glacier formations and arena spectator tiers, structural pillars, arched overhead ribs and floor markings. Decoration has no gameplay colliders. Scenery is combined into at most six material groups per environment.
- Added soft sunlight shadows, themed light colour, ambient sky/equator/ground light and one time-sliced, 128-pixel environment reflection probe per match. No live reflection capture is needed every frame.
- Added a Unity PlayMode import test for car dimensions, forward-axis scale and readable meshes.

## Verification

Blender 4.3.2 generated all eight FBX assets and successfully reimported them. Mesh counts, vertex counts and dimensions matched the source assertions. The final total FBX size is 3,333,120 bytes. The refreshed contact sheet was rendered from those exports and visually inspected. It is an offline Blender render, not a Unity screenshot.

Static C# parsing, manifest validation, metadata uniqueness and Unity asset-reference checks passed. The prior fourteen shared gameplay/protocol logic tests are unaffected by this visual-only pass. Unity API compilation, the new model import test, garage appearance, wheel axes, car batching, combat respawn visibility and scenery rendering have not been executed in this cloud environment.

## Required Unity checks

1. Open the updated project in Unity 6000.6.7f1, let the FBX files import, then select Velocity Rivals → Play Game.
2. Inspect Garage paint/finish and drive in Time Attack on Neon City Circuit. Check the tires spin together, cars face forward, paint/glass are distinct and the track has lane/curb/finish markings.
3. Try the other environment themes and Arena Clash/Boost Battle. Verify scenery never blocks racing/arena movement and roof ribs leave aerial clearance.
4. Take a knockout and respawn; each car should appear once, without duplicate wheel/body surfaces. Restart repeatedly and check memory stabilizes.
5. Run Unity Test Runner, including ImportedVehiclesHaveReadableGeometryAndMeterScale, then profile a standalone build. New geometry and reflection probes add cost; a 60 FPS target has not been established.

Release work still includes Unity playtesting/bug fixing, production textures and LODs, full gameplay balance, final audio/UI, public multiplayer services, platform integrations and a verified Windows executable. The user has demonstrated Editor Play Mode on an empty scene; the game itself has not yet been verified through a supplied gameplay capture or test report.
