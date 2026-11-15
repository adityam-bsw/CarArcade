# Review: smooth catalogue vehicles (batch vehicles2) - 2026-11-15, asset-inspector
Verdict: 25 ready, 2 needs_work (snowmobile, snowboard: forge rated 2/5 placeholder; imported but NOT to be used), fire truck sent back (not imported).
Smooth status lives in `smooth_status` on each item; the old pixel `status` is unchanged so the game keeps working. Items vehicle-skateboard stays `forged` (old art unreviewed), smooth_status ready.
Checked myself:
- Re-ran `python3 -I tools/art/make_vehicles2.py --output <scratch>`: all 97 PNG (previews, bodies, wheels) identical (cmp) to the staging files.
- Canvas of previews and bodies = old Catalog/Previews canvas x integer S for all 28 (S 5 to 11). Wheel cx, cy, r, size in wheels_new.json = old Catalog/wheels.json x S for all 20 wheeled vehicles; wheel PNG sizes match. Pivot centre.
- Preview silhouette bounds vs old (alpha > 8, old px): within 1.25 for all except snowmobile 1.71 (the box is lower by the ski/track, within 1 to 2 old px). Bodies differ more only because wheels are not baked in.
- Alpha: transparent outside, soft AA edge, no black halo (0 black pixels among transparent pixels next to the art, all files). DEFECT: fire-truck preview and body have alpha 255 on the last pixel columns (touches right canvas border, silhouette 46.0 of 46 old px; others keep a margin). Sent back.
- Viewed sheet_color, scene_a, fire truck and snowmobile at 1:1. Facing right, separate wheels, style matches the slice cars. Snowmobile and snowboard are crude (confirmed forge rating).
Import: SmoothArtImporter gave 128 ppu (wrong for larger canvases) and ASTC 6x6 (the rule matches the folder "Vehicles/" only, not "Vehicles2/"). I set per file with script eval: ppu = 16 x S (112, 128, 144, 176, 80 ppu), Android ASTC 5x5 for all 93 files; bilinear, no mipmaps, clamp. Note: "old ppu / S" would shrink the world size; old 16 ppu x S is what keeps width/height in units equal (e.g. car 279 px at 144 ppu = 1.94 u, old 31 px at 16 = 1.94 u).
Follow-up for implementer: make SmoothArtImporter treat Assets/Art/Smooth/Vehicles2 as a vehicle folder (5x5) and honour a per-file ppu; read wheels_new.json; keep widthUnits = clamp(old W / 14, 1.5, 3) as in MAPPING.md.
Known pre-existing data issues (from forge notes): pickup is a semi cab, semi-truck a trailer with rear-only anchors.
