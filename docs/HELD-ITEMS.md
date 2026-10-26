# Held power-ups and developer music — same 0.6.0

Championship and Elimination have four pickup stations with three coloured crates
across the road. Boost Battle's existing eight pickups now fill an inventory rather
than firing immediately. Each driver holds one Shield, Overdrive or Pulse. Full
slots leave crates available to other cars; consumed crates return after 12 match
seconds. Press **F / right bumper (RB)** to activate. The HUD shows the held item
and race shield time. Pause, resume countdown, knockout and results block use.

| Item | Racing | Boost Battle |
| --- | --- | --- |
| Shield | Six seconds of protection from item pulses (not collision immunity) | Six seconds of damage protection |
| Overdrive | +60% boost fuel, capped at full, and a 7 m/s forward kick when grounded | Eight seconds of double damage |
| Pulse | Pushes active unshielded rivals within 18 m; solid obstructions block it | 35 base damage to nearby enemies within 18 m, subject to shield/damage rules |

Rivals wait at least one second after collecting, then use items based on nearby
cars or grounded state. Combat knockout discards a held item. Each new round,
restart and menu return destroys the match inventory. Time Attack, Arena Clash,
Stunt Rush and Free Play remain item-free. Championship, Elimination and Boost
Battle records now use race-rules revision 2; old records/progress are retained.

The custom player-folder music loader and import/reload buttons are removed.
`Assets/Scripts/Audio/MusicLibrary.cs` explicitly lists project audio paths. The
Unity build guard verifies those paths. See MUSIC.md for the code example. Music
volume, playlist preview, crossfading and skipping remain in the game.

Verification: 56 standalone checks pass, including held-slot collection, refusal
to overwrite, blocked activation, consumption and clearing. Static source checks
pass. New Unity tests cover collection/use, keyboard F activation, pause/restart,
Time Attack exclusion, held combat shields and absence of player music importing.
Unity tests, actual RB input, pickup visibility, pulse physics and AI/item balance
remain untested in this cloud workspace. Run the normal test/build script and
playtest before distribution; this is not a verified finished Windows release.
