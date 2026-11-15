# Gameplay rules: boost, ink, coins, power-ups, rewards

Status: built and tested (EditMode tests in `GameplayLogicTests`, `StageDirectorTests`, `StageWiringTests`, Play mode run of Home, Garage, Rewards, HUD, Result).
All numbers live in `Assets/Resources/Gameplay/GameplayTuning.asset` (class `GameplayTuning`); change them there, not in code.

## Where the code is
- `Game/Gameplay/RunDirector.cs`: rules inside one race (boost, combo, power-up timers, air time, flips, distance chests).
- `Game/Gameplay/PickupSpawner.cs`: what spawns ahead of the car and where.
- `Game/Gameplay/MetaProgress.cs`: daily reward, daily missions, gems, vehicle mastery (works on `PlayerProfile` only).
- `Game/RoadGameManager.cs`: owns the director, applies results (coins, ink, lives, time scale), syncs `GameModel`.
- UI: `HudScreen` (boost meter and price, combo, power-up timers, toast, gems), `ResultScreen` (bonus board), `RewardsScreen` (new), Home (REWARDS button with a red dot), Garage (4th upgrade: BOOST).

## Boost
Costs `boostCoinCost` coins (default 5, run coins are spent first) unless the free meter is full. The meter fills from air time,
flips and coin combos; a full meter makes the next boost free and empties it. A boost lasts `boostDuration` plus
`boostDurationPerLevel` per BOOST upgrade level, adds `boostSpeedBonus` to the target speed and has a cooldown. Not enough
coins: the button greys out and a toast says NEED n COINS. (A coin pickup is worth 10, so 5 is the price; a price of 1 is one field away.)

## Ink
Regeneration is 1 per second (was 5), so ink is scarce. Pickups give 45. Random ink never spawns above 80% of the tank.
**Rescue ink (reachable by design):** below 45% of the tank, `PickupSpawner.KeepInkReachable` keeps one ink pickup just past the END OF THE
DRAWN ROAD, at half of what the remaining ink can still draw (2.5 to 14 m), about 0.9 m above the road end. If one already exists but is too
far for the ink left, it is pulled in. It runs every frame, so it follows the road as the player draws. Tested with the real brush over 430 m:
ink stayed between 27 and 80 and the car never ran dry.

## Coins
Shapes along the terrain: arc (jump), line (safe path), zigzag (skill), ring, pile. Each coin is worth 10 times the combo multiplier
(1 + combo / 5, max x5, resets after 2.2 s without a coin), doubled by the x2 power-up.

## Power-ups (pickups in the air)
Shield (absorbs one crash, respawn without losing a heart), Magnet (coins and gems fly to the car for 10 s), x2 coins (12 s),
Slow motion (6 s, time scale 0.65), Ghost road (8 s, drawing costs no ink). Gems (rare) are saved in the profile.

## Skill bonuses and chests
Air time over 0.8 s pays per second (counted up to 4 s, so a long fall is no trick); a flip (300 degrees in the air) pays 25 and fills the meter;
a chest every 250 m pays 50 times its number (and sometimes a gem); passing your previous best pays 100 once.
The result screen lists every bonus.

## Economy values applied from the approved change requests
Vehicle prices SportCyan 600 and MonsterTruck 2000 (`GameCatalog.VehicleCost`), `nearMissCoins` 8, `boostDurationPerLevel` 0.08 s (about +6 percent of the 1.4 s base per level).
Not applied (code formulas, not plain data): engine +0.8 m/s per level, suspension +3 torque per level, ink tank +30 per level; the +6 percent target for them is open.

## Daily reward and missions
7-day streak (50 to 300 coins, 3 gems on day 7); a missed day restarts it. Three missions per day from: distance in one run,
coins, flips, air time, boosts, gems, chests. Progress is added when a run ends; the player claims on the Rewards screen.
The day comes from the device clock (offline game).

## Mastery
Each vehicle earns xp per run (distance / 10 plus 5 per flip); level = 1 + one per 100, 200, 300... xp. Tracked in the save and
in `GameModel.MasteryLevel`; no cosmetic is attached to it yet.

## Track properties (road surface)
Each road surface (`CatalogEntry`, filled by `CatalogBuilder`, table `Surfaces`) carries five numbers. Every track is a trade, not a better or worse road.
| Property | Effect | Where applied |
|---|---|---|
| friction, bounce | grip of the road collider (ice slides, mud sticks, cloud and rope bounce) | `ApplySurface` -> road PhysicsMaterial2D |
| `speedFactor` | multiplies the car's target speed (mud drags, ice and glass are fast) | `RoadGameManager` target speed |
| `inkFactor` | multiplies ink per metre (mud and gravel cheap, glass, ice, lava costly). Ink rescue uses the same cost | brush cost, `InkPerMeter` |
| `coinFactor` | multiplies every coin (risky tracks pay more, up to x1.5) | `RunDirector.CoinFactor` |
Asphalt is the baseline (all 1). The Roads tab shows a one-line summary (`CatalogEntry.TraitsLine`). Tune in `CatalogBuilder.Surfaces`, then run Road Builder > Catalog > Build Catalog.

## Stage director (Phase A data layer, Phase B1 wired into the run)
`Assets/Scripts/Game/Stage/` holds the stage ladder as data plus a pure C# core (details of the planner, determinism and pace model are unchanged from Phase A).
`StageEventDefinition`, `StageLadderDefinition`, `EventCompatibilityMatrix` and the new `SurfaceRuleSO` are ScriptableObjects (units in field names, `Validate(errors)` plus an
editor-only `OnValidate` warning). `StageDirector` builds the plan once in `Begin(StageRunRecord)`; `Tick(distanceM, speedMps, scaledDt)` allocates 0 bytes (tested over a whole simulated run with all five behaviours and the presenter attached).

**The one switch:** `GameplayTuning.stageEvents` (`Off`, `CountrysideOnly` = default, `Always`). Off restores the old game exactly (the old meteor spawner, the old surface handling: nothing below runs).
CountrysideOnly means the Country theme and not a custom layered catalogue world. Other defaults applied for B1 (brief section 14): haptics off (`stageHaptics`), boost price 5, retry from 0 m with the same seed.

**Slice assets** (`Assets/Resources/Stage/`, built by menu `Road Builder > Stage > Build Slice Assets`, which runs every `Validate`; safe to re-run): `SRF-ICE`, `SRF-MUD`, `SRF-GLS` (`SurfaceRuleSO`),
seven `StageEventDefinition` (EVT-ICE, EVT-METEOR, EVT-MUD, EVT-GLASS, EVT-RINGS and the two schedule anchors MON-EATER and BOS-ROB0, which have no behaviour until phase B2 and stay silent),
`EventCompatibilityMatrix` (the 21 pairs of stage-events.md section 5) and `StageLadder` (7 slots, section 3.1). A test keeps the `SurfaceRuleSO` base numbers equal to the catalogue entries.

**Wiring** (`RoadGameManager`, region "STAGE EVENTS"): `StageRunController` (Game/Stage, plain class) owns the director and routes `Started`/`Ended` to behaviour modules registered by event id
(no switch over ids): `SurfaceZoneBehaviour` (ice, mud, glass), `MeteorWindowBehaviour`, `RingWindowBehaviour` (Game/Gameplay/StageBehaviours.cs). The manager ticks it every frame in the Playing state
with the distance and **scaled** `Time.deltaTime` (slow-mo stretches a warning, never shortens it). Run start: new run draws a new seed, retry (previous screen was GameOver) reuses the saved one; the
`StageRunRecord` (`runSeed`, `modeId` 1 = Countryside, `layoutVersion`, `debugJumped`) is saved in `PlayerProfile.lastStageRun` through `SaveService` (no PlayerPrefs). Run end (Died, or leaving the screen) ends the Active events, which clears zones and windows.
Respawn: `OnRespawn(distance)` (hazards blocked 1.5 s, no effect starts for 3 s, live meteor markers cleared, next strike one full interval away; the surface zone stays). App pause: nothing ticks, resume gives 0.5 s without hazards.

**Zones drive the existing surface hooks** while Active: friction (road PhysicsMaterial2D), `_trackSpeed`, `_trackInk` (brush cost and ink rescue), `RunDirector.CoinFactor`, and the road look (the catalogue strip of the zone surface; the player's own surface returns when the zone ends). Plus one ink canister 6 m into each zone.
Formulas in `SurfaceZoneModel` (hazards.md section 3): **mud** target = max(3.0, base x 0.85 minus `mudDragMul` x min(2.0, 1.0 x seconds in zone)); cap per vehicle Red 2.0, Cyan 2.5, Monster 1.2 m/s (`GameplayTuning.mudDragMulByVehicle`); a road slope above 18 degrees ahead caps it at 3.0; boost ignores both.
**Ice** slip: a bend over 25 degrees across 4 m of road switches the motor off for min(0.6, 0.6 x (bend - 25) / 20) s (the car coasts, no damage), re-armed after 0.6 s. Not mapped: ice downhill x1.3 / uphill x0.7 (the car is motor-driven, no along-slope term exists) and glass arming, cracking and dropping (the road has no break state): glass is an event zone with its base numbers only.
**Meteor**: when stage-driven, the existing `MeteorSpawner` strikes only inside the EVT-METEOR window, every 3.5 to 5.5 s (seeded by the event's terrain seed, not `UnityEngine.Random`), at most 3 strikes, and only while `CanSpawnHazard("hz-meteor")`. **Rings**: the gauntlet (10 rings, 16 m apart, heights 1 to 6 m, seeded) is placed when EVT-RINGS starts.

**Warnings** (`StageWarningPresenter` writes `GameModel.Stage*`; `HudScreen` shows them): S1 banner (event name, 1.5 s, tinted by the event's UITheme colour token `ZoneIce/ZoneMud/ZoneGlass/ZoneMeteor/ZoneRings`) with the announcer cue, S2 sign ("ICE IN 44 M", counts metres to the start, then metres left) with a sting, S3 a 1 s edge flash on both screen edges with a cue. The word on the sign and banner is the non-colour channel.
Signals that arrive together after a respawn or jump are spaced 0.6 s apart (`signalMinGapS`). Cue ids come from voice-lines.md 3.3 (`StageCues.Registry`); sound is a placeholder map to existing SFX (`StageCues.Placeholder`), only meteor has a recorded voice line.
**QA** (Editor and development builds only): `RoadGameManager.DebugJumpToStage(m)`, menu `Road Builder > Stage > Debug Jump`, and a small `DBG` panel (F9 or tap) listing 0, 220, 480, 640, 710, 1000, 1350, 1800, 2000 m. A jump rebuilds a flat runway, restores ink and hearts and sets `debugJumped` in the run record.
Not built in B1: the Road-Eater, RO-B0, finish gate and result-screen event lines, per-event terrain shaping (slopes, gaps, the variant layouts; only the terrain seed is used, by the ring gauntlet), the world sign prop and road-strip tint of S2 (the HUD sign and tint replace them), glass break state, the effect-start slide of up to 2 s after a late signal.
Tests: `StageDirectorTests` (SD-01 to SD-22) and `StageWiringTests` (W01 to W31: real assets, 2000-seed plans, formulas, simulated run order and lead times, zero allocation, respawn and pause rules, jump, presenter).

## Not built yet
Gems have no shop to spend them in. Not built: the Road-Eater and RO-B0 (phase B2), fuel, weather, challenge modes, checkpoints (the safety-platform respawn stays).
Near-miss and wheelie bonuses. Sounds for power-ups reuse existing clips. Slow-mo is a global time scale (reset on every screen change).

## Art
Pickup icons `Assets/Resources/RoadBuilder/Pickup_*.png` are drawn by `tools/art/make_pickups.py` (procedural, own work).

## Performance (road building)
Cause of the slowdown: a stroke was one road piece that grew without limit and was fully rebuilt (smoothing, spline, geometry, mesh and
collider) every frame, so cost grew with road length (23 ms at 400 points to 43 ms at 3,000, with 100 MB of garbage). Fixes in `RoadBuilder`:
a stroke is cut into chunks of at most `maxKnotsPerChunk` (36) knots, finished chunks are never rebuilt and are culled behind the camera;
stretching the live tip rebuilds at most every 0.04 s; point lookups use binary search and skip chunks outside the range; the road mesh is
destroyed with its chunk (it leaked); `ParticleManager` pools its particles (max 90 alive, no GameObject churn); a car's physics materials
are shared (they leaked two per run). Measured over 430 m of road drawn by `DebugAutoDraw`: frame time flat (road points stay near 270, 3 chunks).
Our scripts allocate about 2 KB per frame; the remainder seen in the Editor is Unity's own.
`DebugAutoDraw` (Editor and development builds only) plays a run by drawing through the real brush and logs frame time every 300 frames.
