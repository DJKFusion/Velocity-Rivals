# Menu, HUD and soundtrack refresh — same 0.6.0 project

## Implemented

- Ten home tiles in two columns, with short descriptions and a highlighted career entry. Existing game modes, records, garage and settings remain accessible with mouse, keyboard or controller.
- Stronger button focus/hover colors, bold headings, wallet/driver-level header, car showroom and now-playing footer. Long secondary pages retain scrolling and automatic focus reveal.
- Compact upper-left position/lap/score instrument, mode-specific status, smaller minimap and location caption. Speed has a larger numeral, twelve boost segments and slipstream speed-bonus feedback.
- Opening controls disappear after eight seconds so they do not cover the race. Training and boundary recovery retain guidance. Warnings have shadowed text; elimination danger still highlights last place.
- Result report shows outcome, credits/XP or round points, career medal and wallet. Failure, training and intermediate championship rounds have distinct messages.
- All twelve supplied audio assets have been replaced in place, preserving filenames and GUIDs. Twelve groove profiles span ambient, halftime, garage, downtempo, outrun, breakbeat, drum-and-bass, house, industrial, trance, electro and bigbeat. Leads, bass timing, motifs, harmony, percussion and phrase rests differ; intros, breakdowns and outros vary their density.
- Soundtrack opens directly from home; preview either playlist, see playback progress, skip with N and import your own songs through the existing custom-music controls. Preview ends when leaving the page. Race now-playing cards avoid the opening tutorial banner.

## Verification and limits

- All twelve actual OGG deliveries decoded through ffmpeg: stereo 44.1 kHz, finite samples, non-silent RMS, no sample clipping, expected duration. Total ~18.3 minutes. `Tools/Audio/validation.json` records tempo, groove, instrument, duration and decoded signal measurements.
- Static C# syntax, asset GUID/reference, package and delivery checks pass.
- 52 standalone C# checks pass. These exercise shared gameplay/save/archive logic; they do not establish Unity UI or rendering correctness.
- Added Unity PlayMode coverage for home tile bounds, menu/race radio audition and restoring the menu playlist. Extended the HUD pause/resume test to check opening-hint dismissal and boost visibility. These tests cannot be run in this cloud workspace: no licensed Unity Editor is installed.
- No Unity screenshots, audible listening review, Windows EXE or release certification is claimed. This update does not change car physics, maps, progression or the application version.

## Windows acceptance

Extract the new source into a short, fresh folder, keep your previous project, close Unity, and run `Build-SinglePlayer.cmd`. The existing test/build gates remain enabled.

1. Check home tile text and navigation at 1280×720, 1920×1080, ultrawide and 4:3; verify scroll/back behavior on the workshop and soundtrack pages.
2. Audition both playlists and skip between several tracks. Compare the ambient Skyline Dreams with Hyperdrive and Ion Storm. Check volume against engine/effects, transitions, mute and pause ducking.
3. Verify custom OGG/WAV/MP3 reload and playback, including long filenames.
4. Drive Championship, Elimination, Time Attack and each arena mode. Check score, lap, armor, cutoff and speed text; verify guidance clears and warnings remain readable on bright/dark maps.
5. Test winning, losing, a career medal, a championship round and Driving School results; retry/continue/back must still work.
