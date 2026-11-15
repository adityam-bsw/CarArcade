#!/usr/bin/env python3
"""Readability check for vehicles and pickups against the smooth Countryside (Bible 6: tell/hazard vs scenery >= 3:1;
vehicles and pickups should at least separate by outline). Review only.
    check_sprite_contrast.py <bg dir (make_bg_countryside output)> <road dir (make_road asphalt)> <vehicles dir> <pickups dir> <out dir>
Builds a 1080p-scale scene (14 units = 1080 px, so 77 px per world unit): bg layers, road strip, the three assembled vehicles,
all pickups. Writes scene_colour.png, scene_grey.png, icons_grey.png (and prints WCAG luminance ratios per sprite)."""
import sys, os, math
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "assets")); import pixel
BG, ROAD, VEH, PICK, OUT = sys.argv[1:6]; os.makedirs(OUT, exist_ok=True)
U = 77.0
def resize(rows, w, h, nw, nh):
    out = []
    for y in range(nh):
        fy = (y + 0.5) * h / nh - 0.5; y0 = max(0, min(h - 1, int(math.floor(fy)))); y1 = min(h - 1, y0 + 1); ty = fy - math.floor(fy)
        row = []
        for x in range(nw):
            fx = (x + 0.5) * w / nw - 0.5; x0 = max(0, min(w - 1, int(math.floor(fx)))); x1 = min(w - 1, x0 + 1); tx = fx - math.floor(fx)
            ps = [rows[y0][x0], rows[y0][x1], rows[y1][x0], rows[y1][x1]]; ws = [(1 - tx) * (1 - ty), tx * (1 - ty), (1 - tx) * ty, tx * ty]
            a = sum(p[3] * k for p, k in zip(ps, ws))
            if a <= 0: row.append((0, 0, 0, 0)); continue
            row.append(tuple(int(sum(p[i] * p[3] * k for p, k in zip(ps, ws)) / a) for i in range(3)) + (int(a),))
        out.append(row)
    return out
def blit(dst, W, H, src, sw, sh, x0, y0):
    for y in range(sh):
        for x in range(sw):
            p = src[y][x]; a = p[3] / 255; X, Y = x0 + x, y0 + y
            if a <= 0 or not (0 <= X < W and 0 <= Y < H): continue
            q = dst[Y][X]; qa = q[3] / 255; oa = a + qa * (1 - a)
            dst[Y][X] = tuple(int((p[i] * a + q[i] * qa * (1 - a)) / oa) for i in range(3)) + (int(oa * 255),)
def lin(c):
    c /= 255; return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
def L(p): return 0.2126 * lin(p[0]) + 0.7152 * lin(p[1]) + 0.0722 * lin(p[2])
def ratio(a, b): hi, lo = max(a, b), min(a, b); return (hi + .05) / (lo + .05)
INKL = L((0x18, 0x20, 0x2A))
W, H = 1100, 520
# background: preview composite (2048x576 for 14 units) scaled to 77 px/unit => height 1080; take the lower part
pw, ph, prow = pixel.read_png(os.path.join(BG, "bg_countryside_preview_2048x576.png"))
sc = U * 14 / 576.0
big = resize(prow, pw, ph, int(pw * sc), int(ph * sc))
oy = len(big) - H - 80; ox = 700
scene = [[big[oy + y][ox + x][:3] + (255,) for x in range(W)] for y in range(H)]
rw, rh, rr = pixel.read_png(os.path.join(ROAD, "road_asphalt_1024x192.png"))
rk = U / 128.0; rs = resize(rr, rw, rh, int(rw * rk), int(rh * rk)); roadtop = 330
for tx in range(-1, 3):
    blit(scene, W, H, rs, len(rs[0]), len(rs), tx * len(rs[0]), roadtop)
base = [row[:] for row in scene]
items = []     # name, rows, w, h, x, y (top-left)
RIG = {"classic": (2.4, 0.9, 0.42, 0.72, -0.42), "sport": (2.4, 0.8, 0.40, 0.70, -0.38), "monster": (2.6, 1.1, 0.52, 0.78, -0.48)}
xs = {"classic": 20, "sport": 270, "monster": 540}
for name, (bw, bh, wr, ax, ay) in RIG.items():
    w, h, rows = pixel.read_png(os.path.join(VEH, "body_%s.png" % name))
    ppu_c = w / bw; ppy = h / bh; k = U / ppu_c
    layer_w, layer_h = int(w * k) + 60, int(h * k * 1.0) + 120
    L_ = [[(0, 0, 0, 0)] * (int(w * k) + 4) for _ in range(int(h * ky if False else h * U / ppy) + 4 + int(2 * wr * U))] if False else None
    bwid, bhei = int(round(bw * U)), int(round(bh * U))
    b = resize(rows, w, h, bwid, bhei)
    ww, wh, wrows = pixel.read_png(os.path.join(VEH, "wheel_%s.png" % name)); d = int(round(2 * wr * 1.03 * U)); wr2 = resize(wrows, ww, wh, d, d)
    lay_h = bhei + d; lay = [[(0, 0, 0, 0)] * bwid for _ in range(lay_h)]
    blit(lay, bwid, lay_h, b, bwid, bhei, 0, 0)
    cyl = bhei / 2 - ay * U
    for sgn in (-1, 1):
        blit(lay, bwid, lay_h, wr2, d, d, int(round(bwid / 2 + sgn * ax * U - d / 2)), int(round(cyl - d / 2)))
    # wheel bottom touches road top; wheel radius physics = wr
    bottom = cyl + wr * U
    items.append((name, lay, bwid, lay_h, xs[name], int(roadtop + 4 - bottom)))
x = 20
for pn, size in (("Coin.png", 0.65), ("Heart.png", 0.75), ("Star.png", 0.64), ("InkCanister.png", 0.85), ("Pickup_Ring.png", 1.6), ("Pickup_Ring_Overload.png", 1.6)):
    w, h, rows = pixel.read_png(os.path.join(PICK, pn)); s = int(round(size * U)); r2 = resize(rows, w, h, s, s)
    items.append((pn[:-4], r2, s, s, 60 + (len(items) - 3) * 150, 60 if "Ring" not in pn else 80 if 0 else 40))
# pickups hang in the sky at the top row, plus a second set at road height to test against the road/near layer
extra = []
for name, lay, w, h, x, y in list(items)[3:]:
    extra.append((name + "@road", lay, w, h, x, roadtop - h - 8 if 0 else roadtop - h + 10 + 150 * 0))
print("%-22s %9s %9s %9s %9s" % ("sprite", "bandL", "outline:1", "fill:1", "minband:1"))
final = [row[:] for row in base]
def measure(name, lay, w, h, x0, y0):
    mask = [[lay[y][x][3] > 20 for x in range(w)] for y in range(h)]
    R1, R2 = 2, 12
    band = []
    for y in range(-R2, h + R2):
        for x in range(-R2, w + R2):
            if 0 <= y < h and 0 <= x < w and mask[y][x]: continue
            near = False; ring = False
            for dy in range(-R2, R2 + 1, 2):
                for dx in range(-R2, R2 + 1, 2):
                    yy, xx = y + dy, x + dx
                    if 0 <= yy < h and 0 <= xx < w and mask[yy][xx]:
                        dd = max(abs(dy), abs(dx)); ring = True
                        if dd < R1 + 1: near = True
            if ring and not near:
                X, Y = x0 + x, y0 + y
                if 0 <= X < W and 0 <= Y < H: band.append(L(base[Y][X]))
    fills = [L(lay[y][x]) for y in range(h) for x in range(w) if lay[y][x][3] > 240 and abs(L(lay[y][x]) - INKL) > 0.02]
    if not band: return
    bl = sum(band) / len(band); fl = sum(fills) / len(fills) if fills else bl
    worst = min(ratio(INKL, b) for b in band)
    print("%-22s %9.3f %9.2f %9.2f %9.2f" % (name, bl, ratio(INKL, bl), ratio(fl, bl), worst))
for name, lay, w, h, x0, y0 in items:
    measure(name, lay, w, h, x0, y0); blit(final, W, H, lay, w, h, x0, y0)
# second row of pickups on the road line (against mid/near layer) for the contrast table and the picture
for name, lay, w, h, x0, y0 in items[3:]:
    y1 = roadtop - h - 20; x1 = x0 + 0
    measure(name + "@road", lay, w, h, x1, y1); blit(final, W, H, lay, w, h, x1, y1)
pixel.write_png(os.path.join(OUT, "scene_colour.png"), W, H, final)
grey = [[(lambda g: (g, g, g, 255))(int(0.2126 * p[0] + 0.7152 * p[1] + 0.0722 * p[2])) for p in row] for row in final]
pixel.write_png(os.path.join(OUT, "scene_grey.png"), W, H, grey)
