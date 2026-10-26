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

## 14:18 UTC rerun and focused diagnostics

The next report still has 39/41 passing. Keyboard reaches the consumption assertion;
gamepad now explicitly fails the device-state delivery assertion. This proves the
queued synthetic gamepad input is unreliable in this harness; it does not prove
that either physical gameplay binding is broken or fixed.

Tests now enter a Dynamic input update and synchronously apply button state through
InputState.Change, which notifies the production action's state monitors. No direct
UseItem call substitutes for input handling. Additional assertions check the action
exists, is enabled, binds the test device, performs once, and does not repeat while
held. Pause and inventory assertions remain. Runtime input code is unchanged from
the preceding action-based commit.

Run Test-Item-Controls.cmd with Unity closed for the two focused tests. It writes
Artifacts/ItemControls.xml and ItemControls.log and requires exactly two passes.
It does not package a player or bypass Build-SinglePlayer.cmd's full test gates.
Windows/Unity execution is still pending; do not call this a confirmed fix until
those tests pass on the affected machine.

## Bitfield exception correction

The 14:29 UTC report has 39 passing tests and two errors before the activation
assertions. Both throw ArgumentException: Cannot change state of bitfield control
Key:/Keyboard/f using this method. The preceding direct-control injection was
incorrect: InputState.Change rejects controls with nonzero bit offsets or sizes
not divisible by eight. Source parsing could not detect that runtime restriction.

Checked Unity Input System source (1.20.0 Runtime/State/InputState.cs lines 129-132)
and its CoreTests_State.cs full-device state-change/monitor tests. The harness now
passes the entire Keyboard with KeyboardState and entire Gamepad with GamepadState.
It retains real production-action monitoring and all activation assertions. This
corrects the reported bitfield exception; Unity runtime confirmation is pending.

## Editor/player buffer routing

The 14:34 UTC report has 39 passes and both synthetic button-state assertions
failing. Reading InputManager.defaultUpdateType and gameShouldGetInputRegardlessOfFocus
in Unity Input System 1.20.0 shows that BackgroundBehavior.IgnoreFocus only bypasses
Editor focus routing when editorInputBehaviorInPlayMode is also
AllDeviceInputAlwaysGoesToGameView. The harness had set only the first flag. Its
Update() could therefore select Editor buffers while explicit Dynamic writes
updated player buffers. This explains why writes could not be read back.

The harness now sets both supported public settings, asserts that Update() entered
Manual (player) input processing, and uses the same default update context for
whole-device writes. Both focus settings and update mode are restored in finally.
No production gameplay settings or test gates were relaxed. API names checked
against InputSettings.cs and InputState.cs. Runtime verification in Unity remains
pending, and earlier claims of a confirmed input-test fix remain withdrawn.
