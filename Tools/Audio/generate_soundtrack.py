"""Twelve original arrangements, synthesized without samples; numpy + ffmpeg required.

Each score has its own groove, instrument, harmony and motif. Deterministic seeds
make exports reproducible. Existing filenames/GUIDs preserve Unity references.
"""
from pathlib import Path
import json
import subprocess
import tempfile
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
RATE = 44100
# name, playlist, bpm, roots, chord, lead, groove, motif, bars
SCORES = [
 ('01 - Midnight Grid', 'Menu', 92, (45,41,48,43), (0,3,7,10), 'glass', 'halftime', (0,7,10,14,12,7,3,2), 40),
 ('02 - Skyline Dreams', 'Menu', 78, (48,43,45,41), (0,4,7,11), 'bell', 'ambient', (12,7,4,19,16,14,7,4), 32),
 ('03 - Solar Drift', 'Menu', 112, (41,46,48,43), (0,4,7,9), 'keys', 'garage', (0,4,7,9,7,4,12,11), 40),
 ('04 - Electric Afterglow', 'Menu', 100, (48,45,41,43), (0,4,7,9), 'soft', 'downtempo', (7,4,2,0,12,9,7,4), 40),
 ('01 - Neon Pursuit', 'Race', 126, (45,41,48,43), (0,3,7), 'brass', 'outrun', (0,0,7,10,12,10,7,3), 48),
 ('02 - Afterburn', 'Race', 140, (42,40,38,45), (0,3,7), 'acid', 'breakbeat', (0,12,0,7,3,0,10,7), 48),
 ('03 - Hyperdrive', 'Race', 172, (42,38,45,40), (0,3,7,10), 'pulse', 'dnb', (12,10,7,3,0,7,12,15), 64),
 ('04 - Chrome Horizon', 'Race', 122, (47,50,43,45), (0,4,7,9), 'keys', 'house', (0,4,7,11,14,11,7,4), 48),
 ('05 - Ion Storm', 'Race', 136, (40,41,47,38), (0,1,7), 'metal', 'industrial', (0,1,0,7,0,12,7,1), 48),
 ('06 - Final Overdrive', 'Race', 146, (45,41,43,48), (0,3,7,10), 'saw', 'trance', (12,7,10,15,14,10,7,3), 56),
 ('07 - Nightshift Run', 'Race', 130, (43,46,41,48), (0,3,7,10), 'pluck', 'electro', (0,7,12,7,10,3,7,2), 48),
 ('08 - Apex Signal', 'Race', 150, (46,41,44,39), (0,3,7), 'fifth', 'bigbeat', (0,0,12,10,7,3,7,10), 56),
]
GROOVES = {
 'ambient': ((0,), (), (2.5,)),
 'halftime': ((0,2.75), (2,), (0,.75,1.5,2.5,3.25)),
 'garage': ((0,1.75,2.5), (1,3), (.5,1.25,1.75,2.5,3.25,3.75)),
 'downtempo': ((0,1.5,2.75), (1,3), (.5,1.5,2.5,3.5)),
 'outrun': ((0,1,2,3), (1,3), tuple(np.arange(0,4,.5))),
 'breakbeat': ((0,1.5,2.75), (1,3), (0,.5,.75,1.5,2,2.5,3.5,3.75)),
 'dnb': ((0,1.75,2.5), (1,3), tuple(np.arange(0,4,.25))),
 'house': ((0,1,2,3), (1,3), (.5,1.5,2.5,3.5)),
 'industrial': ((0,.75,2,2.5), (1.5,3), (0,.25,1,2,2.25,3.5)),
 'trance': ((0,1,2,3), (1,3), tuple(np.arange(.5,4,.5))),
 'electro': ((0,1.75,2.5), (1,3), (0,.75,1.5,2.25,3,3.75)),
 'bigbeat': ((0,.5,2,2.75), (1,3), (0,.5,1.5,2,2.5,3.25,3.5)),
}


def compose(score, seed):
    name, playlist, bpm, roots, chord, instrument, groove, motif, bars = score
    rng = np.random.default_rng(seed)
    beat = 60 / bpm
    duration = beat * 4 * bars
    mix = np.zeros((round(duration * RATE), 2), dtype=np.float64)

    def add(signal, at, pan=0):
        start = round(at * RATE)
        length = min(len(signal), len(mix)-start)
        if length > 0:
            mix[start:start+length] += signal[:length,None] * np.sqrt([(1-pan)/2,(1+pan)/2])

    def tone(note, seconds, kind, level):
        t = np.arange(round(seconds*RATE))/RATE
        f = 440 * 2**((note-69)/12)
        phase = 2*np.pi*f*t
        attack, decay = .008, seconds*.55
        if kind == 'pad':
            signal = np.sin(phase)+.35*np.sin(phase*1.003)+.18*np.sin(phase*2)
            attack, decay = .25, seconds*4
        elif kind in ('bell','glass'):
            ratio = 2.76 if kind == 'bell' else 3.5
            signal = np.sin(phase + 2.2*np.exp(-t*7)*np.sin(phase*ratio))
            decay = seconds*.4
        elif kind == 'keys':
            signal = np.sin(phase)+.45*np.sin(phase*2)*np.exp(-t*9)+.15*np.sin(phase*3)
        elif kind in ('acid','metal'):
            signal = np.sin(phase + (4 if kind=='metal' else 2)*np.exp(-t*5)*np.sin(phase*(1 if kind=='acid' else 1.41)))
            signal = np.tanh(signal*1.8)*.65
        elif kind in ('brass','saw','pulse','fifth'):
            signal = np.zeros_like(t)
            for h in range(1,13):
                if f*h >= RATE*.45: break
                if kind=='pulse' and h%2==0: continue
                signal += np.sin(phase*h)/h**(1.2 if kind=='saw' else 1.65)
            if kind=='fifth': signal += .45*np.sin(phase*1.5)
            if kind=='brass': attack=.055
        elif kind == 'bass':
            signal = np.sin(phase)+.25*np.sin(phase*2)+.1*np.sin(phase*3)
        else:
            signal = np.sin(phase)+.2*np.sin(phase*3)
            decay = seconds*(.22 if kind=='pluck' else .7)
        envelope = np.minimum(t/attack,1)*np.exp(-t/decay)*np.clip((seconds-t)/.04,0,1)
        return signal*envelope*level

    kick_t=np.arange(round(RATE*.4))/RATE
    kick=np.sin(2*np.pi*(44*kick_t+4*(1-np.exp(-kick_t*30))))*np.exp(-kick_t*13)*np.minimum(kick_t/.002,1)
    for bar in range(bars):
        at=bar*4*beat
        root=roots[(bar//2)%len(roots)]
        intro=bar<4
        breakdown=bars//2 <= bar < bars//2+4
        outro=bar>=bars-4
        full=not (intro or breakdown or outro)
        # Long harmony, syncopated chord stabs, or sparse atmosphere by arrangement.
        chord_stabs=groove in ('house','garage','electro')
        for interval,pan in zip(chord,(-.65,.45,-.25,.65)):
            for offset in ((.5,2,3.5) if chord_stabs else (0,)):
                add(tone(root+12+interval,beat*(.6 if chord_stabs else 4.3), 'keys' if chord_stabs else 'pad', .055),at+offset*beat,pan)
        if not breakdown:
            bass_steps = (0,2.5) if groove=='ambient' else (.5,1.5,2.5,3.5) if groove in ('house','trance') else (0,.75,1.5,2.5,3.25) if groove in ('garage','breakbeat','electro') else tuple(np.arange(0,4,.5))
            for step in bass_steps:
                add(tone(root-12+(12 if step==3.5 else 0),beat*.65,'bass',.21 if full else .12),at+step*beat)
        # Phrase rests and an answering second phrase avoid a continuous identical arpeggio.
        if bar%4 != 3 or full:
            steps=4 if groove in ('ambient','halftime','downtempo') else 8
            for step in range(steps):
                if (bar+step)%7==6: continue
                interval=motif[(step+(4 if bar%4>=2 else 0))%len(motif)]
                note=root+12+interval+(12 if groove=='trance' and bar%8>=4 else 0)
                seconds=beat*(1.8 if steps==4 else .65)
                signal=tone(note,seconds,instrument,.09 if full else .065)
                offset=step*4/steps + (.08 if groove=='garage' and step%2 else 0)
                add(signal,at+offset*beat,.28)
                add(signal*.24,at+(offset+.75)*beat,-.45)
        if intro or outro or breakdown:
            if groove!='ambient' and bar%2==0: add(kick*.22,at)
            continue
        kicks,snares,hats=GROOVES[groove]
        for step in kicks: add(kick*(.48 if playlist=='Race' else .3),at+step*beat)
        for step in snares:
            t=np.arange(round(RATE*.22))/RATE
            noise=rng.uniform(-1,1,len(t));noise=np.diff(noise,prepend=0)
            snare=(noise*.2+np.sin(2*np.pi*(170 if groove!='industrial' else 240)*t)*.2)*np.exp(-t*24)*np.minimum(t/.002,1)
            add(snare*(.8 if playlist=='Race' else .5),at+step*beat,.1)
        for step in hats:
            t=np.arange(round(RATE*.1))/RATE
            noise=rng.uniform(-1,1,len(t));noise=np.diff(noise,prepend=0)
            add(noise*np.exp(-t*65)*.032,at+step*beat,-.4 if int(step*4)%2 else .4)
        # End-of-phrase percussion fill; no borrowed drum loops.
        if bar%8==7:
            for step in (3.25,3.5,3.75):
                add(tone(48+int(step*4),beat*.2,'metal',.09),at+step*beat)

    t=np.arange(len(mix))/RATE
    fade=np.minimum(t/.08,1)*np.clip((duration-t)/(beat*4),0,1)
    mix=np.tanh(mix*1.2)*fade[:,None]
    mix*=.84/max(np.abs(mix).max(),.001)
    target=ROOT/'Assets/Resources/Music'/playlist/(name+'.ogg')
    with tempfile.TemporaryDirectory(prefix='velocity-music-') as temp:
        wav=Path(temp)/'source.wav'
        with wave.open(str(wav),'wb') as out:
            out.setparams((2,2,RATE,len(mix),'NONE','not compressed'))
            out.writeframes((mix*32767).astype('<i2').tobytes())
        subprocess.run(['ffmpeg','-v','error','-y','-i',str(wav),'-c:a','libvorbis','-q:a','5','-metadata','artist=Velocity Rivals','-metadata','title='+name,str(target)],check=True)
    decoded=subprocess.check_output(['ffmpeg','-v','error','-i',str(target),'-f','f32le','-'])
    samples=np.frombuffer(decoded,dtype='<f4').reshape(-1,2)
    rms=float(np.sqrt(np.mean(samples**2)))
    assert np.isfinite(samples).all() and .02<rms<.5 and np.abs(samples).max()<1
    assert abs(len(samples)/RATE-duration)<.1
    result=dict(track=name,playlist=playlist,bpm=bpm,seconds=round(duration,3),style=groove,instrument=instrument,
                channels=2,sample_rate=RATE,bytes=target.stat().st_size,decoded_peak=float(np.abs(samples).max()),decoded_rms=rms,passed=True)
    print(f'{name}: {groove}, {instrument}, {bpm} BPM',flush=True)
    return result


if __name__=='__main__':
    tracks=[compose(score,101+i*113) for i,score in enumerate(SCORES)]
    report=ROOT/'Tools/Audio/validation.json'
    report.write_text(json.dumps({'generator':'Original multi-style synthesis; no external samples','tracks':tracks},indent=2)+'\n')
