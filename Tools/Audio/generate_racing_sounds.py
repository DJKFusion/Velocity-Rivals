"""Generate original synthesized racing effects; no third-party samples."""
from pathlib import Path
import wave
import numpy as np
root=Path(__file__).resolve().parents[2]/'Assets/Resources/RacingAudio'
root.mkdir(parents=True,exist_ok=True)
rate=22050
rng=np.random.default_rng(4812)
def write(name,data,loop=False):
    if loop:
        # Circular crossfade makes broadband loops join smoothly.
        n=512; data=data.copy(); blend=np.linspace(0,1,n)
        data[:n]=data[-n:]*(1-blend)+data[:n]*blend
        data=data[:-n]
    data-=np.mean(data);data=data/max(1,float(np.max(np.abs(data))))*.85
    assert np.isfinite(data).all() and np.sqrt(np.mean(data**2))>.01
    with wave.open(str(root/(name+'.wav')),'wb') as w:
        w.setparams((1,2,rate,0,'NONE','not compressed'));w.writeframes((data*32767).astype('<i2').tobytes())
def noise(n,smooth):
    return np.convolve(rng.uniform(-1,1,n),np.ones(smooth)/smooth,mode='same')
t=np.arange(rate*2)/rate
for name,freq,edge in [('EngineSport',65,.3),('EngineHeavy',44,.5),('EngineExotic',88,.18)]:
    phase=2*np.pi*freq*t+.1*np.sin(2*np.pi*7*t)
    tone=sum(np.sin(phase*h)*np.exp(-h*edge)/h**.35 for h in range(1,12))
    write(name,np.tanh(tone*1.5)*.65+noise(len(t),8)*.2,True)
write('Tyres',noise(len(t),2)*.65+np.sin(2*np.pi*1100*t+4*np.sin(2*np.pi*19*t))*.22,True)
write('Boost',noise(len(t),5)*1.3+.1*np.sin(2*np.pi*180*t),True)
write('Wind',noise(len(t),25)*2,True)
t=np.arange(int(rate*.45))/rate
write('Impact',(noise(len(t),3)*.7+np.sin(2*np.pi*58*t)*.6)*np.exp(-t*13)*np.minimum(t*400,1))
t=np.arange(int(rate*.12))/rate
write('Shift',(noise(len(t),4)*.7+np.sin(2*np.pi*110*t)*.4)*np.sin(np.pi*t/.12)**2)
for path in root.glob('*.wav'):
    with wave.open(str(path)) as w:
        data=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2')/32768
        assert w.getframerate()==rate and np.max(np.abs(data))<1
        print(path.name, 'PASS', round(len(data)/rate,2),'seconds')
