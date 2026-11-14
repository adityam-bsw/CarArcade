# Redesign Plan: Base Game + Mode Packs

Status: PROPOSED (not started). Replaces the single-scene prototype described in
`ARCHITECTURE_AND_HANDOFF.md`. That document stays valid for the road/car tech, which we keep.

## 1. Idea

One stable **base game** (draw a road under a physics car). Everything that changes how it
*plays or looks* is a **Mode pack**, picked by the player before a run, like Hill Climb's Moon
(low gravity). Modes are data + one small rules class, so adding a mode never touches core code.

| Mode | What it changes |
|---|---|
| Classic | Baseline rules, tutorial, countryside |
| Moon | Low gravity, floaty jumps, longer hang time |
| Void Run | An eraser chases you; road behind you disappears; ink is the road you leave |
| Cargo Courier | Fragile cargo on the car; smooth roads matter; deliveries are levels |
| Rhythm Road | Road and pickups are tied to a beat; drive on the notes |
| Daily Run | Same seed for everyone, ghost of others' roads (works on top of any mode) |

## 2. Layers (from the unity-architecture rule: Manager -> Flow -> View -> Animation -> Event)

```
Core   (asmdef RB.Core)    spline, RoadBuilder, RoadChunk, RoadCar, input brush  [moved, not rewritten]
Game   (asmdef RB.Game)    GameSession, Economy, SaveService, ModeRunner, EventBus
Modes  (asmdef RB.Modes)   ModeDefinition (SO), ModeRules (abstract), one folder per mode
UI     (asmdef RB.UI)      UITheme (SO), themed components, ScreenStack, screen prefabs
```

Dependencies point downward only. Core knows nothing about modes. UI never touches physics.

## 3. Mode API

```csharp
[CreateAssetMenu] class ModeDefinition : ScriptableObject {
    string id, displayName, description; Sprite icon;
    ThemeDefinition world;        // palette, parallax art, road style, music
    UITheme uiTheme;              // optional UI reskin (e.g. pencil notebook for Void Run)
    PhysicsTuning physics;        // gravityScale, driveMult, wheelGrip, speed caps
    EconomyTuning economy;        // inkCostMult, inkRegenMult, coinMult
    ModeRules rulesPrefab;        // behaviour, may be null for pure-data modes (Classic, Moon)
    int unlockCost;
}

abstract class ModeRules : MonoBehaviour {
    virtual void OnEnter(ISessionContext s);
    virtual void Tick(float dt);
    virtual void OnRoadExtended(Vector2 from, Vector2 to);  // Void Run feeds ink from this
    virtual void OnCrash(CrashInfo c);                      // Cargo checks cargo here
    virtual void OnExit();
}
```

`ISessionContext` exposes read access to car, road, camera, ink, score and an event bus. Modes
change the game only through `PhysicsTuning`, `EconomyTuning` and `ModeRules`. Moon needs no
code at all, which is the test that the API is right.

**Mutators** (small stackable modifiers: heavy cargo, low ink, night) reuse the same tuning
structs, so Cargo and Daily can later sit on top of any mode.

## 4. Scenes (additive loading, so no scene carries everything)

| Scene | Contents |
|---|---|
| `Boot` | Loads save, services (Save, Audio, Ads stub, Analytics stub), then Menu |
| `Menu` | Home, Mode select, Garage, Settings, Shop as screen prefabs on a ScreenStack |
| `Gameplay` | Car, road, camera, parallax, HUD. Mode loaded at start from `ModeDefinition` |
| `Mode_<Name>` (optional, later) | Extra world objects a mode needs, loaded additively |

Replaces `SampleScene` and the code-built UI fallback.

## 5. UI system

- **TextMeshPro everywhere.** Remove all legacy `UnityEngine.UI.Text` (81 usages).
- **Font family:** one display font (rounded, chunky) plus one body font, both SIL OFL from
  Google Fonts. Proposed: Lilita One (display) + Baloo 2 (body). Static SDF atlas for numbers,
  dynamic SDF with fallback for the rest. Fonts are downloaded only with your OK.
- **UITheme ScriptableObject:** colours, fonts, 9-slice sprites, corner radius, sounds.
  `ThemedText`, `ThemedImage`, `ThemedButton` components bind to tokens, not hardcoded values,
  so a mode swaps the whole look (pencil style for Void Run) by swapping one asset.
- **Prefab library:** Button, Panel, Popup, Toast, CurrencyPill, StatBar, ModeCard.
- **ScreenStack:** push/pop with a transition. Safe-area handling lives here once.

## 6. Save and services

- One versioned `SaveData` JSON (migrates the existing `HCD_*` PlayerPrefs keys once), written
  atomically. Replaces ten scattered keys.
- Services behind interfaces (`IAds`, `IAnalytics`, `IAudio`) with stubs first, so the game
  runs without any SDK and kid/ads policy decisions stay swappable.

## 7. Build order

| Phase | Deliverable | Done when |
|---|---|---|
| 0 Foundation | asmdefs, folders, SaveService, split `RoadGameManager`, Boot/Menu/Gameplay scenes, Classic ported | Classic plays identically to today |
| 1 UI system | TMP, fonts, UITheme, prefab library, all screens rebuilt | No legacy Text left; looks right at 21:9, 16:9, 4:3, portrait |
| 2 Prove the API | Moon (data only) then Void Run (rules) | Both selectable and distinct without edits to Core |
| 3 More modes | Cargo, Rhythm, Daily | Each is a folder plus assets |
| 4 Release | Real art and audio, ads, Android build, closed test, store listing | Passes pre-release smoke test |

Rule for Phase 0: **move and split, do not redesign.** Behaviour must not change until Classic
is verified, otherwise regressions can't be traced.

## 8. Risks

- Scene and prefab YAML is unsafe to hand-edit; scenes and prefabs are created through the
  Unity Editor (MCP package is already in the manifest) or editor scripts. Needs the Editor open.
- Moving scripts must keep `.meta` files and GUIDs (unity-asset-safety).
- Mode scope creep: each mode must be playable as a one-line pitch before it gets polish.
