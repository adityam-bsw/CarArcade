#!/usr/bin/env python3
"""Smooth pickups for the slice: coin, heart, star, ink canister, boost ring (+ overload ring). Own code, procedural, no
generative AI, no downloads. Python 3.9 standard library only.

    python3 tools/art/make_pickups_smooth.py --output <dir> [--only coin,heart,star,ink,ring]

Canvases are SQUARE on purpose: Collectible.Init scales every pickup so that its sprite bounds fit a fixed world size
(coin 0.65, heart 0.75, ink 0.85, ring 1.6, star particle by its own code), so the canvas size sets nothing in gameplay, only
sharpness. The outline is 0.04 world units measured at that DISPLAY size (6 to 8 px on the 128 px sprites).
Colour code: gold = reward (coin, star, ring), purple = ink, pink-red = health. Red warns only, so no pickup uses SignalRed.
"""
import argparse, math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from smoothart import *

def T(*hexes):
    return tuple(hexrgb(h) for h in hexes)

GOLD = T(PAL["GoldLight"], PAL["GoldMid"], PAL["GoldDeep"])
GOLD_HI = T("#FFF3B0", PAL["GoldLight"], PAL["GoldMid"])

def star_pts(cx, cy, ro, ri, d, n=5, rot=-math.pi / 2):
    pts = []
    for i in range(2 * n):
        a = rot + math.pi * i / n; r = ro if i % 2 == 0 else ri
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a), d if i % 2 == 0 else d * 0.5))
    return round_poly(pts, 6)

def coin(path):
    S = 128; sp = Sprite(S, S, S / 0.65 )
    c = S / 2; R = S / 2 - 4 - sp.outline_px
    disc = sp.shape(lambda m: m.circle(c, c, R))
    sp.cel(disc, GOLD, direction=(c - R * 0.8, c - R * 0.8, c + R * 0.9, c + R * 0.9), soft=0.2, split=(0.4, 0.82), rim=3.5)
    groove = sp.mask(); groove.circle(c, c, R * 0.80); g2 = sp.mask(); g2.circle(c, c, R * 0.80 - 3.2)
    sp.paint(groove.cov().minus(g2.cov()), hexrgb(PAL["GoldDeep"]))
    inner = sp.mask(); inner.circle(c, c, R * 0.80 - 3.2)
    sp.cel(inner, T(PAL["GoldDeep"], PAL["GoldMid"], PAL["GoldLight"]), direction=(c - R * 0.7, c - R * 0.7, c + R * 0.7, c + R * 0.7), soft=0.25, split=(0.25, 0.55))
    st = sp.mask(); st.polygon([(x + 2.5, y + 3) for x, y in star_pts(c, c + 1.5, R * 0.46, R * 0.2, R * 0.08)])
    sp.paint(st, hexrgb(PAL["GoldDeep"]), 0.8)
    st = sp.mask(); st.polygon(star_pts(c, c + 1.5, R * 0.46, R * 0.2, R * 0.08))
    sp.cel(st, T("#FFFDE8", "#FFF3B0", "#F6C945"), direction=(c - 20, c - 20, c + 20, c + 20), soft=0.3)
    sp.finish(path)

def heart_pts(cx, cy, k, n=120):
    pts = []
    for i in range(n):
        t = 2 * math.pi * i / n
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((cx + k * x, cy - k * y))
    return pts

def heart(path):
    S = 128; sp = Sprite(S, S, S / 0.75)
    k = (S - 8 - 2 * sp.outline_px) / 33.0
    cx, cy = S / 2, S / 2 + 1.0
    # heart_pts spans y -17..12 (in curve units): centre of that is -2.5
    ys = [y for _, y in heart_pts(0, 0, k)]; cy = S / 2 - (min(ys) + max(ys)) / 2     # centre the real extent
    m = sp.shape(lambda mm: mm.polygon(heart_pts(cx, cy, k)))
    cov = sp.cel(m, T(PAL["HeartLight"], PAL["HeartRed"], PAL["HeartDeep"]), direction=(cx - 40, cy - 40, cx + 30, cy + 55), soft=0.2, split=(0.3, 0.72), rim=3.5)
    sp.gloss(cov, cx - 22, cy - 22, 15, 9, 0.7, color="#FFE8EE", clip=cov)
    g = sp.mask(); g.circle(cx - 36, cy - 6, 4.0); sp.paint(g, hexrgb("#FFE8EE"), 0.8)
    sp.finish(path)

def star(path):
    S = 128; sp = Sprite(S, S, 200.0)
    c = S / 2; ro = S / 2 - 4 - sp.outline_px - 1
    pts = star_pts(c, c + 3, ro, ro * 0.48, ro * 0.18)
    m = sp.shape(lambda mm: mm.polygon(pts))
    cov = sp.cel(m, GOLD_HI, direction=(c - 40, c - 40, c + 30, c + 45), soft=0.22, split=(0.35, 0.75), rim=3.5)
    sp.gloss(cov, c - 10, c - 10, 10, 6, 0.6, color="#FFFFFF", clip=cov)
    sp.finish(path)

def ink(path):
    S = 128; sp = Sprite(S, S, S / 0.85)
    P = PAL; pur = T(P["InkPurpleLight"], P["InkPurple"], P["InkPurpleDeep"])
    body = sp.shape(lambda m: m.rrect(34, 46, 94, 112, 15))
    neck = sp.shape(lambda m: m.rrect(51, 30, 77, 52, 5))
    capm = sp.shape(lambda m: m.rrect(47, 13, 81, 33, 6))
    bc = sp.cel(body, pur, direction=(34, 46, 80, 112), soft=0.2, split=(0.35, 0.8), rim=3.5)
    nc = sp.cel(neck, pur, direction=(51, 30, 77, 52), soft=0.2)
    sh = sp.mask(); sh.polygon([(40, 52), (51, 46), (77, 46), (88, 52), (88, 60), (40, 60)])    # shoulder wedge fills the neck join
    sp.paint(sh.cov().minus(sp.mask().cov()), hexrgb(P["InkPurple"]), 0.0)
    cc = sp.cel(capm, T("#FFF6E0", "#E7E0CF", "#9AA3AD"), direction=(47, 13, 81, 33), soft=0.25, rim=2.5)
    sp.line([(52, 24), (76, 24)], 1.4, "#9AA3AD", clip=cc)
    lab = sp.mask(); lab.rrect(44, 62, 84, 104, 8)
    sp.paint(lab.dilated(2.5).minus(lab), hexrgb(P["InkPurpleDeep"]))
    sp.cel(lab, T("#E9DAFF", "#C7A8F5", "#9C74DB"), direction=(44, 62, 84, 104), soft=0.3)
    drop = sp.mask(); drop.circle(64, 86, 10.5)
    drop.polygon([(64, 64), (73.5, 83), (54.5, 83)])
    sp.paint(drop.dilated(2).minus(drop), hexrgb(P["InkPurpleDeep"]))
    sp.cel(drop, T("#FFFFFF", "#E7E0CF", "#C9B8E8"), direction=(54, 66, 74, 98), soft=0.3)
    sp.gloss(bc, 46, 70, 4, 16, 0.5, clip=bc)
    sp.finish(path)

def ring(path, overload=False):
    S = 256; sp = Sprite(S, S, S / 1.6)
    c = S / 2; Ro = S / 2 - 4 - sp.outline_px; Ri = Ro - 30
    def sil(m):
        m.circle(c, c, Ro); erase_circle(m, c, c, Ri)
    m = sp.shape(sil)
    cov = sp.cel(m, GOLD, direction=(c - Ro * 0.9, c - Ro * 0.9, c + Ro * 0.8, c + Ro * 0.8), soft=0.18, split=(0.4, 0.8), rim=0)
    # torus ridge: cream highlight band along the middle, fading toward the lower right
    mid = (Ro + Ri) / 2
    band = sp.mask(); band.circle(c, c, mid + 4.5); erase_circle(band, c, c, mid - 4.5)
    bf = band.cov()
    def ridge(x, y):
        t = clamp(((x - c) * -0.7 + (y - c) * -0.7) / Ro * 0.5 + 0.5)
        return lerp(hexrgb(PAL["GoldMid"]), hexrgb("#FFF8D0"), t)
    sp.img.paint(bf, ridge, 0.85)
    if overload:
        out = sp.mask(); out.circle(c, c, Ro); erase_circle(out, c, c, Ro - 7)
        sp.paint(out, hexrgb(PAL["OverloadCyan"]))
        inr = sp.mask(); inr.circle(c, c, Ri + 7); erase_circle(inr, c, c, Ri)
        sp.paint(inr, hexrgb(PAL["OverloadCyan"]))
    # soft glint arc, upper left
    arc = [(c + (mid) * math.cos(math.radians(a)), c + (mid) * math.sin(math.radians(a))) for a in range(205, 252, 6)]
    sp.line(arc, 3.2, "#FFFFFF", clip=cov, opacity=0.85)
    sp.finish(path)

if __name__ == "__main__":
    ap = argparse.ArgumentParser(); ap.add_argument("--output", required=True); ap.add_argument("--only", default="coin,heart,star,ink,ring")
    a = ap.parse_args(); os.makedirs(a.output, exist_ok=True); only = a.only.split(",")
    o = lambda n: os.path.join(a.output, n)
    if "coin" in only: coin(o("Coin.png")); print("coin")
    if "heart" in only: heart(o("Heart.png")); print("heart")
    if "star" in only: star(o("Star.png")); print("star")
    if "ink" in only: ink(o("InkCanister.png")); print("ink")
    if "ring" in only: ring(o("Pickup_Ring.png")); ring(o("Pickup_Ring_Overload.png"), True); print("ring")
