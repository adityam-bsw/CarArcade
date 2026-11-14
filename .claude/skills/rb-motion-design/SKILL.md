---
name: rb-motion-design
description: Motion and game-feel design for Road Builder Racer - what should animate, durations, easing choices, the 12 animation principles applied to a mobile racer, feedback recipes (coins, crash, boost, new best, mode switch), choosing DOTween vs Animator vs particles, and a motion checklist. Use when something feels flat, stiff, slow, or floaty, when adding any new feedback moment, or before animating a screen.
---

# Motion design

Animation is the difference between "a prototype" and "a game that feels expensive". Implementation
details are in `rb-dotween`; sound pairing in `unity-audio-juice`.

## Pick the tool

| Case | Tool |
|---|---|
| UI transitions, counters, popups, punch/shake, hit-stop | **DOTween** |
| Looping character/prop states with many clips (wheels, mascot idle) | Animator or frame sprites |
| Bursts, trails, dust, sparks, confetti, speed lines | **Particle System** (pooled, `ParticleManager`) |
| Skeletal characters | Spine (`unity-spine-animation`), only if we adopt it |
| Shader effects (eraser edge, glow, dissolve) | Shader Graph / material property tween |

## Timing table (feels right on a phone)

| Moment | Duration | Easing |
|---|---|---|
| Button press feedback | 80-120 ms | OutQuad, squash to ~0.9 |
| Popup in | 250-350 ms | OutBack (small overshoot) |
| Popup out | 150-200 ms | InCubic (leave faster than enter) |
| Screen slide | 300-400 ms | OutCubic / InOutCubic |
| Number count-up | 600-1200 ms | OutQuad |
| Coin fly to counter | 500-700 ms | InOutQuad + counter punch on arrival |
| Idle pulse / bob | 0.8-1.5 s loop | InOutSine |
| Hit-stop on impact | 40-90 ms | timeScale ~0 |
| Camera shake (crash) | 200-350 ms | decaying, small amplitude |

If something takes over 400 ms and blocks input, it is too slow. Let the player skip/interrupt.

## The principles that matter here

1. **Anticipation and follow-through:** a button dips before it springs; a popup overshoots then
   settles. Use sparingly on gameplay so it never delays input.
2. **Squash and stretch:** small, 5-15%, on buttons, coins, the car landing. Volume feels preserved.
3. **Easing is personality:** Out = arrives, soft and friendly. In = leaves. InOut = ambient loops.
   OutBack/OutElastic = rewards only. Never linear, except constant-speed scrolling.
4. **Stagger:** list items and stats enter 30-60 ms apart so the eye follows.
5. **Secondary motion:** things react to things (the counter punches when the coin lands).
6. **Exaggerate, then tune down:** build at 150% strength, then reduce until it stops annoying.
7. **Consistency:** one set of durations and eases for the whole game (the table above), per-mode
   flavour changes the *feel* (Void Run = scratchy, snappy; Moon = slow, floaty), not the system.
8. **Direction tells meaning:** forward/right = progress, up = reward, shake = danger.

## Feedback recipes (visual + audio + haptic, all within 100 ms)

- **Coin pickup:** pop scale + sparkle burst + pitch-rising ping (pitch climbs with a combo) +
  coin flies to HUD, counter punches, light haptic.
- **Draw stroke:** soft scratch loop, volume follows speed, pencil-dust particles at the tip.
  Ink bar drains smoothly and flashes when under 20%.
- **Boost / hop:** stretch the car, speed-line particles, camera FOV/size nudge out then back,
  whoosh.
- **Crash / life lost:** hit-stop, camera shake, flash, heart shatters, slow-mo 0.3x for 0.4 s,
  then fast respawn.
- **Near miss (barely clears a gap):** brief slow-mo, "close!" pop text, whoosh.
- **New best:** pause, screen dim, number overshoot, confetti, jingle. The biggest moment in the game.
- **Mode switch (Menu):** world crossfade with parallax slide, card scales up, theme colour wipe.
- **UI reward (chest, unlock):** shake anticipation, burst open, items pop out staggered.

## Mobile performance guardrails

- Animate `transform`, `CanvasGroup.alpha` and colour. Avoid animating layout-driven sizes
  and anything that dirties a big Canvas every frame; put frequently moving UI on its own
  sub-Canvas.
- Pool particles and floating text. No `Instantiate` on pickups.
- Cap simultaneous tweens/particles; cut effects first on low-end (quality tier).
- Provide a **Reduce motion** setting that disables shake, flash, slow-mo.

## Motion checklist

- [ ] Every input has instant feedback (<100 ms)?
- [ ] Entrances slower than exits, exits interruptible?
- [ ] Only one thing demands attention at a time?
- [ ] Easing and durations match the table (or a justified exception)?
- [ ] Looks right at 30 fps and on a low-end phone?
- [ ] Sound lands on the exact frame of the visual?
- [ ] Reduce-motion respected?
