"""Helpers for the catalogue-vehicle smooth restyle (make_vehicles2.py). Own code, standard library only, no generative AI.

A catalogue vehicle keeps its OLD canvas (W x H old pixels), content box (1 px margin) and wheel anchors, scaled by an integer
factor S (new canvas = old * S, so every anchor is old * S exactly). Designs are authored in the old canvas' pixel units
(here: OLD pixel coordinates, floats allowed; x from 1 to W-1 and y from 1 to H-1 span the content box) so the silhouette always fills the old bounds, outline included.
"""
import json, math, os
from smoothart import *
from vehicle_layout import wheels_for

HERE = os.path.dirname(os.path.abspath(__file__))
INV = json.load(open(os.path.join(HERE, "vehicles2_inventory.json")))

def T(*hexes):
    return tuple(hexrgb(h) for h in hexes)

def three(base, light_k=0.40, deep_k=0.40):
    return tones(base, light_k, deep_k)

class Ctx:
    """One sprite under construction. k scales every thickness (authored at 200 px per unit)."""
    def __init__(self, vid, with_wheels=True, wheels_only=False):
        s = INV[vid]; self.vid, self.spec = vid, s
        self.W, self.H, self.S = s["W"], s["H"], s["S"]
        self.sp = Sprite(self.W * self.S, self.H * self.S, display_ppu=self.W * self.S / s["units"])
        self.k = self.sp.ppu / 200.0
        self.o = self.sp.outline_px
        self.x0, self.x1 = self.S + self.o, (self.W - 1) * self.S - self.o
        self.y0, self.y1 = self.S + self.o, (self.H - 1) * self.S - self.o
        self.with_wheels = with_wheels
        self.wheels = [(w["cx"] * self.S, w["cy"] * self.S, w["r"] * self.S - 0.5, w) for w in wheels_for(vid, INV)]
    # ---- coordinates
    def X(self, x): return self.x0 + (x - 1.0) / (self.W - 2.0) * (self.x1 - self.x0)
    def Y(self, y): return self.y0 + (y - 1.0) / (self.H - 2.0) * (self.y1 - self.y0)
    def pt(self, p):
        return (self.X(p[0]), self.Y(p[1])) + ((p[2] * self.k,) if len(p) > 2 else ())
    def poly(self, pts, seg=9):
        """Rounded polygon from (fx, fy, fillet) points; fillet in 200-ppu pixels (0 = sharp)."""
        q = [self.pt(p if len(p) > 2 else (p[0], p[1], 0)) for p in pts]
        return round_poly(q, seg)
    # ---- parts
    def body(self, pts, colors, rim=3.0, direction=None, split=(0.45, 0.86), soft=0.16, seg=9):
        P = self.poly(pts, seg)
        m = self.sp.shape(lambda mm: mm.polygon(P))
        if direction is None:
            xa, ya, xb, yb = m.cov().bb
            direction = (xa + (xb - xa) * 0.25, ya, xa + (xb - xa) * 0.55, yb)
        cov = self.sp.cel(m, colors, direction=direction, soft=soft, split=split, rim=rim * self.k)
        return cov, m
    def part(self, pts, colors, rim=0.0, direction=None, in_sil=True, seg=8, soft=0.2, split=(0.4, 0.8), ink=0.0):
        """A coloured polygon part. ink > 0 draws a DeepInk line of that thickness (200-ppu px) around it first (inner part)."""
        P = self.poly(pts, seg)
        m = self.sp.shape(lambda mm: mm.polygon(P), in_silhouette=in_sil)
        if ink > 0:
            self.sp.paint(m.dilated(ink * self.k).minus(m), INK)
        if direction is None:
            xa, ya, xb, yb = m.cov().bb
            direction = (xa, ya, xa + (xb - xa) * 0.6, yb)
        cov = self.sp.cel(m, colors, direction=direction, soft=soft, split=split, rim=rim * self.k)
        return cov, m
    def rect(self, fx0, fy0, fx1, fy1, colors, rad=6, rim=0.0, ink=0.0, in_sil=True, direction=None):
        return self.part([(fx0, fy0, rad), (fx1, fy0, rad), (fx1, fy1, rad), (fx0, fy1, rad)], colors, rim=rim, ink=ink, in_sil=in_sil, direction=direction)
    def pane(self, pts, frame, clip=None, streak=None, glass=("#E4F6FF", PAL["GlassSky"], PAL["GlassSkyDeep"])):
        m = self.sp.mask(); m.polygon(self.poly(pts, 6))
        fr = m.dilated(3.2 * self.k).minus(m)
        self.sp.paint(fr, hexrgb(frame))
        cov = m.cov(); y0, y1 = cov.bb[1], cov.bb[3]
        self.sp.img.paint(cov, vgrad(y0, y1, Ramp([(0, glass[0]), (0.45, glass[1]), (1, glass[2])])))
        if streak:
            self.sp.line([self.pt(p) for p in streak], 2.6 * self.k, "#FFFFFF", clip=cov, opacity=0.55)
        return cov
    def line(self, pts, w, color, clip=None, opacity=1.0):
        return self.sp.line([self.pt(p) for p in pts], w * self.k / 2.0, color, clip=clip, opacity=opacity)
    def lamp(self, fx0, fy0, fx1, fy1, cols, rad=4):
        m = self.sp.mask(); m.rrect(self.X(fx0), self.Y(fy0), self.X(fx1), self.Y(fy1), rad * self.k)
        self.sp.paint(m.dilated(2.2 * self.k).minus(m), INK)
        self.sp.cel(m, T(*cols), direction=(self.X(fx0), self.Y(fy0), self.X(fx1), self.Y(fy1)))
        return m
    def disc(self, fx, fy, r, colors, ink=2.2, rim=0.0):
        m = self.sp.mask(); cx, cy = self.X(fx), self.Y(fy); rr = r          # r is in canvas px
        m.circle(cx, cy, rr)
        if ink > 0:
            self.sp.paint(m.dilated(ink * self.k).minus(m), INK)
        self.sp.cel(m, colors, direction=(cx - rr, cy - rr, cx + rr, cy + rr), rim=rim * self.k)
        return m
    def tube(self, pts, w, colors, in_sil=True, ink=0.0):
        """Round tube (polyline) w old px wide; steel cel; part of the silhouette unless in_sil is False."""
        P = [(self.X(x), self.Y(y)) for x, y in pts]
        r = w * self.S / 2.0
        m = self.sp.mask(); m.polyline(P, r)
        if in_sil: self.sp.sil.polyline(P, r)
        if ink > 0: self.sp.paint(m.dilated(ink * self.k).minus(m), INK)
        xa, ya, xb, yb = m.cov().bb
        self.sp.cel(m, colors, direction=(xa, ya, xa + (xb - xa) * 0.4 + 1, yb), soft=0.2, split=(0.35, 0.8), rim=max(1.2, r * 0.3))
        return m
    def gloss(self, cov, fx, fy, rx, ry, op=0.35):
        self.sp.gloss(cov, self.X(fx), self.Y(fy), rx * self.k, ry * self.k, op, clip=cov)
    def arches(self, env, deep, extra=3.0):
        """Wheel arch liners, centred on the wheel anchors, clipped to the body envelope."""
        e = env if isinstance(env, Cov) else env.cov()
        for (cx, cy, R, wd) in self.wheels:
            if wd.get("noarch"): continue
            out = self.sp.mask(); out.circle(cx, cy, R + extra * self.k + 2.0 * self.k)
            inn = self.sp.mask(); inn.circle(cx, cy, R + extra * self.k)
            self.sp.paint(out.cov().minus(inn.cov()), hexrgb(deep), clip=e)
            self.sp.paint(inn, hexrgb("#1E242C"), clip=e)
    def wheel_at(self, x, y, r, kind="car", accent="#D8323C", **kw):
        """A baked (non-spinning) wheel in old px coordinates; part of the silhouette."""
        cx, cy, R = self.X(x) , self.Y(y), r * self.S - 0.5
        self.sp.sil.circle(cx, cy, R - self.o)
        draw_wheel(self.sp, cx, cy, R, self.k, kind=kind, accent=accent, **kw)
    def draw_wheels(self, kind="car", accent="#D8323C", **kw):
        for (cx, cy, R, w) in self.wheels:
            self.sp.sil.circle(cx, cy, R - self.o)          # outline wraps body + wheel, and is never painted over a wheel
            draw_wheel(self.sp, cx, cy, R, self.k, kind=kind, accent=accent, **kw)
    def save(self, path):
        self.sp.finish(path)
        return self.sp

# ------------------------------------------------------------------ wheels
def draw_wheel(sp, cx, cy, R, k, kind="car", accent="#D8323C", rim_cols=None, spin_marker=True):
    """Wheel disc of outer radius R (DeepInk ring included). kind: car, sport, truck, bike, lug, cart, tractor.
    Always shows rotation: spokes plus one off-centre bright bolt and tread notches."""
    P = PAL; S = sp
    if kind == "wire":
        return draw_wire_wheel(sp, cx, cy, R, k, accent)
    o = max(2.0, 0.04 * sp.ppu)
    ring = S.mask(); ring.circle(cx, cy, R)
    S.paint(ring, INK)
    Rt = R - o                                  # tyre outer radius
    tyre = S.mask(); tyre.circle(cx, cy, Rt)
    thin = False
    S.cel(tyre, T(P["TyreLight"], P["TyreMid"], P["TyreDeep"]), direction=(cx - Rt * 0.7, cy - Rt * 0.7, cx + Rt * 0.9, cy + Rt * 0.9), soft=0.25)
    S.rim(tyre.cov(), P["TyreLight"], max(1.5, 3 * k))
    if kind in ("lug", "tractor"):
        n = 14 if kind == "lug" else 12
        for i in range(n):
            a = 2 * math.pi * (i + 0.5) / n
            S.line([(cx + (Rt * 0.78) * math.cos(a), cy + (Rt * 0.78) * math.sin(a)), (cx + (Rt - 1.0) * math.cos(a), cy + (Rt - 1.0) * math.sin(a))],
                   max(1.5, Rt * (0.075 if kind == "lug" else 0.09)), P["TyreDeep"])
    elif not thin:
        n = 20
        for i in range(n):
            a = 2 * math.pi * (i + 0.5) / n
            S.line([(cx + (Rt * 0.86) * math.cos(a), cy + (Rt * 0.86) * math.sin(a)), (cx + (Rt - 1.0) * math.cos(a), cy + (Rt - 1.0) * math.sin(a))],
                   max(1.2, Rt * 0.05), P["TyreDeep"])
    rr = Rt * {"car": 0.62, "sport": 0.68, "truck": 0.60, "bike": 0.86, "lug": 0.52, "cart": 0.55, "tractor": 0.50}.get(kind, 0.62)
    # sidewall highlight ring
    sw1 = S.mask(); sw1.circle(cx, cy, rr + max(2.0, Rt * 0.10)); sw2 = S.mask(); sw2.circle(cx, cy, rr + max(1.0, Rt * 0.04))
    S.paint(sw1.cov().minus(sw2.cov()), hexrgb(P["TyreLight"]), 0.5)
    rim = S.mask(); rim.circle(cx, cy, rr)
    if rim_cols is None:
        rim_cols = T("#E8F7FF", "#B7C4D0", "#6F7E8C") if kind == "sport" else T("#C9D2DA", "#8A939B", "#59636D")
    if kind == "tractor":
        rim_cols = T("#F6C945", "#E9A92B", "#B88A1E")
    S.cel(rim, rim_cols, direction=(cx - rr, cy - rr, cx + rr, cy + rr), rim=max(1.5, 2.4 * k))
    dr = rr * 0.76
    dish = S.mask(); dish.circle(cx, cy, dr)
    S.cel(dish, T("#59636D", "#3B434D", "#252B36"), direction=(cx - dr, cy - dr, cx + dr, cy + dr), soft=0.3)
    ns = {"sport": 6, "bike": 8, "cart": 4}.get(kind, 5)
    sw = max(2.0, rr * (0.12 if kind == "sport" else 0.17))
    for i in range(ns):
        a = 2 * math.pi * i / ns + 0.15
        pts = [(cx + rr * 0.18 * math.cos(a), cy + rr * 0.18 * math.sin(a)), (cx + rr * 0.86 * math.cos(a), cy + rr * 0.86 * math.sin(a))]
        S.line(pts, sw * 0.5 + 0.8, "#252B36"); S.line(pts, sw * 0.5, "#E7EDF2")
    hub = S.mask(); hub.circle(cx, cy, rr * 0.28)
    S.cel(hub, tones(accent), rim=1.5)
    if spin_marker:
        mk = S.mask(); mk.circle(cx + rr * 0.70, cy, max(1.8, rr * 0.11)); S.paint(mk, hexrgb("#FFF6E0"))

def draw_wire_wheel(sp, cx, cy, R, k, accent):
    """Bicycle / motorbike wheel: dark tyre ring, pale open centre with thin spokes, hub, one bright valve bolt on the tyre."""
    P = PAL; o = max(2.0, 0.04 * sp.ppu)
    ring = sp.mask(); ring.circle(cx, cy, R); sp.paint(ring, INK)
    Rt = R - o; t = max(3.0, Rt * 0.20); Ri = Rt - t
    out = sp.mask(); out.circle(cx, cy, Rt); inn = sp.mask(); inn.circle(cx, cy, Ri)
    cov_out, cov_in = out.cov(), inn.cov()
    sp.img.paint(cov_in, cl_fn(cx, cy, Ri))
    tyre = cov_out.minus(cov_in)
    f = cel3(hexrgb(P["TyreLight"]), hexrgb(P["TyreMid"]), hexrgb(P["TyreDeep"]), cx - Rt * 0.7, cy - Rt * 0.7, cx + Rt * 0.9, cy + Rt * 0.9, soft=0.25)
    sp.img.paint(tyre, f)
    sp.paint(cov_in.minus(sp.mask().cov()), hexrgb("#8A939B"), 0.0)
    n = 12
    for i in range(n):
        a = 2 * math.pi * i / n + 0.12
        sp.line([(cx + Ri * 0.16 * math.cos(a), cy + Ri * 0.16 * math.sin(a)), (cx + Ri * 0.97 * math.cos(a), cy + Ri * 0.97 * math.sin(a))],
                max(0.9, Ri * 0.022), "#59636D", opacity=0.95)
    for i in range(0, n, 3):
        a = 2 * math.pi * i / n + 0.12
        sp.line([(cx + Ri * 0.16 * math.cos(a), cy + Ri * 0.16 * math.sin(a)), (cx + Ri * 0.97 * math.cos(a), cy + Ri * 0.97 * math.sin(a))],
                max(1.4, Ri * 0.04), accent, opacity=0.95)
    hub = sp.mask(); hub.circle(cx, cy, max(3.0, Ri * 0.13)); sp.cel(hub, T("#E7EDF2", "#9AA3AD", "#59636D"), rim=1.0)
    mk = sp.mask(); mk.circle(cx + Rt - t * 0.5, cy, max(1.8, t * 0.28)); sp.paint(mk, hexrgb("#FFF6E0"))

def cl_fn(cx, cy, r):
    return lambda x, y: lerp(hexrgb("#F0F5F9"), hexrgb("#BCC8D3"), min(1.0, math.hypot(x - cx, y - cy) / r))

def wheel_sprite(vid, idx, path, kind="car", accent="#D8323C", **kw):
    """Stand-alone wheel PNG, size*S square, disc filling it (same as the old cut-out, so RoadCar scales it identically)."""
    s = INV[vid]; w = wheels_for(vid, INV)[idx]; n = int(w["size"] * s["S"])
    sp = Sprite(n, n, display_ppu=s["W"] * s["S"] / s["units"])
    draw_wheel(sp, n / 2.0, n / 2.0, w["r"] * s["S"] - 0.5, sp.ppu / 200.0, kind=kind, accent=accent, **kw)
    write_png(sp.img, path)
    return sp
