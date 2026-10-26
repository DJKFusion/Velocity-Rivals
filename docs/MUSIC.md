# Developer-owned music playlists

Players can adjust volume, skip and preview playlists. There is **no in-game music import or folder loader**. Existing player CustomMusic folders are ignored and left untouched.

## Put your songs in the game

1. Open the Unity project and stop Play Mode.
2. Put your OGG/WAV/MP3 files in `Assets/Resources/Music/Custom/`. The existing Unity **Velocity Rivals → Music → Import Menu/Race Track** commands can also copy/import a song into a project playlist folder.
3. Edit `Assets/Scripts/Audio/MusicLibrary.cs`. Add the Resources-relative path, without the extension, to `MenuTracks` or `RaceTracks`:

```csharp
public static readonly string[] MenuTracks = {
    "Music/Custom/MyMenuSong",
};
public static readonly string[] RaceTracks = {
    "Music/Custom/MyRaceSong",
    "Music/Custom/MySecondRaceSong",
};
```

For example, `Assets/Resources/Music/Custom/MyRaceSong.ogg` becomes `"Music/Custom/MyRaceSong"`. Paths and capitalization must match. Keep the supplied entries to mix your songs with the originals, or replace those entries to play only your music. Merely copying a song into the folder no longer adds it to playback: the code list is authoritative.

4. Let Unity compile/import, then audition **Soundtrack → Preview Menu/Race Playlist**. The music importer prepares assets under `Resources/Music` for streaming.
5. Close Unity and run `Build-SinglePlayer.cmd` to bundle the songs. The build guard checks every listed path and requires at least one valid track per playlist. Changes require a new build.

Unlisted files under Resources can still be included by Unity in the build even though they are not played. If you want to remove those files from the package, remove them from the project through Unity after backing them up. Only distribute songs you have rights to use.

Playlists retain shuffle, crossfades, pause ducking, volume settings and N-to-skip. Imported project assets are shared and are not destroyed when the music director closes.

## Supplied soundtrack

Twelve original electronic arrangements (~18.3 minutes) remain the default until you edit the code list.

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
