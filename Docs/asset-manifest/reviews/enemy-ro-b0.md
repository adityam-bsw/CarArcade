# enemy-ro-b0 review (inspector, 2026-11-16)
Verdict: ready (quality 4), with the AI-origin exception and a user-owned licence check.
Checked: zip listing was PNG only; all 10 PNGs viewed before and after cleaning; originals unchanged in Docs/incoming-art/RO-B0/.
Issues found in the originals: faint noisy alpha haze (53k px on body, 210k on shield), glow clipped hard 40 px from the canvas edge,
dark RGB under partial alpha in glows, a stray cut-off bubble at the right edge of pose 04, poses not registered (body drift up to 48 px).
Fixes (tools/art/clean_rob0.py): alpha < 24 dropped, 28 px edge feather, specks < 80 px removed, glow fringe recoloured, 6 px colour bleed,
poses registered on cream shell centroid then one shared crop 956x958, pivot (0.6148,0.5471). Visual check on green background: no dark rim.
Notes: robot faces LEFT (flip in code); pose 06 art is slightly larger and shifted; shield is ~90% opaque inside (plan alpha in code).
Import: bilinear, no mipmaps, ASTC 5x5 Android via SmoothArtImporter; ppu 240 / 170 / 128.
