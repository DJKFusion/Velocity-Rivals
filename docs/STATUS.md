# Implementation and verification ledger

The release-polish pass fixes pause restoration for stopped rivals, pauses on active
controller loss, adds a frozen three-second resume countdown and optional rumble,
validates local ghost data, and shows sorted championship standings. Source checks
and 52 standalone checks pass. New Unity interruption/replay/controller tests are
authored but unrun here; hardware feedback and Windows acceptance remain pending.
See [RELEASE-POLISH.md](RELEASE-POLISH.md).

The presentation/radio refresh adds home tiles, compact race instruments, result
reports, playlist audition and twelve replacement arrangements (~18.3 minutes).
Decoded audio and source checks pass; 52 standalone logic checks pass. New UI
integration tests are authored but not run here. Unity compilation, screenshots,
controller navigation and audible Windows playback remain pending. See
[PRESENTATION-AND-RADIO.md](PRESENTATION-AND-RADIO.md).

The racing expansion adds quick-play Elimination on five circuits, gradual
slipstream bonuses in Championship/Elimination, and traffic-aware lane selection
and corner-speed targets for circuit rivals. Existing career events remain.
52 standalone checks pass; new Unity integration tests and real race balancing
remain pending. See [RACING-EXPANSION.md](RACING-EXPANSION.md).

The career/garage expansion adds credits, early car purchases alongside existing
XP unlocks, three per-car performance upgrade paths, first-improvement medal bonuses,
transactional purchase saves and reward breakdowns. Time Attack stays stock;
other records distinguish upgrade setups. 47 standalone checks pass. Added Unity
save and UI integration tests await local execution. See
[CAREER-GARAGE-EXPANSION.md](CAREER-GARAGE-EXPANSION.md).

Latest work remains in 0.6.0: guided Driving School, live event/course briefings,
career-to-garage return navigation, corrected bronze/game-over presentation,
arena core boundary recovery, camera snapping after relocation, lap notices,
five central circuit landmarks, corner chevrons and Windows logo assignment.
43 standalone checks pass. These additions still require Unity runtime validation;
see [COMPLETION-WORK.md](COMPLETION-WORK.md) for scope and remaining release gates.

Status meanings: **verified here** means a named tool ran successfully in the cloud; **implemented / Unity untested** means source/assets exist but Editor behavior is not established; **unfinished** means the feature is absent or substantially incomplete. None of the gameplay rows below are production-complete. Version 0.6.0 targets Windows single player; networking source is archived behind a disabled assembly and is not part of the default player. See [SINGLE-PLAYER-RELEASE.md](SINGLE-PLAYER-RELEASE.md).

| Area | Status | Concrete implementation / remaining work |
| --- | --- | --- |
| Project/scene/prefab/stat references | Verified here, Unity import untested | Entry scene, six prefabs, six stats assets; static GUID reference checks pass |
| Original generated art | Verified here | Six FBX vehicles + two props round-tripped through Blender with mesh/vertex/bounds assertions; contact sheet inspected |
| Driving | Implemented / Unity untested | Acceleration, reverse, speed steering, traction, drift, boost, ray suspension, jumps, dodge, air steering, recovery; tuning and wall transitions unverified |
| Input and camera | Implemented / Unity untested | Keyboard and controller Input System bindings, follow/FOV, obstacle line test; optional haptics and active-device disconnect pause implemented; no rebinding screen |
| Race rules | Pure logic verified here; Unity untested | Ordered checkpoints, laps, finish guards, current/last/best lap timing; live map/next gate marker and wrong-way warning implemented; geometric crossing and actual race driving still need tests |
| Career tour | Logic verified here; Unity untested | Twelve events across four cups, ordered unlocks, bronze/silver/gold medals, replay, saved progress and completion ending; medal targets and full progression need playtesting |
| Championship and AI | Implemented / Unity untested | Three-round points and standings, three AI opponents, waypoint targets, corner slowing, limited avoidance/recovery, three difficulty settings; not proven to complete races reliably |
| Arena Clash | Implemented / Unity untested | Physics core, two scoring zones, four vehicles, first-to-five/time result; no dedicated carry/launch ability |
| Boost Battle | Implemented / Unity untested | Armor, impact and knockout scoring, timed respawn, shields, double damage and radial pulse pickups; pure damage/timer rules tested; Unity balancing unverified |
| Stunt Rush | Implemented / Unity untested | Rotation/airtime/upright landing scores and timer; no precision landing combo system |
| Time Attack | Implemented / Unity untested | Three-lap time and ghost recording/playback; per-track online leaderboard unfinished |
| Free Play | Implemented / Unity untested | Training, ramps and restart/menu return |
| Worlds | Implemented / Unity untested | Five authored circuit layouts, named districts, inner/outer buildings, tunnels, bridge supports, terrain formations and enclosed stadium architecture; production architecture, shortcut balance, ambience and smoothly rideable walls unfinished |
| Garage | Implemented partially / Unity untested | Saved paint/finish, wheel scale, trail colour, stripe/spoiler variants; lit rotating preview of the actual selected car with matching cosmetics; texture decal library and wheel mesh swaps unfinished |
| Menus | Implemented / Unity untested | Main, mode, garage, records, settings, soundtrack, controls guide, pause, results, credits and career tour; local events pause on focus loss; text-only animated title, no loading cinematic |
| Audio | Files verified here; playback Unity untested | Twelve original stereo electronic arrangements, menu/event shuffle playlists, crossfades, pause ducking, custom-song import menus; synthesized engine/UI/gameplay cues; production sound design/crowd ambience unfinished |
| Rendering | Implemented / Unity untested | Sculpted cars, grouped wheels, batching, URP/bloom/ACES/color grading, themed sky/clouds/stars, road textures/normals, sky reflections, headlights, skid trails and smoke; shader compilation, target hardware profiling, final art/LODs unfinished |
| Progression | Implemented / Unity untested | XP, levels, six unlockable vehicles, basic achievements, per-mode/track/car records, legacy save migration, backup profile; cloud/platform saves unfinished |
| Multiplayer | Excluded from this edition | No network component or menu in the default game; archived source is constrained by the absent VELOCITY_MULTIPLAYER define. No server, relay, account or Internet connection is required to play |
| Windows build | Build/ZIP workflow implemented / Unity untested | Double-click batch launcher + PowerShell tests/build driver, single-player assembly/shader/asset checks, versioned ZIP and SHA-256; full-folder archive helper tested against fixtures, module checks and included controls guide; no executable produced |
| 60 FPS | Unverified | 60 FPS cap is configured; no measured frame-rate, CPU/GPU budget, LOD or target-machine profiling |
| Steam/Epic/PS5/Xbox | Unfinished | Release requirements documented; no store SDKs, platform packages, devkits or certified builds |

## 0.6 changes awaiting Windows verification

The new widescreen UI, scrolling focus, race cards and save/quit flow have source checks and authored Unity tests but no Windows run here. Steam descriptors are fixture-tested; registration, upload and store review remain external steps. See UPDATE-UI-RELEASE.md and RELEASE-WEEK.md.

## 0.5 changes awaiting Windows verification

The owner subsequently confirmed that the corrected 0.4 build script succeeded and the EXE launched and played well. No passing rerun XML or performance measurements were supplied. Version 0.5 adds structured worlds, revised AI corner braking, career presentation and versioned records/ghosts; those changes require a fresh test/build and playtest. See [UPDATE-STRUCTURED-WORLDS.md](UPDATE-STRUCTURED-WORLDS.md).

## Latest supplied Unity evidence

The subsequent 25 October PlayMode XML reports 20 passed and one failed out of
21 tests. The missing UI renderer and package import exceptions are absent from
that log. Recovery incorrectly advanced the next checkpoint from 1 to 2 in
EscapedCarReturnsWithoutAdvancingCheckpointAndFailureOffersRetry. Checkpoint and
boundary rules now read Rigidbody.position through PhysicsPosition instead of
the interpolated display Transform, which can retain the pre-recovery pose.
The regression keeps the checkpoint assertion and checks several subsequent
physics frames as well. Corrected Unity execution remains pending.

The latest supplied PlayMode.log (25 October) reaches the test runner and exits
with code 2 after repeated MissingComponentException messages: the new Course
preview has no CanvasRenderer when RectMask2D performs clipping. CoursePreview
now declares that dependency and its construction explicitly includes the renderer.
The other custom graphics also declare the dependency, and menu construction adds
it explicitly. A regression test creates all custom graphics under a clipping mask
and forces canvas updates. Source checks pass; the corrected Unity run is pending.

The 24 October Windows PlayMode report ran 11 tests: one imported-vehicle check passed; ten failed on the same invalid scenery rotation. Both box helpers have been corrected to use explicit identity rotations. The updated suite adds a focused scenery regression; a later successful build/launch was reported by the owner, without a new XML report. See [TESTING.md](TESTING.md).

## Known issues and risks to resolve in Unity

1. Input System 1.11.2 failed under Unity 6000.6 because Unity's generic `TreeView` migration made its Editor code obsolete errors. The manifest now uses Input System 1.20.1, whose package metadata supports Unity 6000.0+ and whose source handles Unity 6000.2+ generic tree views. The remaining package import and compatibility checks are still pending; save the lockfile after successful resolution.
2. Physics feel, AI cornering, jumping, suspension and collision recovery need real frame-by-frame playtests. Ray adhesion is not proof of continuous wall riding or ceiling driving.
3. Imported FBX orientation/scale and tire animation need Unity review. Blender roundtrip checks cannot establish Unity importer conventions.
4. Race recovery is owned by the checkpoint session and resets its crossing sample; other events use grounded recovery. Manual recovery should be tested near checkpoint planes to ensure no accidental skips or stuck cycles.
5. Championship rivals that have not finished when the player crosses the line are ranked by checkpoint progress. This is a deliberate starter rule, not a simulation of their complete race times.
6. Ghosts use the selected car model with a cyan material, sampled at 10 Hz; no network authority is attached to them. New records and ghost files are separated by mode/environment/vehicle where applicable; legacy records remain labeled as earlier-version records. Unfinished runs cannot set a best time. New lap timing bests reset each round.
7. Arena AI is heuristic and needs balancing. The new damage/power-up combat loop is implemented but untested in Unity; see UPDATE-COMBAT.md for acceptance checks.
8. Multiplayer is excluded from the single-player build. The build guard rejects a networking define, active multiplayer assembly or stale multiplayer DLL in the output. Archived networking guidance is not a release requirement for this edition.
9. Runtime geometry and materials need profiling, batching, asset cleanup validation and art polish. Vehicle bodies share a common collider/mass; detailed per-vehicle collision and suspension tuning remains.
10. Source serialization checks do not prove that authored Unity YAML imports correctly. Test all prefabs and the bootstrap scene in the target Editor.

## Next verification gate

Import in Unity 6000.6.7f1, fix the first actual compile/import failures if any, run both Test Runner suites, complete the entire 12-event career and a three-round championship by keyboard and controller, finish each score mode, then build and test Windows. Do not mark later milestones complete until their acceptance checks pass.
