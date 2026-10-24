> Historical development guide for the earlier multiplayer edition (commit db4284e). Version 0.4.0 is single player: networking is excluded and these menu/deployment steps do not apply. Use [the single-player release guide](SINGLE-PLAYER-RELEASE.md).

# Play with a friend in another home

This update contains an online private-room client and a deployable relay. **No public relay has been deployed for you.** The actual C# transport has passed a local connection test with the Python server; Unity gameplay over it remains untested. Both players need this same update (protocol 3).

## One-time relay setup — the host does this

The relay is a small server that connects the two game clients. It allows friends in separate homes to connect without port forwarding. Hosting it does not build the Unity game.

1. Open [the Render deployment page for this repository](https://render.com/deploy?repo=https://github.com/DJKFusion/Velocity-Rivals). Sign in or create your own Render account.
2. Review the blueprint deployment. It requests one service named `velocity-rivals-relay`, using `Tools/Relay/Dockerfile` and the **free** plan. Free-tier availability depends on your account and the provider's current offering; no service or paid plan has been purchased here.
3. Deploy and wait until the service is healthy. Copy its HTTPS address, for example `https://your-service-name.onrender.com`. This is an example, not an existing game server.
4. Open that address with `/health` appended. It should show `Velocity relay ready`.
5. In the game's relay field, replace `https://` with **`wss://`** and use the service root, without `/health`. Share this address with your friend. Do not share your hosting account login.

If the deploy button doesn't offer the blueprint, create a Render Web Service from this GitHub repository, choose Docker, set the Docker context to `Tools/Relay`, the Dockerfile path to `Tools/Relay/Dockerfile`, port `10000`, and health check `/health`. The checked-in `render.yaml` is the intended configuration; the provider deployment itself has not been exercised here.

Another Docker-capable host works too. It must support WebSockets, forward its public HTTPS/WSS endpoint to container port 10000, and keep both players on the same running relay instance. Room state is in memory: scaling across multiple independent instances requires shared routing/state, which is not implemented.

## Create and join a match

1. Both players open the updated game and choose **MULTIPLAYER**.
2. The host selects **Circuit Duel**, **Arena Duel** or **Gate Sprint**, plus an environment. Vehicle selection comes from each player's saved selection in the Play menu.
3. Both choose **ONLINE ROOM** and paste the same `wss://…` address.
4. The host clicks **CREATE ROOM** and shares the eight-character code shown. The **COPY ROOM CODE** button copies it.
5. The friend enters that code and clicks **JOIN ROOM**.
6. When the host sees that the friend is ready, click **START MATCH**. The countdown starts only after both worlds have loaded.
7. After results, the host can click **REMATCH**. Use **F8** for room controls and **LEAVE ROOM** to exit.

An unused room expires after five minutes. If either player disconnects, that room closes; create a new room to play again. A sleeping free-tier server may need waking through `/health` before the first connection. The client waits up to a minute for its initial relay connection.

## Modes

- **Circuit Duel:** ordered checkpoints and three laps; first finisher wins. At the ten-minute limit, checkpoint progress decides the winner; equal progress draws.
- **Arena Duel:** one car on each side, a synchronized physics core, first to five goals or three minutes. Host/cyan attacks north, guest/orange attacks south.
- **Gate Sprint:** ordered north/east/south/west targets; most gates after two minutes wins.

The host simulates both cars, the core, timers, checkpoints and results. The guest sends controls and displays interpolated state, including boost, wheels, speedometer and camera response. This is private, unranked play: there is no dedicated authoritative game server, client prediction, matchmaking, account identity or host migration. A relay does not prevent a dishonest host from altering gameplay. Internet latency and bandwidth costs must be measured with real sessions.

## What has been tested

- Eight Python relay tests: room pairing/forwarding, wrong protocol, missing room, third-player rejection, role restrictions, disconnect cleanup, oversized messages and room capacity.
- The game's actual `RelayTransport.cs`, compiled with .NET, connected two clients through the local Python service, created/joined a room, exchanged game packets and received a disconnect notification.
- Pure match-rule tests cover countdown, laps, goals, timeout, draws, reset and invalid inputs.

These do not establish Unity API compatibility, TLS hosting deployment, Internet playability or graphics performance. Run a complete match in each mode on two PCs, test rematches and disconnects, and build the Windows player before treating it as ready to share.

## Run the relay locally for development

```sh
python -m pip install -r Tools/Relay/requirements.txt
python Tools/Relay/server.py
```

The default port is 10000. Two clients on that same computer can use `ws://127.0.0.1:10000`. Non-loopback addresses require `wss://`. The service limits payloads to 4096 bytes, caps queues/connections/rooms, validates protocol and sender role, and forwards game state only from the host. It has no account service or durable room database; rate limits are not a substitute for production abuse protection.
