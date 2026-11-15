# ARCHIVED (superseded 2026-11-15 by ART_BIBLE.md, smooth look): Art Bible, Retro Arcade Workshop pixel version

Owner: art-director. Status: PROPOSED, nothing in section 8 is built yet. Date: 2026-11-15.
Read with: `Docs/THEME_PLAN.md`, `Docs/WORLD_AND_VEHICLE_CATALOGUE.md`, `Docs/ASSET_AGENT_RULES.md`, `Docs/design/VERTICAL_SLICE_BRIEF.md` (the brief wins on gameplay numbers).
Rule of this file: it states the look. It does not set legal or technical `ready` status; the asset-inspector owns that.

## 0. Direction lock
- **Pixel art, Retro Arcade Workshop, locked.** No switch to high-res. If a high-res look is ever wanted, it is an extra `ArtStyle` option behind one presentation interface (same gameplay, same colliders, same data). It needs the user's explicit approval and is out of the slice.
- **No generative AI output**, ever. Allowed: CC0 packs, recolour, mirror, crop, composite, our own code (`tools/assets/pixel.py`, `tools/art/*.py`), and pixel-by-pixel drawing from explicit coordinates. Never call procedural results hand-drawn.
- Two sub-looks, one family: the **world** (VISTA scenery, Kenney-style vehicles, tile strips, slice hazards) is pixel art at one density; the **UI shell** (Workshop plates, buttons, badges) is illustrated and lives on a canvas, never in the world. Do not put illustrated UI art in the world or world sprites on a UI plate.

## 1. What exists today (audit, read from files on 2026-11-15)
| Group | Finding | Verdict |
|---|---|---|
| Backgrounds (VISTA, `Assets/Art/Backgrounds/Vista`) | 384x216 layers, point filter, 16 ppu import, compression 0 | Style reference for the whole world |
| Surface strips (`Assets/Art/Surfaces/Forged/*`, e.g. ice, glass, mud) | 128x26 procedural strips, rows 0-1 top, 2-5 body, 6-25 skirt, 6-12 colours, quality 3 of 5 | Right density. Ice, mud, glass are "usable, not polished"; slice art builds on them |
| Forged vehicles (`Assets/Art/Vehicles/Forged/*`) | Tiny: monster body 33x11, wheel 15x15, chassis 40x5, assembled 48x32 | Right density, but the forged set is outside the slice (brief section 8) |
| Kenney vehicles | e.g. ambulance 38x21 px, about 2.4 units at 16 ppu | Density reference for cars |
| **Slice vehicles (`Resources/RoadBuilder/CarBody.png`, `SportCarBody.png`, `MonsterTruckBody.png`, `Wheel.png`)** | **480x180, 480x160, 480x200, wheel 160x160: smooth vector-style with 12 px black outlines** | **Style conflict.** These are not pixel art. See 1.1 |
| Pickups (`Pickup_*.png`) | 16x16 pixel icons, ink outline, procedural, but the ring is 96x96 and coin/heart 128x128 | Pickups are pixel; ring, coin, heart, star, ink canister are high-res and need the same treatment |
| UI shell (`Art/UI/Workshop`) | Illustrated plates, own colours (`ColorToken.Tint`) | Stays as is, UI only |

### 1.1 Decision needed for the three slice vehicles (route to team lead)
The slice vehicles (ClassicRed, SportCyan, MonsterTruck) are the largest remaining style clash against VISTA and the strips. Art-director position: **before the slice is called "looks done", replace them with pixel versions** at the specs in section 3, built by the forge (recolour and re-rig Kenney pixel cars, and the forged monster truck, with source rows in the ledger). Interim: they may stay for grey-box gameplay tests, flagged as `placeholder`, and slice screenshots must not be used for style sign-off while they remain. Vehicle sprites in code stay behind their current load path so swapping is data-only.

## 2. Camera, light, view
- **Side view, orthographic, 2D.** Camera `orthographicSize` 7.0 (14 world units tall; widened on narrow phones, clamp 13.5). No tilt, no perspective, no top-down or isometric pieces anywhere.
- **Everything faces right** (direction of travel). The Road-Eater is the only thing allowed on the left edge; RO-B0 hovers right and ahead of the car.
- **Light from the upper left**, sun about 45 degrees above the horizon. Rules:
  - Highlight on the top and left edges, mid tone on the face, shadow on the bottom and right edges.
  - Cast shadows: a one-tone dark blob below the sprite for floating objects only (one flat shade, no blur, 50 percent alpha of the outline colour). Ground objects get none.
  - Surfaces are lit from above: a 1 to 2 px bright top lip, dark skirt at the bottom (matches the strip layout).
  - Night and dusk scenes tint by multiply only; never repaint lighting per scene.
- **Layering order (back to front):** sky, far, mid, near scenery, road skirt and body, road top lip, props, hazards on the road, car and wheels, boss and projectiles, particles, tell markers, HUD (screen space). Tell markers (red) always draw above everything in the world.
- **Pixel snapping:** background layers snap to the art pixel grid (existing). Vehicles and hazards are not snapped while moving (physics owns position); they rotate freely with the slope, point filtered.

## 3. Proportions, wheels, outlines, detail
### 3.1 Pixels per unit and density
- **One density: 16 pixels per world unit (ppu) for every gameplay sprite.** One art pixel = 0.0625 units.
- Background layers are authored at 384x216 and displayed so 216 px fill the 14-unit camera height (`texelWorld = 14/216` = 0.0648 units, about 15.4 ppu). The difference to 16 ppu is 3.7 percent. Accepted for the slice; at 1080 px tall one background pixel is exactly 5 screen px and one gameplay pixel is about 4.8 screen px. **Open risk:** uneven pixel widths on rotated sprites at some resolutions. Recheck on a phone; if it shows, the fix is to set the camera to 13.5 units (216 / 16) and re-tune, which touches gameplay framing and needs the team lead.
- Never mix densities inside one sprite. Never scale a sprite by a non-integer factor to fit; change the art or the data.
- No sub-pixel rotation baked in. No anti-aliasing and no soft glow in the art (glow is done with 2-tone dither or a ring, see section 8).

### 3.2 Vehicles (side view, facing right)
| Item | Rule |
|---|---|
| Body length | 36 to 56 px (2.25 to 3.5 units). Reference: Kenney cars 38 px, forged monster truck chassis 40 px |
| Body height (without wheels) | 0.30 to 0.45 of body length for cars, up to 0.55 for the monster truck cab |
| Silhouette | Read as car, sports car or truck from the outline alone at 64 px tall icon size. Sports car: low, long nose, 0.30 height ratio. Classic: boxy, cabin about 40 percent of length, 0.40. Monster truck: tall body on a raised chassis, 0.55 |
| **Wheel diameter ratio** | **Car 0.30 to 0.36 of body length (Classic 0.33, Sport 0.30). Monster truck 0.38 to 0.45 of body length (0.42 target, matches forged 15 / 40 = 0.38 chassis).** Wheels are round: use diameters 9, 11, 13 or 15 px (odd sizes give a centre pixel for the hub) |
| Wheel placement | Axle centres at 20 to 25 percent and 75 to 80 percent of body length; wheel bottom 0 to 1 px below the body bottom edge (car) or 2 to 4 px (monster truck). Wheels are separate sprites so they can spin; the art needs a pivot at the exact centre pixel |
| Wheel spin readability | Hub with 3 or 4 spokes or a single off-centre bolt; a plain disc cannot show rotation |
| Details | Windows in the lightest cool tone, headlight 2 px warm yellow, brake light 1 px red, one accent stripe in the vehicle colour. Max 12 colours per vehicle including the outline |

### 3.3 Outline
- **1 px outline** at 16 ppu for every world sprite. Colour: `Outline` token (DeepInk `#18202A`) for dark-on-light; do not use pure black.
- **Selective outlining:** the outline is dropped (0 px) on the side that touches the ground or another part of the same object, and inside, between two shapes, a 1 px darker shade of the lower shape is used instead of the ink colour.
- Hazards, projectiles and boss use the same 1 px DeepInk outline so they sit in the same world. Tell markers (red) have no outline but a 1 px lighter inner stroke.
- Large sprites (RO-B0, Road-Eater) may use a 2 px outline only at the silhouette edge, never inside. Reason: they are 3 to 8 times larger than a car and a 1 px line would vanish.
- Background and scenery layers have no outline (as in VISTA).

### 3.4 Detail level
- Cars: 3 tones per material (light, mid, dark) plus outline. No dithering on vehicles.
- Surfaces and scenery: ordered 4x4 Bayer dither allowed for gradients (existing practice). Maximum 12 colours per strip.
- No texture noise on flat colours inside characters, no gradients, no semi-transparency except shadow blobs, glass and tint overlays.

## 4. Palette tokens
Gameplay and UI use named tokens, never raw colours in scenes or code (`UITheme`). Existing UI tokens (read from `Docs/UI`):
| Token | Hex | Use |
|---|---|---|
| Outline / Background (DeepInk) | `#18202A` | Sprite outline, deepest shade |
| Surface (WorkshopCharcoal) | `#252B36` | Dark metal, boss shell shade |
| Primary (RacingOrange) | `#F4A340` | Player action, Classic car, selection |
| Danger (SignalRed) | `#D94B40` | Damage and tells only |
| Text (WarmCream) | `#E7E0CF` | Paper, lights, highlights |
| Success (WorkshopTeal) | `#6B9B87` | Positive state |
| Secondary (steel) | `#59636D` | Metal mid tone |
World additions for the slice (PROPOSED; implementer adds as `UITheme` or a `WorldPalette` asset, never hardcoded). The colour code of the brief section 4 is binding: **red = damage, blue = ice, brown = mud, cyan-white = glass, gold = reward.** Red is never used for a reward and gold is never used for a hazard.
| Token | Hex (PROPOSED) | Use |
|---|---|---|
| Warn.Red | `#D94B40` (same as SignalRed) | Every telegraph marker, tell line, danger vignette, warning sign border |
| Warn.RedDeep | `#8E2A2A` | Shade of red hazards, marker ring inner |
| Reward.Gold | `#F6C945` | Rings, coins, CLOSE! toast, escape ring timer |
| Reward.GoldDeep | `#B88A1E` | Gold shade, ring rim |
| Ice.Light / Mid / Deep | `#E6F6FF` / `#8CC8F0` / `#3F7FBF` | Ice surface, ice sign, edge flash |
| Mud.Light / Mid / Deep | `#9A6B3E` / `#6B4426` / `#3C2515` | Mud surface, splash |
| Glass.Light / Mid / Deep | `#F2FFFF` / `#9FE4EE` / `#3B8EA6` | Glass pieces, crack lines (Light), sparkle |
| Eater.Hide / Gut / Tooth | `#4A2B5C` / `#8E2A4E` / `#E7E0CF` | Road-Eater body, mouth interior, teeth |
| Bot.Shell / Light / Core | `#59636D` / `#B0BCCA` / `#60D6E6` | RO-B0 metal, panel highlight, overload core |
| Proj.Bolt / Bomb / Roller | `#F4A340` / `#D94B40` / `#C9A640` | Projectile body accents (always with DeepInk outline) |
Palette rules: each world sprite uses at most 12 colours, taken from these tokens plus the VISTA palette (`.gpl` in the pack). A colour that is not in the set needs a ledger note. Contrast: a hazard or tell must differ from the scenery behind it by at least 3:1 luminance; test on the Countryside world, the greenest and busiest.
Colour-blind rule: no threat or reward is told by colour alone. Every tell has a shape (dashed line, circle, spark ring, arrow, teeth), as the brief requires. Check by viewing the frame in greyscale.

## 5. Surfaces and tiles
Strip layout stays: 128x26 (rows 0-1 top lip, 2-5 body, 6-25 skirt), tileable in X (edge seam score under 0.05), repeat wrap, point filter. Slice surface looks (all in section 8): ice, mud, glass. The catalogue previews are 128x26 strips, shown on cards; they are not scaled.

## 6. Import settings (binding, inspector verifies)
| Setting | Value |
|---|---|
| Texture type / mode | Sprite (2D and UI), Single, or Multiple for sheets |
| Pixels per unit | 16 (UI shell art keeps its own settings) |
| Filter mode | Point (no filter) |
| Mipmaps | Off |
| Wrap | Clamp for sprites; Repeat for surface strips and background layers |
| Compression | None for pixel sprites and strips (current files show compression 0 on the default platform). **Note:** the existing `.meta` files carry `textureCompression: 1` on the Android and Standalone overrides, which means the Android build is compressing pixel art. Inspector to confirm in Unity and set Android to uncompressed or RGBA32 for sprites under 256 px; large sheets (over 512 px) may use ASTC 4x4 only after a visual check |
| Max size | Native size; no downscale |
| Mesh type | Tight only for large sprites (RO-B0, Eater); Full Rect for small ones |
| Pivot | Wheels: Centre. Vehicle bodies: Bottom Centre at the axle line. Hazards on the road: Bottom Centre. Floating pieces (projectiles, markers, ring): Centre |
| Sprite sheets | Frames in one row or grid, all frames the same size, power of two not required, 1 px of transparent padding is not needed with point filter and Full Rect |
| Atlas | Pack the slice hazards into one Sprite Atlas (point filter, no compression, padding 2, no tight packing for animated frames) so the slice stays under the draw-call budget |
| Alpha | Alpha is transparency on; no premultiplied tricks |
Do not rely on rotation of pixel art by tweens for hazards; rotate only vehicles and projectiles where the brief needs it.

## 7. Provenance and ledger
- Every new file: a row in `Docs/ASSET_LEDGER.md` (path, source or `own work (procedural)`, author, licence, date, used for), and a `FORGE_NOTES.txt` with method, sources, limits, honest rating 1 to 5. Below 3 is labelled `placeholder`.
- A recolour or composite of CC0 art names every source file. A procedural asset names its script (path under `tools/art/`) and keeps the script committed so the art can be regenerated.
- Art-director signs off style in the review notes; the inspector sets `ready`. Game code references only `ready` or `have` items; unreviewed `forged` items appear only in the Editor and dev builds.
- No fonts, sounds or images copied from games, no AI generator output, no watermarked previews.

## 8. Slice art specs
Common to all: 16 ppu, 1 px DeepInk outline unless stated, facing right, point filter, max 12 colours, palette from section 4, light from the upper left. Sizes in px; "units" = px / 16. Frame rates are suggestions for the animation owner (DOTween or a sprite animator, not specified here).

### 8.1 Road-Eater (MON-EATER)
- **Role:** a giant maw chasing from behind; unkillable, no collider; presented at the left screen edge.
- **Read:** a huge mouth with a jagged teeth row, no eyes needed (a bare mouth is scarier and cheaper). Silhouette must read as "teeth, chewing" at 1 second.
- **Size:** body 96 px wide x 128 px tall (6 x 8 units, about 57 percent of the camera height). It overlaps the left edge: only the right 60 to 70 percent is visible at the gap band Edge; the sprite sheet carries the full body.
- **Frames (3, as the brief):** `eater_open` (mouth wide, teeth top and bottom), `eater_chew` (mouth half closed, jaw offset up 6 px, rubble pixels in the teeth), `eater_closed` (mouth shut, teeth overlapped). Loop open, chew, closed, chew at 6 to 8 fps; the chew rate rises with closeness (data in the SO, not in the art).
- **Colours:** Eater.Hide body with a darker underside, Eater.Gut mouth, Eater.Tooth teeth. Teeth are 6 to 8 px tall triangles, uneven, 7 to 9 per row.
- **Outline:** 2 px silhouette-only outline (large sprite rule).
- **Companion art:** rubble particle 3x3 px, 4 variants (Mud.Mid, Mud.Deep, Surface grey, Tooth cream); dust puff 8 frames of 16x16, warm grey; bitten road edge: a ragged 8x16 end cap sprite (road body colours with a stepped break) placed where the road ends behind the car; **edge meter** for the Calm/Edge/Danger/Snap bands lives in UI (Workshop tokens), not world art; **danger vignette** is a 16:9 overlay of Warn.Red jagged teeth shapes along the left edge, 4 px teeth at 384x216 scale, 40 percent alpha, so the pixel grid matches the background.
- **Reject if:** the mouth is cute or smiling (kids-safe, but it must still read as a threat), blood or gore, a body that covers more than 70 percent of the screen height, any anti-aliased edge, any red used for non-danger.

### 8.2 RO-B0 boss robot (BOS-ROB0)
- **Role:** a floating shooter above and ahead of the car. No hit points, no hitbox, no player attack. Silhouette must be readable against sky and mountains.
- **Size:** 64 x 64 px (4 x 4 units), hover offset in data. Single sprite with a separate 8x8 px arm/cannon pivot optional.
- **Read:** a rounded, friendly-but-mechanical robot: a square-ish head with one wide cyan-white eye/visor, round body, no legs, small hover jets. Friendly enough for kids, clearly the enemy by its red tell colour when it attacks.
- **Frames (4, as the brief):** `idle` (jets flame small, light blinks), `charge` (core glows, shoulders open 4 px), `fire` (cannon forward, muzzle flash 2 frames), `retreat` (tilted back 8 degrees, one smoke puff). Idle bob is a tween, not a frame.
- **Colours:** Bot.Shell body, Bot.Light highlights top and left, Surface dark shade bottom and right, Bot.Core overload meter lamp (cyan). A 2 px "overload" ring of Warn.Red around the core appears only in the final 10 percent of the meter (separate overlay sprite, 16x16, 2 frames).
- **Defeat:** 6 frames of sparks (8x8 each) and a white-flash 1 frame variant of the body (all non-transparent pixels set to cream); no gore, it deflates and flies away.
- **Outline:** 2 px silhouette outline.
- **Reject if:** it looks like a weapon-wielding soldier, has realistic guns, has a skull or eyes that glare red, or is smaller than 48 px (unreadable on a phone).

### 8.3 Three projectile skins (Countryside world skin, `ProjectileSkinSO`)
Each type must be readable **by silhouette alone**: bolt is long and thin, bomb is round and big, roller is wide and flat on the road. Each has a tell (red, drawn separately) and a projectile (themed Countryside). A world skin can later swap the projectile art without touching data.
| ID | Name | Projectile sprite | Tell sprite | Impact |
|---|---|---|---|---|
| PRJ-BOLT | Corn Shot | 16 x 6 px, yellow corn kernel stack with a darker tip and 2 px trailing streak (orange `#F4A340` and cream; Reward.Gold is not used so gold stays reward-only). Hit radius 0.4 units: the sprite is 1 unit wide and the hit circle is smaller than the art | Dashed line: 2 px tall dashes, 4 px on and 4 px off, Warn.Red, scrolling at 8 px per frame; the last 0.4 s the dashes turn solid (the "lock") | 8x8 puff, 4 frames, cream and orange |
| PRJ-BOMB | Pumpkin Bomb | 20 x 20 px round pumpkin with a green stem and a lit fuse pixel that blinks (2 frames). Blast 2.0 units radius: the pumpkin art stays small | **Red circle** equal to the blast: radius 2.0 units = 64 px diameter (4 units), authored once at 64 x 64, never scaled. 2 px ring plus an 8-frame inner hatch that fills over 1.5 s | 24x24 explosion, 6 frames, red and orange, ends in smoke pixels |
| PRJ-SAW | Hay Roller | 19 x 19 px round hay bale seen from the side with a spiral line, rolls along the road (rotation tween); sits on the road line with a 1 px dark base | Orange spark ring on the road (12 x 12 px, 4 frames) plus a 12 x 12 arrow sprite pointing at the car | 12x12 dust, 4 frames, brown |
Common rules: the tell is red or orange and always has a shape; projectile bodies use the DeepInk outline; every projectile shows a 2-frame idle so it feels alive; no realistic weapon forms (no bullets, grenades with pins, blades). Pooled, so all frames of one type share one sprite sheet and one atlas page.
Sprite size never defines the hit shape. Hit shapes are owned by data (`ProjectileDefinitionSO.hitRadius`) and are never read from the sprite.

### 8.4 Ice, mud and glass
All three extend the existing 128x26 strip format and the catalogue entries `surface-ice`, `surface-mud`, `surface-glass`. The brief's colour code applies: blue ice, brown mud, cyan-white glass.
**Ice (SRF-ICE)**
- Top lip: Ice.Light 2 rows with a 1 px white glint every 16 to 24 px. Body: Ice.Mid with 2 to 3 diagonal Ice.Deep cracks (1 px, 3 to 5 px long), no dither on the body, slab edges every 32 px. Skirt: dither fade to transparent.
- Overlay props: 6 px and 10 px icicle sprites hung from the skirt (use sparingly, 1 per 40 px), 3x3 sparkle, 4 frames, for the particle prefab.
- **Sign:** see 8.8. **Blue tint** applied to the scenery by multiply `#CFE6FF`, strength 0.2, never on the car.
**Mud (SRF-MUD)**
- Top lip: Mud.Light irregular edge (1 px bumps every 3 to 5 px), body Mud.Mid with 4 to 6 Mud.Deep speckles and 2 px stones in Surface grey, skirt dithered dark brown to transparent.
- Overlay: a wet-shine row (1 px, Mud.Light, 20 percent alpha) repeated every 24 px.
- **Splash particle:** 6x6 px, 4 frames (blob, spread, droplets, gone), Mud.Mid with a Mud.Light highlight top-left.
- Mud is **never** red, never has a hazard outline; it is a terrain colour.
**Glass (SRF-GLS), with crack stages**
Glass pieces are 4 units long (64 px). The strip must read as translucent glass over dark depth, not as ice: lighter, straight panel lines, lower contrast to the sky, cyan edge.
- **Piece, intact (stage 0):** 64 x 26 px tile. Top lip Glass.Light 1 row with a Glass.Mid 1 row below; body Glass.Mid at 235/255 alpha with 2 diagonal Glass.Light glints; frame lines at both ends (1 px Glass.Deep) so each piece reads as a separate 4 m panel; skirt dither fade.
- **Stage 1: hairline (arms at 0.8 s):** one 1 px Glass.Light crack, 20 to 28 px long, zigzag with 3 bends, from the top edge into the body, plus a 3 px chip at the top. A crack at this stage must be visible as a **shape** against the intact piece even in greyscale: use Glass.Light on Glass.Mid body and 1 px DeepInk shadow pixel on the lower side of the crack.
- **Stage 2: spiderweb (1.6 s):** the hairline plus 4 to 6 branching cracks in a radial star around the weight point (the wheel), 12 to 20 px each, plus 4 small `Glass.Deep` flake pixels falling 1 to 2 px below the skirt. Piece darkens 10 percent (Glass.Mid replaced by Glass.Deep in a 6 px central patch).
- **Stage 3: fall (2.6 s):** the piece drops: 3 frames of the piece splitting into 4 shards (each 14 to 20 px wide) with a 1 px offset per frame, then falls under gravity (physics or tween), no new art needed beyond the shards. Shard sprite: 4 variants, triangular.
- **Sparkle loop:** 2 px Glass.Light pixels, 3 frames, random positions on intact pieces (the "tinkle").
- **Entry platform:** 6 m non-cracking `noEat` piece is the same intact art but with a solid Glass.Deep frame and **no** cracks ever, so players learn where it is safe.
- A fallen piece leaves a hole: no art needed, the road simply has a gap (cleared by RoadChunk).
- Crack stages are shapes plus a sound (brief F1), so the three stages must differ in silhouette, not only colour. Verified when stage 0, 1, 2 are distinguishable in a greyscale screenshot at 50 percent size.

### 8.5 Meteor marker
Already built as a red marker (1.1 s). Art requirements, to keep it in the same look:
- **Shape:** circle ring matching the blast radius 1.7 units (3.4 units = 54 px diameter). Author one 56 x 56 px sprite (3.5 units, 1 px margin) and never scale it.
- **Layers:** ring 2 px Warn.Red, inner hatch (diagonal 2 px stripes, Warn.RedDeep) that fills clockwise over 1.1 s in 8 frames, centre cross 6 x 6 px. A dashed line from the sky to the marker, 2 px wide, dash 4 on / 4 off, scrolling downward.
- **Rock:** 24 x 24 px, dark grey rock (Surface and Secondary) with 3 orange ember pixels on the trailing top-left side, motion streak 2 px in Warn.RedDeep; falls only in the last 0.3 s.
- **Near-miss radius** (4.5 units): shown only by the "CLOSE!" toast, not by a ring (never draw more than one ring).
- **Blast:** 24 x 24, 6 frames, red and orange, DeepInk smoke; **crater:** 12 x 4 px dark patch on the road for 1.0 s.
- Reject if: more than one ring at a time on screen, the marker is gold or yellow, or the marker is tinted by the surface tint (red must always read).

### 8.6 Boost ring (BON-RING)
- **Existing** `Pickup_Ring.png` is 96 x 96 (about 6 units at 16 ppu), high-res; replace with a pixel ring: **36 x 36 px (2.25 units)**: the brief's ring radius 1.1 units means 2.2 units diameter; the hit circle is data.
- Look: gold outer ring 3 px (Reward.Gold), 1 px inner highlight top-left (cream), 1 px Reward.GoldDeep on bottom-right, 1 px DeepInk outline, hollow centre fully transparent. 4 frames of a pulse (ring grows 1 px per side) at 8 fps, plus a 6-frame pass burst (8 star pixels).
- **Chain counter:** numerals in the pixel font of the HUD (UI owns it), gold, shown above the ring for 0.6 s. Chime handled by audio.
- **Overload Ring (RO-B0):** same sprite with a cyan core and a 2 px Bot.Core outer rim so the player can tell the boss ring from the coin ring; both are gold-family shape and cyan accent. The cyan accent is the only difference.
- Reject if: the centre is filled (the player must see through to the road), the ring is red, or it is smaller than 28 px.

### 8.7 (reserved) Coins, hearts, ink canister
Existing coin (128 px), heart (128 px), star and ink canister are not pixel density. Not part of this ask; queue them for the same conversion at 16 x 16 (coin 12 x 12, heart 14 x 12) so the HUD world sprites match. Flag to the team lead as a style debt.

### 8.8 Warning signs
- Used for zone events (S2 signal: sign and tint 40 m before the zone, brief section 4). One sign per event placed beside the road on a post.
- **Format:** 24 x 32 px (1.5 x 2 units): a 2 px post (Secondary), a 20 x 20 px sign plate on top, DeepInk outline, white or cream border 1 px.
| Sign | Plate shape | Colour | Icon (pixel, 10 x 10) |
|---|---|---|---|
| Ice | Diamond | Ice.Mid plate, white border | Snowflake with 6 spokes |
| Mud | Diamond | Mud.Mid plate, cream border | Three wavy lines (splash) |
| Glass | Diamond | Glass.Mid plate, cream border | Crack zigzag |
| Meteor | Triangle point up | **Warn.Red** plate (this is a damage warning) | Rock with 3 streaks |
| Eater warning (edge) | Square with jagged top | Warn.Red | Teeth row |
| Reward (rings) | Round | Reward.Gold | Ring |
- Rule: **shape plus icon plus colour**; icons are shape-readable in greyscale. The ice, mud, glass signs use their surface colours but never red; only danger signs are red (meteor, Eater); gold is reserved for the ring (reward).
- Sign idle animation: 2 frames, a 1 px sway; a "flash" frame (plate fully cream for 1 frame) at S3 for edge cue, 4 flashes in 1.0 s.
- The banner (S1) is a UI element, not world art; its icon reuses the sign icon at 16 x 16.

## 9. Review checklist (art-director sign-off)
A reviewer (art-director) answers every line; a "no" is a rejection with a note. The inspector then runs the legal and technical checks.
**Style**
- [ ] Pixel art only; no smooth, vector or blurred pixels; no semi-transparent AA edge.
- [ ] 16 ppu, point filter, mipmaps off; no non-integer scaling in the scene.
- [ ] Same pixel size inside one sprite (no mixels from resizing).
- [ ] Outline: 1 px DeepInk (2 px for RO-B0 and Road-Eater silhouettes only); selective outline applied.
- [ ] Light from the upper left: highlights top and left, shadows bottom and right.
- [ ] At most 12 colours; every colour from the palette tokens or the VISTA palette; no pure black or pure white fill.
- [ ] Side view, facing right; no tilt.
- [ ] Sits next to VISTA and the road strip on the Countryside world without clashing (check a screenshot at phone size).
**Readability**
- [ ] Silhouette identifies the object at 64 px tall.
- [ ] Red used only for damage and tells; gold only for rewards; ice blue, mud brown, glass cyan-white.
- [ ] Every threat has a shape tell, not colour alone (greyscale test).
- [ ] Tell contrast at least 3:1 against the Countryside world.
- [ ] Crack stages 0, 1, 2 are distinguishable in greyscale.
- [ ] Hit shapes come from data, not from the sprite size.
**Vehicles**
- [ ] Wheel ratio and placement per 3.2; wheel pivot at the centre pixel; hub shows spin.
- [ ] Body 36 to 56 px; one density; max 12 colours.
**Technical**
- [ ] Import settings per section 6 (also for Android override), pivot per sprite type.
- [ ] Sprite sheets have equal frame sizes; atlas page assigned.
- [ ] Draw order per section 2.
**Provenance**
- [ ] Ledger row, `FORGE_NOTES.txt` and script path present; no AI output; sources named.
- [ ] Honest quality rating 1 to 5; below 3 marked `placeholder`.
- [ ] Kids-safe: no gore, no weapons that look real, no scary face for the Road-Eater beyond a toothy maw.
**Verdict values:** `style-approved`, `style-approved-with-notes` (list fixes, may ship), `style-rejected` (list reasons). Style approval is not `ready`; the asset-inspector sets that.

## 10. Review notes (running log)
| Date | Item | Verdict | Notes |
|---|---|---|---|
| 2026-11-15 | Existing slice vehicles (CarBody, SportCarBody, MonsterTruckBody, Wheel) | style-rejected for final | Vector style, 12 px outlines, 480 px wide: do not match 16 ppu pixel world. Allowed as grey-box placeholder only |
| 2026-11-15 | Existing ring, coin, heart, star (96 to 128 px) | style-rejected for final | High-res; convert to pixel (8.6, 8.7) |
| 2026-11-15 | Pickup icons (16x16) | style-approved-with-notes | 16x16 pixel icons with DeepInk outline from `tools/art/make_pickups.py`; match the bible. Ring is the exception (96 px) |
| 2026-11-15 | Strips surface-ice, surface-mud, surface-glass | style-approved-with-notes | Right density and layout; need slice upgrades from 8.4 and an in-game look on the road ribbon; rating 3 of 5 |
| 2026-11-15 | Compression overrides on pixel sprite `.meta` files | open | Android and Standalone overrides show `textureCompression: 1`; inspector to verify in Unity and fix |

## 11. Open questions and risks
1. **Slice vehicles** are the biggest style clash (1.1). Decision needed: convert before the slice look sign-off, or accept placeholder status.
2. **16 vs 15.4 ppu** between gameplay and backgrounds (3.1): check on a device; the alternative changes the camera framing.
3. **Compression:** confirm Android uses uncompressed for pixel sprites.
4. **Frame counts and sizes in section 8 are targets**, not measured; the forge and the first in-game screenshot will test them. RO-B0 and the Road-Eater are the highest-risk pieces for a no-AI pipeline: there is no free side-view asset for either; plan on procedural construction from our own pixel script, or a CC0 monster or robot found by the scout. Any such pack must pass the licence rules first.
5. **Glass crack art** is on the brief's cut list: if cut, keep stage 0 and stage 3 (fall) and drop stages 1 and 2.
6. Nothing in this document has been rendered or imported; all sizes are from file reads (PNG headers) and the brief.
