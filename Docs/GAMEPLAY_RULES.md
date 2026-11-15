# Gameplay rules: boost, ink, coins, power-ups, rewards

Status: built and tested (12 EditMode tests in `GameplayLogicTests`, Play mode run of Home, Garage, Rewards, HUD, Result).
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

## Stage director (data layer, Phase A; not wired into the run)
`Assets/Scripts/Game/Stage/` holds the stage ladder as data plus a pure C# core. Nothing in the scene or the existing run uses it yet.
`StageEventDefinition`, `StageLadderDefinition` and `EventCompatibilityMatrix` are ScriptableObjects (units in field names, `Validate(errors)` plus an
editor-only `OnValidate` warning). `StageDirector` (behind `IStageDirector`) builds the plan once in `Begin(StageRunRecord)` and `Tick(distanceM, speedMps, scaledDt)`
only compares distance with precomputed thresholds (0 bytes allocated, tested). Plan = pure function of (ladder, `runSeed`, `modeId`): per slot a PCG32 seeded
`unchecked(runSeed + stageIndex + modeId)` (`runSeed` generated as raw shifted left 7), draw order jitter, variant, replacement (only on rejection), terrain seed.
Rules in code come from data: breather = stricter of `cooldownAfterM` and `minBreatherS` seconds (closed-form pace model, `StagePace`), start ranges shrink backwards
from fixed anchors so the next event can always keep its breather, matrix default deny, Rewards (ring gauntlet) exempt from the warning-phase overlap term,
modifiers must fit inside their parent. `FixedSpine` plays the slice slots; `Weighted` is the endless ladder (no repeat of the previous primary, difficulty ramp
`cost <= 3 + slot`, breather after two hard slots, rejection fallback by descending weight). Signals S1 to S3 are distances computed with `signalSpeedFloorMps` (16).
Not built: scene wiring, spawner enabling, UI banners, `SurfaceRuleSO`, prefab/audio refs on the definition, early chase end, ladder and event assets.
Tests: `Assets/Tests/EditMode/StageDirectorTests.cs` (SD-01 to SD-22; SD-17 and SD-20 are not automatable and report Ignored).

## Not built yet
Gems have no shop to spend them in. Not built: hazards, fuel, weather, challenge modes, checkpoints (the safety-platform respawn stays).
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
