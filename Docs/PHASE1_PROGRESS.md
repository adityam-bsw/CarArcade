# Phase 1 progress (UI system)

Goal: TextMeshPro everywhere, one theme drives the whole look, screens are prefabs on a stack.
Plan: `REDESIGN_PLAN.md` section 5. This file is replaced, not appended, as things change.

## Status: UI work complete (needs a device test)
| Step | Status |
|---|---|
| TMP Essentials imported | done |
| `RB.UI` assembly: theme, themed components, screen stack, safe area | done |
| Default theme, generated 9-slice shapes, gear and star icons | done |
| Fonts: Lilita One (display) + Baloo 2 (body), TMP SDF assets, in theme | done |
| DOTween imported and wired (`UIAnim`, `PressFeedback`, `AnimatedScreen`) | done |
| Six screens as prefabs: Home, Worlds, Garage, HUD, Result, Settings | done |
| Legacy runtime-built UI removed from `RoadGameManager` (1,173 -> 621 lines) | done |
| `Boot` scene added; `SampleScene` renamed `Main`; build settings set | done |
| Menu / Gameplay scene split | deferred on purpose, see below |

## Decision: no Menu/Gameplay scene split (yet)
The plan listed `Boot`, `Menu`, `Gameplay`. The menus show a showcase car on a podium over the same
world (road, parallax, camera) the run uses, and Retry must start in under a second. Splitting would
duplicate the world in two scenes and reload it on every Retry. So there are two scenes: `Boot`
(settings, then loads `Main`) and `Main` (world + manager + `UIRoot`). The scene still holds no UI logic:
all screens are prefabs. Revisit if load time or memory demands it, or when mode packs need additive scenes.

## Structure
```
Assets/Scripts/Game/Model/   GameModel (observable state + events), IGameActions, GameServices, GameCatalog
Assets/Scripts/Game/Bootstrap.cs        Boot scene entry
Assets/Scripts/UI/           (asmdef RB.UI -> Core, Game, TextMeshPro, uGUI, Input System, DOTween)
  Theme/      UITheme, UIThemeManager, ThemedText, ThemedImage
  Animation/  UIAnim (all tweens), PressFeedback (button squash + click event)
  Screens/    UIScreen, AnimatedScreen, ScreenStack, UIFlow, Home/Worlds/Garage/Hud/Result/Settings
  Layout/     SafeAreaFitter
  Editor/     UIThemeFactory, FontAssetFactory, IconFactory, UIPrefabBuilder, UIKitSceneBuilder
Assets/Prefabs/UI/UIRoot.prefab         Canvas + SafeArea + ScreenStack + UIFlow
Assets/Prefabs/UI/Screens/*.prefab      the six screens (generated, then editable by hand)
Assets/Fonts/, Assets/Art/UI/Generated/, Assets/Resources/UI/UITheme_Default.asset
Assets/Scenes/Boot.unity, Main.unity (game), UIKit.unity (dev sandbox, not in build)
```
Rebuild generated assets from the menu: Road Builder > UI > Create Default Theme / Build Font Assets /
Build Icons / Build Screen Prefabs. Re-running Build Screen Prefabs overwrites hand edits to those prefabs.

## Current logic: how UI and game talk
- The manager (Game layer) owns the rules. It copies state into `GameServices.Model` (`SyncModel` for menu
  data, `SyncHud` every frame while playing) and raises `Changed` / `HudChanged` / `ScreenChanged`.
- Screens read the model and call `GameServices.Actions` (`IGameActions`, implemented by the manager):
  StartRace, GoHome, OpenGarage, OpenWorlds, Boost, UpgradeStat, SelectOrUnlockVehicle, CycleVehicle,
  CycleWorld, CycleRoadStyle, SetSoundEnabled. Screens contain no game rules.
- `UIFlow` listens to `ScreenChanged` and makes the matching screen the root of the `ScreenStack`
  (Home, Worlds, Garage, Hud, Result). Settings is a popup pushed from Home. Escape/Android Back calls the
  top screen's `OnBackPressed`: Worlds, Garage and Result go Home; Home and HUD ignore it.
- `PressFeedback.Pressed` fires on pointer-down for every button; `UIFlow` plays the click sound from it
  (so game actions no longer play it).
- HUD refresh runs every frame but only touches a label when its value changed (no per-frame string work).
- Run end: manager fills `Model.LastRun` (reason, distance, coins, new best) before switching to Result.
- Sound toggle: `SetSoundEnabled` sets `AudioListener.volume` and saves `soundEnabled` in the profile.
- Behaviour change vs the old UI: starting a race with a locked vehicle selected now falls back to the
  Classic car (before, a locked car could be raced).

## Motion in use (rb-motion-design values)
Screens: fade 0.2 s (Home, Garage, HUD), slide-up 0.3 s (Worlds), pop with overshoot 0.28 s (Result,
Settings); exit fade 0.15 s. Buttons squash to the theme's pressed scale in 0.08 s and spring back in 0.25 s.
Result distance counts up. Punches on: coins gained, heart lost or gained, upgrade level up, world change,
new-best badge. Low ink (under 20%) pulses the ink bar. All tweens are linked to their object and use
unscaled time.

## Verified (Editor, Play mode, Boot scene start)
- Compile clean, 0 console errors or warnings.
- Screens captured and reviewed: Home, Worlds, Garage, HUD, Result (new best), Settings.
- Actions: upgrade (290 -> 240 coins, engine level 2), unlock Sport (200 coins), truck refused when
  unaffordable, locked-vehicle fallback, retry from Result, sound toggle (volume 0 and back to 1).
- Boot loads Main and lands on Home. 21:9 layout reviewed. Test saves were restored afterwards.

## Not verified yet (needs a device or manual check)
- Real touch/mouse on buttons: the Editor ignores injected input, so button presses and the Android Back
  key were tested only through their actions. Please click through every screen in Play mode.
- Draw-road input (`BrushController`, still the open item from Phase 0).
- 4:3 tablet and notch devices (anchors and `SafeAreaFitter` are in place, not seen on hardware). Portrait is
  disabled in Player Settings, so it is out of scope.
- Press-and-hold feel, tween smoothness on a low-end phone, tap targets at real phone size.

## Known gaps and cleanup
- Text has no outline; the Worlds preview images are the old placeholder art.
- Both fonts lack the star glyph (a drawn star icon is used instead).
- Now unused legacy art: `Assets/Resources/RoadBuilder/Art/Btn_*.png`, `GameLogoBanner.png`, `StatBarBg.png`,
  `Assets/Resources/RoadBuilder/UI/*`. Not deleted; remove after you confirm.
- The `HomeScreen` etc. rely on `Resources.Load` for world previews; move to references when modes arrive.
- `RoadGameManager` still holds economy, run rules and vehicle/world selection (621 lines). Splitting it is
  Phase 2 work alongside the mode API.
- Android package id is `Com.AM_Games_RoadBuilder`: use a lower-case id (for example `com.amgames.roadbuilder`)
  before the first Play upload, because it can never change afterwards.
