# Release polish — same 0.6.0 project

This pass targets player-facing reliability, not additional modes or a version bump.

## Changes

- Pausing snapshots the drivers that were enabled. Resume no longer re-enables a finished rival, an eliminated car or a disabled arena fighter.
- The game tracks the device used for driving. Disconnecting/removing/disabling that controller pauses a local event and explains how to continue with keyboard or a reconnected controller. Losing window focus also explains why the race paused. Reconnection does not silently restart a race.
- RESUME provides a three-second unscaled countdown after racing has started; physics, race timers, AI and vehicle input stay frozen. Another interruption can pause that countdown. Initial grid countdowns use their existing timer.
- Optional saved vibration strength (Off/Low/Medium/High) adds boost, drift and landing/impact feedback. Motors stop in pause, resume countdown, menus, on loss of focus, on device changes and component disposal. Actual support depends on the controller/driver.
- Championship results display drivers sorted by points; equal totals keep the existing player-first ordering. New profiles initially focus the Driving School tile, without locking out any menu item.
- Imported local ghosts must have finite bounded durations, strictly increasing timestamps, finite positions and approximately unit rotations. Damaged runs are ignored rather than applied to a Transform or allowed to block a replacement record. New recordings are validated before replacing an older replay.

## Verification

Static C# syntax and Unity metadata/reference checks pass. 52 standalone C# checks pass; these are shared logic checks, not execution of the new Unity behavior. Added EditMode cases cover corrupt ghost duration, ordering and transforms. Added PlayMode cases cover interrupted resume, frozen elapsed time, preservation of stopped drivers, synthetic controller disconnection and zero motor output after pause.

Unity compilation and these new Unity tests have **not run** in the cloud. There is no licensed Unity Editor here. No new Windows EXE or hardware rumble test is claimed.

## Required build acceptance

Run `Build-SinglePlayer.cmd` from a short, fresh project folder with Unity closed. Keep both test gates enabled. Then verify the extracted Windows package:

1. Complete a full three-round championship; check standings and advance/retry/menu actions.
2. Pause at speed, resume, pause again during the countdown, then resume. The timer and all cars should remain frozen until the countdown ends.
3. Disconnect the active controller at speed and during a countdown. Reconnect, explicitly resume, then repeat using the keyboard. Disconnect an unused controller while playing with the keyboard; that should not interrupt the race.
4. Alt-tab at speed. On returning, explicitly resume. Check rumble stops immediately and respects Off/Low/Medium/High.
5. Finish Time Attack twice and restart; verify the ghost still loads and personal records persist.
6. Complete the remaining checks in `SINGLE-PLAYER-RELEASE.md`, especially all twelve career events, all five courses, clean-machine launch and performance. Source improvements alone cannot establish that the game is ready to sell.
