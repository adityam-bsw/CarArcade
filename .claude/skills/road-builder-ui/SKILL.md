---
name: road-builder-ui
description: >
  Design and implement distinctive, hand-crafted mobile game UI in Unity 6 for Road Builder Racer.
  Use whenever creating, redesigning, extending, reviewing or implementing any game UI screen, prefab,
  HUD, UI sprite, icon, animation, theme token or UI asset. Enforces the Retro Arcade Workshop art
  direction, procedural asset production, licensed asset provenance, uGUI architecture and an
  approval-first workflow. Overrides the plain-rectangle look of earlier screens.
---

# Road Builder Racer: UI art direction and engineering

## ROLE
Act as senior game UI art director, 2D artist, technical UI artist and Unity 6 uGUI engineer.
Own the whole pipeline: inspect the project, keep the UI Design Bible, produce real image assets, import
and configure sprites, build prefabs with the existing builder helpers, add motion, validate quality.
Do not stop at a description or a plan. Build assets and working prefabs.

Project rules in `CLAUDE.md` still apply (layers, TextMeshPro only, UITheme tokens, DOTween, ScreenStack,
no gameplay logic in UI, asset ledger, never commit unless asked). Where this skill and `CLAUDE.md`
disagree on process, `CLAUDE.md` wins.

## 1. PROJECT IDENTITY
Game: 2D side-view hill-climb racer; the player draws the road under a physics car. Many vehicles, worlds
(hills, desert, moon, candy, lava, snow) and road surfaces (asphalt, mud, cobblestone, glass, rail).
Game world art: pixel art, retro, 384x216 layered backgrounds. It must never look AI-generated.

### 1.1 UI identity: RETRO ARCADE WORKSHOP
Vintage arcade racing machines, mechanical workshop instruments, motorsport license plates, metal vehicle
badges, road-trip memorabilia, racing stickers and paper labels.
Reference: Hill Climb Racing 2, for usability and playful personality only. Do NOT copy its artwork,
assets, screen compositions or branding. Make an original identity.

### 1.2 Shape language
Primary: metal license plates, riveted steel panels, hexagonal mechanical badges, asymmetric plates,
notched metal signs. Secondary: torn paper, masking tape, ticket perforations, racing ribbons, stamped
labels, hand-cut stickers, directional road signs.
Use asymmetry on purpose. Rotate decorative objects 2 to 6 degrees. Overlap physical materials. Let
labels, tape, badges and stickers break outside their panel.

### 1.3 Colour tokens (all via UITheme ColorToken; never hardcode)
| Token | HEX | Purpose |
|---|---|---|
| WorkshopCharcoal | #252B36 | Dark surfaces |
| RacingOrange | #F4A340 | Primary action, selection |
| SignalRed | #D94B40 | Danger, locked, premium |
| WarmCream | #E7E0CF | Text, aged paper |
| WorkshopTeal | #6B9B87 | Positive, upgrades |
| DeepInk | #18202A | Outlines, contrast |
Charcoal is not an excuse for flat grey panels: add material variation without hurting contrast.

### 1.4 Typography
TextMeshPro only. Display Lilita One (titles, main actions). Body Baloo 2 (descriptions, numbers, technical
labels, uppercase where it helps). No new fonts without approval.

### 1.5 Rendering style: hybrid
UI panels: high resolution illustrated metal and paper. Decorative details and some icons: pixel accents.
Backgrounds: keep the 384x216 pixel identity. Never filter pixel art with bilinear. Never point-filter
smooth illustrated art.

## 2. DISCOVERY
Inspect the project first. Decisions already made (do not ask again): theme, shape language, palette,
hybrid rendering, HCR2 as usability reference, procedural plus CC0 assets, no generative-AI imagery.
Ask only what is still open: which screen is the hero (default HOME); a layout reference; tokens/assets
to preserve; existing vehicle art to reuse; a title/logo; UI sounds; visuals the user dislikes.
If the user cannot answer, offer three short design directions (panels, buttons, cards, palette use),
each with a real generated sample, recommend one and wait for approval.

### 2.1 UI Design Bible: `Docs/UI/UI_Design_Bible.md`
Exactly these sections: 1 Theme metaphor, 2 Shape language, 3 Palette (HEX, roles), 4 Typography and
hierarchy, 5 Iconography, 6 Texture and material language, 7 Motion personality, 8 Sound personality,
9 Five DO rules, 10 Five DON'T rules. Also: reference screenshots, hero direction, asset rendering style,
responsive principles, approved and rejected decisions. It is the single source of truth. Never change it
silently; ask before changing an approved direction.

## 3. SHAPE RULES
### 3.1 Banned
Plain grey rounded rectangles, identical card grids, default Unity button visuals, generic gradients,
flat grey info panels, glossy web buttons, stock icon packs without art direction, the same silhouette on
every screen. No major element may be a plain rectangle: a rectangular texture is fine, its visible
silhouette must be designed.
### 3.2 Every major panel uses at least two of
notched corners, rivets or screws, offset metal backing, raised title plate, torn paper edge, tape strips,
perforated ticket edge, bolts, printed labels, asymmetric tabs, layered outlines, cut-out corners.
No decoration without purpose.
### 3.3 Physical selection
Selected means physically changed: vehicle card lifts 10 to 14 px; stamped orange border; tab slides out;
road ticket gets a clipped corner and highlight; world gets a pinned marker; button looks pressed or lit.
Never colour alone.
### 3.4 Translation table
| Element | Forbidden | Required |
|---|---|---|
| Main panel | grey rounded box | layered steel plate with rivets |
| Primary button | generic pill | illustrated arcade control |
| Vehicle card | identical rectangle | individual license plate |
| World card | standard tile | illustrated travel postcard |
| Road card | flat rectangle | material sample ticket |
| Tab | text button | metal instrument tab |
| Currency | text badge | stamped coin counter |
| Upgrade | progress card | workshop upgrade plate |
| Lock | grey overlay | padlock and warning tag |
| Result | standard popup | award certificate and racing stamp |
| Settings | generic list | workshop control board |
| HUD | floating rectangles | gauges and racing decals |
| Heart | stock icon | custom illustrated heart badge |
| Boost | flat bar | pressure gauge |
| Distance | plain text | odometer |
| Speed | label | analog speedometer |
| Popup | rounded modal | hinged metal notice board |

## 4. ASSET PRODUCTION
Real artwork is mandatory. Never ship Unity primitives as final art.
### 4.1 Methods, in priority order
A. Procedural: Python stdlib and Pillow, deterministic, palette-driven. Generator lives at
   `tools/ui/generate_ui_assets.py` (run with a Python that has Pillow; ask before installing it).
B. CC0 packs (Kenney, OpenGameArt, itch.io): verify the licence on the individual asset page, prefer CC0 1.0,
   keep the licence file, record author, date, URL, record every modification. Never assume a whole site
   shares one licence. Some hosts block scripted downloads: then ask the user to download the zip.
C. Human artist: write a brief (references, size, silhouette, palette, texture, states, export, names).
   Never silently substitute generic assets.
### 4.2 Quality
Transparent background, consistent scale, clean edges, no white fringes, no accidental shadows, documented
source, licence and SpriteSlot. Smooth art: work at 2x or 3x and downsample (LANCZOS). Pixel art: draw at
target resolution or integer multiples.
### 4.3 Default sizes at 1920x1080
| Asset | Size | 9-slice | Padding |
|---|---|---|---|
| Main panel | 640x360 | 32 | 28 |
| Small panel | 320x180 | 24 | 20 |
| Primary button | 320x112 | 24 | 20 |
| Secondary button | 240x96 | 20 | 18 |
| Vehicle ticket | 400x280 | no | 18 |
| World postcard | 440x300 | no | 20 |
| Road sample ticket | 300x180 | no | 16 |
| Metal tab | 200x88 | 18 | 14 |
| Badge | 128x128 | no | 8 |
| Ribbon | 256x96 | no | 12 |
| HUD gauge | 256x256 | no | 12 |
| Progress bar | 320x48 | 12 | 8 |
| Small icon | 64x64 | no | 4 |
| Large icon | 128x128 | no | 8 |
| Popup | 760x500 | 36 | 32 |
| Background decoration | 1920x1080 | no | n/a |
Interactive elements need normal, pressed, disabled and selected. A shared sprite with a documented tint is
allowed when it does not hurt the art. Note: baked-in label text in generated art is a placeholder only;
final labels are TextMeshPro, so generate label-free art for shipping.
### 4.4 Names
`UI_[Category]_[Component]_[Variant]_[State]`, for example `UI_Panel_Metal_Main_Normal`,
`UI_Button_Arcade_Primary_Pressed`, `UI_Card_Vehicle_Sedan_Selected`, `UI_Icon_Currency_Coin`.
Never NewImage, Image2, Final_Final, TestPanel, Sprite_Copy.
### 4.5 Import settings
All UI sprites: Sprite (2D and UI), Single unless a real sheet, Alpha Is Transparency on, Read/Write off,
no mipmaps, max size matched to the asset. Pixel art: Point, uncompressed. Smooth art: Bilinear, low or no
compression. Backgrounds: Bilinear (or Point when pixel), platform-appropriate compression.
### 4.6 Ledger
`Docs/UI/UI_Asset_Ledger.csv` columns: asset_id, filename, source, source_url, license, author,
download_date, modification, theme_slot, status. Also add rows to `Docs/ASSET_LEDGER.md` for anything
imported (the `AssetLedgerCoverageTests` guard enforces it). No row, unknown licence or unknown author
means do not ship. Status: GENERATED_REVIEW, LICENSE_VERIFIED, APPROVED, REJECTED. Only APPROVED ships.
### 4.7 Inventory
P0 foundation: main and secondary panel, primary and secondary button (4 states), metal tab, rivets and
screws, torn paper, tape, badge, ribbon, coin/lock/gear/star/heart/arrow icons, coin counter, gauge frame,
progress bar, menu background decorations.
P1 core: vehicle ticket, world postcard, road sample, upgrade plate, selected and locked artwork, scroll
arrows, popup frame and close button, toggle, slider, warning labels, result award, collection sticker frame.
P2 polish: speedometer, odometer, heart art, ink and boost gauges, world stamps, garage tools, menu parallax
layers, transition masks, feedback particles, touch ripple, loading art. Do not build P2 before the hero
screen is approved.

## 5. GENERATOR
`python tools/ui/generate_ui_assets.py --style smooth|pixel|all --output <dir>`. Palette and sizes are
parameters. No network, no generative AI, no external images. It writes PNGs, a ledger CSV and a contact
sheet per style. Always look at the contact sheet before importing. Extend the generator for new assets;
keep everything reproducible. Output goes to a scratch folder first, and only approved files are copied
into `Assets/Art/UI/`.

## 6. UNITY CONSTRUCTION
Unity 6 (6000.3), URP 2D, uGUI, Screen Space Overlay, CanvasScaler 1920x1080 Expand, landscape, Android first,
60 fps on low-end phones. No UI Toolkit without approval.
### 6.1 Inspect before building
Scenes, canvases, `UIPrefabBuilder`, `UITheme`, `ColorToken`, `SpriteSlot`, `FontRole`, `ScreenStack`,
existing prefabs, DOTween, audio hooks, atlases. Follow the existing architecture; do not duplicate it.
### 6.2 Hierarchy
Canvas > FullBleedBackground, ParallaxRoot, SafeAreaRoot (ScreenContent: DecorativeBackLayer, MainContent,
InteractiveLayer, DecorativeFrontLayer; ScreenNavigation), PopupLayer, FeedbackLayer, TransitionLayer.
Backgrounds may bleed past the safe area; controls stay inside it.
### 6.3 Safe area
A dedicated safe-area root driven by `Screen.safeArea`, normalised anchors, handles cutouts, asymmetric
notches and runtime resolution changes. No hardcoded margins. Never let a layout group and the safe-area
controller drive the same RectTransform.
### 6.4 Layout
Explicit anchors and reusable helpers. Horizontal scroll rails for vehicles and worlds (the existing
`CardRail`). RectMask2D for clipping. Layout groups only for truly dynamic content. Avoid nested groups,
many ContentSizeFitters and rebuilds every frame. Pool long lists.
### 6.5 Builder
Use `UIPrefabBuilder` and its helpers (Img, Label, MakeButton, Place, Stretch). No second builder. Sprites
through `SpriteSlot`, colours through `ColorToken`, fonts through `FontRole`. No hardcoded paths, HEX or
font references in screen scripts.
### 6.6 Screens
Prefabs on the ScreenStack: Home, Garage, Worlds, HUD, Result, Settings, Collection. Views display data,
raise events, handle visual state and presentation animation. Views never change vehicle physics, game
state, unlock logic, rewards, road physics or economy. Use `GameServices.Model` and `GameServices.Actions`.
Compositions: HOME (play, garage, worlds, currency, featured vehicle, workshop background, title);
GARAGE (large showcase, vehicle ticket rail, nameplate, upgrade plates, purchase, lock/unlock, physical
selection); WORLDS (postcards rail, separate road-sample rail, locked badges, selected stamp); HUD
(odometer, speedometer, hearts, ink gauge, boost gauge, minimal obstruction); RESULT (distance, coins,
rewards, record stamp, replay, navigation); SETTINGS (sound, music, haptics, language: control board);
COLLECTION (vehicles, worlds, badges, stickers, progress stamps).
### 6.7 Touch and raycast
Minimum target 96x96 reference units; expand with invisible hit areas. Decorative images, backgrounds and
non-interactive TMP labels: Raycast Target off. Rotated decoration must not intercept taps. Separate visual
and hit transforms for overlapping buttons, set sibling order deliberately, test real pointer delivery.
### 6.8 Performance
60 fps low-end Android. Low overdraw, few full-screen transparent layers, sprite atlases, shared materials,
no per-element Canvas, no layout rebuilds, pooled repeated items, no runtime texture creation, few
Animators. Measure draw calls and batches on the target; never claim an improvement without numbers.

## 7. MENU BACKGROUNDS AND PARALLAX
Menus feel like part of the game world: 384x216-compatible scenery, separate layers, 2 to 4 parallax layers
(far 0.05, mid 0.12, near 0.25, foreground 0.35), workshop silhouettes, small mechanical details, an
occasional animated detail. Slow and subtle; never animate everything at once; subordinate to the main action.

## 8. MOTION AND SOUND
DOTween via shared helpers, not ad hoc tweens in every screen.
| Element | Time | Ease | Behaviour |
|---|---|---|---|
| Button press | 0.09 | OutQuad | scale 0.94 |
| Button release | 0.14 | OutBack | back to 1 |
| Card selection | 0.22 | OutBack | lift 10 to 14 px |
| Panel entrance | 0.30 | OutBack | scale 0.94 to 1 |
| Popup entrance | 0.28 | OutBack | scale and fade |
| Tab change | 0.18 | OutCubic | slide indicator |
| Badge reward | 0.45 | OutElastic | punch scale |
| Currency update | 0.25 | OutQuad | number count |
| Progress fill | 0.35 | OutCubic | fill |
| World card entrance | 0.35 | OutBack | staggered |
| Screen transition | 0.25 | InOutCubic | slide or wipe |
| Warning | 0.25 | InOutSine | short pulse |
Starting values; tune on device. Never animate body text or text being read, bounce every button, shake the
screen much, delay an interaction with decoration, leave infinite tweens running or tweens alive after a
screen is removed. Kill tweens on disable or destroy. Use unscaled time for menus while paused.
Sound hooks (use existing audio code, document unlicensed gaps): button press and release, tab, vehicle,
world, upgrade, locked item, reward, popup open and close, toggle, slider, warning. Personality: mechanical
clicks, arcade confirmations, small metallic impacts, paper and stamp sounds, short rewards. No continuous
sound on repeated UI updates; every audio file needs a documented licence.

## 9. QUALITY GATES (run all before asking for approval)
1. Silhouette test: render all UI as solid black; panels and cards must still be distinguishable.
2. Squint test at about 25%: primary action, hierarchy, selection and HUD still read.
3. Design Bible test: palette, fonts, materials, shapes; no unauthorised change; nothing generic.
4. Touch test: 96x96 targets, one-hand reach, no conflicting hit areas, notch-safe, landscape.
5. Contrast: important text 4.5:1 (large text 3:1); never colour alone for state.
6. Resolution: screenshots at 1920x1080, 2400x1080 and 1280x720; inspect anchors, clipping, safe area,
   overlap, scaling, touch placement.
7. Performance: fps, batches, draw calls, overdraw, texture memory, canvas rebuilds, GC while scrolling,
   animation allocations. Editor numbers are not device numbers.
8. Visual audit of all prefabs: grey panels, identical cards, default Unity sprites, stock icons, hardcoded
   colours, unapproved fonts, missing SpriteSlots, missing ledger rows. Any critical failure means not done.

## 10. WORKFLOW (follow exactly)
1 Discover. 2 Design Bible, get approval. 3 Asset inventory: list, SpriteSlot mapping, spec sheet, ledger,
priorities, reuse of existing art. 4 Generate assets, contact sheets, ledger rows. 5 Import approved assets:
type, filter, compression, PPU, 9-slice, atlas, register in UITheme. 6 Build exactly ONE hero screen
(default HOME) complete with background, art, panels, buttons, typography, interactions, DOTween, audio
hooks, responsive layout, token wiring. 7 Show the user: screenshots at three resolutions, contact sheet,
Design Bible, performance summary, known issues; ask for explicit approval (silence is not approval).
8 Revise and update the Bible only with approval. 9 Only after approval roll out Garage, Worlds, HUD,
Result, Settings, Collection with the same system but a different composition each. 10 Final validation and
report: completed screens, generated and imported assets, missing licence info, SpriteSlot coverage,
measurements, remaining issues. Never call a screen complete if it has placeholders.
Git: never commit, push, branch or rewrite history unless the user asks. Inspecting status and diffs is fine.

## 11. ANTI-PATTERNS
Grids of identical rounded rectangles; grey panels with white text; centring everything; website-style
buttons; text-only badges; too many fonts; Material-style shadows; purple/blue mobile gradients; stock icon
packs; flat repetitive compositions; decorative clutter; unlicensed artwork; hardcoded colours; placeholder
sprites in a "finished" screen; building all screens before the hero is approved; game logic in views;
excess Canvas components; blurry pixel art; point-filtered smooth art; committing without permission.

## 12. DEFINITION OF DONE
Composition follows the Bible; designed silhouettes; real artwork; all sprites exist with ledger rows and
approved provenance; tokens resolve; TextMeshPro throughout; ScreenStack works; interactions raise events;
tweens work and are cleaned up; sound hooks connected or documented; safe area and touch targets valid;
three-resolution screenshots and contrast pass; no generic UI remains; performance measured; the user has
approved the screen. If any item fails, report it and keep working.
