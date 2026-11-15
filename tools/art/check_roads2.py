"""Greyscale / lip checks for road strips. python3 tools/art/check_roads2.py <dir_new> [<dir_old>]
Lip row = first row whose alpha >= 0.5, averaged over x (what RoadSkin.lipPx should be; round it). Also min/max (collider-vs-art spread),
mean luma of the body, texture energy, silhouette roughness; pairwise table flags pairs that are not separable in greyscale.
Writes _sheet_grey.png (every strip, colour left / luma right, half size)."""
import sys, os, glob, re
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'assets')); import pixel
dirs = sys.argv[1:]; files = []
for d in dirs: files += sorted(glob.glob(os.path.join(d, "road_*_1024x192.png")))
files = [f for f in files if "crack" not in f]
luma = lambda r, g, b: 0.299 * r + 0.587 * g + 0.114 * b
st = {}; sheet = []; BG = (110, 170, 225)
for f in files:
    n = re.match(r"road_(.*)_1024x192", os.path.basename(f)).group(1); w, h, rows = pixel.read_png(f)
    top = []
    for x in range(1024):
        t = 0
        for y in range(192):
            if rows[y][x][3] >= 128: t = y; break
        top.append(t)
    m = sum(top) / 1024; mn, mx = min(top), max(top)
    vals = [luma(*rows[y][x][:3]) for y in range(24, 80) for x in range(0, 1024, 2) if rows[y][x][3] > 200]
    mean = sum(vals) / len(vals); ge = c = 0
    for y in range(24, 80):
        for x in range(0, 1023):
            a, b = rows[y][x], rows[y][x + 1]
            if a[3] > 200 and b[3] > 200: ge += abs(luma(*a[:3]) - luma(*b[:3])); c += 1
    rough = sum(abs(top[i] - top[i - 1]) for i in range(1024)) / 1024
    # opaque-area fraction in rows 0..100: a gappy silhouette (bridge planks, posts) shows here
    area = sum(1 for y in range(0, 100) for x in range(0, 1024, 4) if rows[y][x][3] > 128) / (100 * 256)
    st[n] = (m, mn, mx, mean, ge / c, rough, area)
    for y in range(0, 192, 3):
        o = []
        for x in range(0, 1024, 3):
            r, g, b, a = rows[y][x]; k = a / 255; R, G, B = r * k + BG[0] * (1 - k), g * k + BG[1] * (1 - k), b * k + BG[2] * (1 - k); o.append((int(R), int(G), int(B), 255))
        sheet.append(o + [(30, 30, 30, 255)] * 3 + [(lambda v: (v, v, v, 255))(int(luma(*p[:3]))) for p in o])
    sheet += [[(30, 30, 30, 255)] * len(sheet[0])] * 2
pixel.write_png(os.path.join(dirs[0], "_sheet_grey.png"), len(sheet[0]), len(sheet), sheet)
print("%-14s %6s %4s %4s %7s %7s %6s %6s" % ("surface", "lipRow", "min", "max", "bodyL", "texE", "rough", "area"))
for n, s in st.items(): print("%-14s %6.1f %4d %4d %7.1f %7.2f %6.2f %6.2f   lipPx=%d" % (n, s[0], s[1], s[2], s[3], s[4], s[5], s[6], round(s[0])))
print("pairs not separable (dL<14 and dTex<1.5 and dRough<0.8 and dArea<0.08):")
names = list(st); bad = 0
for i, a in enumerate(names):
    for b in names[i + 1:]:
        A, B = st[a], st[b]
        if abs(A[3] - B[3]) < 14 and abs(A[4] - B[4]) < 1.5 and abs(A[5] - B[5]) < 0.8 and abs(A[6] - B[6]) < 0.08: print("  WEAK", a, b); bad += 1
print("weak pairs:", bad)
