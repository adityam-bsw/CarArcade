# QA pass 1 UI fixes - current logic

- **Pause:** `IGameActions.PauseRun/ResumeRun/QuitRun/QuitApp`; `GameModel.Paused` + `PausedChanged`. `RoadGameManager.PauseRun` sets `timeScale 0` (not through ApplyTimeScale), `AudioListener.pause`, stops the brush, `_stage.OnPause(true)`; `Update` returns early while paused so the unscaled run director, Eater, RO-B0, projectiles, meteors and near-miss slow-mo all freeze. Resume calls `_stage.OnPause(false)` (0.5 s hazard grace in scaled time) and restores the run's slow-mo scale. App in background = pause. `QuitRun` banks the run (`TriggerGameOver(..., toMenu: true)`, no revive) and goes Home. `SwitchScreen` always clears pause.
- **UI:** HUD `PauseButton` (top-left), `PauseScreen` popup prefab (Resume, sound toggle, Quit to menu) opened by `UIFlow.ApplyPause`. Android Back: HUD pauses, Pause resumes, Home shows "press back again" for 2 s then quits. HUD warning banner, edge flash and toast tweens are paused with the run.
- **Removed:** dead `CycleWorld/CycleVehicle/CycleRoadStyle` (they wiped the selected ids).
- **Buttons:** screen-driven buttons (garage action and upgrades, rewards claims) use `manualSprite` (no Button sprite swap) and `interactable` follows what is actionable, so disabled stays steel.
- **Text:** builder uses the display font (Lilita One) for all labels instead of the body face; `CoinCounter` keeps numbers clear of the slot edge.
- **Garage:** upgrade plates in a vertical scroll at real size (300x146), cost beside the pips, pip bar visible.
- **Cards:** worlds use cover-fit picture in a clipped box; the postcard stamp is redrawn over the picture; locked taps shake the card and show a themed banner.
- **Result:** offer plates 330x123 with two short lines; the medal baked into the certificate art is covered unless new best.
- **Open (art/tool, outside code):** Rolling Hills preview shows tiled clouds (`tools/assets/make_previews.py` sky layer).
