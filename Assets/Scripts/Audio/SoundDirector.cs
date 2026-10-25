using UnityEngine;
using System.Collections.Generic;

namespace VelocityRivals
{
    public enum GameCue { Countdown, Go, Goal, Pickup, Impact, Knockout, Jump, Boost, Drift }
    /// <summary>Streamed menu/race playlists with crossfades and original synthesized effects.</summary>
    public sealed partial class SoundDirector : MonoBehaviour
    {
        private readonly AudioSource[] music = new AudioSource[2];
        private AudioClip[] menuTracks, raceTracks;
        private ShufflePlaylist menuOrder, raceOrder;
        private AudioClip fallback;
        private int active;
        private bool raceMusic;
        private float fade = 1, fadeDuration = 1.5f, outgoingGain, startedAt, duck = 1;
        private AudioSource effects;
        private AudioClip click;
        private AudioClip victory;
        readonly Dictionary<GameCue, AudioClip> cues = new Dictionary<GameCue, AudioClip>();
        readonly Dictionary<GameCue, float> nextCue = new Dictionary<GameCue, float>();
        public static SoundDirector Instance { get; private set; }
        public string NowPlaying => music[active] && music[active].clip ? music[active].clip.name : "";
        public string PlaylistName => raceMusic ? "Race" : "Menu";
        public int MenuTrackCount => menuTracks.Length;
        public int RaceTrackCount => raceTracks.Length;

        private void Awake()
        {
            Instance = this;
            for (int i = 0; i < music.Length; i++)
            {
                music[i] = gameObject.AddComponent<AudioSource>();
                music[i].playOnAwake = false;
                music[i].spatialBlend = 0;
                music[i].ignoreListenerPause = true;
                music[i].priority = 32;
            }
            effects = gameObject.AddComponent<AudioSource>();
            effects.spatialBlend = 0;
            effects.playOnAwake = false;
            menuTracks = LoadTracks("Music/Menu");
            raceTracks = LoadTracks("Music/Race");
            menuOrder = new ShufflePlaylist(menuTracks.Length, System.Environment.TickCount);
            raceOrder = new ShufflePlaylist(raceTracks.Length, System.Environment.TickCount ^ 7919);
            click = Tone("Menu pulse", 660, 0.08f);
            victory = Tone("Finish signal", 880, 0.7f);
            cues.Add(GameCue.Countdown, Tone("Countdown", 440, .12f));
            cues.Add(GameCue.Go, Tone("Go", 880, .35f));
            cues.Add(GameCue.Goal, Tone("Goal", 1175, .55f));
            cues.Add(GameCue.Pickup, Tone("Power pickup", 1320, .18f));
            cues.Add(GameCue.Impact, Tone("Impact", 75, .12f));
            cues.Add(GameCue.Knockout, Tone("Knockout", 45, .6f));
            cues.Add(GameCue.Jump, Tone("Jump", 520, .12f));
            cues.Add(GameCue.Boost, Tone("Boost", 160, .25f));
            cues.Add(GameCue.Drift, Tone("Tire skid", 1450, .12f));
            ApplyVolumes();
            AdvanceTrack();
        }

        private static AudioClip[] LoadTracks(string folder)
        {
            var tracks = new List<AudioClip>();
            foreach (var clip in Resources.LoadAll<AudioClip>(folder))
                if (clip != null && clip.length > .1f) tracks.Add(clip);
            tracks.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return tracks.ToArray();
        }

        private void Update()
        {
            var session = GameSession.Instance;
            bool racing = session != null && session.Running && !session.Finished;
            if (racing != raceMusic) { raceMusic = racing; AdvanceTrack(); }
            duck = Mathf.MoveTowards(duck, session != null && session.Paused ? .4f : 1, Time.unscaledDeltaTime * 2);
            fade = Mathf.Clamp01(fade + Time.unscaledDeltaTime / fadeDuration);
            if (fade >= 1 && music[1 - active].isPlaying) music[1 - active].Stop();
            ApplyMusicVolume();
            var current = music[active];
            // The grace interval lets streamed clips start and prevents a rapid retry loop if audio is unavailable.
            if (fade >= 1 && current.clip && Time.unscaledTime - startedAt > Mathf.Min(3, current.clip.length * .5f))
            {
                float transition = Mathf.Min(1.5f, current.clip.length * .2f);
                if (current.time >= current.clip.length - transition || !current.isPlaying) AdvanceTrack();
            }
        }

        public void NextTrack() { if (fade >= 1) AdvanceTrack(); }

        private void AdvanceTrack()
        {
            AudioClip[] tracks = raceMusic ? raceTracks : menuTracks;
            var order = raceMusic ? raceOrder : menuOrder;
            AudioClip clip;
            int next = order.Next();
            if (next >= 0) clip = tracks[next];
            else
            {
                if (!fallback) fallback = Music();
                clip = fallback;
            }
            outgoingGain = fade;
            active = 1 - active;
            music[active].Stop();
            music[active].clip = clip;
            music[active].volume = 0;
            fadeDuration = Mathf.Min(1.5f, clip.length * .2f);
            startedAt = Time.unscaledTime;
            fade = 0;
            music[active].Play();
        }

        private void ApplyMusicVolume()
        {
            float volume = SaveStore.Current.musicVolume * duck;
            if (music[active]) music[active].volume = volume * fade;
            if (music[1 - active]) music[1 - active].volume = volume * outgoingGain * (1 - fade);
        }

        public void ApplyVolumes()
        {
            AudioListener.volume = SaveStore.Current.masterVolume;
            ApplyMusicVolume();
            if (effects) effects.volume = SaveStore.Current.effectsVolume;
        }
        public void Click() { if (effects && click) effects.PlayOneShot(click, 0.35f); }
        public void Finish() { if (effects && victory) effects.PlayOneShot(victory, 0.5f); }
        public void Cue(GameCue cue)
        {
            if (!effects || !cues.TryGetValue(cue, out var clip)) return;
            if (nextCue.TryGetValue(cue, out float at) && Time.unscaledTime < at) return;
            nextCue[cue] = Time.unscaledTime + (cue == GameCue.Drift || cue == GameCue.Boost ? .3f : .08f);
            effects.PlayOneShot(clip, .45f);
        }

        private static AudioClip Tone(string name, float frequency, float duration)
        {
            const int rate = 22050;
            float[] samples = new float[(int)(rate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * Mathf.Sin(Mathf.PI * i / samples.Length) * 0.4f;
            }
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip Music()
        {
            const int rate = 22050;
            const float beat = 0.4f;
            float[] melody = { 220, 329.63f, 440, 293.66f, 261.63f, 392, 523.25f, 329.63f,
                174.61f, 261.63f, 349.23f, 293.66f, 196, 293.66f, 392, 329.63f };
            float[] samples = new float[Mathf.RoundToInt(rate * beat * 32)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                int step = Mathf.FloorToInt(t / beat);
                float phase = t % beat;
                float envelope = Mathf.Min(phase * 100, 1) * Mathf.Exp(-phase * 9);
                float note = melody[step % melody.Length];
                float bass = Mathf.Sin(2 * Mathf.PI * note * 0.25f * t) * 0.08f;
                float synth = (Mathf.Sin(2 * Mathf.PI * note * t) + 0.25f * Mathf.Sin(2 * Mathf.PI * note * 2 * t)) * envelope * 0.12f;
                float kick = step % 2 == 0 ? Mathf.Sin(2 * Mathf.PI * (48 * phase + 4 * (1 - Mathf.Exp(-phase * 30)))) * Mathf.Exp(-phase * 20) * 0.2f : 0;
                // Loop-edge fade avoids a click without requiring an imported music asset.
                float fade = Mathf.Min(1, Mathf.Min(t, samples.Length / (float)rate - t) * 50);
                samples[i] = (bass + synth + kick) * fade;
            }
            AudioClip clip = AudioClip.Create("Velocity original placeholder loop", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            foreach (var source in music) if (source) { source.Stop(); source.clip = null; Destroy(source); }
            if (effects) { effects.Stop(); Destroy(effects); }
            // Imported assets are shared; only destroy clips created by this component.
            if (fallback) Destroy(fallback);
            if (click) Destroy(click);
            if (victory) Destroy(victory);
            foreach (var clip in cues.Values) if (clip != null) Destroy(clip);
            foreach (var clip in customClips) if (clip) Destroy(clip);
            foreach (var clip in pendingCustomClips) if (clip) Destroy(clip);
            if (Instance == this) Instance = null;
        }
    }
}
