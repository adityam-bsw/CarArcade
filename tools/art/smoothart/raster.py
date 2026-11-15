"""Raster core: float RGBA image, supersampled shape masks, outline, blur, colour bleed, PNG writer. Standard library only.

Pipeline: shapes are rasterised into a 1-bit Mask at SS x resolution, box-downsampled to a Cov (coverage 0..1 per output
pixel), then painted with a colour function. Colour is evaluated at output resolution (gradients are smooth anyway), so
only EDGES pay the supersampling cost.
"""
import math, re, struct, zlib

def _hex(h):
    h = h.lstrip('#')
    return (float(int(h[0:2], 16)), float(int(h[2:4], 16)), float(int(h[4:6], 16)))

class Cov:
    """Coverage map 0..1, flat list of w*h floats."""
    def __init__(self, w, h, data=None, bb=None):
        self.w, self.h = w, h
        self.d = data if data is not None else [0.0] * (w * h)
        self.bb = bb        # (x0, y0, x1, y1) pixel bounds (x1, y1 exclusive) outside which coverage is 0; None = unknown
    def scaled(self, k):
        return Cov(self.w, self.h, [v * k for v in self.d], self.bb)
    def multiplied(self, other):
        bb = self.bb if other.bb is None else other.bb if self.bb is None else \
            (max(self.bb[0], other.bb[0]), max(self.bb[1], other.bb[1]), min(self.bb[2], other.bb[2]), min(self.bb[3], other.bb[3]))
        return Cov(self.w, self.h, [a * b for a, b in zip(self.d, other.d)], bb)
    def minus(self, other):
        return Cov(self.w, self.h, [max(0.0, a - b) for a, b in zip(self.d, other.d)], self.bb)
    def blur(self, r, passes=3, wrap_x=False):
        """Approximate gaussian: repeated box blur of radius r (pixels). Soft shadows and glows. Works inside a padded bbox."""
        d, w, h = self.d, self.w, self.h
        pad = r * passes + 1
        if self.bb is None:
            bb = None; ys = (0, h)
        else:
            x0, y0, x1, y1 = self.bb
            bb = (0, max(0, y0 - pad), w, min(h, y1 + pad)) if wrap_x else (max(0, x0 - pad), max(0, y0 - pad), min(w, x1 + pad), min(h, y1 + pad))
            ys = (bb[1], bb[3])
        for _ in range(passes):
            d = _box_h(d, w, h, r, wrap_x, ys)
            d = _box_v(d, w, h, r, ys)
        return Cov(w, h, d, bb)

def _box_h(d, w, h, r, wrap, ys=None):
    out = [0.0] * (w * h); n = 2 * r + 1
    ya, yb = ys if ys else (0, h)
    for y in range(ya, yb):
        row = d[y * w:(y + 1) * w]
        if not any(row):
            continue
        if wrap:
            ext = row[-r:] + row + row[:r] if r else row
        else:
            ext = [0.0] * r + row + [0.0] * r
        s = sum(ext[:n]); o = y * w
        for x in range(w):
            out[o + x] = s / n
            if x + n < len(ext):
                s += ext[x + n] - ext[x]
    return out

def _box_v(d, w, h, r, ys=None):
    out = [0.0] * (w * h); n = 2 * r + 1
    ya, yb = ys if ys else (0, h)
    for x in range(w):
        col = d[x::w]
        if not any(col):
            continue
        col = [0.0] * r + col + [0.0] * r
        s = sum(col[ya:ya + n])
        for y in range(ya, yb):
            out[y * w + x] = s / n
            if y + n < len(col):
                s += col[y + n] - col[y]
    return out

class Mask:
    """1-bit supersampled shape mask. Coordinates passed to the drawing calls are in OUTPUT pixels (floats ok)."""
    def __init__(self, w, h, ss=4, wrap_x=False):
        self.w, self.h, self.ss, self.wrap = w, h, ss, wrap_x
        self.rows = [bytearray(w * ss) for _ in range(h * ss)]
        self.bx0, self.by0, self.bx1, self.by1 = w * ss, h * ss, 0, 0     # touched bounds in supersampled units (empty = inverted)
    def _touch(self, x0, y0, x1, y1):
        if x0 < self.bx0: self.bx0 = x0
        if y0 < self.by0: self.by0 = y0
        if x1 > self.bx1: self.bx1 = x1
        if y1 > self.by1: self.by1 = y1
    # --- fills -------------------------------------------------------------
    def polygon(self, pts, value=1):
        """Even-odd scanline polygon fill. With wrap_x the polygon is also drawn shifted by +-width (seamless tiling)."""
        offs = (-self.w, 0, self.w) if self.wrap else (0,)
        ss = self.ss; W = self.w * ss; H = self.h * ss
        for ox in offs:
            P = [((x + ox) * ss, y * ss) for x, y in pts]
            if max(p[0] for p in P) < 0 or min(p[0] for p in P) > W:
                continue
            y0 = max(0, int(math.floor(min(p[1] for p in P)))); y1 = min(H - 1, int(math.ceil(max(p[1] for p in P))))
            if value:
                self._touch(max(0, int(min(p[0] for p in P)) - 1), y0, min(W, int(max(p[0] for p in P)) + 2), y1 + 1)
            n = len(P)
            for yy in range(y0, y1 + 1):
                yc = yy + 0.5; xs = []
                for i in range(n):
                    ax, ay = P[i]; bx, by = P[(i + 1) % n]
                    if (ay <= yc < by) or (by <= yc < ay):
                        xs.append(ax + (yc - ay) * (bx - ax) / (by - ay))
                xs.sort(); row = self.rows[yy]
                for i in range(0, len(xs) - 1, 2):
                    a = max(0, int(round(xs[i]))); b = min(W, int(round(xs[i + 1])))
                    if b > a:
                        row[a:b] = bytes([value]) * (b - a)
        return self
    def ellipse(self, cx, cy, rx, ry, n=None):
        n = n or max(24, int((rx + ry) * self.ss * 0.6))
        return self.polygon([(cx + rx * math.cos(2 * math.pi * i / n), cy + ry * math.sin(2 * math.pi * i / n)) for i in range(n)])
    def circle(self, cx, cy, r):
        return self.ellipse(cx, cy, r, r)
    def rrect(self, x0, y0, x1, y1, r, seg=10):
        r = min(r, (x1 - x0) / 2, (y1 - y0) / 2); pts = []
        for (cx, cy, a0) in ((x1 - r, y0 + r, -90), (x1 - r, y1 - r, 0), (x0 + r, y1 - r, 90), (x0 + r, y0 + r, 180)):
            for i in range(seg + 1):
                a = math.radians(a0 + 90 * i / seg)
                pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
        return self.polygon(pts)
    def capsule(self, x0, y0, x1, y1, r):
        """Thick line with round caps (smooth stroke segment)."""
        a = math.atan2(y1 - y0, x1 - x0); pts = []
        for i in range(13):
            t = a - math.pi / 2 + math.pi * i / 12; pts.append((x1 + r * math.cos(t), y1 + r * math.sin(t)))
        for i in range(13):
            t = a + math.pi / 2 + math.pi * i / 12; pts.append((x0 + r * math.cos(t), y0 + r * math.sin(t)))
        return self.polygon(pts)
    def polyline(self, pts, r):
        for (a, b) in zip(pts, pts[1:]):
            self.capsule(a[0], a[1], b[0], b[1], r)
        return self
    def subtract_polygon(self, pts):
        return self.polygon(pts, 0)
    # --- morphology --------------------------------------------------------
    def dilated(self, r_px):
        """Round dilation by r_px output pixels: the smooth outline primitive. outline = dilated(r) minus self."""
        R = max(1, int(round(r_px * self.ss))); out = Mask(self.w, self.h, self.ss, self.wrap)
        H = len(self.rows); W = self.w * self.ss
        hw = [int(math.sqrt(max(0, R * R - dy * dy)) + 0.5) for dy in range(R + 1)]
        run = re.compile(rb"\x01+")
        if self.bx1 > self.bx0 and self.by1 > self.by0:
            out._touch(0 if self.wrap else max(0, self.bx0 - R), max(0, self.by0 - R),
                       W if self.wrap else min(W, self.bx1 + R), min(H, self.by1 + R))
        for y in range(max(0, self.by0), min(H, self.by1)):
            row = self.rows[y]
            for m in run.finditer(bytes(row)):
                s, e = m.span()
                for dy in range(-R, R + 1):
                    yy = y + dy
                    if 0 <= yy < H:
                        a = s - hw[abs(dy)]; b = e + hw[abs(dy)]
                        if self.wrap:
                            for sh in (-W, 0, W):
                                aa = max(0, a + sh); bb = min(W, b + sh)
                                if bb > aa:
                                    out.rows[yy][aa:bb] = b"\x01" * (bb - aa)
                        else:
                            aa = max(0, a); bb = min(W, b)
                            out.rows[yy][aa:bb] = b"\x01" * (bb - aa)
        return out
    def minus(self, other):
        out = Mask(self.w, self.h, self.ss, self.wrap)
        for i, (a, b) in enumerate(zip(self.rows, other.rows)):
            ia = int.from_bytes(a, "big"); ib = int.from_bytes(b, "big")
            out.rows[i] = bytearray((ia & ~ib & int.from_bytes(b"\x01" * len(a), "big")).to_bytes(len(a), "big"))
        out.bx0, out.by0, out.bx1, out.by1 = self.bx0, self.by0, self.bx1, self.by1
        return out
    # --- resolve -----------------------------------------------------------
    def cov(self):
        ss, w, h = self.ss, self.w, self.h; k = 1.0 / (ss * ss)
        if self.bx1 <= self.bx0 or self.by1 <= self.by0:
            return Cov(w, h, None, (0, 0, 0, 0))
        xa = self.bx0 // ss; xb = min(w, self.bx1 // ss + 1); ya = self.by0 // ss; yb = min(h, self.by1 // ss + 1)
        out = [0.0] * (w * h); nb = (xb - xa) * ss
        for y in range(ya, yb):
            acc = 0
            for j in range(ss):
                acc += int.from_bytes(self.rows[y * ss + j][xa * ss:xb * ss], "big")
            b = acc.to_bytes(nb, "big")
            if ss == 4:
                out[y * w + xa:y * w + xb] = [(a + c + d + e) * k for a, c, d, e in zip(b[0::4], b[1::4], b[2::4], b[3::4])]
            else:
                out[y * w + xa:y * w + xb] = [sum(b[x * ss:(x + 1) * ss]) * k for x in range(xb - xa)]
        return Cov(w, h, out, (xa, ya, xb, yb))

class Image:
    """Straight-alpha float RGBA canvas (r,g,b 0..255, a 0..1)."""
    def __init__(self, w, h, fill=(0, 0, 0, 0.0)):
        self.w, self.h = w, h; n = w * h
        self.r = [float(fill[0])] * n; self.g = [float(fill[1])] * n; self.b = [float(fill[2])] * n; self.a = [float(fill[3])] * n
    def paint(self, cov, color, opacity=1.0):
        """Composite colour (an (r,g,b) tuple or f(x, y) -> (r,g,b)) through a Cov, source-over."""
        w, R, G, B, A = self.w, self.r, self.g, self.b, self.a
        if isinstance(color, str):
            color = _hex(color)
        fn = color if callable(color) else (lambda x, y, c=color: c)
        d = cov.d
        x0, y0, x1, y1 = cov.bb if cov.bb is not None else (0, 0, w, self.h)
        for y in range(y0, y1):
            o = y * w; yc = y + 0.5
            if not any(d[o + x0:o + x1]):
                continue
            for x in range(x0, x1):
                sa = d[o + x] * opacity
                if sa <= 0.0:
                    continue
                if sa > 1.0: sa = 1.0
                cr, cg, cb = fn(x + 0.5, yc); i = o + x; da = A[i]
                oa = sa + da * (1 - sa)
                k = da * (1 - sa)
                R[i] = (cr * sa + R[i] * k) / oa; G[i] = (cg * sa + G[i] * k) / oa; B[i] = (cb * sa + B[i] * k) / oa
                A[i] = oa
        return self
    def paint_mask(self, mask, color, opacity=1.0):
        return self.paint(mask.cov(), color, opacity)
    def multiply_alpha(self, fn):
        """Scale alpha by fn(x, y) in 0..1 (soft skirt fade by gradient, not dither)."""
        for y in range(self.h):
            for x in range(self.w):
                self.a[y * self.w + x] *= fn(x + 0.5, y + 0.5)
        return self

def fill_below_curve(img, top_fn, color, ss=4, wrap_x=False, bottom=None):
    """Analytic-vertical, supersampled-horizontal fill of everything below y = top_fn(x) (hills, ground silhouettes).
    top_fn takes x in float output pixels. Far cheaper than a Mask for full-width silhouettes."""
    w, h = img.w, img.h; bottom = h if bottom is None else bottom
    cov = Cov(w, h); ytop = h
    for x in range(w):
        tops = [top_fn(x + (i + 0.5) / ss) for i in range(ss)]
        lo, hi = min(tops), max(tops)
        ytop = min(ytop, max(0, int(lo) - 1))
        for y in range(max(0, int(lo) - 1), min(h, bottom)):
            if y + 1 <= lo:
                continue
            if y >= hi:
                cov.d[y * w + x] = 1.0
            else:
                cov.d[y * w + x] = sum(min(1.0, max(0.0, y + 1 - t)) for t in tops) / ss
    cov.bb = (0, min(ytop, h), w, min(h, bottom))
    img.paint(cov, color)
    return cov

def bleed(img, px=4):
    """Colour bleed: transparent pixels within px of coloured ones take the neighbours' colour (alpha stays 0), so bilinear
    filtering and mip chains never pull in black or white halos (Bible 2.4)."""
    w, h = img.w, img.h; R, G, B, A = img.r, img.g, img.b, img.a
    known = [a > 0.0 for a in A]
    for _ in range(px):
        new = []
        rowhas = [any(known[y * w:(y + 1) * w]) for y in range(h)]
        for y in range(h):
            if not (rowhas[y] or (y > 0 and rowhas[y - 1]) or (y + 1 < h and rowhas[y + 1])) or all(known[y * w:(y + 1) * w]):
                continue
            for x in range(w):
                i = y * w + x
                if known[i]:
                    continue
                sr = sg = sb = n = 0
                for yy in (y - 1, y, y + 1):
                    if 0 <= yy < h:
                        for xx in (x - 1, x, x + 1):
                            if 0 <= xx < w and known[yy * w + xx]:
                                j = yy * w + xx; sr += R[j]; sg += G[j]; sb += B[j]; n += 1
                if n:
                    new.append((i, sr / n, sg / n, sb / n))
        for i, r, g, b in new:
            R[i], G[i], B[i] = r, g, b; known[i] = True
    return img

def tile_seam_score(img):
    """Mean absolute RGB step across the wrap seam divided by the mean step between ordinary neighbouring columns.
    ~1.0 = invisible seam; the bible asks for a tight match, so treat > 1.5 as a visible seam."""
    w, h = img.w, img.h
    def step(x0, x1):
        s = 0.0
        for y in range(h):
            i, j = y * w + x0, y * w + x1
            s += (abs(img.r[i] - img.r[j]) + abs(img.g[i] - img.g[j]) + abs(img.b[i] - img.b[j])) * 0.5 * (img.a[i] + img.a[j]) / 2 \
                 + abs(img.a[i] - img.a[j]) * 255
        return s / h
    inner = sum(step(x, x + 1) for x in range(0, w - 1, max(1, w // 64))) / len(range(0, w - 1, max(1, w // 64)))
    return step(w - 1, 0) / (inner or 1e-6)

def write_png(img, path, do_bleed=True):
    """8-bit RGBA PNG. Applies a 4 px colour bleed first unless disabled."""
    if do_bleed:
        bleed(img, 4)
    w, h = img.w, img.h; raw = bytearray()
    for y in range(h):
        raw.append(0); o = y * w
        row = bytearray(w * 4)
        for x in range(w):
            i = o + x
            row[x * 4] = min(255, max(0, int(img.r[i] + 0.5))); row[x * 4 + 1] = min(255, max(0, int(img.g[i] + 0.5)))
            row[x * 4 + 2] = min(255, max(0, int(img.b[i] + 0.5))); row[x * 4 + 3] = min(255, max(0, int(img.a[i] * 255 + 0.5)))
        raw += row
    def chunk(t, d):
        c = struct.pack(">I", len(d)) + t + d
        return c + struct.pack(">I", zlib.crc32(t + d) & 0xffffffff)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)) +
                chunk(b"IDAT", zlib.compress(bytes(raw), 6)) + chunk(b"IEND", b""))
