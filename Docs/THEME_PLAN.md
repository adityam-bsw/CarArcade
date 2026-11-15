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
**Art style switch (smooth conversion, step 0).** `ArtStyle { Pixel = 0, Smooth }` (`Core/ArtStyle.cs`) is a field on
`BackgroundSkin`, `RoadSkin` and (informational) `UITheme.filterStyle`; default Pixel, so existing assets render unchanged.
`BackgroundDirector.Layout` snaps layers to the art-pixel grid only when the recipe's style is Pixel (recipe style comes from
its base skin); Smooth scrolls in float. A Smooth `RoadSkin` forces its atlas bilinear / repeat U / clamp V when the material
is built; UVs were already world-unit based (`texelWorld`, set 1/128 for smooth skins). `SmoothArtImporter` (Core/Editor)
applies the smooth import table (bilinear, no mips, 128 ppu, 100 under UI/, ~41 under Backgrounds/, ASTC 6x6 / 5x5 Android)
to NEW textures under `Assets/Art/Smooth/` only; `SmoothArt` holds the shared constants. Checks: `SmoothArtImportTests`.
**Smooth Countryside (wired 2026-11-15).** `ThemeArtStyles` (`Core/Background`, asset `Resources/Themes/ThemeArtStyles`) is the single
art-style switch per theme: Country, Desert and CyberNight = Smooth (see Smooth batch 2). `BackgroundRemixer.Pool(mood, style)` only offers skins of
the theme's style, so a remix never mixes styles; `ThemeArtStyles.Override(theme, style)` flips a theme back to Pixel for a session.
Smooth skin: `Resources/Backgrounds/countryside_smooth.asset` (sky, far, mid, near, scroll 0 / 0.2 / 0.4 / 0.8, plus `sun`, 4 `clouds`,
`cloudScroll` 0.1, `groundFill`). `BackgroundDirector` shows smooth layers at scale viewH/14, bottom-aligned (the 864 px near layer is 1.5x
the view), and draws sun and clouds as extra renderers between sky and far. Roads: `Resources/Roads/Smooth/{surface-id}.asset`
(asphalt, dirt, grass-trail, ice, mud, glass), `RoadSkins.GetSmooth(id)`. `RoadSkin.lipPx` is the strip row of the driving surface
(asphalt 4, dirt 7, ice 6, glass 8, mud 8, grass 14); `RoadRenderer` maps a smooth strip 1:1 in world units with that row on the collider
top, so grass sits lower in its strip than asphalt and its blades rise over the collider. `RoadGameManager._worldArt` follows the world
on screen; `ApplySurface` uses the smooth strip only when the world is Smooth AND that surface has one (otherwise its pixel strip stays).
Built by menu `Road Builder > Smooth > Build Countryside Skins` (`SmoothSkinFactory`, re-runnable). Glass crack overlays 1-3 are imported
but NOT wired: no glass break state exists in code yet (glass is only a stage-event definition and a surface), so there is nothing to hook.
Tests: `SmoothArtImportTests` (switch, no style mixing, lip alignment, 1:1 mapping).
**Smooth batch 2 (wired 2026-11-15).** All three worlds are Smooth in `ThemeArtStyles` (Country, Desert, CyberNight); pixel skins and
`ThemeArtStyles.Override(theme, ArtStyle.Pixel)` still work per theme. New background skins: `Resources/Backgrounds/desert_smooth.asset`
(Mood Warm, sun) and `nightcity_smooth.asset` (Mood Dark, moon in the `sun` slot, stars in the new `BackgroundSkin.overlay`; scroll 0 / 0.2 / 0.4 / 0.8;
`groundFill` sampled from each near layer). `BackgroundDirector` draws the overlay (tiled, scroll 0, sorting -49) between sky (-50) and sun/moon (-48);
clouds -47. Roads: 14 smooth strips now (added sand 7, snow 8, gravel 4, cobblestone 4, moon-dust 10, neon-grid 4, wooden-bridge 5, rail-track 2 lip px).
Countryside near layer: `near` = near_v2 (default); `BackgroundSkin.nearAlternates` = {v1, v2_light}, picked per session with
`BackgroundRemixer.SetNearVariant(-1|0|1)` (no UI yet).
**Smooth road fallback (display only).** `RoadGameManager.SmoothDisplaySkin(id)`: in a Smooth world a surface with no smooth strip is drawn with the
world's default smooth road (`ThemeArtStyles.countryRoad/desertRoad/cyberNightRoad` = asphalt / sand / neon-grid) instead of its pixel strip, and one
`[Road]` log line is written per surface id. The selected surface id, friction, bounce, speed/ink/coin factors are untouched (`SetSurfaceTuning` still reads the
selected surface). Surfaces without a smooth strip: beach, salt-flat, cloud, coral, floating-islands, river, sea-water, lava-crust, magma-bridge, candy,
rope-bridge, ice-bridge. Menu `Road Builder > Smooth > Build Desert and Night City Skins` (same factory, builds everything).
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

## Current logic: slice vehicles and pickups by art style (built 2026-11-15)
- Data: `Resources/Smooth/SmoothSliceArt.asset` (class `SmoothSliceArt`, Core) references, without copies, `Assets/Art/Smooth/Vehicles` (body and own wheel for Classic, Sport, Monster) and `Assets/Art/Smooth/Pickups` (coin, heart, ink canister, ring; also ring overload and star, unused). Built by menu Road Builder > Smooth > Build Countryside Skins (`SmoothSkinFactory.BuildSliceArt`), safe to re-run.
- Switch: `RoadGameManager._worldArt` (from `ThemeArtStyles` or the picked world skin) is passed to `RoadCar.Create(pos, VehicleType, ArtStyle)` and set on `PickupSpawner.Art` at run start. Pixel (default) keeps `Resources/RoadBuilder/*` exactly as before.
- `RoadCar.Build(VehicleType)`: Smooth takes the smooth body and the vehicle's own wheel; any missing entry falls back to the pixel body and the shared `RoadBuilder/Wheel`. Body and wheel are still fitted to the same boxes (2.4x0.9, 2.4x0.8, 2.6x1.1; wheel 2*r*1.03); colliders, mass and torque are code data and do not read the sprite. `PickupSpawner.Icon` does the same per name; `Collectible.Init` still scales every sprite to its fixed world size and uses the fixed 0.5 trigger.
- Not wired on purpose: `Pickup_Ring_Overload` (no Overload state or consumer exists in code; `RunDirector` handles only `Collectible.Type.Ring`); `Star` (ParticleManager tints the star per burst colour, a coloured outlined sprite would turn muddy; needs a white variant); gems, shield, magnet, double, slow-mo, ghost (no smooth art).
- Tests: `SmoothSliceArtTests` (asset complete, same collider points, circle radii, mass, torque, sprite bounds for pixel vs smooth cars; own wheel per vehicle; pickup bounds and trigger radius).
- Known mixing (left as is): catalogue vehicles with smooth art now follow the world (see the next section; the others stay pixel); the slice cars are only used when `_selVehicleId` is empty (legacy `CycleVehicle`, no UI calls it) so the default Countryside run still shows a pixel car; HUD/UI plates are smooth but the workshop backdrop lamps and clock, and some HUD hearts, remain pixel; showcase car is not rebuilt when the world changes.

## Current logic: smooth catalogue vehicles (built 2026-11-15)
- Art: `Assets/Art/Smooth/Vehicles2/{previews,bodies,wheels}` (canvas = old preview x integer S, ppu = 16 x S per file, so world size is unchanged). `MAPPING.md` lists S and wheel anchors in new pixels (`wheels_new.json` was never shipped; the numbers are old `Catalog/wheels.json` x S, checked against MAPPING.md by a test).
- Data: `CatalogEntry.smoothPreview / smoothBody / smoothWheels / smoothScale` (additive; old fields untouched). Filled by menu `Road Builder > Catalog > Link Smooth Vehicles` (`SmoothVehicleLinker`, also run by Build Catalog): only items whose manifest `smooth_status` is `ready` and whose files exist. 26 linked (fire truck imported later and linked by re-running the menu); snowmobile and snowboard (needs_work) stay old.
- Switch: `CatalogEntry.MakeRig(ArtStyle)` (used by `RoadGameManager.CreateSelectedCar`): Smooth world + smooth art ready -> smooth body and smooth wheels, else old preview and old wheels. `widthUnits` stays the old `clamp(oldW/14, 1.5, 3)`, mass, torque, speed unchanged; wheel anchors and radius are old x S, so world units are identical; colliders derive only from width and aspect. Cards (`CollectionCard`, `CollectionScreen` detail) use `PreviewFor(ThemeArtStyles.For(Model.World))`.
- Import: `SmoothArt.IsVehicleFolder` (Vehicles, Vehicles2) -> ASTC 5x5; the importer applies defaults only on first import, so per-file 16 x S ppu is kept.
- Tests: `SmoothVehicleTests` (ppu = 16 x S, bilinear, ASTC 5x5 for every Vehicles2 texture; only ready vehicles linked; widthUnits; wheels vs MAPPING.md; pixel vs smooth body box, wheel positions, radii, collider points, joint anchors, mass, torque for every linked vehicle; expected count comes from the manifest, not a constant).
- Known: a showcase car already on screen is not rebuilt when the world style changes (rebuilds on the next vehicle pick or run start).
