# Camera, soundtrack and circuit presentation — 0.6.0

- Right stick orbits the chase camera horizontally and vertically. R3/C recentres;
  releasing the stick for 1.5 seconds starts automatic return. Vertical movement
  is bounded; paused/results screens ignore orbit input. Settings save sensitivity
  and inverted Y. Existing obstacle avoidance remains in use.
- The soundtrack has 12 supplied songs (4 menu, 8 race). Three newly synthesized
  songs add approximately 4.3 minutes. See MUSIC.md for custom songs in the built
  player and for bundling songs into the project.
- Circuit recharge lanes restore 45 percentage points of boost, with per-car
  cooldowns. Both player and AI can collect them during active play. Checkpoints
  still must be passed in order. HUD and pickup effects report recharge.
- Balanced/Quality worlds include road spotlights; Performance omits them when
  creating the world. Desert, sky and glacier have distinct ambient light palettes
  and reflection intensity is slightly increased. These are rendering changes,
  not a claim of photorealistic assets or measured performance gains.

Application version remains 0.6.0. Recharge lanes alter achievable times, so circuit
record/ghost layout revision advances to 3. Existing medals, XP and cars remain;
previous circuit records remain separate from new runs.

Source checks and 43 standalone logic tests pass. All 12 OGG deliveries match the
audio report; the three new files were decoded and checked for finite stereo samples,
duration, signal level and unclipped peak. New Unity tests exercise camera controls,
local WAV loading/reloading and a recharge trigger. Unity Editor is unavailable in
the cloud, so these tests, controller feel, MP3/OGG runtime decoding, lighting and
frame rate require a local run. Build-SinglePlayer.cmd keeps those test gates enabled.
