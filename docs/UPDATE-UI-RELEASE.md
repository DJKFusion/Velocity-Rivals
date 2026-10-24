# 0.6.0 — menu and HUD redesign

- Widescreen menu frame with a dark gradient, diagonal graphic, cyan focus accents,
  left-aligned controls and a dedicated car showcase.
- Larger home-screen vehicle presentation with specifications, XP, next unlock,
  medal totals and the next career event.
- Scrollable pages with a visible scrollbar and automatic reveal of controller/
  keyboard selection. The canvas expands its reference area across aspect ratios.
- Consistent Escape/B back navigation, confirmation before quitting, and a visible
  retry/exit choice if saving fails.
- Separate speed/boost, race-information, lap-time and driving-hint cards. The
  gameplay cards hide during pause/results; wrong-way warning remains prominent.
- Short page fades respect the existing Reduced effects preference.
- Steam depot preparation in the Unity menu, with local VDF generation only.
  No external account action, paid registration, upload or publication is performed.

41 standalone C# checks pass, including Steam descriptor fixtures. Static syntax,
metadata and source packaging are checked. New Unity tests cover scrolling focus,
showroom lifetime across repeated page visits and HUD pause/resume. These have
not run in this cloud workspace; actual UI appearance, controller navigation and
the new executable need Windows verification before public release.

See RELEASE-WEEK.md for the release target, acceptance record and distribution
choices; STEAM-RELEASE.md documents the Steam onboarding constraint.
