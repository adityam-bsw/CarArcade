---
name: difficulty-tuning
description: Use when tuning event weights, distance progression, hazard difficulty, adaptive difficulty, or challenge fairness.
---
# Difficulty Tuning

## Procedure
1. Define the intended skill being tested.
2. Collect attempt-level data by distance, vehicle, mode, surface and event.
3. Separate player failure from unfair overlap, poor warning, and physics bugs.
4. Tune one variable at a time using fixed seeds.
5. Keep event warnings and counterplay stable while changing intensity.
6. Use breather stages after two high-pressure stages.
7. Compare novice, median and expert profiles.
8. Document the change and rerun regression seeds.

## Rules
- Never hide an unavoidable death inside a difficulty label.
- Never increase difficulty solely by reducing ink without counterplay.
- Maximum two ordinary modifiers simultaneously.
- Boss patterns replace overlapping projectile patterns.
- Adaptive difficulty must be transparent/opt-in if it materially changes challenge.

## Checklist
- [ ] Fixed-seed comparison.
- [ ] Novice and expert tested.
- [ ] No impossible combination.
- [ ] Death causes classified.
- [ ] Difficulty curve documented.
