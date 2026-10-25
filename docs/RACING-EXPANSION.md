# Racing expansion — 0.6.0

## Elimination

Select PLAY, cycle Mode to Elimination, choose a venue/car/difficulty and start.
The existing enum values 0–5 remain unchanged; Elimination is appended as 6.
All five environments use their circuit layout for this event. Four cars start,
and the last-place driver is eliminated at 45, 90 and 135 seconds of active match
time. Countdown and pause do not advance the clock. The final survivor wins.

Ranking uses completed ordered checkpoints, then projected progress along the
current checkpoint segment. Raw distance across the map cannot skip checkpoints.
Exact ties use starting driver ID, with the later grid entrant eliminated first.
Eliminated AI vehicles are deactivated and disappear from the HUD map, so they
cannot obstruct surviving cars. The player losing ends the event with game over
and retry. Winning produces normal credit/XP rewards, a record and Last survivor
achievement. Losing does not receive completion credits or a completed record.
The twelve-event career catalog and its existing medals are unchanged.

## Slipstream

Championship and Elimination participants can draft behind another active car.
Both must be grounded and moving forward above 45 km/h. The leader must be 5–30 m
ahead, within 2.5 m laterally and 2 m vertically, with aligned headings and a clear
line of sight. Draft builds gradually, provides up to 10% extra unboosted top speed
and 8% acceleration, and fades after leaving the corridor. HUD shows its strength.
Player and rivals use the same rules. Recovery and results clear drafting state.
Time Attack has no slipstream and retains its stock performance and ghost rules.

## Rival race craft

Circuit AI examines three road lanes and chooses an adjacent lane with clearance
when blocked. Lane decisions are held briefly to reduce oscillation. Rivals look
ahead for bends, brake when traffic blocks their line and reserve boost for clear
straights. Ion uses lower corner speed targets and prefers left passes; Prism has
slightly higher targets and spends boost sooner; Comet is balanced. These are
driving preferences, not guaranteed overtakes or measured difficulty improvements.
Existing checkpoint recovery remains in place for overturned or stuck cars.

Championship records created under the earlier rules are retained separately from
new slipstream-era records. Upgrade signatures still distinguish tuned cars.
Application version remains 0.6.0; Time Attack records and ghosts are unchanged.

## Verification

52 standalone checks pass, including exact cutoff timing, repeated calls, frame
delays, ties, player elimination, invalid input, slipstream eligibility, lane
clearance and corner-speed preferences. Source/asset checks pass. Authored Unity
tests cover menu start, win/rewards/restart, pause/loss, drafting in a physics scene,
record compatibility and all 35 mode/environment combinations.

No Unity Editor is installed in the cloud, so the new PlayMode tests, actual rival
lap completion, controller experience, collision behaviour and balance still need
a local run. Build-SinglePlayer.cmd retains both test suites before packaging.
Play all five circuits in Championship and Elimination before release, particularly
passing near corners and barriers. A passing logic suite does not prove race feel.
