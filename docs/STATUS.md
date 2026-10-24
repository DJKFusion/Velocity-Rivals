# Implementation and verification ledger

Status meanings: **verified here** means a named tool ran successfully in the cloud; **implemented / Unity untested** means source/assets exist but Editor behavior is not established; **unfinished** means the feature is absent or substantially incomplete. None of the gameplay rows below are production-complete.

| Area | Status | Concrete implementation / remaining work |
| --- | --- | --- |
| Project/scene/prefab/stat references | Verified here, Unity import untested | Entry scene, six prefabs, six stats assets; static GUID reference checks pass |
| Original generated art | Verified here | Six FBX vehicles + two props round-tripped through Blender with mesh/vertex/bounds assertions; contact sheet inspected |
| Driving | Implemented / Unity untested | Acceleration, reverse, speed steering, traction, drift, boost, ray suspension, jumps, dodge, air steering, recovery; tuning and wall transitions unverified |
| Input and camera | Implemented / Unity untested | Keyboard and controller Input System bindings, follow/FOV, obstacle line test; no rebinding screen or haptics |
| Race rules | Pure logic verified here; Unity untested | Ordered checkpoints, laps, finish guards; geometric crossing and actual race driving still need tests |
| Championship and AI | Implemented / Unity untested | Three-round points and standings, three AI opponents, waypoint targets, corner slowing, limited avoidance/recovery; not proven to complete races reliably; no difficulty selector |
| Arena Clash | Implemented / Unity untested | Physics core, two scoring zones, four vehicles, first-to-five/time result; no dedicated carry/launch ability |
| Boost Battle | Implemented partially / Unity untested | Collision/knockback scoring and results; original power-ups, health/damage visuals unfinished |
| Stunt Rush | Implemented / Unity untested | Rotation/airtime/upright landing scores and timer; no precision landing combo system |
| Time Attack | Implemented / Unity untested | Three-lap time and ghost recording/playback; per-track online leaderboard unfinished |
| Free Play | Implemented / Unity untested | Training, ramps and restart/menu return |
| Worlds | Implemented starter geometry / Unity untested | Five themes and circuit variants; production architecture, authored shortcut balance, spectator ambience and smoothly rideable walls unfinished |
| Garage | Implemented partially / Unity untested | Saved paint/finish, wheel scale, trail colour, stripe/spoiler variants; no showroom preview, texture decal library or wheel mesh swaps |
| Menus | Implemented / Unity untested | Main, mode, garage, records, settings, pause, results, credits and LAN entry; text-only animated title, no loading cinematic; IP typing requires keyboard |
| Audio | Implemented placeholders / Unity untested | Original synthesized engine, looped music, UI and finish cues; distinct tire/boost/jump/collision/crowd sounds unfinished |
| Rendering | Implemented partially / Unity untested | URP setup, emissive materials, bloom, ACES and boost trail; smoke, sparks, skid marks and polished environmental effects unfinished |
| Progression | Implemented / Unity untested | XP, levels, six unlockable vehicles, basic achievements, local records, backup profile; cloud/platform saves unfinished |
| LAN multiplayer | Protocol framing verified here; session Unity untested | Two-player direct-IP host-authoritative sprint; gameplay latency/recovery, scoring agreement and disconnect tests unrun |
| Production multiplayer | Unfinished | Authenticated lobbies, matchmaking, dedicated servers, prediction/lag compensation, team selection and online modes |
| Windows build | Script implemented / not executed | Supported BuildPipeline call + PowerShell tests/build driver; no executable produced |
| 60 FPS | Unverified | 60 FPS cap is configured; no measured frame-rate, CPU/GPU budget, LOD or target-machine profiling |
| Steam/Epic/PS5/Xbox | Unfinished | Release requirements documented; no store SDKs, platform packages, devkits or certified builds |

## Known issues and risks to resolve in Unity

1. First Editor import and package compatibility are unverified. The manifest uses explicit package versions; resolve any actual Editor-reported incompatibility before upgrading packages, and save the resulting lockfile.
2. Physics feel, AI cornering, jumping, suspension and collision recovery need real frame-by-frame playtests. Ray adhesion is not proof of continuous wall riding or ceiling driving.
3. Imported FBX orientation/scale and tire animation need Unity review. Blender roundtrip checks cannot establish Unity importer conventions.
4. Auto-recovery returns to a recent safe ground location. Manual recovery should be tested near checkpoint planes to ensure no accidental skips or stuck cycles.
5. Championship rivals that have not finished when the player crosses the line are ranked by checkpoint progress. This is a deliberate starter rule, not a simulation of their complete race times.
6. Ghosts are coarse visual boxes sampled at 10 Hz; no network authority is attached to them. Records are currently grouped by mode, while ghost files are per environment and vehicle.
7. Arena AI is heuristic and needs balancing. Boost Battle awards collision points, not a complete damage/power-up combat system.
8. LAN uses TCP and host authority without client prediction. Guest response may lag; host cheating and untrusted Internet peers are outside its scope. It has no service credentials or connection relay.
9. Runtime geometry and materials need profiling, batching, asset cleanup validation and art polish. Vehicle bodies share a common collider/mass; detailed per-vehicle collision and suspension tuning remains.
10. Source serialization checks do not prove that authored Unity YAML imports correctly. Test all prefabs and the bootstrap scene in the target Editor.

## Next verification gate

Import in Unity 6000.6.7f1, fix the first actual compile/import failures if any, run both Test Runner suites, complete a three-round championship by keyboard and controller, finish each score mode, then build and test Windows. Do not mark later milestones complete until their acceptance checks pass.
