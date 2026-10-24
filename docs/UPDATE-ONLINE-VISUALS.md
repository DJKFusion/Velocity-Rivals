# Private rooms, atmosphere and expanded soundtrack

Implemented in the existing Unity 6000.6.7f1 project; this is development source, not a certified or verified finished release.

## Gameplay and networking

The previous fixed LAN sprint now supports a lobby, host-started matches, synchronized countdown, rematches, each player's selected vehicle, host-owned recovery, and three two-player modes across five themes: Gate Sprint, Circuit Duel and Arena Duel. Network worlds suppress local AI, ghosts, single-player scores and local pause. Guest snapshots drive wheel rotation, boost presentation, speed/FOV and the arena core. Input from a silent guest is neutralized after half a second.

Direct LAN remains available. Private online rooms use a WSS client plus a Python WebSocket relay, with eight-character room codes. Deployment files and a setup guide are included. **There is no live public endpoint yet.** See [ONLINE.md](ONLINE.md).

## Visual changes

- Custom URP sky shader with theme palettes, sun/moon glow, stars and moving cloud layers.
- Theme-matched distance fog and sky reflection probes.
- Original tiled asphalt albedo and normal textures, smoother city/ice roads.
- Headlights, rear tyre skid trails and bounded drift-smoke particles. Performance quality disables headlights and smoke.
- Bloom scatter, color contrast/saturation and a restrained vignette.

All textures are generated from original noise and geometry. There are no downloaded car brands or copied game assets. These shader, lighting and effect integrations have not been rendered or profiled in Unity here. They are not evidence of photorealism or a measured frame-rate. Test shader import, tire-trail alignment and appearance on all five environments and all graphics settings.

## Music

Six additional original synthwave tracks bring the total to nine: three menu tracks and six race tracks, about 11.4 minutes altogether. New tracks have longer arrangements and breakdowns. All nine delivered OGG files decode successfully and have verified stereo signal/duration/peaks. Listening and Unity playback checks are still required.

## Verification

28 compiled standalone C# checks pass; eight actual WebSocket relay tests pass; the game's C# relay transport passes a local server integration exercise. Generated textures decode at expected dimensions, music validation passes, and C# syntax/asset references pass. Unity Test Runner now also contains network-world isolation and proxy-presentation tests, which remain unrun.

Remaining: deploy and test a WSS service, run both Unity suites, complete real two-PC sessions in each mode, check controls/physics/graphics, measure latency and frame time, produce and playtest the Windows build. Public matchmaking, production anti-cheat, more than two players, console services, final level art and full release QA are unfinished.
