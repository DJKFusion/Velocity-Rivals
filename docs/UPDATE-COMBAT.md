# Integrated combat update

Implemented in the existing game, not a separate project:

- Boost Battle uses 100 armor per vehicle, impact damage, three-point knockouts, three-second rebuilding and two-second spawn protection. High-speed nonlethal hits award one point; the target is now fifteen points.
- Eight shared player/AI pickup positions cycle through Flux Shield, Overdrive and Pulse Burst. Shields last six seconds, double damage lasts eight, pulse affects rivals within eighteen meters, and each pickup returns after twelve seconds. Timers use the paused match clock.
- Armor, shield, overdrive and rebuilding countdown appear in the battle HUD. Knocked-out cars hide and stop simulating until respawn; opponents can pursue pickups.
- Rookie/Rival/Elite difficulty changes AI acceleration and maximum speed. Steering sensitivity is saved with the player profile.
- Synthesized cues cover countdown, start, goals, jumps, drift, boost, impact, knockout and pickups. Runtime particles show impacts, shields, pickups and respawns. All audio remains starter sound design.
- The garage has a rotating paint/finish preview with an owned render texture and cleanup when leaving the page. Other cosmetic previews are still incomplete.
- Editor startup opens the game only when the current scene is unnamed, clean and contains at most the default two root objects. Velocity Rivals → Play Game explicitly opens and plays the entry scene after offering to save changes.
- Result subtitles resize for multiline standings.

## Evidence

Fourteen compiled console tests passed: the original nine race/framing tests plus five combat tests covering shield expiry, bounded/duplicate damage, respawn delay/protection, overdrive expiry/reset and invalid buffs/damage. The tested `CombatVitals` source is the same class used by `ArenaBattle`. C# syntax and asset-reference checks passed. Three additional Unity EditMode combat tests are authored but unrun.

No new Unity compilation or gameplay claim is made from these checks. The user's previous screenshot demonstrated entering Play Mode after upgrading Input System, but showed an empty Untitled scene; it did not demonstrate the game running. The new combat, preview, sound and scene-launch integrations still require actual Editor validation.

## Next acceptance run

1. Open the updated project and choose Velocity Rivals → Play Game. Check that the main menu appears and Console has no new red errors.
2. Open Garage and change paint/finish. Return to the menu repeatedly; check preview cameras/render textures are destroyed.
3. Play Boost Battle in Cyber Dome. Collect each pickup, receive damage under a shield, take a knockout, and verify delayed respawn, score and spawn protection.
4. Pause while a shield is active or the car is rebuilding; the countdown must stop. Resume and ensure a destroyed car cannot move before rebuilding.
5. Verify AI collects pickups and score/knockout behavior is the same for opponents. Check keyboard and controller handling at all sensitivities and difficulty settings.
6. Run both Unity Test Runner suites, then build and test Windows using the documented build script.

Production online services, console integrations, final art/audio, full vehicle/world polish, measured performance and a verified Windows executable remain unfinished. This update does not complete the full release request.
