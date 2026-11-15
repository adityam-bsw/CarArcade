#!/usr/bin/env python3
"""Smooth road strips, 1024 x 192 (8 x 1.5 units at 128 ppu), seamless in X. Own code, no generative AI.

    python3 tools/art/make_road.py --output <dir> [--surface asphalt|dirt|grass|ice|mud|glass|all]

Each surface has its own silhouette AND texture (not only hue), so it reads in greyscale and for colour-blind players:
  asphalt  flat lip, lane dashes, soft stones            dirt   lumpy lip, tyre ruts, pebbles
  grass    scalloped grass fringe with blade tufts       mud    bulging blobs on the lip, dripping skirt, puddles, bubbles
  ice      smooth lip, angular facets, hanging icicles    glass  two separate 4 m panes with a gap, bevel, gloss bands
  glass also writes road_glass_crack1/2/3 overlays (hairline, web, shatter), same size, transparent, clipped to the panes.
Layout (Bible section 4): rounded top lip with highlight, body, skirt that fades to transparent by gradient (never dither).
Writes <name>_1024x192.png (+ colour bleed 4 px) and a 3-tile preview. Colour code (Bible 4): blue ice, brown mud, cyan-white glass.
"""
import argparse, math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from smoothart import *

W, H, SS = 1024, 192, 4
INK = PAL["DeepInk"]

def periodic_dist(a, b):
    d = abs(a - b) % W
    return min(d, W - d)

def strip_cov(top, bottom=H):
    """Coverage of everything below the wavy top edge y = top(x) (analytic vertically, supersampled horizontally)."""
    cov = Cov(W, H); cov.bb = (0, 0, W, bottom)
    for x in range(W):
        tops = [top(x + (i + .5) / SS) for i in range(SS)]
        for y in range(0, bottom):
            cov.d[y * W + x] = sum(min(1.0, max(0.0, y + 1 - t)) for t in tops) / SS
    return cov

def column_cache(fn):
    c = [fn(x + 0.5) for x in range(W)]
    return lambda x: c[min(W - 1, max(0, int(x)))]

def mottled(ramp, top, k1=0.07, k2=0.04, seed=5):
    mott = ValueNoise2D(8, 2, seed); fine = ValueNoise2D(32, 4, seed + 4)
    def body(x, y):
        c = ramp(y - top(x))
        k = 1.0 + k1 * (mott.at(x / W, y / H) - 0.5) + k2 * (fine.at(x / W, y / H) - 0.5)
        return (c[0] * k, c[1] * k, c[2] * k)
    return body

def lip_shadow(img, top, y_off=18, y_end=34, alpha=0.28, color=INK):
    cov = Cov(W, H); cov.bb = (0, 0, W, H)
    for x in range(W):
        t = top(x)
        for y in range(max(0, int(t) + y_off), min(H, int(t) + y_end)):
            cov.d[y * W + x] = math.exp(-((y - (t + (y_off + y_end) / 2 - 0.5)) / 3.5) ** 2)
    img.paint(cov, color, alpha)

def stones(img, rnd, n, ylo, yhi, rx, ry, shade_col, lit_ramp, skip=None, shade_a=0.55, lit_a=0.75):
    lit = Mask(W, H, SS, True); shade = Mask(W, H, SS, True)
    for _ in range(n):
        x = rnd.uniform(0, W); y = rnd.uniform(ylo, yhi)
        if skip and skip[0] < y < skip[1]: y += skip[2]
        a, b = rnd.uniform(*rx), rnd.uniform(*ry)
        shade.ellipse(x + 1.5, y + 1.8, a, b); lit.ellipse(x - 0.5, y - 0.8, a * .92, b * .85)
    img.paint(shade.cov(), shade_col, shade_a)
    img.paint(lit.cov(), vgrad(ylo, yhi + 10, lit_ramp), lit_a)

def skirt_fade(img, y0, y1):
    img.multiply_alpha(lambda x, y: 1.0 - smoothstep(y0, y1, y))

# ---------------------------------------------------------------------------------------------------------------- asphalt
def asphalt():
    top = column_cache(lambda x: 4.0 + 1.1 * wave_fbm(x, W, 11, octaves=3, base=3))
    img = Image(W, H)
    ramp = Ramp([(0, PAL["SteelLight"]), (4, "#7C8791"), (14, "#626C77"), (22, "#515B66"), (27, "#394149"),
                 (34, "#4C5662"), (95, "#434C57"), (140, "#303843"), (192, PAL["WorkshopCharcoal"])])
    img.paint(strip_cov(top), mottled(ramp, top))
    stones(img, random.Random(3), 16, 40, 100, (6, 15), (3, 6), "#2C333C", Ramp([(0, "#69737E"), (1, "#56606B")]), skip=(48, 74, 30))
    sh = Mask(W, H, SS, True); da = Mask(W, H, SS, True)
    for k in range(4):
        x0 = 76 + k * 256
        sh.rrect(x0 + 2, 58, x0 + 106, 72, 7); da.rrect(x0, 54, x0 + 104, 66, 6)
    img.paint(sh.cov().blur(2, 2, True), INK, 0.35)
    img.paint(da.cov(), vgrad(54, 66, Ramp([(0, PAL["WarmCream"]), (1, "#BDB59F")])))
    lip_shadow(img, top)
    skirt_fade(img, 62, 190)
    return img

# ---------------------------------------------------------------------------------------------------------------- dirt
def dirt():
    # lumpy lip: low and mid frequency, a rougher edge than asphalt
    top = column_cache(lambda x: 7.0 + 3.4 * wave_fbm(x, W, 31, octaves=5, base=4, gain=0.62))
    img = Image(W, H)
    ramp = Ramp([(0, "#E3B77C"), (3, "#D29C5E"), (13, "#B98450"), (21, "#966233"), (26, "#7B4F29"), (34, "#A27048"),
                 (95, "#8C5D36"), (140, "#5E3A21"), (192, "#3C2515")])
    img.paint(strip_cov(top), mottled(ramp, top, 0.10, 0.06, 15))
    rnd = random.Random(8)
    # tyre ruts: two long shallow depressions, dark upper half, light lower rim (reads as a groove)
    for (yc, ph) in ((58, 0), (84, 260)):
        dk = Mask(W, H, SS, True); lt = Mask(W, H, SS, True)
        for k in range(3):
            cx = (ph + k * 341 + rnd.uniform(-30, 30)) % W; rx = rnd.uniform(150, 210)
            dk.ellipse(cx, yc, rx, 7); lt.ellipse(cx + 1, yc + 5.5, rx - 6, 4.2)
        img.paint(dk.cov().blur(1, 2, True), "#5A3820", 0.55)
        img.paint(lt.cov().blur(1, 2, True), "#C99660", 0.5)
    stones(img, rnd, 46, 36, 150, (3, 8), (2, 4.5), "#4A2D18", Ramp([(0, "#C9A072"), (1, "#9A6B40")]), shade_a=0.6, lit_a=0.8)
    lip_shadow(img, top, 14, 28, 0.25)
    skirt_fade(img, 80, 190)
    return img

# ---------------------------------------------------------------------------------------------------------------- grass
def grass():
    base = lambda x: 15.0 + 1.0 * wave_fbm(x, W, 41, octaves=3, base=3)       # soil line the collision should use
    top = column_cache(base)
    img = Image(W, H)
    soil = Ramp([(0, "#8B6540"), (20, "#7A5535"), (60, "#6A4A31"), (130, "#4C3524"), (192, "#2E2118")])
    # soil body below a scalloped grass overhang
    fringe = column_cache(lambda x: 16 + 12 * (0.5 + 0.5 * math.sin(2 * math.pi * 21 * x / W)) ** 0.7 + 3 * wave_fbm(x, W, 7, 3, 9))
    img.paint(strip_cov(top), mottled(soil, lambda x: 0, 0.08, 0.05, 25))
    stones(img, random.Random(12), 22, 48, 130, (4, 10), (2.5, 5), "#2C1F15", Ramp([(0, "#A89479"), (1, "#7E6C58")]))
    # grass cap: body to scalloped lower edge
    cap = Cov(W, H); cap.bb = (0, 0, W, 40)
    for x in range(W):
        tops = [base(x + (i + .5) / SS) for i in range(SS)]; fr = fringe(x)
        for y in range(0, 40):
            a = sum(min(1.0, max(0.0, y + 1 - t)) for t in tops) / SS
            b = min(1.0, max(0.0, fr + 1 - y))
            cap.d[y * W + x] = a * b
    img.paint(cap, vgrad(0, 30, Ramp([(0, "#9BDB62"), (0.25, "#6FBF4A"), (0.7, "#4C9A3C"), (1, "#3A7A33")])))
    # shade under the fringe so the overhang reads
    shade = Cov(W, H); shade.bb = (0, 0, W, 50)
    for x in range(W):
        fr = fringe(x)
        for y in range(int(fr) + 1, min(H, int(fr) + 9)):
            shade.d[y * W + x] = math.exp(-((y - fr - 2) / 3.2) ** 2)
    img.paint(shade, INK, 0.40)
    # blade tufts poking above the soil line: pointed triangles, varied height, wrap-safe
    rnd = random.Random(5); tuft = Mask(W, H, SS, True); tl = Mask(W, H, SS, True)
    for _ in range(70):
        x = rnd.uniform(0, W); h = rnd.uniform(6, 15); w = rnd.uniform(2.2, 4.2); lean = rnd.uniform(-3, 3); b = base(x) + 2.5
        tuft.polygon([(x - w, b), (x + lean, b - h), (x + w, b)])
        tl.polygon([(x - w, b), (x + lean, b - h), (x - w * 0.1, b)])
    img.paint(tuft.cov(), vgrad(0, 16, Ramp([(0, "#8CD455"), (1, "#4E9E3B")])))
    img.paint(tl.cov(), "#C2F28A", 0.55)
    # light grass-cap rim highlight along the very top of the cap
    skirt_fade(img, 70, 190)
    return img

# ---------------------------------------------------------------------------------------------------------------- mud
def mud():
    rnd = random.Random(21)
    bumps = [(rnd.uniform(0, W), rnd.uniform(14, 34), rnd.uniform(4, 9)) for _ in range(30)]   # (x, half width, height)
    def topf(x):
        t = 11.0 + 1.0 * wave_fbm(x, W, 51, octaves=3, base=5)
        for bx, hw, hh in bumps:
            d = periodic_dist(x, bx)
            if d < hw:
                t -= hh * (0.5 + 0.5 * math.cos(math.pi * d / hw)) ** 0.8        # round, blobby bulge
        return t
    top = column_cache(topf)
    img = Image(W, H)
    ramp = Ramp([(0, "#B07F4F"), (3, "#8E6038"), (12, "#6B4426"), (24, "#573620"), (34, "#4C2F1C"), (95, "#47291A"),
                 (150, "#3C2515"), (192, "#2A1A0E")])
    img.paint(strip_cov(top), mottled(ramp, lambda x: 11.0, 0.14, 0.05, 35))
    # dark rim hugging the blob edge for a wet, thick look
    rim = Cov(W, H); rim.bb = (0, 0, W, 60)
    for x in range(W):
        t = top(x)
        for y in range(int(t), int(t) + 12):
            rim.d[y * W + x] = math.exp(-((y - t - 7) / 2.2) ** 2)
    img.paint(rim, "#2B170C", 0.45)
    hi = Cov(W, H); hi.bb = (0, 0, W, 60)
    for x in range(W):
        t = top(x)
        for y in range(int(t), int(t) + 6):
            hi.d[y * W + x] = max(0.0, 1 - (y + .5 - t) / 3.5)
    img.paint(hi, "#C9956300"[:7], 0.7)
    # puddles: wide shallow ellipses, lighter wet fill with a bright crescent highlight
    pud = Mask(W, H, SS, True); hl = Mask(W, H, SS, True); dk = Mask(W, H, SS, True)
    for (cx, cy, rx, ry) in ((120, 62, 70, 11), (410, 70, 80, 11), (650, 52, 55, 8), (860, 72, 70, 11), (980, 56, 36, 6)):
        dk.ellipse(cx, cy + 1.5, rx + 3, ry + 2); pud.ellipse(cx, cy, rx, ry)
        hl.ellipse(cx - rx * 0.25, cy - ry * 0.35, rx * 0.5, ry * 0.28)
    img.paint(dk.cov(), "#2B170C", 0.55)
    img.paint(pud.cov(), vgrad(50, 150, Ramp([(0, "#7F5532"), (1, "#6B4528")])))
    img.paint(hl.cov().blur(1, 2, True), "#D9B48A", 0.75)
    # bubbles
    bub = Mask(W, H, SS, True); bhl = Mask(W, H, SS, True)
    for _ in range(18):
        x = rnd.uniform(0, W); y = rnd.uniform(40, 150); r = rnd.uniform(3, 7)
        bub.circle(x, y, r); bhl.circle(x - r * .3, y - r * .3, r * .38)
    img.paint(bub.cov(), "#8A5E38", 0.9); img.paint(bhl.cov(), "#E6C9A0", 0.8)
    # clods
    stones(img, rnd, 14, 40, 140, (5, 10), (3, 6), "#26140A", Ramp([(0, "#7A4F2E"), (1, "#5E3B22")]), shade_a=0.5, lit_a=0.6)
    # dripping skirt: rounded drips of varying length, then gradient fade
    drips = [(rnd.uniform(0, W), rnd.uniform(8, 18), rnd.uniform(25, 75)) for _ in range(26)]
    def bottom(x):
        b = 100.0
        for dx, hw, ln in drips:
            d = periodic_dist(x, dx)
            if d < hw:
                b = max(b, 100 + ln * math.sqrt(max(0.0, 1 - (d / hw) ** 2)) ** 0.8)
        return b
    bc = column_cache(bottom)
    img.multiply_alpha(lambda x, y: 1.0 - smoothstep(bc(x) - 14, bc(x) + 3, y))
    return img

# ---------------------------------------------------------------------------------------------------------------- ice
def ice():
    top = column_cache(lambda x: 6.0 + 0.7 * wave_fbm(x, W, 61, octaves=2, base=2))     # smooth glassy lip
    img = Image(W, H)
    ramp = Ramp([(0, "#FFFFFF"), (3, "#E6F6FF"), (14, "#BFE4F8"), (24, "#8CC8F0"), (60, "#7DB9E8"), (120, "#5E9AD2"), (192, "#3F7FBF")])
    img.paint(strip_cov(top), mottled(ramp, top, 0.04, 0.02, 45))
    rnd = random.Random(14)
    # angular facets: large lighter / darker polygons (frozen plates)
    for i in range(22):
        cx, cy = rnd.uniform(0, W), rnd.uniform(26, 150); n = rnd.choice((3, 4, 5)); R = rnd.uniform(24, 64)
        a0 = rnd.uniform(0, 6.28)
        pts = [(cx + R * rnd.uniform(.6, 1.1) * math.cos(a0 + 6.2832 * k / n), cy + R * 0.5 * rnd.uniform(.6, 1.1) * math.sin(a0 + 6.2832 * k / n)) for k in range(n)]
        m = Mask(W, H, SS, True); m.polygon(pts)
        light = i % 2 == 0
        img.paint(m.cov(), "#EAF8FF" if light else "#4F8CC8", 0.30 if light else 0.22)
    # fracture lines inside the ice (thin, white, angular)
    cr = Mask(W, H, SS, True)
    for _ in range(9):
        x = rnd.uniform(0, W); y = rnd.uniform(34, 120); pts = [(x, y)]
        for k in range(rnd.randint(3, 5)):
            x += rnd.uniform(14, 40); y += rnd.uniform(-14, 14); pts.append((x, y))
        cr.polyline(pts, 0.9)
    img.paint(cr.cov(), "#F4FBFF", 0.8)
    # diagonal gloss glints: slanted capsules, the ice signature
    gl = Mask(W, H, SS, True); gl2 = Mask(W, H, SS, True)
    for k in range(7):
        x = 60 + k * 150 + rnd.uniform(-20, 20); y = 36 + (k % 3) * 38
        gl.capsule(x, y + 20, x + 34, y - 4, 3.4); gl2.capsule(x + 44, y + 17, x + 54, y + 12, 2.2)
    img.paint(gl.cov(), "#FFFFFF", 0.80); img.paint(gl2.cov(), "#FFFFFF", 0.75)
    lip_shadow(img, top, 16, 30, 0.18, "#2F5F96")
    # icicles on the skirt: triangular tapering teeth, with a bright left edge
    ic = [(rnd.uniform(0, W), rnd.uniform(5, 11), rnd.uniform(18, 52)) for _ in range(34)]
    def bottom(x):
        b = 112.0
        for dx, hw, ln in ic:
            d = periodic_dist(x, dx)
            if d < hw:
                b = max(b, 112 + ln * (1 - d / hw))
        return b
    bc = column_cache(bottom)
    img.multiply_alpha(lambda x, y: 1.0 - smoothstep(bc(x) - 9, bc(x) + 1, y))
    # soften only the very bottom: keep icicle tips solid, fade the slab above them slightly
    skirt_fade(img, 150, 192)
    return img

# ---------------------------------------------------------------------------------------------------------------- glass
PANES = ((8, 504), (520, 1016))                     # two 4 m pieces (512 px each) with a 16 px gap, seamless on the wrap
GTOP, GBOT = 8, 168

def glass_masks():
    m = Mask(W, H, SS, True); inner = Mask(W, H, SS, True)
    for a, b in PANES:
        m.rrect(a, GTOP, b, GBOT, 16); inner.rrect(a + 11, GTOP + 11, b - 11, GBOT - 11, 8)
    return m, inner

def glass():
    img = Image(W, H); m, inner = glass_masks()
    cx = lambda x: min((abs(x - (a + b) / 2) for a, b in PANES), default=0)
    # frame: bright rim at the top and left (light from upper left), cyan body, deeper teal toward bottom
    frame = Ramp([(0, "#F2FFFF"), (0.12, "#CFF6FA"), (0.45, "#9FE4EE"), (0.85, "#6BC3D6"), (1, "#3B8EA6")])
    img.paint(m.cov(), vgrad(GTOP, GBOT, frame))
    img.paint(inner.cov(), vgrad(GTOP + 11, GBOT - 11, Ramp([(0, "#D9FAFC"), (0.3, "#B4EFF4"), (1, "#7ACFE0")])), 0.9)
    # dark bevel line separating frame and face (inner outline), then outer edge shadow
    ed = inner.dilated(1.6).minus(inner)
    img.paint(ed.cov(), "#3B8EA6", 0.55)
    # diagonal gloss bands per pane (parallelograms, soft)
    gl = Mask(W, H, SS, True); gl2 = Mask(W, H, SS, True)
    for a, b in PANES:
        x = a + 70; gl.polygon([(x, GBOT - 14), (x + 58, GBOT - 14), (x + 126, GTOP + 14), (x + 68, GTOP + 14)])
        x = a + 205; gl2.polygon([(x, GBOT - 14), (x + 22, GBOT - 14), (x + 90, GTOP + 14), (x + 68, GTOP + 14)])
    inner_cov = inner.cov()
    img.paint(gl.cov().multiplied(inner_cov), "#FFFFFF", 0.55); img.paint(gl2.cov().multiplied(inner_cov), "#FFFFFF", 0.4)
    # sparkle stars (4 point)
    sp = Mask(W, H, SS, True)
    for a, b in PANES:
        for dx, dy, r in ((60, 40, 9), (330, 112, 7), (430, 52, 6)):
            x, y = a + dx, GTOP + dy
            sp.polygon([(x, y - r), (x + r * .22, y - r * .22), (x + r, y), (x + r * .22, y + r * .22), (x, y + r), (x - r * .22, y + r * .22), (x - r, y), (x - r * .22, y - r * .22)])
    img.paint(sp.cov(), "#FFFFFF", 0.95)
    # glossy lip highlight along the top
    hi = Mask(W, H, SS, True)
    for a, b in PANES:
        hi.rrect(a + 18, GTOP + 2.5, b - 18, GTOP + 6.5, 2)
    img.paint(hi.cov(), "#FFFFFF", 0.9)
    # translucent skirt: pane fades to transparent over its lower third
    skirt_fade(img, 100, 178)
    return img

def jag(rnd, p0, p1, seg=7, amp=4.0):
    pts = [p0]; dx, dy = p1[0] - p0[0], p1[1] - p0[1]; L = math.hypot(dx, dy) or 1; nx, ny = -dy / L, dx / L
    for i in range(1, seg):
        t = i / seg; o = rnd.uniform(-amp, amp)
        pts.append((p0[0] + dx * t + nx * o, p0[1] + dy * t + ny * o))
    pts.append(p1); return pts

def crack_overlay(stage, base_img):
    """Stage 1 hairline, 2 spiderweb, 3 shatter. Transparent overlay, clipped to the glass alpha, with a soft white glow."""
    rnd = random.Random(70 + stage)
    line = Mask(W, H, SS, True); fat = Mask(W, H, SS, True); shard = Mask(W, H, SS, True); hole = Mask(W, H, SS, True)
    for pi, (a, b) in enumerate(PANES):
        ox, oy = a + 250 + pi * 40, GTOP + 70 + pi * 12
        if stage == 1:                                   # one hairline across the pane, no branches
            line.polyline(jag(rnd, (a + 30, GTOP + 12), (ox, oy), 6, 4) + jag(rnd, (ox, oy), (b - 40, GBOT - 14), 6, 4)[1:], 1.1)
        else:
            nr = 9 if stage == 2 else 14
            ends = []
            for k in range(nr):
                ang = 2 * math.pi * k / nr + rnd.uniform(-0.2, 0.2)
                L = rnd.uniform(60, 240) if stage == 2 else rnd.uniform(90, 250)
                ex = max(a + 8, min(b - 8, ox + L * math.cos(ang))); ey = max(GTOP + 6, min(GBOT - 8, oy + L * 0.62 * math.sin(ang)))
                ends.append((ex, ey)); (fat if stage == 3 else line).polyline(jag(rnd, (ox, oy), (ex, ey), 8, 5), 1.5 if stage == 2 else 2.2)
            for rr in ((26, 58) if stage == 2 else (22, 50, 90)):       # concentric web rings
                ring = []
                for k in range(nr):
                    ang = 2 * math.pi * k / nr + rnd.uniform(-0.1, 0.1) + 0.2
                    ring.append((max(a + 8, min(b - 8, ox + rr * math.cos(ang) * 1.5)), max(GTOP + 6, min(GBOT - 8, oy + rr * math.sin(ang)))))
                for p, q in zip(ring, ring[1:] + ring[:1]):
                    (fat if stage == 3 else line).polyline(jag(rnd, p, q, 3, 2), 1.1 if stage == 2 else 1.8)
            if stage == 3:                                # shards pushed out: dark holes and displaced light plates
                for k in range(5):
                    ang = rnd.uniform(0, 6.28); r0 = rnd.uniform(24, 90); x = ox + r0 * math.cos(ang) * 1.4; y = oy + r0 * math.sin(ang) * .7
                    s = rnd.uniform(10, 22)
                    pts = [(x + s * math.cos(ang + 2.1 * j) * rnd.uniform(.6, 1.1), y + s * math.sin(ang + 2.1 * j) * rnd.uniform(.5, .9)) for j in range(3)]
                    (hole if k % 2 == 0 else shard).polygon(pts)
                hole.circle(ox, oy, 14)
    clip = Cov(W, H, [min(1.0, v * 1.6) for v in base_img.a], (0, 0, W, H))
    out = Image(W, H)
    glow = (line.cov().scaled(1) if stage < 3 else fat.cov()).blur(2, 2, True)
    out.paint(glow.multiplied(clip), "#FFFFFF", 0.55)
    if stage == 3:
        out.paint(hole.cov().multiplied(clip), "#1F6477", 0.8)
        out.paint(shard.cov().multiplied(clip), "#FFFFFF", 0.5)
        out.paint(fat.cov().multiplied(clip), "#1F6477", 0.95)
    else:
        out.paint(line.cov().multiplied(clip), "#2A7E93", 0.95)
    return out

SURFACES = {"asphalt": asphalt, "dirt": dirt, "grass": grass, "ice": ice, "mud": mud, "glass": glass}

def preview(im, path, back=(60, 90, 130, 1.0)):
    prev = Image(W * 3, H + 60, back)
    for t in range(3):
        for y in range(H):
            for x in range(W):
                i = y * W + x; j = (y + 30) * (W * 3) + t * W + x; a = im.a[i]
                prev.r[j] = im.r[i] * a + prev.r[j] * (1 - a); prev.g[j] = im.g[i] * a + prev.g[j] * (1 - a); prev.b[j] = im.b[i] * a + prev.b[j] * (1 - a)
    write_png(prev, path, do_bleed=False)

if __name__ == "__main__":
    ap = argparse.ArgumentParser(); ap.add_argument("--output", required=True); ap.add_argument("--surface", default="all")
    ap.add_argument("--no-preview", action="store_true"); a = ap.parse_args()
    os.makedirs(a.output, exist_ok=True)
    for name in (SURFACES if a.surface == "all" else [a.surface]):
        im = SURFACES[name](); write_png(im, os.path.join(a.output, "road_%s_1024x192.png" % name))
        print(name, "seam score (1.0 = invisible):", round(tile_seam_score(im), 3), flush=True)
        if not a.no_preview: preview(im, os.path.join(a.output, "road_%s_preview_x3.png" % name))
        if name == "glass":
            for st in (1, 2, 3):
                ov = crack_overlay(st, im); write_png(ov, os.path.join(a.output, "road_glass_crack%d_1024x192.png" % st))
                print("  crack", st, "seam", round(tile_seam_score(ov), 3), flush=True)
