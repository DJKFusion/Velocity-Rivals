# Add music to Velocity Rivals

## The included soundtrack

Twelve original electronic arrangements are included, totaling about 18.3 minutes. Each has a different groove, lead instrument and motif; these replace the previous arrangements in the same project.

| Track | Playlist | Style / lead | Tempo | Length |
| --- | --- | --- | --- | --- |
| Midnight Grid | Menu | halftime / glass | 92 BPM | 104.3s |
| Skyline Dreams | Menu | ambient / bell | 78 BPM | 98.5s |
| Solar Drift | Menu | garage / keys | 112 BPM | 85.7s |
| Electric Afterglow | Menu | downtempo / soft | 100 BPM | 96.0s |
| Neon Pursuit | Race | outrun / brass | 126 BPM | 91.4s |
| Afterburn | Race | breakbeat / acid | 140 BPM | 82.3s |
| Hyperdrive | Race | dnb / pulse | 172 BPM | 89.3s |
| Chrome Horizon | Race | house / keys | 122 BPM | 94.4s |
| Ion Storm | Race | industrial / metal | 136 BPM | 84.7s |
| Final Overdrive | Race | trance / saw | 146 BPM | 92.1s |
| Nightshift Run | Race | electro / pluck | 130 BPM | 88.6s |
| Apex Signal | Race | bigbeat / fifth | 150 BPM | 89.6s |

Open **Soundtrack** from the home menu or Settings. Preview either playlist without starting an event, see playback progress, or select **Next Track**. Press **N** to skip during menus or driving. Leaving Soundtrack restores the appropriate menu/race playlist. A brief now-playing card appears while driving; the menu footer also names the track.

Playlists shuffle without repeating until their cycle ends, crossfade, and duck during pause. Custom music support and saved volume controls remain available. The new arrangements passed decoded-file signal checks; listening balance and Unity playback still require a Windows test.

## Add songs in the built game — no rebuild

1. Open **Settings → Soundtrack → Open Custom Music Folder**.
2. Put OGG, WAV or MP3 files in its **Menu** or **Race** subfolder.
3. Return to the game and choose **Reload Custom Music**. Songs mix with the supplied soundtrack.

The normal Windows location is `%USERPROFILE%\AppData\LocalLow\DJKFusion\Velocity Rivals\CustomMusic`.
Files are loaded at launch too. Each playlist accepts up to 30 custom songs, up to
30 MB per file. Unsupported or unreadable songs are skipped and the soundtrack
page reports how many loaded. Remove a song from the folder and reload to remove
it from the playlist. Personal songs are local to that computer; they are not
automatically included in builds you distribute.

The implementation and developer comments are in
`Assets/Scripts/Audio/SoundDirector.CustomMusic.cs`. `CustomMusicFolder` determines
the location; `ReloadCustomMusic()` reloads the playlists. UnityWebRequest loads
local file URLs only; this feature does not download music from websites.

## Bundle your own songs in the project

1. Open the updated project in Unity and stop Play Mode.
2. Choose **Velocity Rivals → Music → Import Race Track** (or **Import Menu Track**).
3. Choose an MP3, WAV, OGG, AIF or AIFF file on your computer. Unity copies it into the project and prepares it for streaming.
4. Wait for import to finish, then choose **Velocity Rivals → Play Game**. Start an event to hear race music. The soundtrack page lets you skip to another track after the current transition finishes.

Repeat for additional songs. The original file on your computer is left intact; importing the same filename again creates a second copy. Rename the imported asset in Unity to change the displayed song title.

Alternatively, drag audio files into **Assets → Resources → Music → Menu** or **Race** in Unity's Project panel. You can select several files that way. Subfolders under either playlist are supported. Keep audio inside one of those two playlists; files directly under `Music` are not selected.

To use only your music, remove the supplied tracks from the playlists **in Unity's Project panel**, preserving any originals you want to keep. Restart Play Mode after adding/removing songs. An empty playlist uses the original synthesized fallback loop, so the game still works.

## Include music in the downloadable game

After importing songs in Unity, use **Velocity Rivals → Build Windows ZIP**. Imported music is bundled automatically. For personal songs without rebuilding, use the CustomMusic folders above. Use songs you own or have permission to distribute when sharing a build.

## If it is silent

- Check your Windows output device and Unity Game view's Mute Audio button.
- Check both Master Volume and Music Volume in Settings.
- Race songs start in events; menu songs play on menus and event results.
- Confirm the file appears as an AudioClip inside `Resources/Music/Menu` or `Race`, then stop and restart Play Mode.
- If Unity reports an import/compile error, fix the first red Console error before testing playback.

## Source and checks

`Tools/Audio/generate_soundtrack.py` creates the twelve compositions using additive synthesis, generated percussion and deterministic random seeds. No external samples or recordings are used. It requires Python with NumPy and FFmpeg with libvorbis. Regenerating overwrites the supplied tracks and `Tools/Audio/validation.json`; custom songs with other filenames are unaffected. `--extra` regenerates only Electric Afterglow, Nightshift Run and Apex Signal.

The generator decodes the actual OGG deliveries and checks duration, stereo samples, finite values, audible signal level and peak below 1. All twelve passed. This is file validation, not a listening review or Unity playback test. Imported clips are streamed; only the two crossfade sources play concurrently.
