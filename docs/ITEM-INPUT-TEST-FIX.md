# Item input test timing fix

The uploaded 26 October 2026 14:05 UTC PlayMode report contains 40 tests:
39 passed and FKeyActivatesHeldRaceItem failed (expected None, actual Shield).
Unity exited with code 2. Racing audio, dropped hazards and direct item activation
passed. This is a different failure from the obsolete CustomMusic compilation error.

The failing test queued a synthetic keyboard event and yielded two player-loop
frames before checking consumption. That does not control when Input System
processes the event relative to the session's one-frame press poll in batchmode.
The revised test explicitly establishes released state, processes manual input
updates, and immediately invokes the production GameSession.Update handler. It
restores the input update mode and current devices in finally. No runtime item
binding, assertion or build gate was removed.

Coverage now includes F and right shoulder, one activation per new press, holding
without consuming another item, and refusing activation while paused. Cloud syntax
validation and 58 standalone checks pass. The revised Unity tests have not been
run here; a Windows test/build rerun is required to verify resolution.

## Follow-up: direct input actions

The 14:15 UTC rerun had 41 tests: the same 39 passed, and both F and RB tests
failed to consume Shield. The first test-only change did not resolve the issue.
It should not be regarded as a confirmed fix.

Item activation now uses a Unity InputAction bound to keyboard F and gamepad
right shoulder, with the performed callback calling the guarded UseItem path.
The action is enabled/disabled with the session and disposed on destruction.
Frame-polled wasPressedThisFrame activation was removed to prevent duplicate
handling. Presses while unavailable are not queued for later activation.

Tests now deliver device events to that production action, assert the actual
button state, and verify consumption without invoking Update via SendMessage.
Synthetic tests temporarily ignore window focus and restore the previous input
settings/devices in finally. Gameplay focus-pause behaviour is unchanged.
This addresses the input implementation as well as the test harness; a fresh
Unity run is still necessary to confirm success on the affected machine.
