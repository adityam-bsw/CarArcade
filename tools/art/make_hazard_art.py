"""Hazard art (own work, procedural, pure Python stdlib, no generative AI): 15 world projectiles, shared telegraph shapes, RO-B0 parts, 10 Road-Eater bodies, trap strip, finish gate, vignette.
Smooth style: flat fills, soft supersampled edges, a darker rim and one light highlight. Usage: python3 make_hazard_art.py <out_dir>"""
import math, os, struct, sys, zlib

def png(path, w, h, px):
    raw = b"".join(b"\x00" + b"".join(bytes(px[y * w + x]) for x in range(w)) for y in range(h))
    ch = lambda t, d: struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d))
    open(path, "wb").write(b"\x89PNG\r\n\x1a\n" + ch(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)) + ch(b"IDAT", zlib.compress(raw)) + ch(b"IEND", b""))

def hexc(h): h = h.lstrip("#"); return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))
def shade(c, k): return tuple(max(0, min(255, int(v * k))) for v in c)
def light(c, k): return tuple(max(0, min(255, int(v + (255 - v) * k))) for v in c)

class Canvas:
    def __init__(s, w, h, ss=3): s.w, s.h, s.ss = w, h, ss; s.buf = [[0.0, 0.0, 0.0, 0.0] for _ in range(w * h)]
    def paint(s, inside, col, alpha=1.0):
        """inside(x, y) in pixel units -> bool; coverage by supersampling; source-over onto the buffer."""
        ss = s.ss; n = ss * ss
        for y in range(s.h):
            for x in range(s.w):
                c = 0
                for j in range(ss):
                    for i in range(ss):
                        if inside(x + (i + .5) / ss, y + (j + .5) / ss): c += 1
                if not c: continue
                a = alpha * c / n; p = s.buf[y * s.w + x]; oa = p[3]; na = a + oa * (1 - a)
                if na <= 0: continue
                for k in range(3): p[k] = (col[k] * a + p[k] * oa * (1 - a)) / na
                p[3] = na
    def save(s, path):
        px = [(int(p[0]), int(p[1]), int(p[2]), int(p[3] * 255)) for p in s.buf]
        png(path, s.w, s.h, px)

def ell(cx, cy, rx, ry, rot=0.0):
    c, sn = math.cos(rot), math.sin(rot)
    def f(x, y):
        dx, dy = x - cx, y - cy; u, v = dx * c + dy * sn, -dx * sn + dy * c
        return (u / rx) ** 2 + (v / ry) ** 2 <= 1
    return f
def poly(pts):
    def f(x, y):
        ins = False; n = len(pts); j = n - 1
        for i in range(n):
            xi, yi = pts[i]; xj, yj = pts[j]
            if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi + 1e-9) + xi: ins = not ins
            j = i
        return ins
    return f
def rbox(cx, cy, hw, hh, r):
    def f(x, y):
        dx, dy = abs(x - cx) - (hw - r), abs(y - cy) - (hh - r)
        return math.hypot(max(dx, 0), max(dy, 0)) + min(max(dx, dy), 0) <= r
    return f
def ring(cx, cy, r0, r1):
    return lambda x, y: r0 <= math.hypot(x - cx, y - cy) <= r1
def seg(x0, y0, x1, y1, w):
    def f(x, y):
        dx, dy = x1 - x0, y1 - y0; t = max(0, min(1, ((x - x0) * dx + (y - y0) * dy) / (dx * dx + dy * dy + 1e-9)))
        return math.hypot(x - (x0 + t * dx), y - (y0 + t * dy)) <= w / 2
    return f

def body(cv, shape, base, shapeIn=None, hl=(0.3, -0.3)):
    """rim + fill + highlight for an ellipse-like body described by shape(scale)."""
    cv.paint(shape(1.0), shade(base, 0.55)); cv.paint(shape(0.88), base); cv.paint(shape(0.62), light(base, 0.18))

S = 96
def sprite(name, fn, out, size=S):
    cv = Canvas(size, size); fn(cv, size); cv.save(os.path.join(out, name + ".png"))

def circ(base, extra=None):
    def fn(cv, n):
        c = n / 2
        body(cv, lambda k: ell(c, c, n * .42 * k, n * .42 * k), base)
        cv.paint(ell(c - n * .12, c - n * .14, n * .09, n * .06, -.6), (255, 255, 255), .75)
        if extra: extra(cv, n, base)
    return fn

def spike(base):
    def fn(cv, n):
        c = n / 2; pts = [(c, n * .06), (n * .8, c), (c, n * .94), (n * .2, c)]
        cv.paint(poly(pts), shade(base, .55)); cv.paint(poly([(c, n * .14), (n * .72, c), (c, n * .86), (n * .28, c)]), base)
        cv.paint(poly([(c, n * .14), (n * .72, c), (c, c)]), light(base, .3))
    return fn
def shard(base):
    def fn(cv, n):
        c = n / 2; pts = [(c, n * .05), (n * .72, n * .7), (c, n * .95), (n * .28, n * .7)]
        cv.paint(poly(pts), shade(base, .55)); cv.paint(poly([(c, n * .13), (n * .64, n * .68), (c, n * .87), (n * .36, n * .68)]), base); cv.paint(poly([(c, n * .13), (c, n * .87), (n * .36, n * .68)]), light(base, .35))
    return fn
def bolt(base):
    def fn(cv, n):
        pts = [(n * .55, n * .05), (n * .25, n * .52), (n * .46, n * .52), (n * .35, n * .95), (n * .78, n * .4), (n * .55, n * .4), (n * .7, n * .05)]
        cv.paint(poly(pts), shade(base, .5)); cv.paint(poly([(a * .96 + n * .02, b) for a, b in pts]), base)
    return fn
def drone(base):
    def fn(cv, n):
        c = n / 2; cv.paint(rbox(c, c, n * .4, n * .22, n * .12), shade(base, .55)); cv.paint(rbox(c, c, n * .35, n * .17, n * .09), base)
        cv.paint(ell(c, c, n * .1, n * .1), (255, 60, 50)); cv.paint(ell(c - 2, c - 2, n * .035, n * .035), (255, 255, 255), .9)
        cv.paint(seg(n * .12, n * .26, n * .3, n * .3, 4), (60, 70, 85)); cv.paint(seg(n * .88, n * .26, n * .7, n * .3, 4), (60, 70, 85))
    return fn
def bubble(base):
    def fn(cv, n):
        c = n / 2; cv.paint(ell(c, c, n * .42, n * .42), light(base, .1), .55); cv.paint(ring(c, c, n * .38, n * .43), shade(base, .6)); cv.paint(ell(c - n * .14, c - n * .15, n * .1, n * .06, -.7), (255, 255, 255), .9)
    return fn
def gumdrop(base):
    def fn(cv, n):
        c = n / 2; cv.paint(ell(c, n * .62, n * .4, n * .3), shade(base, .55)); cv.paint(ell(c, n * .6, n * .36, n * .27), base); cv.paint(ell(c, n * .5, n * .2, n * .12), light(base, .45), .8)
        cv.paint(poly([(n * .1, n * .5), (n * .2, n * .38), (n * .2, n * .62)]), (255, 230, 120)); cv.paint(poly([(n * .9, n * .5), (n * .8, n * .38), (n * .8, n * .62)]), (255, 230, 120))
    return fn
def thorn(base):
    def fn(cv, n):
        c = n / 2
        for i in range(8):
            a = i * math.pi / 4; cv.paint(poly([(c + math.cos(a - .22) * n * .3, c + math.sin(a - .22) * n * .3), (c + math.cos(a) * n * .48, c + math.sin(a) * n * .48), (c + math.cos(a + .22) * n * .3, c + math.sin(a + .22) * n * .3)]), (120, 80, 40))
        body(cv, lambda k: ell(c, c, n * .3 * k, n * .3 * k), base)
    return fn
def pearl(base):
    def fn(cv, n):
        c = n / 2; body(cv, lambda k: ell(c, c, n * .4 * k, n * .4 * k), base); cv.paint(poly([(n * .56, n * .12), (n * .4, n * .5), (n * .52, n * .5), (n * .44, n * .86), (n * .66, n * .44), (n * .54, n * .44)]), (255, 240, 120), .85)
    return fn
def glob(base):
    def fn(cv, n):
        c = n / 2; body(cv, lambda k: ell(c, n * .56, n * .4 * k, n * .34 * k), base); cv.paint(ell(c - n * .1, n * .3, n * .1, n * .12), base); cv.paint(ell(c + n * .12, n * .3, n * .07, n * .09), base)
        cv.paint(ell(c - n * .12, n * .5, n * .06, n * .04, -.5), (255, 255, 255), .7)
    return fn
def boulder(base):
    def fn(cv, n):
        c = n / 2; pts = [(c + math.cos(i * .785) * n * (.42 if i % 2 else .38), c + math.sin(i * .785) * n * (.42 if i % 2 else .38)) for i in range(8)]
        cv.paint(poly(pts), shade(base, .55)); cv.paint(poly([(c + (x - c) * .86, c + (y - c) * .86) for x, y in pts]), base); cv.paint(ell(c - n * .1, c - n * .12, n * .14, n * .09, -.6), light(base, .35), .8); cv.paint(seg(n * .5, n * .5, n * .66, n * .66, 4), shade(base, .6))
    return fn
def fire(base):
    def fn(cv, n):
        c = n / 2; body(cv, lambda k: ell(c, n * .58, n * .36 * k, n * .36 * k), base); cv.paint(poly([(n * .3, n * .4), (n * .42, n * .06), (n * .5, n * .3), (n * .6, n * .08), (n * .7, n * .42)]), (255, 210, 60), .9); cv.paint(ell(c, n * .6, n * .16, n * .14), (255, 235, 150), .9)
    return fn
def meteor(base):
    def fn(cv, n):
        c = n / 2; cv.paint(poly([(n * .15, n * .1), (n * .6, n * .5), (n * .5, n * .62)]), (255, 190, 80), .8); cv.paint(poly([(n * .06, n * .3), (n * .55, n * .55), (n * .42, n * .68)]), (255, 120, 60), .7); body(cv, lambda k: ell(n * .6, n * .6, n * .26 * k, n * .26 * k), base)
    return fn
def orb(base, dark=False):
    def fn(cv, n):
        c = n / 2; body(cv, lambda k: ell(c, c, n * .38 * k, n * .38 * k), base)
        cv.paint(ring(c, c, n * .43, n * .47), light(base, .4), .6)
        if dark: cv.paint(ell(c - n * .1, c - n * .04, n * .06, n * .09), (240, 240, 255)); cv.paint(ell(c + n * .1, c - n * .04, n * .06, n * .09), (240, 240, 255))
    return fn

PRJ = {
    "prj_boulder": boulder(hexc("9e8c73")), "prj_sandspike": spike(hexc("e0b866")), "prj_iceshard": shard(hexc("9fdcff")), "prj_gravityorb": orb(hexc("a897ff")),
    "prj_meteorcore": meteor(hexc("ff6a40")), "prj_lavabomb": fire(hexc("e8591a")), "prj_bubble": bubble(hexc("9be6ff")), "prj_gumdrop": gumdrop(hexc("ff69b4")),
    "prj_crystal": shard(hexc("7ff2e0")), "prj_neonbolt": bolt(hexc("ff40d8")), "prj_shadoworb": orb(hexc("40305a"), True), "prj_thornball": thorn(hexc("5a9a33")),
    "prj_drone": drone(hexc("dbe0f0")), "prj_stormpearl": pearl(hexc("f0f0ff")), "prj_acidglob": glob(hexc("8cf23f")),
}

def tell_ring(cv, n): c = n / 2; cv.paint(ring(c, c, n * .38, n * .48), (255, 255, 255), .95); cv.paint(ring(c, c, n * .2, n * .24), (255, 255, 255), .6)
def tell_marker(cv, n): cv.paint(ell(n / 2, n / 2, n * .48, n * .22), (255, 255, 255), .85); cv.paint(ell(n / 2, n / 2, n * .3, n * .12), (255, 255, 255), .6)
def tell_glow(cv, n):
    c = n / 2
    for r, a in ((.48, .2), (.36, .3), (.24, .5), (.12, .8)): cv.paint(ell(c, c, n * r, n * r), (255, 255, 255), a)
def trap_strip(cv, n):
    cv.paint(rbox(n / 2, n * .7, n * .48, n * .12, 6), (90, 60, 40))
    for i in range(6): x = n * (.1 + i * .16); cv.paint(poly([(x, n * .62), (x + n * .07, n * .1), (x + n * .14, n * .62)]), (235, 235, 240)); cv.paint(poly([(x + n * .03, n * .62), (x + n * .07, n * .3), (x + n * .11, n * .62)]), (255, 255, 255), .6)
def finish_gate(cv, n):
    cv.paint(rbox(n * .1, n * .55, n * .05, n * .42, 4), (235, 235, 240)); cv.paint(rbox(n * .9, n * .55, n * .05, n * .42, 4), (235, 235, 240)); cv.paint(rbox(n / 2, n * .16, n * .5, n * .1, 6), (40, 40, 52))
    for i in range(10): cv.paint(rbox(n * (.1 + i * .08), n * .16, n * .035, n * .06, 1), (255, 255, 255) if i % 2 else (20, 20, 28))
def vignette(cv, n):
    c = n / 2
    for r, a in ((1.0, .0), (.9, .08), (.8, .12), (.7, .16), (.6, .2)): cv.paint(lambda x, y, r=r: math.hypot(x - c, y - c) >= n * r * .5, (255, 255, 255), a)

def robo_body(cv, n):
    c = n / 2; cv.paint(ell(c, c + n * .05, n * .46, n * .34), shade(hexc("8a96a8"), .55)); cv.paint(ell(c, c + n * .05, n * .42, n * .3), hexc("8d99ad")); cv.paint(ell(c, c - n * .04, n * .3, n * .15), light(hexc("8d99ad"), .3), .8)
    cv.paint(rbox(c, c + n * .2, n * .3, n * .06, 6), hexc("5c6577"))
    for dx in (-.28, -.14, 0, .14, .28): cv.paint(ell(c + n * dx, c + n * .2, n * .018, n * .018), (255, 220, 90))
def robo_trim(cv, n): c = n / 2; cv.paint(ring(c, c + n * .05, n * .43, n * .5), (255, 255, 255), .95); cv.paint(ell(c, c + n * .05, n * .5, n * .36), (255, 255, 255), .0)
def robo_eye(cv, n): c = n / 2; cv.paint(ell(c, c, n * .45, n * .45), shade((240, 240, 250), .6)); cv.paint(ell(c, c, n * .4, n * .4), (250, 250, 255)); cv.paint(ell(c - n * .08, c, n * .2, n * .2), (30, 35, 60)); cv.paint(ell(c - n * .13, c - n * .06, n * .06, n * .06), (255, 255, 255))
def robo_ant(cv, n): c = n / 2; cv.paint(rbox(c, n * .6, n * .08, n * .36, 3), (200, 205, 215)); cv.paint(ell(c, n * .18, n * .2, n * .14), (255, 90, 80)); cv.paint(ell(c - 2, n * .14, n * .05, n * .04), (255, 255, 255), .8)
def robo_jet(cv, n): c = n / 2; cv.paint(poly([(n * .3, n * .1), (n * .7, n * .1), (c, n * .95)]), (255, 190, 70), .9); cv.paint(poly([(n * .4, n * .1), (n * .6, n * .1), (c, n * .6)]), (255, 250, 200), .9)

EATERS = {"hills": "7d5634", "desert": "d9b366", "moon": "8b80bf", "mars": "bf5a40", "volcano": "f2661a", "underwater": "265999", "candy": "ff73b3", "cave": "808088", "synthwave": "e633e6", "haunted": "403366"}
def eater(world, col):
    base = hexc(col)
    def fn(cv, n):
        c = n / 2; body(cv, lambda k: ell(c, c, n * .46 * k, n * .42 * k), base)
        cv.paint(ell(c + n * .1, c + n * .02, n * .26, n * .28), (40, 12, 30)); cv.paint(ell(c + n * .12, c + n * .06, n * .2, n * .2), (120, 30, 60))
        for i in range(5):
            y = c - n * .22 + i * n * .11; cv.paint(poly([(c + n * .3, y), (c + n * .05, y + n * .03), (c + n * .3, y + n * .08)]), (250, 245, 230))
        cv.paint(ell(c - n * .12, c - n * .2, n * .1, n * .1), (255, 255, 255)); cv.paint(ell(c - n * .09, c - n * .19, n * .045, n * .045), (20, 20, 30))
        cv.paint(ell(c - n * .1, c - n * .3, n * .14, n * .05, -.5), light(base, .4), .7)
    return fn

def main():
    out = sys.argv[1]; os.makedirs(out, exist_ok=True)
    for k, f in PRJ.items(): sprite(k, f, out)
    for k, f in (("tell_ring", tell_ring), ("tell_marker", tell_marker), ("tell_glow", tell_glow)): sprite(k, f, out)
    sprite("trap_strip", trap_strip, out); sprite("finish_gate", finish_gate, out, 128); sprite("vignette", vignette, out, 128)
    sprite("robo_body", robo_body, out, 192); sprite("robo_trim", robo_trim, out, 192); sprite("robo_eye", robo_eye, out, 64); sprite("robo_antenna", robo_ant, out, 64); sprite("robo_jet", robo_jet, out, 64)
    for w, col in EATERS.items(): sprite("eater_" + w, eater(w, col), out, 192)
    print("wrote", len(os.listdir(out)), "files to", out)
main()
