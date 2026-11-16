"""Slice Master484 'Dragon 1' sheet (CC0) into separate Road Eater part PNGs (4 px colour bleed, transparent bg).
Usage: python3 -I tools/art/slice_road_eater.py <M484OrangeDragon.png> <out_dir>"""
import struct, sys, zlib, os
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__))))
from smoothart.raster import Image, write_png

def read_png(p):
    d = open(p, "rb").read(); pos = 8; idat = b""; 
    while pos < len(d):
        n, t = struct.unpack(">I4s", d[pos:pos+8]); c = d[pos+8:pos+8+n]; pos += 12 + n
        if t == b"IHDR": w, h, bd, ct, _, _, il = struct.unpack(">IIBBBBB", c)
        elif t == b"IDAT": idat += c
    assert bd == 8 and ct == 6 and il == 0, (bd, ct, il)
    raw = zlib.decompress(idat); s = w * 4; rows = []; prev = bytearray(s); o = 0
    for y in range(h):
        f = raw[o]; line = bytearray(raw[o+1:o+1+s]); o += 1 + s
        for i in range(s):
            a = line[i-4] if i >= 4 else 0; b = prev[i]; c2 = prev[i-4] if i >= 4 else 0
            if f == 1: line[i] = (line[i] + a) & 255
            elif f == 2: line[i] = (line[i] + b) & 255
            elif f == 3: line[i] = (line[i] + (a + b) // 2) & 255
            elif f == 4:
                pa, pb, pc = abs(b - c2), abs(a - c2), abs(a + b - 2 * c2)
                pr = a if pa <= pb and pa <= pc else (b if pb <= pc else c2)
                line[i] = (line[i] + pr) & 255
        rows.append(line); prev = line
    return w, h, rows

def components(w, h, rows):
    seen = bytearray(w * h); comps = []
    for y0 in range(h):
        for x0 in range(w):
            if seen[y0*w+x0] or rows[y0][x0*4+3] == 0: continue
            st = [(x0, y0)]; seen[y0*w+x0] = 1; x1 = x2 = x0; y1 = y2 = y0; px = []
            while st:
                x, y = st.pop(); px.append((x, y)); x1 = min(x1, x); x2 = max(x2, x); y1 = min(y1, y); y2 = max(y2, y)
                for dx in (-1, 0, 1):
                    for dy in (-1, 0, 1):
                        nx, ny = x+dx, y+dy
                        if 0 <= nx < w and 0 <= ny < h and not seen[ny*w+nx] and rows[ny][nx*4+3] > 0:
                            seen[ny*w+nx] = 1; st.append((nx, ny))
            if len(px) > 300: comps.append((x1, y1, x2, y2, px))
    return comps

def save(name, comp, rows, out, pad=6):
    x1, y1, x2, y2, px = comp; W, H = x2-x1+1+2*pad, y2-y1+1+2*pad
    img = Image(W, H)
    for x, y in px:
        r = rows[y][x*4:x*4+4]; i = (y-y1+pad)*W + (x-x1+pad)
        img.r[i], img.g[i], img.b[i], img.a[i] = r[0], r[1], r[2], r[3]/255
    write_png(img, os.path.join(out, name + ".png")); print(name, W, H)

if __name__ == "__main__":
    w, h, rows = read_png(sys.argv[1]); out = sys.argv[2]; os.makedirs(out, exist_ok=True)
    # sheet has an opaque pure-black background: key out black connected to the border (open mouths become see-through)
    st = [(x, y) for x in range(w) for y in (0, h-1)] + [(x, y) for y in range(h) for x in (0, w-1)]
    while st:
        x, y = st.pop()
        if 0 <= x < w and 0 <= y < h and rows[y][x*4+3] and not any(rows[y][x*4:x*4+3]):
            rows[y][x*4+3] = 0; st += [(x+1, y), (x-1, y), (x, y+1), (x, y-1)]
    cs = components(w, h, rows)
    for c in cs: print("comp", c[:4], len(c[4]))
    # layout: top row y<260 heads/split parts; ball (y 270-400,x<300); flames (y 360-450, 380<x<940); rest = worm examples (ignored)
    top = sorted([c for c in cs if c[3] < 260], key=lambda c: c[0])
    ball = [c for c in cs if 270 < c[1] and c[3] < 400 and c[2] < 300][0]
    flames = sorted([c for c in cs if 360 < c[1] < 450 and 380 < c[0] < 940], key=lambda c: c[0])
    whole = [c for c in top if c[2]-c[0] > 150 and c[3]-c[1] < 170 and not (c[0] > 290 and c[0] < 320)]
    split = [c for c in top if 290 < c[0] < 320]
    upper = min(split, key=lambda c: c[1]); lower = max(split, key=lambda c: c[1])
    for i, c in enumerate(whole, 1): save("eater_head_%d" % i, c, rows, out)
    save("eater_upper_head", upper, rows, out); save("eater_lower_jaw", lower, rows, out)
    save("eater_body_segment", ball, rows, out)
    for i, c in enumerate(flames, 1): save("eater_flame_%d" % i, c, rows, out)
