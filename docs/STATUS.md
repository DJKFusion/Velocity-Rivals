# Implementation and verification ledger

Status meanings: **verified here** means a named tool ran successfully in the cloud; **implemented / Unity untested** means source/assets exist but Editor behavior is not established; **unfinished** means the feature is absent or substantially incomplete. None of the gameplay rows below are production-complete.

| Area | Status | Concrete implementation / remaining work |
| --- | --- | --- |
| Project/scene/prefab/stat references | Verified here, Unity import untested | Entry scene, six prefabs, six stats assets; static GUID reference checks pass |
| Original generated art | Verified here | Six FBX vehicles + two props round-tripped through Blender with mesh/vertex/bounds assertions; contact sheet inspected |
| Driving | Implemented / Unity untested | Acceleration, reverse, speed steering, traction, drift, boost, ray suspension, jumps, dodge, air steering, recovery; tuning and wall transitions unverified |
| Input and camera | Implemented / Unity untested | Keyboard and controller Input System bindings, follow/FOV, obstacle line test; no rebinding screen or haptics |
| Race rules | Pure logic verified here; Unity untested | Ordered checkpoints, laps, finish guards; geometric crossing and actual race driving still need tests |
| Championship and AI | Implemented / Unity untested | Three-round points and standings, three AI opponents, waypoint targets, corner slowing, limited avoidance/recovery, three difficulty settings; not proven to complete races reliably |
| Arena Clash | Implemented / Unity untested | Physics core, two scoring zones, four vehicles, first-to-five/time result; no dedicated carry/launch ability |
| Boost Battle | Implemented / Unity untested | Armor, impact and knockout scoring, timed respawn, shields, double damage and radial pulse pickups; pure damage/timer rules tested; Unity balancing unverified |
| Stunt Rush | Implemented / Unity untested | Rotation/airtime/upright landing scores and timer; no precision landing combo system |
| Time Attack | Implemented / Unity untested | Three-lap time and ghost recording/playback; per-track online leaderboard unfinished |
| Free Play | Implemented / Unity untested | Training, ramps and restart/menu return |
| Worlds | Implemented / Unity untested | Five themes/circuit variants, road markings, lit towers/natural formations, spectator tiers and roof ribs; production architecture, shortcut balance, ambience and smoothly rideable walls unfinished |
| Garage | Implemented partially / Unity untested | Saved paint/finish, wheel scale, trail colour, stripe/spoiler variants; rotating paint/finish preview; texture decal library and wheel mesh swaps unfinished |
| Menus | Implemented / Unity untested | Main, mode, garage, records, settings, pause, results, credits and LAN entry; text-only animated title, no loading cinematic; IP typing requires keyboard |
| Audio | Implemented placeholders / Unity untested | Original engine/music/UI, countdown, goal, jump, drift, boost, impact, pickup and knockout cues; production sound design/crowd ambience unfinished |
| Rendering | Implemented partially / Unity untested | Sculpted cars, surface materials, grouped wheel animation, mesh batching, URP/bloom/ACES, soft shadows, reflection probe, boost trail and impact/pickup particles; production textures/LODs, smoke and skid marks unfinished |
| Progression | Implemented / Unity untested | XP, levels, six unlockable vehicles, basic achievements, local records, backup profile; cloud/platform saves unfinished |
| LAN multiplayer | Protocol framing verified here; session Unity untested | Two-player direct-IP host-authoritative sprint; gameplay latency/recovery, scoring agreement and disconnect tests unrun |
| Production multiplayer | Unfinished | Authenticated lobbies, matchmaking, dedicated servers, prediction/lag compensation, team selection and online modes |
| Windows build | Script implemented / not executed | Supported BuildPipeline call + PowerShell tests/build driver; no executable produced |
| 60 FPS | Unverified | 60 FPS cap is configured; no measured frame-rate, CPU/GPU budget, LOD or target-machine profiling |
| Steam/Epic/PS5/Xbox | Unfinished | Release requirements documented; no store SDKs, platform packages, devkits or certified builds |

## Known issues and risks to resolve in Unity

1. Input System 1.11.2 failed under Unity 6000.6 because Unity's generic `TreeView` migration made its Editor code obsolete errors. The manifest now uses Input System 1.20.1, whose package metadata supports Unity 6000.0+ and whose source handles Unity 6000.2+ generic tree views. The remaining package import and compatibility checks are still pending; save the lockfile after successful resolution.
2. Physics feel, AI cornering, jumping, suspension and collision recovery need real frame-by-frame playtests. Ray adhesion is not proof of continuous wall riding or ceiling driving.
3. Imported FBX orientation/scale and tire animation need Unity review. Blender roundtrip checks cannot establish Unity importer conventions.
4. Auto-recovery returns to a recent safe ground location. Manual recovery should be tested near checkpoint planes to ensure no accidental skips or stuck cycles.
5. Championship rivals that have not finished when the player crosses the line are ranked by checkpoint progress. This is a deliberate starter rule, not a simulation of their complete race times.
6. Ghosts are coarse visual boxes sampled at 10 Hz; no network authority is attached to them. Records are currently grouped by mode, while ghost files are per environment and vehicle.
7. Arena AI is heuristic and needs balancing. The new damage/power-up combat loop is implemented but untested in Unity; see UPDATE-COMBAT.md for acceptance checks.
8. LAN uses TCP and host authority without client prediction. Guest response may lag; host cheating and untrusted Internet peers are outside its scope. It has no service credentials or connection relay.
9. Runtime geometry and materials need profiling, batching, asset cleanup validation and art polish. Vehicle bodies share a common collider/mass; detailed per-vehicle collision and suspension tuning remains.
10. Source serialization checks do not prove that authored Unity YAML imports correctly. Test all prefabs and the bootstrap scene in the target Editor.

## Next verification gate

Import in Unity 6000.6.7f1, fix the first actual compile/import failures if any, run both Test Runner suites, complete a three-round championship by keyboard and controller, finish each score mode, then build and test Windows. Do not mark later milestones complete until their acceptance checks pass.
