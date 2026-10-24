# Release target and acceptance record

Target preparation week: **26 October–1 November 2026**, Europe/Dublin.
This is a testing and packaging target, not a promise that a storefront will approve
the game by that date. The owner confirmed the older Windows EXE ran; the redesigned
0.6 UI and 0.5 tracks still require their own full acceptance run.

## Distribution choices

- **itch.io:** no upfront listing fee; upload the complete Windows game ZIP. It can
  be free or paid. Payment-provider fees apply to sales; check the chosen payment
  mode and revenue-sharing settings. Start with an unpublished draft page.
  https://itch.io/docs/creators/getting-started
- **Microsoft Store PC:** possible for a Windows desktop game, subject to developer
  onboarding, eligible packaging and certification. Microsoft has introduced free
  individual onboarding; verify the currently available account flow and Irish
  eligibility before assuming a zero fee. No live fee verification was possible
  from this workspace. https://learn.microsoft.com/windows/apps/publish/
- **Xbox console:** a separate platform build and approved publishing/certification
  route are required. A PC Store listing or Windows EXE does not make an Xbox game.
  ID@Xbox: https://developer.microsoft.com/games/publish/id/welcome
- **Steam:** normal waiting/review requirements rule out next week's public release
  from a not-yet-registered account. See STEAM-RELEASE.md.

## Work sequence

| Date | Task | Evidence to retain |
| --- | --- | --- |
| 26 Oct | Build 0.6 and fix compile/test failures | Current EditMode.xml, PlayMode.xml and build.log |
| 27 Oct | Complete all career events and score modes | Results, failures with reproducible steps |
| 28 Oct | Test new UI and every circuit with keyboard/controller | 720p/1080p/1440p captures, camera/sign checks |
| 29 Oct | Test saves, updates, fresh profile and another PC | Hardware/settings and measured performance |
| 30 Oct | Fix release blockers, rebuild and retest affected paths | Updated reports, exact package and checksum |
| 31 Oct | Capture real screenshots/trailer, prepare store draft | Accurate copy, credits and support information |
| 1 Nov | Decide go/no-go on the tested package | Completed acceptance list below |

## Go/no-go checks — not yet signed off

- [ ] Current Unity EditMode and PlayMode tests pass; Windows package launches.
- [ ] All 12 career events can be completed; final screen and medal replay work.
- [ ] AI completes each track without persistent stuck loops; all modes end correctly.
- [ ] Cars and camera clear tunnels/bridges; no missing materials or unreadable signs.
- [ ] Keyboard and controller reach every menu item; scrolling and Back work.
- [ ] HUD is readable at 720p, 1080p and 1440p, including pause and result transitions.
- [ ] Existing profile keeps XP/cars/medals; new-layout records/ghosts remain separate.
- [ ] Fresh profile, repeated restarts, quit/reopen, controller reconnection and alt-tab work.
- [ ] Second-PC installation works; measured performance supports advertised requirements.
- [ ] Store description matches the shipped build; all music/assets have appropriate rights.
- [ ] Player guide, credits, contact/support route and tested ZIP are ready.

Do not mark these passed just because the standalone logic runner passed. If a
blocking issue remains, delay the public release and use a limited playtest build.
