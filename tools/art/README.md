# tools/art: procedural smooth vector art (own code, no generative AI, no downloads)

Implements Step 0 of `Docs/design/ART_BIBLE.md` section 10. Python 3.9 standard library only: neither Pillow nor numpy is
installed on this machine, so the rasteriser is our own (`smoothart/`). Do not `pip install` anything.

    python3 tools/art/make_road.py --output <dir> [--surface asphalt|dirt|grass|ice|mud|glass|all]   # 1024x192 strips (~25 s all)
    python3 tools/art/make_bg_countryside.py --output <dir>   # sky, sun, clouds, far, mid, near layers (~25 s)
    python3 tools/art/make_road2.py --output <dir>   # sand snow gravel cobblestone moon_dust neon_grid wooden_bridge rail_track (~30 s)
    python3 tools/art/make_bg_worlds.py --world desert|nightcity --output <dir>   # smooth Desert / Night City layers (~30 s each)
    python3 tools/art/make_bg_countryside_v2.py --output <dir>   # near layer v2 (fences, boulders, flowers) + light-trees alternate
    python3 tools/art/check_roads2.py <dir> [<old dir>]; python3 tools/art/check_bg_world.py <dir> <world>   # lip rows, greyscale pairs, hazard contrast
    python3 tools/art/check_roads.py <dir>; python3 tools/art/check_bg_contrast.py <dir>   # greyscale and contrast numbers

    python3 tools/art/make_vehicles.py --output <dir>          # slice cars: body_*.png + wheel_*.png (about 3 s)
    python3 tools/art/make_pickups_smooth.py --output <dir>    # coin, heart, star, ink canister, boost ring (+ overload) (about 2 s)
    python3 tools/art/make_vehicles2.py --output <dir> [--only id,id]   # 28 CATALOGUE vehicles: previews/ bodies/ wheels/ (canvas = old x S, same wheel anchors; about 25 s)
    python3 tools/art/vehicles2_report.py <dir>; python3 tools/art/review_vehicles2.py <dir> <out.png> [--ids a,b] [--old] [--grey] [--scene bg.png road.png]   # MAPPING.md, bounds check, review sheets
    python3 tools/art/assemble_preview.py <veh dir> <out>; python3 tools/art/check_sprite_contrast.py <bg> <road> <veh> <pick> <out>   # review only

Outputs go to a scratch directory first; copying into `Assets/Art/...` (and import settings, ledger row, manifest item) is a
separate, deliberate step.

## smoothart modules
- `palette`: `PAL` (Bible section 6 tokens), `SCENERY` (per-world ramps), `Ramp` multi-stop gradients, `lerp`, `smoothstep`.
- `raster`: `Mask` (4x supersampled 1-bit shapes: polygon, ellipse, rrect, capsule, polyline, `dilated` for the smooth
  outline, `minus`), `Cov` (coverage, `blur` for soft shadows and glows), `Image.paint` (source-over through coverage with a
  colour function), `fill_below_curve` (analytic hill silhouettes), `bleed` (4 px colour bleed, applied by `write_png`),
  `tile_seam_score` (about 1.0 means invisible seam), `write_png`.
- `noise`: `wave_fbm` (periodic 1D, seamless), `ValueNoise2D` (wraps in X).
- `shading`: `vgrad`, `dgrad`, `cel3` (three-tone soft cel shading along the light direction, upper left).

- `sprites`: `Sprite` (outline from the DISPLAY density: 0.04 world units, cel, rim, gloss, finish), `tones`, `round_poly` (fillet
  polygon), `erase_circle`, `circle_pts`.

## Conventions
- Seamless X: use periodic noise, `Mask(..., wrap_x=True)` (shapes are redrawn at +-width), and `blur(wrap_x=True)`.
- Outline for sprites: `edge = mask.dilated(2.5).minus(mask)`, painted DeepInk (0.04 unit = about 5 px at 128 ppu is a 2.5 px
  dilation each side if the stroke straddles; use 5 px for outside-only).
- Colour functions get pixel-centre (x, y) and return (r, g, b); alpha comes only from coverage, so no dither and no halos.
- Speed: pure Python, roughly 1 s per full-size paint of a 1 MP layer. Prefer `fill_below_curve` for big silhouettes and
  small-bbox Masks for sprites. A Mask is full canvas size (4x4 bytes per pixel), so keep sprite canvases tight.
