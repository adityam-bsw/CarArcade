#!/usr/bin/env python3
"""Clean the user-supplied RO-B0 sprites for bilinear filtering (standard library only, uses tools/assets/pixel.py).

usage: python3 -I tools/art/clean_rob0.py <incoming_dir> <out_dir>

Per image: drop the faint noisy alpha haze (alpha < HAZE), feather the hard clip the generator left 40 px from the
canvas edge, remove tiny stray specks, repair dark RGB under partial alpha in glows (thruster, shield), then
colour-bleed the RGB of edge pixels into the transparent neighbours so bilinear sampling shows no dark rim.
Poses 01-06 are registered on the cream shell centroid so the body sits identically, then cropped with ONE shared
box (+PAD). Shield and projectiles are cropped to their own bounds (+PAD). Writes CROP_OFFSETS.txt.
"""
import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "assets"))
import pixel

HAZE, FEATHER_START, FEATHER_LEN, PAD, BLEED = 24, 40, 28, 8, 6
POSES = ["01_body_idle", "02_eye_open", "03_eye_angry", "04_eye_hurt_dizzy", "05_cannon_firing", "06_body_damaged"]
OTHERS = ["07_shield_bubble", "08_projectile_rock", "09_projectile_bolt", "10_projectile_bubble"]
EXCLUDE = {"04_eye_hurt_dizzy": lambda x, y: x >= 880}   # stray cut-off shield bubble at the right edge of pose 04
N8 = [(-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1)]


def load(path):
    w, h, rows = pixel.read_png(path)
    return w, h, [list(p) for row in rows for p in row]


def clean(name, w, h, px):
    ex = EXCLUDE.get(name)
    for y in range(h):
        for x in range(w):
            p = px[y * w + x]
            d = min(x, y, w - 1 - x, h - 1 - y)
            f = max(0.0, min(1.0, (d - FEATHER_START) / FEATHER_LEN))
            a = int(p[3] * f)
            if a < HAZE or (ex and ex(x, y)):
                px[y * w + x] = [0, 0, 0, 0]
            else:
                p[3] = a
    # remove small specks (8-connected components under 80 px)
    seen = bytearray(w * h)
    for s in range(w * h):
        if seen[s] or px[s][3] == 0:
            continue
        comp, stack = [s], [s]
        seen[s] = 1
        while stack:
            i = stack.pop()
            x, y = i % w, i // w
            for dx, dy in N8:
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h:
                    j = ny * w + nx
                    if not seen[j] and px[j][3]:
                        seen[j] = 1; comp.append(j); stack.append(j)
        if len(comp) < 80:
            for i in comp:
                px[i] = [0, 0, 0, 0]
    # dark RGB under partial alpha with no solid dark outline next to it (glow fringe): take the nearby bright colour
    fixed = []
    for y in range(h):
        for x in range(w):
            p = px[y * w + x]
            if 0 < p[3] < 230 and max(p[:3]) < 48:
                sr = sg = sb = sw = 0
                outline = False
                for dy in range(-4, 5):
                    for dx in range(-4, 5):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < w and 0 <= ny < h:
                            q = px[ny * w + nx]
                            if q[3] >= 230 and max(q[:3]) < 60 and abs(dx) <= 2 and abs(dy) <= 2:
                                outline = True
                            if q[3] >= HAZE and max(q[:3]) >= 60:
                                sr += q[0] * q[3]; sg += q[1] * q[3]; sb += q[2] * q[3]; sw += q[3]
                if not outline and sw:
                    fixed.append((y * w + x, [sr // sw, sg // sw, sb // sw]))
    for i, c in fixed:
        px[i][:3] = c
    # colour bleed into transparent neighbours
    for _ in range(BLEED):
        new = []
        for y in range(h):
            for x in range(w):
                if px[y * w + x][3] == 0:
                    sr = sg = sb = n = 0
                    for dx, dy in N8:
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < w and 0 <= ny < h:
                            q = px[ny * w + nx]
                            if q[3] > 0 or len(q) > 4:
                                sr += q[0]; sg += q[1]; sb += q[2]; n += 1
                    if n:
                        new.append((y * w + x, [sr // n, sg // n, sb // n, 0, 1]))
        if not new:
            break
        for i, c in new:
            px[i] = c
    return [(p[0], p[1], p[2], p[3]) for p in px]


def anchor(w, h, px):
    sx = sy = n = 0
    for i, p in enumerate(px):
        if p[3] >= 250 and min(p[:3]) >= 200 and max(p[:3]) - min(p[:3]) <= 45:
            sx += i % w; sy += i // w; n += 1
    return sx / n, sy / n


def bbox(w, h, px):
    xs = [i % w for i, p in enumerate(px) if p[3]]
    ys = [i // w for i, p in enumerate(px) if p[3]]
    return min(xs), min(ys), max(xs), max(ys)


def shifted(w, h, px, dx, dy):
    out = [(0, 0, 0, 0)] * (w * h)
    for i, p in enumerate(px):
        x, y = i % w + dx, i // w + dy
        if 0 <= x < w and 0 <= y < h:
            out[y * w + x] = p
    return out


def save(path, px, w, h, box):
    x0, y0, x1, y1 = box
    rows = [[px[y * w + x] for x in range(x0, x1 + 1)] for y in range(y0, y1 + 1)]
    pixel.write_png(path, x1 - x0 + 1, y1 - y0 + 1, rows)


def main(src, out):
    os.makedirs(out, exist_ok=True)
    notes = []
    imgs = {}
    for n in POSES + OTHERS:
        w, h, px = load(os.path.join(src, n + ".png"))
        imgs[n] = (w, h, clean(n, w, h, px))
    ref = anchor(*imgs[POSES[0]])
    reg, boxes = {}, []
    for n in POSES:
        w, h, px = imgs[n]
        ax, ay = anchor(w, h, px)
        dx, dy = round(ref[0] - ax), round(ref[1] - ay)
        notes.append("%s anchor (%.1f, %.1f) shift (%+d, %+d)" % (n, ax, ay, dx, dy))
        reg[n] = shifted(w, h, px, dx, dy)
        boxes.append(bbox(w, h, reg[n]))
    box = (max(0, min(b[0] for b in boxes) - PAD), max(0, min(b[1] for b in boxes) - PAD),
           min(1023, max(b[2] for b in boxes) + PAD), min(1023, max(b[3] for b in boxes) + PAD))
    cw, ch = box[2] - box[0] + 1, box[3] - box[1] + 1
    for n in POSES:
        save(os.path.join(out, "rob0_" + n[3:] + ".png"), reg[n], 1024, 1024, box)
    px_ = ref[0] - box[0]; py_ = ref[1] - box[1]
    notes.insert(0, "Shared crop box on the registered 1024x1024 canvas (x0,y0,x1,y1) = %s, size %dx%d" % (box, cw, ch))
    notes.insert(1, "Registration reference = cream shell centroid of pose 01 at canvas (%.1f, %.1f); poses shifted by the offsets below before cropping" % ref)
    notes.insert(2, "Pivot (body anchor) inside the crop: (%.1f, %.1f) px from top-left = (%.4f, %.4f) normalised, y up" % (px_, py_, px_ / cw, 1 - py_ / ch))
    for n in OTHERS:
        w, h, px = imgs[n]
        b = bbox(w, h, px)
        bx = (max(0, b[0] - PAD), max(0, b[1] - PAD), min(w - 1, b[2] + PAD), min(h - 1, b[3] + PAD))
        save(os.path.join(out, "rob0_" + n[3:] + ".png"), px, w, h, bx)
        notes.append("%s crop box %s size %dx%d" % (n, bx, bx[2] - bx[0] + 1, bx[3] - bx[1] + 1))
    open(os.path.join(out, "CROP_OFFSETS.txt"), "w").write("\n".join(notes) + "\n")
    print("\n".join(notes))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
