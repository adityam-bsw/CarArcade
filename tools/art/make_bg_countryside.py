#!/usr/bin/env python3
"""Countryside background as separate layers (Bible sections 5 and 8). Own code, no generative AI, no downloads.

    python3 tools/art/make_bg_countryside.py --output <dir> [--only sky,sun,clouds,far,mid,near,preview]

Outputs (all horizontally seamless where they scroll; colour bleed 4 px applied):
  bg_countryside_sky_4x576.png        vertical gradient strip, opaque (stretch it)
  bg_countryside_sun_256.png          sun disc + soft glow sprite, transparent
  bg_countryside_cloud_a..d.png       separate puffy cloud sprites (flat base, no stray bumps), transparent
  bg_countryside_far_2048x576.png     back mountain range (pointed peaks, hazed) + front soft hills, transparent above
  bg_countryside_mid_2048x576.png     rolling hills with tree-line bumps, transparent above
  bg_countryside_near_2048x864.png    dark saturated foreground hill, round trees and pines, bushes, grass tufts
  bg_countryside_preview_2048x576.png composite for review only (NOT a shipped asset)
Placement assumption used for the preview: layers bottom-aligned to the 576 px view (near layer shows its lowest 576 rows).
Hill heights vary through an amplitude envelope (some peaks tall, some low), so silhouettes are not uniform.
"""
import argparse, math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from smoothart import *

W, H, HN, SS = 2048, 576, 864, 4
P = SCENERY["countryside"]; HAZE = hexrgb(P["haze"])

def pdist(a, b, period=W):
    d = abs(a - b) % period
    return min(d, period - d)

def env(x, seed, lo=0.45):
    """Slow amplitude envelope 0..1 (periodic): makes some stretches tall and some low."""
    return lo + (1 - lo) * (0.5 + 0.5 * wave_fbm(x, W, seed, octaves=2, base=1))

def cache_cols(fn, n=W):
    c = [fn(x + 0.5) for x in range(n)]
    return lambda x: c[min(n - 1, max(0, int(x)))]

def paint_hill(img, top, ground, c_lit, c_shade, haze_amt, depth_span, tree_col=None, ss=SS, h=H, rim=0.18):
    """Fill below top(x). Colour: lit/shaded by slope (light upper left), haze grows toward the top of the layer far side,
    optional darker tree colour where the silhouette rises above the ground line."""
    tc = cache_cols(top); gc = cache_cols(ground) if ground else tc
    cl, cs = hexrgb(c_lit), hexrgb(c_shade); tcol = hexrgb(tree_col) if tree_col else None
    def col(x, y):
        t = tc(x); g = gc(x); slope = (gc(x + 3) - gc(x - 3)) / 6.0
        d = min(1.0, max(0.0, (y - g) / depth_span))
        lit = 0.5 + (smoothstep(-0.6, 0.6, -slope) - 0.5) * (1 - smoothstep(0, 70, y - g))   # slope shading only near the crest (no vertical banding)
        c = lerp(cs, cl, lit)
        if tcol is not None and y < g - 1.5:                            # tree zone: flat tree colour, vertical gradient, no slope banding
            c = lerp(lerp(tcol, cl, 0.30), tcol, smoothstep(0, 40, y - t))
            return lerp(c, (255, 255, 255), rim * 0.5 * (1.0 - smoothstep(0, 4.5, y - t)))
        c = lerp(c, HAZE, haze_amt * (0.2 + 0.8 * (1 - d)) * 0.8)       # hazier toward the crest/horizon
        r = 1.0 - smoothstep(0, 4.5, y - g)
        return lerp(c, (255, 255, 255), rim * r * (0.4 + lit))
    fill_below_curve(img, top, col, ss, bottom=h)

# ------------------------------------------------------------------------------------------------------------------- sky
def make_sky():
    img = Image(4, H)
    ramp = Ramp([(0, P["sky_top"]), (0.65, P["sky_hor"]), (1, P["haze"])])
    for y in range(H):
        c = ramp((y + .5) / H)
        for x in range(4):
            i = y * 4 + x; img.r[i], img.g[i], img.b[i], img.a[i] = c[0], c[1], c[2], 1.0
    return img

def make_sun(n=256):
    img = Image(n, n); c = n / 2
    glow = Cov(n, n); glow.bb = (0, 0, n, n)
    for y in range(n):
        for x in range(n):
            d = math.hypot(x + .5 - c, y + .5 - c)
            if d < c: glow.d[y * n + x] = (1 - d / c) ** 2.2 * 0.75
    img.paint(glow, (255, 247, 214))
    img.paint(Mask(n, n, SS).circle(c, c, 44).cov(), vgrad(c - 44, c + 44, Ramp([(0, "#FFFBE6"), (1, "#FFE9A8")])))
    return img

# ------------------------------------------------------------------------------------------------------------------- clouds
CLOUDS = {   # (puffs as (cx, cy, r) relative to a 420x200 canvas, flat base y)
    "a": ([(110, 120, 52), (180, 92, 68), (255, 108, 58), (318, 128, 44)], 170),
    "b": ([(90, 130, 40), (150, 105, 56), (220, 118, 46), (280, 135, 34)], 168),
    "c": ([(70, 128, 36), (130, 104, 54), (205, 82, 62), (275, 104, 52), (335, 130, 38)], 168),
    "d": ([(100, 124, 44), (165, 112, 50), (235, 126, 40)], 166),
}
def make_cloud(key):
    cw, ch = 420, 200; puffs, B = CLOUDS[key]
    m = Mask(cw, ch, SS)
    for cx, cy, r in puffs: m.circle(cx, cy, r)
    m.polygon([(0, B), (cw, B), (cw, ch), (0, ch)], 0)             # flat base: nothing may hang below it (fixes the old stray bump)
    x0 = min(cx - r for cx, cy, r in puffs) + 14; x1 = max(cx + r for cx, cy, r in puffs) - 14
    m.rrect(x0 + 8, B - 30, x1 - 8, B, 15)
    top = min(cy - r for cx, cy, r in puffs)
    img = Image(cw, ch); cov = m.cov()
    img.paint(cov, vgrad(top, B, Ramp([(0, "#FFFFFF"), (0.55, "#F4FAFF"), (0.8, "#D3E8F7"), (1, "#B3D3EA")])))
    # soft upper-left highlight and a cool shade on the lower right (light from upper left)
    hi = Mask(cw, ch, SS)
    for cx, cy, r in puffs: hi.circle(cx - r * .22, cy - r * .25, r * .62)
    hi.polygon([(0, B - 40), (cw, B - 40), (cw, ch), (0, ch)], 0)
    img.paint(hi.cov().multiplied(cov).blur(5, 2), "#FFFFFF", 0.7)
    img.paint(cov, dgrad(x0, top, x1, B, Ramp([(0, (255, 255, 255)), (1, "#9FC4E2")])), 0.0)
    return img

# ------------------------------------------------------------------------------------------------------------------- far
def mountains(base, amp, seed, k=3, sharp=0.03):
    def f(x):
        v = wave_fbm(x, W, seed, octaves=4, base=k, gain=0.55)
        peak = 1 - math.sqrt(v * v + sharp)                            # pointed where v crosses 0, rounded valleys
        return base - amp * env(x, seed + 100) * (peak - 0.45)
    return f

def make_far():
    img = Image(W, H)
    back = mountains(330, 230, 21, 3)
    paint_hill(img, back, None, "#B4D3E0", "#8DB6CC", 0.38, 160)
    front = lambda x: 392 + 46 * env(x, 17, 0.3) * wave_fbm(x, W, 18, octaves=3, base=2) + 10 * wave_fbm(x, W, 19, octaves=2, base=6)
    paint_hill(img, front, None, "#9ED0C8", "#7FB8B4", 0.30, 150)
    return img

# ------------------------------------------------------------------------------------------------------------------- mid
def make_mid():
    img = Image(W, H); rnd = random.Random(33)
    ground = lambda x: 446 + 52 * env(x, 5, 0.25) * (0.7 * wave_fbm(x, W, 6, octaves=2, base=2) + 0.3 * wave_fbm(x, W, 7, octaves=3, base=5))
    gc = cache_cols(ground)
    trees = []
    for _ in range(260):
        x = rnd.uniform(0, W)
        if wave_fbm(x, W, 40, 2, 3) + rnd.uniform(-0.3, 0.3) > 0.1:          # tree clusters only in some stretches
            trees.append((x, rnd.uniform(9, 20)))
    buckets = {}
    for x, r in trees:
        for b in range(int((x - r) // 32) - 1, int((x + r) // 32) + 2):
            buckets.setdefault(b % (W // 32), []).append((x, r))
    def top(x):
        t = ground(x)
        for tx, r in buckets.get(int(x // 32) % (W // 32), ()):
            d = pdist(x, tx)
            if d < r:
                cy = gc(tx) - r * 0.55
                t = min(t, cy - math.sqrt(r * r - d * d))
        return t
    paint_hill(img, top, ground, "#6DB77B", "#4F9A62", 0.08, 140, tree_col="#3F8A58")
    return img

# ------------------------------------------------------------------------------------------------------------------- near
def make_near():
    img = Image(W, HN); rnd = random.Random(77)
    ground = lambda x: 772 + 30 * env(x, 61, 0.35) * (0.7 * wave_fbm(x, W, 62, octaves=2, base=2) + 0.3 * wave_fbm(x, W, 63, octaves=3, base=5))
    gc = cache_cols(ground)
    paint_hill(img, ground, None, "#4FA35B", "#2F7A46", 0.0, 220, ss=SS, h=HN, rim=0.30)
    # trees: round canopy (3 overlapping puffs) on a trunk, and tiered pines; spaced so they do not overlap
    spots = []
    for _ in range(400):
        x = rnd.uniform(0, W)
        if all(pdist(x, s[0]) > 120 for s in spots) and len(spots) < 15: spots.append((x, 'round' if len(spots) % 3 != 2 else 'pine', rnd.uniform(0.6, 1.15)))
    trunk = Mask(W, HN, SS, True); can = Mask(W, HN, SS, True); inf = []
    for x, kind, s in spots:
        g = gc(x) + 8
        if kind == 'round':
            R = 54 * s; th = 70 * s
            trunk.rrect(x - 8 * s, g - th, x + 8 * s, g + 6, 4 * s)
            cy = g - th - R * 0.55
            for dx, dy, r in ((-R * .6, R * .25, R * .72), (R * .6, R * .3, R * .68), (0, -R * .1, R * .92)):
                can.circle(x + dx, cy + dy, r)
            inf.append((x, cy, R * 1.25))
        else:
            hgt = 190 * s; trunk.rrect(x - 7 * s, g - 24 * s, x + 7 * s, g + 6, 3 * s)
            for tier in range(3):
                bw = (62 - tier * 14) * s; by = g - 18 * s - tier * hgt * 0.27; ty = by - hgt * 0.42
                can.polygon([(x - bw, by), (x, ty), (x + bw, by)])
            inf.append((x, g - hgt * 0.45, hgt * 0.55))
    def canopy_col(x, y):
        best = None
        for cx, cy, R in inf:
            d = pdist(x, cx)
            if d < R * 1.6 and abs(y - cy) < R * 1.6:
                k = (d * d + (y - cy) ** 2) / (R * R)
                if best is None or k < best[0]: best = (k, cx, cy, R)
        if best is None: return hexrgb("#2F7A46")
        _, cx, cy, R = best; sx = x - cx
        sx = ((x - cx + W / 2) % W) - W / 2
        t = ((-sx) * 0.55 + (-(y - cy)) * 0.8) / R                       # >0 faces the upper-left light
        return Ramp([(-1, "#1B5A39"), (-0.2, "#2A7545"), (0.25, "#3F9A52"), (0.8, "#6CC06A")])(t)
    img.paint(trunk.cov(), vgrad(500, 840, Ramp([(0, "#6B4A2E"), (1, "#4A3220")])))
    img.paint(can.cov(), canopy_col)
    # bushes and grass tufts along the crest
    bush = Mask(W, HN, SS, True); tuft = Mask(W, HN, SS, True)
    for _ in range(36):
        x = rnd.uniform(0, W); r = rnd.uniform(14, 28); g = gc(x)
        bush.ellipse(x, g + 2, r * 1.5, r * 0.75); bush.circle(x - r * .4, g - r * .2, r * .6); bush.circle(x + r * .5, g - r * .1, r * .5)
    img.paint(bush.cov(), vgrad(700, 830, Ramp([(0, "#4FA35B"), (1, "#2F7A46")])))
    for _ in range(260):
        x = rnd.uniform(0, W); h = rnd.uniform(8, 20); w = rnd.uniform(2.5, 4.5); g = gc(x) + 3; lean = rnd.uniform(-4, 4)
        tuft.polygon([(x - w, g), (x + lean, g - h), (x + w, g)])
    img.paint(tuft.cov(), "#5CB460")
    return img

# ------------------------------------------------------------------------------------------------------------------- preview
def blit(dst, src, ox, oy):
    for y in range(src.h):
        yy = y + oy
        if 0 <= yy < dst.h:
            for x in range(src.w):
                xx = x + ox
                if 0 <= xx < dst.w:
                    i = y * src.w + x; a = src.a[i]
                    if a <= 0: continue
                    j = yy * dst.w + xx
                    dst.r[j] = src.r[i] * a + dst.r[j] * (1 - a); dst.g[j] = src.g[i] * a + dst.g[j] * (1 - a); dst.b[j] = src.b[i] * a + dst.b[j] * (1 - a)

def compose(sky, sun, clouds, far, mid, near):
    out = Image(W, H, (0, 0, 0, 1.0))
    for y in range(H):
        for x in range(W):
            i = y * 4; j = y * W + x; out.r[j], out.g[j], out.b[j] = sky.r[i], sky.g[i], sky.b[i]
    blit(out, sun, 1500 - 128, 170 - 128)
    for (cl, ox, oy, sc) in clouds: blit(out, cl, ox, oy)
    blit(out, far, 0, 0); blit(out, mid, 0, 0); blit(out, near, 0, H - HN)
    return out

if __name__ == "__main__":
    ap = argparse.ArgumentParser(); ap.add_argument("--output", required=True); ap.add_argument("--only", default="sky,sun,clouds,far,mid,near,preview")
    a = ap.parse_args(); o = a.output; only = a.only.split(","); os.makedirs(o, exist_ok=True); L = {}
    def put(name, im, **k):
        write_png(im, os.path.join(o, name), **k); print(name, flush=True)
    for key in ("sky", "sun", "far", "mid", "near"):
        if key in only or "preview" in only:
            im = {"sky": make_sky, "sun": make_sun, "far": make_far, "mid": make_mid, "near": make_near}[key]()
            L[key] = im
            if key in only:
                put({"sky": "bg_countryside_sky_4x576.png", "sun": "bg_countryside_sun_256.png", "far": "bg_countryside_far_2048x576.png",
                     "mid": "bg_countryside_mid_2048x576.png", "near": "bg_countryside_near_2048x864.png"}[key], im)
                if key in ("far", "mid", "near"): print("  seam score:", round(tile_seam_score(im), 3), flush=True)
    if "clouds" in only or "preview" in only:
        L["clouds"] = {k: make_cloud(k) for k in CLOUDS}
        if "clouds" in only:
            for k, im in L["clouds"].items(): put("bg_countryside_cloud_%s.png" % k, im)
    if "preview" in only:
        cl = L["clouds"]; place = [(cl["a"], 80, 60, 1), (cl["b"], 620, 20, 1), (cl["c"], 1100, 120, 1), (cl["d"], 1640, 30, 1), (cl["b"], 1850, 100, 1)]
        write_png(compose(L["sky"], L["sun"], place, L["far"], L["mid"], L["near"]), os.path.join(o, "bg_countryside_preview_2048x576.png"), do_bleed=False)
