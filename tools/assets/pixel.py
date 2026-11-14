#!/usr/bin/env python3
"""Tiny pixel-art toolkit, standard library only (no Pillow needed). Used by the asset-forge agent.

Images are (width, height, rows) where rows is a list of lists of (r, g, b, a) tuples, row 0 at the top.
Non-interlaced PNGs: 8-bit grayscale, RGB, palette, grayscale+alpha, RGBA, and 1/2/4-bit palette or grayscale.
"""
import struct
import zlib


def read_png(path):
    data = open(path, "rb").read()
    assert data[:8] == b"\x89PNG\r\n\x1a\n", "not a PNG"
    pos, idat, pal, trns = 8, b"", None, None
    w = h = depth = ctype = 0
    while pos < len(data):
        n, = struct.unpack(">I", data[pos:pos + 4])
        kind, body = data[pos + 4:pos + 8], data[pos + 8:pos + 8 + n]
        pos += 12 + n
        if kind == b"IEND":
            break
        if kind == b"IHDR":
            w, h, depth, ctype, _, _, interlace = struct.unpack(">IIBBBBB", body)
            assert interlace == 0, "interlaced PNGs are not supported"
            assert depth == 8 or (depth in (1, 2, 4) and ctype in (0, 3)), "unsupported bit depth for this colour type"
        elif kind == b"PLTE":
            pal = [tuple(body[i:i + 3]) for i in range(0, len(body), 3)]
        elif kind == b"tRNS":
            trns = body
        elif kind == b"IDAT":
            idat += body
    channels = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}[ctype]
    bits = channels * depth
    bpp = max(1, bits // 8)               # filter distance in bytes
    stride = (w * bits + 7) // 8          # bytes per scanline
    raw, prev, i, rows = zlib.decompress(idat), bytearray(stride), 0, []
    for _ in range(h):
        f, line = raw[i], bytearray(raw[i + 1:i + 1 + stride])
        i += 1 + stride
        for x in range(stride):
            a = line[x - bpp] if x >= bpp else 0
            b = prev[x]
            c = prev[x - bpp] if x >= bpp else 0
            if f == 1: line[x] = (line[x] + a) & 255
            elif f == 2: line[x] = (line[x] + b) & 255
            elif f == 3: line[x] = (line[x] + ((a + b) >> 1)) & 255
            elif f == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                line[x] = (line[x] + (a if pa <= pb and pa <= pc else (b if pb <= pc else c))) & 255
        prev = line
        row = []
        for x in range(w):
            if depth < 8:                  # packed samples, high bits first
                per = 8 // depth
                v = (line[x // per] >> (8 - depth * (x % per + 1))) & ((1 << depth) - 1)
                if ctype == 3:
                    r, g, bl = pal[v]
                    row.append((r, g, bl, trns[v] if trns and v < len(trns) else 255))
                else:
                    gray = v * 255 // ((1 << depth) - 1)
                    row.append((gray, gray, gray, 255))
                continue
            q = line[x * bpp:(x + 1) * bpp]
            if ctype == 6: row.append((q[0], q[1], q[2], q[3]))
            elif ctype == 2: row.append((q[0], q[1], q[2], 255))
            elif ctype == 0: row.append((q[0], q[0], q[0], 255))
            elif ctype == 4: row.append((q[0], q[0], q[0], q[1]))
            else:
                r, g, bl = pal[q[0]]
                row.append((r, g, bl, trns[q[0]] if trns and q[0] < len(trns) else 255))
        rows.append(row)
    return w, h, rows


def write_png(path, w, h, rows):
    raw = bytearray()
    for row in rows:
        raw.append(0)
        for r, g, b, a in row:
            raw += bytes((r, g, b, a))

    def chunk(kind, body):
        c = struct.pack(">I", len(body)) + kind + body
        return c + struct.pack(">I", zlib.crc32(kind + body) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b"")
    open(path, "wb").write(png)


def blank(w, h, colour=(0, 0, 0, 0)):
    return [[colour for _ in range(w)] for _ in range(h)]


def crop(rows, x, y, w, h):
    return [row[x:x + w] for row in rows[y:y + h]]


def flip_x(rows):
    return [list(reversed(row)) for row in rows]


def flip_y(rows):
    return [list(row) for row in reversed(rows)]


def scale_nearest(rows, k):
    out = []
    for row in rows:
        wide = [p for p in row for _ in range(k)]
        out += [list(wide) for _ in range(k)]
    return out


def paste(dst, src, x, y):
    """Alpha-over paste of src onto dst at (x, y). Mutates dst."""
    for sy, row in enumerate(src):
        for sx, (r, g, b, a) in enumerate(row):
            dx, dy = x + sx, y + sy
            if a == 0 or not (0 <= dy < len(dst) and 0 <= dx < len(dst[0])):
                continue
            if a == 255:
                dst[dy][dx] = (r, g, b, 255)
            else:
                R, G, B, A = dst[dy][dx]
                t = a / 255.0
                dst[dy][dx] = (int(r * t + R * (1 - t)), int(g * t + G * (1 - t)), int(b * t + B * (1 - t)), max(a, A))
    return dst


def recolour(rows, fn):
    """fn((r,g,b,a)) -> (r,g,b,a). Transparent pixels are left alone."""
    return [[p if p[3] == 0 else fn(p) for p in row] for row in rows]


def swap_colours(rows, mapping):
    """Replace exact colours (ignoring alpha) using {(r,g,b): (r,g,b)}."""
    return recolour(rows, lambda p: (*mapping.get(p[:3], p[:3]), p[3]))


def shift_hue(rows, degrees):
    import colorsys
    def fn(p):
        h, s, v = colorsys.rgb_to_hsv(p[0] / 255, p[1] / 255, p[2] / 255)
        r, g, b = colorsys.hsv_to_rgb((h + degrees / 360.0) % 1.0, s, v)
        return (int(r * 255), int(g * 255), int(b * 255), p[3])
    return recolour(rows, fn)


def tint(rows, rgb, amount):
    """Blend every visible pixel towards rgb by amount (0..1)."""
    return recolour(rows, lambda p: tuple(int(p[i] * (1 - amount) + rgb[i] * amount) for i in range(3)) + (p[3],))


def palette(rows):
    """Set of opaque colours used."""
    return {p[:3] for row in rows for p in row if p[3] > 0}


def bounds(rows):
    """(x, y, w, h) of the non-transparent area, or None."""
    ys = [y for y, row in enumerate(rows) if any(p[3] for p in row)]
    xs = [x for row in rows for x, p in enumerate(row) if p[3]]
    return (min(xs), min(ys), max(xs) - min(xs) + 1, max(ys) - min(ys) + 1) if xs else None


def tileable_score(rows):
    """0 = left and right edges match exactly; larger = a visible seam when tiled horizontally."""
    h = len(rows)
    total = 0
    for y in range(h):
        a, b = rows[y][0], rows[y][-1]
        total += sum(abs(a[i] - b[i]) for i in range(4))
    return total / (h * 4 * 255.0)


if __name__ == "__main__":
    import sys
    if len(sys.argv) > 1:
        w, h, rows = read_png(sys.argv[1])
        print(f"{sys.argv[1]}: {w}x{h}, {len(palette(rows))} colours, bounds {bounds(rows)}, tile seam {tileable_score(rows):.3f}")
