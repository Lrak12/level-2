"""Original, deterministic synthesized score and Foley. No third-party samples.
Run with Python + NumPy. Music tails wrap across the loop boundary.
"""
from pathlib import Path
import json
import wave
import numpy as np

OUT = Path(__file__).resolve().parents[1] / 'Assets/ArtPolish/Audio'
OUT.mkdir(parents=True, exist_ok=True)
SR = 32000
rng = np.random.default_rng(32107)
report = {}

def save(name, a, peak=.78):
    a = np.asarray(a, dtype=np.float64)
    a = np.nan_to_num(a)
    a -= np.mean(a, axis=0)
    a *= peak / max(.001, np.max(np.abs(a)))
    data = (np.clip(a, -1, 1) * 32767).astype('<i2')
    with wave.open(str(OUT / (name + '.wav')), 'wb') as w:
        w.setnchannels(1 if a.ndim == 1 else a.shape[1])
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    report[name] = {'seconds': round(len(a)/SR, 3), 'peak': round(float(np.max(np.abs(a))), 3),
                    'rms': round(float(np.sqrt(np.mean(a*a))), 4)}

def tarray(d): return np.arange(int(d*SR))/SR
def hz(n): return 440 * 2 ** ((n-69)/12)
def noise(n, smooth=1):
    a = rng.normal(0, .5, n)
    if smooth > 1: a = np.convolve(a, np.ones(smooth)/smooth, mode='same')
    return a

def note(midi, dur, kind='plucked'):
    t = tarray(dur)
    f = hz(midi)
    attack = np.minimum(1, t / .014)
    if kind == 'pad':
        a = (np.sin(2*np.pi*f*t) + .22*np.sin(2*np.pi*f*2.003*t) +
             .12*np.sin(2*np.pi*f*3*t)) * (1-np.exp(-t*2.8)) * np.minimum(1, (dur-t)/.8)
        return a * .32
    if kind == 'bell':
        a = (np.sin(2*np.pi*f*t)*np.exp(-t*2.8) +
             .35*np.sin(2*np.pi*f*2.76*t)*np.exp(-t*7) +
             .13*np.sin(2*np.pi*f*5.4*t)*np.exp(-t*10))
    elif kind == 'flute':
        a = (np.sin(2*np.pi*f*t + .024*np.sin(t*31)) + .12*np.sin(2*np.pi*f*2*t))
        a *= np.minimum(1, t/.12)*np.exp(-t*1.7)
    else:
        a = (np.sin(2*np.pi*f*t) + .32*np.sin(2*np.pi*f*2*t) + .1*np.sin(2*np.pi*f*3*t))
        a *= np.exp(-t*4)
    return a * attack * np.minimum(1, (dur-t)/.025)

def drum(low=True):
    t=tarray(.65 if low else .28)
    f=64 if low else 160
    phase=2*np.pi*(f*t+34*.045*(1-np.exp(-t/.045)))
    return (np.sin(phase)*np.exp(-t*(9 if low else 21))+
            noise(len(t),4)*np.exp(-t*70)*.8)*np.minimum(1,t/.003)

def score(name, bpm, dark):
    beat=60/bpm
    bars=16
    a=np.zeros((round(bars*4*beat*SR),2))
    def add(sound, when, gain, pan=0):
        ix=(np.arange(len(sound))+round(when*SR))%len(a)
        np.add.at(a[:,0],ix,sound*gain*np.sqrt((1-pan)/2))
        np.add.at(a[:,1],ix,sound*gain*np.sqrt((1+pan)/2))
    chords=([38,41,45,50],[34,38,41,46],[36,39,43,48],[33,37,40,45]) if dark else (
            [50,53,57,60],[46,50,53,57],[53,57,60,65],[48,52,55,60])
    melody=([74,69,72,65,69,62,65,69] if dark else [74,77,81,79,77,74,72,69])
    for bar in range(bars):
        start=bar*4*beat
        chord=chords[(bar//2)%4]
        for j,n in enumerate(chord): add(note(n,4*beat+1.5,'pad'),start,.09 if dark else .07,(j-1.5)*.33)
        for b in [0,2,3.5] if dark else [0,1.5,2,3.5]:
            add(drum(True),start+b*beat,.25 if dark else .19,0)
        for b in [.75,1.75,2.75,3.25]: add(drum(False),start+b*beat,.085,-.22 if b<2 else .22)
        for b in np.arange(0,4,.5):
            t=tarray(.09)
            add(noise(len(t))*np.exp(-t*65)*np.minimum(1,t/.002),start+b*beat,.07,.4)
        for b in range(4):
            add(note(chord[0]-12,.9),start+b*beat,.13)
            if not dark or b%2==0:
                n=chord[(b+bar)%4]+12
                add(note(n,1.7,'bell'),start+(b+.25)*beat,.11,-.35)
                add(note(n,1.7,'bell'),start+(b+1)*beat,.025,.45)
        if bar%2==0:
            n=melody[(bar//2)%len(melody)]
            add(note(n,2.8,'flute'),start+beat,.105 if dark else .14,.15)
            add(note(n,2.8,'flute'),start+beat*1.75,.028,-.5)
    save(name,a,.72)

score('Coastal_Watch',100,False)
score('Ashen_Procession',84,True)

# Circular filtered noise yields continuous ambience at the exact loop boundary.
def periodic_noise(d, cutoff):
    n=int(d*SR)
    raw=rng.normal(0,1,n)
    freq=np.fft.rfftfreq(n,1/SR)
    return np.fft.irfft(np.fft.rfft(raw)/(1+(freq/cutoff)**3),n)

d=16; t=tarray(d)
surf=periodic_noise(d,800)*(0.24+.19*(.5+.5*np.sin(2*np.pi*t/8))**2)
surf+=periodic_noise(d,120)*.12
save('Coastal_Surf',np.column_stack((surf,np.roll(surf,SR//7))),.55)
rumble=periodic_noise(d,170)*(.8+.2*np.sin(2*np.pi*t/8))+.055*periodic_noise(d,1200)
save('Citadel_Rumble',np.column_stack((rumble,np.roll(rumble,SR//9))),.6)
save('Rain_Loop',periodic_noise(d,4200)*(.9+.1*np.cos(2*np.pi*t/8)),.5)
fire=periodic_noise(d,1300)*.3
for _ in range(95):
    ix=rng.integers(len(fire)); crack=noise(500)*np.exp(-np.arange(500)/60)
    np.add.at(fire,(ix+np.arange(500))%len(fire),crack*.5)
save('Fire_Loop',fire,.6)

for i in range(3):
    t=tarray(.15)
    a=noise(len(t),3)*np.exp(-t*32)+.4*np.sin(2*np.pi*92*t)*np.exp(-t*48)
    a*=np.minimum(1,t/.003)*np.minimum(1,(.15-t)/.012)
    save('Step_'+str(i+1),a,.5)
for i in range(3):
    t=tarray(.43)
    env=np.sin(np.pi*np.minimum(t/.43,1))**2*np.exp(-t*3)
    a=noise(len(t),3+i)*env+.13*np.sin(2*np.pi*(900*t-780*t*t))*env
    save('Slash_'+str(i+1),a,.7)
t=tarray(.42)
save('Blade_Impact',(noise(len(t),2)*np.exp(-t*32)+.44*np.sin(2*np.pi*1920*t)*np.exp(-t*16)+.55*np.sin(2*np.pi*110*t)*np.exp(-t*18))*np.minimum(1,t/.002),.83)
t=tarray(.5)
save('Player_Hurt',(noise(len(t),10)*.8+np.sin(2*np.pi*(100*t-25*t*t))*.65)*np.exp(-t*12)*np.minimum(1,t/.003),.7)
t=tarray(.55)
save('Dash',noise(len(t),7)*np.sin(np.pi*t/.55)**2*np.exp(-t*3),.64)
t=tarray(1.4)
save('Enemy_Down',(noise(len(t),30)*.8+np.sin(2*np.pi*(70*t-12*t*t))*.3)*np.exp(-t*5)*np.minimum(1,t/.008),.7)
t=tarray(1.8)
save('Boss_Slam',(noise(len(t),8)*.6+np.sin(2*np.pi*(52*t+3*(1-np.exp(-t*4)))))*np.exp(-t*4)*np.minimum(1,t/.003),.9)
t=tarray(1.3)
save('Boss_Warning',(np.sin(2*np.pi*(120*t+90*t*t))*.65+noise(len(t),18)*.3)*np.minimum(1,t/.15)*np.minimum(1,(1.3-t)/.06),.65)

def chime(name, notes, gap=.13, dur=2):
    a=np.zeros(int(dur*SR))
    for j,n in enumerate(notes):
        s=note(n,dur-j*gap,'bell')
        start=int(j*gap*SR); end=min(len(a),start+len(s))
        a[start:end]+=s[:end-start]
    save(name,a,.67)
chime('Checkpoint',[74,81,86])
chime('Relic',[69,76,81,88],.1,2.3)
chime('Heal',[72,76,79,84],.12,2.1)
chime('Victory',[62,69,74,77,81,86],.15,3.5)
chime('Defeat',[57,53,50,45],.28,3)
chime('UI_Click',[81],.1,.22)
(OUT/'AudioManifest.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
