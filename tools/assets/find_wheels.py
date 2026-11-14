#!/usr/bin/env python3
"""Finds the wheels drawn inside each vehicle preview (dark ring around a lighter hub) and writes
Assets/Art/Catalog/wheels.json plus one round, transparent wheel picture per wheel in Assets/Art/Catalog/Wheels/.
The Unity catalogue builder links them to the vehicle entries; at run time each wheel picture is laid over its baked
wheel and spins with the physics wheel. Needs Pillow:  python3 tools/assets/find_wheels.py [--debug dir]
"""
import json, math, pathlib, sys
from PIL import Image, ImageDraw

ROOT = pathlib.Path(__file__).resolve().parents[2]
PREV = ROOT / "Assets/Art/Catalog/Previews"
OUT = ROOT / "Assets/Art/Catalog/Wheels"
# vehicles that roll on tracks, runners or water: no wheel to spin
SKIP = {"tank", "sled", "snowboard", "toboggan", "snowmobile", "speedboat", "jet-ski", "hovercraft", "rocket-sled", "skateboard", "handcar"}
# hand corrections where detection is not enough: id -> [(cx, cy, r)] in pixels from the top-left
MANUAL = {}
# a wheel the detector cannot see (hidden behind a fender): same height and size as the first wheel, at this x
MIRROR_X = {"taxi": 6.0}

def dark(px):
    return px[3] > 200 and (0.3 * px[0] + 0.59 * px[1] + 0.11 * px[2]) < 90

def detect(im):
    w, h = im.size
    px = im.load()
    found = []
    for r2 in range(24, 3, -1):            # radius in half pixels, big first
        r = r2 / 2.0
        n = max(12, int(r * 10))
        for cy2 in range(int(h * 0.8), 2 * h):
            cy = cy2 / 2.0
            if cy + r > h + 0.01 or cy < h * 0.35: continue
            for cx2 in range(0, 2 * w):
                cx = cx2 / 2.0
                if cx - r < -0.01 or cx + r > w + 0.01: continue
                hit = 0
                for i in range(n):
                    a = 2 * math.pi * i / n
                    x = int(cx + (r - 0.6) * math.cos(a)); y = int(cy + (r - 0.6) * math.sin(a))
                    if 0 <= x < w and 0 <= y < h and dark(px[x, y]): hit += 1
                if hit / n < 0.92: continue
                # a wheel has a lighter hub: centre not dark
                hub = px[min(w - 1, int(cx)), min(h - 1, int(cy))]
                if r >= 3 and dark(hub) and sum(1 for dx in (-1, 0, 1) for dy in (-1, 0, 1) if dark(px[min(w-1,max(0,int(cx)+dx)), min(h-1,max(0,int(cy)+dy))])) == 9:
                    # solid dark disc (e.g. monster tyre): accept only when the ring is at the sprite's lower edge
                    pass
                if any(math.hypot(cx - fx, cy - fy) < 0.9 * max(r, fr) for fx, fy, fr in found): continue
                found.append((cx, cy, r))
    if not found: return found
    # wheels stand on the ground: keep the circles whose lowest point is (nearly) the lowest of all
    low = max(cy + r for _, cy, r in found)
    keep = [t for t in found if t[1] + t[2] >= low - 1.5]
    big = max(t[2] for t in keep)
    return [t for t in keep if t[2] >= big * 0.6]

def main():
    debug = pathlib.Path(sys.argv[sys.argv.index("--debug") + 1]) if "--debug" in sys.argv else None
    OUT.mkdir(parents=True, exist_ok=True)
    for f in OUT.glob("*.png"): f.unlink()
    data = []
    for f in sorted(PREV.glob("vehicle-*.png")):
        vid = f.stem
        if vid[8:] in SKIP: continue
        im = Image.open(f).convert("RGBA")
        ws = MANUAL.get(vid[8:]) or detect(im)
        if vid[8:] in MIRROR_X and len(ws) == 1: ws = ws + [(MIRROR_X[vid[8:]], ws[0][1], ws[0][2])]
        ws = sorted(ws, key=lambda t: t[0])
        if len(ws) < 2: continue          # needs a back and a front wheel
        if not ws: continue
        rows = []
        for i, (cx, cy, r) in enumerate(ws):
            ri = int(math.ceil(r))
            size = 2 * ri
            tile = Image.new("RGBA", (size, size), (0, 0, 0, 0))
            for y in range(size):
                for x in range(size):
                    sx, sy = cx - ri + x + 0.5, cy - ri + y + 0.5
                    if math.hypot(sx - cx, sy - cy) <= r + 0.15 and 0 <= int(sx) < im.width and 0 <= int(sy) < im.height:
                        tile.putpixel((x, y), im.getpixel((int(sx), int(sy))))
            name = "%s_w%d.png" % (vid, i)
            tile.save(OUT / name)
            rows.append({"cx": cx, "cy": cy, "r": r, "image": "Assets/Art/Catalog/Wheels/" + name, "size": size})
        data.append({"id": vid, "width": im.width, "height": im.height, "wheels": rows})
        if debug:
            debug.mkdir(parents=True, exist_ok=True)
            k = 12
            big = im.resize((im.width * k, im.height * k), Image.NEAREST)
            d = ImageDraw.Draw(big)
            for cx, cy, r in ws: d.ellipse([(cx - r) * k, (cy - r) * k, (cx + r) * k, (cy + r) * k], outline=(255, 0, 255, 255), width=3)
            big.save(debug / (vid + ".png"))
    (ROOT / "Assets/Art/Catalog/wheels.json").write_text(json.dumps({"vehicles": data}, indent=1))
    print("%d vehicles with wheels: %s" % (len(data), ", ".join("%s:%d" % (v["id"][8:], len(v["wheels"])) for v in data)))

main()
