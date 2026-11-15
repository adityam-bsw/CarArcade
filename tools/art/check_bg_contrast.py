"""Luminance contrast of tells/hazards against the Countryside composite (Bible 6: >= 3:1). Usage: check_bg_contrast.py <dir>
Reads bg_countryside_preview_2048x576.png plus the individual layers; WCAG relative luminance, ratio = (L1+.05)/(L2+.05)."""
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "assets")); import pixel
D = sys.argv[1]
def lin(c):
    c /= 255; return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
def L(r, g, b): return 0.2126 * lin(r) + 0.7152 * lin(g) + 0.0722 * lin(b)
def hx(h): return L(int(h[1:3], 16), int(h[3:5], 16), int(h[5:7], 16))
def ratio(a, b): hi, lo = max(a, b), min(a, b); return (hi + .05) / (lo + .05)
tells = {"SignalRed #D94B40": hx("#D94B40"), "DeepInk #18202A": hx("#18202A"), "WarmCream #E7E0CF": hx("#E7E0CF"), "IceMid #8CC8F0": hx("#8CC8F0")}
w, h, rows = pixel.read_png(os.path.join(D, "bg_countryside_preview_2048x576.png"))
bands = {"sky (y 40-250)": (40, 250), "horizon/far+mid (y 250-430)": (250, 430), "near hill (y 480-576)": (480, 576)}
print("%-30s %6s" % ("band", "meanL"), *["%24s" % k for k in tells])
for bn, (a, b) in bands.items():
    ls = [L(*rows[y][x][:3]) for y in range(a, b, 2) for x in range(0, w, 2)]
    m = sum(ls) / len(ls)
    cells = []
    for tn, tl in tells.items():
        ok = sum(1 for l in ls if ratio(tl, l) >= 3) / len(ls)
        cells.append("%5.2f:1 (%3d%% >=3)" % (ratio(tl, m), ok * 100))
    print("%-30s %6.3f" % (bn, m), *["%24s" % c for c in cells])
# per layer mean colour luminance (atmospheric perspective check: far lightest, near darkest)
for n, hh in (("far", 576), ("mid", 576), ("near", 864)):
    w2, h2, r2 = pixel.read_png(os.path.join(D, "bg_countryside_%s_2048x%d.png" % (n, hh)))
    ls = [L(*r2[y][x][:3]) for y in range(0, h2, 3) for x in range(0, w2, 3) if r2[y][x][3] > 200]
    print("layer %-5s opaque mean L=%.3f" % (n, sum(ls) / len(ls)))
