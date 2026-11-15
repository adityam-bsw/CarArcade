# Hazard Spec: Road-Eater, RO-B0, projectiles, surface rules

Owner: hazard-designer. Status: PROPOSED (design only; nothing built, nothing measured). Date: 2026-11-15.
Reconciled with `VERTICAL_SLICE_BRIEF.md` (source of truth, section 13) on 2026-11-15; where this file and the brief differ, the brief wins.
Replaces the hazard tasks in `VERTICAL_SLICE_BRIEF.md` section 10 (the brief asked for `HAZARD_SPEC.md`; the lead asked for this
file name). Read with the brief F0 to F4 and `GAMEPLAY_RULES.md`. All numbers are first guesses for ScriptableObjects, never code.
Every "expected outcome" below is a design target to be checked by the acceptance tests in section 9; none is a measured result.

## 0. Decisions that override or refine the brief
| # | Brief said | This spec | Why |
|---|---|---|---|
| D1 | Eater initial gap 40 m, speed 85 to 92 percent of car | Spawn 32 m behind, hold a target gap of 18 m, speed capped at 1.25 x vehicle speed (gap-based controller, 4.2) | Lead instruction; a controller makes the gap the thing the player feels |
| D2 | Escape at 1700 m | Escape = gap of at least 45 m held for 8 s. Timeout retreat at 1750 m with half payout (75 coins first time, 38 repeat) so the chase never lasts forever | Lead instruction; fallback avoids an unwinnable chase |
| D3 | RO-B0: 3 hits / survive 10 waves | No HP. A counter meter ("Overload", 0 to 100) fills by dodging; three boss phases by meter; 5 reusable attack patterns | Lead instruction |
| D4 | Telegraph at least 1.0 s | Floor 0.8 s for any hazard, 1.2 to 2.0 s for every boss telegraph | Lead instruction |
| D5 | Projectiles fixed | Three behaviour archetypes (bolt, bomb, roller) with a world skin table; the slice ships the Countryside skin only | Lead instruction ("world-specific projectiles") |
| D6 | Glass cracks after the rear wheel leaves | Piece arms on front-wheel entry, hairline at 0.8 s, web at 1.6 s, drop at 2.6 s dwell OR 0.6 s after the rear wheel leaves | Brief text ("hesitation = fall") contradicted its own rule: a piece that only falls after you leave never punishes hesitation |
| D7 | Mud drag 1.0 m/s per s to a floor of 3.0 | Drag acts on the target speed and is capped at 2.0 m/s of extra reduction (section 3.2); the vehicle `mudDragMul` scales the cap | Uncapped drag would make a 150 m zone take about 50 s |
| D8 | Shield absorbs crash | Shield beats every damage source here (bite, bolt, bomb, roller, meteor) and grants 1.5 s of invulnerability | Lead instruction ("shield beats damage") |

Units: metres (m) are path length along the road unless said "world"; seconds (s) use scaled game time (pause and slow-mo stretch a
telegraph, never shorten it). Speeds in m/s. "Car speed" `v_car` is the car's current measured forward speed.
Hazards test hits against the hurtbox shrunk to 85 percent (generosity); near-miss tests use the full hurtbox.

### 0.1 Real car numbers (read from `Assets/Scripts/Core/RoadCar.cs` on 2026-11-15; replaces the assumed 2.2 x 0.9 m hurtbox and 2.5 m wheelbase)
Facts from code, not measured in play. Built-in cars (`Build(position, VehicleType)`), body root at the sprite centre, metres:
| Car | Sprite w x h | Body collider (polygon, 0.04..0.96 w, 0.28..0.95 h) | Wheel radius | Wheelbase (back to front axle) | Whole-car box incl. wheels and roof sensor |
|---|---|---|---|---|---|
| ClassicRed | 2.4 x 0.9 | 2.21 x 0.60 | 0.42 | 1.44 (anchors +-0.72) | 2.28 x 1.43 |
| SportCyan | 2.4 x 0.8 | 2.21 x 0.54 | 0.40 | 1.40 (+-0.70) | 2.21 x 1.32 |
| MonsterTruck | 2.6 x 1.1 | 2.39 x 0.74 | 0.52 | 1.56 (+-0.78) | 2.60 x 1.67 |
Catalogue vehicles (`Build(position, VehicleRig)`): width = catalogue `widthUnits`, height = width x sprite aspect, wheel radius =
clamp(0.3 x height, 0.22, 0.50). Without cut-out wheels the wheelbase is 0.60 x width (anchors at +-0.30 w); with cut-out wheels the outer two
drawn wheels define it. Collider and roof sensor use the same normalised polygon, so body length is about 0.92 x width.
- **Hurtbox to use:** the body polygon (about 0.92 x width long, 0.67 x height tall) plus both wheel circles. The whole-car box above is the
  conservative near-miss box. A fixed 2.2 m length is right for the two cars at 2.4 m width and about 0.4 m short for the Monster Truck; the old
  0.9 m height was too tall for the body and too short for body plus wheels.
- **Wheelbase is 1.40 to 1.56 m, not 2.5 m** (catalogue about 0.6 x width, so 1.4 to 2.0 m for typical widths). `spawnClearance` and glass piece
  timing that assumed 2.5 m must use the vehicle value (`RoadCar` has no public wheelbase today; the director reads it from vehicle data).
- **Speed (target, not measured):** `RoadGameManager` sets `TargetSpeed = min(11.5 + 0.8 x engineLevel, 4.2 + 0.8 x engineLevel + 0.02 x distanceM) x trackSpeed`
  plus `boostSpeedBonus` 4.5 while boosting. So the cap is reached at 365 m for every engine level, the cap itself is **12.3 m/s at engine level 1
  and 15.5 m/s at level 5** (not a flat 11.5), and boost adds 4.5 (up to about 20 m/s). Start speed is 4.2 m/s at the menu car, 5.0 after the
  per-level bonus. `SpeedMultiplier` (catalogue vehicles) only scales the speed set at run start, not the per-frame target.
- **Airborne:** `RoadCar.IsGrounded` is false when no wheel and not the body touches a non-trigger collider (pickups are triggers and do not
  count). Paid air time starts at `airMinSeconds` 0.8 s and stops paying at `airMaxPaidSeconds` 4 s (`GameplayTuning`). Hazards can read
  `IsGrounded`; there is no separate "jump" flag.
- **Consequences for this spec:** FR-2 car half length is 1.1 m (body) to 1.3 m (Monster), so the 1.1 m used below stays valid for the two
  cars and is 0.2 m low for the Monster Truck; the reaction-window distances in sections 4 and 5 that assume 11.5 m/s are about 7 percent
  short at engine level 1, 35 percent at level 5 and 74 percent at level 5 with boost. Re-check them when the implementer wires hazards (Phase B).

---

## 1. Fairness rules (apply to every hazard; each is an acceptance test in section 9)
| ID | Rule |
|---|---|
| FR-1 | Minimum telegraph 0.8 s for any damaging event; boss and Road-Eater damaging events 1.2 to 2.0 s. Telegraph = visual shape in the place of danger + audio cue (+ optional haptic). Colour is never the only channel |
| FR-2 | A projectile or bite never spawns inside, or within `spawnClearance` of, the car hurtbox. Clearance = blast or hit radius + car half length (1.1 m) + 1.4 m margin. If a spawn point violates this it is moved along its path, or the beat is skipped |
| FR-3 | No unavoidable position: for every attack there is, at lock time, at least one answer the player can still perform in the remaining time: change speed (boost or slope), change height (ramp or gap), or hold still where the hazard is not aimed. Standing still on a fresh road never takes damage from RO-B0 (aim rules 5.4) |
| FR-4 | Counter availability gate: an aimed beat that needs a counter (bolt, direct bomb, roller) is only fired if the player has one: free boost meter full OR 5 or more coins, OR ink at least 15 percent of the tank (draw a hill or gap). Otherwise the beat downgrades to a no-threat variant ("lead" bomb, warning bolt, skipped roller) |
| FR-5 | Reaction window after the shape is final ("lock") is at least 0.8 s before any contact (4.3 and 5.3 give the numbers) |
| FR-6 | Homing/tracking turns slower than a player can out-turn: aim tracker 15 to 25 deg/s, locks 0.4 s before the shot, projectiles fly straight after launch (no in-flight homing) |
| FR-7 | Shield beats damage: one hit consumes the shield, costs no heart, grants 1.5 s invulnerability; overlapping hits in that window do nothing |
| FR-8 | Max 2 projectiles alive; never two of the same type within 1.5 s; never two telegraphs whose contact times are within 0.8 s |
| FR-9 | After a hit, respawn, pause resume or phase change: 1.5 s, 0.5 s, 0.5 s grace (no new damage). Respawn clears hazards within 15 m and the car sits on a 6 m safety platform that nothing may remove |
| FR-10 | Nothing may take away the segment under the car (Road-Eater, glass, projectiles) without a safe alternate (safety platform) being placed first |
| FR-11 | Hazards read the run seed; same seed gives the same beat order (Retry replay), but aim positions come from the live car state |
| FR-12 | Reduce-shake setting disables screen shake; every warning still has shape and sound. Haptics: opt-in setting stored through `SaveService` (needs a settings field, see section 11); default OFF is PROPOSED and still an OPEN question in the brief (question 4) |

---

## 2. Road-Eater (MON-EATER)

### 2.1 Model
The Eater is not a world object with physics. It is a scalar `s_e` (path distance along the drawn road, metres) behind the car's `s_c`.
Gap `g = s_c - s_e`. All behaviour is the controller below. The Eater is unkillable and has no collider; the only gameplay effects are
(a) deleting road behind its front, (b) the bite when it resolves a Snap.

### 2.2 State machine
| State | Enter | Behaviour | Exit |
|---|---|---|---|
| Dormant | run start | nothing | `s_c >= triggerDistance - warnMetres` (940 m) |
| Warn | 940 m | 6 s of cues (2.5). No deletion, no controller | after `warnSeconds` 6.0, or `s_c >= 1000` |
| Chase.Track | Warn ends (Eater spawned `startGap` = 32 m behind), or Pushback ends | Controller 2.3, deletion on | gap < 6 (Snap), surge timer fires (Surge), escape timer completes, 1750 m reached |
| Chase.Surge | surge timer, only if `g >= 21` | ratio forced to 1.25 for 2.5 s after a 1.2 s telegraph | after 2.5 s, or `g < 12` (surge aborts, back to Track) |
| Chase.Snap | `g < snapGap` (6 m) | last-chance telegraph, 1.0 s, closing frozen (2.5) | `g >= 8` (cancel, hysteresis) or timer ends (bite) |
| Pushback | bite resolves | heart lost via crash path (or shield consumed), respawn on safety platform, Eater placed at `s_c - 32`, frozen 1.5 s, 1.5 s grace | after 1.5 s, back to Chase.Track |
| Escape | gap >= 45 m for 8.0 s continuous | Eater slows to 0.5 x `v_ref`, roar fades, 150 coins first time / 75 repeat + 0.5 frenzy (+ catalogue card first time), banner; deletion stops | after 3 s, despawn |
| Retreat | car reaches 1750 m without escape | same as Escape visually, payout 75 coins first time / 38 repeat + 0.25 frenzy, no card, telemetry `outcome=timeout` | after 3 s, despawn (no later than about 1785 m at cap speed; the Ring Gauntlet may not start before it ends) |
| Dead | hearts 0 or run end | despawn, audio stop | none |
Pause freezes every timer in this table; resume adds a 0.5 s damage grace.

### 2.3 Speed controller (formula)
Reference speed `v_ref = max(v_car, vFloor)` with `vFloor` = 3.0 m/s (the Eater must still move when the car is stopped).
Ratio by gap `r(g)` (piecewise linear, clamped):
| Gap g (m) | r | Meaning |
|---|---|---|
| g <= 6 | 0.85 (floor) | Eater already backing off; Snap handles the rest |
| 6 to 18 | 0.85 rising to 1.00 | too close: eater slows, gap restores toward 18 |
| 18 (target) | 1.00 | gap holds |
| 18 to 36 | 1.00 rising to 1.25 | catch-up (rubber band), hard cap 1.25 |
| >= 36 | 0.90 | "loses track": falls behind, gap grows 0.1 x v per second |
Target Eater speed `v_t = r(g) * v_ref`; hard cap `v_t <= 1.25 * v_ref` always (FR, lead rule). Surge overrides `r = 1.25`.
Actual speed `v_e` moves toward `v_t` limited by `accelMax` 4.0 m/s2 up, `decelMax` 5.0 m/s2 down. `s_e += v_e * dt`. During a
chew delay (2.5.3) `v_t *= jumpSlowRatio` (0.4).
Consequences (design arithmetic, to be confirmed in sim, section 9 AT-E1): with the car at `v_ref >= 3` and `r < 1` below the target
gap, the Eater can only close on a car that is slower than about 0.85 x its own pace, i.e. stalled, stopped, or on a stall slope. A car
stopped from gap 18: relative closing 3.0 to 3.75 m/s, so Snap in about 3.2 s and bite in about 4.2 s. Pace is therefore never
punished; hesitation is. Escape needs +27 m over the target gap; sources: chew delay (about 0.6 x v x 2.0 s, around 12 m at 10 m/s per
jump), boost lag, and drifting once past 36 m. Expected competent chase 45 to 75 s (target, to be measured).

### 2.4 Gap bands and telegraphs
Band thresholds are `SO` data; surge, snap and escape numbers below are all at or above FR-1.
| Gap | Band | Visual | Audio | Haptic (opt-in) |
|---|---|---|---|---|
| > 24 m | Calm | edge meter small, maw not on screen | low rumble loop | none |
| 12 to 24 m | Edge | maw visible at left screen edge, dust trail, meter yellow (target gap 18 sits here) | chew loop, tempo 1.0x | none |
| 6 to 12 m | Danger | red vignette (shape: jagged teeth border), meter red | roar once on entry, chew tempo 1.5x | 15 ms pulse on entry |
| < 6 m | Snap | full-screen teeth flash, countdown ring around the car 1.0 s | rising siren, chew tempo 2.0x | 60 ms pulse at start |
| >= 45 m | Escaping | gold ring timer (8 s) on edge meter | hopeful chime tick each second | none |

Event telegraphs (all scaled-time, all at or above their floor):
| Event | Telegraph length | Cue | Floor check |
|---|---|---|---|
| Warn start (940 m) | 6.0 s | banner "ROAD-EATER" with maw icon, ground rumble, shake (off if reduce-shake), roar | ok, floor 1.0 |
| Surge | 1.2 s | maw lunges forward at edge + exclamation icon on the meter, roar, 20 ms haptic | ok, floor 0.8 |
| Snap (bite) | 1.0 s | see table above; closing frozen during it | ok, floor 0.8 |
| Escape timer | 8.0 s | gold ring + chimes | reward, no floor |
| Pushback | 1.5 s | Eater roars and retreats, "Phew" banner | recovery, no damage |
| Gap jump delay | 2.0 s | Eater chews at the gap: slowed animation, crunch loop | reward feedback |

### 2.5 Deletion rules (what exactly is eaten)
1. **Granularity.** The road is `RoadChunk`s of at most 36 knots (existing). The eat front is `s_e`.
   - Whole chunks with `chunkEnd <= s_e - 1.5 m` are removed at once (mesh, collider and any pickups attached).
   - The single chunk that straddles `s_e` is trimmed ("bitten"): its end is cut back to `s_e` quantised to 1.0 m, rebuilt at most every
     0.1 s (10 Hz), one chunk per tick. Mesh and collider are updated in the same frame; a collider lagging the mesh by more than one tick
     is a bug.
   - Visual: a 3 m wide ragged bite edge (`biteWidth`) with rubble particles. It is cosmetic; the cut is the logical edge.
2. **Never under the vehicle (FR-10).** Hard clamp: `s_e <= s_c - protectedZone` where `protectedZone` = 6.0 m. The Eater cannot
   advance its front into that zone by any means. The only time the front reaches the car is the Snap bite, and then the car has already
   lost a heart (or shield) and been moved to a fresh 6 m safety platform before any deletion (Pushback). The safety platform, the start
   platform and any `noEat` flagged piece (rescue platforms, glass entry platform, finish gate; the flag is defined in brief section 7 and `stage-events.md` section 5) are never deleted.
3. **Jump delay (the key decision).** When the eat front reaches the start of a road gap that the car has already crossed, with gap
   length `L` in [6, 14] m, the Eater "chews": `v_t *= 0.4` for `jumpDelaySeconds` 2.0 s. Limits: one trigger per gap, at least 8 s
   between triggers, at most 4 per chase. Gaps over 14 m are not rewarded (the car would have fallen). It is bounded so that a bot
   cannot farm gap spam with ink.
4. **Surge is not a free hit.** A surge only starts if `g >= 21` (and aborts at 12), so the worst gap it creates is above 9 m; the
   Snap threshold (6 m) is reachable only by the player stalling.
5. **Spawn.** At the end of Warn the Eater is placed at `s_c - 32`. If drawn road only exists for the last X < 32 m, the Eater spawns
   at the oldest existing road point and `g = X` (never less than 18 m; if less, Warn extends up to 3 s until the player has 18 m of road
   behind them). If the car is airborne it spawns anyway (it is behind the car).
6. **Rescue.** If ink < 15 percent during the chase, the existing `KeepInkReachable` rule runs every frame and is forced on (no
   random-ink 80 percent cap). A chase must never reach an unwinnable no-ink state.
7. **Performance.** Chunk rebuild budget average 1.5 ms, hard 4 ms per tick (a tick above 4 ms defers the next trim by 0.1 s; deletion
   by whole chunk is O(1)). No allocations per frame; particles via the existing `ParticleManager` pool.

### 2.6 Compat, failure cases
Compat per brief (F2): never with ice or meteors; **mud is not enabled in the chase in the slice**; glass allowed from the Track phase; RO-B0 only after
Escape or Retreat. **No surface zone (ice, mud, glass) and no meteor may be Active or Warning during the Warn state** (`stage-events.md` rule X11).
| Case | Behaviour |
|---|---|
| Pause or background | all timers freeze, resume grace 0.5 s |
| Heart lost (any source) while chasing | Pushback: gap reset to 32 m, 1.5 s frozen + 1.5 s grace |
| Shield consumed by bite | same Pushback, no heart lost |
| Death | despawn, result "EATEN AT X m" (cause `eater_bite`) |
| Restart | Dormant, seed replay, `Reset()` |
| Car stopped on glass or mud | glass dwell rule 7.3 and Snap apply; mud floor speed 3.0 means mud alone never stops the car |
| Car falls off road (existing crash path) | Pushback as above |
| Boost during Snap | cancels Snap if `g >= 8` within the 1.0 s |
| Car airborne when Snap resolves | Snap timer pauses while airborne, max 1.5 s, then resolves |
| No ink and no road | rescue ink rule 2.5.6; if still no road and car stalled, bite resolves normally (Pushback gives a platform and ink pickup) |
| Reduce-shake on | no shake; edge meter and vignette still show |

### 2.7 Data: `MonsterDefinitionSO` (Road-Eater)
| Field | Unit | Default | Range | Note |
|---|---|---|---|---|
| id | string | `MON-EATER` | | |
| triggerDistance | m | 1000 | 800 to 1500 | |
| warnMetres | m | 60 | 40 to 100 | about 6 s at 10 m/s |
| warnSeconds | s | 6.0 | 3 to 8 | |
| startGap | m | 32 | 24 to 50 | spawn gap |
| targetGap | m | 18 | 12 to 30 | controller target |
| vFloor | m/s | 3.0 | 2 to 5 | min reference speed |
| maxSpeedRatio | x | 1.25 | 1.0 to 1.25 | hard cap (do not raise above 1.25) |
| minSpeedRatio | x | 0.85 | 0.6 to 1.0 | |
| catchUpEndGap | m | 36 | 30 to 45 | loses track above this |
| lostTrackRatio | x | 0.90 | 0.7 to 1.0 | |
| accelMax / decelMax | m/s2 | 4.0 / 5.0 | 2 to 8 | |
| snapGap / snapCancelGap | m | 6 / 8 | 3 to 10 | |
| snapSeconds | s | 1.0 | 0.8 to 1.5 | floor 0.8 |
| protectedZone | m | 6.0 | 4 to 10 | never eaten |
| trimTickSeconds | s | 0.1 | 0.05 to 0.2 | |
| trimQuantum | m | 1.0 | 0.5 to 2 | |
| wholeChunkMargin | m | 1.5 | 0 to 3 | |
| biteWidth | m | 3.0 | 2 to 4 | cosmetic |
| pushbackGap | m | 32 | 24 to 40 | |
| pushbackFreeze / pushbackGrace | s | 1.5 / 1.5 | 1 to 3 | |
| surgeIntervalMin / Max | s | 12 / 16 | 8 to 25 | seeded |
| surgeTelegraph | s | 1.2 | 0.8 to 2.0 | floor 0.8 |
| surgeDuration | s | 2.5 | 1 to 4 | |
| surgeMinGap / surgeAbortGap | m | 21 / 12 | 15 to 30 / 9 to 15 | |
| jumpMinGapLength / jumpMaxGapLength | m | 6 / 14 | | |
| jumpDelaySeconds / jumpSlowRatio | s / x | 2.0 / 0.4 | 1 to 3 / 0.2 to 0.7 | |
| jumpCooldown / jumpMaxPerChase | s / n | 8 / 4 | | |
| escapeGap / escapeHoldSeconds / escapeResetGap | m / s / m | 45 / 8.0 / 43 | | reset timer if below 43 for 0.5 s |
| timeoutDistance | m | 1750 | 1500 to 2000 | |
| escapeCoins / escapeFrenzy | coins / 0..1 | 150 first, 75 repeat / 0.5 | | repeat = profile has escaped before |
| timeoutCoins / timeoutFrenzy | coins / 0..1 | 75 first, 38 repeat / 0.25 | | 50 percent of the escape payout; no catalogue card |
| bandCalm / bandEdge / bandDanger | m | 24 / 12 / 6 | | |
| inkRescueThreshold | 0..1 | 0.15 | | |
| warnSfx, chaseLoop, biteSfx, roarSfx, chewLoop, sprite, edgeMeterSprite, vignetteSprite | refs | | | |

---

## 3. Surface interactions (formulas, units, fairness proof)
Implementation status (phase B1, 2026-11-16): the mud formula (3.2), the ice slip (3.1) and the base numbers are built in `SurfaceZoneModel` and driven by `SurfaceRuleSO` assets. Not built: ice downhill x1.3 / uphill x0.7 (motor-driven car, no along-slope term), glass arming, cracking and dropping (3.3; the road has no break state, glass is a zone with base numbers only), the glass 6 m entry platform. Mud stall uses the slope ahead of the car, uphill only.
Base numbers are the existing table (friction, speed x, ink x, coin x: ice 0.35/1.15/1.30/1.50, mud 1.60/0.85/0.75/1.30,
glass 0.50/1.12/1.30/1.40). Surface events never overlap the Eater Warn, RO-B0 or meteors (brief). Zone length 120 to 180 m, S2 sign and tint at 40 m
(about 3 s at the fastest cap; the S1 announcer is 5 s before the start, `stage-events.md` section 4). Entering a zone shows a tint strip + banner + sound at the 40 m mark.
Notation: `v_base` = existing target speed (4.2 m/s rising 0.02 per m, cap 11.5), `v_veh` = vehicle speedMul (F8).

### 3.1 Ice (SRF-ICE)
- Target speed: `v_t = v_base * 1.15 * v_veh`.
- Slope: along-slope acceleration `a = a_norm(theta) * k`, where `k = 1.3` downhill and `k = 0.7` uphill (so hills brake less).
  Brake is only by drawing uphill; there is no brake pedal in this game.
- Slip: measure turn angle `dtheta` of the road tangent over a 4 m span. If `dtheta > slipAngle` (25 deg x `iceSlipMul`), the drive motor
  goes off for `t_slip = min(0.6, 0.6 * (dtheta - 25) / 20)` s; the car coasts (no sideways teleport, no damage from the slip itself).
- Fairness proof: (1) the slip trigger is a property of the player's own line: a line with every 4 m span at or under 25 deg never
  slips, so a no-fault player who draws gently never loses drive; (2) the worst slip is 0.6 s of coasting, which at 11.5 m/s is 7 m and
  cannot cause a fall on road that already exists; (3) pre-authored terrain around ice has no forced bend above 25 deg (stage-director
  rule); (4) the 40 m warn gives about 3.5 s to prepare; (5) ink factor 1.30 is covered by the rescue rule. Crash cause "ice slip"
  must not exist as a heart loss; losses only come from the player's own gaps.

### 3.2 Mud (SRF-MUD)
- Target speed with suction: `v_t = max(vFloor_mud, v_base * 0.85 * v_veh - mudDragMul * min(mudDragCapMps, dragPerSec * t_in_zone))`, with
  `dragPerSec` 1.0 m/s per s, `mudDragCapMps` 2.0 (the cap on extra reduction, reached after 2.0 s), `vFloor_mud` 3.0 m/s. At cap on Red: 11.5 * 0.85 = 9.8 then
  7.8 m/s after 2 s. The vehicle multiplier scales the whole drag term, so the cap is 2.0 x `mudDragMul` (Cyan 2.5, Monster 1.2 m/s).
- Stall: road slope above `stallAngle` 18 deg caps the speed at 3.0 m/s (not 0) unless boosting. Boost ignores drag and stall for
  its duration.
- Fairness proof: speed never falls under 3.0 m/s, so mud cannot stop the car (no soft-lock), and a 150 m zone costs at most
  150 / 3.0 = 50 s in the worst case (stall everywhere) and about 19 s at 7.8 m/s in the normal case. Stall needs the player's own
  steep ramp (over 18 deg); a no-fault line (under 18 deg) or one boost (5 coins or free meter) always passes. With the Eater:
  floor 3.0 m/s equals `vFloor`, so at gap 18 (r = 1) the Eater does not close on a car that is moving at the mud floor; the real mud
  risk is stopping, not slowness. Mud is not enabled during the Eater chase in the slice (brief D3), so this stays a future case.

### 3.3 Glass (SRF-GLS)
- Pieces: 4 m long (`pieceLength`). Each piece has a dwell clock that starts when the **front wheel** enters it.
- States: 0.0 to 0.8 s intact; 0.8 s hairline (tinkle, 1 pulse); 1.6 s spiderweb (chime, 2 pulses); 2.6 s drop.
  After the **rear wheel** leaves, the piece drops 0.6 s later regardless (cosmetic and Eater synergy: cannot go back).
  Telegraph from hairline to drop is 1.8 s, from web to drop 1.0 s, both above the 0.8 s floor.
- Safe speed: a car clears a piece (front wheel in to rear wheel out) in `(pieceLength + wheelbase) / v = 6.5 / v` s. The drop at
  2.6 s dwell is avoided for `v >= 2.5 m/s` (vehicle crackMul scales the 2.6). Therefore hesitation means stopping or crawling under
  2.5 m/s, which is exactly what the brief wants punished.
- Fairness proof: (1) a moving car (at least 2.5 m/s, any vehicle: slowest is the monster truck at ~0.95 x) never gets dropped under
  it; (2) the first piece of the zone and the respawn platform are 6 m non-cracking; (3) a bridge drawn with no gap over 14 m can
  always be crossed; (4) hairline and web states are visible shapes, not colour only; (5) ink x1.30 is covered by rescue ink and
  ghost-road; (6) no glass during RO-B0; (7) a fall counts as the existing crash path (shield absorbs).

### 3.4 Surface data: extension of `SurfaceRuleSO`
Existing fields from the brief (`friction`, `speedFactor`, `inkFactor`, `coinFactor`, `downhillBoost`, `uphillBrake`, `slipAngleDeg`,
`slipSeconds`, `dragPerSec`, `dragFloorSpeed`, `stallAngleDeg`, `crackDelay`, `crackSeconds`, `pieceLength`, `warnMetres`, ...) plus:
| Field | Unit | Default | Range |
|---|---|---|---|
| iceSlipSpanMetres | m | 4 | 2 to 8 |
| iceSlipMaxSeconds | s | 0.6 | 0.2 to 1.0 |
| iceSlipRampDegrees | deg | 20 | 5 to 45 |
| mudDragCapMps | m/s | 2.0 | 1.0 to 2.0 | hard cap on extra reduction before `mudDragMul`; do not raise above 2.0 |
| mudBoostIgnoresDrag | bool | true | |
| glassDwellHairline / Web / Drop | s | 0.8 / 1.6 / 2.6 | drop at least 2.0, steps at least 0.8 apart |
| glassDropAfterRearLeave | s | 0.6 | 0.3 to 1.0 |
| glassWheelbaseAssumed | m | 2.5 | read from vehicle, do not hand-tune |
| safeEntryPlatformMetres | m | 6 | 4 to 10 |

---

## 4. Projectile archetypes and world skins
Three behaviours; the slice ships the Countryside skin. Other worlds add a skin row (sprite, name, sfx, tint) with identical behaviour
numbers, so balance does not change per world. Colour code: red = damage (never reused), per the brief.

### 4.1 Archetype table
| | PRJ-BOLT | PRJ-BOMB | PRJ-SAW (roller) |
|---|---|---|---|
| Countryside skin | Corn Shot | Pumpkin Bomb | Hay Roller |
| Role | vertical dodge | speed/height dodge | jump or gap |
| Hitbox | circle radius 0.4 m | blast cylinder radius 2.0 m, height 2.2 m above road; active 0.25 s | circle radius 0.6 m (1.2 m size) on the road surface |
| Speed | 14 m/s straight | arc flight 1.1 s | 8 m/s along the road toward the car |
| Lifetime | 2.5 s | blast 0.25 s + fx 0.5 s | 3.5 s or until it leaves road (a road gap of 2.5 m or more destroys it) |
| Damage | 1 heart (shield absorbs) | 1 heart | 1 heart |
| Tell shape | thin aim line, dashed, from RO-B0 muzzle toward the predicted position, red | filled red circle exactly the blast radius on the road + falling shadow | orange spark ring on the road at the spawn point + short arrow along the road |
| Tell length (boss) | 1.2 s (floor) | 1.5 s | 1.4 s |
| Audio tell | rising pip train, high pitch | low whistle | sizzle and clatter |
| Near-miss (centre distance) | 1.6 m (hit at about 0.85) | 4.5 m (hit at about 2.7) | 1.6 m (hit at about 1.05) |
| Counterplay | change slope or speed after lock | outpace the marker or slow, or clear 2.4 m of height | jump 1.5 m or more, or draw a gap of 2.5 m |
| Pool | 3 | 2 (+3 markers) | 2 |

### 4.2 Bolt (Corn Shot) numbers
- Aim tracker: the dashed line turns toward the predicted target at no more than `aimTurnRate` deg/s (phase 1: 15, phase 2: 20,
  phase 3: 25). Predicted target = car position after `flightTime` using the car's velocity at that instant.
- Lock: the line freezes `lockLead` = 0.4 s before fire; fire = end of tell (1.2 s). Flight from muzzle (at least 9 m away; boss hover is
  5.5 to 8 m ahead and 7.5 m up) is at least 0.64 s at 14 m/s. Reaction window after lock is `0.4 + flight >= 1.04 s` (FR-5).
- Evade condition: the car's centre differs from the locked line by more than `0.4 + 0.45 * 0.85 + 0.2 margin` at contact time
  (about 1.0 m). Slope change over the 1.04 s window (about 10 m of road at cap) provides it; this needs ink or a boost (FR-4 gate).
- In flight: straight line, no homing. No spawn inside the hurtbox (FR-2): muzzle at least 9 m from the car.
- Stationary (car under 1.0 m/s): aim at a point on the road 8 m ahead (no threat).

### 4.3 Bomb (Pumpkin Bomb) numbers
- Marker placed on the road at lock; position `x_m` computed once, then fixed. Marker is the exact blast circle. It lands at
  `t = tell` (1.5 s). Launch at 0.4 s (arc 1.1 s).
- Two modes: **Lead**: `x_m = x_car + v_car * (tell + 0.6) + 3.1` (a car holding its speed reaches the blast edge 0.6 s after the
  blast ended: constant speed is safe, stopping or slowing is not). **Direct**: `x_m = x_car + v_car * tell` (constant speed is hit; the
  answers are +2.1 m/s or more for 1.5 s (boost +4.5 m/s), a downhill, a drawn uphill that slows the car, or 2.4 m of height).
  Direct is used only in phase 2 and 3 and only if the FR-4 gate passes.
- If `|x_m - x_car|` is under 4.5 m (blast 2.0 + half length 1.1 + 1.4), push it to 4.5 m ahead (FR-2).
- If no drawn road exists at `x_m`, snap to the nearest drawn road point within 12 m, else the bomb lands as a harmless dud on terrain
  (puff only, no marker).
- Aftermath: 2.0 s of non-damaging smoulder (visual only, no lingering hazard).

### 4.4 Roller (Hay Roller) numbers
- Spawn on drawn road at distance `D0 = max(20, (v_car + 8) * 1.2 + 2)` m ahead of the car (spark tell 1.4 s, roller appears and moves
  after the tell). Closing speed `v_car + 8` so first contact is at least `(D0 - 0.6 - 1.1) / (v_car + 8) >= 1.0 s` after the roller
  starts (FR-5). If drawn road ahead is shorter than `D0`, the roller is skipped (replaced by a Corn Shot warning bolt).
- Rolls along the road surface following slope; destroyed at a road gap of 2.5 m or more, at lifetime end, or if the Eater deletes the road
  (it never runs while the Eater is active anyway).
- Evade: jump (hurtbox bottom 0.3 m above roller top, which means about 1.5 m of clearance), or a drawn gap 2.5 m or more at least 3 m
  ahead of contact (costs ink), or boost past in the 1.0 s window only if it spawns beside a ramp (not guaranteed, so FR-4 requires jump
  or gap capability = ink at least 15 percent).

### 4.5 Data: `ProjectileDefinitionSO` and `ProjectileSkinSO`
| Field | Unit | Bolt | Bomb | Roller |
|---|---|---|---|---|
| id | | PRJ-BOLT | PRJ-BOMB | PRJ-SAW |
| tellSeconds | s | 1.2 | 1.5 | 1.4 |
| lockLeadSeconds | s | 0.4 | 0 | 0 |
| speed | m/s | 14 | arc | 8 |
| hitRadius | m | 0.4 | 2.0 | 0.6 |
| blastHeight | m | - | 2.2 | - |
| activeSeconds | s | lifetime 2.5 | 0.25 | lifetime 3.5 |
| aimTurnRate (per phase) | deg/s | 15 / 20 / 25 | - | - |
| minMuzzleDistance | m | 9 | - | - |
| leadMode metres | m | - | 3.1 + 0.6 s | - |
| spawnClearance | m | 2.3 | 4.5 | 2.3 |
| nearMissRadius | m | 1.6 | 4.5 | 1.6 |
| counterGate | enum | Ink or Boost | Ink or Boost | Ink only |
| damage | hearts | 1 | 1 | 1 |
| poolSize | n | 3 | 2 | 2 |
| tellPrefab, projectilePrefab, impactVfx, tellSfx, fireSfx, impactSfx | refs | | | |
`ProjectileSkinSO`: `worldId`, `archetypeId`, `displayName`, `sprite`, `tellSprite`, `tint`, `fireSfx`. Skin never alters numbers.

---

## 5. RO-B0 (BOS-ROB0)

### 5.1 Structure
Trigger: 2000 m, only after the Eater is escaped or retreated. Never with surface events or meteors. Starts with a 2.0 s intro (hover in,
banner "RO-B0", no projectiles) then phase 1. The boss has no body hitbox: touching it does nothing; it cannot be reached. No player
attack exists (pillar).

### 5.2 Hover sine controller
Anchor follows the car (screen right, above): `anchor = (x_car + 6.5, y_ref + 7.5)` where `y_ref = max(car y, terrain y at anchor x)`.
Added motion, with seeded random phases `phi_x, phi_y`:
`x(t) = anchor_x + Ax * sin(2*pi*fx*t + phi_x)`, `y(t) = anchor_y + Ay * sin(2*pi*fy*t + phi_y)`.
| Param | Value | Note |
|---|---|---|
| Ax / fx | 1.2 m / 0.30 Hz | |
| Ay / fy | 0.8 m / 0.50 Hz | |
| followSmoothTime | 0.6 s | critically damped follow of the anchor |
| hover height above car | 7.5 m, clamp 6.0 to 10.0 | never lower than 4.5 m above the car roof (so it never overlaps the hurtbox) |
| anchor x ahead | 6.5 m, clamp 5.5 to 8.0 | stays on screen right; muzzle at least 9 m from the car for the bolt rule |
| charge pose | Ax, Ay scaled to 20 percent for the tell | the aim point is readable |
| retreat | rises 6 m and moves off right in 1.5 s | |
Phase pose cue changes on phase change: colour trim (blue, yellow, orange; red is reserved for damage telegraphs).

### 5.3 Counter meter ("Overload", 0 to 100) instead of HP
| Source | Meter change |
|---|---|
| Passive | +0.2 per second (floor; a perfect survive-only run still progresses) |
| Clean dodge (a projectile aimed at the player resolves with no hit) | +3 |
| Near miss (not perfect) | +3 + 2 |
| Perfect near miss | +3 + 3 |
| Overload ring passed (gold ring after each pattern, 4 to 7 m ahead, at most 3 m above the road) | +8 |
| Player hit (heart lost) | -8, clamped at 0 (a shield-absorbed hit costs 0) |
| Reach 100 | Overload: defeat sequence |
Target (to be measured): one pattern is about 3 to 8 projectile-beats plus ring = about 12 to 15 meter; 7 to 8 patterns with
8 s each gives about 55 to 65 s. Hard timeout 105 s: boss retreats ("got bored"), pays 100 coins first time / 50 repeat, no gem, no catalogue unlock
(first-time rewards need an Overload, which stays reachable by dodging).
Phases by meter: Phase 1 `0 to 33`, Phase 2 `33 to 66`, Phase 3 `66 to 100`. Phase change: 2.0 s telegraph (boss
flares, shield bubble flash on the car as a "safe" cue), no projectiles for 2.5 s, 0.5 s grace (FR-9).
Defeat: 4 s sequence, projectiles cleared at once, bonus 200 coins first time / 100 repeat; 1 gem and catalogue unlock `boss-robo-b0` first time only.

### 5.4 Aim rules
1. Aim target is the predicted car position (4.2). Never aim at a position inside the spawn clearance of the car (FR-2).
2. A stationary car (speed under 1.0 m/s) is only targeted by warning shots (aim 8 m ahead, bomb Lead mode, roller skipped): no damage
   possible (FR-3). Time cannot make this a stall trap: the stall is not the boss's job (the Eater punishes it).
3. After a hit, respawn, or phase change: 3.0 s lull, 0.5 s additional grace.
4. FR-4 gate runs at the start of every aimed beat; if it fails, downgrade silently (telemetry `proj_downgraded`).
5. Airborne car: bolts still aim (height is the answer), bombs target the road below the predicted landing, rollers wait until the car lands
   or is 6 m past the spawn.

### 5.5 Five reusable attack patterns
A pattern is an ordered list of beats `(t offset s, projectile, mode, relative position rule)`. Beat offsets are measured from the
pattern start; tell lengths are those in section 4.5; contact times are always at least 0.8 s apart (FR-8). Offsets give fire times;
tells begin earlier by the tell length (so offsets below are tell starts).
| ID | Name | Beats (t offset s : projectile : mode) | Duration | Max alive | Notes |
|---|---|---|---|---|---|
| AP-1 | Single Pop | 0.0 : Bolt : aimed | 2.4 s (tell 1.2 + flight 0.7 + settle) | 1 | teaches the line |
| AP-2 | Double Tap | 0.0 Bolt aimed; 2.0 Bolt aimed (re-aim) | 5.0 s | 1 | two bolts 2.0 s apart, never within 1.5 s |
| AP-3 | Lob Pair | 0.0 Bomb Lead; 2.6 Bomb Direct (phase 2+, gated) else Lead | 6.0 s | 1 to 2 | bomb contact times 2.6 s apart |
| AP-4 | Roller Run | 0.0 Roller; 3.2 Bolt aimed | 6.0 s | 2 | roller contact about 2.4 s, bolt contact 5.1 s: apart by 2.7 s |
| AP-5 | Crossfire | 0.0 Bomb Direct; 1.4 Roller; (phase 3 only) 4.4 Bolt | 7.5 s | 2 | contact times 1.5, 2.8, 5.6: all at least 0.8 s apart; 2 alive max |
After each pattern: 3.0 s lull in which **exactly one Overload Ring** appears (one per pattern, never more; the brief now says the same),
then the next pattern. Selection is seeded and never repeats the previous pattern.
Phase pools: Phase 1 AP-1, AP-2, AP-3 (Lead only). Phase 2 AP-2, AP-3, AP-4. Phase 3 AP-4, AP-5, AP-2. Intensity scale per phase: aim
turn rate 15/20/25 deg/s; tell lengths never below their floors; speeds fixed (no speed creep).
Introductions: the first time a projectile appears it is alone (AP-1 bolt, AP-3 bomb, AP-4
roller) so introductions happen in phases 1 and 2 by pool.

### 5.6 Pooling counts
Bolt 3, bomb 2, bomb marker 3, roller 2, aim lines 3, spark tells 2, impact puffs 4, rings 2, boss 1, banner 1. All preallocated in
Awake; zero allocations per frame; despawn returns to pool within one frame.

### 5.7 Data
`BossDefinitionSO`:
| Field | Unit | Default |
|---|---|---|
| id | | BOS-ROB0 |
| triggerDistance | m | 2000 |
| introSeconds | s | 2.0 |
| hoverAheadMetres / Min / Max | m | 6.5 / 5.5 / 8.0 |
| hoverHeight / Min / Max | m | 7.5 / 6.0 / 10.0 |
| hoverAx, Fx, Ay, Fy | m, Hz | 1.2, 0.30, 0.8, 0.50 |
| followSmoothTime | s | 0.6 |
| meterMax | | 100 |
| meterPassivePerSec | /s | 0.2 |
| meterDodge, meterNear, meterPerfect, meterRing, meterHitPenalty | | 3, 5, 6, 8, -8 |
| phaseThresholds | meter | 33, 66 |
| phaseTransitionTelegraph | s | 2.0 |
| phaseTransitionQuiet | s | 2.5 |
| lullSeconds | s | 3.0 |
| maxAlive | n | 2 |
| sameTypeMinSeparation | s | 1.5 |
| contactMinSeparation | s | 0.8 |
| timeoutSeconds | s | 105 |
| defeatCoins / defeatGem / timeoutCoins | | 200 first, 100 repeat / 1 (first time only) / 100 first, 50 repeat |
| overloadRingsPerPattern | n | 1 |
| catalogUnlockId | | boss-robo-b0 |
| patternPools[phase] | AttackPatternSO[] | per 5.5 |
`AttackPatternSO`: `id`, `beats[]` {`tOffset` s, `projectileId`, `mode` (Aimed, Lead, Direct), `phaseMin`, `fallbackMode`}, `duration` s, `maxAlive`.

### 5.8 RO-B0 failure cases
| Case | Behaviour |
|---|---|
| Pause or background | all timers, projectiles, boss freeze; resume grace 0.5 s |
| Heart lost | projectiles cleared, 3.0 s lull, meter -8 |
| Shield absorbs | projectile consumed, no heart, 1.5 s invulnerability, meter unchanged |
| Death | hazards despawn, result "RO-B0 got you at Overload X percent" |
| Restart | meter 0, phase 1, seed replay |
| Car stationary | warning shots only (5.4.2) |
| Counter gate fails (no coins and ink under 15 percent) | beat downgraded; rescue ink forced on (same rule as Eater) |
| Drawn road too short for a roller | roller skipped |
| Bomb marker has no road | harmless dud |
| Hover target would be inside the hurtbox | hover height clamp 4.5 m above roof |
| Time-scale slow-mo | tells scale with time (never shorter) |
| Reduce-shake | no screen shake on impact or phase flare |

---

## 6. Near-miss radii and lingering-farm guard
| Source | Hit threshold (centre) | Near-miss radius | Perfect (within half the band) | Coins (chain 1 / 2 / 3 / 4+, PROPOSED) |
|---|---|---|---|---|
| Meteor | blast 1.7 + body | 4.5 m | first 1.5 m beyond hit | 8 / 10 / 12 (4th and later 15) |
| Pumpkin Bomb | about 2.7 m | 4.5 m | 2.7 to 3.6 m | 8 / 10 / 12 (15) |
| Corn Shot | about 0.85 m | 1.6 m | 0.85 to 1.2 m | 8 / 10 / 12 (15) |
| Hay Roller | about 1.05 m | 1.6 m | 1.05 to 1.3 m | 8 / 10 / 12 (15) |
| Road-Eater | gap 6 m (Snap) | gap under 4 m recovered to over 12 m within 4 s | | 20 once per 10 s |
Perfect doubles the coins (brief); surface coin factor and boost active still count. The chain window of 4 s applies; base pay is at most 15 and a perfect at most 30 (brief F4, PROPOSED).
Lingering-farm guard (all must hold or the near miss pays nothing):
1. Pays once per hazard instance, and cooldown 1.5 s between payments from the same source id.
2. The car travelled at least 8 m along the road during that hazard's telegraph and averaged at least 3.0 m/s in the last 1.0 s.
3. Hazards are aimed at the predicted car position, not placed statically, so there is nothing to hang near. Time-near-hazard per event
   is logged; a threshold of 3 s per event raises the alert (brief F4 accept d).
4. Near-miss is voided if the same frame had a damage hit, or 1.5 s after respawn.
5. Share cap: near-miss coins never exceed 12 percent of the run total; payment = `min(step, floor(0.12 / 0.88 x nonNearMissCoins) - nearMissPaid)`, floored at 0 (brief F4). A capped near miss still shows the toast and gives frenzy.
Eater near miss requires the gap to go under 4 m (Snap passed) and come back over 12 m; it cannot be farmed by sitting at 6 m because
Snap resolves (bite) within 1.0 s.

---

## 7. Telegraph master table
| Hazard | Telegraph s | Floor s | Visual | Audio | Haptic (opt-in) | Reaction after lock |
|---|---|---|---|---|---|---|
| Eater warn | 6.0 | 1.0 | banner, edge meter, shake | roar, rumble | 30 ms | n/a |
| Eater surge | 1.2 | 0.8 | maw lunge, ! icon | roar | 20 ms | 1.2 s |
| Eater snap | 1.0 | 0.8 | teeth flash, ring | siren | 60 ms | 1.0 s |
| Corn Shot | 1.2 | 1.2 (boss) | dashed red line | pips | none | at least 1.04 s |
| Pumpkin Bomb | 1.5 | 1.2 | red circle + shadow | whistle | none | 1.5 s |
| Hay Roller | 1.4 | 1.2 | orange sparks + arrow | sizzle | none | at least 1.0 s |
| Phase change | 2.0 | 2.0 | boss flare, cyan flash | tone | 40 ms | n/a |
| Ice zone | 40 m (about 3.5 s) | 2.0 | blue tint, sparkle | whoosh | none | n/a |
| Mud zone | 40 m | 2.0 | brown strip, splash | squelch | none | n/a |
| Glass zone | 40 m | 2.0 | cyan-white strip | tinkle | none | n/a |
| Glass crack | hairline to drop 1.8 s | 0.8 | 3 crack states | tinkle, chime | none | 1.0 s from web |
| Meteor (existing) | 1.1 | 0.8 | red marker | whistle | none | 1.1 s |

---

## 8. Hazard x vehicle x speed test matrix (design targets, not results)
Speeds: low 4.2 m/s (run start), cap 11.5 m/s x vehicle speedMul x surface factor (Red 11.5, Cyan 12.4, Monster 10.9 on asphalt).
Vehicle multipliers from brief F8 (starting guesses): Cyan speed 1.08, mass 0.85, mud drag 1.25, ice slip 1.2; Monster speed 0.95, mass 1.4,
mud drag 0.6, glass crack 0.85, pushback 1.3. "Safe" = a no-fault player (gentle line, uses boost when needed) loses no heart.
| Hazard | Red low | Red cap | Cyan low | Cyan cap | Monster low | Monster cap |
|---|---|---|---|---|---|---|
| Eater, constant speed | safe: gap holds at 18 | safe | safe | safe (eater scales with speed) | safe | safe |
| Eater, stop for 5 s | Snap at 3.2 s, bite 4.2 s | same (v_ref floor 3.0) | same | same | same, pushback x1.3 | same |
| Eater surge (closes 0.25 x v x 2.5 s) | about 2.6 m, safe | about 7.2 m, safe (starts only if gap 21 m or more) | about 2.6 m, safe | about 7.8 m, safe | about 2.6 m, safe | about 6.8 m, safe |
| Eater jump delay (gain 0.6 x v x 2.0 s) | about 5 m | about 14 m | about 5 m | about 15 m | about 5 m | about 13 m |
| Corn Shot (window 1.04 s) | 4.4 m of road: change slope or boost; FR-4 gated | 12 m of road: slope change | same as Red | 13 m | heavy, ramps lose speed: needs earlier input | 11 m |
| Pumpkin Bomb Lead | safe at constant speed | safe | safe | safe | safe | safe |
| Pumpkin Bomb Direct | boost needed (+2.1 m/s) or ramp | boost or uphill | same | boost +4.5 clears | same, heavy so ramp loses speed | boost |
| Hay Roller | jump 1.5 m or gap; harder at low speed (window equal 1.0 s but short road) | jump | jump (light = easier) | jump | jump harder (mass) | gap |
| Ice slip | slips above 25 deg | slips above 25 deg | slips above about 21 deg (more sensitive) | about 21 deg | slips above 25 deg | 25 deg |
| Mud | v 3.6 at run start, 7.8 at cap (floor 3.0) | 7.8 | drag x1.25: start 3.9 minus 2.5 hits the 3.0 floor | 8.1 (12.4 x 0.85 minus 2.5) | drag x0.6: start 3.4 minus 1.2 hits the 3.0 floor | 8.1 (10.9 x 0.85 minus 1.2) |
| Glass | clear at 2.5 m/s or more: crossing 6.5 m takes 1.6 s at 4.2 | 0.57 s | same | same | crack x0.85 means drop at 2.2 s: needs 2.9 m/s or more | same |
Figures in the table are back-of-envelope arithmetic from the formulas, to be replaced by sim output (AT-E1, AT-V1); they are not measurements. Ice slip multiplier direction: `iceSlipMul` is applied to the slip angle threshold (Cyan 25 / 1.2 = 20.8 deg, i.e. more slip) in the
implementation; the brief's "x1.2 slip angle" means "slips 20 percent more easily", so the field is stored as a sensitivity.
Required pass per cell: no run loses a heart to the hazard when the player's behaviour is no-fault; at least one counter is available for every
aimed attack at both speeds (FR-3, FR-4).

---

## 9. Acceptance tests
Each test has a method and an explicit pass criterion. No results are recorded here.
| ID | Method | Pass criterion |
|---|---|---|
| AT-E1 | EditMode sim of the controller, car speed profiles (constant, stop 5 s, boost 2.4 s, surge) | `v_e <= 1.25 * v_ref` at every step; gap holds within 18 plus or minus 3 m at constant speed after 10 s; stopped car gets Snap between 2.5 and 4.0 s |
| AT-E2 | EditMode: spawn at 32 m, car starts at 4.2 and 11.5 m/s | gap reaches 18 plus or minus 3 m within 12 s |
| AT-E3 | Bot drawing continuous road 1000 to 1750 m with no stalls | zero hearts lost to the Eater; eat front never passes `s_c - 6` (assert every frame) |
| AT-E4 | Bot with gap-jumps (6 to 14 m) every 10 s | gap reaches 45 m and holds 8 s within 90 s of chase; escape fires; payout exact (150 first time / 75 repeat, 0.5 frenzy) |
| AT-E5 | Deletion test: eat a 100-chunk road | no frame over 25 ms; trim tick average under 1.5 ms (device capture, record the number); mesh and collider removed in the same frame |
| AT-E6 | Safe alternate test: force Snap resolve | heart lost (or shield consumed), safety platform present (6 m) before the deletion; the platform is never deleted |
| AT-E7 | Pause 10 s during Snap | telegraph resumes with at least 0.8 s remaining |
| AT-E8 | Chase with 0 ink | rescue pickup reachable (existing rule) within 15 s |
| AT-E9 | Timeout | at 1750 m, Retreat fires, payout 75 coins first time / 38 repeat, no catalogue card |
| AT-B1 | Aim test: 1000 randomized car states | no projectile or marker overlaps the spawn clearance; no bolt muzzle under 9 m; marker never within 4.5 m of the car |
| AT-B2 | Stationary player test: 60 s standing on a fresh road | zero hits (FR-3) |
| AT-B3 | Telegraph audit: log every `proj_fire` | tell length at least floor (1.2 bolt, 1.2 bomb, 1.2 roller; observed 1.2, 1.5, 1.4); contact-to-lock at least 0.8 s (FR-5) |
| AT-B4 | Max-alive test over 100 simulated runs | never above 2; never two of the same type within 1.5 s; no two contacts within 0.8 s |
| AT-B5 | Gate test: coins 0, ink 5 percent, free meter empty | no aimed beat fires; beats downgrade; `proj_downgraded` logged |
| AT-B6 | Counter meter sim: scripted perfect-dodge bot and a "never dodges, never hit" bot | perfect bot defeats the boss in 50 to 75 s; passive-only bot ends by timeout at 105 s |
| AT-B7 | Phase test | transitions at 33 and 66; 2.0 s telegraph and 2.5 s quiet; patterns match phase pools |
| AT-B8 | Shield test | each projectile and the bite consumed by shield, no heart lost, 1.5 s invulnerability |
| AT-B9 | Allocation test (Profiler on device) | zero GC allocations per frame during RO-B0 and the Eater |
| AT-B10 | Human: 10 players, 3 encounters, then "what do you do about this?" per projectile | telegraph comprehension 90 percent or more; hit rate 15 to 35 percent per projectile on its first 3 appearances (brief) |
| AT-S1 | EditMode: ice slip formula | 25 deg and under never slips; 45 deg gives 0.6 s; slip never causes a heart loss on its own |
| AT-S2 | EditMode: mud formula | `v_t` never below 3.0 m/s; extra reduction never above 2.0 x `mudDragMul`; boost ignores drag and stall |
| AT-S3 | Glass: car speed 2.0, 2.5, 3.0 m/s over one piece | drop under the car at 2.0 only; telegraph states at 0.8 s and 1.6 s; piece drops 0.6 s after the rear wheel leaves |
| AT-N1 | Near-miss audit | one payment per hazard; farm bot (standing at the radius edge) earns 0 |
| AT-V1 | The 6 x 3 vehicle matrix (section 8) | no cell has an unavoidable hit; sim fills the figures; spread between vehicles per hazard under 20 points of completion |

---

## 10. Telemetry for hazards
`eater_warn`, `eater_start`, `eater_gap_sample` every 2 s `{gap, speed, ratio, state}`, `eater_surge`, `eater_snap`, `eater_hit`,
`eater_jump_used {gapLen}`, `eater_escape {seconds, outcome}`, `boss_start`, `boss_phase {n, meter}`, `proj_fire {type, pattern, tell,
mode}`, `proj_downgraded {reason}`, `proj_hit`, `proj_near {type, dist, perfect}`, `proj_dodged`, `boss_end {outcome, meter, seconds}`,
`surface_enter/exit`, `glass_fall {dwell}`, `mud_stall`, `ice_slip {seconds}`, `death {cause, source id}`.

## 11. Dependencies and changes requested outside this file
- game-designer: D1 to D8 adopted into the brief (gap 32, Overload boss, glass arming), PROPOSED until the user approves.
- stage-director: done in `stage-events.md` (rule X11 forbids surface zones in Eater Warn; `noEat` flag defined); ink pickups follow the rescue rule while the Eater is active.
- unity-implementer: expose car hurtbox size, wheelbase, current speed, airborne flag; `RoadChunk` trim API (trim end, remove, collider
  rebuild in the same frame, `noEat` flag); `SaveService` settings fields for haptics (default off) and reduce-shake; move the surface table out of
  `CatalogBuilder` into `SurfaceRuleSO` (brief open question 3).
- economy-balancer: Eater timeout pays 75, boss timeout pays 100 (first time; repeat 38 and 50); meter values and near-miss share cap are tunables; verify that
  5-coin boost price is a decision during the chase.
- voice-director: done in `voice-lines.md` section 3.3 (surge, snap, escape, retreat, phase change, Overload ring, defeat, three projectile tells).
- asset agents: skins (Corn Shot, Pumpkin Bomb, Hay Roller), tell sprites, maw 3 frames, RO-B0 4 frames; all CC0 or procedural with ledger rows.

## 12. Open risks
| Risk | Mitigation |
|---|---|
| The rubber-band controller makes the Eater a non-threat for a player who never stalls (closing only on stalls) | Surges (every 12 to 16 s), glass and mud sections, and forced forward drawing keep pressure; tune `surgeInterval`, `minSpeedRatio` in sim |
| Escape needs about +27 m over target and may be too hard or too easy | AT-E4 sim; `catchUpEndGap`, `lostTrackRatio`, `jumpDelaySeconds` are SO fields |
| Hurtbox, wheelbase and speed are assumed, not read from `RoadCar` | implementer confirms before any test is run |
| FR-4 gate downgrades a lot at low ink, making RO-B0 dull | telemetry `proj_downgraded` rate; tune threshold 15 percent |
| The counter-meter pace (55 to 65 s) is a design guess | AT-B6 sim then device playtest |
| Surface multiplier semantics (ice slip, mud drag) are ambiguous in the brief | recorded in section 8 notes and D7 |
