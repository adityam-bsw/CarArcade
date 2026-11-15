---
name: vfx-juice
description: Use when implementing impact feedback, near misses, road damage, rewards, transitions, particles, or DOTween juice.
---
# VFX & Juice

## Procedure
1. Identify the gameplay event and the information the effect must communicate.
2. Separate critical telegraph VFX from decorative celebration VFX.
3. Use pooled particles and reusable DOTween sequences.
4. Specify duration, scale, opacity, screen-space/world-space, sorting and audio hook.
5. Ensure VFX do not hide road, wheels, projectile telegraphs or touch targets.
6. Add low-quality settings for particle count, shader complexity and screen shake.
7. Kill tweens and return pooled effects on reset/unload.
8. Test readability at 30 FPS and small phone screens.

## Rules
- Critical warning colour/shape must remain readable without sound.
- Near-miss slow motion must not interfere with required input.
- No unbounded particle spawning.
- No per-frame allocations.
- Screen shake is brief and intensity-capped.
- Respect reduced-motion/accessibility settings where implemented.

## Checklist
- [ ] Effect communicates a specific event.
- [ ] Pooling used.
- [ ] Cleanup tested.
- [ ] Low-end quality mode exists.
- [ ] Gameplay visibility preserved.
