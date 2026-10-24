# 0.5.0 — structured worlds and career presentation

The previous 0.4 Windows EXE launched and was positively playtested by the owner.
The owner identified sparse, unstructured maps as a major remaining gap. This
update changes the layouts and scenery themselves rather than just adding effects.

## Changes

- Five authored circuits with evenly spaced gates, distinct bends and four named
  districts each. These replace the earlier oval variations.
- Buildings on clear footprints inside and outside the circuit: podiums, towers,
  windows, rooftops and a central landmark. Desert mesas/refinery outposts and
  glacier formations/research stations give those venues different silhouettes.
- Covered track sections, named overhead signs, road lights and start-area
  grandstands where there is room. Skyfall has a supported bridge deck.
- Arenas have end facades, concourses, seating, entrance bays and end signs, plus
  themed surroundings outside the playing area. Core sport retains a clear field.
- Source primitive meshes are reused when combining scenery, avoiding thousands
  of temporary primitive GameObjects per match. Final meshes remain grouped by
  material. Target-PC profiling is still required for the denser scenes.
- AI uses the upcoming bend to reduce target speed before entering corners and
  uses boost on suitable straights. Actual AI completion/balance needs playtests.
- District names and event objectives in the HUD; a direct practice button in
  How to Play; four-cup trophy cabinet; XP/car unlock feedback; a tour finale and
  an all-gold achievement; confirmation before abandoning/restarting an event.
- Circuit records and ghosts are versioned. Old records stay labeled as old
  layouts; prior XP, cars and career medals are preserved. No files are deleted.
- Windows packages include PLAYER GUIDE.txt. Version is 0.5.0.

## Evidence and remaining verification

38 standalone C# checks pass, including layout spacing, finite coordinates,
clearance between distant road sections, corner smoothness and unique layouts.
Static C# syntax and asset references pass. Unity tests additionally check venue
signs, the authored route in each racing mode, trophy navigation and old/new
record separation. The updated Unity tests have not run in this workspace.

The owner's successful 0.4 EXE does not validate these new layouts or UI changes.
Build 0.5 with Build-SinglePlayer.cmd, then drive every circuit and arena. Verify
no scenery intrudes on the road, signs render, the chase camera clears covered
sections, AI finishes and recovers, and saves remain compatible. Complete a tour
to check the finale. Measure performance on the intended release hardware.
This update is not a claim of a completed, fully tested commercial release.
