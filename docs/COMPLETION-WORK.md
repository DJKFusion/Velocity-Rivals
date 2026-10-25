# Single-player completion work

This work stays in the existing 0.6.0 project. It does not certify a finished release.

## Integrated flow

- Main menu offers Driving School alongside Career Tour and quick play.
- Driving School verifies maintained speed, boosting, an airborne interval followed
  by an upright landing, and manual recovery. It saves completion and offers a direct
  route into the career. A failed landing must be repeated. Training does not award
  race records or farm event XP.
- Event selection and career pages show a course preview, starting position and
  selected vehicle. Quick-play rules update when changing modes; course and car
  previews update with the selection. Entering the garage from career returns there.
- Career bronze completion displays event completion rather than a contradictory
  game-over heading. Failed quick-play events retain retry. Championships retain
  next-round flow, and the existing twelve-event tour retains its finale.
- Start, lap changes and the last minute receive HUD feedback. Camera recovery snaps
  to the car after a large relocation instead of flying through scenery.

## Worlds and packaging

Five central circuit landmarks replace the repeated central tower/outpost: city
communications spire, sky observation rings, desert rock arch and solar court,
dome reactor and glacier crystal formation. Footprint clearance remains enforced.
Corner chevrons complement the safety fences and checkpoint recovery.
These are procedural art improvements, not a replacement production art library.

Escaped arena balls reset to centre, with velocity cleared and a kickoff grace
period. Balls outside the field or below the floor cannot score a goal.
The build script assigns the supplied logo as the standalone application icon.

## Evidence and remaining release gates

43 standalone checks pass, including ordered training, invalid time input and
upright-landing requirements. Asset reference and C# syntax checks pass. Added
Unity integration tests cover training/save/career navigation and arena ball escape.
Existing tests cover all mode/theme worlds, boundary recovery, game over and retry.

The cloud has no Unity Editor or Windows player runtime: Unity API compilation,
new PlayMode tests, controller behaviour, visual review and performance are not
verified here. Build-SinglePlayer.cmd runs the Unity checks and creates the Windows
package locally. The resulting executable must be driven through the complete
career and tested on another supported Windows PC before release. The owner's
earlier successful executable predates these changes.

Remaining production decisions include final car/environment art, handling and
AI tuning, measured hardware requirements and storefront assets. No store upload,
certification or release approval has taken place.
