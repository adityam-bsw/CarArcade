# Art Bible: Smooth Arcade Workshop (clean high-res 2D cartoon)

Owner: art-director. Status: PROPOSED, nothing in sections 7 and 8 is converted yet. Date: 2026-11-15.
Supersedes the pixel-art bible (kept for reference in `Docs/design/ART_BIBLE_PIXEL_ARCHIVE.md`). Trigger: the user judged the game "too much pixel" and asked for a less pixel look. Read with `Docs/ASSET_AGENT_RULES.md` (its pixel-only clauses are superseded by section 6 here; the rules agent owner must update it), `Docs/THEME_PLAN.md` (pixel direction superseded, build logic stays), `Docs/design/VERTICAL_SLICE_BRIEF.md` (gameplay numbers win).
Rule of this file: it states the look. The asset-inspector still owns legal and technical `ready` status.

## 0. Direction lock
- **Smooth, clean, high-resolution 2D cartoon arcade.** Vector-clean shapes, smooth outlines, soft gradients, bilinear filtering, no visible pixel grid anywhere (world, UI, icons, fonts).
- **Workshop identity stays.** Charcoal and orange plates, hazard stripes, rivets, tape, a friendly robot mechanic. Only the rendering changes: from pixel to smooth. The UI plates (already drawn at 3x and filtered bilinear) are the nearest thing to the target and are the style anchor together with the smooth vehicle sprites (`CarBody.png`, `SportCarBody.png`, `MonsterTruckBody.png`, `Wheel.png`).
- **No generative AI output**, ever. Allowed: CC0 packs (prefer Kenney vector and smooth packs, OpenGameArt, itch CC0), recolour, mirror, crop, composite, and our own procedural vector art (Python/SVG rasterised at 2 to 4 times supersampling, scripts committed under `tools/art/`). Never call procedural results hand-drawn.
- **Gameplay is untouched.** Hit shapes, colliders, timings and data stay as in the brief and ScriptableObjects. Art swaps are data-only (skin assets, sprite references).
- Pixel art stays only as an optional retro `ArtStyle` later, not now. If wanted, it is an extra option behind one presentation interface. Existing pixel files are not deleted by this plan.

## 1. Why it reads as pixel today (audit evidence, files read 2026-11-15)
Seen in `Docs/UI/home_workshop.png` and the asset files; the Oct 28 gameplay screenshots in `Assets/Screenshots` are from before the pixel switch (smooth hills, smooth car) and show the target feel for the world: soft gradients, big cartoon clouds, thick outlined car.
| Cause | Where | Evidence |
|---|---|---|
| Point filter | All 42 VISTA layers, 23 forged strips, 57 Kenney vehicles, 59 forged vehicle files, 60 forged world layers, 27 of 66 Workshop UI files, 7 `Resources/RoadBuilder` pickups | `filterMode: 0` in `.meta` |
| Low PPU / tiny art | Backgrounds 384x216 at 16 ppu (about 5x magnification on a 1080p phone); Kenney cars 38 px long; forged strips 128x26; forged cars 33 to 48 px | PNG headers |
| Dithered, noisy, palette-limited look | VISTA layers (dither bands, noise terrain), forged strips (Bayer dither), CityNight4Col (4 colours) | viewed `mesa_2_mid.png` |
| Pixel garage backdrop and props | `Docs/UI/home_workshop.png`: pegboard tools, robot, tyres, barrel, lamp, tape corners show stair-stepped edges; the garage door view is a point-filtered 5x pixel scene | viewed |
| Pixel snapping in code | `BackgroundDirector` snaps layers to the art-pixel grid; `RoadSkin.texelWorld = 14/216` and point-filtered road atlas | `Docs/THEME_PLAN.md` |
| Hi-res photo-ish pixelation | `OpenGameArt/motorcycle.png` is 968x429 but is a blown-up 48 px image with big visible blocks; point filtered | viewed |
Already smooth and kept: Workshop UI plates and buttons (39 bilinear files), fonts Lilita One and Baloo 2 (outline-based TMP SDF), the four slice vehicle sprites (but see 3.3: outline weight must be reduced and unified), Volcano pack (vector, filter 1), `ui_*` generated shapes (bilinear).

## 2. Style rules
### 2.1 Camera, view, light
- Unchanged: side view, orthographic, `orthographicSize` 7 (14 units tall), everything faces right, light from the upper left about 45 degrees, no tilt. Layering order unchanged: sky, far, mid, near, road skirt and body, road top lip, props, road hazards, car and wheels, boss and projectiles, particles, tell markers (always on top), HUD.
- Light is expressed as soft gradients: lighter top-left, a 2 to 3 step shade bottom-right, a gentle rim highlight on the upper left edge of every object. Cast shadow: a soft blurred ellipse (50 percent ink alpha, 0.4 unit blur) below floating objects only; ground objects get none. Night and dusk tint by multiply only.
- No grain, no dither, no noise texture on flat colour. Texture is allowed only as large soft shapes (see road, 4).

### 2.2 Density and sizes (authoring and display)
- **World gameplay sprites are authored at 128 px per world unit** (2x the phone screen density at 1080p, which is about 77 px per unit). Rule: a sprite is never magnified on any target phone (at most 1440p, about 103 px per unit), so 128 covers it. Existing 480 px vehicles at 100 ppu stay valid (about 100 px per unit, display-equivalent). Never mix densities inside one sprite, never scale by an arbitrary factor to fit: change the art or the data.
- **Backgrounds** (soft shapes tolerate magnification up to 2x): sky 4 x 576 gradient strip (or 2 x 1024 with cloud sprites), far and mid layers 2048 x 576 seamless, near layer 2048 x 864 seamless. 576 px covers the 14-unit view at about 41 px per unit; do not push below 36.
- **Road strips**: authored at 128 px per unit, 1024 px wide tileable segment (8 units), 192 px tall (top lip, body, skirt), tileable in X (seam score under 0.02 on the smooth content).
- **UI**: authored at 3x the layout size, imported bilinear, 9-sliced where a plate stretches (existing practice).
- **Sizes in section 8 are in units**, resolution follows from 128 px per unit. The pixel bible's sizes divided by 16 give the same units.

### 2.3 Outline
- **One outline family, smooth, 0.04 world units (about 5 px at 128 ppu, about 3 screen px at 1080p) on every world sprite**, DeepInk `#18202A`, rounded joins, anti-aliased at supersampling. Not pure black.
- Current slice vehicles have 12 px outlines at 100 ppu (0.12 units); that is about 3x too heavy next to the new scenery and must be thinned to 0.04 to 0.05 units on conversion (the red tell and 3.3 vehicle rules apply). It is the only change needed besides re-authoring at the density above.
- Selective outlining: no outline on edges that touch the ground or another part of the same object; inside an object use a 0.02 unit line of a darker shade of the lower shape.
- Large sprites (RO-B0, Road-Eater) use 0.06 units, silhouette edge only.
- Scenery layers have no outline. Hazards, projectiles, boss use the same DeepInk outline so they sit in the same world. Tell markers have no outline and a soft 0.02 unit lighter inner stroke.

### 2.4 Detail level
- Vehicles: 3 tone cel shading per material (light, mid, dark) with soft transitions, one gloss highlight, a windscreen gradient. Up to 16 colours plus outline.
- Scenery: 3 to 5 soft silhouettes per layer with atmospheric haze (lighter and bluer toward the horizon) and simple shapes; no fine detail on far layers, medium detail on near layer.
- No semi-transparent edge artefacts: every sprite has a 4 px transparent bleed (premultiplied-safe edge) so bilinear never shows halos.

## 3. Proportions (vehicles and wheels)
Rules carry over in units and ratios from the pixel bible; only the rendering changes.
### 3.1 Vehicles, side view facing right
| Item | Rule |
|---|---|
| Body length | 3.0 to 4.8 units (existing slice cars 4.8) |
| Body height without wheels | 0.30 to 0.45 of length for cars, up to 0.55 for the monster truck |
| Silhouette | Reads as car, sports car, truck from outline alone at 64 px tall icon. Sports 0.30, Classic 0.40 (cabin about 40 percent of length), monster truck 0.55 |
| Wheel diameter | Car 0.30 to 0.36 of body length, monster truck 0.38 to 0.45. Wheels are separate sprites, pivot at exact centre |
| Axle placement | Axle centres at 20 to 25 and 75 to 80 percent of body length; wheel bottom 0 to 0.06 units below body (car), 0.15 to 0.25 (monster truck) |
| Wheel spin readability | Hub with 4 to 6 spokes or a single off-centre bolt and a tyre tread arc; a plain disc cannot show rotation |
| Details | Windows light cool gradient, 2 headlights warm yellow glow, brake light red, one accent stripe in the vehicle colour |
### 3.2 Hazards, pickups, signs
Sized in units from the slice brief (hit shapes come from data, never from sprite size). Eater 6 x 8 units, RO-B0 4 x 4, bolt 1.0 x 0.4, pumpkin 1.25, hay roller 1.2, bomb tell 4.0 diameter, meteor tell 3.4 diameter, ring 2.25, sign 1.5 x 2, crack lengths as the pixel bible converted to units. Section 8 lists changes.
### 3.3 Current vehicle files, verdict
Slice vehicles: **style anchor with one fix** (thin outline, re-author to 128 px per unit, sRGB bleed). They are no longer a style conflict. Kenney pixel cars and all forged pixel vehicles are replaced (section 5).

## 4. Surfaces (road)
- Smooth stylised ground: soft gradient body, a rounded 0.12 to 0.2 unit top lip with a lighter 0.04 unit highlight, a darker soft skirt that fades by gradient (not dither) to transparent over 1 unit. Subtle large-scale detail only: lane dashes with rounded ends, a few soft stones, grass tufts as vector blobs.
- Strip: 1024 x 192, repeat wrap, bilinear, no mipmaps unless shimmer is seen on a device (then enable mipmaps and trilinear for road atlases only).
- Surface colour code is binding and unchanged: **red = damage, blue = ice, brown = mud, cyan-white = glass, gold = reward**. Red never marks a reward, gold never marks a hazard.
- Catalogue previews: re-rendered from the new strips at the card's size, not downscaled composites of pixel art.

## 5. Backgrounds
- Four layers (sky, far, mid, near) keep the director contract (scroll factors 0, 0.2, 0.4, 0.8 and per-world mood). The art changes to **soft vector silhouettes with atmospheric perspective**: far lightest and bluest, near darkest and most saturated, large puffy cartoon clouds as separate sprites, sun and moon discs with a soft glow.
- Source order: (1) our own generator under `tools/art/` (supersampled layered noise or bezier silhouettes with haze, palette-driven per world, CC0-free of third-party pixels), (2) CC0 smooth packs found by the scout (Kenney Background Elements Redux, flat cartoon, is on the shortlist already, plus OpenGameArt and itch CC0 packs found per the rules), (3) the existing smooth-ish forms from the Oct 28 prototype hills as a reference only.
- VISTA is **not smoothed by filtering**. Bilinear on dithered pixel art looks blurry and worse. If kept at all, it is as the optional retro style.
- Parallax scroll no longer snaps to a pixel grid (code change in section 7).

## 6. Palette and tokens
Tokens, never raw colours in scenes or code (`UITheme`, plus a `WorldPalette` asset for world additions). Existing UI tokens stay:
| Token | Hex | Use |
|---|---|---|
| DeepInk | `#18202A` | Outlines, deepest shade |
| WorkshopCharcoal | `#252B36` | Dark metal, panels |
| RacingOrange | `#F4A340` | Primary action, Classic car |
| SignalRed | `#D94B40` | Damage and tells only |
| WarmCream | `#E7E0CF` | Text, highlights |
| WorkshopTeal | `#6B9B87` | Positive state |
| Steel | `#59636D` | Metal mid tone |
World additions (PROPOSED, gradients are built from Light, Mid, Deep): Warn.Red `#D94B40`, Warn.RedDeep `#8E2A2A`; Reward.Gold `#F6C945` / Deep `#B88A1E`; Ice `#E6F6FF` `#8CC8F0` `#3F7FBF`; Mud `#9A6B3E` `#6B4426` `#3C2515`; Glass `#F2FFFF` `#9FE4EE` `#3B8EA6`; Eater Hide `#4A2B5C`, Gut `#8E2A4E`, Tooth `#E7E0CF`; Bot Shell `#59636D`, Light `#B0BCCA`, Core `#60D6E6`.
New for smooth scenery (PROPOSED, one `SceneryRamp` per world: sky top, sky horizon, far, mid, near, ground, haze, sun): three starter ramps, all gradients.
| World | Sky top / horizon | Far / Mid / Near | Haze |
|---|---|---|---|
| Countryside | `#5BB6F0` / `#CDEBFF` | `#9CC9C4` / `#6FB07A` / `#3E8A4F` | `#E9F6FF` |
| Desert | `#FFB35C` / `#FFE2B0` | `#E3A27B` / `#C97C54` / `#9A5A3A` | `#FFF1D6` |
| Night City | `#141B3A` / `#3A3F7A` | `#2B3260` / `#222850` / `#161A38` | `#5560A8` |
Rules: gradient steps allowed; saturation of the near layer at most 80 percent so the road and car pop; luminance contrast between a hazard or tell and the scenery behind at least 3:1 (test on Countryside, the busiest); colour-blind rule unchanged (every tell has a shape; check in greyscale).

## 7. Import settings, fonts, UI skin, code implications
### 7.1 Texture import (binding; inspector verifies)
| Setting | Old (pixel) | New (smooth) |
|---|---|---|
| Filter mode | Point | **Bilinear** (trilinear only for road atlas if shimmer) |
| Pixels per unit | 16 (world), 100 (UI) | **128 for gameplay sprites and road strips, about 41 for backgrounds (576 px / 14 units), UI keeps 100** or per file so the unit size matches section 8 |
| Mipmaps | Off | **Off** for sprites and UI; **On** for the road atlas and tiled background layers only if shimmer or minification shows |
| Compression (Android) | None / RGBA32 | **ASTC 6x6** for backgrounds and scenery, **ASTC 5x5 or 4x4** for vehicles, hazards, UI plates; **ASTC 4x4 or uncompressed** for tiny gradient strips that band (sky gradient strips: uncompressed or RGBA32 at 4 x 576) |
| Max size | native | 2048 cap; background layers 2048 wide; do not exceed 4096 anywhere |
| Wrap | Clamp, Repeat for strips | Unchanged (Repeat for road and layers) |
| Mesh type | Tight large, Full Rect small | Tight for sprites over 256 px with big transparent areas (RO-B0, Eater, clouds); Full Rect for tiling layers |
| Sprite atlas | point, no compression | **bilinear, ASTC 5x5, padding 4 px, one atlas per world scenery, one for hazards, one for vehicles**, Allow Rotation off |
| sRGB / alpha | on | on; alpha is transparency; 4 px colour bleed in the art so no dark halo |
| Android override | meta shows `textureCompression: 1` | set per the table, do not rely on defaults |
**Memory budget per world (computed estimate, measure on device):** ASTC 6x6 is 3.56 bits per pixel, so a 2048 x 576 layer is about 0.5 MB; four layers about 2.5 MB; a 2048 x 864 near layer about 0.8 MB; props and clouds atlas 1 to 2 MB. A world therefore needs about 5 to 8 MiB of art, well under the 25 to 40 MiB per-world cap, which leaves headroom for 4096 atlases if needed.
### 7.2 UI font and skin (for `road-builder-ui`)
- **Fonts: keep Lilita One (display) and Baloo 2 (body).** Both are smooth outline fonts rendered through TMP SDF (not pixel fonts). Required: SDF atlas at 1024 or larger with sampling 90 or higher and bilinear filter, so edges stay clean at all scales; add a soft 0.1 outline and a faint drop shadow through the TMP material (shader params, not scene hardcoding). Check that HUD numerals in Lilita One use the shared TMP material. No pixel fonts anywhere.
- **Skin:** the 39 bilinear Workshop plates, buttons and cards stay as the base. Restyle the 27 point-filtered Workshop files (props, backdrop tiles, badges with stair-stepped edges) by re-running `tools/ui/workshop_home.py` with the pixel-backdrop and pixel-prop functions replaced by smooth vector drawing, at 3x with bilinear import. The garage-door view shows the live world, so it follows the world art with no special asset.
- `UITheme` tokens: add `FilterStyle = Smooth` (informational) and replace sprite references for restyled files via the same IDs so screens keep working; no scene or prefab YAML edits by hand.
- Corner radii: plates 0.5 to 0.6 of the radius already used; icons use rounded geometry; no hard pixel notches. Shadows soft (blur 12 to 16 px at 3x), not hard offset blocks.
- **Docs to update (route to the UI owner):** `Docs/UI/UI_Design_Bible.md` lines on "pixel backdrop at 5x with Point filter" and "Hybrid".
### 7.3 Code and settings that must change (route to implementer)
- `BackgroundDirector`: turn off art-pixel snapping; scroll in float; layer tile width read from the sprite bounds; layer height from the new 576 px sprites.
- `RoadSkin.texelWorld` and `RoadTextured.shader`/material: texture filter bilinear, UVs by world units not texel snapping; new strip dimensions; `RoadSkinFactory` bakes.
- `AssetLedgerCoverageTests` and inspector checklist: pixel-only checks (point filter, 16 ppu, uncompressed) must become the table in 7.1.
- Camera pixel-perfect component (if present): disable.
- URP 2D: set the 2D renderer default sprite filtering to bilinear; confirm no point-sample override on the sprite material.

## 8. Art specs for the slice, smooth versions (units)
Common: 128 px per unit, 0.04 unit DeepInk outline, facing right, bilinear, palette from section 6, light from the upper left. All sizes are in units (same as the pixel bible's px / 16), so no gameplay data changes.
| Item | Size (units) | Notes |
|---|---|---|
| Road-Eater | 6 x 8, 3 frames (open, chew, closed) | Huge toothy maw, gradient gut, no eyes, 0.06 outline, teeth as vector triangles 0.4 to 0.5 tall; danger vignette is a soft red gradient with jagged tooth silhouettes along the left edge, 40 percent alpha |
| RO-B0 | 4 x 4, 4 frames | Rounded friendly robot with a wide cyan visor, hover jets with soft glow; overload ring 1.0 unit soft red ring; defeat sparks as a soft-particle sheet |
| Projectiles | Bolt 1.0 x 0.4, pumpkin 1.25, hay roller 1.2 | Silhouettes distinct as before; tell shapes unchanged: dashed line, red circle 4.0 diameter authored once at 4.0 units (never scaled), orange spark ring plus arrow |
| Ice, mud, glass | strips 8 x 1.5 | Gradient bodies, glint highlights, glass stages as vector crack lines with soft glow; stage 0, 1, 2 must differ in silhouette (greyscale test unchanged) |
| Meteor marker | ring 3.4 diameter authored once, rock 1.5, blast 1.5 | One ring on screen at a time, red never tinted |
| Boost ring | 2.25 diameter | Gold ring with 0.2 thick body, cream highlight, hollow centre; Overload ring has a cyan rim |
| Signs | 1.5 x 2 | Same shape plus icon plus colour table (ice diamond, mud diamond, glass diamond, meteor triangle red, Eater square red, reward round gold), icons as smooth vector glyphs |
| Coins, hearts, star, ink canister | existing 128 px files | **No longer a style debt**; re-author at the common density, thin the outline to match |
Reject if: a hard pixel edge or dither remains, an outline is not 0.04 (0.06 for the two large sprites) within 10 percent, the sprite is magnified more than 1x on a 1440p phone, red is used for a reward, a ring is filled in the centre.

## 9. Audit table: pixel-styled asset groups
Effort: S under half a day of forge work, M 1 to 2 days, L 3 or more days. Counts from file reads.
| Group (path) | Count / size | Action | Effort | Notes |
|---|---|---|---|---|
| Backgrounds VISTA (`Assets/Art/Backgrounds/Vista`) | 40 layers 384x216, point, 16 ppu | **Replace** with own smooth generated layers (or CC0 smooth packs); keep files untouched as optional retro style | L (generator M, 3 worlds first S each) | Top priority after road. Do not smooth by filtering |
| Forged worlds (`Art/Worlds/Forged`) | 60 layers (15 scenes x 4), derivatives of VISTA or procedural | **Replace**, generated from the same new generator with palette per world | M (parameter sets once generator exists) | World list drives order of conversion |
| Third-party world packs (`Worlds/CityNight4Col`, `DesertForest`, `ParallaxForestZD`, `WarpedCaves`, `Underwater`) | 31 files, 120 to 1280 px, pixel | **Replace** (unused raw source art; not needed once generator exists); keep in quarantine or leave unreferenced | S | Not referenced by `ready` items after conversion |
| Forged surface strips (`Art/Surfaces/Forged`) | 23 files 128x26, Bayer dither | **Replace** with 1024x192 smooth vector strips, procedural | M | Catalogue previews follow |
| Volcano pack (`Surfaces/Volcano`) | 60 vector PNG, filter 1 | **Keep** (already smooth); lava or ash road look can use it | S (import ppu check only) | Style anchor for ground texture. Matching style test needed: its dark ground is grey patterned, check against the new palette |
| BlazesPlatformer, FloatingIslands, SeasonalTiles, Lava, Water (`Surfaces/*`) | about 28 files, 16 or 32 px tiles | **Replace** as road sources; do not use as raw tiles. Lava strip 16x32 and ocean 32x32 animations: replace with shader or vector scroll | S each | Only needed as colour references now |
| Kenney pixel vehicles (`Vehicles/Kenney`) | 50 vehicles 38 px, 7 props | **Replace** | L | Pixel vehicle pack. Scout a CC0 smooth side-view vehicle pack; otherwise forge procedural vector cars from the CarBody style |
| Forged vehicles (`Vehicles/Forged`) | 59 files, 33 to 48 px | **Replace** (monster truck and bicycle first) | M per vehicle set, L total | Procedural vector rigs with separate wheels |
| Reactorcore dirt bike, Tank, Skateboard, OGA motorcycles and chopper (`Vehicles/*`) | 24 parts, 32x32 tank, 968x429 blown-up bike | **Replace**; Reactorcore/Tank keep as hull rig reference | M | OGA motorcycle at 968 px is blocky, not smooth |
| Slice vehicles (`Resources/RoadBuilder/CarBody`, `SportCarBody`, `MonsterTruckBody`, `Wheel`) | 480x180 to 160x160, 12 px outline | **Keep and restyle outline** (thin to 0.04 to 0.05 units, 128 ppu) | S | Style anchor |
| Pickups (`Resources/RoadBuilder/Pickup_*`, `Coin`, `Heart`, `Star`, `InkCanister`, `Pickup_Ring`) | 16x16 pixel icons plus 96 to 128 px hi-res | **Restyle** pixel set (7 files) by regenerating `tools/art/make_pickups.py` as vector; keep hi-res four | S to M | Ring already smooth but outline weight needs checking |
| Workshop UI plates, buttons, cards, badge | 39 files bilinear | **Keep** | none | Anchor |
| Workshop UI pixel props and backdrop tiles | 27 files point | **Restyle** via `tools/ui/workshop_home.py` | M | Pegboard, robot, barrel, tyres, lamp, floor tiles |
| `Art/UI/Generated` and `Resources/RoadBuilder/Art` | 9 plus buttons, bilinear | **Keep** | none | |
| Catalogue previews and wheels (`Art/Catalog`) | 123 files, composites of pixel art | **Regenerate** after the above (`make_previews.py`, `find_wheels.py` replaced by direct renders) | S once sources exist | Last step |
| Hazards in Slice (Eater, RO-B0, projectiles, markers, signs, cracks) | not yet built | **Build new in smooth style** per section 8 | L | Never build pixel versions now |
| Fonts | Lilita One, Baloo 2 | **Keep** | none | Fine as is; SDF settings per 7.2 |
| Audio | n/a | unaffected | none | |

## 10. Prioritised conversion order (what the player sees most first)
| Step | What | Done when | Effort |
|---|---|---|---|
| 0 | Fix pipeline: import presets (7.1), disable pixel snapping, ledger and inspector checks updated, generator skeleton under `tools/art/` | One test world renders smooth in-game | M |
| 1 | **Road surfaces**: asphalt, dirt, grass trail, plus ice, mud, glass strips as 1024x192 vector strips with soft lip and skirt | Road looks smooth on slopes and curves at phone size | M |
| 2 | **Backgrounds** for the 3 main worlds (Countryside, Desert, Night City) via the generator; then remaining 12 forged worlds | 3 worlds approved by the art director on device-size screenshots | M then M |
| 3 | **Slice vehicles restyle** (outline) plus replace Kenney default car set; wheels | Garage default vehicles match the world | S then L |
| 4 | **UI**: restyle the 27 point-filtered Workshop files; update docs; TMP SDF settings | Home screen and HUD have no stair-stepped edges | M |
| 5 | **Hazards and pickups** (Eater, RO-B0, projectiles, markers, signs, ring, coins) | Slice looks complete in one screenshot | L |
| 6 | **Props and particles**: coins, hearts, clouds, roadside props, catalogue previews regenerated | Collection browser consistent | M |
Parallel option: the user may prefer hazards before UI if hazard playtests are imminent; hazards are not on screen until the boss slice runs, so the order above follows screen time. Order inside step 2 follows the world with most play time first.

## 11. Licence implications of existing pixel packs
| Pack | Licence in ledger | Implication |
|---|---|---|
| VISTA (najjar320) | CC0 1.0, only reselling the pack is forbidden | Keep files and ledger rows. Hidden risk: none if unreferenced; leave in place or move to a retro folder. Derived forged worlds (`world-*`) are also CC0 derivatives, stay logged. If files are deleted, remove or mark ledger rows `removed` and the credits screen (generated from the ledger) updates |
| Kenney Pixel Vehicle Pack | CC0 1.0 | No credit needed; keep rows even if unreferenced |
| Reactorcore, OGA vehicles, Tank, Skateboard, Volcano, Lava, Water, Blaze, Floating Islands, Seasonal, Warped Caves, DesertForest, ParallaxForestZD, CityNight4Col, Underwater (ansimuz) | CC0 on their pages | No obligations either way |
| Forged vehicles, strips, worlds, previews, wheels, pickups, UI | own work (derivatives of CC0 palettes or art) | Stay our own work; derivative rows list sources |
| Fonts | SIL OFL 1.1 | Unchanged; credit stays |
| Apple Zarvox VO | Open licence question | Unrelated to this change but still open |
Conclusions: (1) switching the look needs **no new licence work**: no CC-BY item exists, so no credit lines appear or disappear. (2) New sources must obey `ASSET_AGENT_RULES.md` section 2; Kenney vector packs are CC0 on the creator page, but the scout must re-read each page and save evidence as for the existing packs. (3) Removing an unreferenced pack from `Assets/` must go through the inspector so `AssetLedgerCoverageTests` stays green (rows without files may fail it). (4) Procedural vector art is own work: row names `tools/art/<script>.py` and the dependency (for example cairosvg or Pillow) is a build tool, not shipped.

## 12. Provenance and review checklist
- Every file: ledger row (path, source or `own work (procedural)`, author, licence, date, used for) and a `FORGE_NOTES.txt` (method, sources, limits, rating 1 to 5; below 3 is `placeholder`). Generator scripts committed so art can be regenerated. Never describe procedural output as hand-drawn.
- **Style** [ ] no pixel grid or dither; no stair-stepped edge at 100 percent zoom; [ ] bilinear, mipmaps per table, no sprite magnified over 1x; [ ] outline 0.04 units (0.06 for two large) rounded; [ ] light from upper left via gradient; [ ] palette tokens and scenery ramps; [ ] sits next to the slice vehicles and Workshop plates without clashing (phone-size screenshot); [ ] side view facing right.
- **Readability** [ ] silhouette readable at 64 px tall; [ ] red only for damage, gold only for reward; [ ] every threat has a shape tell; [ ] tell contrast 3:1 on Countryside; [ ] crack stages 0, 1, 2 distinct in greyscale; [ ] hit shapes from data.
- **Vehicles** [ ] wheel ratio and placement per 3.1; wheel pivot centred; spin readable.
- **Technical** [ ] import per 7.1 including Android override; [ ] atlas assigned; [ ] 4 px transparent bleed; [ ] draw order unchanged.
- **Provenance** [ ] ledger row, notes, script path; no AI; [ ] kids-safe (no gore, no realistic weapons, Eater a toothy maw only).
Verdicts: `style-approved`, `style-approved-with-notes`, `style-rejected`. Style approval is not `ready`.

## 13. Review notes
| Date | Item | Verdict | Notes |
|---|---|---|---|
| 2026-11-15 | Direction change to smooth | recorded | User decision: pixel look too strong; supersedes pixel bible |
| 2026-11-15 | Slice vehicles, Workshop plates, fonts | style-approved-with-notes | Anchors; thin the vehicle outline |
| 2026-11-15 | VISTA, forged strips, Kenney and forged vehicles, pixel Workshop props, 16x16 pickups | style-rejected for the new direction | Replace or restyle per section 9 |
| 2026-11-15 | Volcano pack | style-approved-with-notes | Already vector, check against palette |

## 14. Open risks and questions
1. **No free smooth side-view vehicle pack confirmed.** The scout has not searched yet. Fallback is procedural vector cars; quality of procedural cars is the highest-risk item (aim 3 of 5 for placeholders).
2. **Generated backgrounds may look flat** compared with hand-painted packs. Mitigation: layered haze, cloud sprites, a one-world look test before committing all worlds.
3. **Banding in gradients on ASTC.** Use 6x6 only for busy layers, uncompressed or 4x4 for sky gradients, and add a subtle noise at export if banding shows on a device.
4. **Code changes needed** (7.3): snapping off, road shader and filter, asset test rules. Route to the team lead and implementer; none are done.
5. **Doc conflicts to update by their owners:** `Docs/THEME_PLAN.md` (pixel direction), `Docs/ASSET_AGENT_RULES.md` (pixel quality bar, 16 ppu, point filter), `Docs/UI/UI_Design_Bible.md`, `Docs/design/VERTICAL_SLICE_BRIEF.md` art notes. Not changed here.
6. **Mixed look during transition:** pixel and smooth assets must never share a world or a screen. Convert a whole layer set or a whole surface at once.
7. **Numbers in this document are targets** (sizes, ASTC memory, outline weight); the first converted world and a device test will confirm them. Nothing here has been rendered or imported.
