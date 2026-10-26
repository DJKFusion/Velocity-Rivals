namespace VelocityRivals
{
    /// <summary>
    /// Developer-owned playlists. Put audio under Assets/Resources, then list its
    /// Resources-relative path below, without the extension. Rebuild to ship changes.
    /// Example: Assets/Resources/Music/Custom/MySong.ogg -> "Music/Custom/MySong".
    /// Remove entries to replace the supplied soundtrack; add entries to extend it.
    /// No player-folder scanning, downloads or in-game importing occurs.
    /// </summary>
    public static class MusicLibrary
    {
        public static readonly string[] MenuTracks = {
            "Music/Menu/01 - Midnight Grid",
            "Music/Menu/02 - Skyline Dreams",
            "Music/Menu/03 - Solar Drift",
            "Music/Menu/04 - Electric Afterglow",
            // "Music/Custom/MyMenuSong",
        };
        public static readonly string[] RaceTracks = {
            "Music/Race/01 - Neon Pursuit",
            "Music/Race/02 - Afterburn",
            "Music/Race/03 - Hyperdrive",
            "Music/Race/04 - Chrome Horizon",
            "Music/Race/05 - Ion Storm",
            "Music/Race/06 - Final Overdrive",
            "Music/Race/07 - Nightshift Run",
            "Music/Race/08 - Apex Signal",
            // "Music/Custom/MyRaceSong",
        };
    }
}
