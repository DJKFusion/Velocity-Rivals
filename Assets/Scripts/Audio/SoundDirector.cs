using UnityEngine;
using System.Collections.Generic;

namespace VelocityRivals
{
    public enum GameCue { Countdown, Go, Goal, Pickup, Impact, Knockout, Jump, Boost, Drift }
    /// <summary>Original synthesized placeholder music and UI cues; no downloaded audio.</summary>
    public sealed class SoundDirector : MonoBehaviour
    {
        private AudioSource music;
        private AudioSource effects;
        private AudioClip click;
        private AudioClip victory;
        readonly Dictionary<GameCue, AudioClip> cues = new Dictionary<GameCue, AudioClip>();
        readonly Dictionary<GameCue, float> nextCue = new Dictionary<GameCue, float>();
        public static SoundDirector Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            music = gameObject.AddComponent<AudioSource>();
            effects = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.spatialBlend = effects.spatialBlend = 0;
            music.clip = Music();
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
            music.Play();
        }

        public void ApplyVolumes()
        {
            AudioListener.volume = SaveStore.Current.masterVolume;
            if (music) music.volume = SaveStore.Current.musicVolume;
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
            if (music && music.clip) Destroy(music.clip);
            if (click) Destroy(click);
            if (victory) Destroy(victory);
            foreach (var clip in cues.Values) if (clip != null) Destroy(clip);
            if (Instance == this) Instance = null;
        }
    }
}
