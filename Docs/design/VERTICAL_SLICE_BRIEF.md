# Vertical Slice Brief: the 5-minute Road-Eater run

Owner: game-designer. Status: PROPOSED (design only, nothing built). Date: 2026-11-15.
Read with: `Docs/GAMEPLAY_RULES.md` (what exists), `Docs/WORLD_AND_VEHICLE_CATALOGUE.md`, `Docs/REDESIGN_PLAN.md`,
`Docs/ai-kit/12_WEEK_ROADMAP.md` (weeks 2 to 5 map to this slice).

**This brief is the single source of truth for the slice.** Detail lives in four companion docs: `stage-events.md` (when, where, what may
stack), `hazards.md` (formulas, state machines, fairness rules, full SO field lists), `economy.md` (payouts, prices, simulation) and
`voice-lines.md` (cue ids, priorities, subtitles). Where a companion differs from this brief, **this brief wins** and the companion must
be corrected. Every conflict found on 2026-11-15 and how it was settled is in section 13 (Reconciliation log).
Marking used throughout: **PROPOSED** = a number or rule the user has not approved; **OPEN** = an unanswered question (section 12).
Nothing in this document has been measured or built.

## 0. Scope in one paragraph
One run, about 5 minutes, one world (Countryside), three vehicles, three surfaces (ice, mud, glass), one chasing monster
(Road-Eater, appears at 1000 m), one robot boss (RO-B0, three projectile types), five stage events, near-miss bonus, boost ring,
result screen. If this is not fun with grey-box art, content volume will not save it.

## 1. What already exists (do not redesign)
Verified by reading `GAMEPLAY_RULES.md`, `GameplayTuning.cs`, `RoadGameManager.cs`, `CatalogBuilder.cs`, `RoadCar.cs`.
| Exists | Where | Slice use |
|---|---|---|
| Boost (5 coins or free meter), ink 100 max (+30 per tank level today), regen 1/s, ink pickup 45, rescue ink | `RunDirector`, `PickupSpawner` | Kept, except PROPOSED upgrade scaling of +6 percent per level (section 13, R3). Ink is the central resource all new threats pressure |
| 3 lives, shield power-up, safety-platform respawn | `RoadGameManager` (`maxLives=3`) | Kept. Every new damage source goes through the same crash path so shield works |
| Speed 4.2 m/s rising 0.02 per metre to a cap of 11.5, times surface `speedFactor` | `RoadGameManager` | Kept. Used to time the 5-minute arc |
| Surface numbers (friction, speed, ink, coin) for ice (0.35, x1.15, x1.30, x1.50), mud (1.60, x0.85, x0.75, x1.30), glass (0.50, x1.12, x1.30, x1.40), asphalt 1.25 baseline | `CatalogBuilder.Surfaces` | Reused as the base profile; slice adds only the special rule per surface |
| Meteor storm: red marker 1.1 s, blast radius 1.7, near-miss radius 4.5, today pays 15 coins and 0.15 frenzy; today starts at 150 m with a strike every 6 to 11 s | `MeteorSpawner`, `GameplayTuning` | Kept as event EVT-METEOR and as the near-miss **radius** source of truth. Slice overrides (F6): one window only, strike interval 3.5 to 5.5 s, coins per F4 (8, PROPOSED). The existing spawner runs only inside the EVT-METEOR window |
| Boost ring pickup: 0.9 s free burst, 5 coins, 0.2 frenzy, 60 percent chance per coin arc | `Collectible.Type.Ring` | Kept as the boost ring; slice adds the ring gauntlet event |
| Frenzy overdrive 7 s (+3 speed, x2 coins) | `RunDirector` | Kept as the reward for chaining near misses and rings |
| Vehicles `ClassicRed`, `SportCyan`, `MonsterTruck` | `VehicleType` in `RoadCar` | The three slice vehicles. No new art needed |
| Result screen with bonus board, rewards screen, mastery | `ResultScreen`, `MetaProgress` | Extended, not replaced |
| Hazards, fuel, weather, checkpoints | "Not built yet" | Slice builds hazards only. Fuel, weather, checkpoints stay out |

Gap to note: there is no ScriptableObject for events, monsters or surface special rules yet; `GameplayTuning` is a single flat
asset and surface numbers live in `CatalogBuilder` code. The SO fields below are the new data the implementer must add.

## 2. Pillars applied to the slice
| Pillar | How the slice proves it |
|---|---|
| Draw to survive | The road is the only defence. Road-Eater eats it from behind; RO-B0 shots are dodged by drawing ramps and gaps. Ink is the cost |
| The world fights back | Road-Eater (chase), RO-B0 (shoots), meteors (sky), surfaces (ground) |
| Every surface changes the rules | Ice, mud, glass each change what you draw (section 5) |
| Runs tell stories | Result screen names one story line per run ("Escaped the Road-Eater with 4% ink") from telemetry |
| Permanent collection | Vehicle mastery xp, first-time Road-Eater and RO-B0 defeat unlock a catalogue card (data only; no new store) |

Design test for every feature: it must change a decision about WHERE to draw, WHEN to boost, or WHAT to spend ink on. Features that
only add damage or decoration are rejected.

## 3. Player flow (distance is the spine; times are calculated, not measured)
Distances are the nominal starts of `stage-events.md` section 3.1 (seeded jitter moves slots 0 to 2 by at most about 15 m). Times are
arithmetic on the existing speed curve (4.2 m/s rising 0.02 per metre to the 11.5 m/s cap at about 365 m; asphalt, no boost, no stalls:
`t = 50 * ln((4.2 + 0.02 d) / 4.2)` s up to 365 m, then 11.5 m/s). They are targets for telemetry T-RUN-PACE, not facts (open question 5).
Rule: the next threat is always signalled 1 to 5 s ahead (section 4).

| Time (calc) | Distance m | Beat | Player decision | Signal |
|---|---|---|---|---|
| 0:00 to 0:07 | 0 to 30 | Launch on asphalt. Flat start platform (`noEat`), ink full | Draw first line | Brush hint only on run 1 |
| 0:07 to 0:34 | 30 to 205 | Warm-up: coins, one ring arc, ink pickups. No hazard before 190 m | Learn draw, boost, ring | Coin shapes lead the eye |
| 0:34 to 0:49 | 205 to 345 | **EVT-ICE** (140 m; start 190 to 220 after jitter) | Draw flatter, brake with uphill, or boost over | Banner 5 s, sign and blue tint 40 m, edge flash 1 s |
| 0:49 to 1:03 | 345 to 505 | Breather: coins, ink | Collect, rebuild ink | None |
| 1:03 to 1:11 | 505 to 605 | **EVT-METEOR** (100 m; start 490 to 520). The only meteor window | Leave the red marker, near-miss coins | Banner, then existing red marker 1.1 s |
| 1:11 to 1:23 | 605 to 735 | Breather (at least 130 m and 12 s clear) | Collect, rebuild ink | None |
| 1:23 to 1:33 | 735 to 855 | **EVT-MUD** (120 m; start 720 to 750) | Boost through, or a cheap shallow line | Brown strip, splash particles |
| 1:33 to 1:40 | 855 to 940 | Breather. Nothing active | Collect, prepare ink | None |
| 1:40 to 1:46 | 940 to 1000 | **Road-Eater Warn** (6 s timer or 1000 m, whichever first). No surface zone, no meteor, no hazard | Prepare ink | See F2 |
| 1:46 to 2:51 at the latest | 1000 to 1750 at the latest | **Road-Eater chase.** Ends at Escape (gap 45 m held 8 s) or at the 1750 m timeout retreat. **EVT-GLASS** runs inside it around 1350 (start 1310 to 1390). No mud in the chase | Keep drawing ahead of the eater; never stop | Road behind visibly eaten |
| chase end to 1800 | chase end to 1800 (50 m at the latest) | Escape payoff: monster falls away, payout, breather | Spend coins on boost, refill ink | "ESCAPED" (or "IT WANDERED OFF" on timeout) banner |
| 2:55 to 3:11 | 1800 to 1980 (start 1790 to 1810, 180 m) | **EVT-RINGS** (reward, no hazard) | Chain rings for frenzy | Ring trail |
| 3:13 to about 4:13 | 2000 to about 2650 | **RO-B0 boss**: Overload meter to 100, target 55 to 65 s, hard timeout 105 s | Read tell, dodge 3 projectile types, take the Overload ring | See F3 |
| about 4:26 | finish gate at `max(2750, boss end + 150)` | Finish gate, slow-mo flourish | Optional boost | Finish banner |
| +20 s | | **Result screen** (F7) | Retry / Garage | Retry button within 1 tap |

Calculated finish is about 4:26 for a median pace that never stalls, shorter than the 300 s earlier drafts assumed; the boss timeout (105 s)
would add up to about 45 s. Treat 300 s as unverified (section 13, unresolved U1) and let T-RUN-PACE decide.

Rhythm rule (StageDirector): at most one primary threat active at a time (the boss may carry two modifiers from its own data; glass is a
modifier of the chase); at least 12 s and the event's `cooldownAfterM` of no threat after any threat (the "breather"). The Eater Warn phase
(940 m to chase start) allows only coins, ink and rings: no surface zone, no meteor, no hazard is Active or Warning there.

Death path: lives 3. Crash loses a heart and respawns on the safety platform (existing). Run ends at 0 hearts, shows the result
screen with distance reached; Retry restarts at 0 m (no mid-run checkpoint in the slice; OPEN, question 2).

## 4. Readability contract (applies to every threat)
1. Telegraph floors (`hazards.md` FR-1): 0.8 s for any damaging event; 1.2 to 2.0 s for every boss telegraph; Road-Eater warn 6 s, surge
   1.2 s, snap 1.0 s; meteor marker 1.1 s (existing). Zone events use three signals: S1 announcer 5.0 s, S2 sign and tint 3.0 s (40 m at
   cap speed), S3 edge cue 1.0 s (`stage-events.md` section 4). Always two channels: visual (shape or colour in the lane) and audio.
2. A threat is always shown in a fixed place: ground hazards on the road line, sky hazards as a red marker, monster as a screen-edge
   meter. No threat appears from outside the screen without an edge arrow.
3. Colour code: red = damage, blue = ice, brown = mud, cyan-white = glass, gold = reward. Never reuse red for rewards.
4. Hazards that cannot be answered by drawing, boosting or waiting are cut (no unfair hits). Acceptance in each brief states this.
5. Accessibility: every warning also has a shape (not colour only) and a sound; a "reduce shake" setting disables screen shake.

---

## 5. Feature briefs

Common conventions. All numbers are first guesses, to be tuned in SO assets, never in code. Difficulty is 1 (trivial) to 10
(hard to build). "Compat" lists what it can run with. Failure cases cover pause, death and restart for each feature; the general rule
is in F0.

### F0 Common failure rules (apply to every feature below)
- **Pause:** all timers, markers, projectiles and the monster freeze (use scaled time; the existing slow-mo already uses time scale).
  Pausing during a telegraph must not shorten it. Resume gives a 0.5 s grace window before damage applies.
- **Death (hearts reach 0):** all hazards despawn, audio stops, result screen shows. A hit while shield is active consumes the
  shield and the respawn does not cost a heart (existing behaviour).
- **Heart lost but alive:** car respawns on the safety platform; active hazards are cleared within 15 m; the Road-Eater is pushed back
  (see F2); RO-B0 projectiles in the air are cleared; surface zones remain.
- **Restart:** every spawner has `Reset()` and uses the run seed, so Retry gives the same event order (needed for the deterministic
  seed in roadmap week 5). Slow-mo and time scale are reset.
- **App backgrounded:** treated as pause.

### F1 Surfaces: ice, mud, glass (IDs SRF-ICE, SRF-MUD, SRF-GLS)
- Pitch: each surface changes how you draw, not just how fast you go.
- Trigger: a surface zone placed by StageDirector as part of an event, or chosen as the player's road style in the Roads tab
  (existing `surface-*` catalogue entries). In the slice, events force a zone of a given surface onto the road the player draws
  inside the zone bounds; outside it the player's chosen surface returns.
- Base numbers (existing, from `CatalogBuilder`): friction / speed x / ink x / coin x.
  Ice 0.35 / 1.15 / 1.30 / 1.50. Mud 1.60 / 0.85 / 0.75 / 1.30. Glass 0.50 / 1.12 / 1.30 / 1.40.
- New special rules (formulas and fairness proofs in `hazards.md` section 3):
  - **Ice:** low grip means the car keeps its velocity on slopes: downhill speed gain x1.3, uphill braking x0.7. Brake is only by drawing
    uphill. Road bends over 25 degrees (measured over a 4 m span) cut the drive for up to 0.6 s (the car coasts; no teleport, no damage).
  - **Mud:** target speed x0.85 minus a drag that grows 1.0 m/s per second and is **capped at 2.0 m/s of extra reduction** (reached after
    2.0 s in the zone), with a speed floor of 3.0 m/s so mud alone never stops the car. The vehicle `mudDragMul` scales the drag and its cap
    (cap = 2.0 x `mudDragMul`: Red 2.0, Cyan 2.5, Monster 1.2 m/s). Slopes over 18 degrees stall the car (speed capped 3.0) unless boosting.
    Boost ignores drag and stall for its duration (the decision: spend 5 coins or draw a gentle line). Ink is cheaper (x0.75).
  - **Glass (arming rule):** pieces are 4 m long. A piece **arms when the front wheel enters it**: hairline crack at 0.8 s of dwell,
    spiderweb at 1.6 s, the piece drops at 2.6 s of dwell, or 0.6 s after the rear wheel leaves it, whichever comes first. A car moving at
    2.5 m/s or more always clears a piece (6.5 m of travel in under 2.6 s); hesitation under that speed means a fall. The first piece of a
    zone and any respawn platform are 6 m non-cracking `noEat` pieces. `glassCrackMul` scales the 2.6 s drop dwell (Monster 2.2 s).
    The hairline and web states are shapes, not colour only, and each has a sound.
- Counterplay: ice, draw slow dips and uphills before bends; mud, boost or low angle; glass, keep moving, draw far ahead, use ghost
  road power-up.
- Duration: a surface zone is 120 to 180 m (slice: ice 140, mud 120, glass 160).
- Difficulty: ice 4, mud 3, glass 6 (the falling road is new geometry removal; share code with Road-Eater road removal).
- Compat: any vehicle. **Mud is never active during the Road-Eater chase in the slice** (data: EVT-MUD `requires` can never be met; the
  matrix keeps the conditional cell so it can be enabled after the slice). Forbidden stacks: ice + meteor storm, ice + Eater, glass +
  RO-B0, **any surface zone during the Eater Warn phase (940 m to chase start)**, any two surface zones at once. Mud and glass never
  within 300 m of each other.
- Failure cases: pause freezes crack timers; death clears zones; restart replays the same zone positions; a respawn on a glass
  zone puts the car on a safe 6 m non-cracking platform.
- SO fields (`SurfaceRuleSO`): `surfaceId` (catalogue id), `friction`, `speedFactor`, `inkFactor`, `coinFactor` (mirror of existing
  numbers; the implementer should move the table out of `CatalogBuilder` into this SO or reference it), `downhillBoost`,
  `uphillBrake`, `slipAngleDeg`, `slipSeconds`, `iceSlipSpanMetres`, `dragPerSec`, `mudDragCapMps` (2.0, never above 2.0),
  `dragFloorSpeed`, `stallAngleDeg`, `glassDwellHairline` (0.8), `glassDwellWeb` (1.6), `glassDwellDrop` (2.6),
  `glassDropAfterRearLeave` (0.6), `pieceLength`, `safeEntryPlatformMetres` (6), `tintColorToken`, `particlePrefab`, `enterSfx`,
  `loopSfx`, `warnMetres` (40, the S2 sign and tint distance). Full list and ranges: `hazards.md` section 3.4.
- Art/audio: tinted road strips (existing `surface-ice`, `surface-mud`, `surface-glass` catalogue textures); sparkle, mud splash,
  glass crack sprites (3 states); loops: ice whoosh, mud squelch, glass tinkle; crack and shatter one-shots. All CC0 or procedural.
- Telemetry: `surface_enter {id, distance, vehicle, ink01}`, `surface_exit {id, seconds, crashed, inkSpent}`, `glass_fall {distance,
  dwell}`, `mud_stall {distance, boosted}`, `ice_slip {seconds}`.
- Playtest: the same 300 m course, each of 3 vehicles, 3 surfaces, 5 players. Ask "what did you do differently on ice vs mud?".
- Accept: (a) at least 4 of 5 testers name a different action per surface; (b) crash rate per zone between 8 and 25 percent for new
  players; (c) glass fall rate from hesitation (not from bad geometry) at least 70 percent of glass falls; (d) no surface zone makes a
  run impossible with full ink; (e) fast 60 fps with 3 zones active.

### F2 Road-Eater (ID MON-EATER)
- Pitch: a giant maw eats your road from behind, so stopping to think means falling.
- Model (`hazards.md` section 2): not a physics object; a scalar `s_e` behind the car's `s_c`, gap `g = s_c - s_e`, driven by a gap
  controller. Unkillable, no collider. Hazards.md holds the state machine, formulas and full field list; the numbers below are the
  ones other documents quote.
- Trigger: car passes 1000 m (`triggerDistance`). Warn starts at 940 m (60 m earlier).
- Phases:
  1. **Warn (6.0 s or until 1000 m, whichever first):** screen rumble, low roar, edge meter at the left, banner "ROAD-EATER"; no road is
     eaten. No surface zone and no meteor may be Active or Warning (matrix X4, X5, X11).
  2. **Chase:** the Eater spawns `startGap` 32 m behind the car and holds a target gap of 18 m. Speed is a gap controller capped at
     1.25 x the car's reference speed (never faster than that), so a car that keeps moving is never caught and a car that stalls is
     bitten (stopped from gap 18: Snap at about 3 s, bite at about 4 s). It deletes road behind its front (3 m wide bite).
     Occasional **Surge** (every 12 to 16 s, only if gap is 21 m or more, 1.2 s telegraph, 2.5 s at ratio 1.25). If it bites the car,
     the car loses one heart (shield absorbs) and the Eater is reset to 32 m behind (x `eaterPushbackMul`), frozen 1.5 s.
  3. **End of chase, two ways:** **Escape** = gap of 45 m or more held for 8.0 s: the Eater slows and despawns, pays the full
     escape reward (F7) and a banner. **Timeout retreat** at 1750 m if no escape: same visual, **reduced payout, no catalogue card**,
     banner "IT WANDERED OFF". The chase therefore cannot last longer than 750 m.
- Gap bands (SO data): over 24 m Calm, 12 to 24 m Edge (maw visible at the left edge), 6 to 12 m Danger (red jagged-teeth vignette, roar),
  under 6 m Snap (teeth flash, 1.0 s countdown ring, siren). Escaping (45 m or more): gold ring timer.
- Safety: the Eater never eats within 6 m of the car and never eats a `noEat` piece (start platform, safety and rescue platforms, glass
  entry platform, finish gate). Ink under 15 percent during the chase forces the existing rescue ink rule.
- Counterplay: keep speed up (boost, downhill), never stall on mud (mud is not in the chase, but glass is), spend ink only forward. The
  Eater cannot cross an air gap you draw: when its front reaches a gap of 6 to 14 m that the car has already crossed, it chews and slows
  to 40 percent for 2.0 s (at most 4 times per chase, 8 s apart). Key decision: draw a jump to buy time, at an ink cost.
- Duration: about 45 to 75 s for a competent player (design target), 750 m at most.
- Difficulty: 7 (road removal behind the car plus a follow target; reuses RoadChunk culling but needs a "bitten" flag and the `noEat` flag).
- Compat: **no mud in the chase (slice)**; EVT-GLASS allowed from the chase Track phase on (both erase road); never with ice or meteors;
  RO-B0 only after the chase has ended (Escape or Retreat).
- Failure cases: pause stops the Eater and adds 0.5 s resume grace; heart loss triggers Pushback; death ends the chase and the result
  shows "EATEN AT X m"; restart resets to Dormant with the same seed; if ink is 0 during the chase, the rescue ink logic is forced.
- SO fields (`MonsterDefinitionSO`, headline subset; full list in `hazards.md` section 2.7): `id`, `triggerDistance` (1000), `warnMetres`
  (60), `warnSeconds` (6.0), `startGap` (32), `targetGap` (18), `maxSpeedRatio` (1.25, do not raise), `minSpeedRatio` (0.85),
  `catchUpEndGap` (36), `snapGap` (6), `snapSeconds` (1.0), `protectedZone` (6.0), `biteWidth`, `pushbackGap` (32), `surgeIntervalMin/Max`
  (12/16), `jumpDelaySeconds` (2.0), `jumpSlowRatio` (0.4), `escapeGap` (45), `escapeHoldSeconds` (8.0), `timeoutDistance` (1750),
  `escapeCoins` (150 first / 75 repeat), `timeoutCoins` (75 first / 38 repeat, PROPOSED), `escapeFrenzy` (0.5; timeout 0.25), band
  thresholds (24, 12, 6), `inkRescueThreshold` (0.15), audio and sprite refs.
- Art/audio: one maw sprite sheet (3 frames: mouth open, chewing, closed) side view at the left edge, rubble particles, dust trail,
  edge meter, vignette; roar, chew loop (tempo rises with closeness), bite crunch. Procedural or CC0 only.
- Telemetry: `eater_warn`, `eater_start`, `eater_gap_sample` every 2 s `{gap, speed, ratio, state}`, `eater_surge`, `eater_snap`,
  `eater_hit`, `eater_jump_used {gapLen}`, `eater_escape {seconds, outcome: escaped|timeout, inkSpent}`, `eater_death {distance}`.
- Playtest: 1000 m chase test (roadmap week 2). 8 players, no tutorial, record gap graph and cause of loss.
- Accept: (a) at least 90 percent of players identify the Eater as "chasing, keep moving" after the first warning (ask 1 sentence);
  (b) no unavoidable road deletion: a bot that draws continuous road never loses a heart except from its own gaps;
  (c) true Escape rate on the first attempt 35 to 65 percent (timeout does not count as escape); (d) average chase gap stays above 6 m
  for a competent player; (e) no frame over 25 ms while the Eater deletes road.

### F3 RO-B0 robot and its three projectiles (ID BOS-ROB0)
- Pitch: a flying robot shoots at your car, and each shot type needs a different drawing answer.
- Trigger: car passes 2000 m (`triggerDistance`); only after the Road-Eater chase has ended (Escape or Retreat).
- Structure (`hazards.md` section 5): RO-B0 hovers screen-right above the car (6.5 m ahead, 7.5 m up, sine sway) with a 2.0 s intro.
  It has **no hit points and no body hitbox**; the player has no attack (pillar). Progress is an **Overload meter** (0 to 100) that fills
  by dodging: passive +0.2 per second, clean dodge +3, near miss +5, perfect near miss +6, **one Overload Ring per attack pattern +8**,
  a heart lost -8 (a shield-absorbed hit costs nothing). At 100 the boss overloads and the defeat sequence plays. Three phases by meter
  (0 to 33, 33 to 66, 66 to 100); each phase change has a 2.0 s telegraph and 2.5 s of no projectiles.
- Attack patterns (5 reusable, seeded, never the same pattern twice in a row): AP-1 Single Pop, AP-2 Double Tap, AP-3 Lob Pair, AP-4 Roller
  Run, AP-5 Crossfire (phase 3 only); phase pools in `hazards.md` section 5.5. After every pattern: a 3.0 s lull in which **exactly one
  Overload Ring** appears (4 to 7 m ahead, at most 3 m above the road).
- Projectile types (archetype ID, Countryside skin; each unique decision):
  | ID | Name | Tell | Speed / size | Counterplay |
  |---|---|---|---|---|
  | PRJ-BOLT | Bolt (Corn Shot) | Dashed red aim line, 1.2 s (locks 0.4 s before the shot) | 14 m/s, radius 0.4 | Change slope or speed after the lock; or jump |
  | PRJ-BOMB | Bomb (Pumpkin Bomb) | Red circle marker the size of the blast, 1.5 s, falls in an arc | Blast radius 2.0 m, near-miss radius 4.5 | Outpace or slow against the marker (Lead mode: constant speed is safe), or clear 2.4 m of height |
  | PRJ-SAW | Roller (Hay Roller, the "road-saw") | Orange spark ring on the road plus arrow, 1.4 s | 8 m/s along the road toward the car, lasts 3.5 s, 1.2 m size | Jump 1.5 m or more, or draw a 2.5 m gap (the roller falls in it) |
- Fairness (all in `hazards.md` section 1): minimum telegraph 1.2 s for boss attacks (0.8 s floor for any hazard); reaction window after lock at
  least 0.8 s; max 2 projectiles alive; never two of the same type within 1.5 s; no two contact times within 0.8 s; an aimed beat fires only
  if the player has a counter (boost meter full, or 5 or more coins, or ink at least 15 percent) else it downgrades; a car under 1.0 m/s
  is only given warning shots (no damage to a stationary player); no spawn inside the car's clearance.
- Numbers: damage = 1 heart (shield absorbs and gives 1.5 s invulnerability). Near miss per F4. **Overload (true defeat):** 200 coins first
  time, 100 repeat, plus 1 gem and catalogue unlock `boss-robo-b0` **first time only**. **Timeout** (105 s without Overload): RO-B0 "gets
  bored" and retreats, 100 coins first time, 50 repeat (PROPOSED repeat), no gem, no unlock.
- Counterplay summary: bolt = vertical dodge, bomb = horizontal speed, roller = jump or gap. All three can be answered by drawing, so
  RO-B0 forces new decisions and does not repeat meteor.
- Duration: target 55 to 65 s (design guess), hard cap 105 s. Difficulty: 8 (new moving agent, 3 projectile behaviours, aim, pooled objects).
- Compat: forbidden with any surface event, meteors or the Eater. Allowed power-ups: all. Slow motion stretches tells, never shortens them.
- Failure cases: pause freezes; heart loss clears all projectiles and starts a 3.0 s lull; death shows "RO-B0 got you at Overload X percent";
  restart resets meter 0 and phase 1; if the car is stationary, RO-B0 shoots only at the road ahead.
- SO fields (`BossDefinitionSO` + `AttackPatternSO` + `ProjectileDefinitionSO`; full lists `hazards.md` sections 4.5 and 5.7): boss `id`,
  `triggerDistance`, `introSeconds`, `hoverAheadMetres`, `hoverHeight`, `meterMax`, `meterPassivePerSec`, `meterDodge`, `meterNear`,
  `meterPerfect`, `meterRing`, `meterHitPenalty`, `phaseThresholds`, `overloadRingsPerPattern` (1), `lullSeconds`, `maxAlive`,
  `timeoutSeconds` (105), `defeatCoins` (200 first / 100 repeat), `timeoutCoins` (100 first / 50 repeat), `defeatGem`, `catalogUnlockId`,
  `patternPools[phase]`; projectile `id`, `tellSeconds`, `speed`, `hitRadius`, `blastRadius`, `lifetime`, `tellPrefab`, `projectilePrefab`,
  `impactVfx`, `tellSfx`, `fireSfx`, `impactSfx`, `damage`, `nearMissRadius`, `counterGate`, `poolSize`; `ProjectileSkinSO` per world.
- Art/audio: RO-B0 sprite (idle, charge, fire, retreat: 4 frames), three projectile sprites, three tell sprites, impact puffs;
  charge-up sound, three distinct fire sounds (pitched differently so the type can be told by ear), impact, retreat. CC0 or procedural.
- Telemetry: `boss_start`, `boss_phase {n, meter}`, `proj_fire {type, pattern, tellSeconds, mode}`, `proj_downgraded {reason}`, `proj_hit {type,
  pattern}`, `proj_near {type, dist, perfect}`, `proj_dodged {type}`, `boss_end {outcome: overload|timeout|death, meter, seconds}`.
- Playtest: boss prototype (roadmap week 4), 10 players, run the boss only (debug jump to 2000 m). After 3 encounters ask players to
  explain the counterplay for each projectile.
- Accept: (a) telegraph comprehension at least 90 percent (roadmap rule); (b) each projectile type hit rate between 15 and 35 percent
  for new players on its first 3 appearances; (c) no projectile damages a player who stood still on a fresh road (the tell is always
  honoured); (d) at most 2 projectiles alive; (e) no allocation per frame (pooled); (f) a scripted perfect-dodge bot reaches Overload in 50
  to 75 s and a passive bot ends by timeout at 105 s.

### F4 Near-miss bonus (ID BON-NEAR)
- Pitch: coming close to danger pays, so the player wants to be near it.
- Trigger: a damage source (meteor blast, bomb, bolt, roller, Road-Eater bite) resolves, the car is not hit, and the nearest distance is
  within the source's near-miss radius: meteor 4.5 m, bomb 4.5 m, bolt 1.6 m, roller 1.6 m (centre distances, `hazards.md` section 6).
  Eater: the gap goes under 4 m and is recovered to over 12 m within 4 s (once per 10 s).
- Numbers (**PROPOSED**, needs user approval; today's code pays a flat 15 per meteor near miss):
  | Chain step (next within 4 s) | Coins |
  |---|---|
  | 1st | 8 |
  | 2nd | 10 |
  | 3rd | 12 |
  | 4th and later | 15 |
  | Eater near miss | 20 |
  A "perfect" (within half the near-miss band) doubles the coins (so 30 at most per event). Chain resets after 4 s. Frenzy per near miss
  stays 0.15 (existing). Surface coin factor applies after the cap check. Boost active counts; shield active counts.
- **Per-run cap:** near-miss coins never exceed 12 percent of the run's coin total. A payment is
  `min(step, floor(0.12 / 0.88 x nonNearMissCoinsSoFar) - nearMissCoinsPaid)`, floored at 0. When the cap blocks a payment, the "CLOSE!"
  toast, sound and frenzy still fire (the player still feels rewarded; only the coins stop). Median run: about 6 near misses, which the cap
  trims to about 12 percent of roughly 270 coins (economy sim, unverified).
- Lingering-farm guard (all must hold or no coins): one payment per hazard instance; 1.5 s between payments from the same source; the car
  travelled at least 8 m along the road during the telegraph and averaged 3.0 m/s over the last 1.0 s; voided if the same frame had a
  damage hit; 1.5 s grace after a respawn.
- Counterplay: not a hazard; the risk is the damage source itself. The ability to farm is limited by chain cap and the run cap.
- Duration: instant plus a 4 s chain. Difficulty: 3 (existing meteor event; needs a shared interface for projectiles).
- Compat: everything.
- Failure cases: pause freezes the chain timer; death/restart reset chain and cap counters; the bonus is voided if the same frame had a
  damage hit.
- SO fields (extend `GameplayTuning`): `nearMissChainWindow` (4), `nearMissCoinsStep[]` (8, 10, 12, 15), `nearMissPerfectMultiplier` (2),
  `nearMissRunCapFraction` (0.12), `nearMissRadiusByType[]`, `nearMissEaterPay` (20), `nearMissEaterRecover`, `nearMissEaterCooldown`,
  `nearMissToast`, `nearMissSfx`. The existing field `nearMissCoins` (15) becomes the first step (8).
- Art/audio: a "CLOSE!" toast with a chain count (gold), a swoosh sound with rising pitch per chain step, a brief slow-down hit stop
  (0.06 s, only on perfect). Reuses the existing `Cue.NearMiss`.
- Telemetry: `near_miss {source, radius01, chain, coins, capped}`, run total near misses and near-miss coin share.
- Playtest: meteor storm plus RO-B0, check that players do not "farm" by standing under hazards: measure distance from hazard.
- Accept: (a) near misses per run for a median player between 4 and 12; (b) near-miss coins at most 12 percent of the run's coins (enforced
  by the cap, verified per run); (c) at least 60 percent of testers say they noticed the "CLOSE!" and understood it; (d) no run where the
  player deliberately lingers (time near hazard under 3 s per event).

### F5 Boost ring (ID BON-RING)
- Pitch: a gold ring in the air gives a free burst if you hit it, rewarding good jumps.
- Trigger: spawns on 60 percent of coin arcs (existing `ringChancePerArc`) and in E5 Ring Gauntlet.
- Numbers (existing): 0.9 s free burst, 5 coins, 0.2 frenzy. Slice adds: a chain of rings within 1.5 s each extends the burst by
  0.3 s (max 2.4 s) and adds +1 coin each; a missed ring in a gauntlet breaks the chain. Ring radius 1.1 m.
- Counterplay: none (reward). The decision is the draw angle to reach it, and the ink spent to build the ramp.
- Duration: 0.9 to 2.4 s. Difficulty: 2 (exists; chain extension is a small change). Compat: everything; during glass, rings are
  placed above the next piece (so the player is rewarded for continuing). During the Eater chase, ring density doubles (a pressure
  relief and a gap-gainer). During RO-B0, exactly one Overload Ring appears per attack pattern (in the lull after it, +8 Overload, see F3).
- Failure cases: pause freezes; death ends; restart resets chain; a ring whose arc is unreachable (above 8 m of jump) is not spawned
  (validate against the PickupSpawner arc).
- SO fields (extend `GameplayTuning`): `ringRadius`, `ringChainWindow`, `ringChainExtend`, `ringChainMaxBurst`, `ringChainCoinBonus`,
  `ringDensityMultiplierEater`, `overloadRingsPerPattern`, `ringSprite`, `ringPassSfx`, `ringChainSfx`.
- Art/audio: existing ring; add a chain counter and a rising chime per link.
- Telemetry: `ring_pass {chain, distance}`, `ring_miss`, ring total per run.
- Playtest: E5 gauntlet, 6 players. Do they understand "hit the ring" without text? Does a ring pass feel like a boost?
- Accept: (a) 80 percent of first-time players pass a ring within the first 220 m; (b) average chain length in E5 between 3 and 6;
  (c) ring placement never requires a jump above 8 m or an ink amount above 60 percent of the tank.

### F6 Five stage events (IDs EVT-ICE, EVT-MUD, EVT-METEOR, EVT-GLASS, EVT-RINGS)
All events are driven by the StageDirector (section 7) with a seed. Positions below are the nominal starts of `stage-events.md` section 3.1
(that file holds the full ladder, matrix and field list). Common fields in `StageEventDefinition` (a ScriptableObject): `id`, `displayNameKey`,
`minDistanceM`, `maxDistanceM`, `lengthM`, `startJitterM`, `lockDistanceM`, `cooldownAfterM`, `minBreatherS`, `announcerLeadS`,
`signLeadS`, `cueLeadS`, `forbiddenWith[]`, `requires[]`, `weight`, `difficultyCost`, `surfaceRuleRef`, `spawnPrefab`, `bannerKey`,
`bannerIcon`, `announcerCueId` (`vo.rob0.s1.<event>`, see `voice-lines.md` section 3.3), `warnSfx`, `cueSfx`, `noEat`, `telemetryId`.
Common telemetry: `event_start {id, distance, seed}`, `event_end {id, seconds, hearts lost, inkSpent, coinsGained}`.

| ID | Name | Pitch | Trigger and length | Numbers | Counterplay | Difficulty | Compat / forbidden | Accept |
|---|---|---|---|---|---|---|---|---|
| EVT-ICE | Ice Sheet | The road turns to ice: speed up, grip gone | Window 190 to 390 m; nominal start 205, start range 190 to 220; 140 m long; S1 5 s, S2 sign 40 m, S3 1 s | SRF-ICE rules, coins x1.5, ink x1.3 | Draw flat, use uphills to brake, boost to power across | 4 | Not with meteors, Eater, glass, mud. First event | 4 of 5 testers change line shape; hearts lost per event under 0.4 |
| EVT-METEOR | Meteor Storm | Red markers show rocks falling on your road | Window 490 to 640 m; nominal 505, start range 490 to 520; 100 m. **Only window in the slice** (no second window at 1800 to 2000) | Existing marker 1.1 s, blast 1.7, near-miss 4.5. **Strike interval 3.5 to 5.5 s** (slice override of the existing 6 to 11 s), at most 3 strikes (PROPOSED). Near miss per F4 | Move out of the marker; boost | 2 (exists) | Not with ice, mud, glass, Eater, RO-B0, rings | Marker seen by 95 percent; hit rate under 25 percent |
| EVT-MUD | Mud Pit | Heavy mud drags you; boost or draw gentle | Window 720 to 870 m; nominal 735, start range 720 to 750; 120 m. **Not enabled during the Eater chase** | SRF-MUD rules (drag cap 2.0 m/s, floor 3.0), coins x1.3, ink x0.75 | Boost (5 coins), shallow slope | 3 | Not with glass (and never within 300 m of it), meteors, Eater, ice | Stall death under 12 percent; testers pick boost or low slope |
| EVT-GLASS | Glass Bridge | Road cracks under you; keep moving | Inside the Eater chase: window 1250 to 1600 m, nominal 1350, start range 1310 to 1390, 160 m, must end by 1550; `requires` the chase Track phase at its start | SRF-GLS rules (arms on front-wheel entry), coins x1.4, ink x1.3 | Keep moving; ghost road | 6 | Allowed with Eater only (never in its Warn phase); never mud within 300 m | Falls from hesitation at least 70 percent; none from unfair geometry |
| EVT-RINGS | Ring Gauntlet | A reward stretch of rings: chain them for frenzy | Window 1790 to 1990 m; nominal start 1800, start range 1790 to 1810; **180 m**; no hazard | 8 to 12 rings over 180 m, spacing 14 to 20 m, burst chain per F5 | Draw ramps to reach each ring | 3 | Always safe, no hazards; counts as a breather; starts only after the Eater has despawned | Median chain at least 3; frenzy triggered by at least 40 percent of players |

Rewards for events (flat zone-clear coins, escape and boss payouts) are listed once, in F7. Event banner: 1 to 3 words plus icon, shown
1.5 s at the top. Voice: a one-word cue from `voice-lines.md` section 3.3 at S1.

Event playtest scenario (all five): run the full slice 10 times with 5 different players and 3 vehicles; record the event
timeline. Pass if (a) each event reached by at least 70 percent of runs, (b) no run reports a forbidden stack, (c) a player can
say what the event wants in one sentence after the banner.

### F7 Result screen (ID UI-RESULT)
- Pitch: the end of a run shows what you did, tells the story, and offers an instant retry.
- Trigger: run ends (0 hearts, or the finish gate at `max(2750 m, boss end + 150 m)`).
- Content, in order (each shown within 0.4 s of the previous so the screen reads in 3 s): headline (distance and "ESCAPED THE
  ROAD-EATER", "IT WANDERED OFF" or "EATEN AT 1,240 m"), **story line** (single sentence from the largest telemetry stat, see table), bonus
  board (existing: coins, air, flips, chests, record, plus near misses, rings, zone clears, Eater result, RO-B0 result), total coins, mastery
  xp bar, vehicle used, **RETRY** (primary) and Garage (secondary).
- **Rewards (PROPOSED; single table for the whole slice, `economy.md` mirrors it).** "First" = the profile has never completed that
  event (stored through `SaveService`); "repeat" = it has. Repeat pays half.
  | Source | First time | Repeat | Notes |
  |---|---|---|---|
  | Road-Eater **escape** | 150 coins + 0.5 frenzy + catalogue card | 75 coins + 0.5 frenzy | Gap 45 m held 8 s |
  | Road-Eater **timeout retreat** (1750 m) | 75 coins + 0.25 frenzy | 38 coins + 0.25 frenzy | No card. Repeat value is PROPOSED by this reconciliation |
  | RO-B0 **Overload** (defeat) | 200 coins + 1 gem + catalogue card `boss-robo-b0` | 100 coins | Gem and card first time only |
  | RO-B0 **timeout** (105 s) | 100 coins | 50 coins | No gem, no card. Repeat value is PROPOSED by this reconciliation |
  | Zone clear without losing a heart (flat, every run) | ice 20, mud 15, glass 30 | same | On top of the surface coin factor (economy section 3) |
  | Near miss | steps 8 / 10 / 12 / 15, perfect x2, run cap 12 percent | same | F4 |
  | Rings | 5 each, +1 per chain link | same | F5; the gauntlet has no flat bonus |
  | Finish gate | 0 extra | 0 | The boss payout is the finish |
  Vehicle prices (PROPOSED, today's code 200 and 350): ClassicRed 0, SportCyan 600, MonsterTruck 2000. Upgrade scaling PROPOSED: +6 percent per
  level on every stat (boost 0.08 s per level, ink tank +6 per level). No vehicle pays more coins per run than another.
- Story lines (data): pick the highest-priority matching rule: Eater escape with ink under 10 percent ("Escaped with 8% ink"), most near
  misses in a run ("Cut it close: 9 near misses"), glass completed without a fall, most rings in a chain, RO-B0 overloaded without a
  heart lost, Eater timeout ("The Road-Eater lost interest"), RO-B0 timeout ("RO-B0 got bored"), "Eaten at X m", "Fell on the glass",
  default "X m in Y s".
- Numbers: result visible 0.6 s after the last hit; coin count-up 1.2 s (skippable by tap); RETRY accepts tap within 0.5 s.
- Difficulty: 3 (extends existing `ResultScreen`). Compat: all.
- Failure cases: pause not applicable; restart from the result must reset every spawner (F0); if the app is closed on the screen,
  the coins are already saved (existing `SaveService`); the screen must never block on the network (no ads in the slice; if
  rewarded ads come later they stay opt-in and never appear here forced).
- SO fields (`ResultStoryRuleSO[]`): `id`, `priority`, `condition` (enum: stat, threshold), `textTemplate`, `icon`; screen layout lives in
  UI prefab and `UITheme` tokens (not here).
- Art/audio: UI prefab reuse, a gold "ESCAPED" banner, a "EATEN" banner; fanfare vs sting; coin count tick sound.
- Telemetry: `run_end {distance, seconds, outcome, vehicle, surfaceId, nearMisses, nearMissCoins, rings, eaterResult: escaped|timeout|eaten|none, bossResult: overload|timeout|death|none}`,
  `result_retry`, `result_garage`, time on screen.
- Playtest: after 3 runs, ask "what happened in the last run?" without looking.
- Accept: (a) at least 80 percent of players state the story line meaning; (b) Retry is tapped within 6 s by at least 60 percent of
  players; (c) the screen reaches interactive in under 1.5 s from run end; (d) all numbers shown match the telemetry.

### F8 Vehicles in the slice (IDs VEH-RED, VEH-CYAN, VEH-MONSTER)
Not new; it is a **decision matrix** so each vehicle changes choices. Existing `VehicleType` values.
| Vehicle | Intended role | Rule differences to specify (data only) |
|---|---|---|
| ClassicRed | Balanced, forgiving; the tutorial car | Baseline |
| SportCyan | Fast and light: easier on ice and glass pace, hard on mud | Speed x1.08, mass x0.85, mud drag x1.25, ice slip angle x1.2 |
| MonsterTruck | Heavy and grippy: mud and the Eater push-back favourite, slow on glass cracks | Speed x0.95, mass x1.4, mud drag x0.6, glass crack delay x0.85, bite push-back x1.3 |
- Multiplier semantics (match `hazards.md` section 8): `mudDragMul` scales mud drag and its 2.0 m/s cap; `iceSlipMul` is a sensitivity (slip
  threshold = 25 degrees / mul, so Cyan slips at about 21 degrees); `glassCrackMul` scales the 2.6 s drop dwell; `eaterPushbackMul` scales
  `pushbackGap`.
- Prices (PROPOSED, `economy.md` section 9): ClassicRed 0, SportCyan 600, MonsterTruck 2000. Prices buy variety, never income.
- Compat: all events. The numbers above are starting guesses and must be verified in economy-balancer's simulation, not stated as facts.
- SO fields (`VehicleRuleSO`, extend the existing `VehicleData` or a SO referencing it): `speedMul`, `massMul`, `mudDragMul`,
  `iceSlipMul`, `glassCrackMul`, `eaterPushbackMul`, `price`, `hint` (one-line shown in Garage, for example "Fast; careful in mud").
- Difficulty: 3. Failure cases: vehicle swap mid-run is not allowed; vehicle is locked once the run begins.
- Telemetry: `run_end` includes vehicle; per-vehicle completion rate for each event.
- Playtest: same course on all three, compare event hearts lost.
- Accept: no vehicle wins every event; completion-rate spread between best and worst vehicle per event stays under 20 percentage points;
  each vehicle has at least one event where it is clearly the better choice.

---

## 6. Telemetry baseline (also needed before any tuning, roadmap week 1)
Events listed per feature, plus: `run_start {seed, vehicle, tankLevel}`, `draw_stats {metres drawn, ink spent, ink at 30 s steps}`,
`death {cause, distance, source id}`, `pause {distance}`, `fps_sample` every 10 s, `T-RUN-PACE` = time at 500, 1000, chase end (escape or 1750 timeout), 2000, boss end, finish.
Privacy: no personal data, no device ids, local-only unless the data policy is decided (see `PLAY_STORE_READINESS.md`; kids-safe).
Implementation note: an `ITelemetry` stub in Game layer with a local log first.

## 7. StageDirector contract (what the director must produce, for the team lead)
Deterministic per seed. Fixed spine for the slice (`stage-events.md` section 3.1, positions in F6). Seeded jitter of up to plus or minus 40 m on
event start, clamped to each event's start range (effective about plus or minus 15 m for ice, meteor and mud; only glass reaches 40 m), never
moving an event closer than 12 s and `cooldownAfterM` after the previous threat. Eater, boss and finish take no jitter. Exposes: `NextEvent`,
`ActiveEvent`, `CanSpawnHazard(id)`, `Breather(bool)`. All forbidden-stack rules live in the compatibility matrix asset, not code; a pair
missing from the matrix is forbidden.
Definitions that every doc uses:
- **Overlap** between two events = a shared metre of their Active windows, or one event's Active window inside the other's Warning phase
  (S1 to start, about 60 m). The 150 m `lockDistanceM` only commits the choice and pre-builds geometry; it never blocks an event that is
  already committed. For the Eater the Warning phase is 940 m to the chase start.
- **`noEat` flag:** a road piece the Eater (or glass) may never delete: start platform (0 to 30 m), safety and respawn platforms, rescue-ink
  platforms, the 6 m glass entry platform, the finish gate. Set by the director when it places the piece.
- **Chase end:** Escape, or timeout retreat at 1750 m (`eaterTimeoutM`). The breather before EVT-RINGS lasts from chase end to 1800 m
  (50 m at the latest); EVT-RINGS is a reward, not a threat, so it needs no cooldown. If the retreat animation is still running at the
  ring start, the ring start slides later (never overlaps the Eater).
- **Finish gate** at `max(2750 m, boss end + 150 m)`; the slice length is therefore not fixed when the boss runs long.
- **Debug jump points** (QA): 0, 220, 480, 640, 710, 1000, 1350, 1800, 2000 m (`stage-events.md` section 8).

## 8. Scope cuts (explicitly out of the slice)
- No other worlds, no other surfaces beyond ice, mud, glass (asphalt is baseline), no more than the three existing vehicles.
- No weapons or player attacks. No fuel, weather, checkpoints, or new currencies. No gem shop.
- No Daily Seed Ghost, no leaderboards, no multiplayer, no road codes (roadmap weeks 6 to 10).
- No second monster, no more than three projectile types, no player attack on the boss; the Overload meter (F3) is the only boss progress.
- No new vehicles from the Forged art set (jet ski, hovercraft etc.); no buoyancy.
- No voice acting recordings in the slice beyond the cue list; text banners are the fallback.
- No cosmetic rewards for mastery. No ads. No new `PlayerPrefs` keys (use `SaveService`).
- Cut first if schedule slips (in order): glass cracking art states, perfect-near-miss slow hit stop, RO-B0 saw projectile (keep bolt
  and bomb), Ring Gauntlet (keep ring arcs), story-line variety (keep three lines).

## 9. Risks
| Risk | Likelihood | Mitigation |
|---|---|---|
| Road-Eater road removal is a perf or geometry bug (collider rebuild stalls, 36-knot chunks) | High | Eat by whole chunk culling plus one partial chunk; prototype first (roadmap week 2) |
| Chase feels unfair (player cannot draw faster than the eater on mud) | High | Gap bands, pushback, jump-delay rule, rescue ink forced; tune with gap telemetry |
| Boss projectile telegraphs not read in 1 s on a phone | Medium | Test on device, increase `tellSeconds` in SO, use shape plus colour plus sound |
| 5 minutes too long or too short (speed curve, breathers) | Medium | Pace telemetry; breathers are SO values; length is distance based |
| Surface rules hidden in `CatalogBuilder` code, hard to tune | Medium | Move to SO (F1 SO fields); a catalogue rebuild must not overwrite tuning |
| Ice and glass punish low-skill players, killing retention | Medium | Zone length caps, safe respawn, acceptance (crash rate 8 to 25 percent) |
| Ink economy: Eater and glass both increase ink need while ink regen is 1/s | High | economy-balancer simulation of ink in the chase; Eater jump trick, rescue ink |
| Near-miss encourages standing in hazards | Medium | Chain cap, radius limit, telemetry on lingering |
| Scope: 5 new systems in 4 weeks for one developer | High | Scope cuts above; build order Eater, surfaces, RO-B0, director |
| Hazard art needs without AI output | Medium | Procedural sprites like `tools/art/make_pickups.py`; CC0 packs; ledger rows |

## 10. Companion documents (delivered under different file names than first requested)
| Original ask | Delivered as | Role now | Still to do |
|---|---|---|---|
| `STAGE_DIRECTOR_SPEC.md` | `stage-events.md` | Ladder, matrix, signals, terrain rules, `StageEventDefinition` | Pace check (question 5); EditMode tests SD-01 to SD-22 |
| `HAZARD_SPEC.md` | `hazards.md` | Eater and RO-B0 formulas, fairness rules FR-1 to FR-12, surface formulas, test matrix | Sim AT-E1 to AT-V1; implementer confirms hurtbox, wheelbase, speed (assumed, unverified) |
| `ECONOMY_SLICE.md` | `economy.md` | Income bands, payouts, prices, first-hour schedule, tunables | Re-run the simulation after the 2026-11-15 changes (unresolved U5); ink curve once `draw_stats` exists |
| `VOICE_AND_CUES.md` | `voice-lines.md` | Cue ids, lines, priorities, subtitles | Licence decision (OPEN, question 6); measure clip lengths |
Per-document hand-off lists stay in each document's last section. If a hand-off list disagrees with this brief, the brief wins.

## 11. Definition of done for the slice
- A run on device lasts 4:30 to 5:30 for a median player who reaches the finish.
- All five events and both bosses are seen in at least 50 percent of runs by the second playtest.
- Every acceptance line above has a measured result recorded by QA (this document records none).
- 60 fps on the target low-end phone while Road-Eater and glass are active; no per-frame allocations in the new spawners.
- Telegraph comprehension at least 90 percent; counterplay explained by 80 percent of testers after 3 encounters.
- Kids-safe review passes (no forced ads, no scary imagery, no new `PlayerPrefs` keys).

## 12. Open questions (OPEN = not decided; this brief does not decide them)
1. **OPEN** Do the three vehicles stay `ClassicRed`, `SportCyan`, `MonsterTruck` (assumed throughout, including prices and F8), or use catalogue vehicles?
2. **OPEN** Is Retry from 0 m acceptable in the slice, or should there be checkpoints? (Slice text assumes retry from 0 m; the Road-Eater
   and boss rules work either way but economy first-hour numbers assume short retries.)
3. Routing, not a user question: unity-implementer moves the surface table from `CatalogBuilder` to `SurfaceRuleSO`.
4. **OPEN** Is a haptic cue acceptable on mobile for a kids-safe game? `hazards.md` FR-12 and `stage-events.md` S3 assume an opt-in setting
   that is **default OFF** (PROPOSED); nothing is built on haptics until this is answered.
5. **OPEN (measurable)** Does 1000 m at the current speed curve really land near 1:46 (calculated) to 1:50 (earlier drafts)? T-RUN-PACE
   (week 1) must confirm before distances are locked.
6. **OPEN** Voice licence source: the 54 existing `say` (Zarvox) clips are unresolved in `Docs/ASSET_LEDGER.md`; final audio must be a human
   recording, a CC0 or licensed pack, or a licensed TTS the user approves by name. Until then the slice ships subtitles, banners and SFX.
7. **OPEN** Approval of the PROPOSED economy changes: vehicle prices 600 / 2000, upgrade scaling +6 percent per level, near-miss steps
   8 / 10 / 12 / 15 with a 12 percent run cap, half pay on repeat clears, timeout payouts (75 Eater, 100 boss, and the derived repeat values
   38 and 50), flat zone-clear coins 20 / 15 / 30. These change the existing code values (`GameCatalog.VehicleCost` 200 / 350,
   `nearMissCoins` 15, `boostDurationPerLevel` 0.25 s, tank +30 per level).
8. **OPEN** Boost price: keep 5 coins, or test 10 (`economy.md` section 6 says 5 is not a real decision for a median player).

## 13. Reconciliation log (2026-11-15)
Rule applied: this brief is the source of truth; where two companion docs disagreed, the later and more detailed decision was adopted only if it
is listed here, and every companion doc was edited so that its numbers match. "R" items are the four requested reconciliations; "X" items are
further disagreements found while doing them.

### R1 Event positions (adopted `stage-events.md` D1 to D5)
| Item | Old (brief, other docs) | Now |
|---|---|---|
| Ice | 220 to 400 m | window 190 to 390, nominal 205, start range 190 to 220, 140 m |
| Meteor | 940 to 1000 m plus 1800 to 2000 m, 5 strikes, 6 to 11 s | **one** window, nominal 505 (490 to 520), 100 m, interval **3.5 to 5.5 s**, at most 3 strikes (PROPOSED) |
| Mud | 600 to 780 m, plus 1300+ in the chase | nominal 735 (720 to 750), 120 m, **disabled in the chase** |
| Eater arm / chase | meteors pre-warm at 940 | Warn 940 to 1000 free of zones and meteors; chase to 1750 at the latest |
| Rings | 1800 to 2000, 200 m | window 1790 to 1990, nominal 1800, 180 m |
| Debug points | 0, 220, 600, 1000, 1350, 1800, 2000 | 0, 220, 480, 640, 710, 1000, 1350, 1800, 2000 (600 sat inside the meteor window, so it became 640) |
Edited: brief sections 3, F1, F2, F6, 7; `stage-events.md` D1 (its prose said 490 and 725 while its own table said 505 and 735; the table
wins), section 3.1 notes, section 8, section 9.2; `hazards.md` 2.6, 3.2, 3 intro; `economy.md` unchanged (its segment table already fits).

### R2 Hazards
| Item | Decision |
|---|---|
| Eater timeout | Retreat at 1750 m if no Escape (gap 45 m held 8 s). Brief "escape at 1700" and `stage-events.md` slot 3 length 700 replaced; breather slot now runs from chase end to 1800 |
| Glass arming | Arms on front-wheel entry: hairline 0.8 s, web 1.6 s, drop 2.6 s dwell or 0.6 s after the rear wheel leaves. Brief "cracks 0.6 s after the rear wheel leaves, 0.3 s steps" replaced |
| Mud drag cap | Extra reduction capped at 2.0 m/s (floor 3.0 m/s stays); vehicle `mudDragMul` scales the cap (see unresolved U4) |
| Overload ring | Exactly one per attack pattern, in the lull after it. Brief "every third wave" and `stage-events.md` "every 3rd wave" replaced |
| Surface zones in Eater Warn | New matrix rule X11 in `stage-events.md`; brief section 3 rhythm rule and F1 |
| `noEat` flag | Defined in brief section 7 and `stage-events.md` (matrix notes, `StageEventDefinition.noEat`); `hazards.md` 2.5 points to it |
Also resolved here: lock windows no longer count as overlap (previously mud, which may end at 870 m, collided with the Eater's 850 m lock, and
the Ring Gauntlet's 1650 m lock collided with the chase). Only Active windows and Warning phases count (section 7).

### R3 Economy
| Item | Decision (all PROPOSED until the user approves, question 7) |
|---|---|
| Near miss | 8 / 10 / 12 / 15 (was 15 / 20 / 25 / 30), perfect x2, Eater near miss 20 (was 40), run cap 12 percent with an exact formula (F4) |
| Repeat clears | Eater escape 150 first / 75 repeat, RO-B0 200 / 100; gem and catalogue card first time only. Flat zone-clear coins and ring coins are not halved (unresolved U8) |
| Timeout payouts | Eater 75, boss 100 (first time); repeat 38 and 50 are derived (50 percent) and flagged U3 |
| Vehicle prices | 600 (SportCyan), 2000 (MonsterTruck); ClassicRed 0 |
| Upgrade scaling | +6 percent per level (boost 0.08 s per level, ink tank +6 per level) |
Edited: brief F4, F7 (rewards table), F8, section 1; `economy.md` sections 0, 2.1, 3, 4, 12, 13; `hazards.md` 2.2, 2.7, 5.3, 5.7, 6.

### R4 Voice cue ids
`voice-lines.md` section 3.3 is now the registry: every stage-event signal (S1 voice for ice, mud, meteor, glass, rings, Eater, boss; S2 and
S3 as SFX keys), every hazard telegraph in `hazards.md` section 7 and every Eater and boss state has a cue id. New cues added:
`s1.meteor`, `eater.surge`, `eater.retreat`, `boss.phase`, `boss.ring`, `finish`, plus banner and SFX ids for the pieces that carry no voice.
`announcerCueId` in `StageEventDefinition` uses these ids. Voice gap bands now quote 12 m (Danger) and 6 m (Snap) to match `hazards.md`;
event warns now trigger at S1 (5.0 s), not "40 m ahead".

### X items (found while reconciling)
| # | Disagreement | Decision |
|---|---|---|
| X1 | Eater model: brief gap 40 m and 85 to 92 percent speed, pushback 35 m, escape at 1700 m vs `hazards.md` D1 (spawn 32, target 18, cap 1.25 x) | Adopted `hazards.md` D1 and D2 (listed as lead instructions there). Brief F2 rewritten. PROPOSED |
| X2 | Boss: brief 10 waves, 3 weak points, bolt ring every 3rd wave vs `hazards.md` Overload meter | Adopted Overload meter and 5 patterns (F3 rewritten). PROPOSED |
| X3 | Telegraph floor: brief 1.0 s vs hazards 0.8 s (boss 1.2 to 2.0 s) vs voice "1.0 s minimum" | Floors per hazards FR-1 (section 4); voice text corrected |
| X4 | Projectile tells: brief 1.0 / 1.3 / 1.2 s vs hazards 1.2 / 1.5 / 1.4 s | Hazards values |
| X5 | Zone warning: brief "40 m before", voice "40 m ahead", stage-events S1 5 s / S2 3 s / S3 1 s | Stage-events signals; 40 m is the S2 sign and tint distance (3 s at the fastest cap, 13.2 m/s) |
| X6 | Eater gap bands 30 / 15 / 6 (brief) and "under 15 m" (voice) vs 24 / 12 / 6 (hazards) | 24 / 12 / 6 |
| X7 | Finish fixed at 2750 m vs boss timeout 105 s (about 1150 m at cap) | Finish gate at `max(2750, boss end + 150)` |
| X8 | Names: `StageEventSO` (brief) vs `StageEventDefinition`; `HAZARD_SPEC.md`, `ECONOMY_SLICE.md`, `VOICE_AND_CUES.md`, `STAGE_DIRECTOR_SPEC.md` vs delivered names | `StageEventDefinition`; section 10 maps the file names |
| X9 | Mud drag figures for Cyan and Monster in `hazards.md` section 8 (including 8.4 m/s at run start) were arithmetic slips | Row recomputed from the formula; still arithmetic, not a measurement |
| X10 | Gap after respawn: brief 35 m, stage-events debug jump 40 m vs hazards 32 m | 32 m everywhere |

### Unresolved (could not be settled without the user or measurements)
| # | Contradiction or gap | Why unresolved |
|---|---|---|
| U1 | Slice length: brief target 300 s and definition of done 4:30 to 5:30, but arithmetic on the speed curve gives about 4:26 to finish with a 60 s boss (about 5:10 with a 105 s boss timeout) and the Eater at 1:46 not 1:50 | Needs T-RUN-PACE (question 5). Distances are distance-based so the beats still hold |
| U2 | A 100 m meteor window at 11.5 m/s is about 8.7 s, so 3.5 to 5.5 s intervals give 2 to 3 strikes; whether that "changes a decision" is untested, and the 3-strike cap is a guess | Playtest |
| U3 | Repeat values for timeouts (38 coins, 50 coins, 0.25 frenzy) are my derivation; the request gave only 75 and 100 | Needs user approval (question 7) |
| U4 | Mud drag cap 2.0 m/s: I made the vehicle multiplier scale the cap (Cyan 2.5, Monster 1.2). If the cap should be absolute for every vehicle, Monster loses its mud advantage except ramp speed | Needs a decision |
| U5 | `economy.md` simulation outputs (sections 2.2, 10.2, 10.3) were produced under the old Eater (escape at 1700, about 45 percent escape), old distances and old payout structure. Not re-run | Needs the economy-balancer to rerun; unlock-minute figures are stale |
| U6 | Margin between the Eater timeout retreat (retreat animation ends about 1785 m at cap) and the earliest Ring Gauntlet start (1790 m) is about 5 m; the slide rule covers it but the ring may start late on timeout runs | Verify in the grey-box run |
| U7 | `hazards.md` figures (surge, jump delay, Snap timings) and the whole test matrix remain design arithmetic; hurtbox 2.2 x 0.9 m and wheelbase 2.5 m are assumptions not read from `RoadCar` | Needs implementer to read the code, then sim |
| U8 | "Half pay on repeat clears" applied to Eater and boss payouts only. If it should also halve the flat zone-clear coins or ring coins, `economy.md` income bands shift | Needs a decision |
| U9 | Ink budget for the Eater chase and glass is still not simulated (`economy.md` section 7); `stage-events.md` test SD-17 depends on it | Needs `draw_stats` telemetry |
| U10 | Existing code and `Docs/GAMEPLAY_RULES.md` (outside this task's write scope) still hold today's values (prices 200 / 350, near miss 15, meteor from 150 m every 6 to 11 s). Intentional until the user approves question 7 | Routing: implementer and a later GAMEPLAY_RULES update |

## 14. User decisions log
- 2026-11-15 APPROVED by the user: the PROPOSED economy numbers (vehicle prices 600/2000, near-miss 8/10/12/15 with 12% run cap, half pay on repeat Eater/boss clears, timeout payouts 75/100, upgrade scaling +6%/level) and the slice vehicle trio (ClassicRed, SportCyan, MonsterTruck).
- Still OPEN (not answered): retry-from-0 vs checkpoints, haptics default, voice licence source, boost price 5 vs 10, mud cap scaling per vehicle, adoption of Eater model (32/18/1.25x) and Overload boss meter.
- 2026-11-16 **implementer defaults, user may override** (applied by the team lead's instruction for phase B1 so work is not blocked; none of these is an approval):
  | Open question | Default applied | Where it lives (change it there) |
  |---|---|---|
  | Retry from 0 m or checkpoints | Retry from 0 m (current behaviour); the same run seed is reused so the ladder repeats | `RoadGameManager.BeginStageRun` (retry = previous screen was GameOver) |
  | Haptics | Off. One `Handheld.Vibrate()` call on S3 only when the flag is on; nothing else is built on it | `GameplayTuning.stageHaptics` (false) |
  | Boost price 5 or 10 | Unchanged at 5 | `GameplayTuning.boostCoinCost` |
  | Mud drag cap per vehicle | Scales: Red 2.0, Cyan 2.5, Monster 1.2 m/s (`mudDragMul` 1 / 1.25 / 0.6 times the 2.0 cap) | `GameplayTuning.mudDragMulByVehicle`, `SurfaceRuleSO.mudDragCapMps` |
  | Eater model and boss meter | As in `hazards.md` (32 m start gap, 18 m target, 1.25x cap; Overload meter). Built in phase B2 (`GAMEPLAY_RULES.md`, section Road-Eater, RO-B0) | done |
  | Stage events in a real run | `GameplayTuning.stageEvents` = CountrysideOnly (ON by default in the Countryside world, off elsewhere; Off restores the old game exactly) | `GameplayTuning.stageEvents` |
