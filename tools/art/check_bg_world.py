"""Luminance contrast of hazard tells against any world composite. python3 tools/art/check_bg_world.py <dir> <world>   (world: countryside|desert|nightcity)
Reads bg_<world>_preview_2048x576.png and the layers. WCAG ratio = (L1+.05)/(L2+.05); Bible 6 asks >= 3:1.
Prints per band: ratio of each tell to the mean band luminance and the share of pixels where it reaches 3:1.
Also checks the hazard with its rim: a tell drawn with a DeepInk outline OR a WarmCream outline (best of both) is reported as 'best outline'."""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "assets")); import pixel
D, WORLD = sys.argv[1], sys.argv[2]
def lin(c):
    c /= 255; return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
def L(r, g, b): return 0.2126 * lin(r) + 0.7152 * lin(g) + 0.0722 * lin(b)
def hx(h): return L(int(h[1:3], 16), int(h[3:5], 16), int(h[5:7], 16))
def ratio(a, b): hi, lo = max(a, b), min(a, b); return (hi + .05) / (lo + .05)
tells = {"SignalRed": hx("#D94B40"), "DeepInk": hx("#18202A"), "WarmCream": hx("#E7E0CF"), "IceMid": hx("#8CC8F0"), "GoldLight": hx("#F6C945")}
w, h, rows = pixel.read_png(os.path.join(D, "bg_%s_preview_2048x576.png" % WORLD))
bands = {"sky (y 40-250)": (40, 250), "horizon/far+mid (y 250-430)": (250, 430), "near (y 480-576)": (480, 576)}
print("world:", WORLD); print("%-30s %6s" % ("band", "meanL"), *["%22s" % k for k in tells], "%22s" % "best outline")
for bn, (a, b) in bands.items():
    ls = [L(*rows[y][x][:3]) for y in range(a, b, 2) for x in range(0, w, 2)]; m = sum(ls) / len(ls); cells = []
    for tn, tl in tells.items():
        ok = sum(1 for l in ls if ratio(tl, l) >= 3) / len(ls); cells.append("%4.2f:1 (%3d%% >=3)" % (ratio(tl, m), ok * 100))
    ink, cream = tells["DeepInk"], tells["WarmCream"]
    ok = sum(1 for l in ls if max(ratio(ink, l), ratio(cream, l)) >= 3) / len(ls)
    print("%-30s %6.3f" % (bn, m), *["%22s" % c for c in cells], "%22s" % ("%3d%% >=3" % (ok * 100)))
