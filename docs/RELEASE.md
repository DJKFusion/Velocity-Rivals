# Release preparation

The current deliverable is development source. The Windows pipeline script exists but has not produced or validated a player build. A shipping release requires completed features, gameplay QA, production assets/audio, profiling, legal/store checks and actual target builds.

## Windows

Install the exact Editor and Windows build support in Unity Hub. Run Test Runner, build using the menu or PowerShell driver, and distribute the whole `Builds/Windows` directory. Exercise installation, save permissions, display modes, controller hot-plug and clean shutdown on separate PCs. Retain symbol files and build logs for crash diagnosis. Add signed installers, versioning, crash reporting and patch infrastructure before public distribution.

## Steam and Epic Games Store

Register through [Steamworks](https://partner.steamgames.com/) and [Epic Developer Portal](https://dev.epicgames.com/portal/), accept their current distribution terms and complete required onboarding. Obtain application identifiers and store access through official channels. Add and test the authorized SDK integrations for accounts, achievements, cloud saves, invites/overlay and store build upload as applicable. Store presence and fees do not substitute for technical testing or content review. No account setup, SDK integration or upload was performed here.

## PlayStation 5 and Xbox Series X|S

Apply through [PlayStation Partners](https://partners.playstation.net/) and [ID@Xbox](https://www.xbox.com/developers/id). Acceptance, contracts/NDAs, authorized platform SDKs, development hardware and an appropriate Unity console licensing arrangement are prerequisites. Download console modules only through the platform and Unity access supplied to an approved developer. This project includes no restricted SDK material and no console binaries.

Once authorized: implement platform storage/user selection, privilege and parental checks, controller conventions, suspend/resume, disconnect handling, account switching, invitations, achievements/trophies, accessibility and required error presentation. Profile CPU/GPU/memory on actual development hardware. Run each platform's current certification requirements and submit only tested builds. Documentation here is preparation, not certification evidence.

## Networking and cross-play

Use the LAN sprint for development only. For a production direction, evaluate pinned Unity Netcode for GameObjects + Unity Transport + Multiplayer Services/Relay, moving competitive physics/results to trusted dedicated servers. Measure bandwidth, CPU per match and expected concurrency before choosing hosting commitments. Add account identity, authenticated sessions, rate limits, matchmaking, reconnect behavior, telemetry and moderation appropriate to the released features. Cross-play additionally requires platform permissions, service agreements and testing on every participating platform. No cross-play functionality is claimed.

## Asset and project rights

Generated meshes and synthesized audio are original starter content produced for this project. Unity, Blender, packages, fonts and any later third-party content retain their own license terms. Keep provenance and required notices for all shipping content; do not add copyrighted vehicle brands, game logos, music or copied arena designs. No open-source license for the user's whole game is imposed by this delivery.
