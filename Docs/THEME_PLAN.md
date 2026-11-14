# Theme plan: real assets, endless scroll

Status: PROPOSED, nothing downloaded or built yet. Replaces the earlier "pure procedural" plan: themes now
come from **downloaded free art**, and code only arranges it so the world never ends.
Licences below were read on the creators' own pages on 2026-11-14; re-check before shipping.

## Status (updated)
| Step | State |
|---|---|
| 0 Get assets: VISTA imported, CC0 verified, logged in the ledger | done |
| 1 Look test | done in-game: pixel-snapped layers behind the real camera and road |
| 2 Background director: remix rules, wrap, region schedule, cross-fade, tints | done, see "Current logic: background" |
| 3a Road: collider/renderer split, `RoadSkin`, textured renderer, independence tests | done, 4 tests pass, flat skins look unchanged |
| 3b Road: pixel atlases baked from ambientCG textures | waiting on download approval |
| 4 Cars: pixel vehicles | not started |
| 5 Hardening: device test, memory | not started |

## 1. Direction
- **Retro pixel look.** The best verified free source (VISTA, below) is 384x216 pixel art, so the whole world
  adopts one pixel density (about 5 screen pixels per art pixel at 1080p). Cars, pickups and road must match.
- **Three independent layers** (unchanged from before): Background, Road look, Game logic.
  Game logic needs only the road's shape; the background needs only the camera; road look needs only the road
  shape. Any background can pair with any road skin and any mode.
- **Never-ending feeling** comes from arrangement, not from endless art: seamless looping layers, scenery that
  changes with distance, per-run variety, and a road texture that follows the drawn line forever.

## 2. Asset shortlist

### Backgrounds: VISTA by najjar320 (recommended)
| Item | Detail |
|---|---|
| Source | https://najjar320.itch.io/vista-parallax-backgrounds |
| Licence | CC0: free for commercial use, no credit needed. Only restriction stated: no reselling it as an asset pack |
| Size | `vista-ten-1.0.0.zip`, 1.1 MB |
| Content | 10 scenes x 4 layers (sky, far, mid, near), 384x216, PNG with alpha, `.gpl`/`.hex` palettes, one `.json` per scene |
| Scenes | night city (sodium), alpine (cairn), misty valley (hollow), desert badlands (mesa), rainforest (canopy), industrial (flue), volcanic (kiln), rolling hills (downs), coastal cliffs (leeward), frozen tundra (tundra) |
| Looping | Every layer tiles horizontally ("the right edge is its left edge") |
| Scroll speeds | Same in all scenes: sky 0.00, far 0.20, mid 0.40, near 0.80 (in the JSON) |
| How it was made | Script-generated, not hand-painted: fractal-noise terrain and a physical haze model. The page states no generative AI was used and ships the Python generator |
One pack covers all the themes we planned (countryside, desert, night city) plus six more for later.

### Backgrounds: alternatives (all verified on creator pages)
| Pack | Licence | Notes |
|---|---|---|
| Dark Forest, val-demar (itch.io) | CC0 1.0 | 5 seamless layers, 97 kB, "no generative AI", forest only |
| Forest / Industrial / Warped Ocean parallax, ansimuz (OpenGameArt) | CC0 on the pages seen | OpenGameArt blocked a direct fetch, so re-check on the page |
| Kenney Background Elements Redux | CC0, 90 files | Loose objects (trees, hills, clouds), flat cartoon style: does not match pixel look |
| Seamless SimpliCity, beedly10 | CC BY 4.0 | Day and night city, needs a credit line. Fallback only |

### Road: no ready-made side-view hill road found
Free road art online is almost all top-down or isometric. Plan: build the road look from **real CC0 material
textures** and convert them to our palette at import time (an editor tool, not hand-drawn art):
| Source | Licence | Use |
|---|---|---|
| ambientCG (asphalt, asphalt damage, road lines, gravel categories) | CC0 1.0, credit optional, raw files may go in a game | Top surface, body slab, lane markings, wear, gravel shoulder |
| Kenney Pixel Platformer (18x18 tiles, 200 files) | CC0 | Possible edge and prop pieces. The page does not list terrain tiles, so unconfirmed |
Process: downsample with nearest-neighbour to about 64x64, map colours to the VISTA palette (`.gpl` is included),
optional ordered dither. Output is tileable and matches the backgrounds. Exact ambientCG materials get chosen
after a visual look on their site (their listing did not show IDs to fetch).

### Cars and pickups
Current art is a high-res placeholder and will clash with a pixel world. A pixel vehicle pack is needed
(Kenney has pixel vehicle packs; licence and fit not yet checked). Planned right after the look is locked.

## 3. How it feels endless
1. **Seamless layer wrap.** Each of the 4 layers is drawn as 2 or 3 tiles that are re-placed as the camera
   moves. Layer x = camera x scaled by the layer's scroll factor, wrapped by the layer width. Y is anchored in
   world space (as today) so falls do not whip the backdrop.
2. **Scenery that changes with distance.** A schedule picks the next scene every N metres (for example hills,
   then mesa, then night city). Layers cross-fade over about 150 m. All VISTA scenes share the same 4-layer
   structure, so any scene blends with any other.
3. **Per-run variety.** Random start scene, random horizontal offset, optional mirrored layers (a mirrored
   seamless layer still loops), time-of-day tint (dawn, day, dusk, night) applied as a palette-friendly colour
   multiply. 10 scenes x flips x 4 tints gives hundreds of distinct runs from one 1 MB pack.
4. **Road texture follows the line.** The ribbon mesh uses UVs by arc length with repeat wrapping, so asphalt,
   markings and gravel never stretch on slopes and never run out.
5. **Props on a rule.** Poles, signs and rails spawned ahead of the car from a small sprite set with jittered
   spacing, pooled and recycled behind.
6. **Pixel snapping.** Layer positions snap to the art-pixel grid so nothing shimmers while the camera moves.

## 4. Code contract (as before)
- `RoadBuilder` keeps geometry and raises `SegmentBuilt(points)`. `RoadChunk` splits into `RoadCollider`
  (physics) and `RoadRenderer` (look, driven by a `RoadSkin`).
- `BackgroundSkin` (ScriptableObject): scene name, 4 layer sprites, scroll factors, palette, tint set.
  `BackgroundDirector` replaces `ParallaxBackground`.
- `ThemeDefinition` pairs a BackgroundSkin and a RoadSkin (and optional physics profile).
- Independence tests: same stroke gives identical collider points under every RoadSkin; swapping a
  BackgroundSkin changes no gameplay value.

## 5. Build order
| Step | Deliverable | Done when |
|---|---|---|
| 0 Get assets | VISTA zip imported (palettes, JSON, 40 layers), logged in `ASSET_LEDGER.md` | Files in the project, licences recorded |
| 1 Look test | One scene scrolling in a test scene at 5x, pixel-snapped, on the real camera | You approve the look on a phone-sized view |
| 2 Director | `BackgroundDirector` + `BackgroundSkin`, wrap, scene schedule, cross-fade, tints | A 5 km run shows 3+ scenes with no seams or pops |
| 3 Road | Collider/renderer split, converted textures, arc-length UVs, markings, edge props | Asphalt and dirt look right on slopes and curves |
| 4 Cars | Pixel vehicle pack chosen and integrated | Car, road and world share one pixel density |
| 5 Hardening | Memory, draw calls, low-end phone test | 60 fps, no per-frame allocations |

## 6. Risks
- **Style lock-in.** VISTA's 384x216 pixel art commits the game to a pixel look; the car and pickups must follow.
- **Generated, not painted.** The pack is uniform and clean by design. It reads cohesive, but it is not hand art.
- **Road conversion quality.** Photo textures reduced to a palette can look noisy; needs an eye test per material.
- **Seams** at the wrap point if scale or pixel snapping is off: test early (step 1).
- **Licences drift.** Re-read each page when downloading and keep a copy of the licence text in the project.

## Current logic: background (built)
Files: `Assets/Scripts/Core/Background/` (`BackgroundSkin`, `BackgroundRemixer`, `BackgroundDirector`, `EnvironmentTheme`),
`Assets/Scripts/Core/Editor/BackgroundSkinFactory.cs`, skins in `Assets/Resources/Backgrounds/*.asset`, art in
`Assets/Art/Backgrounds/Vista/`. `BackgroundDirector` replaced `ParallaxBackground` (same GUID, same `Init`,
`ApplyTheme`, `Follow` calls, so the manager and scene did not change).
- **Skins:** one per VISTA scene (sky, far, mid, near, scroll factors read from the scene JSON) plus a mood:
  Green (downs, canopy, leeward), Warm (mesa, kiln), Cool (cairn, hollow, tundra), Dark (sodium, flue).
  Worlds map to moods: Country = Green, Desert = Warm, Night City = Dark. Cool is unused until Arctic/Moon exist.
- **Remix rules** (`BackgroundRemixer.Make`): the base scene supplies sky and far; mid and near come from any
  scene of the same mood about half the time; each layer is mirrored with 40 to 50 % chance and shifted by a random
  offset; a time-of-day tint (day, dawn, dusk) is multiplied on; Dark worlds never get dawn. A region never
  repeats the previous base scene.
- **Variety measured** over 2,000 recipes per world (offsets not counted): Country 754 distinct looks, Desert 346,
  Night City 232, zero broken recipes, zero consecutive repeats.
- **Endless scroll:** each layer is a tiled sprite wider than the view, placed at `camera x * (1 - scroll)` and
  moved in whole-tile steps, so the loop is seamless. Layer height equals the view height, positions snap to the
  art-pixel grid. Vertical position lags the camera slightly per layer so falls do not drag the backdrop; a
  ground-coloured fill covers the gap below if the camera dips.
- **Changing scenery:** every 450 to 800 m the director builds a new remix and cross-fades over 160 m. The sky of
  the outgoing scene stays opaque during the fade so no gap shows. Menus never trigger a change (camera is still).
- **Per-run variety:** `ApplyTheme` (world pick and every run start) seeds from the clock, so every run differs.
- **Derived art:** `downs_0_sky_clean.png` is the Downs sky with the dome clouds removed (cleanest 8 px strip
  tiled across); `leeward_0_sky_clean.png` is a copy of it. Originals untouched. CC0 allows this.
- **Cost:** 10 sprite renderers; 40 uncompressed 384x216 textures resident (about 13 MB), to be reduced if needed.

### Not yet verified
- On a real phone: frame time, memory, and how the pixel scale looks at resolutions other than 1080p (one art
  pixel is exactly 5 screen pixels only at 1080 tall; elsewhere pixels are slightly uneven).
- Long-run behaviour over several region changes in real driving (tested by moving the camera, one transition).
- The car, pickups and road still look high-resolution next to the pixel backdrop (steps 3 and 4).

## Current logic: road (split built, textures pending)
Files: `Assets/Scripts/Core/Road/` (`RoadGeometry`, `RoadSkin` + `RoadSkins`, `RoadRenderer`), `RoadChunk.cs` (geometry + collider
only), `Assets/Shaders/RoadTextured.shader`, `Assets/Resources/Roads/*.asset`, `Assets/Resources/RoadBuilder/RoadTextured.mat`,
`Assets/Scripts/Core/Editor/RoadSkinFactory.cs`, tests in `Assets/Tests/EditMode/RoadSkinIndependenceTests.cs`.
- `RoadBuilder` still owns the drawing rules and hands points to `RoadChunk`. `RoadChunk.Build` computes `RoadGeometry`
  (spine, smoothed normals, distance along the line), sets the EdgeCollider2D from it, and only then asks the
  `RoadRenderer` to draw it with the current `RoadSkin`. The collider code never reads the skin.
- A `RoadSkin` has two modes. Flat: the original four colours (all three skins use this today, so the look is unchanged).
  Textured: a pixel atlas (rows from the top: curb, body, skirt) mapped by distance along the road (u) and cross-section (v),
  point filtered, repeating along the road. `texelWorld` (default 14/216) matches the background art pixel size.
- `RoadSkins.Get(style)` loads `Resources/Roads/<style>`, or falls back to the original colours if the asset is missing.
- `RoadStyle` enum values are unchanged, so saves still load.
- Proven by tests: identical collider points under a flat, two textured and a tinted skin; collider sits on the driving
  surface; physics material unchanged; atlas u grows with distance and v spans the atlas.
- Next: bake atlases from CC0 ambientCG textures (Road007, Asphalt033, Ground109, Gravel043), assign them to the skins, tune.
