using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace VelocityRivals
{
    public sealed partial class SoundDirector
    {
        // Developer option: bundled songs go in Assets/Resources/Music/Menu or Music/Race.
        // Player option: drop OGG/WAV/MP3 files into these folders, then reload in Soundtrack.
        public string CustomMusicFolder => Path.Combine(Path.GetDirectoryName(SaveStore.FilePath), "CustomMusic");
        public string CustomMusicStatus { get; private set; } = "OGG / WAV / MP3 • up to 30 songs per playlist, 30 MB per file";
        readonly List<AudioClip> customClips = new List<AudioClip>();
        readonly List<AudioClip> pendingCustomClips = new List<AudioClip>();
        bool loadingCustomMusic;
        void Start() { ReloadCustomMusic(); }
        public void ReloadCustomMusic()
        {
            if (!loadingCustomMusic) StartCoroutine(LoadCustomMusic());
        }
        bool EnsureMusicFolders()
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(CustomMusicFolder,"Menu"));
                Directory.CreateDirectory(Path.Combine(CustomMusicFolder,"Race"));
                return true;
            }
            catch (Exception e) { CustomMusicStatus="Cannot open music folder: "+e.Message;return false; }
        }
        public void OpenCustomMusicFolder()
        {
            if (EnsureMusicFolders()) Application.OpenURL(new Uri(CustomMusicFolder+Path.DirectorySeparatorChar).AbsoluteUri);
        }
        IEnumerator LoadCustomMusic()
        {
            if (!EnsureMusicFolders()) yield break;
            loadingCustomMusic=true;
            CustomMusicStatus="Loading your music…";
            var loaded = pendingCustomClips;
            var menus = new List<AudioClip>(LoadTracks("Music/Menu"));
            var races = new List<AudioClip>(LoadTracks("Music/Race"));
            int skipped=0;
            foreach (string playlist in new[]{"Menu","Race"})
            {
                string[] files;
                try { files=Directory.GetFiles(Path.Combine(CustomMusicFolder,playlist));Array.Sort(files,StringComparer.OrdinalIgnoreCase); }
                catch (Exception) { skipped++;continue; }
                int accepted=0;
                foreach (string file in files)
                {
                    string extension=Path.GetExtension(file).ToLowerInvariant();
                    AudioType type=extension==".ogg"?AudioType.OGGVORBIS:extension==".wav"?AudioType.WAV:extension==".mp3"?AudioType.MPEG:AudioType.UNKNOWN;
                    if(type==AudioType.UNKNOWN)continue;
                    bool valid;
                    try { valid=accepted<30 && new FileInfo(file).Length<=30*1024*1024; }
                    catch(Exception) { valid=false; }
                    if(!valid){skipped++;continue;}
                    using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(file).AbsoluteUri,type))
                    {
                        request.timeout=15;
                        ((DownloadHandlerAudioClip)request.downloadHandler).streamAudio=true;
                        yield return request.SendWebRequest();
                        if(request.result!=UnityWebRequest.Result.Success){skipped++;continue;}
                        AudioClip clip;
                        try { clip=DownloadHandlerAudioClip.GetContent(request); }
                        catch(Exception) { skipped++;continue; }
                        if(!clip || clip.length<=.1f){if(clip)Destroy(clip);skipped++;continue;}
                        clip.name=Path.GetFileNameWithoutExtension(file);
                        loaded.Add(clip);(playlist=="Menu"?menus:races).Add(clip);accepted++;
                    }
                }
            }
            // Detach old clips before destroying them, including the outgoing crossfade source.
            foreach(var source in music){source.Stop();source.clip=null;}
            foreach(var clip in customClips)if(clip)Destroy(clip);
            customClips.Clear();customClips.AddRange(loaded);
            int loadedCount=loaded.Count;
            loaded.Clear();
            menuTracks=menus.ToArray();raceTracks=races.ToArray();
            menuOrder=new ShufflePlaylist(menuTracks.Length,Environment.TickCount);
            raceOrder=new ShufflePlaylist(raceTracks.Length,Environment.TickCount^7919);
            AdvanceTrack();loadingCustomMusic=false;
            CustomMusicStatus=loadedCount+" custom songs loaded"+(skipped>0?" • "+skipped+" skipped (format, size or limit)":"");
        }
    }
}
