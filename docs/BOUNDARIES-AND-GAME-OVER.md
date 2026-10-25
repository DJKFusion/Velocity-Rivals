# Boundaries and game over — 0.6.0

Circuit edges now have 2.4 m crash walls with framed safety fencing to 6 m.
If a player or AI escapes, the session checks distance to the actual road segments
and returns the car to its last checkpoint once it is more than 16 m from the
centreline. Falling below -4 m also recovers the car. Jumps above the road remain
allowed. Arena modes recover vehicles outside the arena perimeter. Recovery
clears vehicle velocity, preserves checkpoint progress and leaves the clock running.
The player sees an out-of-bounds notification after boundary recovery.

Failed events now show GAME OVER, the result reason, TRY AGAIN and MAIN MENU.
Career failures also offer CAREER TOUR. Completed intermediate championship rounds
still offer the next round. Retry restarts the event (the whole championship for
that mode); previously saved progression is retained.

Validation: source syntax/assets checked in cloud. PlayMode regression tests were
added for all five themes, legitimate jumps, escape recovery without checkpoint
advancement, timeout, the game-over heading and retry. Unity is unavailable in the
cloud, so these tests and high-speed collision behaviour must run locally through
Build-SinglePlayer.cmd. Before release, drive/jump into both sides of sharp corners,
try leaving every arena, fall off Skyfall, and check controller retry/menu navigation.

This update keeps version 0.6.0. Rebuild the player to include it; an existing EXE
does not update when source files change.
