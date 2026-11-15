"""Sprite helpers for gameplay objects (vehicles, wheels, pickups). Standard library only, no generative AI.

Rules implemented (Docs/design/ART_BIBLE.md 2.3, 2.4): DeepInk outline outside the silhouette only, sized from the sprite's
DISPLAY density (0.04 world units), 3-tone soft cel shading from the upper left, gentle upper-left rim light, gloss, 4 px bleed.

    sp = Sprite(480, 180, display_ppu=200)          # canvas px, and how many canvas px one world unit covers when shown
    sp.fill(shape_fn, colors=("#F2826F", "#E2453A", "#8E2A2A"))   # shape_fn(mask) draws into a Mask; also joins the silhouette
    sp.finish("out.png")                              # silhouette outline, rim light handled per fill, write_png with bleed
"""
import math
from .palette import PAL, hexrgb, lerp, smoothstep, clamp
from .raster import Image, Cov, Mask, write_png
from .shading import cel3, vgrad

INK = hexrgb(PAL["DeepInk"])

def mix(a, b, t):
    a = hexrgb(a) if isinstance(a, str) else a; b = hexrgb(b) if isinstance(b, str) else b
    return lerp(a, b, t)

def tones(base, light_k=0.38, deep_k=0.42, deep_toward="#18202A", light_toward="#FFF6E0"):
    """(light, mid, deep) from one base colour: light mixes toward warm cream, deep toward ink (slightly), keeps hue."""
    b = hexrgb(base) if isinstance(base, str) else base
    return (lerp(b, hexrgb(light_toward), light_k), b, lerp(b, hexrgb(deep_toward), deep_k))

def round_poly(pts, seg=8):
    """Rounded polygon. pts are (x, y, d): vertex and fillet tangent length d (0 = sharp). Quadratic-Bezier fillets."""
    n = len(pts); out = []
    for i in range(n):
        px, py, _ = pts[i - 1]; x, y, d = pts[i]; nx, ny, _ = pts[(i + 1) % n]
        if d <= 0:
            out.append((x, y)); continue
        e0 = math.hypot(px - x, py - y); e1 = math.hypot(nx - x, ny - y)
        d0 = min(d, e0 * 0.5); d1 = min(d, e1 * 0.5)
        a = (x + (px - x) / e0 * d0, y + (py - y) / e0 * d0); b = (x + (nx - x) / e1 * d1, y + (ny - y) / e1 * d1)
        for k in range(seg + 1):
            t = k / seg; u = 1 - t
            out.append((u * u * a[0] + 2 * u * t * x + t * t * b[0], u * u * a[1] + 2 * u * t * y + t * t * b[1]))
    return out

def erase_poly(mask, pts):
    mask.polygon(pts, 0); return mask

def erase_circle(mask, cx, cy, r, n=96):
    mask.polygon([(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n)) for i in range(n)], 0)
    return mask

def circle_pts(cx, cy, r, n=96):
    return [(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n)) for i in range(n)]

def shift_cov(cov, dx, dy):
    """Integer-pixel shift of a Cov (used for the upper-left rim light)."""
    w, h = cov.w, cov.h; out = [0.0] * (w * h); d = cov.d
    for y in range(h):
        yy = y + dy
        if not (0 <= yy < h): continue
        row = d[y * w:(y + 1) * w]
        if not any(row): continue
        if dx >= 0: out[yy * w + dx:yy * w + w] = row[:w - dx]
        else: out[yy * w:yy * w + w + dx] = row[-dx:]
    bb = None
    if cov.bb: bb = (max(0, cov.bb[0] + min(0, dx)), max(0, cov.bb[1] + min(0, dy)), min(w, cov.bb[2] + max(0, dx)), min(h, cov.bb[3] + max(0, dy)))
    return Cov(w, h, out, bb)

def bbox(cov):
    return cov.bb

class Sprite:
    def __init__(self, w, h, display_ppu, outline_world=0.04):
        self.w, self.h = w, h
        self.img = Image(w, h)
        self.sil = Mask(w, h)
        self.outline_px = outline_world * display_ppu
        self.ppu = display_ppu
        self.pending_outline = True

    def mask(self):
        return Mask(self.w, self.h)

    def shape(self, fn, in_silhouette=True):
        """Build a Mask from fn(mask). Joins the silhouette (outline source) unless in_silhouette is False."""
        m = self.mask(); fn(m)
        if in_silhouette:
            fn(self.sil)
        return m

    def paint(self, mask_or_cov, color, opacity=1.0, clip=None):
        cov = mask_or_cov.cov() if isinstance(mask_or_cov, Mask) else mask_or_cov
        if clip is not None:
            cov = cov.multiplied(clip if isinstance(clip, Cov) else clip.cov())
        self.img.paint(cov, color, opacity)
        return cov

    def cel(self, mask, base_or_tones, direction=None, soft=0.18, rim=0.0, split=(0.33, 0.66), clip=None):
        """Paint a mask with 3-tone cel shading. direction=(x0,y0,x1,y1) in canvas px, default the mask's bbox diagonal."""
        cov = mask.cov() if isinstance(mask, Mask) else mask
        if clip is not None:
            cov = cov.multiplied(clip if isinstance(clip, Cov) else clip.cov())
        t = tones(base_or_tones) if not isinstance(base_or_tones, tuple) or not isinstance(base_or_tones[0], (tuple, str)) else base_or_tones
        t = tuple(hexrgb(c) if isinstance(c, str) else c for c in t)
        x0, y0, x1, y1 = cov.bb if cov.bb else (0, 0, self.w, self.h)
        if direction is None:
            # light from the upper left: a 3 tone ramp along the diagonal, biased so most of the part stays mid
            direction = (x0 + (x1 - x0) * 0.10, y0 + (y1 - y0) * 0.0, x0 + (x1 - x0) * 0.55, y1 + 0.0)
        f = cel3(t[0], t[1], t[2], *direction, soft=soft, split=split)
        self.img.paint(cov, f)
        if rim > 0:
            self.rim(cov, t[0], rim)
        return cov

    def rim(self, cov, color, px=3.0, opacity=0.85):
        """Gentle highlight along the upper-left edge inside the shape."""
        k = max(1, int(round(px)))
        sh = shift_cov(cov, k, k)
        r = cov.minus(sh)
        self.img.paint(r, hexrgb(color) if isinstance(color, str) else color, opacity)

    def gloss(self, cov, cx, cy, rx, ry, opacity=0.35, color="#FFFFFF", clip=None):
        m = Mask(self.w, self.h); m.ellipse(cx, cy, rx, ry)
        g = m.cov().blur(max(1, int(min(rx, ry) * 0.25)), 2)
        if clip is not None:
            g = g.multiplied(clip if isinstance(clip, Cov) else clip.cov())
        self.img.paint(g, hexrgb(color), opacity)

    def line(self, pts, r, color, clip=None, opacity=1.0):
        m = self.mask(); m.polyline(pts, r)
        cov = m.cov()
        if clip is not None:
            cov = cov.multiplied(clip if isinstance(clip, Cov) else clip.cov())
        self.img.paint(cov, hexrgb(color) if isinstance(color, str) else color, opacity)
        return cov

    def outline(self):
        """DeepInk outline outside the whole silhouette (call once, after all fills; it paints UNDER them via order trick)."""
        out = self.sil.dilated(self.outline_px).minus(self.sil)
        return out.cov()

    def finish(self, path):
        """Paint the outline into the transparent area around the silhouette and write the PNG (with 4 px bleed)."""
        self.img.paint(self.outline(), INK)
        write_png(self.img, path)
        return self.img
