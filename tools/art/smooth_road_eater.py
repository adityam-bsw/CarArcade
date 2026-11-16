"""Smooth the CC0 Road-Eater pixel sprites (QA11 P3). Own code, python stdlib only, no generative AI.
For every eater_*.png in <dir> writes eater_*_hd.png at 2x size (use ppu x2 so the world size is unchanged):
  1. EPX / Scale2x edge-preserving doubling (diagonals stay diagonal, no blocky stair-steps)
  2. premultiplied-alpha 3x3 binomial blur on alpha (soft anti-aliased silhouette), 35 percent blend on colour (removes pixel steps, keeps the inked detail)
  3. 4 px colour bleed into transparent pixels so bilinear filtering shows no dark fringe
Usage: python3 -I tools/art/smooth_road_eater.py Assets/Art/Smooth/Hazards/RoadEater
"""
import os, struct, sys, zlib

def read_png(p):
    d = open(p, "rb").read(); pos = 8; idat = b""
    while pos < len(d):
        n, t = struct.unpack(">I4s", d[pos:pos + 8]); c = d[pos + 8:pos + 8 + n]; pos += 12 + n
        if t == b"IHDR": w, h, bd, ct, _, _, il = struct.unpack(">IIBBBBB", c)
        elif t == b"IDAT": idat += c
    assert bd == 8 and ct == 6 and il == 0
    raw = zlib.decompress(idat); s = w * 4; px = []; prev = bytearray(s); o = 0
    for y in range(h):
        f = raw[o]; line = bytearray(raw[o + 1:o + 1 + s]); o += 1 + s
        for i in range(s):
            a = line[i - 4] if i >= 4 else 0; b = prev[i]; c2 = prev[i - 4] if i >= 4 else 0
            if f == 1: line[i] = (line[i] + a) & 255
            elif f == 2: line[i] = (line[i] + b) & 255
            elif f == 3: line[i] = (line[i] + (a + b) // 2) & 255
            elif f == 4:
                pa, pb, pc = abs(b - c2), abs(a - c2), abs(a + b - 2 * c2)
                line[i] = (line[i] + (a if pa <= pb and pa <= pc else (b if pb <= pc else c2))) & 255
        for x in range(w): px.append(tuple(line[x * 4:x * 4 + 4]))
        prev = line
    return w, h, px

def write_png(path, w, h, px):
    raw = bytearray()
    for y in range(h):
        raw.append(0)
        for x in range(w): raw += bytes(px[y * w + x])
    def chunk(t, d): c = struct.pack(">I", len(d)) + t + d; return c + struct.pack(">I", zlib.crc32(t + d) & 0xffffffff)
    open(path, "wb").write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b""))

def epx(w, h, px):
    W, H = w * 2, h * 2; out = [None] * (W * H)
    def g(x, y):
        x = min(w - 1, max(0, x)); y = min(h - 1, max(0, y)); return px[y * w + x]
    for y in range(h):
        for x in range(w):
            p = g(x, y); a = g(x, y - 1); b = g(x + 1, y); c = g(x - 1, y); d = g(x, y + 1)
            e1 = e2 = e3 = e4 = p
            if c == a and c != d and a != b: e1 = a
            if a == b and a != c and b != d: e2 = b
            if d == c and d != b and c != a: e3 = c
            if b == d and b != a and d != c: e4 = d
            o = 2 * y * W + 2 * x; out[o] = e1; out[o + 1] = e2; out[o + W] = e3; out[o + W + 1] = e4
    return W, H, out

def smooth(w, h, px):
    n = w * h
    A = [p[3] / 255.0 for p in px]; R = [px[i][0] * A[i] for i in range(n)]; G = [px[i][1] * A[i] for i in range(n)]; B = [px[i][2] * A[i] for i in range(n)]
    def blur(ch):
        t = [0.0] * n
        for y in range(h):
            r = y * w
            for x in range(w):
                l = ch[r + max(0, x - 1)]; c = ch[r + x]; rr = ch[r + min(w - 1, x + 1)]
                t[r + x] = (l + 2 * c + rr) * 0.25
        o = [0.0] * n
        for y in range(h):
            ym = max(0, y - 1) * w; yp = min(h - 1, y + 1) * w
            for x in range(w):
                o[y * w + x] = (t[ym + x] + 2 * t[y * w + x] + t[yp + x]) * 0.25
        return o
    A2 = blur(A); R2 = blur(R); G2 = blur(G); B2 = blur(B)
    k = 0.35; out = []
    for i in range(n):
        a = A2[i]
        if a <= 0.004: out.append((0, 0, 0, 0)); continue
        # colour: premultiplied mix of original and blurred, then divide by the blurred coverage
        r = (R[i] * (1 - k) + R2[i] * k) / max(A[i] * (1 - k) + a * k, 1e-4) if A[i] > 0 else R2[i] / a
        g = (G[i] * (1 - k) + G2[i] * k) / max(A[i] * (1 - k) + a * k, 1e-4) if A[i] > 0 else G2[i] / a
        b = (B[i] * (1 - k) + B2[i] * k) / max(A[i] * (1 - k) + a * k, 1e-4) if A[i] > 0 else B2[i] / a
        out.append((min(255, int(r + .5)), min(255, int(g + .5)), min(255, int(b + .5)), min(255, int(a * 255 + .5))))
    return out

def bleed(w, h, px, rad=4):
    px = list(px)
    for _ in range(rad):
        nxt = list(px)
        for y in range(h):
            for x in range(w):
                if px[y * w + x][3] > 0: continue
                s = [0, 0, 0, 0]
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        xx, yy = x + dx, y + dy
                        if 0 <= xx < w and 0 <= yy < h and px[yy * w + xx][3] > 0 and (px[yy * w + xx][0] or px[yy * w + xx][1] or px[yy * w + xx][2] or True):
                            q = px[yy * w + xx]; s[0] += q[0]; s[1] += q[1]; s[2] += q[2]; s[3] += 1
                if s[3]: nxt[y * w + x] = (s[0] // s[3], s[1] // s[3], s[2] // s[3], 0)
        px = nxt
    return px

def main(d):
    for f in sorted(os.listdir(d)):
        if not f.startswith("eater_") or not f.endswith(".png") or f.endswith("_hd.png"): continue
        w, h, px = read_png(os.path.join(d, f))
        W, H, big = epx(w, h, px); big = smooth(W, H, big); big = bleed(W, H, big)
        write_png(os.path.join(d, f[:-4] + "_hd.png"), W, H, big); print(f, "->", W, "x", H)

if __name__ == "__main__": main(sys.argv[1])
