# vehicle-fire-truck (smooth restyle) - READY

Verdict: ready (smooth_status). Earlier sent back for touching the right canvas border.

Checked 2026-11-15:
- Margin (alpha > 0 including bleed): previews left 8 / right 8 / top 8 / bottom 12; bodies left 8 / right 8 / top 8 / bottom 26. Required >= 8 (1 old px at S=8). Pass.
- `tools/art/make_vehicles2.py --output <dir> --only vehicle-fire-truck`: preview, body, w0, w1 SHA-256 identical to staging. Pass.
- Canvas 368x184 (46x23 x 8), wheel anchors (72,144,28,64) and (240,144,28,64) unchanged; wheels 64x64.
- Visual 1:1: clean red cel-shaded truck facing right, ladder and light bar fully inside the canvas, nothing clipped.
- Import: previews/bodies/wheels under Assets/Art/Smooth/Vehicles2/, ppu 128 (16 x 8), bilinear, no mipmaps, Android ASTC 5x5. MAPPING.md and FORGE_NOTES.txt (REVISION note) updated.
- Ledger row for Vehicles2 updated (97 PNG), manifest smooth_status ready.
Quality 3. Linking into the game is done by the Link Smooth Vehicles menu.
