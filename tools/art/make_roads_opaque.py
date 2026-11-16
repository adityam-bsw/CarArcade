#!/usr/bin/env python3
"""Regenerate existing road strips with an OPAQUE body (QA7-06 rule). Same names and lip rows as the originals.
    python3 tools/art/make_roads_opaque.py --output <dir> [--surface a,b|all]
Sets make_road.OPAQUE_BODY (skirt_fade and the mud/ice skirt multiply_alpha become no-ops) then solidify() fills holes under the lip."""
import argparse, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_road, make_road2, make_road3
from smoothart import Image, write_png, tile_seam_score
make_road.OPAQUE_BODY = True
Image.multiply_alpha = lambda self, fn: self
SRC = {}
SRC.update({k: v for k, v in make_road.SURFACES.items()}); SRC.update(make_road2.SURFACES); SRC.update(make_road3.SURFACES)
if __name__ == "__main__":
    ap = argparse.ArgumentParser(); ap.add_argument("--output", required=True); ap.add_argument("--surface", default="all"); a = ap.parse_args()
    os.makedirs(a.output, exist_ok=True)
    for n in (SRC if a.surface == "all" else a.surface.split(",")):
        im = make_road.solidify(SRC[n]()); write_png(im, os.path.join(a.output, "road_%s_1024x192.png" % n)); print(n, round(tile_seam_score(im), 3), flush=True)
        if n == "glass":
            for st in (1, 2, 3):
                ov = make_road.crack_overlay(st, im); write_png(ov, os.path.join(a.output, "road_glass_crack%d_1024x192.png" % st))
