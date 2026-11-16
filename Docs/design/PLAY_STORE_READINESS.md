# Play Store readiness (living checklist, owner: unity-implementer + qa-playtester + store-marketer)

## Audit of current ProjectSettings (2026-11-15)
| Item | Current | Status |
|---|---|---|
| Scripting backend (Android) | IL2CPP | OK |
| Architectures | ARM64 only | OK (Play requires 64-bit) |
| Min SDK | 25 | OK |
| Target SDK | 0 (automatic highest) | Check it meets the current Play target-API requirement at upload |
| Package id | `Com.AM_Games_RoadBuilder` | **DECISION NEEDED**: uppercase and underscores, permanent once published. Suggest `com.amgames.roadbuilder` |
| Version / code | 1.0 / 1 | OK for first upload; bump code every upload |
| Orientation | Auto (4) | Decide: lock to one orientation if the game is landscape-only |
| Managed stripping | not set | Set Medium + verify DOTween/TMP survive on device |
| Release keystore | none in repo | **User action**: create upload key, keep it out of git |
| Output format | APK? | Build an **AAB** for Play |
| Ads / IAP / analytics SDK | none installed | Choose only after target audience decision (Families policy) |
| Debug / profiler in release | unchecked | Ensure Development Build off |

## Needed before upload
1. Target audience decision (children or general) -> drives ads SDK and consent.
2. Privacy policy URL, Data Safety form, content rating questionnaire.
3. App icon (adaptive), feature graphic 1024x500, 2+ phone screenshots.
4. Release build on a real low-end device: 60 fps, cold start < 4 s, no ANR, no per-frame GC.
5. Asset ledger complete (`AssetLedgerCoverageTests` passing), CC-BY credits screen if any CC-BY asset is used.
6. Internal testing track, then closed test, then production.

## Cheat panel (added 2026-11-16)
- `GameplayTuning.cheatsEnabled` (Resources/Gameplay/GameplayTuning.asset) shows the CHEAT button and panel (`CheatPanel`, `RoadGameManager.Cheats.cs`). Default TRUE during testing.
- **Must be FALSE (or the panel compiled out) for any Play Store build.** Check it in the release checklist; the cheat actions are inert when it is false.
