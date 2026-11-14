# Phase 0 progress (foundation)

Rule: move and split, do not redesign. Classic must play identically. Updated by replacing entries,
not appending history. Plan: `REDESIGN_PLAN.md`.

## Tooling
- Unity Pipeline package (`com.unity.pipeline` 1.0.0) added to `Packages/manifest.json` so the
  Unity CLI can drive the open Editor (compile, console, scenes, Play mode, screenshots).
  Commands are `unity command <group> <name> --project-path <path>` (e.g. `script recompile`).

## Baseline (Classic, before any refactor)
- `Docs/Images/baseline/classic_home.png`: Home, 150 coins, record 0 m, default red car.
- `Docs/Images/baseline/classic_play.png`: after `OnClickStartRace()` with no input, 4 s in:
  HUD (distance, speed, 3 hearts, coins, ink bar 100%, Boost), car leaves the road end and
  loses a heart.
- Source backup (untracked): scratchpad `baseline/RoadBuilder_scripts` and `SampleScene.unity`.
- Compare against these after each step.

## Steps
| # | Step | Status |
|---|---|---|
| 1 | Pipeline package + Editor connection | done |
| 2 | Baseline captured | done |
| 3 | `SaveService` replaces `HCD_*` PlayerPrefs | done, verified |
| 4 | `Collectible.Collected` event replaces `RoadGameManager.Instance` call | done, verified |
| 5 | Folders + asmdefs `RB.Core` and `RB.Game` (Modes, UI added when they have code) | done, verified |
| 6 | Extract `BrushController`, `PickupSpawner`, `GameCameraRig` | done; see verification note |
| 7 | Extract screen views, isolate `LegacyUIBuilder` | next |
| 8 | Boot / Menu / Gameplay scenes | pending |
| 9 | Final Classic-vs-baseline comparison | pending |

## Current structure
```
Assets/Scripts/
  Core/   (asmdef RB.Core, no dependencies)
    BezierSpline, RoadBuilder, RoadChunk, RoadCar, Collectible, ParallaxBackground,
    SoundManager, ParticleManager, SpriteFactory
  Game/   (asmdef RB.Game -> RB.Core, Unity.InputSystem, UnityEngine.UI)
    RoadGameManager (state machine + UI wiring, 1173 lines, was 1385), SaveService
    Gameplay/  BrushController, PickupSpawner, GameCameraRig
```
Namespace is still `RoadBuilderGame` everywhere. Core knows nothing about Game.

## Current logic: pickups
`Collectible` raises static event `Collected` on car contact (before destroying itself).
`RoadGameManager` subscribes in `OnEnable` / unsubscribes in `OnDisable` and awards the pickup in
`OnCollectItem` (+10 coins, +1 life, +45 ink). Verified: triggering a coin gave +10.

## Current logic: extracted gameplay classes
Plain C# classes created in `RoadGameManager.Awake`, no scene changes, Inspector values stay on the manager.
- `BrushController`: `Tick()` reads pointer (New Input System, legacy mouse fallback), runs the
  stroke (start / continue / end), exponential smoothing. Gets ink through `InkAvailable` and
  spends it through `SpendInk` callbacks. Manager copies `brushSmoothingSpeed`, `dragSpacing`,
  `inkCostPerMeter` into it each frame. `Cancel()` drops the stroke (life loss, new session).
- `PickupSpawner`: `Tick(carX)` spawns coin arcs / ink / hearts ahead on a world sine wave and
  culls behind; `Reset(firstSpawnX)` clears all.
- `GameCameraRig`: `Tick(car, following)` smooth follow + parallax; `Snap(car)` jumps at session start.

## Verification notes (what was and was not proven)
- Compile clean, 0 console errors/warnings, after each step. Home and Gameplay match baseline.
- Stroke logic (direct): started, drew 20 m, road created where none existed, 22.5 ink spent.
- Extracted bodies diffed against the original: identical apart from comments, parameter names,
  and ink spend routed through `SpendInk` (same clamp, same HUD update).
- NOT proven here: real pointer input into `BrushController.Tick()`. The Editor ignores injected
  Input System events while it is not driving the Game view (position reads back 0,0). **Manual
  check needed:** press Play, Start Race, drag a road ahead of the car with the mouse. Expected: road
  appears under the finger, ink bar drains, brush sound and sparks play.

## Current logic: saving
- `Assets/Scripts/Game/SaveService.cs`: `PlayerProfile` (plain data, enums as ints) and static
  `SaveService.Load/Save`. File: `<persistentDataPath>/save.json` (+ `.bak`).
- Load order: `save.json` -> `save.json.bak` -> legacy `HCD_*` PlayerPrefs (imported once, then
  saved to JSON) -> defaults (150 coins, level 1 upgrades). Levels clamped 1..5.
- `RoadGameManager.LoadPlayerData/SavePlayerData` map its public fields to/from `PlayerProfile`.
  Legacy PlayerPrefs keys are left untouched after migration.
- Verified in Play mode: legacy 570 coins / engine 3 / Sport unlock migrated; saved 999 coins
  survived a Play-mode restart; test data cleaned afterwards.
- Known limit: save is not encrypted or cloud-synced (later phase).
