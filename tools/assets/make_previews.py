#!/usr/bin/env python3
"""Builds one small preview PNG per catalogue item into Assets/Art/Catalog/Previews/ and writes
Assets/Art/Catalog/catalog.json (the list the Unity catalogue builder reads). Standard library only.
Safe to re-run any time (after more assets are forged or approved). Previews are crops/composites of project art.

  python3 tools/assets/make_previews.py
"""
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "assets"))
import pixel  # noqa: E402

ITEMS = ROOT / "Docs" / "asset-manifest" / "items"
OUT = ROOT / "Assets" / "Art" / "Catalog"
PREV = OUT / "Previews"
A = "Assets/Art/"
KEN = A + "Vehicles/Kenney/Cars/"

# Manifest item -> source file for items that already exist as project art.
VEHICLE_SRC = {
    "vehicle-car": KEN + "sedan.png", "vehicle-sports-car": KEN + "sports_red.png", "vehicle-race-car": KEN + "formula.png",
    "vehicle-taxi": KEN + "taxi.png", "vehicle-police-car": KEN + "police.png", "vehicle-pickup": KEN + "truckcabin.png",
    "vehicle-semi-truck": KEN + "truck_trailer.png", "vehicle-bus": KEN + "bus.png", "vehicle-tractor": KEN + "tractor.png",
    "vehicle-fire-truck": KEN + "firetruck.png", "vehicle-camper": KEN + "van_large.png", "vehicle-dune-buggy": KEN + "buggy.png",
    "vehicle-scooter": KEN + "scooter.png", "vehicle-motorcycle": A + "Vehicles/Reactorcore/Full Bike.png",
    "vehicle-skateboard": A + "Vehicles/Skateboard/skateboard.png",
}
# tank: first 32x32 frame of an 11-frame strip
VEHICLE_FRAME = {"vehicle-tank": (A + "Vehicles/Tank/Tank_Standard.png", 0, 0, 32, 32)}

VISTA_WORLD = {
    "world-rolling-hills": "downs", "world-coast": "leeward", "world-desert": "mesa", "world-volcano": "kiln",
    "world-winter": "tundra", "world-arctic": "cairn", "world-jungle": "canopy", "world-night-city": "sodium",
    "world-industrial": "flue",
}

GROUPS = {
    "vehicle": {
        "Cars": ["car", "sports-car", "race-car", "taxi", "police-car", "dune-buggy", "golf-cart", "tuk-tuk"],
        "Trucks": ["pickup", "semi-truck", "bus", "tractor", "fire-truck", "camper", "monster-truck", "tank"],
        "Bikes": ["motorcycle", "scooter", "bicycle", "quad-atv", "skateboard"],
        "Water": ["jet-ski", "speedboat", "hovercraft"],
        "Snow": ["snowmobile", "sled", "toboggan", "snowboard"],
        "Space and rail": ["moon-rover", "rocket-sled", "handcar", "mine-cart"],
        "Fun": ["shopping-cart"],
    },
    "surface": {
        "Road": ["asphalt", "dirt", "gravel", "cobblestone", "rail-track", "mud", "neon-grid", "glass"],
        "Nature": ["grass-trail", "sand", "beach", "salt-flat", "moon-dust"],
        "Snow and ice": ["snow", "ice", "ice-bridge"],
        "Water and sky": ["sea-water", "river", "cloud", "floating-islands", "coral"],
        "Fire": ["lava-crust", "magma-bridge"],
    },
    "world": {
        "Seasons": ["spring-blossom", "summer", "autumn-leaves", "winter", "christmas"],
        "Nature": ["rolling-hills", "coast", "jungle", "farm", "desert", "canyon", "wild-west", "arctic"],
        "Space": ["moon", "mars", "space-station"],
        "City": ["night-city", "industrial", "synthwave"],
        "Fantasy": ["candy-land", "cloud-kingdom", "haunted-halloween", "cave", "volcano", "underwater"],
    },
}
FLAT_SURFACES = {  # colours of the original flat road skins: (top, body)
    "surface-asphalt": ((245, 245, 250), (56, 61, 71)),
    "surface-dirt": ((219, 173, 107), (140, 97, 56)),
    "surface-neon-grid": ((0, 255, 242), (31, 20, 61)),
}


def group_of(category, key):
    for g, keys in GROUPS[category].items():
        if key in keys:
            return g
    return "Other"


def strip_backdrop(rows):
    """Some forged previews were saved on a flat opaque backdrop. Remove that colour where it touches the border."""
    h, w = len(rows), len(rows[0])
    corners = {tuple(rows[0][0]), tuple(rows[0][w - 1]), tuple(rows[h - 1][0]), tuple(rows[h - 1][w - 1])}
    if len(corners) != 1:
        return rows
    bg = corners.pop()
    if bg[3] != 255:
        return rows
    seen = set()
    stack = [(x, y) for x in range(w) for y in (0, h - 1)] + [(x, y) for y in range(h) for x in (0, w - 1)]
    while stack:
        x, y = stack.pop()
        if (x, y) in seen or not (0 <= x < w and 0 <= y < h) or tuple(rows[y][x]) != bg:
            continue
        seen.add((x, y))
        stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    out = [list(r) for r in rows]
    for x, y in seen:
        out[y][x] = (0, 0, 0, 0)
    return out


def crop_to_bounds(rows, pad=1):
    rows = strip_backdrop(rows)
    b = pixel.bounds(rows)
    if not b:
        return rows
    x, y, w, h = b
    out = pixel.blank(w + pad * 2, h + pad * 2)
    pixel.paste(out, pixel.crop(rows, x, y, w, h), pad, pad)
    return out


def road_mock(strip):
    """A strip (top 2 rows, body 4, skirt fade) drawn as a short road on a dark sky-coloured backdrop (2:1)."""
    w = 128
    row = [strip[y] * ((w // len(strip[0])) + 1) for y in range(len(strip))]
    row = [r[:w] for r in row]
    canvas = pixel.blank(w, 64, (30, 38, 58, 255))
    for y in range(64):                                   # soft vertical backdrop gradient
        shade = int(8 * y / 64)
        for x in range(w):
            canvas[y][x] = (30 + shade, 38 + shade, 58 + shade, 255)
    pixel.paste(canvas, row, 0, 20)
    return canvas


def downscale2(rows):
    return [row[::2] for row in rows[::2]]


def world_composite(layer_paths):
    canvas = pixel.blank(384, 216, (0, 0, 0, 255))
    for p in layer_paths:
        w, h, rows = pixel.read_png(p)
        if (w, h) != (384, 216):
            return None
        pixel.paste(canvas, rows, 0, 0)
    return downscale2(canvas)


def build():
    PREV.mkdir(parents=True, exist_ok=True)
    entries = []
    skipped = []
    for f in sorted(ITEMS.glob("*.json")):
        it = json.loads(f.read_text(encoding="utf-8"))
        iid, cat, status = it["id"], it["category"], it["status"]
        key = iid.split("-", 1)[1]
        if status not in ("have", "ready", "forged"):
            skipped.append((iid, status))
            continue
        rows = None
        if cat == "vehicle":
            if iid in VEHICLE_SRC:
                rows = pixel.read_png(str(ROOT / VEHICLE_SRC[iid]))[2]
            elif iid in VEHICLE_FRAME:
                p, x, y, w, h = VEHICLE_FRAME[iid]
                rows = pixel.crop(pixel.read_png(str(ROOT / p))[2], x, y, w, h)
            else:
                for name in ("preview_assembled.png", "preview.png"):
                    p = ROOT / A / "Vehicles" / "Forged" / iid / name
                    if p.exists():
                        rows = pixel.read_png(str(p))[2]
                        break
            if rows:
                rows = crop_to_bounds(rows)
        elif cat == "surface":
            p = ROOT / A / "Surfaces" / "Forged" / iid / f"{iid}.png"
            if p.exists():
                w, h, strip = pixel.read_png(str(p))
                rows = road_mock(strip)
            elif iid in FLAT_SURFACES:
                top, body = FLAT_SURFACES[iid]
                strip = pixel.blank(64, 26, (0, 0, 0, 0))
                for y in range(26):
                    for x in range(64):
                        strip[y][x] = (*top, 255) if y < 2 else ((*body, 255) if y < 6 else (*body, max(0, 255 - (y - 6) * 14)))
                rows = road_mock(strip)
            elif iid == "surface-sea-water":
                _, _, s = pixel.read_png(str(ROOT / A / "Surfaces/Water/Ocean_SpriteSheet.png"))
                rows = [r[:32] * 2 for r in s[:32]]
            elif iid == "surface-lava-crust":
                _, _, s = pixel.read_png(str(ROOT / A / "Surfaces/Lava/lava_strip8.png"))
                rows = [r[:16] * 4 for r in s[:32]]
        elif cat == "world":
            if iid in VISTA_WORLD:
                s = VISTA_WORLD[iid]
                rows = world_composite([str(ROOT / A / f"Backgrounds/Vista/{s}/{s}_{i}_{n}.png")
                                        for i, n in enumerate(("sky", "far", "mid", "near"))])
            else:
                d = ROOT / A / "Worlds" / "Forged" / iid
                names = [d / f"{key}_{i}_{n}.png" for i, n in enumerate(("sky", "far", "mid", "near"))]
                if all(p.exists() for p in names):
                    rows = world_composite([str(p) for p in names])
            if iid == "world-underwater":
                bg = pixel.read_png(str(ROOT / A / "Worlds/Underwater/background.png"))[2]
                mg = pixel.read_png(str(ROOT / A / "Worlds/Underwater/midground.png"))[2]
                canvas = pixel.blank(384, 216, (14, 36, 56, 255))
                pixel.paste(canvas, [r[:384] for r in bg[:216]], 0, 0)
                pixel.paste(canvas, [r[:384] for r in mg[40:256]], 0, 0)
                rows = downscale2(canvas)
        if not rows:
            skipped.append((iid, f"{status} but no preview source"))
            continue
        pixel.write_png(str(PREV / f"{iid}.png"), len(rows[0]), len(rows), rows)
        entries.append({
            "id": iid, "kind": cat, "name": it["name"], "group": group_of(cat, key), "status": status,
            "quality": it.get("quality") or 0, "note": (it.get("notes") or "")[:160],
            "licence": it.get("licence") or "see ledger",
            "preview": f"Assets/Art/Catalog/Previews/{iid}.png",
        })
    order = {cat: list(groups) for cat, groups in GROUPS.items()}
    kind_order = {"vehicle": 0, "surface": 1, "world": 2}
    entries.sort(key=lambda e: (kind_order[e["kind"]],
                                order[e["kind"]].index(e["group"]) if e["group"] in order[e["kind"]] else 99,
                                e["name"]))
    (OUT / "catalog.json").write_text(json.dumps({"entries": entries}, indent=2) + "\n", encoding="utf-8")
    print(f"{len(entries)} previews written; skipped {len(skipped)}")
    for s in skipped:
        print("  skipped", s[0], "-", s[1])


if __name__ == "__main__":
    build()
