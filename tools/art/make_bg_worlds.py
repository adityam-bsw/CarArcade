#!/usr/bin/env python3
"""Smooth Desert and Night City backgrounds, same generator, sizes and layer contract as make_bg_countryside.py
(Bible sections 5 and 8; SCENERY ramps in smoothart/palette.py). Own code, no generative AI, no downloads.

    python3 tools/art/make_bg_worlds.py --world desert|nightcity --output <dir> [--only sky,sun,stars,far,mid,near,preview]

desert:     bg_desert_sky_4x576, bg_desert_sun_256, far (mesas + dunes), mid (dunes, outcrops, small cacti), near (saguaros, boulders,
            agave, dry tufts: everything under about 190 px so it does not crowd car height), preview.
nightcity:  bg_nightcity_sky_4x576, moon_320 (striped neon moon), stars_2048x576 (transparent overlay), far (hazed skyline, few lit
            windows), mid (skyline, window grids, neon trim and signs), near (street level: shops, lamps, billboards, all <= 190 px), preview.
Layers wrap seamlessly in X (periodic noise, wrapped masks). Preview is for review only. It reuses the Countryside helpers
(paint_hill, mountains, env) unchanged; nothing in make_bg_countryside.py is modified.
"""
import argparse, math, os, random, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_bg_countryside as mc
from smoothart import *
from make_bg_countryside import W, H, HN, SS, pdist, env, cache_cols, paint_hill, mountains, blit

def rect(m, x0, y0, x1, y1): m.polygon([(x0, y0), (x1, y0), (x1, y1), (x0, y1)])

def vfade(y0, y1, a0, a1, w=W, h=H):
    c = Cov(w, h); c.bb = (0, 0, w, h)
    for y in range(h):
        t = min(1.0, max(0.0, (y + .5 - y0) / (y1 - y0))); v = a0 + (a1 - a0) * t
        for x in range(w): c.d[y * w + x] = v
    return c

def sky_img(ramp):
    img = Image(4, H)
    for y in range(H):
        c = ramp((y + .5) / H)
        for x in range(4):
            i = y * 4 + x; img.r[i], img.g[i], img.b[i], img.a[i] = c[0], c[1], c[2], 1.0
    return img

def glow_disc(n, glow_col, glow_a, power=2.2):
    img = Image(n, n); c = n / 2; g = Cov(n, n); g.bb = (0, 0, n, n)
    for y in range(n):
        for x in range(n):
            d = math.hypot(x + .5 - c, y + .5 - c)
            if d < c: g.d[y * n + x] = (1 - d / c) ** power * glow_a
    img.paint(g, glow_col); return img

def radial_bleed(img):
    """Fill RGB of every alpha-0 pixel with the colour of the outermost visible pixel on the ray to the centre.
    Without this the transparent corners are black and bilinear filtering draws a dark rim/square round the glow (QA7-05)."""
    w, h = img.w, img.h; cx, cy = w / 2, h / 2
    for y in range(h):
        for x in range(w):
            i = y * w + x
            if img.a[i] > 0: continue
            dx, dy = cx - (x + .5), cy - (y + .5); L = math.hypot(dx, dy) or 1; k = 0
            while k < L:
                k += 1; xx = int(x + .5 + dx / L * k); yy = int(y + .5 + dy / L * k); j = yy * w + xx
                if 0 <= xx < w and 0 <= yy < h and img.a[j] > 0:
                    img.r[i], img.g[i], img.b[i] = img.r[j], img.g[j], img.b[j]; break
    for i in range(w * h):  # +-0.5/255 hashed alpha dither breaks 8-bit banding rings in the glow falloff
        if 0 < img.a[i] < 0.98:
            n = ((i * 2654435761) >> 7) & 255; img.a[i] = min(1.0, max(1.0 / 255, img.a[i] + (n / 255.0 - 0.5) / 255.0))
    return img

def shaded(items, ramp, dflt, reach=1.6):
    """Colour fn for blobs: items (cx, cy, R); t > 0 faces the upper-left light. Nearest item wins."""
    def f(x, y):
        best = None
        for cx, cy, R in items:
            d = pdist(x, cx)
            if d < R * reach and abs(y - cy) < R * reach:
                k = (d * d + (y - cy) ** 2) / (R * R)
                if best is None or k < best[0]: best = (k, cx, cy, R)
        if best is None: return dflt
        _, cx, cy, R = best; sx = ((x - cx + W / 2) % W) - W / 2
        return ramp(((-sx) * 0.55 + (-(y - cy)) * 0.8) / R)
    return f

# ================================================================================================================ DESERT
D = SCENERY["desert"]; DHAZE = hexrgb(D["haze"])

def desert_sky(): return sky_img(Ramp([(0, D["sky_top"]), (0.62, D["sky_hor"]), (1, D["haze"])]))

def desert_sun(n=256):
    img = glow_disc(n, (255, 238, 190), 0.8); c = n / 2
    img.paint(Mask(n, n, SS).circle(c, c, 50).cov(), vgrad(c - 50, c + 50, Ramp([(0, "#FFFDF0"), (1, "#FFEBB0")])))
    return img

def mesa_cols(specs, base, seed):
    """Per-column top y and u (position inside the dominant mesa, -1 left face .. 1 right face). specs (cx, hw, h, edge)."""
    tops, us = [], []
    for xi in range(W):
        x = xi + .5; best, bu = base, 0.0
        for cx, hw, h, edge in specs:
            d = ((x - cx + W / 2) % W) - W / 2; ad = abs(d)
            hgt = max(h * smoothstep(0, 1, (hw + edge - ad) / (2 * edge)), 0.33 * h * smoothstep(0, 1, (hw * 2.3 - ad) / (hw * 1.7)))
            if hgt > 0.5 and base - hgt < best: best, bu = base - hgt, max(-1.0, min(1.0, d / (hw + edge)))
        tops.append(best + 1.2 * wave_fbm(x, W, seed, 3, 11)); us.append(bu)
    return tops, us

def paint_mesas(img, specs, base, seed, lit, shade, haze_amt, h=H, strata=0.18):
    tops, us = mesa_cols(specs, base, seed); cl, cs = hexrgb(lit), hexrgb(shade)
    top = lambda x: tops[min(W - 1, max(0, int(x)))]
    def col(x, y):
        xi = min(W - 1, max(0, int(x))); d = y - tops[xi]; u = us[xi]
        c = lerp(cl, cs, smoothstep(0.1, 0.6, u))                              # right-hand face in shade (light upper left)
        band = 0.5 + 0.5 * math.sin(d / 8.0 + 2.5 * math.sin(d / 29.0))
        c = lerp(c, cs, strata * smoothstep(0.55, 0.8, band))                   # rock strata
        c = lerp(c, DHAZE if mc.HAZE == DHAZE else mc.HAZE, haze_amt * (1 - 0.55 * smoothstep(0, 200, d)))
        return lerp(c, (255, 255, 255), 0.18 * (1 - smoothstep(0, 4, d)))
    fill_below_curve(img, top, col, SS, bottom=h)

def desert_far():
    mc.HAZE = DHAZE; img = Image(W, H); rnd = random.Random(205)
    back = [(180, 150, 150, 18), (560, 70, 205, 14), (760, 36, 190, 9), (980, 120, 120, 16), (1330, 95, 175, 14), (1620, 44, 215, 10), (1800, 130, 135, 18), (1990, 40, 120, 8)]
    paint_mesas(img, back, 392, 211, "#EFB898", "#D58F6C", 0.38)
    front = mountains(446, 46, 215, 3, 0.07)
    paint_hill(img, front, None, "#EDBE97", "#D79F7A", 0.28, 140)
    return img

def desert_mid():
    mc.HAZE = DHAZE; img = Image(W, H); rnd = random.Random(233)
    out = [(310, 90, 72, 12), (1020, 60, 95, 10), (1480, 110, 60, 14), (1850, 70, 80, 12)]
    paint_mesas(img, out, 470, 237, "#D98F60", "#B96D47", 0.14)
    ground = mountains(478, 80, 241, 3, 0.07)
    gc = cache_cols(ground)
    paint_hill(img, ground, None, "#DE9A66", "#BA6E48", 0.10, 140)
    cact = Mask(W, H, SS, True); rocks = Mask(W, H, SS, True)
    for _ in range(14):
        x = rnd.uniform(0, W); g = gc(x) + 3; h = rnd.uniform(26, 48); r = h * 0.09
        cact.capsule(x, g, x, g - h, r); a = rnd.choice((-1, 1)); ah = h * rnd.uniform(.3, .45)
        cact.polyline([(x, g - h * .45), (x + a * r * 3.2, g - h * .45), (x + a * r * 3.2, g - h * .45 - ah)], r * .75)
    for _ in range(22):
        x = rnd.uniform(0, W); g = gc(x) + 3; rr = rnd.uniform(7, 15); rocks.ellipse(x, g - rr * .25, rr * 1.2, rr * .65)
    img.paint(rocks.cov(), "#B06C48"); img.paint(cact.cov(), vgrad(380, 520, Ramp([(0, "#8FA066"), (1, "#6F8458")])))
    return img

def desert_near():
    mc.HAZE = DHAZE; img = Image(W, HN); rnd = random.Random(277)
    ground = lambda x: 772 + 30 * env(x, 61, 0.35) * (0.7 * wave_fbm(x, W, 62, octaves=2, base=2) + 0.3 * wave_fbm(x, W, 63, octaves=3, base=5))
    gc = cache_cols(ground)
    paint_hill(img, ground, None, "#B06B45", "#8A4B31", 0.0, 220, ss=SS, h=HN, rim=0.30)
    spots = []
    kinds = ["saguaro", "rock", "agave", "saguaro", "barrel", "rock", "saguaro", "agave", "rock", "barrel", "saguaro", "agave"]
    for _ in range(600):
        if len(spots) >= len(kinds): break
        x = rnd.uniform(0, W)
        if all(pdist(x, s[0]) > 130 for s in spots): spots.append((x, kinds[len(spots)], rnd.uniform(0.75, 1.15)))
    cact = Mask(W, HN, SS, True); verts = []; flowers = Mask(W, HN, SS, True); rocks = Mask(W, HN, SS, True); ritems = []
    ag = Mask(W, HN, SS, True); rib = Mask(W, HN, SS, True)
    for x, kind, s in spots:
        g = gc(x) + 8
        if kind == "saguaro":
            hgt = 170 * s; r = 14 * s; cact.capsule(x, g, x, g - hgt, r); verts.append((x, g - hgt / 2, hgt / 2 + r))
            for side, yy, aw, ah in ((-1, 0.42, 3.6, 0.30), (1, 0.55, 3.4, 0.26)):
                if kind == "saguaro" and (rnd.random() < 0.2 and side == -1): continue
                ax = x + side * r * aw; ay = g - hgt * yy
                cact.polyline([(x, ay), (ax, ay), (ax, ay - hgt * ah)], r * .66)
                verts.append((ax, ay - hgt * ah / 2, hgt * ah / 2 + r * .7))
            if rnd.random() < 0.6: flowers.circle(x, g - hgt - r * .6, 5 * s); flowers.circle(x + 7 * s, g - hgt - r * .2, 4 * s)
        elif kind == "barrel":
            r = 26 * s; cact.ellipse(x, g - r * .85, r, r * .95); verts.append((x, g - r * .85, r * 1.1)); flowers.circle(x, g - r * 1.7, 5 * s)
        elif kind == "rock":
            R = 44 * s
            for dx, dy, rx, ry in ((0, 0, 1.0, .62), (-.7, .18, .6, .38), (.75, .22, .55, .34)):
                rocks.ellipse(x + dx * R, g - R * .35 + dy * R * .3, rx * R, ry * R)
            ritems.append((x, g - R * .45, R * 1.1))
        else:
            for k in range(9):
                a = -math.pi / 2 + (k - 4) * 0.30; L = (52 + 8 * (k % 2)) * s
                ag.polygon([(x - 6 * s, g), (x + L * math.cos(a), g + L * math.sin(a)), (x + 6 * s, g)])
    img.paint(rocks.cov(), shaded(ritems, Ramp([(-1, "#6A3C2B"), (-0.2, "#8C5238"), (0.25, "#B06E4A"), (0.8, "#D6946A")]), hexrgb("#8A4B31")))
    img.paint(ag.cov(), vgrad(g0 := 690, 790, Ramp([(0, "#9FD0B0"), (1, "#4F8E78")])))
    img.paint(cact.cov(), shaded(verts, Ramp([(-1, "#2E6B44"), (-0.2, "#3F8A4E"), (0.4, "#5DAA5A"), (1, "#86CC6E")]), hexrgb("#3F8A4E"), 1.15))
    img.paint(flowers.cov(), "#FF8FB1")
    mound = Mask(W, HN, SS, True)                                      # soil mounds hide the round trunk ends so plants look planted
    for x, kind, s in spots:
        if kind in ("saguaro", "barrel", "agave"): mound.ellipse(x, gc(x) + 9, 34 * s, 9 * s)
    img.paint(mound.cov(), vgrad(740, 830, Ramp([(0, "#A8683F"), (1, "#8F5233")])))
    # dry tufts and pebbles along the crest
    tuft = Mask(W, HN, SS, True); peb = Mask(W, HN, SS, True)
    for _ in range(200):
        x = rnd.uniform(0, W); h = rnd.uniform(9, 22); w = rnd.uniform(2, 4); g = gc(x) + 3; lean = rnd.uniform(-5, 5)
        tuft.polygon([(x - w, g), (x + lean, g - h), (x + w, g)])
    for _ in range(60):
        x = rnd.uniform(0, W); g = gc(x) + rnd.uniform(4, 40); r = rnd.uniform(3, 8); peb.ellipse(x, g, r * 1.3, r * .7)
    img.paint(peb.cov(), vgrad(700, 830, Ramp([(0, "#C98258"), (1, "#9A5A3A")]))); img.paint(tuft.cov(), "#E2B766")
    return img

# ================================================================================================================ NIGHT CITY
N = SCENERY["night_city"]; NHAZE = hexrgb(N["haze"]); WARM, CY, MG = (255, 210, 120), (110, 235, 255), (255, 95, 205)

def night_sky(): return sky_img(Ramp([(0, N["sky_top"]), (0.42, "#252B63"), (0.66, N["sky_hor"]), (0.86, "#7A4F9F"), (1, "#A3639F")]))

def night_moon(n=320):
    img = glow_disc(n, (255, 150, 220), 0.55, 2.0); c = n / 2; R = 100
    disc = Mask(n, n, SS).circle(c, c, R).cov(); bands = Mask(n, n, SS)
    for i in range(6):                                              # synthwave slats growing toward the bottom
        y0 = c + 4 + i * 17; bands.polygon([(0, y0), (n, y0), (n, y0 + 2 + i * 1.6), (0, y0 + 2 + i * 1.6)])
    img.paint(disc.minus(bands.cov()), vgrad(c - R, c + R, Ramp([(0, "#FFF1FF"), (0.45, "#FFB3E6"), (1, "#FF5FC8")])))
    return img

def night_stars():
    img = Image(W, H); rnd = random.Random(311); a = Mask(W, H, SS, True); b = Mask(W, H, SS, True)
    for _ in range(230):
        x = rnd.uniform(0, W); y = (rnd.random() ** 1.5) * 330 + 8; r = rnd.uniform(1.0, 2.0)
        if rnd.random() < 0.12:
            r *= 2.1; b.polygon([(x, y - r), (x + r * .2, y - r * .2), (x + r, y), (x + r * .2, y + r * .2), (x, y + r), (x - r * .2, y + r * .2), (x - r, y), (x - r * .2, y - r * .2)])
        else: a.circle(x, y, r)
    img.paint(a.cov(), "#DDE6FF", 0.85); img.paint(b.cov(), "#FFFFFF", 0.95); return img

def skyline(seed, wlo, whi, hlo, hhi, gap_p, cl_seed):
    rnd = random.Random(seed); x = 0.0; out = []
    while x < W - 1:
        w = rnd.uniform(wlo, whi)
        if x + w > W - wlo * 0.6: w = W - x
        e = env(x + w / 2, cl_seed, 0.2); h = hlo + (hhi - hlo) * (e ** 1.2) * rnd.uniform(0.55, 1.0)
        out.append(dict(x0=x, x1=x + w, h=h, kind=rnd.choices(["flat", "step", "spire", "slant"], [4, 3, 2, 2])[0], tone=rnd.randrange(3)))
        x += w + (rnd.uniform(3, 14) if rnd.random() < gap_p and x + w < W - 20 else 0)
    return out

def city_layer(seed, cl_seed, ybase, wlo, whi, hlo, hhi, gap_p, top_col, bot_col, haze_amt, cell, lit_p, neon_p, spire_r):
    img = Image(W, H); rnd = random.Random(seed + 1); B = skyline(seed, wlo, whi, hlo, hhi, gap_p, cl_seed)
    bodies = [Mask(W, H, SS, True) for _ in range(3)]; rim = Mask(W, H, SS, True); roof = Mask(W, H, SS, True); spire = Mask(W, H, SS, True)
    wins = {k: Mask(W, H, SS, True) for k in ("w", "c", "m")}; trim = {"c": Mask(W, H, SS, True), "m": Mask(W, H, SS, True)}; sign = {"c": Mask(W, H, SS, True), "m": Mask(W, H, SS, True)}
    for b in B:
        x0, x1 = b["x0"], b["x1"]; w = x1 - x0; top = ybase - b["h"]; m = bodies[b["tone"]]; cx = (x0 + x1) / 2; roof_y = top
        if b["kind"] == "slant":
            m.polygon([(x0, top + 0.14 * b["h"] * 0.35 + 8), (x1, top), (x1, H), (x0, H)]); roof_y = top + 6
        else: rect(m, x0, top, x1, H)
        if b["kind"] == "step" and w > 50:
            u0, u1 = x0 + w * rnd.uniform(.18, .3), x1 - w * rnd.uniform(.18, .3); uh = rnd.uniform(18, 40); rect(m, u0, top - uh, u1, top + 2); roof.polygon([(u0, top - uh), (u1, top - uh), (u1, top - uh + 3), (u0, top - uh + 3)]); rim.polygon([(u0, top - uh), (u0 + 4, top - uh), (u0 + 4, top), (u0, top)])
        if b["kind"] == "spire": spire.capsule(cx, top, cx, top - rnd.uniform(25, 60), spire_r)
        rect(rim, x0, roof_y, x0 + 4, H); rect(roof, x0, roof_y, x1, roof_y + 3)
        # windows: lit cells on a grid, away from the edges
        cw, ch = cell; nx = int((w - 10) // cw); ny = int((H - 10 - roof_y) // ch)
        for ix in range(nx):
            for iy in range(ny):
                if rnd.random() < lit_p * (1.2 - 0.9 * iy / max(1, ny)):
                    wx = x0 + 6 + ix * cw + (w - 10 - nx * cw) / 2; wy = roof_y + 8 + iy * ch
                    if wy + ch > 520: continue
                    k = rnd.choices(("w", "c", "m"), (6, 3, 2))[0]; rect(wins[k], wx, wy, wx + cw * 0.5, wy + ch * 0.55)
        if rnd.random() < neon_p:
            k = rnd.choice(("c", "m")); trim[k].rrect(x0 - 1, roof_y - 1, x1 + 1, roof_y + 2.5, 1)
            if rnd.random() < 0.6 and w > 40:
                sx = x1 - rnd.uniform(6, 12); sh = rnd.uniform(40, 90); sign[rnd.choice(("c", "m"))].rrect(sx - 2.5, roof_y + 14, sx + 2.5, roof_y + 14 + sh, 2)
    lit, deep = hexrgb(top_col), hexrgb(bot_col)
    body_ramp = Ramp([(0, lit), (1, deep)])
    for k, m in enumerate(bodies):
        f = (0.92, 1.0, 1.1)[k]
        img.paint(m.cov(), lambda x, y, f=f: tuple(min(255, c * f) for c in body_ramp(min(1.0, max(0.0, (y - 150) / 426.0)))))
    allb = Mask(W, H, SS, True)
    # haze/glow at the base of the skyline (atmospheric perspective, synthwave horizon)
    bc = bodies[0].cov()
    for m in bodies[1:]:
        c2 = m.cov(); bc = Cov(W, H, [max(a, b) for a, b in zip(bc.d, c2.d)], (0, 0, W, H))
    img.paint(bc.multiplied(vfade(240, 540, 0.0, haze_amt)), "#9A63BF")
    img.paint(bc.multiplied(vfade(150, 420, haze_amt * 0.5, 0.0)), N["haze"])
    img.paint(rim.cov(), lerp(NHAZE, (200, 215, 255), 0.5), 0.30 + 0.2 * (1 - haze_amt)); img.paint(roof.cov(), (190, 205, 255), 0.35)
    img.paint(spire.cov(), lerp(deep, NHAZE, 0.5))
    for k, col in (("w", WARM), ("c", CY), ("m", MG)):
        c = wins[k].cov(); img.paint(c.blur(2, 2, True), col, 0.28); img.paint(c, col, 0.9)
    for k, col in (("c", CY), ("m", MG)):
        for m in (trim[k], sign[k]):
            c = m.cov(); img.paint(c.blur(4, 3, True), col, 0.55); img.paint(c, lerp(col, (255, 255, 255), 0.5))
    return img

def night_far():
    mc.HAZE = NHAZE
    return city_layer(401, 17, 470, 34, 96, 80, 250, 0.35, "#4A5399", "#2B3260", 0.55, (9, 12), 0.10, 0.10, 1.2)

def night_mid():
    mc.HAZE = NHAZE
    return city_layer(421, 29, 560, 56, 130, 110, 330, 0.30, "#2B3266", "#1B2048", 0.40, (14, 18), 0.22, 0.30, 2.0)

def night_near():
    mc.HAZE = NHAZE; img = Image(W, HN); rnd = random.Random(471)
    ground = lambda x: 772 + 30 * env(x, 61, 0.35) * (0.7 * wave_fbm(x, W, 62, octaves=2, base=2) + 0.3 * wave_fbm(x, W, 63, octaves=3, base=5))
    gc = cache_cols(ground)
    paint_hill(img, ground, None, "#252B5C", "#12152F", 0.0, 220, ss=SS, h=HN, rim=0.14)
    kinds = [("shop", 112), ("lamp", 40), ("board", 100), ("shop", 120), ("lamp", 40), ("shop", 100), ("board", 100), ("lamp", 40)]
    spots = []
    for _ in range(800):
        if len(spots) >= len(kinds): break
        x = rnd.uniform(0, W); k, hw = kinds[len(spots)]
        if all(pdist(x, s[0]) > hw + s[3] + 40 for s in spots): spots.append((x, k, rnd.uniform(0.9, 1.1), hw))
    body = Mask(W, HN, SS, True); pole = Mask(W, HN, SS, True); rim = Mask(W, HN, SS, True); wins = {k: Mask(W, HN, SS, True) for k in ("w", "c", "m")}
    neon = {"c": Mask(W, HN, SS, True), "m": Mask(W, HN, SS, True)}; lamp = Mask(W, HN, SS, True); panel = Mask(W, HN, SS, True); awn = Mask(W, HN, SS, True)
    for x, kind, s, hw in spots:
        g = gc(x) + 10
        if kind == "shop":
            w = hw * 2 * s; h = rnd.uniform(96, 140); x0, x1 = x - w / 2, x + w / 2
            body.rrect(x0, g - h, x1, g + 10, 5); rect(rim, x0, g - h + 3, x0 + 3.5, g)
            awn.polygon([(x0 - 4, g - h * .42), (x1 + 4, g - h * .42), (x1, g - h * .42 + 15), (x0, g - h * .42 + 15)])
            for i in range(int((w - 24) // 34)):
                wx = x0 + 14 + i * 34; wins[rnd.choices(("w", "c", "m"), (6, 2, 2))[0]].rrect(wx, g - h + 16, wx + 20, g - h + 38, 2)
            doorx = x0 + w * rnd.uniform(.3, .6); wins["w"].rrect(doorx, g - h * .38 + 18, doorx + 26, g, 2)
            k = rnd.choice(("c", "m")); neon[k].rrect(x0 + 10, g - h + 5, x1 - 10, g - h + 8.5, 1.5)
        elif kind == "lamp":
            hh = 150 * s; pole.capsule(x, g, x, g - hh, 3.2); pole.capsule(x, g - hh, x + 26, g - hh - 2, 2.6); lamp.rrect(x + 16, g - hh - 4, x + 40, g - hh + 3, 3)
        else:
            hh = 120 * s; pole.capsule(x - 34, g, x - 34, g - hh, 3.4); pole.capsule(x + 34, g, x + 34, g - hh, 3.4)
            panel.rrect(x - 62, g - hh - 58, x + 62, g - hh + 2, 6)
    img.paint(body.cov(), vgrad(600, 790, Ramp([(0, "#2B3270"), (1, "#1A1E46")])))
    img.paint(awn.cov(), "#3A2A6E"); img.paint(pole.cov(), "#222855")
    img.paint(rim.cov(), "#8FA2FF", 0.45)
    c = panel.cov(); img.paint(c.blur(10, 3, True), MG, 0.35); img.paint(c, vgrad(600, 800, Ramp([(0, "#EE6AD0"), (1, "#7A45D8")])), 0.9)
    for k, col in (("w", WARM), ("c", CY), ("m", MG)):
        c = wins[k].cov(); img.paint(c.blur(2, 2, True), col, 0.3); img.paint(c, col, 0.85)
    for k, col in (("c", CY), ("m", MG)):
        c = neon[k].cov(); img.paint(c.blur(4, 3, True), col, 0.6); img.paint(c, lerp(col, (255, 255, 255), 0.5))
    c = lamp.cov(); img.paint(c.blur(12, 3, True), WARM, 0.45); img.paint(c, (255, 240, 200))
    return img

# ================================================================================================================ preview
def compose(sky, sun, sunpos, far, mid, near, stars=None):
    out = Image(W, H, (0, 0, 0, 1.0))
    for y in range(H):
        for x in range(W):
            i = y * 4; j = y * W + x; out.r[j], out.g[j], out.b[j] = sky.r[i], sky.g[i], sky.b[i]
    if stars is not None: blit(out, stars, 0, 0)
    blit(out, sun, sunpos[0] - sun.w // 2, sunpos[1] - sun.h // 2); blit(out, far, 0, 0); blit(out, mid, 0, 0); blit(out, near, 0, H - HN)
    return out

WORLDS = {
    "desert": dict(sky=desert_sky, sun=desert_sun, far=desert_far, mid=desert_mid, near=desert_near, sunname="sun_256", sunpos=(1450, 190)),
    "nightcity": dict(sky=night_sky, sun=night_moon, far=night_far, mid=night_mid, near=night_near, stars=night_stars, sunname="moon_320", sunpos=(1500, 190)),
}

if __name__ == "__main__":
    ap = argparse.ArgumentParser(); ap.add_argument("--world", required=True, choices=list(WORLDS)); ap.add_argument("--output", required=True)
    ap.add_argument("--only", default="sky,sun,stars,far,mid,near,preview"); a = ap.parse_args()
    os.makedirs(a.output, exist_ok=True); Wd = WORLDS[a.world]; only = a.only.split(","); pre = "bg_%s_" % a.world; L = {}
    names = {"sky": "sky_4x576", "sun": Wd["sunname"], "stars": "stars_2048x576", "far": "far_2048x576", "mid": "mid_2048x576", "near": "near_2048x864"}
    for key in ("sky", "sun", "stars", "far", "mid", "near"):
        if key not in Wd or not (key in only or "preview" in only): continue
        im = Wd[key](); L[key] = im
        if key in only:
            write_png(im, os.path.join(a.output, pre + names[key] + ".png")); print(pre + names[key] + ".png", flush=True)
            if key in ("far", "mid", "near"): print("  seam score:", round(tile_seam_score(im), 3), flush=True)
    if "preview" in only:
        write_png(compose(L["sky"], L["sun"], Wd["sunpos"], L["far"], L["mid"], L["near"], L.get("stars")), os.path.join(a.output, pre + "preview_2048x576.png"), do_bleed=False)
