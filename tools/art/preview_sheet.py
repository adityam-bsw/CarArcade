#!/usr/bin/env python3
"""Review helper: lay PNGs side by side on a flat backdrop (or a Countryside crop) with an optional integer upscale.
    preview_sheet.py out.png "#9CC9C4" 2 a.png b.png ...        [greyscale: add --grey]   (review only, not a shipped asset)"""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "assets")); import pixel
args = [a for a in sys.argv[1:] if not a.startswith("--")]; grey = "--grey" in sys.argv
out, bg, k, files = args[0], args[1], int(args[2]), args[3:]
bgc = tuple(int(bg[i:i + 2], 16) for i in (1, 3, 5))
imgs = [pixel.read_png(f) for f in files]
pad = 16
W = sum(w for w, h, r in imgs) * k + pad * (len(imgs) + 1); H = max(h for w, h, r in imgs) * k + pad * 2
rows = [[bgc + (255,)] * W for _ in range(H)]
x0 = pad
for w, h, r in imgs:
    for y in range(h):
        for x in range(w):
            p = r[y][x]; a = p[3] / 255
            if a <= 0: continue
            for dy in range(k):
                for dx in range(k):
                    q = rows[pad + y * k + dy][x0 + x * k + dx]
                    rows[pad + y * k + dy][x0 + x * k + dx] = tuple(int(p[i] * a + q[i] * (1 - a)) for i in range(3)) + (255,)
    x0 += w * k + pad
if grey:
    rows = [[(lambda g: (g, g, g, 255))(int(0.2126 * p[0] + 0.7152 * p[1] + 0.0722 * p[2])) for p in row] for row in rows]
pixel.write_png(out, W, H, rows)
