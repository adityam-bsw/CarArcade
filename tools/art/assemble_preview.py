#!/usr/bin/env python3
"""Review helper: assemble body + two wheels exactly like RoadCar.cs does (body stretched to its world box, wheel diameter
2*r*1.03 at the anchors) and write one PNG per vehicle, optionally on a flat backdrop. Not a shipped asset.
    assemble_preview.py <dir with body_*.png wheel_*.png> <out_dir> [--bg "#9CC9C4"] [--scale 1]"""
import sys, os, math
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "assets")); import pixel
D, OUT = sys.argv[1], sys.argv[2]
bg = sys.argv[sys.argv.index("--bg") + 1] if "--bg" in sys.argv else None
# name: (box w, box h units, wheel radius, back anchor x, anchor y (up positive))
RIG = {"classic": (2.4, 0.9, 0.42, 0.72, -0.42), "sport": (2.4, 0.8, 0.40, 0.70, -0.38), "monster": (2.6, 1.1, 0.52, 0.78, -0.48)}
PPU = 200
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
os.makedirs(OUT, exist_ok=True)
for name, (bw, bh, wr, ax, ay) in RIG.items():
    f = os.path.join(D, "body_%s.png" % name)
    if not os.path.exists(f): continue
    w, h, rows = pixel.read_png(f)
    pad = 100; W, H = w + 2 * pad, h + 2 * pad + 120
    dst = [[(0, 0, 0, 0)] * W for _ in range(H)]
    if bg:
        c = tuple(int(bg[i:i + 2], 16) for i in (1, 3, 5)) + (255,); dst = [[c] * W for _ in range(H)]
    sx, sy = w / bw, h / bh      # canvas px per world unit
    blit(dst, W, H, rows, w, h, pad, pad)
    ww, wh, wrows = pixel.read_png(os.path.join(D, "wheel_%s.png" % name))
    d = 2 * wr * 1.03
    pw, ph = int(round(d * sx)), int(round(d * sy))
    wr2 = resize(wrows, ww, wh, pw, ph)
    for sgn in (-1, 1):
        cx = pad + w / 2 + sgn * ax * sx; cy = pad + h / 2 - ay * sy
        blit(dst, W, H, wr2, pw, ph, int(round(cx - pw / 2)), int(round(cy - ph / 2)))
    pixel.write_png(os.path.join(OUT, "assembled_%s.png" % name), W, H, dst)
    print(name, W, H)
