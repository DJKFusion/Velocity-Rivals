# Cornering and screenshot fixes — same 0.6.0

The owner reported that corners were difficult to turn, with screenshots showing
the Bulwark. The previous steering formula retained only 43% of the steering rate
at top speed; the handbrake reduced lateral grip to 16% of base grip.

## Changes

- Shared `HandlingModel` retains 75% of steering authority at top speed, multiplies
  base steering rate by 1.25 and increases angular response from 8 to 16. Inputs
  transition over roughly 0.11 seconds to soften abrupt keyboard direction changes.
  Reverse steering and no stationary pivot are preserved.
- Normal lateral grip is 1.4× base. Drift grip is 0.55× base, with 18% more yaw while
  handbraking. Cars retain their distinct base stats and workshop upgrades.
- Strong turns without boost/handbrake progressively lower forward throttle targets
  to 65% of top speed. Deceleration is bounded at 9 m/s². Throttle stops accelerating
  above that target; boost overrides the assistance. AI uses the same car rules.
- Glacier's base grip modifier changes from 0.45 to 0.8. It remains less grippy
  than the other surfaces, but the previous severe grip reduction is removed.
- Boost trail switches from opaque Lit material to the existing transparent,
  vertex-alpha soft-effects shader and texture. It is narrower, shorter and clears
  during recovery, preventing a streak across a teleport. Missing effect shaders
  disable the visual rather than falling back to an opaque ribbon.
- Removes the visible white side softbox, reduces showroom light/emission intensity,
  roughens the plinth and lowers paint smoothness/metallicity to reduce glare.
- New records carry handling revision 1. Existing records are retained and labeled
  as earlier handling. New ghosts use a handling-specific filename. Progress,
  purchased cars, medals and credits remain intact.

## Evidence and remaining acceptance

55 standalone checks pass, including heavy-car high-speed steering, steering sign,
reverse/stationary cases, drift authority, corner speed targets and boost overrides.
Source syntax and metadata/reference checks pass. Added Unity tests drive a Bulwark
through a sustained corner on a flat surface, inspect trail material/alpha and
verify earlier handling records remain separate. These Unity tests are unrun here.

Build using `Build-SinglePlayer.cmd` with Unity closed. Test Bulwark and a light car
at stock sensitivity on Neon City and Glacier, first without boost and then while
boosting. Check slow corners, high-speed sweeping bends, reverse, brief handbrake
turns, camera feel and controller partial-stick input. Finish a championship to
check AI against the new physics. Compare showroom/trail visuals to the supplied
screenshots. Actual handling feel, physics stability and Unity rendering have not
been verified by the standalone checks; this is not a claim of a finished release.
