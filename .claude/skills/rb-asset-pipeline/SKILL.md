---
name: rb-asset-pipeline
description: Get art, audio, fonts and icons for Road Builder Racer without a design team - decide build vs download, find free commercially safe assets, verify licences, log them in the ledger, and import them for mobile. Use whenever the game needs a sprite, background, icon, UI skin, sound, music, font or VFX, or when replacing placeholder art.
---

# Asset pipeline (no design team)

## 1. Decide: generate in code, download, or hand-build?

| Need | Default route |
|---|---|
| Flat shapes, UI panels, bars, gradients, particles, backgrounds | **Build**: procedural sprite/mesh or 9-slice, one palette from `UITheme`. Cheapest and always consistent |
| Icons, buttons, UI frames, vehicles, props | **Download** a CC0 pack, then recolour to our palette |
| Sound effects | **Download** CC0 (Kenney audio, CC0 freesound clips). Synth only for simple blips |
| Music | **Download** CC0/CC-BY loops, or compose short loops with a free tracker. Credit if CC-BY |
| Fonts | Google Fonts (SIL OFL) only |
| Hero art (logo, store icon, feature graphic) | Build by hand in code/vector, never a stock pack, so it is ours and trademarkable |

Style first: pick **one** style per mode (e.g. Void Run = pencil on paper) and only take assets that
fit it. Mixed styles are the fastest way to look amateur.

## 2. Where to look (check each asset's own page for its licence)

- **Kenney.nl**: CC0 art and audio packs, the safest default.
- **OpenGameArt.org**: mixed licences per item (CC0, CC-BY, CC-BY-SA, GPL). Check every one.
- **itch.io (free assets, filter by licence)**: licence set per creator.
- **Google Fonts**: OFL, free for commercial use and embedding.
- **Freesound.org**: filter to CC0; CC-BY needs credit; reject CC-BY-NC.
- Verify on the original creator page, not a "best free assets" blog.

## 3. Licence gate (do this before download)

- OK without credit: **CC0 / public domain**.
- OK with credit in the in-game Credits screen: **CC-BY**, **OFL** fonts (keep licence file).
- **Reject:** any **NC** (non-commercial), **ND**, GPL art, "free for personal use", ripped game
  assets, AI-generated assets with unclear terms, anything with no stated licence.
- **CC-BY-SA:** avoid; it can force our derivative art to be share-alike.

Downloading is an outward action: **state the file name, source and size to the user and get a
yes first.** Download each pack into its own empty folder outside `Assets/`, inspect it, then
import only what is used (never whole packs; build size matters).

## 4. Ledger (mandatory)

Append one row per asset or pack to `Docs/ASSET_LEDGER.md`:
`| file(s) in project | source URL | author | licence | credit text needed? | date | used in |`
The Credits screen is generated from this file.

## 5. Import for mobile

- Folder: `Assets/Art/<Mode or Shared>/...`, `Assets/Audio/{Sfx,Music}/`, `Assets/Fonts/`.
- Sprites: Sprite (2D), Pixels Per Unit set once per style, **no mipmaps**, max size power-of-two
  when atlased, compression ASTC (Android), pack into a SpriteAtlas per mode.
- UI 9-slice: set borders in the Sprite Editor, use Sliced mode, keep sources at 2x and test at 4:3,
  16:9, 21:9 and portrait.
- Audio: SFX = Decompress On Load, Vorbis, mono, 22-44 kHz. Music = Streaming, Vorbis, stereo.
  Normalise loudness, trim silence at the start (late sounds feel broken).
- Fonts: create TMP SDF atlases; static atlas for digits, dynamic for the rest.
- Rename to our convention (`btn_primary_normal`, `sfx_coin_01`). Move files **inside Unity** so
  GUIDs survive (see `unity-asset-safety`).

## 6. Recolour and unify

Run every downloaded sprite through the mode palette (tint via material or re-export), match outline
weight, and check it on the real background. If it needs more than a recolour, pick another asset.

## Done means

Ledger row added, licence verified on source page, atlas/compression set, budget checked
(texture memory and APK size), asset used somewhere real (no orphan files).
