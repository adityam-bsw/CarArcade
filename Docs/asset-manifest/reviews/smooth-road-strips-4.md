# Review: smooth road strips batch 4 - 2026-11-16, asset-inspector
Verdict: ready for all 12 (coral, beach, rope_bridge are quality 3, imported with that note).
Checked independently: make_road4.py re-run reproduces all 12 byte for byte (cmp). The make_road.py OPAQUE_BODY change leaves defaults unchanged: asphalt (make_road.py) and acid, honey (make_road3.py) regenerated with default flags are byte-identical to the shipped files.
Opaque: check_roads_opaque.py on the 12: bottom row alpha 255, zero non-opaque pixels below lip+2, seam lip step <= interior max (coral 2 vs 13), RGB seam ratio <= 1.52. Strips and 3x previews viewed; seams clean.
Import: sprite, bilinear, no mips, 128 ppu, Repeat, Full Rect, ASTC 6x6 Android set explicitly; meta matches road_asphalt.
Quality notes: coral crest bumps small, beach shells faint, rope_bridge plain dark body; salt_flat/ice_bridge close to snow in luma. Existing 30 strips were NOT replaced (opaque regen stays in staging).
