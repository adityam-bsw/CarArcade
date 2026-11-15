#!/usr/bin/env python3
"""Countryside near layer v2: more variety, plus a light-trees alternate. Does NOT change the established look: same hill, same
palette, same tree positions and kinds as make_bg_countryside.make_near (same random stream), then adds extras with its own stream.

    python3 tools/art/make_bg_countryside_v2.py --output <dir> [--only near_v2,near_v2_light,preview]

  bg_countryside_near_v2_2048x864.png        same trees as v1 + post-and-rail fences, grey-green boulders, flower clusters (white / lilac / sky blue)
  bg_countryside_near_v2_light_2048x864.png  same extras, but trees 22% smaller, 35% lighter toward the mid-layer green and 62% opaque, so the
                                             mid layer shows through and the tree line stops crowding car height (optional alternate)
Flower colours avoid red (damage) and gold (reward) by the Bible 4 colour code. Layer size and alignment are identical to the v1 near layer.
"""
import argparse, math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_bg_countryside as mc
from make_bg_worlds import shaded
from smoothart import *
from make_bg_countryside import W, H, HN, SS, pdist, env, cache_cols, paint_hill

def near(light):
    mc.HAZE = hexrgb(mc.P["haze"]); img = Image(W, HN); rnd = random.Random(77); ex = random.Random(177)
    ground = lambda x: 772 + 30 * env(x, 61, 0.35) * (0.7 * wave_fbm(x, W, 62, octaves=2, base=2) + 0.3 * wave_fbm(x, W, 63, octaves=3, base=5))
    gc = cache_cols(ground)
    paint_hill(img, ground, None, "#4FA35B", "#2F7A46", 0.0, 220, ss=SS, h=HN, rim=0.30)
    spots = []
    for _ in range(400):                                             # identical to v1: same stream, same layout
        x = rnd.uniform(0, W)
        if all(pdist(x, s[0]) > 120 for s in spots) and len(spots) < 15: spots.append((x, 'round' if len(spots) % 3 != 2 else 'pine', rnd.uniform(0.6, 1.15)))
    k = 0.78 if light else 1.0
    trunk = Mask(W, HN, SS, True); can = Mask(W, HN, SS, True); inf = []
    for x, kind, s in spots:
        s *= k; g = gc(x) + 8
        if kind == 'round':
            R = 54 * s; th = 70 * s; trunk.rrect(x - 8 * s, g - th, x + 8 * s, g + 6, 4 * s); cy = g - th - R * 0.55
            for dx, dy, r in ((-R * .6, R * .25, R * .72), (R * .6, R * .3, R * .68), (0, -R * .1, R * .92)): can.circle(x + dx, cy + dy, r)
            inf.append((x, cy, R * 1.25))
        else:
            hgt = 190 * s; trunk.rrect(x - 7 * s, g - 24 * s, x + 7 * s, g + 6, 3 * s)
            for tier in range(3):
                bw = (62 - tier * 14) * s; by = g - 18 * s - tier * hgt * 0.27; ty = by - hgt * 0.42; can.polygon([(x - bw, by), (x, ty), (x + bw, by)])
            inf.append((x, g - hgt * 0.45, hgt * 0.55))
    ramp = Ramp([(-1, "#1B5A39"), (-0.2, "#2A7545"), (0.25, "#3F9A52"), (0.8, "#6CC06A")])
    MID = hexrgb("#6FB07A")
    cf = shaded(inf, (lambda t: lerp(ramp(t), MID, 0.35)) if light else ramp, hexrgb("#2F7A46"), 1.6)
    op = 0.62 if light else 1.0
    img.paint(trunk.cov(), vgrad(500, 840, Ramp([(0, "#6B4A2E"), (1, "#4A3220")])), op)
    img.paint(can.cov(), cf, op)
    # bushes and grass tufts (v1 stream continues unchanged)
    bush = Mask(W, HN, SS, True); tuft = Mask(W, HN, SS, True)
    for _ in range(36):
        x = rnd.uniform(0, W); r = rnd.uniform(14, 28); g = gc(x)
        bush.ellipse(x, g + 2, r * 1.5, r * 0.75); bush.circle(x - r * .4, g - r * .2, r * .6); bush.circle(x + r * .5, g - r * .1, r * .5)
    img.paint(bush.cov(), vgrad(700, 830, Ramp([(0, "#4FA35B"), (1, "#2F7A46")])))
    for _ in range(260):
        x = rnd.uniform(0, W); h = rnd.uniform(8, 20); w = rnd.uniform(2.5, 4.5); g = gc(x) + 3; lean = rnd.uniform(-4, 4)
        tuft.polygon([(x - w, g), (x + lean, g - h), (x + w, g)])
    img.paint(tuft.cov(), "#5CB460")
    # ---- v2 extras, kept low (<= 70 px), drawn in front of the trees
    free = lambda x, hw: all(pdist(x, s[0]) > 28 for s in spots)   # extras may sit in front of trunks (they are low), just not on top of each other
    items = []
    def place(n, hw):
        out = []
        for _ in range(500):
            x = ex.uniform(0, W)
            if len(out) < n and free(x, hw) and all(pdist(x, o) > hw + oh + 20 for o, oh in items): out.append(x); items.append((x, hw))
        return out
    fence = Mask(W, HN, SS, True); rails = Mask(W, HN, SS, True); fsh = Mask(W, HN, SS, True)
    for x0 in place(3, 90):
        L = ex.choice((132, 176, 220)); n = int(L // 44); xs = [x0 - L / 2 + i * L / n for i in range(n + 1)]
        tops = [(x, gc(x) + 4 - 54) for x in xs]
        for x, t in tops: fence.rrect(x - 4.5, t, x + 4.5, gc(x) + 8, 2.4); fsh.rrect(x - 3, t + 3, x + 6, gc(x) + 8, 2.4)
        for (xa, ta), (xb, tb) in zip(tops, tops[1:]):
            for off in (12, 31): rails.capsule(xa, ta + off, xb, tb + off, 3.0)
    img.paint(fsh.cov(), "#0F3A22", 0.35)
    img.paint(rails.cov(), vgrad(780, 800, Ramp([(0, "#B08258"), (1, "#7A5333")])))
    img.paint(fence.cov(), vgrad(700, 830, Ramp([(0, "#C29668"), (1, "#7A5333")])))
    fl_x = place(9, 36)
    rk = Mask(W, HN, SS, True); ritems = []
    for x in place(4, 40):
        g = gc(x) + 8; R = ex.uniform(26, 44)
        for dx, dy, rx, ry in ((0, 0, 1.0, .62), (-.75, .2, .55, .36), (.7, .24, .5, .32)): rk.ellipse(x + dx * R, g - R * .35 + dy * R * .3, rx * R, ry * R)
        ritems.append((x, g - R * .4, R * 1.1))
    img.paint(rk.cov(), shaded(ritems, Ramp([(-1, "#4E6358"), (-0.2, "#6E8277"), (0.25, "#93A79C"), (0.8, "#BCCFC4")]), hexrgb("#6E8277")))
    stem = Mask(W, HN, SS, True); pet = {c: Mask(W, HN, SS, True) for c in ("#FFFFFF", "#C79BF0", "#8EC9FF")}; ctr = Mask(W, HN, SS, True)
    for x in fl_x:
        col = ex.choice(list(pet))
        for _ in range(ex.randint(5, 9)):
            fx = x + ex.uniform(-40, 40); g = gc(fx) + 4; h = ex.uniform(14, 30); r = ex.uniform(4.2, 6.0); tx = fx + ex.uniform(-3, 3)
            stem.capsule(fx, g, tx, g - h, 1.1)
            for a in range(5):
                an = 6.2832 * a / 5 + fx; pet[col].circle(tx + r * 0.9 * math.cos(an), g - h + r * 0.9 * math.sin(an), r * 0.62)
            ctr.circle(tx, g - h, r * 0.5)
    img.paint(stem.cov(), "#2F7A46")
    for c, m in pet.items(): img.paint(m.cov(), c)
    img.paint(ctr.cov(), "#8E6FD0")
    return img

if __name__ == "__main__":
    ap = argparse.ArgumentParser(); ap.add_argument("--output", required=True); ap.add_argument("--only", default="near_v2,near_v2_light,preview"); a = ap.parse_args()
    os.makedirs(a.output, exist_ok=True); only = a.only.split(","); L = {}
    for key, light in (("near_v2", False), ("near_v2_light", True)):
        if key in only or "preview" in only:
            im = near(light); L[key] = im
            if key in only: write_png(im, os.path.join(a.output, "bg_countryside_%s_2048x864.png" % key)); print(key, "seam", round(tile_seam_score(im), 3), flush=True)
    if "preview" in only:
        from smoothart import Image
        S = os.path.join(os.path.dirname(os.path.abspath(a.output)), "bg")                  # existing v1 layers for composition (read only)
        for key, im in L.items():
            sky, sun = mc.make_sky(), mc.make_sun(); far, mid = mc.make_far(), mc.make_mid()
            cl = {k: mc.make_cloud(k) for k in mc.CLOUDS}; place = [(cl["a"], 80, 60, 1), (cl["b"], 620, 20, 1), (cl["c"], 1100, 120, 1), (cl["d"], 1640, 30, 1)]
            write_png(mc.compose(sky, sun, place, far, mid, im), os.path.join(a.output, "bg_countryside_%s_preview_2048x576.png" % key), do_bleed=False)
