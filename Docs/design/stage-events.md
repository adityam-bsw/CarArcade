# Stage Events and StageDirector spec (vertical slice)

Owner: level-stage-director. Status: PROPOSED; the data layer (section 9 assets, `IStageDirector`, deterministic core) is built under `Assets/Scripts/Game/Stage/` with EditMode tests SD-01 to SD-22, and phase B1 (2026-11-16) wires it into the real run behind `GameplayTuning.stageEvents` with the slice assets in `Assets/Resources/Stage/`, three-signal warnings on the HUD, ice, mud and glass as event zones on the existing surface hooks, the meteor window, the ring gauntlet and QA jump points; see `Docs/GAMEPLAY_RULES.md` (Stage director) for what is and is not built. Nothing is measured on a device. Date: 2026-11-15.
Reconciled with `VERTICAL_SLICE_BRIEF.md` (source of truth) on 2026-11-15; see the brief's section 13. Where this file and the brief differ, the brief wins.
Source: `Docs/design/VERTICAL_SLICE_BRIEF.md` sections 3, 5 (F1 to F6), 7, 10. Rules: `CLAUDE.md`, `Docs/GAMEPLAY_RULES.md`.
Consumer: unity-implementer builds `StageEventDefinition` assets and the `StageDirector` from this file. Hazard behaviour
details (Eater state machine, RO-B0 patterns, surface formulas) stay in `hazards.md`; this file owns WHEN, WHERE and WHAT MAY STACK.

## 0. Decisions (D1 to D10, adopted into the brief on 2026-11-15 as PROPOSED)
| # | Brief says | Problem | This spec |
|---|---|---|---|
| D1 | Spine: ice 220, mud 600, meteors 940 to 1000, Eater at 1000, 12 s breather after any threat | Meteors at 940 to 1000 collide with the Eater warning and arm phase (brief section 3 rhythm rule), and mud ending at 780 then meteors leaves no 12 s gap. Cannot fit three events plus 12 s breathers in 220 to 1000 m with the old positions | **Nominal starts: ice 205 (range 190 to 220), Meteor Storm 505 (range 490 to 520, 100 m), mud 735 (range 720 to 750, 120 m).** Eater (1000), glass (1350), rings (1800), boss (2000) keep their anchors. (An earlier draft of this row said 490 and 725; the ladder table in 3.1 was always 505 and 735 and is correct.) Fallback if pace telemetry shows no fit: drop the meteor and restore the old ice and mud positions |
| D2 | Meteor window B 1800 to 2000 | Overlaps Ring Gauntlet (a no-hazard reward) and removes the breather before RO-B0 | Meteor B removed from the slice. Meteors appear once |
| D3 | Brief F6 "mud also at 1300+ in chase if enabled" | Hardest stack, untested | Disabled in the slice (`requires` unmet by design). Matrix keeps the conditional cell so it can be enabled later |
| D4 | Role brief: warning 150 m before, event 350 to 650 m, recovery 100 to 200 m | Slice events are 100 to 200 m long (brief F6) and warnings are 40 m | 150 m is the **lock distance** (next event is committed, geometry pre-built, incompatible spawns blocked). The player-facing warning is the three time-based signals (section 4). Event length 350 to 650 m applies to the endless ladder (section 3.2), not the slice spine |
| D5 | Meteor interval 6 to 11 s | A 100 m window at about 11 m/s gives 1 strike, not a behaviour change | Slice override `strikeIntervalSecMin/Max` = 3.5 / 5.5 for EVT-METEOR only (brief says tune in SO); at most 3 strikes (PROPOSED); the existing spawner runs only inside the window |
| D6 | Eater escape at 1700 m (brief) | Hazards D2: Escape = gap 45 m held 8 s, timeout retreat at 1750 m | Chase slot is up to 750 m (not 700). The breather runs from chase end to 1800 m (50 m at the latest). Glass window and the ring start follow (3.1 notes) |
| D7 | Overlap included the 150 m lock window | Mud (ends at most 870 m) overlapped the Eater lock (850 m), and the Ring Gauntlet lock (1650 m) overlapped the chase | Overlap = shared Active metres, or one Active window inside the other's Warning phase (about 60 m; the Eater's is 940 to 1000 m). Lock only commits and pre-builds |
| D8 | Surface zones near the Eater | Hazards asked: surface zones forbidden during Eater Warn; a `noEat` flag for safety, start and rescue pieces | Matrix rule X11 and `noEat` field added (sections 5 and 9) |
| D9 | Finish gate fixed at 2750 m | Boss may run to its 105 s timeout (about 1150 m) | Finish gate at `max(2750, boss end + 150)` |
| D10 | Debug point 600 m | 600 m is inside the meteor window after D1 | Replaced by 640 m (a breather). Eater start gap in the debug table is 32 m (hazards D1) |

All distances assume the brief's speed curve (4.2 m/s rising 0.02 per metre, cap 11.5 at about 365 m). Brief open question 5 (does 1000 m land at
1:50) is unverified; **every distance below is a first guess until T-RUN-PACE telemetry exists**.

## 1. Vocabulary
- **Primary event:** the one thing the stage is about right now (Ice, Mud, Meteor, Rings, Eater chase, Boss). At most one at a time.
- **Modifier:** a layer on top of a primary (surface zone such as Glass during the chase, ring density x2 during the chase). Max two active.
- **Hard event:** `difficultyCost` >= 6 (Glass 6, Eater 7, RO-B0 8). **Breather:** a slot with no primary event and no hazard; coins and ink only.
- **Slot:** one entry in the ladder. `stageIndex` = slot number starting at 0.

## 2. StageDirector contract
Lives in the Game layer (`Game/Stage/`), data from `StageEventDefinition` assets, no reference to any Mode. Vehicle, road and
brush controllers never contain event logic and never read an event id. The director publishes effects as data
(`SurfaceRule` for a zone, hazard spawner enable flags, spawn multipliers); controllers consume those through existing hooks.

```
interface IStageDirector {
  StageEventState NextEvent   { get; }  // chosen, locked or not; never null after Begin()
  StageEventState ActiveEvent { get; }  // primary, null in a breather
  int  ActiveModifierCount    { get; }  // 0..2
  bool CanSpawnHazard(string hazardId); // false during breather, during warn of an incompatible event, 1.5 s after respawn
  bool Breather               { get; }
  void Begin(RunRecord run);            // builds the ladder from the seed
  void Tick(float carDistanceM, float carSpeedMps, float scaledDt); // distance driven, scaled time only (pause freezes)
  void OnRespawn(float carDistanceM);
  void OnRunEnd(RunOutcome outcome);
  void JumpTo(float distanceM);         // debug builds only
  event Action<StageEventState> Warned, Started, Ended;   // UI and audio subscribe; director never touches UI
}
```
`StageEventState` = definition ref, `slotIndex`, `startM`, `endM`, `variant`, phase (Planned, Locked, Warning, Active, Recovery).

Rules the director enforces: one primary at a time (except boss: boss may carry up to two modifiers only from its own data); no hazard
spawner runs unless `CanSpawnHazard` says yes; all stack rules come from the matrix asset (section 5), never from code `if`s.

### 2.1 Determinism
- `seed = unchecked(runSeed + stageIndex + modeId)` per slot, as the brief requires. `runSeed`, `modeId`, the slice layout version and
  the resulting list of `(slot, eventId, startM, variant)` are written to the run record via `SaveService` at `Begin()` (`run_start {seed}` telemetry).
- **Collision guard:** the plain sum collides (run 5 slot 1 equals run 6 slot 0). Generate `runSeed` as `raw << 7` (low 7 bits zero) and keep
  `stageIndex < 64`, `modeId < 64`... precisely `stageIndex + modeId < 128`, so no two (run, slot) pairs share a seed. `modeId` is a stable int
  field on `ModeDefinition` (Countryside value to be assigned by the implementer; never reuse or renumber).
- **Own PRNG:** a small struct (PCG32 or SplitMix64) seeded from the slot seed. Not `UnityEngine.Random`, not `System.Random`.
  No dependence on time, frame count, framerate, player input, or the player's choices. Slot RNGs are independent, so a player who
  dies and retries, or jumps with a debug key, sees the same layout.
- **Draw order per slot (fixed, never reorder):** 1 jitter, 2 variant, 3 replacement choice (only if a rejection happens), 4 terrain seed handed to the event spawner.
- Retry reuses the same `runSeed` (brief F0). A new run draws a new `runSeed`. Daily seed is out of the slice.

### 2.2 Per-frame budget
`Tick` is O(1): compares distance against precomputed thresholds. No allocation, no `FindObjectOfType`, no LINQ. Ladder built once in `Begin()`.

## 3. Distance ladder

### 3.1 Slice timeline (data for `StageLadderDefinition`)
Metres from the start line. "Start range" is the legal start after jitter. Cooldown is the minimum clear road after the event ends
(= 12 s at that surface's fast speed, rounded up; ice runs 1.15x so it needs more metres). Warm-up 0 to 190 contains the 30 m start
platform, coins, one ring arc and ink pickups, no hazard.

| Slot | Event id | Role | Window min m | Window max m | Length m | Nominal start m | Start range m | Lock m before | Cooldown after m | Cost | Hard |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 0 | EVT-ICE | Primary | 190 | 390 | 140 | 205 | 190 to 220 | 150 | 160 | 4 | no |
| 1 | EVT-METEOR | Primary | 490 | 640 | 100 | 505 | 490 to 520 | 150 | 130 | 2 | no |
| 2 | EVT-MUD | Primary | 720 | 870 | 120 | 735 | 720 to 750 | 150 | 130 | 3 | no |
| 3 | MON-EATER | Primary (monster) | 1000 | 1750 | up to 750 (chase ends on Escape or the 1750 timeout) | 1000 (fixed, no jitter) | 1000 | 150 to commit; overlap uses the Warn window 940 to 1000 (D7) | 100 | 7 | yes |
| 3a | EVT-GLASS | Modifier inside slot 3 | 1250 | 1600 | 160 | 1350 | 1310 to 1390 | 150 | n/a (chase continues) | 6 | yes |
| 4 | Breather | Recovery | chase end | 1800 | 1800 minus chase end (50 at the latest) | chase end | fixed | n/a | n/a | 0 | no |
| 5 | EVT-RINGS | Primary (reward) | 1790 | 1990 | 180 | 1800 | 1790 to 1810 | 150 | 0 (boss follows) | 3 | no |
| 6 | BOS-ROB0 | Primary (boss) | 2000 | none (ends on Overload or the 105 s timeout) | about 600 (target 55 to 65 s), at most about 1150 | 2000 (fixed) | 2000 | 150 | 150 | 8 | yes |
| 7 | Finish | Gate | 2750 | no max | n/a | max(2750, boss end + 150) | derived | n/a | n/a | 0 | no |

Notes:
- Jitter: start = nominal + seeded integer in [-40, +40] metres, then clamped to the Start range. The Start range is the intersection of the
  window and the "breather fits" bounds computed backwards from the fixed Eater at 1000 m (mud latest start = 1000 - 130 - 120 = 750; meteor latest
  = 750 - 130 - 100 = 520; ice latest = 520 - 160 - 140 = 220). Effective jitter is therefore about +-15 m for slots 0 to 2; the brief's +-40 m is
  only reachable for glass. Fixed-anchor slots (Eater, boss, finish) take no jitter so the monster and boss feel are comparable between runs.
- Slot 1 and 2 start ranges depend on the previous event's actual end: `start >= prevEnd + prevCooldown`. The director resolves slots in order.
- Glass start range keeps glass fully inside the chase and at least 200 m clear of the 1750 m timeout (latest end 1550). Glass is a modifier of
  the chase, not its own slot, so it does not need a cooldown. `requires` (chase Track phase) is checked at its start only: a zone already Active
  when the chase ends (early Escape) runs to its end. Glass never starts in the Eater Warn phase (rule X11).
- Warm-up, breather and "ESCAPED" payoff distances are data in the ladder asset, not code constants.
- **12 s breather guarantee:** after any primary or modifier threat ends, no new threat starts until `cooldownAfterM` metres AND at least
  `minBreatherS` (12) seconds of scaled time have passed. The signals of the next event may begin inside the breather (they are warnings, not
  threats), but threat effects may not. The stricter of metres and seconds wins; if seconds is stricter the event start slides later within its
  window max, otherwise it is dropped and logged `event_dropped_breather` (never silently shortened).
- Eater warning window: 940 to 1000 m is fixed by the brief (6.0 s timer or 1000 m, whichever first). **No surface zone, meteor or hazard may be Active or Warning there** (rule X11 and X5; the Warn is the Eater's block window). The
  breather rule above is satisfied because the last threat (mud) ends at most 870 m.
- Time targets: use the calculated table in brief section 3 (ice about 0:34, meteor 1:03, mud 1:23, Eater 1:46, rings 2:55, boss 3:13 on the speed curve; all unverified until T-RUN-PACE).

### 3.2 Endless ladder (post-slice, same data, same rules)
For modes without a fixed spine (`LadderMode.Weighted`): repeat `[warm event slot][recovery]` units.
- Event length 350 to 650 m (data per event), lock 150 m, recovery (cooldown) 100 to 200 m, drawn by the slot RNG in whole 10 m steps.
- **Next event is chosen when the previous event ends** (or at stage start for slot 0), never later than its lock point, so terrain and coin shaping can
  be prepared 150 m ahead and the choice is stored in the run record before the warning plays.
- Pool filter in order: (1) distance window and `requires`; (2) matrix says compatible with the previous and any active modifiers; (3) **never the
  same primary id as the previous primary**; (4) if the last two primaries were hard, only breather slots are legal; (5) modifier count after adding
  stays <= 2; (6) weighted pick by `weight` from the slot RNG.
- Rejection fallback (also used by the slice if a data edit makes a slot illegal): take the next candidate by descending weight with ties broken by
  the RNG; if the pool is empty insert a breather of the event's cooldown and retry once at the next slot; if still empty, play the lowest-cost
  event with no modifiers. Every rejection logs `event_rejected {slot, id, reason}`.
- Difficulty ramp: slot `n` may only pick events with `difficultyCost <= 3 + n` until cost 8.

## 4. Warning signals
Three signals per event, timed by **time to the start line** at the car's current speed, with speed floored at the surface cap
(`max(currentSpeed, targetCapSpeed)`) so boosting cannot skip a signal. Timer is scaled time, so pause and slow-mo never shorten a warning.

| Signal | Lead time | Channels (visual + audio, plus a non-colour shape) | Content |
|---|---|---|---|
| S1 Announcer | 5.0 s | Banner top of screen 1.5 s with icon and 1 to 3 words, plus one-word voice cue (text banner is the fallback) | Names the event ("ICE AHEAD"). Voice IDs belong to voice-director |
| S2 Sign | 3.0 s | World sign prop at the road edge at the start line minus 3 s, plus the colour tint on the road strip ahead (blue ice, brown mud, cyan-white glass, red sky for meteors, gold rings), plus a sting | Where it starts; shape icon so it reads without colour |
| S3 Cue | 1.0 s | Edge flash of the event shape (not full-screen white), short sharp SFX, optional haptic (setting, brief open question 4) | Last chance, effect begins when it ends |

Rules:
- Minimum gap between S2 and effect start is 3.0 s for zone events (ice, mud, glass, rings) and 1.0 s for point hazards (meteor marker 1.1 s
  already exists). S3 is never later than 1.0 s before the effect.
- If a signal time was already passed (respawn, debug jump, speed jump), fire missed signals in order with at least 0.6 s between them, and slide the
  effect start later by the missing time, up to 2.0 s of road (never skip a signal; never start a threat with fewer than S3 played).
- Eater and boss use their own warn sequences (hazard spec) but are registered with the director as S1 to S3 so that the compat and breather
  rules still apply. The Eater S1 plays at 940 m (5.2 s at cap speed, 6.0 s timer) which is the brief's fixed warning.
- A reduce-shake setting disables shake; the shape and sound still fire. Nothing is signalled by colour alone.
- Warn lead times and prefab/sfx refs are fields of `StageEventDefinition`, so voice-director and audio can retune without code.

## 5. EventCompatibilityMatrix
Asset `EventCompatibilityMatrix` (ScriptableObject) holding a list of `Pair {idA, idB, overlapRule, adjacentMinGapM, reasonKey}`; the table
below is the data. Symmetric. `X` forbidden to overlap, `OK` may overlap (as primary + modifier only), `C` conditional (see note), `--` n/a.
"Overlap" means a shared metre of two Active windows, or one event's Active window inside the other's Warning phase (S1 to start, about 60 m; for the Eater, 940 to 1000 m). The 150 m lock only commits the choice and never blocks an already-committed event (D7).

| | ICE | MUD | METEOR | GLASS | RINGS | EATER | BOSS |
|---|---|---|---|---|---|---|---|
| **ICE** | -- (no repeat) | X1 | X2 | X3 | X7 | X4 | X6 |
| **MUD** | X1 | -- | X2 | X8 + adjacent | X7 | C1 (disabled in slice) | X6 |
| **METEOR** | X2 | X2 | -- | X2 | X7 | X5 | X5 |
| **GLASS** | X3 | X8 + adjacent | X2 | -- | X7 | OK (modifier) | X6 |
| **RINGS** | X7 | X7 | X7 | X7 | -- | X9 | X9 |
| **EATER** | X4 | C1 | X5 | OK | X9 | -- | X10 |
| **BOSS** | X6 | X6 | X5 | X6 | X9 | X10 | -- |

| Code | Rule and reason | Brief source |
|---|---|---|
| X1 | One surface zone at a time: two rule sets on one road is unreadable | F1 |
| X2 | Meteors are a sky threat; they never mix with ice (no steering room), mud (no escape speed), glass (double timing) | F6 |
| X3 | Ice and glass both punish hesitation or slides: stacking is unfair | F6 EVT-ICE |
| X4 | Ice with the Eater: forbidden for the whole chase (ice grants an unfair speed spike); see also X11 | F2, F1 |
| X5 | Meteors never with the Eater or RO-B0: two aimed sky threats exceed the single-threat rhythm | F2, F3, F6 |
| X6 | RO-B0 forbids any surface event: projectile counterplay needs predictable grip | F1, F3 |
| X7 | Ring Gauntlet is a reward: no hazards, no surface rules | F6 EVT-RINGS |
| X8 | Mud and glass never overlap and never within `adjacentMinGapM` = 300 m (one is a stall, the other a fall) | F1, F6 |
| X9 | Rings (event) and monsters: the gauntlet is the breather before the boss, not a layer on a chase | F6 |
| X10 | Eater and boss: boss only after the chase has ended (Escape or timeout retreat) | F2, F3 |
| X11 | Any surface zone (ice, mud, glass) is forbidden during the Eater Warn phase (940 to 1000 m); glass may start only from the chase Track phase. Matrix pair: each surface event x MON-EATER, `rule = Forbid`, `conditionKey = phase:Warn` | `hazards.md` 2.6, brief F1 |
| C1 | Mud with the Eater only >= 20 s into the chase, mud <= 120 m, start >= 1300 m. **Disabled in the slice (D3)** | F1, F2 |
| OK | Glass during the chase is allowed as a modifier (both erase road, flavour) | F2, F6 |

Additional rules (data in `StageEventDefinition` or the ladder asset, not matrix cells):
- Never the same primary event twice in a row (applies to the endless ladder; the slice has no repeats).
- Modifier cap 2. Modifier set in the slice: surface zone (Glass), ring density x2 (during the chase), near-miss and ring chain rules are not modifiers (always on).
- Hard-stage rule: after two consecutive slots with `difficultyCost >= 6`, the next slot must be a breather. Slice check: Eater (7) is followed by the
  breather between chase end (1750 at the latest) and 1800, boss (8) by the finish. Glass is inside the Eater slot and is not counted as a second hard slot.
- Ring Gauntlet has no `forbiddenWith` other than the matrix (it is safe), but also grants no cooldown, because the boss warning is its own S1.
- **`noEat` pieces:** the director marks these as never deletable by the Eater or by glass: start platform (0 to 30 m), safety and respawn platforms, rescue-ink platforms, the 6 m glass entry platform, the finish gate. The flag lives on the placed piece (`StagePiece.noEat`), is set from `StageEventDefinition.noEat` and the ladder gates, and is read by the road trim API (`hazards.md` 2.5).
- Power-ups: all allowed everywhere. Ghost road and slow-mo may not shorten any signal lead (section 4).

## 6. Per-event terrain rules
The player draws the road, so "terrain" is the world ground, gaps, pickups and the shape the road must reach. Terrain rules constrain what the
spawner places so the event is answerable by drawing, boosting or waiting (brief section 4 rule 4). Global caps: **jump height cap 8 m**;
longest gap the car can cross without a ramp 6 m; no ring or coin needing more than 60 percent of a full tank; no steeper terrain than the
event's slope maximum.

| Event | Slope range deg | Longest gap m | Coins | Rings | Ink pickups (45 ink each) | What the terrain forces |
|---|---|---|---|---|---|---|
| EVT-ICE | -12 to +15 (slip needs bends > 25, so terrain alone never forces slip) | 6 | Line shape, coin x1.5 factor applies | 1 ring arc mid-zone, height <= 6 m | 1 at zone entry (it is 1.30x ink), spacing <= 110 m | One 10 to 12 degree downhill into a bend so the player must flatten or brake uphill |
| EVT-METEOR | -10 to +10 | 6 | Coins in lanes off the marker spots | none | Spacing <= 110 m | Open ground so a marker can always be left by speed change; no gap inside 20 m of a strike |
| EVT-MUD | 0 to +14 (mud stall at 18) | 0 inside the pit | Coins on the low route, x1.3 | 1 ring reachable by a gentle <= 12 degree ramp (the boost alternative) | 1 at start (mud is 0.75x ink, so the pit is the rest surface) | A shallow line exists end to end; a boost line exists |
| EVT-GLASS (in chase) | -12 to +12 | 0 on glass pieces; 6 between glass pieces only if a jump to the next piece is <= 5 m high | Coins above next piece, x1.4 | Rings above the next piece (reward for continuing) | Pickup every <= 90 m (glass is 1.30x ink and the Eater is behind) | Continuous forward path, never a dead-end piece |
| MON-EATER | -12 to +12 | 6 on purpose, at least 3 along the chase (the jump-delay trick, 2 s chew) | Line plus arcs, ring density x2 | Ring density x2 (relief and gap gainer) | Pickup <= 80 m, plus forced rescue ink (existing) | Jumps are optional, never required |
| EVT-RINGS | -8 to +12 | 6 | Coins between rings | 8 to 12 rings over 180 m, spacing 14 to 20 m, radius 1.1 m, each reachable by an <= 8 m jump; chain window 1.5 s | Spacing <= 110 m | Ring heights vary 1 to 6 m so each needs a different ramp |
| BOS-ROB0 | -8 to +8 | 6 | Sparse | One Overload Ring in the lull after every attack pattern | Pickup <= 100 m, at least one per two patterns | Flat enough that the car is not hostage to terrain while dodging |

**Ink guarantee (rescue threshold 45 percent):** the ink level may be below 45 percent for no more than 15 s. The director enforces it with data:
`inkPickupSpacingM` (default 110, tighter in the Eater and glass) is the maximum distance between pickups the spawner may leave, and the
existing `KeepInkReachable` rescue logic remains authoritative below 45 percent. 15 s at the surface cap speed (13.2 m/s on ice, 11.5 m/s
asphalt) is 172 to 198 m, so a 110 m spacing leaves margin. This is a design check; economy-balancer's ink simulation must verify it (no
measurement is claimed here).

Variants: each event has `variantCount` terrain layouts (slice: 2 for ice, mud, rings; 1 for meteor, glass). The slot RNG picks one, so a run
replays identically.

## 7. Start gate, finish gate, warning window, respawn
- **Start gate:** distance 0 to 30 m flat platform, ink full, no hazard; the "GO" gate prop is passed at 0 m. `CanSpawnHazard` false until 190 m.
  Brush hint only on run 1.
- **Finish gate:** at `max(finishMinM 2750, bossEndM + finishAfterBossM 150)`, placed after the boss retreat, `noEat`. Slow-mo flourish 0.65 scale for 1.5 s
  (existing slow-mo is global time scale; restore on exit), finish banner, then Result screen. A run that reaches the gate is `outcome = Finished`.
- **Eater warning window 940 to 1000 m:** the only things allowed on the road are coins, ink and rings. Directors rule: any in-flight meteor, surface
  zone or marker is cleared or already ended (guaranteed by the ladder: last threat ends <= 870 m, cooldown 130 m).
- **Respawn (heart lost, still alive):** the existing safety-platform respawn runs. Director actions on `OnRespawn(d)`:
  1. Clear every hazard within 15 m of `d` (both ahead and behind). Projectiles in the air are cleared. Surface zones remain.
  2. `CanSpawnHazard` false for 1.5 s (matches near-miss grace) and no effect may start within 3.0 s of respawn.
  3. If the respawn is on a glass zone: place a 6 m non-cracking platform (brief F1).
  4. Eater: gap reset to 32 m (hazard spec). Boss: 3 s lull. Meteor: next strike delayed by one full interval.
  5. Signals already played are not replayed, unless the respawn lands inside the S1 to S3 window of a not-yet-started event: then replay S2 and S3 so
     the player gets the 3 s zone-event guarantee.
  6. The ladder does not rewind; the player continues at `d`. Event start positions are unchanged.
- **Death (0 hearts):** all events end, `OnRunEnd` clears hazards and audio, result screen shows the event name or "Eaten at X m" (brief F7).
- **Restart:** `Reset()` on the director and every spawner, same run record seed, same layout, time scale restored.
- **Pause and app background:** all director timers use scaled time; a pause during any signal does not shorten it; resume gives 0.5 s grace
  before damage applies (brief F0).

## 8. Debug jump points
`JumpTo(distanceM)` is Editor and development builds only (behind the existing debug gate; no new `PlayerPrefs` key). Jump sets
car distance, rebuilds the Active/Planned state from the ladder (an event whose window contains the point enters its Warning phase and plays S2 and
S3; one whose start has passed enters Active), restores full ink and 3 hearts, and logs `debug_jump`.

| Distance m | Beat tested | Expected on landing |
|---|---|---|
| 0 | Start gate, warm-up | Start platform, no hazard until 190 m |
| 220 | Ice Sheet (slot 0) | Ice Active or in Warning, zone ends by 360 m |
| 480 | Meteor warning window (extra point added by D1) | S1 to S3 replayed in order (catch-up rule), first strike after S3 |
| 640 | After the meteor window (D10; the brief's old 600 now sits inside it) | In a breather; QA uses it to test "no hazard in breather" |
| 710 | Mud warning window (extra, D1) | S1 to S3 replayed in order, effect after S3 |
| 1000 | Eater start | Warning already played; chase Active, gap 32 m |
| 1350 | Glass Bridge in the chase | Glass Active, Eater gap valid |
| 1800 | Ring Gauntlet | Rings spawned, no hazard |
| 2000 | RO-B0 | Boss intro, pattern 1 (phase 1), no projectile fired for the first 3 s |

Also expose: `JumpToEvent(id)` (lands at lock point minus 10 m) and a seed override field in the debug panel.

## 9. `StageEventDefinition` asset spec
One asset per event, plus `StageLadderDefinition` (slots, gates) and `EventCompatibilityMatrix`. Units in field names. All numbers are first
guesses; ranges are enforced by an editor validator (`OnValidate` in the SO, as pure-data checks, no scene logic). Nothing in these assets
is read by vehicle, road or brush controllers.

### 9.1 `StageEventDefinition`
| Field | Type | Unit | Slice values (ICE / METEOR / MUD / GLASS / RINGS) | Range | Notes |
|---|---|---|---|---|---|
| `id` | string | | EVT-ICE / EVT-METEOR / EVT-MUD / EVT-GLASS / EVT-RINGS | unique | Same as brief IDs; Eater and boss use `MonsterDefinitionSO` and `BossDefinitionSO` and register via an adapter with the same shape |
| `role` | enum | | Primary / Primary / Primary / Modifier / Primary | Primary, Modifier, Monster, Boss, Reward | Reward = no hazard, counts as breather for the hard-stage rule |
| `displayNameKey` | loc key | | | | No raw text |
| `difficultyCost` | int | 1 to 10 | 4 / 2 / 3 / 6 / 3 | 1..10 | Hard = >= 6 |
| `minDistanceM` | float | m | 190 / 490 / 720 / 1250 / 1790 | >= 0 | Window start |
| `maxDistanceM` | float | m | 390 / 640 / 870 / 1600 / 1990 | > minDistanceM + lengthM | Window end |
| `lengthM` | float | m | 140 / 100 / 120 / 160 / 180 | 60..700 | Endless ladder 350 to 650 |
| `startJitterM` | float | m | 40 / 40 / 40 / 40 / 40 | 0..60 | Applied then clamped (section 3.1) |
| `lockDistanceM` | float | m | 150 all | 100..250 | Commit, geometry pre-build, spawn block |
| `cooldownAfterM` | float | m | 160 / 130 / 130 / 0 / 0 | 0..250 | Endless ladder 100 to 200 |
| `minBreatherS` | float | s | 12 all | 8..20 | Seconds guarantee, stricter of m and s wins |
| `weight` | float | | 1 each | 0..10 | Endless only |
| `announcerLeadS` | float | s | 5.0 | 4..6 | S1 |
| `signLeadS` | float | s | 3.0 | 2.5..4 | S2, zone events |
| `cueLeadS` | float | s | 1.0 | 1.0..1.5 | S3, never below 1.0 |
| `signalMinGapS` | float | s | 0.6 | 0.4..1.0 | Catch-up spacing |
| `bannerKey` | loc key | | | | 1 to 3 words |
| `bannerIcon` | sprite ref | | | | Shape icon, not colour only |
| `bannerSeconds` | float | s | 1.5 | 1.0..2.5 | |
| `announcerCueId` | string | | from `Docs/design/voice-lines.md` | | Text banner fallback |
| `signPrefab` | prefab ref | | | | World sign |
| `warnSfx`, `cueSfx` | audio ref | | | | |
| `tintColorToken` | UITheme token | | ice blue, mud brown, glass cyan-white, meteor red, rings gold | | Token, not a hex |
| `forbiddenWith` | id[] | | per matrix | | Mirror of the matrix, validated against it |
| `requires` | id[] | | EVT-GLASS requires MON-EATER active | | Hard prerequisite |
| `adjacentForbiddenWith` | id[] | | MUD: GLASS; GLASS: MUD | | With `adjacentMinGapM` |
| `adjacentMinGapM` | float | m | 300 | 0..600 | |
| `modifierSlots` | int | | 0 for primaries, 1 for modifiers | 0..2 | Counts toward the 2 cap |
| `surfaceRuleRef` | `SurfaceRuleSO` ref | | ICE / null / MUD / GLASS / null | | Surface numbers live there (brief F1) |
| `spawnPrefab` | prefab ref | | | | Spawner root; not required to be in Main scene by hand |
| `spawnerId` | string | | | | Which hazard spawner the director enables |
| `terrainSlopeMinDeg`, `terrainSlopeMaxDeg` | float | deg | section 6 | -20..+20 | |
| `terrainLongestGapM` | float | m | 6 (0 for mud, glass pieces) | 0..8 | |
| `terrainJumpHeightCapM` | float | m | 8 all | <= 8 | Global cap; validator rejects more |
| `coinShape` | enum | | Line / Lanes / Low route / Above-next-piece / Rings | | |
| `ringCount` | int | | 1 / 0 / 1 / 1 per piece / 8..12 | 0..16 | |
| `ringSpacingM` | float | m | n/a / n/a / n/a / n/a / 14..20 | 10..30 | |
| `ringMaxHeightM` | float | m | 6 / 0 / 4 / 5 / 6 | <= 8 | |
| `inkPickupSpacingM` | float | m | 110 / 110 / 110 / 90 / 110 | 50..150 | Max gap; see ink guarantee |
| `variantCount` | int | | 2 / 1 / 2 / 1 / 2 | 1..4 | Picked by slot RNG |
| `strikeIntervalSecMin`, `strikeIntervalSecMax` | float | s | meteor only: 3.5 / 5.5 | 2..12 | D5 |
| `strikeMaxCount` | int | | meteor only: 3 | 1..5 | |
| `clearOnRespawnM` | float | m | 15 | 10..25 | |
| `safePlatformM` | float | m | 6 (glass) / 0 | 0..10 | |
| `noEat` | bool | | true for glass entry platform and respawn platforms; false otherwise | | Set on the placed pieces; Eater and glass never delete them |
| `telemetryId` | string | | same as `id` | | `event_start {id, distance, seed}`, `event_end {id, seconds, hearts lost, inkSpent, coinsGained}` |

### 9.2 `StageLadderDefinition`
`ladderMode` (FixedSpine / Weighted), `layoutVersion` (int, stored in run record), `slots[]` (`eventId`, `nominalStartM`, `startRangeMinM`,
`startRangeMaxM`, `lengthM`), `startPlatformM` 30, `noHazardUntilM` 190, `breatherSlots[]` (`startM`, `lengthM`), `eaterWarnM` 940, `eaterTimeoutM` 1750, `finishMinM` 2750, `finishAfterBossM` 150,
`finishSlowmoScale` 0.65, `finishSlowmoS` 1.5, `maxModifiers` 2, `hardCostThreshold` 6, `hardStagesBeforeBreather` 2, `debugJumpPointsM[]`
(0, 220, 480, 640, 710, 1000, 1350, 1800, 2000).

### 9.3 `EventCompatibilityMatrix`
`pairs[]` of `{idA, idB, rule (Forbid / Allow / Conditional), adjacentMinGapM, conditionKey, reasonKey}`. A pair missing from the list is
**forbidden** (default deny), so a new event cannot stack by accident. `conditionKey` can carry a phase (for example `phase:Warn` for X11). Editor validator: matrix symmetric, every event id present.

## 10. Edge cases
| Case | Behaviour |
|---|---|
| Player boosts into a warning | Signals use the cap speed floor, all three still play (section 4) |
| Slow-mo active | Lead times are scaled time; a hazard telegraph never goes below its floor at scale 1 (`hazards.md` FR-1: 0.8 s, boss 1.2 s), a zone S3 never below 1.0 s |
| Respawn inside an event | Event continues; hazards cleared within 15 m; no new hazard for 1.5 s |
| Respawn inside a signal window | Replay S2 and S3; effect start slides up to 2.0 s of road |
| Death in the Eater warning | `OnRunEnd` clears the warning; retry replays from 0 m, same ladder |
| Player stands still on a fresh road | No event timers depend on the player moving except distance; a stationary car does not progress the ladder (distance driven), and no aimed hazard fires under 0.8 s |
| Debug jump past events | Skipped events are marked Ended, not run; stats not recorded (flag `debugJumped` on run record, excluded from telemetry pace) |
| Pace slower than design (cooldown seconds stricter than metres) | Start slides later inside the window; if it would exceed window max, event dropped and logged |
| Data edit makes a stack illegal | Section 3.2 rejection fallback runs; logs `event_rejected`; never throws in a release build |
| Missing matrix pair | Treated as forbidden |
| Run seed 0 or modeId missing | Validator error in Editor; runtime falls back to `runSeed = 128`, logs a warning, record stores the fallback |
| Two events want the same metre | Only one primary; second slides to its window max or is dropped |
| Ring unreachable (> 8 m jump) | Spawner skips it (brief F5), director counts `ring_skipped` |
| Backgrounded app | Treated as pause |
| Mode with no stage data | Director disabled, `CanSpawnHazard` returns false for all ids (safe default) |

## 11. Acceptance tests
EditMode tests (pure data, no scene) first, PlayMode with the grey-box run second. None of these has been run; this document records no result.

| ID | Test | Pass condition |
|---|---|---|
| SD-01 | Determinism | Same `runSeed + modeId`, 1000 builds of the ladder give identical `(slot, id, startM, variant)` lists |
| SD-02 | Seed independence | Ladder is identical regardless of player distance history, frame rate (30, 60, 120 fps simulated) and pause points |
| SD-03 | Seed formula | Slot seed equals `runSeed + stageIndex + modeId` (unchecked); no two (run, slot) pairs collide for `runSeed = raw << 7`, `stageIndex + modeId < 128` |
| SD-04 | Spine ranges | For 10 000 random seeds every slice start is inside its Start range and every event ends before its window max |
| SD-05 | 12 s breather | For 10 000 seeds, between consecutive threats there are >= `cooldownAfterM` metres, and in a speed simulation (4.2 rising to cap, surface factors) >= 12 s |
| SD-06 | No repeat | In the weighted ladder over 10 000 seeds no primary id follows itself |
| SD-07 | Hard-stage breather | In the weighted ladder no two consecutive hard slots are followed by a non-breather |
| SD-08 | Modifier cap | At no distance in any seed is `ActiveModifierCount` > 2 |
| SD-09 | Matrix | Every forbidden pair in section 5 never overlaps (including lock windows) in 10 000 seeds; matrix is symmetric; missing pair denies |
| SD-10 | Rejection fallback | Forcing a rejected pick yields a legal replacement and one `event_rejected` log; never an exception |
| SD-11 | Warnings | For each event, at speeds 4.2, 11.5 and 11.5 + boost: S1, S2, S3 fire in order at >= 5 s, 3 s, 1 s of scaled time before the effect; effect never starts before S3 + 1.0 s |
| SD-12 | Pause | Pausing during S2 for 10 s then resuming leaves the remaining S2 to effect time unchanged |
| SD-13 | Respawn | After `OnRespawn(d)`: no hazard within 15 m; `CanSpawnHazard` false for 1.5 s; glass respawn has 6 m platform; no effect starts within 3 s |
| SD-14 | Retry | `Reset()` then `Begin(sameRecord)` reproduces the ladder; time scale is 1 |
| SD-15 | Debug jump | Each point in section 8 lands in the stated state; no forbidden stack at any point |
| SD-16 | Terrain caps | Spawned geometry never exceeds jump cap 8 m, slope range, longest gap per section 6 (validator over 1000 generated layouts per event) |
| SD-17 | Ink | Over 1000 simulated runs per event, ink below 45 percent for more than 15 s never occurs in a bot that draws continuous road (economy-balancer sim) |
| SD-18 | Boundary | Director has no reference to `RoadCar`, `RoadBuilder` or any Mode; asmdef check |
| SD-19 | Alloc | `Tick` allocates 0 bytes; no per-frame GC in a 5-minute bot run |
| SD-20 | Playtest | Brief section F6 scenario: 10 runs, 5 players, 3 vehicles; each event reached in >= 70 percent of runs, zero forbidden stacks reported, each player states what the event wants after the banner |
| SD-21 | Eater Warn | In 10 000 seeds no surface zone, meteor or hazard is Active or Warning between 940 m and the chase start; glass never starts before the Track phase (X11) |
| SD-22 | noEat and chase end | Pieces flagged `noEat` are never in the Eater delete set; chase end is Escape or 1750 m at the latest; EVT-RINGS never starts while the Eater is alive |

Behaviour check (the design test for every event): the event must change WHERE to draw, WHEN to boost, or WHAT to spend ink on.
Ice: draw flatter and brake uphill. Meteor: leave the marker by speed or boost. Mud: boost or a shallow line. Glass: never stop, draw ahead.
Rings: draw ramps to chain. An event that only adds visuals fails review.

## 12. Hand-off
- unity-implementer: SOs in section 9, `IStageDirector` in `Game/Stage/`, PCG32 struct, run-record fields (`runSeed`, `modeId`, `layoutVersion`),
  telemetry stubs; move surface table to `SurfaceRuleSO` (brief open question 3). Tests SD-01 to SD-19 as EditMode.
- hazard-designer: D1 to D10 and glass as a chase modifier are adopted in the brief (section 13); `hazards.md` was updated to match.
- economy-balancer: ink guarantee (section 6), coin factors, SD-17 simulation.
- voice-director: S1 cue ids exist in `voice-lines.md` section 3.3 (`vo.rob0.s1.<event>`); S2 and S3 are SFX keys (`sfx.<event>.sign`, `sfx.<event>.cue`).
- team lead / user: D1 to D10 are PROPOSED in the brief; brief open question 5 (pace) must be answered before locking distances.
