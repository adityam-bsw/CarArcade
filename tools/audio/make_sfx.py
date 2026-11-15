"""Generates the sound-effect set. Usage: python3 make_sfx.py OUT_DIR. Own work (procedural), no samples."""
import os, sys, math, random
from synth import *


def seq(notes, kind="square", gap=0.0, **kw):
    """notes: list of (midi, dur). Returns one sample list."""
    out = []
    for m, d in notes:
        out += note(kind, midi_hz(m), d, release=0.02, **kw)[: int((d + gap) * SR)]
    return out


def mix(*parts):
    n = max(len(p) for p in parts)
    return [sum(p[i] for p in parts if i < len(p)) for i in range(n)]


def to(path, samples, peak=0.85):
    b = Buf(len(samples) / SR + 0.01)
    b.add(0, samples)
    b.save(path, peak)


def make(out):
    os.makedirs(out, exist_ok=True)
    p = lambda n: os.path.join(out, n + ".wav")
    r = random.Random(5)

    # --- pickups
    to(p("coin"), seq([(83, .05), (88, .16)], "square", duty=0.25, decay=6))
    to(p("gem"), seq([(88, .06), (91, .06), (95, .06), (100, .22)], "tri", decay=5))
    to(p("ink"), note("sine", 300, .22, slide=14, decay=5))
    to(p("shield"), mix(seq([(60, .08), (67, .08), (72, .08), (79, .3)], "square", duty=0.5, decay=3),
                         note("sine", 2200, .3, 0.2, slide=-6, decay=8)))
    to(p("magnet"), mix(note("saw", 120, .5, 0.5, slide=14, vib=0.03), note("sine", 480, .5, 0.4, slide=12)))
    to(p("x2"), seq([(72, .07), (72, .07), (79, .07), (84, .25)], "square", duty=0.125, decay=4))
    to(p("slowmo"), note("saw", 440, .7, 0.6, slide=-24, decay=2))
    to(p("ghost"), mix(note("sine", 880, .6, 0.5, vib=0.02, slide=7), note("sine", 1320, .6, 0.3, vib=0.025, slide=-5)))
    to(p("heart"), seq([(76, .08), (81, .08), (88, .2)], "tri"))
    to(p("chest"), seq([(60, .07), (64, .07), (67, .07), (72, .07), (76, .07), (79, .07), (84, .4)], "square", duty=0.25, decay=3))
    # --- boost / movement
    to(p("boost"), mix(noise_hit(.9, 0.8, 3.0, 0.8, 9), note("saw", 90, .9, 0.6, slide=30, decay=1.2), note("square", 180, .9, 0.2, slide=24, decay=1.5)))
    to(p("whoosh"), noise_hit(.4, 0.9, 6.0, 0.88, 11))
    to(p("flip"), seq([(64, .05), (68, .05), (71, .05), (76, .05), (80, .1)], "square", duty=0.25))
    to(p("land"), mix(kick(1.0), noise_hit(.12, 0.4, 30, 0.7, 4)))
    to(p("jump_pad"), note("square", 220, .35, 0.7, slide=24, decay=3, duty=0.25))
    to(p("ring"), mix(seq([(84, .06), (91, .2)], "sine", decay=6), note("sine", 3000, .15, 0.15, decay=14)))
    to(p("nearmiss"), mix(noise_hit(.18, 0.7, 14, 0.85, 7), note("sine", 1200, .15, 0.3, slide=-12, decay=8)))
    to(p("meteor_warn"), seq([(84, .1), (79, .1), (84, .1), (79, .1)], "square", duty=0.125))
    to(p("explosion"), mix(noise_hit(.9, 1.0, 4.5, 0.78, 21), kick(1.0), note("saw", 70, .6, 0.5, slide=-18, decay=4)))
    to(p("frenzy_on"), mix(note("saw", 110, .8, 0.6, slide=36, decay=1.2), seq([(72, .1), (76, .1), (79, .1), (84, .1), (88, .1), (91, .3)], "square", duty=0.25)))
    to(p("frenzy_off"), note("saw", 600, .6, 0.6, slide=-30, decay=2))
    to(p("combo_up"), seq([(72, .05), (76, .05), (79, .1)], "tri"))
    # --- crash / fail
    to(p("crash"), mix(noise_hit(.7, 1.0, 5, 0.6, 31), kick(1.0), note("saw", 200, .5, 0.4, slide=-20, decay=5)))
    to(p("gameover"), seq([(67, .2), (64, .2), (60, .2), (55, .5)], "saw", decay=2))
    to(p("lowink"), seq([(70, .09), (60, .09), (70, .09), (60, .09)], "square", duty=0.5))
    # --- meta / UI
    to(p("click"), note("square", 880, .04, 0.7, duty=0.25, decay=30))
    to(p("back"), note("square", 600, .06, 0.7, duty=0.25, slide=-5, decay=20))
    to(p("open"), note("square", 400, .1, 0.6, duty=0.5, slide=12, decay=10))
    to(p("upgrade"), seq([(60, .06), (64, .06), (67, .06), (72, .06), (76, .2)], "square", duty=0.25, decay=3))
    to(p("claim"), seq([(72, .07), (76, .07), (79, .07), (84, .07), (88, .07), (91, .07), (96, .4)], "square", duty=0.125, decay=2.5))
    to(p("newbest"), seq([(67, .1), (72, .1), (76, .1), (79, .1), (84, .1), (79, .08), (84, .5)], "square", duty=0.25, decay=2))
    to(p("count"), note("square", 660, .12, 0.7, duty=0.5, decay=8))
    to(p("go"), note("square", 990, .35, 0.8, duty=0.5, decay=4))
    to(p("draw"), noise_hit(.12, 0.4, 16, 0.8, 41))

    # --- engine: loopable buzz (harmonics related so the loop is seamless at 0.5 s)
    n = int(0.5 * SR)
    eng = []
    for i in range(n):
        t = i / SR
        v = 0.6 * math.sin(TAU * 60 * t) + 0.35 * math.sin(TAU * 120 * t) + 0.25 * math.sin(TAU * 180 * t + 1.2)
        v *= 1 + 0.25 * math.sin(TAU * 20 * t)
        eng.append(v)
    to(p("engine"), eng, 0.6)


if __name__ == "__main__":
    make(sys.argv[1])
    print("sfx ok")
