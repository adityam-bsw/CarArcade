---
name: enemy-hazard-design
description: Use when designing the Road-Eater, RO-B0 robot, projectiles, bosses, or environmental hazards.
---
# Enemy & Hazard Design

## Procedure
1. Identify the player's readable threat and the intended response: draw, jump, boost, shield, or reposition.
2. Specify spawn conditions, world-space speed, attack range, telegraph, hitbox, cooldown, lifetime, and cleanup.
3. Use simple deterministic movement: pursuit gaps, sine hover, ballistic arcs, bounded homing.
4. Make world variants data-driven; reuse attack patterns and swap projectile definitions.
5. Define projectile-road interaction and prevent multiple persistent effects on one segment.
6. Add boss phases with explicit phase transitions and recovery windows.
7. Add announcer cues with audio priority below critical warnings only when safe.
8. Test every attack at minimum and maximum vehicle speed, during pause, and during restart.

## Rules
- No projectile spawns inside the vehicle hitbox.
- Standard projectile telegraph >= 0.8s; boss attack telegraph >= 1.2s.
- Homing turn rate must be physically evadable.
- Near-miss is measured against the vehicle hitbox, not sprite bounds.
- Despawn hazards on death, reset, world unload, and mode change.
- Shield activation wins same-frame collision resolution.

## Checklist
- [ ] Telegraph matches hit timing.
- [ ] At least one viable counter exists.
- [ ] Projectile pooling is used.
- [ ] Surface interactions are bounded.
- [ ] Boss phase cannot softlock.
- [ ] Voice line never masks warning.
