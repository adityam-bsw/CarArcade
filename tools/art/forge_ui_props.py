import sys, math, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from smoothart import *
from smoothart.sprites import Sprite, tones
from smoothart.raster import Mask, Image, write_png
from smoothart.palette import hexrgb

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "ui_props")
C = dict(steel="#59636D", steell="#8A939B", steeld="#3B434D", orange="#F4A340", red="#D94B40", teal="#6B9B87",
         cream="#E7E0CF", paper="#D9C9A5", ink="#18202A", glow="#F9C77D", dark="#0A0E14")

class M:
    """Mask proxy: draw in 1x unit coords, rasterised at K x."""
    def __init__(s, m, k): s.m, s.k = m, k
    def polygon(s, pts): s.m.polygon([(x*s.k, y*s.k) for x, y in pts]); return s
    def ellipse(s, cx, cy, rx, ry): s.m.ellipse(cx*s.k, cy*s.k, rx*s.k, ry*s.k); return s
    def circle(s, cx, cy, r): return s.ellipse(cx, cy, r, r)
    def rrect(s, x0, y0, x1, y1, r): s.m.rrect(x0*s.k, y0*s.k, x1*s.k, y1*s.k, r*s.k); return s
    def capsule(s, x0, y0, x1, y1, r): s.m.capsule(x0*s.k, y0*s.k, x1*s.k, y1*s.k, r*s.k); return s

class P:
    def __init__(s, w, h, k, outline=True):
        s.k = k; s.sp = Sprite(w*k, h*k, display_ppu=1, outline_world=1.0*k if outline else 0.0); s.outline = outline
    def part(s, fn, base, soft=0.2, rim=1.0, silhouette=True):
        m = s.sp.shape(lambda m: fn(M(m, s.k)), in_silhouette=silhouette)
        s.sp.cel(m, tones(C.get(base, base)), soft=soft, rim=rim*s.k*0.8)
        return m
    def flat(s, fn, col, op=1.0, clip=None, sil=False):
        m = s.sp.shape(lambda m: fn(M(m, s.k)), in_silhouette=sil)
        s.sp.paint(m, hexrgb(C.get(col, col)), op, clip=clip); return m
    def save(s, name):
        if s.outline: s.sp.finish(os.path.join(OUT, name))
        else: write_png(s.sp.img, os.path.join(OUT, name))

def rod(a, b, w):  # rectangle along a segment as polygon fn
    ax, ay = a; bx, by = b; L = math.hypot(bx-ax, by-ay); nx, ny = -(by-ay)/L*w/2, (bx-ax)/L*w/2
    return lambda m: m.polygon([(ax+nx, ay+ny), (bx+nx, by+ny), (bx-nx, by-ny), (ax-nx, ay-ny)])

def gloss(p, cx, cy, rx, ry, op=.45): p.sp.gloss(p.sp.img and Mask(1,1).cov() if False else None, 0,0,1,1) if False else None

def hl(p, fn, op=0.5):  # soft white highlight streak
    p.flat(fn, "#FFFFFF", op)

def lamp(k):
    p = P(27, 26, k)
    p.part(lambda m: m.rrect(12.2, 0, 14.8, 13, 0.6), "steel", rim=0.5)   # cord (no outline sideways problem: thin)
    p.part(lambda m: m.polygon([(9, 13), (18, 13), (25.5, 24), (1.5, 24)]), "steeld" if False else "#6B9B87")
    p.part(lambda m: m.rrect(9.5, 11.5, 17.5, 14, 1), "steel")            # socket cap
    p.flat(lambda m: m.rrect(4, 23.3, 23, 24.6, 0.6), "glow")             # glowing rim
    p.flat(lambda m: m.polygon([(11, 15), (13, 15), (8.5, 21), (6, 21)]), "#FFFFFF", .28)
    p.save("UI_Prop_lamp.png")

def glow(k):
    w, h = 100, 76; img = Image(w*k, h*k); cx, cy = w*k/2, 6*k
    def f(x, y):
        return hexrgb(C["glow"])
    from smoothart.raster import Cov
    d = []
    for y in range(h*k):
        for x in range(w*k):
            dx = (x+.5-cx)/(w*k/2); dy = (y+.5-cy)/(h*k-cy)
            r = math.hypot(dx, dy*0.95); t = max(0, 1-r); d.append(t*t*0.85 if t > 0 else 0)
    img.paint(Cov(w*k, h*k, d, (0,0,w*k,h*k)), hexrgb(C["glow"]))
    write_png(img, os.path.join(OUT, "UI_Prop_lamp_glow.png"))

def clock_face(k):
    p = P(21, 21, k)
    p.part(lambda m: m.circle(10.5, 10.5, 9.5), "cream", rim=1.0)
    p.flat(lambda m: (m.circle(10.5, 10.5, 9.5), None)[0], "#000000", 0)
    ring = p.sp.shape(lambda m: M(m, k).circle(10.5, 10.5, 8.6), in_silhouette=False)
    for i in range(12):
        a = i*math.pi/6; r0, r1 = (7.0, 8.2) if i % 3 else (6.3, 8.2)
        p.flat(lambda m, a=a, r0=r0, r1=r1: m.capsule(10.5+r0*math.sin(a), 10.5-r0*math.cos(a), 10.5+r1*math.sin(a), 10.5-r1*math.cos(a), .45), "ink")
    p.flat(lambda m: m.circle(10.5, 10.5, 1.0), "red")
    p.save("UI_Prop_clock_face.png")

def hand(name, w, h, k):
    p = P(w, h, k, outline=False)
    p.flat(lambda m: m.rrect(0, 0, w, h, w/2), "ink"); p.save(name)

def can(col, k):
    p = P(14, 20, k)
    p.part(lambda m: m.rrect(2, 2, 12, 19, 1.6), col)
    p.flat(lambda m: m.rrect(2, 6.5, 12, 11.5, 0.3), "cream", 0.9)
    p.flat(lambda m: m.ellipse(7, 2.4, 5, 1.3), "steell")
    hl(p, lambda m: m.rrect(3.2, 3.5, 4.6, 17.5, .6), .35)
    p.save(f"UI_Prop_can_{col}.png" if col in ("red",) else f"UI_Prop_can_{col}.png")

def drum(k):
    p = P(31, 39, k)
    p.part(lambda m: m.rrect(1.5, 2, 29.5, 38, 3), "steel")
    for y in (12, 27): p.flat(lambda m, y=y: m.rrect(1.5, y, 29.5, y+2, .6), "steeld")
    p.flat(lambda m: m.ellipse(15.5, 3.5, 13.5, 2.5), "steell"); p.flat(lambda m: m.ellipse(15.5, 3.6, 10, 1.4), "steeld", .6)
    p.flat(lambda m: m.rrect(5, 15, 8, 25, 1.4), "#FFFFFF", .3)
    p.save("UI_Prop_drum.png")

def gear(k):
    p = P(24, 24, k); cx = cy = 12; n = 8; pts = []
    for i in range(n*4):
        a = 2*math.pi*i/(n*4); ph = i % 4
        r = 10.6 if ph in (1, 2) else 8.4; pts.append((cx+r*math.cos(a), cy+r*math.sin(a)))
    p.part(lambda m: m.polygon(pts), "orange", rim=1.0)
    p.flat(lambda m: m.circle(12, 12, 5), "orange", 0)
    p.flat(lambda m: m.circle(12, 12, 4.2), "steeld"); p.flat(lambda m: m.circle(12, 12, 3.2), "ink")
    p.save("UI_Prop_gear.png")

def hammer(k):
    p = P(27, 39, k)
    p.part(rod((13.5, 10), (13.5, 37.5), 5), "paper", rim=0.8)
    p.part(lambda m: m.rrect(1.5, 1.5, 25.5, 11.5, 2.2), "steell")
    p.part(lambda m: m.rrect(1.5, 1.5, 8.5, 11.5, 2), "steel", rim=0.5)
    hl(p, lambda m: m.rrect(10, 3, 23, 4.8, .8), .5)
    p.save("UI_Prop_hammer.png")

def screwdriver(k):
    p = P(40, 14, k)
    p.part(lambda m: m.rrect(1.5, 2.5, 17, 11.5, 3.5), "red")
    for x in (6, 9.5, 13): p.flat(lambda m, x=x: m.capsule(x, 4, x, 10, .5), "#8A2A24", .7)
    p.part(rod((17, 7), (36, 7), 2.6), "steell", rim=.5)
    p.part(lambda m: m.polygon([(35, 5.4), (38.5, 6), (38.5, 8), (35, 8.6)]), "steel", rim=0)
    p.save("UI_Prop_screwdriver.png")

def saw(k):
    p = P(38, 26, k); pts = [(1.5, 3), (26, 3), (26, 14)]
    teeth = [(26, 14)] + [(26-i*2.2*1.0, 14+(2.4 if i % 2 == 0 else 0)) for i in range(1, 11)]
    teeth = [(26 - i*2.3, 14 + (3 if i % 2 else 0)) for i in range(0, 11)]
    p.part(lambda m: m.polygon([(1.5, 3)] + [(26, 3)] + teeth[:] + [(1.5, 14)]), "steell", rim=.6)
    p.part(lambda m: m.rrect(25, 2, 36.5, 24, 3), "orange")
    p.flat(lambda m: m.rrect(28, 6, 33.5, 15, 1.8), "ink", .8)
    p.save("UI_Prop_saw.png")

def toolbox(k):
    p = P(35, 25, k)
    p.part(lambda m: m.rrect(11, 1.5, 24, 8, 2.5), "steel")
    p.flat(lambda m: m.rrect(13.5, 3.8, 21.5, 8, 1.5), "#000000", 0)
    p.part(lambda m: m.rrect(1.5, 6.5, 33.5, 23.5, 2.5), "red")
    p.flat(lambda m: m.rrect(1.5, 12, 33.5, 13.6, .4), "#8A2A24", .8)
    p.flat(lambda m: m.rrect(14.5, 10.5, 20.5, 15.5, 1.2), "steell")
    hl(p, lambda m: m.rrect(3.5, 8, 30, 9.3, .6), .4)
    p.save("UI_Prop_toolbox.png")

def tyre(k):
    p = P(45, 23, k)
    p.part(lambda m: m.ellipse(22.5, 11.5, 21, 10), "#2A313C", rim=1.0)
    p.flat(lambda m: m.ellipse(22.5, 11.5, 12, 5.6), "steel")
    p.flat(lambda m: m.ellipse(22.5, 11.5, 7.5, 3.4), "steeld")
    p.flat(lambda m: m.ellipse(22.5, 11.5, 2.6, 1.2), "steell")
    p.save("UI_Prop_tyre.png")

def shelf(k):
    p = P(85, 18, k)
    p.part(lambda m: m.rrect(1.5, 1.5, 83.5, 7.5, 1.5), "steell")
    p.flat(lambda m: m.rrect(1.5, 6, 83.5, 7.5, .7), "steel")
    p.part(lambda m: m.polygon([(6, 8), (12, 8), (6, 16.5)]), "steeld", rim=0)
    p.part(lambda m: m.polygon([(79, 8), (73, 8), (79, 16.5)]), "steeld", rim=0)
    p.save("UI_Prop_shelf.png")

def wrench(k):
    p = P(41, 18, k)
    p.part(rod((10, 9), (31, 9), 4.5), "steell", rim=.6)
    p.part(lambda m: m.circle(8.5, 9, 6.2), "steell")
    p.part(lambda m: m.circle(32.5, 8, 5.2), "steell")
    p.flat(lambda m: m.polygon([(0, 6.5), (7, 9), (0, 11.5)]), "#000000", 0)
    p.sp.img.paint(p.sp.mask().polygon([(0,0)]).cov(), (0,0,0), 0)
    p.flat(lambda m: m.circle(33.5, 8, 1.8), "ink", .9)
    p.save("UI_Prop_wrench.png")

def pegboard(k):
    p = P(79, 93, k)
    p.part(lambda m: m.rrect(1, 1, 78, 92, 2.5), "steeld", soft=.1, rim=1.0)
    for y in range(7, 90, 6):
        for x in range(6, 76, 6):
            p.flat(lambda m, x=x, y=y: m.circle(x, y, .8), "ink", .75)
    p.flat(lambda m: m.rrect(1.5, 1.5, 77.5, 3.2, .8), "steell", .45)
    p.save("UI_Prop_pegboard.png")

def robot_body(k):
    p = P(48, 64, k)
    # legs/feet
    for x0 in (13, 28):
        p.part(lambda m, x0=x0: m.rrect(x0, 51, x0+8, 59.5, 1.2), "#4A7262", rim=.5)
        p.part(lambda m, x0=x0: m.rrect(x0-2, 58, x0+11, 62.5, 2), "#35574A", rim=.4)
    # torso
    p.part(lambda m: m.rrect(6, 30, 42, 52, 4), "teal", soft=.28)
    p.flat(lambda m: m.rrect(9, 33, 39, 49, 2.5), "#3B434D", .9)
    for i, col in enumerate(("red", "orange", "cream")):
        p.flat(lambda m, i=i: m.circle(15+i*6.5, 41, 2.0), col)
    p.flat(lambda m: m.rrect(31, 38, 37, 44, 1.5), "ink", .7)
    p.flat(lambda m: m.rrect(24, 28.5, 33, 31.5, 1), "steeld")   # neck
    # head
    p.part(lambda m: m.rrect(9, 3, 39, 28, 6), "teal", soft=.28)
    p.flat(lambda m: m.rrect(12, 9, 36, 22, 4), "dark")                       # visor, eyes layered separately
    hl(p, lambda m: m.rrect(13, 4.6, 30, 6.4, .9), .5)
    p.save("UI_Prop_robot_body.png")

def robot_eyes(k):
    p = P(19, 11, k, outline=False)
    for cx in (4.5, 14.5):
        p.flat(lambda m, cx=cx: m.rrect(cx-3.4, 1.5, cx+3.4, 9.5, 3.2), "orange")
        p.flat(lambda m, cx=cx: m.rrect(cx-1.8, 3, cx+0.6, 5, 1), "#FFE3B0", .9)
    p.save("UI_Prop_robot_eyes.png")

def robot_arm(k):
    p = P(13, 26, k)
    p.part(lambda m: m.rrect(3, 1.5, 10, 17, 3.2), "teal", rim=.6)
    p.part(lambda m: m.circle(6.5, 20, 4.6), "steell", rim=.6)
    p.flat(lambda m: m.circle(6.5, 20, 1.4), "ink", .8)
    p.save("UI_Prop_robot_arm.png")

def robot_antenna(k):
    p = P(10, 13, k)
    p.part(rod((5, 12), (5, 5), 1.8), "steel", rim=0)
    p.part(lambda m: m.circle(5, 4.2, 3), "red", rim=.8)
    hl(p, lambda m: m.circle(4.2, 3.3, .8), .8)
    p.save("UI_Prop_robot_antenna.png")

ALL = [lamp, clock_face, drum, gear, hammer, screwdriver, saw, toolbox, tyre, shelf, wrench, pegboard,
       robot_body, robot_eyes, robot_arm, robot_antenna, glow]
def build(k, suffix):
    global OUT
    for f in ALL: f(k)
    for c in ("red", "orange", "teal"): can(c, k)
    hand("UI_Prop_clock_hand_hour.png", 1, 6, k); hand("UI_Prop_clock_hand_minute.png", 1, 9, k)
if __name__ == "__main__":
    k = int(sys.argv[1]); OUT = sys.argv[2]; os.makedirs(OUT, exist_ok=True); build(k, "")
