# Steam preparation

The owner has not registered or paid the Steam app fee as of 24 October 2026.
Under Steam's normal rules, a public release in the week of 26 October–1 November
is not feasible: there is normally a 30-day wait after the app fee and at least
two weeks with a public Coming Soon page, plus required reviews. The Steamworks
dashboard is authoritative for this app's eligible release date. The documentation
URLs below returned HTTP 403 from this workspace, so live rules were not rechecked.

- https://partner.steamgames.com/doc/gettingstarted/onboarding
- https://partner.steamgames.com/doc/store/releasing
- https://partner.steamgames.com/doc/sdk/uploading

## Account and store page

The owner completes Steamworks identity, tax, banking, agreements and app-fee steps
directly with Valve. Obtain the real App ID and Windows depot ID from the dashboard.
Do not use another game's IDs. Create the store page with actual gameplay captures,
accurate supported features and measured system requirements. Submit the store and
build for review according to the dashboard checklist. No registration, payment,
submission, upload or publication was performed by this project update.

## Prepare this game's upload

1. Run Build-SinglePlayer.cmd for version 0.6.0 and resolve failed tests/builds.
2. Test the extracted Windows ZIP, including the complete career and save/reload.
3. Open the project in Unity. Choose **Velocity Rivals → Prepare Steam Depot**.
4. Enter your Steam App ID and Windows depot ID; select **Create local SteamPipe files**.
5. The tool creates app_build.vdf and depot_build.vdf under a fresh Artifacts/Steam
   folder. Review ContentRoot and the IDs. The complete Windows player is mapped.
   There is no SetLive instruction: preparing/uploading does not publish a branch.
6. Use Valve's Steamworks SDK/SteamCMD with an appropriately permitted developer
   account. Log in locally; do not paste passwords or Steam Guard codes into chat.

Example command, substituting your own username and generated path:

```text
steamcmd +login YOUR_STEAM_USERNAME +run_app_build "C:\path\to\Artifacts\Steam\build-folder\app_build.vdf" +quit
```

In Steamworks, set Windows launch options to VelocityRivals.exe. Follow Valve's
workflow to put the uploaded build on a private test branch. Install through the
Steam client on a second PC and test launch, overlay/focus, controller input,
saves, reinstall and updates. Release only after reviews, timing requirements and
your acceptance checks pass. This project does not yet implement Steam achievements,
Steam Cloud or Steam account services; do not advertise those features.

The descriptor generator is tested against file fixtures. Actual SteamPipe upload
and Steam-client installation remain untested.
