"""Original, deterministic synthwave compositions. Requires numpy and ffmpeg.

No samples, downloaded melodies or external recordings are used. Generated OGGs
are stereo 44.1 kHz; Unity imports them as streamed music. Run from any folder.
"""
from pathlib import Path
import json
import subprocess
import tempfile
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
RATE = 44100


def frequency(note):
    return 440 * 2 ** ((note - 69) / 12)


def compose(name, playlist, bpm, roots, seed, intensity):
    rng = np.random.default_rng(seed)
    beat = 60 / bpm
    duration = beat * 4 * 24  # 24 bars, including introduction and closing section.
    mix = np.zeros((round(duration * RATE), 2), dtype=np.float64)

    def add(signal, at, pan=0):
        start = round(at * RATE)
        length = min(len(signal), len(mix) - start)
        if length <= 0:
            return
        gains = np.sqrt([(1 - pan) / 2, (1 + pan) / 2])
        mix[start:start + length] += signal[:length, None] * gains

    def tone(note, seconds, level, pad=False):
        t = np.arange(round(seconds * RATE)) / RATE
        f = frequency(note)
        if pad:
            # A softly detuned, band-limited additive pad.
            signal = sum(np.sin(2 * np.pi * f * h * t) / h ** 1.8 for h in range(1, 7))
            signal += .35 * np.sin(2 * np.pi * f * 1.003 * t)
            env = np.minimum(t / .3, 1) * np.minimum((seconds - t) / .5, 1)
            pump = .68 + .32 * (1 - np.exp(-(t % beat) * 15))
        else:
            signal = np.sin(2 * np.pi * f * t) + .28 * np.sin(4 * np.pi * f * t)
            env = np.minimum(t / .008, 1) * np.exp(-t / (seconds * .35)) * np.minimum((seconds - t) / .04, 1)
            pump = 1
        return signal * env * pump * level

    for bar in range(24):
        root = roots[(bar // 2) % len(roots)]
        at = bar * 4 * beat
        full = 4 <= bar < 20
        for interval, pan in zip((0, 3, 7, 10), (-.6, .35, -.25, .6)):
            add(tone(root + 12 + interval, 4.4 * beat, .065, True), at, pan)
        for eighth in range(8):
            note = root - 12 + (12 if eighth in (3, 7) else 0)
            add(tone(note, beat * .48, .27 if full else .16), at + eighth * beat / 2, -.05)
        pattern = (0, 7, 12, 3, 10, 7, 15, 12) if seed % 2 else (12, 7, 3, 10, 7, 0, 3, 15)
        for step, interval in enumerate(pattern):
            phrase = tone(root + 24 + interval, beat * .7, .095 if full else .05)
            pan = .45 * (-1 if step % 2 else 1)
            add(phrase, at + step * beat / 2, pan)
            add(phrase * .24, at + step * beat / 2 + beat * .75, -pan)
        if bar < 2 or bar >= 23:
            continue
        for kick_at in (0, 1, 2, 3):
            t = np.arange(round(RATE * .38)) / RATE
            kick = np.sin(2 * np.pi * (46 * t + 3.5 * (1 - np.exp(-t * 28)))) * np.exp(-t * 13)
            kick *= np.minimum(t / .002, 1)
            add(kick * .5 * intensity, at + kick_at * beat)
        for snare_at in (1, 3):
            t = np.arange(round(RATE * .24)) / RATE
            noise = rng.uniform(-1, 1, len(t))
            noise = noise - np.concatenate(([0], noise[:-1])) * .65
            snare = (noise * .25 + np.sin(2 * np.pi * 185 * t) * .14) * np.exp(-t * 22)
            snare *= np.minimum(t / .002, 1)
            add(snare * intensity, at + snare_at * beat, .12)
        for hat_at in range(8 if not full else 16):
            count = 8 if not full else 16
            t = np.arange(round(RATE * .09)) / RATE
            noise = rng.uniform(-1, 1, len(t))
            high = noise - np.concatenate(([0], noise[:-1]))
            add(high * np.exp(-t * 75) * .035 * intensity, at + hat_at * 4 * beat / count, -.3)

    t = np.arange(len(mix)) / RATE
    fade = np.minimum(t / .06, 1) * np.minimum((duration - t) / (beat * 2), 1)
    mix = np.tanh(mix * 1.3) * fade[:, None]
    peak = np.abs(mix).max()
    mix *= .88 / max(peak, .001)
    assert np.isfinite(mix).all() and np.abs(mix).max() < 1
    target = ROOT / 'Assets/Resources/Music' / playlist / (name + '.ogg')
    target.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='velocity-music-') as temp:
        wav = Path(temp) / 'source.wav'
        with wave.open(str(wav), 'wb') as out:
            out.setparams((2, 2, RATE, len(mix), 'NONE', 'not compressed'))
            out.writeframes((mix * 32767).astype('<i2').tobytes())
        subprocess.run(['ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', str(wav),
                        '-c:a', 'libvorbis', '-q:a', '5', '-metadata', 'artist=Velocity Rivals',
                        '-metadata', 'title=' + name, str(target)], check=True)
    # Decode the actual delivery file, checking both channels, signal level and finite samples.
    decoded = subprocess.check_output(['ffmpeg', '-v', 'error', '-i', str(target), '-f', 'f32le', '-'])
    samples = np.frombuffer(decoded, dtype='<f4').reshape(-1, 2)
    assert np.isfinite(samples).all() and .02 < np.sqrt(np.mean(samples ** 2)) < .5
    assert np.abs(samples).max() < 1 and abs(len(samples) / RATE - duration) < .1
    return dict(track=name, playlist=playlist, bpm=bpm, seconds=round(duration, 3),
                channels=2, sample_rate=RATE, bytes=target.stat().st_size,
                decoded_peak=float(np.abs(samples).max()), passed=True)


if __name__ == '__main__':
    tracks = [
        compose('01 - Midnight Grid', 'Menu', 100, (45, 41, 48, 43), 101, .65),
        compose('01 - Neon Pursuit', 'Race', 128, (45, 48, 43, 41), 202, 1),
        compose('02 - Afterburn', 'Race', 140, (42, 45, 40, 38), 303, 1.1),
    ]
    report = ROOT / 'Tools/Audio/validation.json'
    report.write_text(json.dumps({'generator': 'Original additive synthesis; no external samples', 'tracks': tracks}, indent=2) + '\n')
    print(json.dumps(tracks, indent=2))
