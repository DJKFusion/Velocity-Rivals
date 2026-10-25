# Expanded maps — integrated into 0.6.0

All earlier menu/HUD, soundtrack, career/garage, elimination/slipstream, controller,
resume and replay-integrity changes remain included.

## Size

Circuit control points expand uniformly by 1.8 while preserving the 20-metre road
width and approximately 10-metre checkpoint spacing. More segments are generated;
checkpoint distances are not stretched. Gates, barriers, fences, directional
markings, recharge lanes, district signs, bridge supports and tunnels follow the
resampled route. Skyfall ramps use proportional locations.

| Circuit | Approximate lap length | Checkpoints |
| --- | ---: | ---: |
| Neon City | 967 m | 97 |
| Skyfall | 1,059 m | 106 |
| Desert | 1,162 m | 117 |
| Cyber Dome | 970 m | 98 |
| Glacier | 1,083 m | 109 |

Arena dimensions grow from 90 × 100 to 126 × 140 metres (1.96× floor area).
Walls, ramps, goals and spectator structures expand horizontally. Cars, the core
and gravity retain their sizes/settings. Spawn positions and combat pickups move
outward. Goal detection, AI goal targets, recovery boundaries and map corners use
the same expansion factor.

Circuit terrain and reflection coverage extend around the routes. District scenery
covers a wider footprint with a coarser grid to limit object growth. Circuit road
lights are spaced to keep their total near ten rather than increasing with every
new road segment. Road geometry still grows and needs target-PC profiling.

## Progress and balance

Three-lap races and ten-minute circuit limits remain. Time-trial gold targets grow
to 270/297/315/342 seconds; Arena Clash gold becomes 150 seconds and career combat
gold becomes 130 seconds. These are initial adjusted targets, not verified balance.
Elimination keeps its 45-second cutoffs and 135-second total.

Layout revision 4 separates new circuit and arena records from earlier records;
Time Attack uses new ghost filenames. Old data is retained. Existing career medals,
XP, purchases, upgrades and customization are not reset.

## Checks

53 standalone checks pass, including larger route lengths, finite coordinates,
roughly 10-metre spacing, smooth bends, separation of distant road segments and
career time targets below the event deadline. Static source/reference checks pass.
Added Unity tests check the arena floor/walls, new legal driving space, scoring at
the moved goal and separation of older arena records. Existing boundary/core
recovery checks now use the expanded bounds.

Unity tests and Windows runs have not been executed here. Before distribution,
build with `Build-SinglePlayer.cmd`, drive every circuit, finish the career time
trials, check arena goals and recoveries, and measure frame rate on the target PC.
