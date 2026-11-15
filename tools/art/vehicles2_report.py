#!/usr/bin/env python3
"""Checks and tables for the restyled catalogue vehicles. Writes MAPPING.md and wheels_new.json into <dir>. Standard library only.
    python3 tools/art/vehicles2_report.py <vehicles2 dir>
Bounds check: new silhouette box (alpha > 8) in OLD pixel units against the old preview's silhouette box.
"""
import json, os, re, sys
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.join(HERE, "..", "..")
sys.path.insert(0, os.path.join(ROOT, "tools", "assets")); sys.path.insert(0, HERE)
import pixel
from veh2lib import INV

def bbox(rows, thr=8):
    w = len(rows[0]); h = len(rows); x0 = y0 = 10**9; x1 = y1 = -1
    for y in range(h):
        for x in range(w):
            if rows[y][x][3] > thr:
                x0 = min(x0, x); x1 = max(x1, x); y0 = min(y0, y); y1 = max(y1, y)
    return x0, y0, x1 + 1, y1 + 1

def main():
    d = sys.argv[1]
    src = open(os.path.join(ROOT, "Assets/Scripts/Game/Editor/CatalogBuilder.cs")).read()
    costs = {a: (b, int(c)) for a, b, c in re.findall(r'\{ "(vehicle-[a-z-]+)", P\("(\w+)", (\d+)\)', src)}
    cat = {e["id"]: e for e in json.load(open(os.path.join(ROOT, "Assets/Art/Catalog/catalog.json")))["entries"]}
    lines = ["# Old file to new file, canvas, wheel anchors", "",
             "All new canvases are the old canvas times an integer S, so every anchor is old x S exactly. `widthUnits` in the game is",
             "`clamp(oldW / 14, 1.5, 3)`; it must be kept as the OLD value (the new files are bigger, so `preview.rect.width / 14` would clamp to 3 for most).",
             "Wheel cut-outs: `cx, cy, r` below are in NEW pixels (from the top-left of the new body/preview). Bounds = silhouette box in OLD px (alpha > 8): old vs new.", "",
             "| id | status, cost, class | old preview (WxH) | new canvas (WxH) | S | widthUnits | old bounds | new bounds (old px) | wheels old (cx,cy,r) -> new (cx,cy,r,size) | new files |",
             "|---|---|---|---|---|---|---|---|---|---|"]
    out = {"vehicles": []}
    for vid in sorted(INV):
        pth = os.path.join(d, "previews", vid + ".png")
        if not os.path.exists(pth): continue
        v = INV[vid]; S = v["S"]
        ow, oh, orows = pixel.read_png(os.path.join(ROOT, "Assets/Art/Catalog/Previews", vid + ".png"))
        ob = bbox(orows)
        nw, nh, nrows = pixel.read_png(pth)
        assert (nw, nh) == (v["W"] * S, v["H"] * S), vid
        nb = bbox(nrows); nbo = tuple(round(c / S, 2) for c in nb)
        cs = costs.get(vid); st = cat[vid]["status"]
        wo = "; ".join("(%g,%g,%g)" % (w["cx"], w["cy"], w["r"]) for w in v["wheels"]) or "none (invisible default wheels)"
        wn = "; ".join("(%g,%g,%g,%d)" % (w["cx"] * S, w["cy"] * S, w["r"] * S, w["size"] * S) for w in v["wheels"]) or "none"
        files = "previews/%s.png, bodies/%s.png%s" % (vid, vid, (", wheels/%s_w0..w%d.png" % (vid, len(v["wheels"]) - 1)) if v["wheels"] else "")
        lines.append("| %s | %s, %s | %dx%d | %dx%d | %d | %.4f | %d,%d-%d,%d | %.1f,%.1f-%.1f,%.1f | %s -> %s | %s |" % (
            vid, st, ("cost %d %s" % (cs[1], cs[0])) if cs else "not playable (no rig)", ow, oh, nw, nh, S, v["units"], ob[0], ob[1], ob[2], ob[3], nbo[0], nbo[1], nbo[2], nbo[3], wo, wn, files))
        if v["wheels"]:
            out["vehicles"].append({"id": vid, "width": nw, "height": nh, "wheels": [
                {"cx": w["cx"] * S, "cy": w["cy"] * S, "r": w["r"] * S, "image": "Assets/Art/Catalog/Wheels/%s_w%d.png" % (vid, i), "size": w["size"] * S}
                for i, w in enumerate(v["wheels"])]})
    open(os.path.join(d, "MAPPING.md"), "w").write("\n".join(lines) + "\n")
    json.dump(out, open(os.path.join(d, "wheels_new.json"), "w"), indent=1)
    print("\n".join(lines))
main()
