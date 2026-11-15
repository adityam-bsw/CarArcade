"""Generates one distinct looping BGM per world (plus menu). Usage: python3 make_music.py OUT_DIR [id ...]
Own work: procedural chiptune, no samples. Each world differs in tempo, key, scale, timbre, drums and chords."""
import os, random, sys
from synth import *

SCALES = {
    "major": [0, 2, 4, 5, 7, 9, 11], "minor": [0, 2, 3, 5, 7, 8, 10], "dorian": [0, 2, 3, 5, 7, 9, 10],
    "phrygian": [0, 1, 3, 5, 7, 8, 10], "mixo": [0, 2, 4, 5, 7, 9, 10], "lydian": [0, 2, 4, 6, 7, 9, 11],
    "penta": [0, 2, 4, 7, 9, 11, 12], "harm": [0, 2, 3, 5, 7, 8, 11], "blues": [0, 3, 5, 6, 7, 10, 12],
}

# id: bpm, root(midi), scale, lead(kind,duty), bass kind, drums, arp kind or None, chord prog (degrees), lead density 0-1, seed
S = {
    "menu":                 (112, 57, "major",  ("square", .25), "tri",  "half",   "square", [0, 5, 3, 4], .55, 1),
    "world-rolling-hills":  (124, 60, "major",  ("square", .5),  "tri",  "four",   "square", [0, 4, 5, 3], .6, 2),
    "world-summer":         (132, 62, "mixo",   ("square", .25), "saw",  "four",   "square", [0, 3, 4, 0], .7, 3),
    "world-spring-blossom": (108, 64, "lydian", ("tri", .5),     "tri",  "soft",   "square", [0, 4, 1, 5], .5, 4),
    "world-autumn-leaves":  (96,  57, "dorian", ("tri", .5),     "tri",  "half",   "square", [0, 3, 6, 4], .45, 5),
    "world-winter":         (84,  59, "minor",  ("sine", .5),    "sine", "soft",   "tri",    [0, 5, 3, 6], .35, 6),
    "world-arctic":         (90,  55, "penta",  ("sine", .5),    "tri",  "soft",   "tri",    [0, 3, 4, 0], .4, 7),
    "world-christmas":      (116, 67, "major",  ("square", .125), "tri", "half",   "square", [0, 3, 4, 0], .75, 8),
    "world-coast":          (118, 60, "lydian", ("tri", .5),     "tri",  "break",  "square", [0, 1, 4, 0], .6, 9),
    "world-desert":         (104, 57, "phrygian", ("saw", .5),   "tri",  "gallop", "square", [0, 1, 0, 6], .55, 10),
    "world-canyon":         (112, 55, "harm",   ("square", .5),  "saw",  "gallop", None,     [0, 5, 0, 4], .5, 11),
    "world-wild-west":      (138, 52, "mixo",   ("square", .125), "tri", "gallop", None,     [0, 3, 0, 4], .65, 12),
    "world-jungle":         (126, 53, "dorian", ("square", .25), "saw",  "break",  "tri",    [0, 6, 3, 4], .6, 13),
    "world-farm":           (140, 62, "major",  ("square", .5),  "tri",  "four",   None,     [0, 3, 4, 0], .8, 14),
    "world-mars":           (118, 48, "phrygian", ("saw", .5),   "saw",  "break",  "square", [0, 1, 5, 0], .5, 15),
    "world-moon":           (80,  50, "penta",  ("sine", .5),    "sine", "soft",   "tri",    [0, 4, 5, 3], .35, 16),
    "world-industrial":     (136, 45, "minor",  ("saw", .5),     "saw",  "four",   "square", [0, 0, 5, 4], .45, 17),
    "world-night-city":     (122, 52, "dorian", ("saw", .5),     "saw",  "four",   "square", [0, 5, 3, 6], .65, 18),
    "world-synthwave":      (128, 57, "minor",  ("saw", .5),     "saw",  "four",   "square", [0, 5, 3, 6], .75, 19),
    "world-candy-land":     (146, 65, "major",  ("square", .125), "tri", "break",  "square", [0, 4, 5, 3], .85, 20),
    "world-cave":           (76,  47, "phrygian", ("tri", .5),   "sine", "soft",   "sine",   [0, 1, 0, 3], .3, 21),
    "world-cloud-kingdom":  (100, 64, "lydian", ("tri", .5),     "tri",  "soft",   "tri",    [0, 4, 5, 1], .5, 22),
    "world-haunted-halloween": (100, 50, "harm", ("square", .25), "tri", "half",   "square", [0, 5, 0, 4], .5, 23),
    "world-underwater":     (92,  55, "dorian", ("sine", .5),    "sine", "soft",   "tri",    [0, 4, 3, 5], .4, 24),
    "world-volcano":        (150, 46, "phrygian", ("saw", .5),   "saw",  "gallop", "square", [0, 1, 0, 6], .6, 25),
}

BARS = 16


def degree_note(root, scale, deg):
    sc = SCALES[scale]
    o, i = divmod(deg, 7)
    return root + sc[i] + 12 * o


def drum_pattern(style, step):
    """step in 0..15 -> list of drum names."""
    k = step % 16
    h = []
    if style == "four":
        if k % 4 == 0: h.append("K")
        if k in (4, 12): h.append("S")
        if k % 2 == 0: h.append("H")
        if k % 4 == 2: h.append("O")
    elif style == "break":
        if k in (0, 6, 10): h.append("K")
        if k in (4, 12): h.append("S")
        if k % 2 == 0: h.append("H")
    elif style == "half":
        if k in (0, 10): h.append("K")
        if k == 8: h.append("S")
        if k % 4 == 0: h.append("H")
    elif style == "gallop":
        if k in (0, 3, 4, 8, 11, 12): h.append("K")
        if k in (4, 12): h.append("S")
        if k % 2 == 1: h.append("H")
    elif style == "soft":
        if k == 0: h.append("K")
        if k == 8: h.append("S")
        if k in (4, 12): h.append("H")
    return h


def make(wid):
    bpm, root, scale, (lk, ld), bk, drums, arp, prog, dens, seed = S[wid]
    rnd = random.Random(seed * 7919)
    step_s = 60.0 / bpm / 4  # 16th note
    loop_s = BARS * 16 * step_s
    buf = Buf(loop_s + 1.5)
    kck, snr, hh, oh = kick(), snare(seed=seed), hat(0.5, False, seed), hat(0.45, True, seed + 1)

    # drums
    if drums != "none":
        for s in range(BARS * 16):
            if s < 16 and wid != "menu" and drums in ("four",) and False:
                continue
            for d in drum_pattern(drums, s):
                t = s * step_s
                if d == "K": buf.add(t, kck, 0.95)
                elif d == "S": buf.add(t, snr, 0.55)
                elif d == "H": buf.add(t, hh, 0.28 if (s % 4) else 0.4)
                elif d == "O": buf.add(t, oh, 0.25)

    # per bar chord from prog (each chord held 1 bar, prog length 4 -> repeats)
    # bass: root on beat, octave jump on off-beats
    for bar in range(BARS):
        deg = prog[bar % 4]
        bn = degree_note(root, scale, deg) - 12
        for st in range(16):
            t = (bar * 16 + st) * step_s
            if st % 4 == 0 or (st % 8 == 6):
                m = bn + (12 if st % 8 == 6 else 0) + (7 if st % 16 == 12 and bar % 2 else 0)
                dur = step_s * (3.5 if st % 4 == 0 else 1.5)
                buf.add(t, note(bk, midi_hz(m), dur, 0.55, attack=0.006, release=0.04, duty=0.5), 0.6)

    # arpeggio on chord tones (16th), softer
    if arp:
        for bar in range(BARS):
            deg = prog[bar % 4]
            tones = [degree_note(root, scale, deg + o) + 12 for o in (0, 2, 4, 7)]
            order = [0, 1, 2, 3, 2, 1, 2, 3] if bar % 2 == 0 else [0, 2, 1, 3, 0, 2, 3, 1]
            for st in range(0, 16):
                if (bar >= 4) or st % 2 == 0:
                    t = (bar * 16 + st) * step_s
                    m = tones[order[(st // (1 if bar >= 8 else 2)) % 8] % 4]
                    buf.add(t, note(arp, midi_hz(m), step_s * 0.9, 0.35, release=0.02, duty=0.25), 0.22)

    # lead: A (bars 0-3) repeats, B (4-7) answers; bars 8-15 repeat 0-7 with a high octave on the final 4
    def phrase(rs):
        notes = []
        d = rs.choice([2, 4, 7])
        for st in range(16):
            if rs.random() < dens and (st % 2 == 0 or rs.random() < 0.35):
                d = max(0, min(13, d + rs.choice([-2, -1, -1, 0, 1, 1, 2, 3])))
                notes.append((st, d, rs.choice([1, 2, 2, 3])))
        return notes

    phr = [phrase(random.Random(seed * 100 + i)) for i in range(4)]
    for bar in range(BARS):
        p = phr[(bar % 8) // 2 if bar % 8 < 4 else (bar % 8 - 4)]
        chord_deg = prog[bar % 4]
        oct_up = 12 if bar >= 12 else 0
        if bar < 2 and wid != "menu":
            continue  # two bars of rhythm only, then the melody comes in
        for (st, d, ln) in p:
            m = degree_note(root, scale, d + (chord_deg if rnd.random() < 0.3 else 0)) + 12 + oct_up
            t = (bar * 16 + st) * step_s
            buf.add(t, note(lk, midi_hz(m), step_s * ln * 0.9, 0.5, attack=0.008, release=0.06, duty=ld, vib=0.004 if lk in ("sine", "tri") else 0.0), 0.55)

    buf.fold_tail(loop_s)
    return buf


if __name__ == "__main__":
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    ids = sys.argv[2:] or list(S)
    for wid in ids:
        make(wid).save(os.path.join(out, wid + ".wav"))
        print("ok", wid, flush=True)
