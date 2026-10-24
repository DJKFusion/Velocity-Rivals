# Multiplayer status

`LanRaceSession` implements a two-player, direct-IP LAN gate sprint. It is a development feature, not production Internet matchmaking. Unity runtime testing is still required: no Unity Editor or built game was available in the cloud environment when this code was written.

## Run locally

The bootstrap adds `LanRaceSession` to its root. Press F8 to display its controls. Open two standalone instances (or one Editor and one standalone instance), click Host on the first, then enter `127.0.0.1` and Join on the second. For two computers on the same trusted LAN, enter the host's LAN IPv4 address and permit the application through the host firewall for TCP 7777. Do not expose this experimental port to the public Internet.

Both peers load Cyber Dome Free Play. The host owns both vehicle rigidbodies, validates bounded input from its guest, controls a 120-second timer, and awards one point for each ordered gate reached. The gate sequence is north, east, south, west. The guest sends input at 20 Hz and interpolates host snapshots. When time expires the host freezes driving and announces a host/guest win or draw. Host listening begins immediately, but the timer starts only when a guest joins.

Keyboard: W/S drive, A/D steer, Space jump, Shift boost, Ctrl handbrake, Q/E plus steering air roll. Gamepad: triggers drive, left stick steer, South jump, East boost, West handbrake, left shoulder plus steering air roll. These network bindings currently use direct Input System device reads. F8 and connection controls are keyboard/mouse-only development UI.

## Manual acceptance checklist (not yet run)

- Host and guest connect, see two cars, and independently drive without controlling the other car.
- Both displays agree on vehicle positions, ordered gate scores, timer and result.
- A client cannot assign its position or score: its messages contain input only.
- Third connection is rejected; invalid or oversized messages disconnect safely.
- Disconnect, connection refusal, ten-second timeout and host shutdown return clear messages.
- Returning to the main menu and reconnecting cleans up sockets and spawned objects.
- A client that stops sending input is neutralized after 0.5 seconds.
- Run a packet-delay/loss simulation. TCP retransmission will introduce stalls; this is a known limitation.

## Scope still unfinished

This implementation has no authentication, encryption, public or private service lobbies, Relay, matchmaking, NAT traversal, reconnect migration, client prediction, rollback, lag compensation, team selection, online save data or cross-platform support. It synchronizes a dedicated LAN gate sprint, not the single-player championship or arena scoring. Guest speedometer/effects need dedicated snapshot fields. It is unsuitable for untrusted public peers or competitive deployment. No multiplayer test or executable has been claimed as passed.

## Production direction

Use Unity Netcode for GameObjects with Unity Transport for authoritative sessions, Unity Multiplayer Services for lobby/session management, and Relay initially for connection establishment. Keep competitive results authoritative on a trusted server; Relay hosting on a player's computer alone does not prevent host cheating. Evaluate dedicated server hosting before adding ranked play. Costs depend on concurrent players, bandwidth and server allocation; measure real sessions before budgeting. Pin SDK versions to the verified Unity Editor and test upgrades.

Console and cross-platform work requires the relevant platform agreements, authorized SDK packages, account authentication, invitations, privilege checks, disconnect policies, certification testing and hardware. Unity package availability alone is not proof of console compatibility. Steam and Epic services are separate integrations with their own application setup and testing requirements.
