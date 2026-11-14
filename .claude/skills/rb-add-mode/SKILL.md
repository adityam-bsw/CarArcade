---
name: rb-add-mode
description: Step-by-step procedure to add or change a Mode pack (Moon, Void Run, Cargo, Rhythm, Daily) in Road Builder Racer - design the one-line pitch, create the ModeDefinition and optional ModeRules, theme, UI skin, tuning, unlock, and the acceptance tests. Use whenever a new gameplay mode, theme, mutator or world is requested, or when a mode needs to change core rules.
---

# Add a Mode pack

Prerequisite: Phase 0/2 of `Docs/REDESIGN_PLAN.md` (ModeDefinition + ModeRules exist). If they do
not exist yet, stop and build those first.

## 0. Pitch gate (before any file is created)

Write, in this order, and get user agreement:
1. **One-line pitch** a stranger understands in 5 seconds ("An eraser chases you; the road behind
   you is your ink").
2. **What the player does differently** compared with Classic (verb, not theme).
3. **The 15-second clip** someone would share.
4. **Fail and fair-death rule.**
5. Does it need a **new rule** (ModeRules) or just **data** (physics/economy/world)?
If the answer to 2 is "nothing", it is a skin, not a mode. Make it a theme.

## 1. Folder

```
Assets/Modes/<Name>/
  <Name>Mode.asset         (ModeDefinition)
  <Name>Rules.cs           (only if rules needed; asmdef RB.Modes)
  Prefabs/  Art/  Audio/   (all mode-specific assets, logged in Docs/ASSET_LEDGER.md)
  <Name>Theme.asset        (ThemeDefinition: palette, parallax, road style, music)
  <Name>UI.asset           (UITheme, optional reskin)
```

## 2. Rules

- **Data first.** Try physics (`gravityScale`, drive, grip, caps) and economy (`inkCostMult`,
  `inkRegenMult`, `coinMult`) before writing code. Moon is data only.
- **Code only in `ModeRules`.** Use `ISessionContext` and hooks (`OnEnter`, `Tick`,
  `OnRoadExtended`, `OnCrash`, `OnExit`). Never edit Core to special-case a mode. If a hook is
  missing, add a **generic** hook to Core (useful to any mode), not an `if (mode == X)`.
- **Clean up in `OnExit`:** restore Physics2D gravity, kill tweens, destroy spawned objects.
  Switching modes must leave no residue.
- **Tuning in ScriptableObjects**, no magic numbers in the rules class.
- **Visuals:** own ThemeDefinition; UI reskin through `UITheme` only. Motion follows
  `rb-motion-design`, UX follows `rb-game-ux`.
- **Mode HUD element:** use the reserved top-centre slot; never cover the drawing area.

## 3. Wire up

1. Register the `ModeDefinition` in the `ModeCatalog` (one list, drives Mode select).
2. Add unlock rule and price (`unlockCost` or goal) and the card preview.
3. Save: per-mode best score through `SaveService`, never a new PlayerPrefs key.
4. Add the mode to Daily Run's eligible list if suitable.

## 4. Acceptance tests (all must pass)

- [ ] Selectable from Menu, starts, plays, ends, returns, and Classic still plays unchanged.
- [ ] Switch Classic -> Mode -> Classic five times: gravity, speed, UI and road identical to fresh.
- [ ] Distinct in a screenshot and in 5 seconds of gameplay (show a friend, ask what changed).
- [ ] 60 fps on the low-end target; no per-frame allocations from the rules class.
- [ ] No fair-death complaints in a 5-player playtest (`unity-playtest-script`).
- [ ] Ledger and credits updated, graph refreshed (`graphify update .`), docs updated.

## Mode ideas ready to implement (with their rule hooks)

- **Moon:** `physics.gravityScale 0.38`, longer hang time, camera size +10%. Data only.
- **Void Run:** Eraser entity advances with a speed curve; `OnRoadExtended` credits ink;
  the Eraser deletes road chunks behind (Core `CullBefore`); touching it costs a life; rescue
  boost once per run.
- **Cargo Courier:** cargo rigidbody on the car; `OnCrash`/tilt tracking; score by cargo health
  and time; levels as a list of delivery distances.
- **Rhythm Road:** beat clock; pickups placed on beats; draw to hit them; combo multiplier.
- **Daily Run:** seeded collectible/obstacle spawner + ghost of the day's best road. Works as a
  wrapper over any mode.
