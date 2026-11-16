#!/usr/bin/env python3
"""Smooth pickups batch 2 (QA7-01): Gem, Magnet, Shield, Double, SlowMo, Ghost, Star_White. Own code, procedural, no
generative AI. Usage: python3 tools/art/make_pickups2.py <output dir>"""
import math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from smoothart import *
from make_pickups_smooth import T, GOLD, star_pts, coin
OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
def new(): return Sprite(128, 128, 128 / 0.75)
def hx(n): return hexrgb(PAL[n])
CREAM = T("#FFFFFF", "#E7E0CF", "#9AA3AD")

def star(p):
    sp = new(); c = 64; ro = 64 - 4 - sp.outline_px - 1
    m = sp.shape(lambda mm: mm.polygon(star_pts(c, c + 3, ro, ro * .48, ro * .18)))
    cov = sp.cel(m, T("#FFFFFF", "#F2F2F2", "#C9CED4"), direction=(c-40, c-40, c+30, c+45), soft=.22, split=(.35, .8), rim=3)
    sp.finish(p)

def gem(p):
    sp = new(); c = 64
    pts = [(34, 40), (94, 40), (116, 62), (64, 116), (12, 62)]
    m = sp.shape(lambda mm: mm.polygon(round_poly([(x, y, 3) for x, y in pts], 4)) if False else mm.polygon(pts))
    cyan = T(PAL["CarCyanLight"], PAL["CarCyan"], PAL["CarCyanDeep"])
    cov = sp.cel(m, cyan, direction=(14, 40, 100, 116), soft=.2, split=(.3, .7), rim=3)
    # facets
    facets = [([(34,40),(50,62),(12,62)], PAL["CarCyanLight"], .55), ([(94,40),(116,62),(78,62)], PAL["CarCyanDeep"], .4),
              ([(50,62),(78,62),(64,116)], PAL["CarCyanDeep"], .45), ([(34,40),(94,40),(78,62),(50,62)], "#FFFFFF", .35)]
    for poly, col, o in facets:
        f = sp.mask(); f.polygon(poly); sp.paint(f, hexrgb(col), o, clip=cov)
    sp.line([(12,62),(116,62)], 1.4, PAL["CarCyanDeep"], clip=cov, opacity=.8)
    sp.line([(50,62),(64,116),(78,62)], 1.4, PAL["CarCyanDeep"], clip=cov, opacity=.8)
    sp.line([(34,40),(50,62)], 1.2, PAL["CarCyanDeep"], clip=cov, opacity=.7)
    sp.line([(94,40),(78,62)], 1.2, PAL["CarCyanDeep"], clip=cov, opacity=.7)
    sp.gloss(cov, 42, 50, 7, 4, .8, clip=cov)
    sp.finish(p)

def magnet(p):
    sp = new(); c = 64
    # U shape: thick arc with two legs, cream tips
    def sil(m):
        m.circle(64, 62, 46)
        erase_circle(m, 64, 62, 22)
        m.polygon([(18, 62), (42, 62), (42, 112), (18, 112)]) if False else None
        m.rrect(18, 62, 42, 108, 3); m.rrect(86, 62, 110, 108, 3)
        m.polygon([(42, 40), (86, 40), (86, 62), (42, 62)]) if False else None
        erase_poly(m, [(40, 62), (88, 62), (88, 120), (40, 120)]) if False else None
    m = sp.shape(sil)
    # cut the inner gap of legs and the lower half of the circle
    body = sp.mask(); body.circle(64, 62, 46); erase_circle(body, 64, 62, 22)
    lower = sp.mask(); lower.polygon([(0, 62), (128, 62), (128, 128), (0, 128)])
    top = sp.mask(); top.polygon([(0, 0), (128, 0), (128, 62), (0, 62)])
    sp2 = sp.shape(lambda mm: (mm.circle(64, 66, 44), erase_circle(mm, 64, 66, 21), erase_poly(mm, [(0, 66), (128, 66), (128, 128), (0, 128)]), mm.rrect(20, 66, 43, 106, 2), mm.rrect(85, 66, 108, 106, 2)))
    red = T(PAL["CarRedLight"], PAL["CarRed"], PAL["CarRedDeep"]) 
    org = T(PAL["CarOrangeLight"], PAL["CarOrange"], PAL["CarOrangeDeep"])
    cov = sp.cel(sp2, T("#7FB8FF", "#3E7FE0", "#1F4A9A"), direction=(20, 20, 108, 90), soft=.2, split=(.3, .7), rim=3)
    for x0, x1 in ((20, 43), (85, 108)):
        t = sp.mask(); t.rrect(x0, 88, x1, 106, 2)
        sp.paint(t, hexrgb("#E7E0CF"), 1, clip=cov)
        sp.cel(t, CREAM, direction=(x0, 88, x1, 106), soft=.3, clip=cov)
        sp.line([(x0, 88), (x1, 88)], 1.3, PAL["DeepInk"], clip=cov, opacity=.8)
    sp.gloss(cov, 40, 36, 14, 4, .6, clip=cov)
    sp.finish(p)

def shield(p):
    sp = new(); c = 64
    def pts():
        r = []
        r += [(20, 18), (108, 18), (112, 58)]
        for i in range(1, 12):
            t = i / 12; r.append((112 - 48 * t * (1 - 0) * 1.0 + 0, 58 + 62 * (t ** 1.6)))
        return r
    pts = [(20,20),(64,12),(108,20),(110,62),(100,88),(64,118),(28,88),(18,62)]
    m = sp.shape(lambda mm: mm.polygon(round_poly([(x,y,8) for x,y in pts], 6)))
    teal = T("#8CF0D8", "#2FBFA2", "#17806C")
    cov = sp.cel(m, teal, direction=(20, 14, 100, 110), soft=.2, split=(.3, .75), rim=3.5)
    inner = sp.mask(); inner.polygon(round_poly([(x*0.74+64*0.26, y*0.74+64*0.26+1, 6) for x,y in pts], 6))
    sp.paint(inner.dilated(2).minus(inner), hexrgb("#17806C"), .9, clip=cov)
    sp.cel(inner, T("#E8FFF8", "#B8F4E4", "#79D9C0"), direction=(30, 20, 96, 100), soft=.3, clip=cov)
    sp.line([(46, 62), (60, 78), (86, 44)], 6.5, PAL["DeepInk"], opacity=.35) if False else None
    sp.line([(47, 63), (60, 77), (84, 45)], 6, "#17806C", clip=cov)
    sp.gloss(cov, 34, 30, 7, 4, .7, clip=cov)
    sp.finish(p)

def double(p):
    sp = Sprite(128, 128, 128 / 0.75); 
    def disc(cx, cy, R, z):
        d = sp.shape(lambda m: m.circle(cx, cy, R))
        sp.cel(d, GOLD, direction=(cx-R*.8, cy-R*.8, cx+R*.9, cy+R*.9), soft=.2, split=(.4, .82), rim=3)
        g = sp.mask(); g.circle(cx, cy, R*.76); g2 = sp.mask(); g2.circle(cx, cy, R*.76-3)
        sp.paint(g.cov().minus(g2.cov()), hexrgb(PAL["GoldDeep"]))
    disc(46, 46, 36, 0); 
    sp.outline() if False else None
    disc(84, 84, 36, 1)
    # x2 on the front coin: draw "2" with polyline
    two = [(70,72),(74,66),(84,66),(92,72),(90,82),(70,100),(96,100)]
    sp.line(two, 4.2, PAL["DeepInk"])
    sp.finish(p)

def slowmo(p):
    sp = new(); c = 64; R = 64 - 4 - sp.outline_px
    d = sp.shape(lambda m: m.circle(c, c, R))
    ice = T(PAL["IceLight"], PAL["IceMid"], PAL["IceDeep"])
    cov = sp.cel(d, ice, direction=(c-R*.8, c-R*.8, c+R*.9, c+R*.9), soft=.2, split=(.35, .8), rim=3.5)
    f = sp.mask(); f.circle(c, c, R*.74)
    sp.paint(f.dilated(2.2).minus(f), hexrgb(PAL["IceDeep"]), clip=cov)
    sp.cel(f, T("#FFFFFF", "#F4FAFF", "#CFE6F8"), direction=(c-40, c-40, c+40, c+40), soft=.3, clip=cov)
    for a in range(0, 360, 90):
        x, y = c + R*.6*math.cos(math.radians(a)), c + R*.6*math.sin(math.radians(a))
        k = sp.mask(); k.circle(x, y, 3.4); sp.paint(k, hexrgb(PAL["IceDeep"]), clip=cov)
    sp.line([(c, c), (c, c-R*.52)], 3.6, PAL["DeepInk"], clip=cov)
    sp.line([(c, c), (c+R*.36, c+R*.2)], 3.6, PAL["DeepInk"], clip=cov)
    k = sp.mask(); k.circle(c, c, 6); sp.paint(k, hexrgb(PAL["DeepInk"]), clip=cov)
    sp.gloss(cov, 38, 30, 12, 5, .6, clip=cov)
    sp.finish(p)

def ghost(p):
    sp = new()
    def sil(m):
        m.circle(64, 54, 40)
        m.rrect(24, 54, 104, 100, 2)
        # wavy hem: scallops
        for cx in (34, 54, 74, 94):
            m.circle(cx, 100, 10)
        for cx in (44, 64, 84):
            erase_circle(m, cx, 112, 9)
    m = sp.shape(sil)
    cov = sp.cel(m, T("#FFFFFF", "#DCE8F8", "#9FB4D8"), direction=(24, 14, 100, 112), soft=.25, split=(.35, .78), rim=3.5)
    for x in (50, 78):
        e = sp.mask(); e.ellipse(x, 54, 6.5, 9); sp.paint(e, hexrgb(PAL["DeepInk"]), clip=cov)
        h = sp.mask(); h.circle(x - 2, 50, 2.2); sp.paint(h, hexrgb("#FFFFFF"), clip=cov)
    mo = sp.mask(); mo.ellipse(64, 76, 6, 4.5); sp.paint(mo, hexrgb(PAL["DeepInk"]), clip=cov)
    sp.gloss(cov, 42, 30, 10, 5, .7, clip=cov)
    sp.finish(p)

for n, f in (("Star_White", star), ("Pickup_Gem", gem), ("Pickup_Magnet", magnet), ("Pickup_Shield", shield),
             ("Pickup_Double", double), ("Pickup_SlowMo", slowmo), ("Pickup_Ghost", ghost)):
    f(os.path.join(OUT, n + ".png")); print(n)
