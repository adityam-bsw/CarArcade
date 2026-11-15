#!/usr/bin/env python3
"""Smooth road strips, batch 2 (same generator and conventions as make_road.py). Own code, no generative AI, no downloads.

    python3 tools/art/make_road2.py --output <dir> [--surface sand|snow|gravel|cobblestone|moon_dust|neon_grid|wooden_bridge|rail_track|all]

1024 x 192, seamless in X, rounded top lip, skirt fading by gradient. Each surface differs in silhouette AND texture:
  sand      long soft dune lip, wind ripples, grain          snow      puffy white drifts over frozen earth, scalloped underside
  gravel    lip of protruding pebbles, dense angular stones  cobblestone  running-bond rounded stones, mortar, scalloped lip
  moon_dust crater dips in the lip, crater rims, boulders    neon_grid  flat slab, glowing cyan/magenta edge, LED caps, grid
  wooden_bridge  separate deck planks with gaps, beam, posts rail_track  steel rail on sleepers over ballast
Writes road_<name>_1024x192.png (+ colour bleed) and a 3-tile preview. Measure lip rows with check_roads2.py.
"""
import argparse, math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from smoothart import *
from make_road import W, H, SS, INK, periodic_dist, strip_cov, column_cache, mottled, lip_shadow, stones, skirt_fade, preview

def blob(rnd, cx, cy, rx, ry, n=None, j=0.22):
    n = n or rnd.choice((5, 6, 7)); a0 = rnd.uniform(0, 6.28)
    return [(cx + rx * (1 + rnd.uniform(-j, j)) * math.cos(a0 + 6.2832 * k / n), cy + ry * (1 + rnd.uniform(-j, j)) * math.sin(a0 + 6.2832 * k / n)) for k in range(n)]

def bumps_fn(base, items, shape=1.0):
    """items: (x, half width, height) bumps raised above base (y decreases). Round pebble-like profile."""
    def f(x):
        t = base
        for bx, hw, hh in items:
            d = periodic_dist(x, bx)
            if d < hw: t -= hh * math.sqrt(max(0.0, 1 - (d / hw) ** 2)) ** shape
        return t
    return f

# ------------------------------------------------------------------------------------------------------------------- sand
def sand():
    top = column_cache(lambda x: 7.0 + 2.6 * wave_fbm(x, W, 71, octaves=2, base=2) + 1.0 * wave_fbm(x, W, 72, octaves=2, base=5))
    img = Image(W, H); rnd = random.Random(101)
    ramp = Ramp([(0, "#FFF4CF"), (3, "#F4D28A"), (14, "#E6B76A"), (26, "#C98A3C"), (44, "#CC9144"), (100, "#B27A34"), (150, "#9A6A30"), (192, "#8A5E2D")])
    img.paint(strip_cov(top), mottled(ramp, top, 0.05, 0.04, 75))
    # wind ripples: long gently sine-bent crests, light above / shadowed below (the sand signature)
    lt = Mask(W, H, SS, True); dk = Mask(W, H, SS, True)
    for k in range(22):
        x0 = rnd.uniform(0, W); y0 = rnd.uniform(30, 132); L = rnd.uniform(110, 260); amp = rnd.uniform(2, 4.5); ph = rnd.uniform(0, 6.28)
        pts = [(x0 + L * i / 14, y0 + amp * math.sin(ph + 3.0 * i / 14 * 2)) for i in range(15)]
        dk.polyline([(x, y + 1.9) for x, y in pts], 1.5); lt.polyline(pts, 0.9)
    img.paint(dk.cov().blur(1, 2, True), "#A8733A", 0.55)
    img.paint(lt.cov(), "#FFF1C0", 0.42)
    # a few smooth dune shadows (broad soft crescents) and warm-lit crests
    sh = Mask(W, H, SS, True)
    for k in range(6):
        x = rnd.uniform(0, W); y = rnd.uniform(60, 120); sh.ellipse(x, y, rnd.uniform(70, 130), rnd.uniform(9, 15))
    img.paint(sh.cov().blur(6, 3, True), "#B27D3E", 0.25)
    # grains
    g1 = Mask(W, H, SS, True); g2 = Mask(W, H, SS, True)
    for _ in range(420):
        x = rnd.uniform(0, W); y = rnd.uniform(14, 150); r = rnd.uniform(0.7, 1.5)
        (g1 if rnd.random() < .5 else g2).circle(x, y, r)
    img.paint(g1.cov(), "#FFF7DA", 0.8); img.paint(g2.cov(), "#B2793C", 0.6)
    stones(img, rnd, 9, 40, 130, (3, 6), (2, 3.5), "#8A5E2D", Ramp([(0, "#F2DAA2"), (1, "#D9B070")]), shade_a=0.5, lit_a=0.85)
    lip_shadow(img, top, 14, 28, 0.14, "#8A5E2D")
    skirt_fade(img, 80, 190)
    return img

# ------------------------------------------------------------------------------------------------------------------- snow
def snow():
    rnd = random.Random(111)
    items = [(rnd.uniform(0, W), rnd.uniform(40, 95), rnd.uniform(4, 10)) for _ in range(16)]
    base = lambda x, _f=bumps_fn(14.0, [], 1): 14.0 - sum(hh * (0.5 + 0.5 * math.cos(math.pi * periodic_dist(x, bx) / hw)) ** 1.3 for bx, hw, hh in items if periodic_dist(x, bx) < hw)                                  # puffy drifts bulging up
    top = column_cache(base)
    img = Image(W, H)
    soil = Ramp([(0, "#7C8CA6"), (30, "#5F6F8C"), (80, "#47546E"), (192, "#272F42")])
    fringe = column_cache(lambda x: 42 + 17 * abs(math.sin(math.pi * 13 * x / W)) ** 0.7 + 4 * wave_fbm(x, W, 83, 3, 7))
    img.paint(strip_cov(top), mottled(soil, lambda x: 0, 0.08, 0.05, 85))
    stones(img, rnd, 18, 70, 140, (4, 9), (2.5, 5), "#1E2536", Ramp([(0, "#8E9AB0"), (1, "#667389")]), shade_a=0.55, lit_a=0.8)
    cap = Cov(W, H); cap.bb = (0, 0, W, 80)
    for x in range(W):
        tops = [base(x + (i + .5) / SS) for i in range(SS)]; fr = fringe(x)
        for y in range(0, 80):
            a = sum(min(1.0, max(0.0, y + 1 - t)) for t in tops) / SS
            cap.d[y * W + x] = a * min(1.0, max(0.0, fr + 1 - y))
    # shade under the snow overhang so it reads as thick
    shade = Cov(W, H); shade.bb = (0, 0, W, 90)
    for x in range(W):
        fr = fringe(x)
        for y in range(int(fr) + 1, min(H, int(fr) + 14)):
            shade.d[y * W + x] = math.exp(-((y - fr - 3) / 4.5) ** 2)
    img.paint(shade, INK, 0.45)
    img.paint(cap, vgrad(0, 62, Ramp([(0, "#FFFFFF"), (0.35, "#F4F9FF"), (0.75, "#DCE9F8"), (1, "#B9CFEA")])))
    # soft blue shading on the lower-right of each drift, sparkles, a couple of tiny pines-less snow lumps
    sd = Mask(W, H, SS, True); sp = Mask(W, H, SS, True)
    for bx, hw, hh in items:
        sd.ellipse(bx + hw * 0.35, 28, hw * 0.7, 9)
    img.paint(sd.cov().blur(5, 3, True).multiplied(cap), "#A9C4E6", 0.35)
    for _ in range(46):
        x = rnd.uniform(0, W); y = rnd.uniform(8, 44); r = rnd.uniform(1.4, 3.2)
        sp.polygon([(x, y - r), (x + r * .25, y - r * .25), (x + r, y), (x + r * .25, y + r * .25), (x, y + r), (x - r * .25, y + r * .25), (x - r, y), (x - r * .25, y - r * .25)])
    img.paint(sp.cov().multiplied(cap), "#FFFFFF", 0.95)
    skirt_fade(img, 80, 190)
    return img

# ------------------------------------------------------------------------------------------------------------------- gravel
def gravel():
    rnd = random.Random(121)
    items = [(rnd.uniform(0, W), rnd.uniform(5, 13), rnd.uniform(2.5, 7)) for _ in range(120)]
    top = column_cache(bumps_fn(10.0, items))
    img = Image(W, H)
    ramp = Ramp([(0, "#B5AC9C"), (6, "#7F786C"), (20, "#524E48"), (60, "#46423E"), (140, "#35322F"), (192, "#242220")])
    img.paint(strip_cov(top), mottled(ramp, top, 0.12, 0.08, 125))
    shade = Mask(W, H, SS, True); tone = [Mask(W, H, SS, True) for _ in range(4)]; hi = Mask(W, H, SS, True)
    # crest stones along the lip first (they define the silhouette), then dense body stones
    for bx, hw, hh in items:
        y = 10.0 + 2.0; pts = blob(rnd, bx, y, hw * 0.95, hw * 0.78, rnd.choice((5, 6)), 0.18)
        k = rnd.randrange(4); tone[k].polygon(pts); hi.polygon([(x - 0.8, yy - 1.0) for x, yy in blob(rnd, bx - hw * .15, y - hw * .2, hw * .5, hw * .32, 5, .1)])
        shade.polygon([(x + 1.5, yy + 1.8) for x, yy in pts])
    for _ in range(1000):
        x = rnd.uniform(0, W); y = rnd.uniform(16, 165); rx = rnd.uniform(5, 14); ry = rx * rnd.uniform(.55, .85)
        pts = blob(rnd, x, y, rx, ry); k = rnd.randrange(4)
        shade.polygon([(a + 1.5, b + 1.8) for a, b in pts]); tone[k].polygon(pts)
        hi.polygon(blob(rnd, x - rx * .25, y - ry * .3, rx * .5, ry * .35, 5, .1))
    img.paint(shade.cov(), "#23211F", 0.6)
    for m, c in zip(tone, ("#C9C0AE", "#A39A8A", "#847C70", "#6B655C")):
        img.paint(m.cov(), c)
    img.paint(hi.cov(), "#EDE6D6", 0.55)
    lip_shadow(img, top, 16, 28, 0.2)
    skirt_fade(img, 82, 190)
    return img

# ------------------------------------------------------------------------------------------------------------------- cobblestone
def cobblestone():
    rnd = random.Random(131); RH = 25; ROWS = 5; N = 16
    img = Image(W, H)
    img.paint(strip_cov(lambda x: 18.0), vgrad(18, 192, Ramp([(0, "#4A433F"), (0.5, "#3A3532"), (1, "#25211F")])))
    tones = ((("#D3CBC0"), "#A39B90"), ("#C2B7A8", "#938878"), ("#B7B2B8", "#86828C"), ("#CDB8A0", "#9E8B76"))
    for r in range(ROWS):
        y0 = 3 + r * RH + (1 if r else 0); y1 = y0 + RH - 3
        off = 0 if r % 2 == 0 else 32
        cuts = [i * 64 + off + rnd.uniform(-8, 8) for i in range(N)]
        groups = [Mask(W, H, SS, True) for _ in tones]; shd = Mask(W, H, SS, True); hl = Mask(W, H, SS, True)
        for i in range(N):
            xa = cuts[i] + 2.2; xb = cuts[(i + 1) % N] + (W if i == N - 1 else 0) - 2.2
            ya = y0 + (rnd.uniform(-1.8, 1.0) if r == 0 else rnd.uniform(-1, 1)); yb = y1 + rnd.uniform(-1, 1.5)
            if r == 0: ya = y0 + rnd.uniform(-1.8, 1.2)
            groups[rnd.randrange(len(tones))].rrect(xa, ya, xb, yb, 8.5)
            shd.rrect(xa + 1, ya + 2.5, xb + 1.5, yb + 3, 8.5)
            hl.rrect(xa + 3, ya + 1.2, xb - 6, ya + 5.5, 2.6)
        img.paint(shd.cov(), "#1A1614", 0.55)
        for m, (lt, dk) in zip(groups, tones):
            img.paint(m.cov(), vgrad(y0, y1 + 2, Ramp([(0, lt), (0.5, lt), (1, dk)])))
        img.paint(hl.cov(), "#FFFFFF", 0.38 if r == 0 else 0.22)
    # soft darkening toward the bottom of the skirt happens through the fade; add a few chips and moss specks
    ch = Mask(W, H, SS, True)
    for _ in range(80):
        x = rnd.uniform(0, W); y = rnd.uniform(8, 126); ch.circle(x, y, rnd.uniform(.8, 1.7))
    img.paint(ch.cov(), "#6E675D", 0.55)
    skirt_fade(img, 88, 190)
    return img

# ------------------------------------------------------------------------------------------------------------------- moon dust
def moon_dust():
    rnd = random.Random(141)
    craters = [(130, 70, 7.0), (420, 55, 5.0), (700, 90, 8.0), (905, 48, 4.5)]     # lip dips: (x, radius, depth)
    def topf(x):
        t = 8.0 + 0.9 * wave_fbm(x, W, 91, octaves=3, base=3)
        for cx, r, dp in craters:
            d = periodic_dist(x, cx)
            if d < r * 1.35:
                if d < r: t += dp * (0.5 + 0.5 * math.cos(math.pi * d / r)) ** 0.9            # bowl
                rim = max(0.0, 1 - abs(d - r * 1.05) / (r * 0.4)); t -= dp * 0.28 * rim * rim * rim   # raised rim
        return t
    top = column_cache(topf)
    img = Image(W, H)
    ramp = Ramp([(0, "#D9DAE4"), (3, "#B7B9C7"), (14, "#9EA0B0"), (26, "#868899"), (40, "#7B7D8F"), (100, "#6A6C7E"), (150, "#4F5163"), (192, "#363847")])
    img.paint(strip_cov(top), mottled(ramp, top, 0.08, 0.07, 145))
    # body craters: dark bowl with a lit far rim and shadowed near wall
    dk = Mask(W, H, SS, True); lt = Mask(W, H, SS, True); fl = Mask(W, H, SS, True)
    for (cx, cy, rx, ry) in ((250, 72, 46, 12), (560, 96, 62, 15), (800, 60, 34, 9), (60, 110, 40, 10), (980, 88, 28, 8), (480, 52, 24, 6)):
        lt.ellipse(cx - 1, cy - 1.5, rx + 4, ry + 3); dk.ellipse(cx, cy, rx, ry); fl.ellipse(cx + rx * .1, cy + ry * .35, rx * .8, ry * .55)
    img.paint(lt.cov(), "#C9CAD8", 0.7); img.paint(dk.cov(), "#4C4E60", 0.85); img.paint(fl.cov(), "#6A6C7E", 0.8)
    # fine regolith speckle and small rocks
    s1 = Mask(W, H, SS, True); s2 = Mask(W, H, SS, True)
    for _ in range(360):
        x = rnd.uniform(0, W); y = rnd.uniform(14, 160); (s1 if rnd.random() < .5 else s2).circle(x, y, rnd.uniform(.7, 1.6))
    img.paint(s1.cov(), "#D2D3DF", 0.7); img.paint(s2.cov(), "#3C3E4E", 0.6)
    stones(img, rnd, 12, 36, 140, (4, 9), (2.5, 5), "#2C2E3B", Ramp([(0, "#A7A9B8"), (1, "#7E8092")]), shade_a=0.6, lit_a=0.85)
    lip_shadow(img, top, 14, 28, 0.2, "#2C2E3B")
    skirt_fade(img, 82, 190)
    return img

# ------------------------------------------------------------------------------------------------------------------- neon grid
def neon_grid():
    TOPY = 7.0
    top = column_cache(lambda x: TOPY)
    img = Image(W, H)
    img.paint(strip_cov(top), vgrad(TOPY, 192, Ramp([(0, "#2E2A72"), (0.12, "#1E1A55"), (0.6, "#14113C"), (1, "#0C0A28")])))
    cyan, mag = (70, 240, 255), (255, 79, 216)
    mix = lambda x: lerp(cyan, mag, 0.5 + 0.5 * math.cos(2 * math.pi * 2 * x / W))
    # grid: verticals every 64 px, horizontals at fixed rows; glow first, crisp lines on top
    gv = Mask(W, H, SS, True); gh = Mask(W, H, SS, True)
    for i in range(16): gv.rrect(i * 64 + 31, TOPY + 10, i * 64 + 33, 150, 1)
    for y in (36, 68, 104, 144): gh.rrect(-10, y - 1, W + 10, y + 1, 1)
    gvc, ghc = gv.cov(), gh.cov()
    fade = lambda x, y: max(0.0, 1 - (y - TOPY) / 150)
    img.paint(gvc.blur(3, 3, True), lambda x, y: mix(x), 0.28); img.paint(ghc.blur(3, 3, True), lambda x, y: mix(x + 256), 0.28)
    img.paint(gvc, lambda x, y: lerp((14, 12, 44), mix(x), 0.35 + 0.65 * fade(x, y)), 0.9)
    img.paint(ghc, lambda x, y: lerp((14, 12, 44), mix(x + 256), 0.35 + 0.65 * fade(x, y)), 0.9)
    # LED caps: small rounded blocks standing on the lip, every 128 px
    cap = Mask(W, H, SS, True)
    for i in range(8): cap.rrect(i * 128 + 52, TOPY - 5, i * 128 + 76, TOPY + 4, 3)
    img.paint(cap.cov(), vgrad(TOPY - 5, TOPY + 4, Ramp([(0, "#7C77E0"), (1, "#3A3590")])))
    # neon edge: glow, then a bright core
    edge = Mask(W, H, SS, True); edge.rrect(-10, TOPY - 1.5, W + 10, TOPY + 2.5, 2)
    ec = edge.cov(); img.paint(ec.blur(5, 3, True), lambda x, y: mix(x), 0.55)
    img.paint(ec, lambda x, y: lerp(mix(x), (255, 255, 255), 0.55))
    lip_shadow(img, top, 18, 30, 0.0)
    skirt_fade(img, 84, 190)
    return img

# ------------------------------------------------------------------------------------------------------------------- wooden bridge
def wooden_bridge():
    rnd = random.Random(151); N = 16
    img = Image(W, H)
    # posts and braces under the beam (drawn first so the beam overlaps them); they fade by gradient with the skirt
    posts = Mask(W, H, SS, True); pshade = Mask(W, H, SS, True); brace = Mask(W, H, SS, True)
    for i in range(4):
        x = i * 256 + 128; posts.rrect(x - 15, 42, x + 15, 190, 3); pshade.rrect(x + 3, 42, x + 15, 190, 3)
        brace.polyline([(x + 15, 58), (x + 241 - 30 + 15, 150)], 4.5); brace.polyline([(x + 241 - 15, 58), (x + 30, 150)], 4.5)
    img.paint(brace.cov(), vgrad(40, 160, Ramp([(0, "#7A512C"), (1, "#4F3219")])))
    img.paint(posts.cov(), vgrad(40, 190, Ramp([(0, "#8F6137"), (1, "#5A3A1E")])))
    img.paint(pshade.cov(), "#3B2412", 0.4)
    # stringer beam
    beam = Mask(W, H, SS, True); beam.rrect(-10, 24, W + 10, 46, 1)
    img.paint(beam.cov(), vgrad(24, 46, Ramp([(0, "#9A6B3E"), (0.2, "#7E5530"), (1, "#5C3B1F")])))
    sh = Cov(W, H); sh.bb = (0, 0, W, 70)
    for x in range(W):
        for y in range(46, 58): sh.d[y * W + x] = math.exp(-((y - 48) / 4.0) ** 2) * 0.9
    img.paint(sh, INK, 0.30)
    # deck planks: separate boards with gaps, slight height jitter, nails, grain
    pl = [Mask(W, H, SS, True) for _ in range(3)]; hl = Mask(W, H, SS, True); nails = Mask(W, H, SS, True); gr = Mask(W, H, SS, True); psh = Mask(W, H, SS, True)
    for i in range(N):
        xa = i * 64 + 2.5; xb = (i + 1) * 64 - 2.5; ya = 4.0 + rnd.uniform(-1.4, 1.4); yb = 26.0
        pl[rnd.randrange(3)].rrect(xa, ya, xb, yb, 5); psh.rrect(xa + 1, ya + 2.5, xb + 1, yb + 3, 5); hl.rrect(xa + 3, ya + 1.3, xb - 3, ya + 4.3, 1.6)
        nails.circle(xa + 6, ya + 9, 1.5); nails.circle(xb - 6, ya + 9, 1.5)
        for _ in range(2):
            gy = rnd.uniform(ya + 8, yb - 4); gx = rnd.uniform(xa + 8, xb - 30); gr.capsule(gx, gy, gx + rnd.uniform(14, 28), gy + rnd.uniform(-.8, .8), 0.7)
    img.paint(psh.cov(), INK, 0.42)
    for m, (a, b) in zip(pl, (("#D9A56A", "#B07840"), ("#CC9559", "#A26C38"), ("#C28850", "#96612F"))):
        img.paint(m.cov(), vgrad(2, 28, Ramp([(0, a), (1, b)])))
    img.paint(hl.cov(), "#FFE4B8", 0.55); img.paint(gr.cov(), "#7C512A", 0.5); img.paint(nails.cov(), "#3E2A18", 0.9)
    skirt_fade(img, 96, 190)
    return img

# ------------------------------------------------------------------------------------------------------------------- rail track
def rail_track():
    rnd = random.Random(161)
    img = Image(W, H)
    # ballast bed: grey-brown gravel from under the sleepers down, many small stones
    img.paint(strip_cov(lambda x: 30.0), vgrad(30, 192, Ramp([(0, "#6C675F"), (0.3, "#575249"), (1, "#2D2A27")])))
    shd = Mask(W, H, SS, True); tone = [Mask(W, H, SS, True) for _ in range(3)]; hi = Mask(W, H, SS, True)
    for _ in range(650):
        x = rnd.uniform(0, W); y = rnd.uniform(36, 165); rx = rnd.uniform(4, 11); ry = rx * rnd.uniform(.55, .8)
        pts = blob(rnd, x, y, rx, ry); shd.polygon([(a + 1.2, b + 1.5) for a, b in pts]); tone[rnd.randrange(3)].polygon(pts); hi.polygon(blob(rnd, x - rx * .25, y - ry * .3, rx * .45, ry * .3, 5, .1))
    img.paint(shd.cov(), "#1E1C1A", 0.55)
    for m, c in zip(tone, ("#8D877B", "#716B61", "#5A554D")): img.paint(m.cov(), c)
    img.paint(hi.cov(), "#C8C1B2", 0.45)
    # sleepers (seen end-on) every 64 px
    sl = Mask(W, H, SS, True); slh = Mask(W, H, SS, True); ssh = Mask(W, H, SS, True)
    for i in range(16):
        x = i * 64 + 32; ssh.rrect(x - 21, 20, x + 23, 41, 4); sl.rrect(x - 22, 18, x + 22, 38, 4); slh.rrect(x - 18, 19.5, x + 18, 23.5, 1.5)
    img.paint(ssh.cov(), INK, 0.45)
    img.paint(sl.cov(), vgrad(18, 38, Ramp([(0, "#8A5B34"), (1, "#5E3B20")])))
    img.paint(slh.cov(), "#C8946A", 0.5)
    # the rail: flange, web, head with bright top; fishplate bolts every 256 px
    flange = Mask(W, H, SS, True); web = Mask(W, H, SS, True); head = Mask(W, H, SS, True); hh = Mask(W, H, SS, True); bolts = Mask(W, H, SS, True)
    flange.rrect(-10, 15, W + 10, 20, 1.5); web.rrect(-10, 9, W + 10, 16, 0.5); head.rrect(-10, 2, W + 10, 11, 3.5); hh.rrect(-10, 3.2, W + 10, 5.6, 1.2)
    for i in range(4): bolts.circle(i * 256 + 20, 13, 1.7); bolts.circle(i * 256 + 44, 13, 1.7)
    img.paint(flange.cov(), "#6C7580"); img.paint(web.cov(), "#8D97A1"); img.paint(head.cov(), vgrad(2, 11, Ramp([(0, "#EEF3F7"), (0.4, "#BCC6CF"), (1, "#7C8790")])))
    img.paint(hh.cov(), "#FFFFFF", 0.8); img.paint(bolts.cov(), "#3C434B", 0.9)
    skirt_fade(img, 84, 190)
    return img

SURFACES = {"sand": sand, "snow": snow, "gravel": gravel, "cobblestone": cobblestone, "moon_dust": moon_dust,
            "neon_grid": neon_grid, "wooden_bridge": wooden_bridge, "rail_track": rail_track}

if __name__ == "__main__":
    ap = argparse.ArgumentParser(); ap.add_argument("--output", required=True); ap.add_argument("--surface", default="all"); ap.add_argument("--no-preview", action="store_true")
    a = ap.parse_args(); os.makedirs(a.output, exist_ok=True)
    for name in (SURFACES if a.surface == "all" else a.surface.split(",")):
        im = SURFACES[name](); write_png(im, os.path.join(a.output, "road_%s_1024x192.png" % name))
        print(name, "seam score (1.0 = invisible):", round(tile_seam_score(im), 3), flush=True)
        if not a.no_preview: preview(im, os.path.join(a.output, "road_%s_preview_x3.png" % name))
