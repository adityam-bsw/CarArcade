---
name: surface-behaviour-modules
description: Use when creating or modifying road surface gameplay behaviours.
---
# Surface Behaviour Modules

## Procedure
1. Define one signature behaviour that changes player decisions.
2. Separate static SurfaceDefinition data from per-segment runtime state.
3. Implement the behaviour as a small module with explicit lifecycle callbacks.
4. Define wheel contact, vehicle contact, projectile impact, monster contact, and segment destruction responses.
5. Add compatibility validation and merge rules (e.g. water + lava => obsidian).
6. Add visual/audio feedback and a three-second tutorial hint.
7. Test light, heavy, fast, slow, shielded, and airborne vehicles.
8. Profile module ticking and collision queries on a low-end Android target.

## Rules
- Never store mutable timers/durability in shared ScriptableObjects.
- No giant switch statement keyed by surface type.
- Tick noncritical modules at 20Hz where sufficient.
- Cache module references; no allocations in Tick.
- Limit persistent effects per segment.
- Every surface needs a counterplay and clear feedback.

## Checklist
- [ ] Signature behaviour is visible.
- [ ] Static data and runtime state are separated.
- [ ] Vehicle affinity is documented.
- [ ] Robot/monster interactions are defined.
- [ ] Compatibility validator passes.
- [ ] Tutorial hint is <= 3 seconds.
