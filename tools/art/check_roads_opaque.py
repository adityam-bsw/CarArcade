#!/usr/bin/env python3
"""QA7-06 checks. python3 tools/art/check_roads_opaque.py <dir> [<dir2>...]
Per strip: size, bottom-row alpha, opaque-body test (every pixel below lip+2 has alpha 255, except columns with no lip), lip row mean/min/max,
seam notch test on a 3x tiling: lip(x) step across x=1023|1024 and 2047|2048 compared with the largest step between interior neighbours, and
the RGB step across the seam vs the interior mean (seam ratio). PASS needs: opaque, seam lip step <= max(1, interior max step), ratio < 2.5."""
import sys, os, glob, re
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'assets')); import pixel
bad = 0
print("%-20s %5s %6s %9s %6s %12s %8s %s" % ("surface", "size", "botA", "nonOpaque", "lip", "seamStep/max", "rgbRatio", "result"))
for d in sys.argv[1:]:
    for f in sorted(glob.glob(os.path.join(d, "road_*_1024x192.png"))):
        n = re.match(r"road_(.*)_1024x192", os.path.basename(f)).group(1)
        if "crack" in n: continue
        w, h, rows = pixel.read_png(f)
        lip = [next((y for y in range(h) if rows[y][x][3] >= 128), None) for x in range(w)]
        L = [l for l in lip if l is not None]; nonop = sum(1 for x in range(w) if lip[x] is not None for y in range(lip[x] + 2, h) if rows[y][x][3] < 255)
        bot = min(rows[h - 1][x][3] for x in range(w))
        # 3x tiling lip profile
        t3 = lip * 3; steps = [abs(t3[i] - t3[i - 1]) for i in range(1, len(t3)) if t3[i] is not None and t3[i - 1] is not None]
        interior = max(abs(lip[i] - lip[i - 1]) for i in range(1, w) if lip[i] is not None and lip[i - 1] is not None)
        seam = max(abs((lip[0] or 0) - (lip[w - 1] or 0)), 0)
        def col_step(a, b): return sum(abs(rows[y][a][c] - rows[y][b][c]) for y in range(h) for c in range(3) if rows[y][a][3] > 0 and rows[y][b][3] > 0) / h
        inner = sum(col_step(x, x + 1) for x in range(0, w - 1, 16)) / len(range(0, w - 1, 16)); ratio = col_step(w - 1, 0) / (inner or 1e-6)
        ok = bot == 255 and nonop == 0 and w == 1024 and h == 192 and seam <= max(1, interior) and ratio < 2.5
        bad += 0 if ok else 1
        print("%-20s %5s %6d %9d %6.1f %6d/%-5d %8.2f %s" % (n, "%dx%d" % (w, h), bot, nonop, sum(L) / len(L), seam, interior, ratio, "PASS" if ok else "FAIL"))
print("FAILS:", bad)
