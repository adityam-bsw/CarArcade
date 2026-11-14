# Road Builder Racer: project rules

Unity 6 (6000.3.20f1), URP 2D, mobile first (Android target, Google Play). One developer plus AI,
no art or design team. Read `Docs/REDESIGN_PLAN.md` before any structural work.

## Architecture the game must follow
- **Base game + Mode packs.** Core never references a mode. A mode changes the game only through
  `ModeDefinition` data (physics, economy, world, UI theme) and one `ModeRules` class.
- **Layers, downward dependencies only:** Core -> Game -> Modes -> UI. Each is an asmdef.
- **Manager -> Flow -> View -> Animation -> Event.** Views only display and raise events. No
  gameplay logic in UI scripts. No `FindObjectOfType` in hot paths. Static singletons only for
  services created in `Boot`.
- **Scenes:** `Boot` and `Main` (+ optional additive `Mode_*`). `Main` keeps world and menus together by
  design. UI is prefabs on a ScreenStack, never built in code at runtime. Screens read `GameServices.Model`
  and call `GameServices.Actions`; they hold no game rules.
- **UI:** TextMeshPro only, no legacy `UI.Text`. Colours, fonts, sprites come from `UITheme`
  tokens, never hardcoded in scenes or scripts.
- **Animation:** DOTween for UI and feel (see skill `rb-dotween`). Rules in `rb-motion-design`.
- **Data:** tuning lives in ScriptableObjects, not magic numbers. Save through `SaveService` only
  (no new `PlayerPrefs` keys).
- **Assets:** every third-party asset is logged in `Docs/ASSET_LEDGER.md` with source and
  licence (see `rb-asset-pipeline`). No ledger entry, no merge.

## Working rules
- **Code lookups use the graph first:** `graphify explain "Type"` / `graphify query "..."`, then
  read only the lines you edit. After code edits run `graphify update .`.
  (`graphify` lives in `~/.local/bin`.) Do not open `.unity`/`.prefab` YAML to search.
- **Scenes and prefabs** are edited through the Unity Editor (MCP) or editor scripts, never by
  hand-editing YAML. Keep `.meta` files with their assets. Move assets inside Unity.
- **Phase 0 rule:** when moving or splitting code, behaviour must not change.
- **Mobile budgets:** 60 fps on a low-end phone, no per-frame allocations in gameplay, textures
  atlased and compressed, cold start under 4 s. Measure before optimising (`unity-perf-lowend`).
- Never commit, push or discard changes unless the user asks.
- After a change that alters behaviour or structure, update the relevant file in `Docs/`
  (`unity-change-tracker`).

## Project skills (in `.claude/skills/`)
| Skill | Use when |
|---|---|
| `rb-asset-pipeline` | Need art, audio, fonts or icons: find free ones, build our own, import for mobile |
| `rb-game-ux` | Designing any screen, flow, HUD, onboarding, reward or store moment |
| `rb-dotween` | Writing any tween: API rules, patterns, cleanup, pitfalls |
| `rb-motion-design` | Deciding what should move, how long, which easing, what feel |
| `rb-add-mode` | Adding or changing a Mode pack |

Global Unity skills (`unity-*`) also apply: architecture, canvas UI, audio juice, android release,
kids safety, perf, playtest script.
