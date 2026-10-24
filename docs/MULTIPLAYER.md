# Multiplayer status

The game implements two-player Gate Sprint, Circuit Duel and Arena Duel over direct LAN or private online rooms. Both use the same host-authoritative rules and protocol version 3. The online transport and server have passed local tests; **Unity multiplayer gameplay and Internet deployment are unverified**.

For friends in different homes, follow [ONLINE.md](ONLINE.md). It explains the included relay deployment and room-code UI. There is no live public relay provisioned for this project yet.

## Direct LAN

1. Run two updated standalone instances, or one Editor and one standalone instance.
2. On the host, choose **MULTIPLAYER**, select the event/environment, then **LOCAL NETWORK / LAN → HOST LAN SESSION**.
3. The guest enters the host computer's LAN IPv4 address and chooses Join. For two instances on one computer, use `127.0.0.1`. Permit TCP 7777 on the host's private-network firewall if prompted.
4. The host clicks **START MATCH** once the friend is ready. Both worlds load before a three-second countdown.
5. Use **F8** for controls, **REMATCH** after results, or **LEAVE ROOM** to exit.

Selected vehicles come from each saved profile. Circuit Duel requires three ordered laps; Arena Duel is one-on-one, first to five goals or three minutes; Gate Sprint awards ordered north/east/south/west targets over two minutes. All modes can use the five environment themes. Arena layouts are still shared across themes.

The host owns both rigidbodies, the core, checkpoint/goal validation, timer and results. The guest sends input at 20 Hz and interpolates snapshots. Guests receive speed, fuel, boost/grounding and core state for presentation. Recovery is handled on the host with a two-second cooldown. Input stops after 0.5 seconds without fresh guest controls; a nonresponsive connected peer times out after 15 seconds. Network worlds cannot be paused by one player's local pause menu.

## Acceptance checklist — requires Unity and two clients

- Both players see the selected cars and control only their own vehicle.
- Start waits for world loading; countdown, clocks, laps, goals and final results agree.
- Finish all three modes in each theme; reject reverse/skipped/duplicate circuit gates.
- Core goals reset players correctly; first to five and time-limit draws work.
- Guest boost, wheels, speedometer and camera reflect snapshots; there is no second local physics simulation.
- Rematches clear progress and stale input; local profile scores are not awarded by network worlds.
- Recovery, falling off Skyfall, input stalls and window switching do not break the match.
- Test disconnect/refusal/timeouts, third-peer rejection and return to menu; test malformed states.
- Repeat via a deployed WSS relay with friends in different homes. Measure input latency, bandwidth, sustained frame rate and memory.

## Limits

This is private, unranked two-player play. It has no public matchmaking, player accounts, server-authoritative physics, anti-cheat, prediction/rollback, host migration, durable rooms, more-than-two-player teams or console services. TCP/WebSocket retransmission and guest interpolation can produce noticeable input delay. Online rooms close when either player leaves; create a new room to reconnect.

The supplied relay forwards messages and enforces role/size/rate/room limits; it does not prevent a host from cheating. Production multiplayer still needs service operation, identity, abuse controls, trusted game servers where required and measured latency compensation. Platform matchmaking and cross-play require additional authorized SDKs and platform testing.
