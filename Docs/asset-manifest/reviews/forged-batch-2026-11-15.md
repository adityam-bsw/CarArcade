# Inspector review: forged batch 2026-11-15

Verdict: 50 ready, 1 left forged (skateboard), 4 left forged (not playable: hovercraft, jet-ski, speedboat, rocket-sled), 0 rejected.

Checked by the inspector itself: pixel.py read of every PNG; seam scores; visual contact sheets (strips tiled 2x at 3x, worlds composited 4 layers, vehicle catalogue previews at 4x); .meta settings compared with CatalogBuilder.ConfigureStrip/ConfigureWorldLayer/ConfigureWheel; LICENCE_EVIDENCE + FORGE_NOTES per folder; ledger rows; AssetLedgerCoverageTests (3/3 passed); console 0 errors.

## Surfaces (23 ready)
All 128x26, seam 0.000, 6-29 colours, Default texture, point, wrapU repeat / wrapV clamp, no mips, uncompressed, alpha transparency (identical to ConfigureStrip output). Quality 3 (cobblestone 3.5). surface-rope-bridge is a quality-2 PLACEHOLDER, accepted: reads as a plank deck with rope marks and tiles cleanly; replace later.

## Worlds (15 ready)
All layers 384x216, Sprite, 16 ppu, point, repeat, no mips, uncompressed. Seams 0.000-0.067 (cloud-kingdom mid 0.067, underwater mid 0.057 / sky 0.039 were viewed tiled: no visible seam). Nine worlds had no forge rating; inspector rating 3 (summer, spring-blossom 4). Style sits next to VISTA.

## Vehicles (12 ready)
bicycle, golf-cart, handcar, monster-truck, moon-rover, quad-atv, shopping-cart, sled, snowboard, snowmobile, toboggan, tuk-tuk. All face right, 28-53 px wide previews, consistent density. Game uses Catalog/Previews and Catalog/Wheels (all point, 16 ppu, uncompressed). The Forged source PNGs (59) were bilinear/compressed/100 ppu; reimported through the Editor (Sprite, point, 16 ppu, uncompressed, no mips). Runner vehicles and handcar have no wheel cut-outs by design (find_wheels SKIP).
Honest notes: bicycle frame is thin and un-outlined; moon-rover is busy; sled/toboggan riders are generic blocks. All still quality 3.

## Left forged
- vehicle-skateboard: the new two-wheel forge is fine (q3), but the game still shows the old wheelless red board: tools/assets/make_previews.py maps skateboard to Vehicles/Skateboard/skateboard.png and find_wheels.py SKIPs it. Fix the mapping (forge/main session), regenerate previews and the catalogue, re-inspect.
- hovercraft, jet-ski, speedboat, rocket-sled: not playable in game, not reviewed this round.
