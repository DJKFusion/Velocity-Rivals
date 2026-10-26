# Droppable race hazards

Championship and Elimination crates now include Oil Slick (green) and Shock Mine
(red), alongside Shield, Overdrive and Pulse. Hold one item and press F / RB while
grounded to drop a trap four metres behind the car. A failed airborne or invalid
surface drop keeps the item. AI racers can collect and deploy the same traps.

- Oil Slick: reduces horizontal speed by 20% and grip for 1.6 seconds.
- Shock Mine: reduces horizontal speed by 45% without stopping steering.
- Both: one-second arming warning, three-second owner protection, one target per
  trap, twenty-second match-time lifetime, twelve active traps maximum.
- Shields absorb and consume the trap. Two-second hit immunity prevents chains.
- Grounded cars can be hit; airborne cars can jump over hazards. Bright green/red
  discs mark the occupied lane. They have no blocking colliders.
- Pause freezes hazards; restarting removes them. Recovery clears reduced grip.
- Swept physics positions detect cars crossing quickly, while large recovery
  teleports do not trigger traps along the teleport path.

No online multiplayer is added by this change. Time Attack remains trap-free;
Boost Battle retains its original three items. No third-party game assets used.

Validation: standalone inventory checks and C# source parsing run in cloud.
Unity tests added for airborne use rejection, active cap, expiry, paused lifetime,
repeat-hit immunity, recovery and restart. Unity tests and gameplay balance still
need verification in the Windows build. Test shields, rival drops, dodging and
high-speed contact on each circuit before release.
