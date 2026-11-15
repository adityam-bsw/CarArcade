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
**Surface behaviour (section C).** On top of the five numbers, the selected surface runs a behaviour set (`Assets/Resources/Stage/Surfaces/BHV-*.asset`, built by Road Builder > Stage > Build Surface Modules) in a run: ice slides on sharp bends, mud and honey sink, glass and lava crust crack and drop when you stand still, cloud dissolves 2 s after contact, rope bridge and floating rocks sway, neon pads and spring road launch every third piece, wood burns when hit by fire, magnet rail pulls metal vehicles down, acid drains durability, oil and gravel rock the car, sticky candy grabs then releases, ice crystals snag unless you boost, haunted shadow fades but stays solid, electric rail cuts the drive in charged windows, fragile paper tears under heavy vehicles, water floats light vehicles, cobblestone shakes the suspension, lava river platforms sink in turn. Unmapped surfaces keep the old friction and speed only. Details: `Docs/design/hazards.md` section 3.

## Stage director (Phase A data layer, Phase B1 wired into the run)
`Assets/Scripts/Game/Stage/` holds the stage ladder as data plus a pure C# core (details of the planner, determinism and pace model are unchanged from Phase A).
`StageEventDefinition`, `StageLadderDefinition`, `EventCompatibilityMatrix` and the new `SurfaceRuleSO` are ScriptableObjects (units in field names, `Validate(errors)` plus an
editor-only `OnValidate` warning). `StageDirector` builds the plan once in `Begin(StageRunRecord)`; `Tick(distanceM, speedMps, scaledDt)` allocates 0 bytes (tested over a whole simulated run with all five behaviours and the presenter attached).

**The one switch:** `GameplayTuning.stageEvents` (`Off`, `CountrysideOnly`, `Always` = default since batch A1). Countryside runs the 7-slot slice and then the endless ladder; every other world runs the endless ladder from 0 m. Off restores the old game exactly (the old meteor spawner, the old surface handling: nothing below runs).
CountrysideOnly means the Country theme and not a custom layered catalogue world. Other defaults applied for B1 (brief section 14): haptics off (`stageHaptics`), boost price 5, retry from 0 m with the same seed.

**Slice assets** (`Assets/Resources/Stage/`, built by menu `Road Builder > Stage > Build Slice Assets`, which runs every `Validate`; safe to re-run): `SRF-ICE`, `SRF-MUD`, `SRF-GLS` (`SurfaceRuleSO`),
seven `StageEventDefinition` (EVT-ICE, EVT-METEOR, EVT-MUD, EVT-GLASS, EVT-RINGS and the two events MON-EATER and BOS-ROB0, which get their behaviour in phase B2, see the next section),
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
Not built in B1/B2: result-screen event lines, per-event terrain shaping (slopes, gaps, the variant layouts; only the terrain seed is used, by the ring gauntlet), the world sign prop and road-strip tint of S2 (the HUD sign and tint replace them), glass break state, the effect-start slide of up to 2 s after a late signal.
Tests: `StageDirectorTests` (SD-01 to SD-22) and `StageWiringTests` (W01 to W31: real assets, 2000-seed plans, formulas, simulated run order and lead times, zero allocation, respawn and pause rules, jump, presenter).

## Mutation events and the endless ladder (gap list A1, behind the same `stageEvents` switch)
**Twelve mutations** (data in `Assets/Resources/Stage/Events/EVT-*.asset`, built by Road Builder > Stage > Build Mutation Pack; numbers are the blueprint A2 table): Gravity Shift (vehicle gravity x0.65, 450 m), Heavy Gravity (x1.6, 400 m), Speed Tunnel (target speed x1.45, 300 m), Ink Storm (ink regen x0.5 plus seeded wind gusts of 4 m/s^2 peak, 1.2 s each, 450 m), Boost Drain (a running boost burns 2x faster, 400 m), Coin Vacuum (coins within 7 m drift away at 4.5 m/s, at most 6 m, 350 m), Wind Canyon (steady 3 m/s^2 side push with a slow swell, 450 m), Low Ink (tank x0.45, 300 m), Road Tax (10 coins every 100 m unless a coin gate was crossed; gates sit 40 m before each checkpoint; only coins of this run are taken, never below 0, 400 m), Fragile Vehicle (a collision harder than 60 percent of a 12 m/s reference impact costs a heart, 350 m), Tiny (vehicle x0.55) and Giant (x1.6), 300 m each.
**How it works.** `StageEventDefinition.effects` lists `StageEffectSpec` (kind, a, b). One behaviour per event id (`StageEffectBehaviour`, or `RoadTaxBehaviour` for the tax kind; `StageEffectRegistry.RegisterAll` registers them from the data, no switch on ids) adds its specs to `StageEffectState` on start and removes them on end. The manager folds the state once per frame (`ApplyStageEffects`): car gravity scale, wind acceleration and scale on `RoadCar` (`SetGravityMul`, `WindAccel`, `SetScale`), target speed, ink regen and tank size, `RunDirector.BoostDrainMul`, `Collectible.RepelTarget`, impact check. The world gravity is never touched. Effects revert on event end, death, restart and quit (`EndStageRun` resets the state and re-applies), are frozen by pause (scaled time) and survive a respawn (the scale persists, wheels are re-seated). Behaviours that need a tick implement `IStageTickBehaviour` (zero allocation); Road Tax re-places its gates after a jump or revive (`IStagePickupsAware`).
**Vehicle scale** (`RoadCar.SetScale`): body and wheel transforms scale (colliders follow), mass and torque scale with s^2, wheel joints are rebuilt on the scaled anchors, wheels re-seated, a growing car is lifted. Reverts exactly. Fixed on the way: the motor target used a 0.25 m wheel-radius floor, so a Tiny wheel never reached the target speed; the floor now scales with the car.
**Warnings and learning.** Same three signals as every event (S1 banner 5 s, S2 sign word 3 s, S3 cue 1 s), zone colours `ZoneGravity/Speed/Ink/Wind/Economy/Body/Scale`, cue ids `vo.rob0.s1.KEY`, `sfx.KEY.sign`, `sfx.KEY.cue` (placeholders), sign word as the non-colour channel. First sighting shows rule and counterplay, second the counterplay, then only the name (`StageCues.Preview`, counter `evtseen:ID` in the profile counters).
**Compatibility** (`EndlessCompatibilityMatrix.asset`, all 136 pairs of the 17 pool events explicit): never-stack list includes tiny+giant, gravity+gravity, heavy gravity+fragile, fragile+meteor, fragile+wind, monster+boss, speed+wind, two winds, ink storm+low ink; Allow pairs are the compatible stacks. The 21-pair slice matrix is unchanged.
**Endless ladder** (`StageLadderMode.Endless`, `Resources/Stage/EndlessLadder.asset`, `EndlessLadderPlanner`): stage k covers [k x 1000, (k+1) x 1000) m. 0 training (nothing), 1 Road-Eater (fixed), 2 first physics mutation, 3 movement, 4 environmental, 5 two compatible modifiers, 6 elite (Hard+), 7 surface combo, 8 RO-B0 (fixed), 9 extreme, 10 survival gauntlet, 11+ controlled random. Per stage one primary drawn by tier (Easy/Medium/Hard/Extreme from `difficultyCost`, weights 40/35/20/5 among the tiers that have a candidate), then by event weight, among events whose `suitableWorlds` match the world tags (`StageLadderDefinition.worldTags`, 24 catalogue worlds; `all` suits every world). Rows relax in a fixed order (tag, then tier range) and never the family window. Rules: at most 2 events active together (`endlessModifierCap`), from 5000 m a stacked event starts at least 100 m after the primary, no event may overlap another without an explicit Allow pair, no same primary twice in a row, a hazard family at most twice in four stages, a breather stage (no event) after two hard stages (cost >= 7, fixed stages exempt), seed per stage = unchecked(runSeed + stageIndex + modeId) (mode ids: 1 Countryside slice, 2 endless), every stage consumes the same draws so a later change never shifts an earlier stage. Event distance windows of the slice are ignored in the endless ladder.
**Keep going.** Reaching the slice finish gate no longer ends the run: the course is credited as complete (`finished` for progression), a banner says KEEP GOING and `StageRunController.ContinueEndless` plans the endless ladder from the gate (first stage whose warning starts after it). Dying later is a normal game over. The endless ladder has no finish gate.
**Adaptive Challenge** (Settings toggle, `PlayerProfile.adaptiveChallenge`, default OFF): with it on, a death in an endless stage counts as a fail of that stage (`stagefail:K` counter, cleared when the stage is passed); after 3 fails the next plan eases that stage (Hard and Extreme weights x0.35 and one easier tier allowed). Visible indicator: the ON/OFF switch in Settings and the banner "EASIER STAGE (ADAPTIVE)" when the eased stage starts.
**QA.** Editor and development builds only: `MutationFxProbe.Run()` / `RunLadders()` (Gameplay/MutationFxProbe.cs) drives a real run through each event and logs before/during/after values with the prefix `[FxProbe]`; `RoadGameManager.Debug*` accessors; `StageDirector.DebugInject`. Needs Play mode with `Application.runInBackground = true` when the editor window is not focused.

## Road-Eater, RO-B0, near-miss economy (Phase B2, behind the same `stageEvents` switch)
Specs: hazards.md (numbers) and the brief F2 to F4. All numbers are ScriptableObjects (`Assets/Scripts/Game/Hazards/`, assets in `Resources/Stage/Hazards/`, built by menu `Road Builder > Stage > Build Hazard Assets`, code defaults used when an asset is missing):
`MonsterDefinitionSO`, `BossDefinitionSO`, `ProjectileDefinitionSO` (bolt, bomb, roller), `ProjectileSkinSO` (Countryside: Corn Shot, Pumpkin Bomb, Hay Roller; own procedural circle shapes, no art), `AttackPatternSO` (AP-1 to AP-5). Behaviours are registered by event id on `StageRunController` with `DelegateBehaviour` (no switch).
**Road-Eater** (`RoadEaterModel`, pure, zero allocation; `RoadEaterController` is the scene side). S1 now fires at 940 m (planner fix: the Eater's warn lead is exactly `eaterWarnLeadM`, 60 m, not the longer 5 s x 16 m/s), `StartChase` at 1000 m spawns it 32 m behind (never under 18 m, or at the oldest drawn road). Gap controller ratio 0.85 / 1.0 at 18 / 1.25 at 36 / 0.9 beyond, hard cap 1.25 x v_ref, accel 4 / decel 5 m/s2.
Surge (seeded 12 to 16 s, only from a gap of 21 m or more, 1.2 s telegraph, 2.5 s), Snap (gap under 6 m: 1.0 s, closing frozen, cancels at 8 m, waits up to 1.5 s while airborne) then Bite = the normal crash path (a shield absorbs it) and Pushback (gap 32 m, 1.5 s frozen, 1.5 s grace). Escape = gap 45 m held 8 s (reset after 0.5 s under 43 m); timeout retreat when the car reaches 1750 m or the event window ends.
Chew delay: a crossed road gap of 6 to 14 m at the eat front slows the Eater to 0.4 for 2 s (8 s cooldown, max 4 per chase). Deletion: `RoadBuilder.EatBefore(frontX, margin, quantum, maxTrims)` every 0.1 s: whole chunks behind the front go at once, the straddling chunk is cut back (quantised to 1 m) and its mesh and collider rebuilt in the same call. The front is clamped 6 m behind the car; the active chunk, safety platforms (`Segment.noEat`) and `AddNoEatRange` ranges (set for events with `noEat`, glass) are never eaten.
**RO-B0** (`BossModel` pure, `BossController` scene): starts when BOS-ROB0 starts (2000 m), 2 s intro; no HP, Overload meter 0 to 100 (+0.2/s passive, dodge 3, near 5, perfect 6, ring 8, hit -8, shield hit 0); phases at 33 and 66 (2.0 s telegraph, 2.5 s quiet); patterns picked by a seeded generator, never the same twice; exactly one gold ring in each 3 s lull;
FR-4 gate (boost full, 5 coins or 15 percent ink) downgrades bolts to warning shots, direct bombs to lead bombs, skips rollers; a stationary car only gets warning shots. Hover = anchor ahead and above the car plus a sine, tell pose scales the sine to 20 percent. Tells: bolt 1.2 s (aim line turns 15/20/25 deg/s, locks 0.4 s before firing), bomb 1.5 s (exact blast marker), roller 1.4 s. The fight outlives the planned 600 m window: it ends by Overload (4 s defeat) or the 105 s timeout; hits need `_hitGraceLeft` (1.5 s after any hit) and the stage grace to be clear.
**Near miss** (`NearMissLedger`, stage mode only; the old flat bonus stays when `stageEvents` is Off): chain 8/10/12/15 within 4 s, perfect x2, Eater 20, run cap 12 percent (`min(step, floor(0.12/0.88 x other coins) - paid)`), lingering guard (8 m travelled in the tell, 3 m/s over the last second, 1.5 s per source, 1.5 s after a respawn, voided in a hit frame); config in `GameplayTuning`. A capped near miss still toasts and gives frenzy.
**Payouts** (`HazardRewards`, counters `eaterEscapes/eaterTimeouts/bossDefeats/bossTimeouts` in `PlayerProfile`, saved by SaveService): Eater escape 150 first / 75 repeat, timeout 75 / 38; boss defeat 200 + 1 gem first / 100 repeat, timeout 100 / 50. **Finish gate**: `max(plan.FinishM 2750, boss end + 150)`, not while the boss is fighting: the run ends as Finished ("COURSE COMPLETE!") through the game-over path.
Cue ids are in `StageCues.Registry` (placeholder sounds). Not built: the glass break state and 6 m glass entry platform, edge-meter HUD and vignette (the model fields `GameModel.Eater*`/`Boss*` are written for a HUD), Eater/boss catalogue card unlock, pickups on eaten road are not removed, finish-gate prop and slow-mo, hit stop on a perfect near miss, forced rescue ink during the chase (the existing ink rescue below 45 percent runs).
Tests: `HazardModelTests` (Eater controller, ledger, payouts, boss patterns, meter, phases, timeout, zero allocation, trim API).

## Not built yet
Gems have no shop to spend them in. Not built: fuel, weather, challenge modes, checkpoints (the safety-platform respawn stays).
Wheelie bonus. Sounds for power-ups reuse existing clips. Slow-mo is a global time scale (reset on every screen change).

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

## Hazards (gap list B, 2026-11-16)
- World projectile: chosen by world id (WorldProjectiles.ForWorld) and fired by boss Bolt beats in non-Countryside worlds; pattern, speed, telegraph and persistent surface effect are ProjectileDefinitionSO data. At most one persistent patch per projectile, reported to ProjectileSurfaceHooks.Current (C).
- Boss tier by distance of the encounter (Scout, 3000 Hunter, 5000 Interceptor, 8000 Commander, 10000 Overlord); counter meter 10 points; third unshielded hit ends the round without reward; defeat pays per BossTierSO.
- Road-Eater world variant (EaterVariantSO): one signature attack module and one counterplay option (boost-ring lure, collapse trap, heavy-vehicle platform, three pursuit phases, final gate hold 8 s). Defeat through the option pays 250-500 coins + trophy + 1 mastery point.
- Announcer: see design/voice-lines.md section 9.
