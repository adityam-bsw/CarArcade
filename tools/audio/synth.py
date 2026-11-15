"""Tiny chiptune synth (pure Python, no dependencies). Own work, no samples, no generative AI.
Used by make_music.py and make_sfx.py. All output is mono 16-bit WAV."""
import math, random, struct, wave

SR = 22050
TAU = math.pi * 2


def midi_hz(m):
    return 440.0 * 2 ** ((m - 69) / 12.0)


def osc(kind, ph, duty=0.5):
    """ph in [0,1)."""
    if kind == "square":
        return 1.0 if ph < duty else -1.0
    if kind == "saw":
        return 2.0 * ph - 1.0
    if kind == "tri":
        return 4.0 * ph - 1.0 if ph < 0.5 else 3.0 - 4.0 * ph
    if kind == "sine":
        return math.sin(TAU * ph)
    return 0.0


class Buf:
    def __init__(self, seconds):
        self.n = int(seconds * SR)
        self.d = [0.0] * self.n

    def add(self, start_s, samples, gain=1.0):
        i0 = int(start_s * SR)
        d = self.d
        n = self.n
        for k, v in enumerate(samples):
            j = i0 + k
            if j >= n:
                break
            d[j] += v * gain

    def fold_tail(self, loop_s):
        """Wrap anything after loop_s back to the start so the loop is seamless."""
        L = int(loop_s * SR)
        for j in range(L, self.n):
            self.d[j - L] += self.d[j]
        self.d = self.d[:L]
        self.n = L

    def save(self, path, peak=0.85):
        m = max(1e-9, max(abs(x) for x in self.d))
        g = peak / m
        with wave.open(path, "wb") as w:
            w.setnchannels(1)
            w.setsampwidth(2)
            w.setframerate(SR)
            w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, x * g)) * 32767)) for x in self.d))


def note(kind, hz, dur, vol=1.0, attack=0.004, release=0.05, duty=0.5, vib=0.0, slide=0.0, decay=0.0):
    """Render one note. slide = total semitone change over the note, decay = exp amplitude decay rate (1/s)."""
    n = int((dur + release) * SR)
    out = []
    ph = 0.0
    for i in range(n):
        t = i / SR
        f = hz * 2 ** ((slide * t / max(dur, 1e-3)) / 12.0) if slide else hz
        if vib:
            f *= 1 + vib * math.sin(TAU * 6.0 * t)
        ph = (ph + f / SR) % 1.0
        env = 1.0
        if t < attack:
            env = t / attack
        if decay:
            env *= math.exp(-decay * t)
        if t > dur:
            env *= max(0.0, 1 - (t - dur) / release)
        out.append(osc(kind, ph, duty) * env * vol)
    return out


def noise_hit(dur, vol=1.0, decay=40.0, lp=0.0, seed=1):
    """Decaying noise. lp in (0,1): one-pole low-pass amount (higher = darker)."""
    r = random.Random(seed)
    n = int(dur * SR)
    out = []
    y = 0.0
    for i in range(n):
        x = r.uniform(-1, 1)
        y = y * lp + x * (1 - lp) if lp else x
        out.append(y * math.exp(-decay * i / SR) * vol)
    return out


def kick(vol=1.0):
    n = int(0.16 * SR)
    out = []
    ph = 0.0
    for i in range(n):
        t = i / SR
        f = 45 + 110 * math.exp(-t * 35)
        ph += f / SR
        out.append(math.sin(TAU * ph) * math.exp(-t * 16) * vol)
    return out


def snare(vol=1.0, seed=2):
    nz = noise_hit(0.14, vol * 0.8, 26, 0.3, seed)
    body = note("tri", 190, 0.06, vol * 0.5, release=0.04, decay=30)
    return [a + (body[i] if i < len(body) else 0) for i, a in enumerate(nz)]


def hat(vol=0.5, open_=False, seed=3):
    return noise_hit(0.12 if open_ else 0.035, vol, 14 if open_ else 90, 0.0, seed)
