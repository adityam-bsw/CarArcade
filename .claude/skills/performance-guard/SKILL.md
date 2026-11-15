---
name: performance-guard
description: Use when implementing or reviewing gameplay, art, VFX, UI, memory, allocations, loading, or Android performance.
---
# Low-End Android Performance Guard

## Procedure
1. Identify the minimum target device/GPU/RAM and Unity build configuration.
2. Profile a development build on physical hardware.
3. Capture CPU, GPU, memory, GC allocations, draw calls, batches, overdraw and load times.
4. Inspect per-frame allocations in Update, FixedUpdate, Tick, collision and UI callbacks.
5. Pool projectiles, road segments, particles and repeated UI items.
6. Use Sprite Atlases and platform texture compression.
7. Load one world at a time; release previous world assets.
8. Test worst-case stage stacking, many particles, long road history and background/resume.
9. Record before/after measurements.

## Rules
- Target 60 FPS; define a documented fallback quality profile.
- Zero recurring managed allocations in hot gameplay loops.
- No runtime texture creation for ordinary effects.
- Avoid one Canvas per element.
- Do not optimize from Editor-only measurements.
- Do not claim performance success without device profiling.

## Checklist
- [ ] Physical device tested.
- [ ] Hot-loop allocations inspected.
- [ ] Memory budget recorded.
- [ ] Worst-case VFX tested.
- [ ] World unload verified.
- [ ] Frame-time distribution recorded.
