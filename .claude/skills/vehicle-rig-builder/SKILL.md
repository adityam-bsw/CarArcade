---
name: vehicle-rig-builder
description: Use when creating a vehicle sprite, separating parts, rigging wheels, or adding vehicle variants.
---
# Vehicle Rig Builder

## Procedure
1. Inspect vehicle physics dimensions, wheel positions, pivots, and collision geometry.
2. Generate a true side-view master vehicle facing right.
3. Separate body, front wheel, rear wheel, driver, glass, suspension, and accessories.
4. Ensure body artwork has transparent wheel wells and consistent ground baseline.
5. Create a hierarchy with BodyPivot and wheel pivots aligned to physics wheel anchors.
6. Drive wheel rotation from angular velocity or v/r, never arbitrary animation.
7. Keep Rigidbody2D/colliders authoritative; visual rig follows physics.
8. Create colour/accessory variants from shared parts.
9. Test flips, landings, suspension travel, damage, and vehicle swaps.

## Rules
- Do not alter physics dimensions to fit art without approval.
- No perspective angle changes between vehicles.
- No baked ground shadow.
- No duplicate physics body for a visual transformation.
- Separate art pivots from gameplay collider pivots.

## Checklist
- [ ] Side view faces right.
- [ ] Wheel baseline matches.
- [ ] Pivots align with physics.
- [ ] Rotation follows actual motion.
- [ ] Parts have provenance.
- [ ] Variants remain visually consistent.
