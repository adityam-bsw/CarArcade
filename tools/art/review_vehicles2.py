#!/usr/bin/env python3
"""Review sheet for the restyled catalogue vehicles: colour on Countryside sky/ground, greyscale, and old art for comparison.
    python3 tools/art/review_vehicles2.py <vehicles2 dir> <out.png> [--ids a,b,c] [--old] [--grey] [--cols N]
Reads PNGs with tools/assets/pixel.py. Standard library only."""
import argparse, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "assets")); sys.path.insert(0, HERE)
import pixel
from veh2lib import INV

def lerp(a, b, t): return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))

def over(dst, src, x0, y0, grey=False):
    sw, sh, rows = src
    for y in range(sh):
        dy = y0 + y
        if not (0 <= dy < len(dst)): continue
        row = dst[dy]; srow = rows[y]
        for x in range(sw):
            dx = x0 + x
            if not (0 <= dx < len(row)): continue
            r, g, b, a = srow[x]
            if a <= 0: continue
            if grey:
                v = int(0.299 * r + 0.587 * g + 0.114 * b); r = g = b = v
            d = row[dx]; k = a / 255.0
            row[dx] = (d[0] * (1 - k) + r * k, d[1] * (1 - k) + g * k, d[2] * (1 - k) + b * k, 255)

def scaled(rows, w, h, nw):
    """Area-average downscale with premultiplied alpha (review only)."""
    nh = max(1, int(h * nw / w)); out = []; fx = w / nw; fy = h / nh
    for y in range(nh):
        ya = int(y * fy); yb = max(ya + 1, int((y + 1) * fy)); row = []
        for x in range(nw):
            xa = int(x * fx); xb = max(xa + 1, int((x + 1) * fx)); r = g = b = al = 0.0; n = 0
            for yy in range(ya, min(h, yb)):
                srow = rows[yy]
                for xx in range(xa, min(w, xb)):
                    pr, pg, pb, pa = srow[xx]; r += pr * pa; g += pg * pa; b += pb * pa; al += pa; n += 1
            row.append((r / al, g / al, b / al, al / n) if al > 0 else (0, 0, 0, 0))
        out.append(row)
    return nw, nh, out

def scene(a):
    """Vehicles at the in-game scale (41 px per unit on a 576 px tall Countryside view) on the Countryside backdrop and asphalt strip."""
    ids = a.ids.split(",")
    bgw, bgh, bg = pixel.read_png(a.scene[0]); rw, rh, road = pixel.read_png(a.scene[1])
    k = 41.0 / 128.0
    W = a.width; x0 = 400
    img = [[(p[0], p[1], p[2], 255) for p in bg[y][x0:x0 + W]] for y in range(bgh)]
    rw2, rh2, road2 = scaled(road, rw, rh, int(rw * k))
    gy = 440
    for t in range(0, W, rw2 - 1):
        over(img, (rw2, rh2, road2), t, gy - 2)
    x = 40
    for i in ids:
        w, h, rows = pixel.read_png(os.path.join(a.dir, a.sub, i + ".png"))
        w2, h2, r2 = scaled(rows, w, h, max(1, int(w * k)))
        yb = gy + 2 + int(INV[i]["S"] * k)
        over(img, (w2, h2, r2), x, yb - h2, a.grey)
        x += w2 + 24
    pixel.write_png(a.out, W, bgh, [[(int(p[0]), int(p[1]), int(p[2]), 255) for p in r] for r in img])

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("dir"); ap.add_argument("out"); ap.add_argument("--ids", default="")
    ap.add_argument("--old", action="store_true"); ap.add_argument("--grey", action="store_true"); ap.add_argument("--cols", type=int, default=2)
    ap.add_argument("--scene", nargs=2, default=None); ap.add_argument("--width", type=int, default=1100); ap.add_argument("--sub", default="previews"); ap.add_argument("--scale", type=float, default=1.0)
    a = ap.parse_args()
    if a.scene: return scene(a)
    ids = a.ids.split(",") if a.ids else sorted(f[:-4] for f in os.listdir(os.path.join(a.dir, a.sub)) if f.endswith(".png"))
    cell_w = int(max(INV[i]["W"] * INV[i]["S"] for i in ids) * a.scale) + 40
    cell_h = int(max(INV[i]["H"] * INV[i]["S"] for i in ids) * a.scale) + 60
    cols = a.cols; nrows = (len(ids) + cols - 1) // cols
    W, H = cell_w * cols, cell_h * nrows
    top, hor, gr = (91, 182, 240), (205, 235, 255), (62, 138, 79)
    img = []
    for y in range(H):
        ty = (y % cell_h) / cell_h
        base = lerp(top, hor, min(1.0, ty / 0.78)) if ty < 0.78 else gr
        img.append([base + (255,)] * W)
    for n, i in enumerate(ids):
        cx, cy = (n % cols) * cell_w, (n // cols) * cell_h
        p = os.path.join(a.dir, a.sub, i + ".png")
        w, h, rows = pixel.read_png(p)
        if a.scale != 1.0: w, h, rows = scaled(rows, w, h, int(w * a.scale))
        x0 = cx + 20; y0 = cy + int(cell_h * 0.78) - h + int(1.0 * INV[i]["S"] * a.scale)
        over(img, (w, h, rows), x0, y0, a.grey)
        if a.old:
            ow, oh, orows = pixel.read_png(os.path.join(HERE, "..", "..", "Assets/Art/Catalog/Previews", i + ".png"))
            k = max(1, int(round(w / ow)))
            orows = pixel.scale_nearest(orows, k)
            over(img, (ow * k, oh * k, orows), cx + 20, cy + 10)
    pixel.write_png(a.out, W, H, [[(int(p[0]), int(p[1]), int(p[2]), 255) for p in r] for r in img])
main()
