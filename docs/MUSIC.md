# Add music to Velocity Rivals

## The included soundtrack

Nine original electronic/synthwave tracks are included, totaling about 11.4 minutes:

| Track | Used in | Tempo | Length |
| --- | --- | --- | --- |
| Midnight Grid | Menus | 100 BPM | 57.6 seconds |
| Neon Pursuit | Events | 128 BPM | 45 seconds |
| Afterburn | Events | 140 BPM | 41.1 seconds |
| Skyline Dreams | Menus | 96 BPM | 100 seconds |
| Solar Drift | Menus | 88 BPM | 109.1 seconds |
| Hyperdrive | Events | 150 BPM | 76.8 seconds |
| Chrome Horizon | Events | 124 BPM | 92.9 seconds |
| Ion Storm | Events | 138 BPM | 83.5 seconds |
| Final Overdrive | Events | 144 BPM | 80 seconds |

The game selects the menu or race playlist automatically, crossfades between tracks, shuffles each playlist without consecutive repeats when it has multiple tracks, and lowers music during pause. In-game **Settings → Soundtrack** shows the current song and offers **Next Track**. Music volume is saved in your profile. These integrations still require an audible test in Unity and the Windows player.

## Add your own song — no coding

1. Open the updated project in Unity and stop Play Mode.
2. Choose **Velocity Rivals → Music → Import Race Track** (or **Import Menu Track**).
3. Choose an MP3, WAV, OGG, AIF or AIFF file on your computer. Unity copies it into the project and prepares it for streaming.
4. Wait for import to finish, then choose **Velocity Rivals → Play Game**. Start an event to hear race music. The soundtrack page lets you skip to another track after the current transition finishes.

Repeat for additional songs. The original file on your computer is left intact; importing the same filename again creates a second copy. Rename the imported asset in Unity to change the displayed song title.

Alternatively, drag audio files into **Assets → Resources → Music → Menu** or **Race** in Unity's Project panel. You can select several files that way. Subfolders under either playlist are supported. Keep audio inside one of those two playlists; files directly under `Music` are not selected.

To use only your music, remove the supplied tracks from the playlists **in Unity's Project panel**, preserving any originals you want to keep. Restart Play Mode after adding/removing songs. An empty playlist uses the original synthesized fallback loop, so the game still works.

## Include music in the downloadable game

After adding songs, use **Velocity Rivals → Build Windows ZIP**. Imported music is bundled automatically. Copying songs beside an already-built EXE does not add them; rebuild after changing the playlist. Use songs you own or have permission to distribute when sharing a build.

## If it is silent

- Check your Windows output device and Unity Game view's Mute Audio button.
- Check both Master Volume and Music Volume in Settings.
- Race songs start in events; menu songs play on menus and event results.
- Confirm the file appears as an AudioClip inside `Resources/Music/Menu` or `Race`, then stop and restart Play Mode.
- If Unity reports an import/compile error, fix the first red Console error before testing playback.

## Source and checks

`Tools/Audio/generate_soundtrack.py` creates the nine compositions using additive synthesis, generated percussion and a deterministic random seed. No external samples or recordings are used. It requires Python with NumPy and FFmpeg with libvorbis. Regenerating overwrites the nine named supplied tracks and `Tools/Audio/validation.json`; custom songs with other filenames are unaffected. `--expand` preserves the first three and regenerates only the six additions.

The generator decodes the actual OGG deliveries and checks duration, stereo samples, finite values, audible signal level and peak below 1. All nine passed. This is file validation, not a listening review or Unity playback test. Imported clips are streamed; only the two crossfade sources play concurrently.
