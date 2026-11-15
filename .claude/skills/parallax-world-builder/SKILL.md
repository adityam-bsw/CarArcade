---
name: parallax-world-builder
description: Use when creating a world background, parallax layers, environment props, or world transitions.
---
# Parallax World Builder

## Procedure
1. Define world palette, horizon, lighting direction, atmosphere, and gameplay readability zones.
2. Create four independent layers: far sky/mountains, mid terrain, near terrain, foreground props.
3. Keep road collision and road rendering separate from decorative parallax.
4. Specify layer dimensions, horizontal tiling/overlap, pivot and scroll multiplier.
5. Create a reusable prop atlas with consistent scale and outline.
6. Implement camera-relative parallax with cached transforms and no per-frame allocations.
7. Add world-specific ambient VFX as pooled effects.
8. Validate at target aspect ratios and on low-end Android.

## Defaults
- Sky: 0.02
- Far: 0.06
- Mid: 0.14
- Near: 0.25
- Foreground: 0.35

## Rules
- Do not bake road geometry into parallax layers.
- No seams at loop boundaries.
- Foreground must not obscure vehicle or warning telegraphs.
- Load only active world assets.
- Reuse common props where palette/material permits.

## Checklist
- [ ] Four depth layers exist.
- [ ] Tiling seam tested.
- [ ] Road readability preserved.
- [ ] Memory budget recorded.
- [ ] Unload path tested.
