# Review: smooth-vehicles-slice, smooth-pickups - 2026-11-15, asset-inspector
Verdict: ready for both (style approved by the user; Classic stays cherry red #D8323C by design).
Checked: tools/art/make_vehicles.py and make_pickups_smooth.py re-run reproduce all 12 PNGs byte for byte (cmp). Canvases equal the old
Resources sprites (bodies 480x180/160/200, wheels 160x160, coin/heart/ink 128 square); Star is 128 (old 64) so it is imported at 200 ppu
(0.64 units); ring is 256 square (old 96), scaled by bounds in Collectible.Init so no change. Pivot centre on all. Silhouette bboxes sit
within the old ones. Alpha: fully transparent outside, soft AA edge, no pixel touches the canvas border, transparent pixels carry
bled colour (no dark halos). Viewed the assembled review pictures and the pickup sheet: facing right, readable, separate wheels.
Import: bilinear, no mips, 128 ppu (Star 200), Android ASTC 5x5 vehicles via SmoothArtImporter. Rings bilinear.
Notes: game loads one shared Wheel; per-vehicle wheels need a RoadCar change (implementer); until then wheel_classic fits all.
Heart outline contrast at road height is weak (2.96:1). Ratings 3.5 to 4. Not wired; Resources sprites untouched.
