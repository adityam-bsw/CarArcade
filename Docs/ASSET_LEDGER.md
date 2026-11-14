# Asset Ledger

Every third-party asset (art, audio, fonts, icons, plugins) is logged here. No row, no merge.
The in-game Credits screen is generated from this file. See `.claude/skills/rb-asset-pipeline`.

| File(s) in project | Source URL | Author | Licence | Credit needed? | Date | Used in |
|---|---|---|---|---|---|---|
| Assets/Resources/RoadBuilder/* (current placeholder art and procedural audio) | made in-house for the prototype | project team | own work | no | 2026-10 | Classic. To be replaced in Phase 4 |
| Assets/Fonts/LilitaOne/LilitaOne-Regular.ttf, `LilitaOne SDF.asset`, OFL.txt | https://github.com/google/fonts/tree/main/ofl/lilitaone | Juan Montoreano | SIL OFL 1.1 | Yes, credit in Credits screen. Keep OFL.txt. Unmodified (Reserved Font Name "Lilita") | 2026-11-14 | Display font: titles, buttons, numbers |
| Assets/Fonts/Baloo2/Baloo2-Variable.ttf, `Baloo2 SDF.asset`, OFL.txt | https://github.com/google/fonts/tree/main/ofl/baloo2 | The Baloo 2 Project Authors (EkType) | SIL OFL 1.1 | Yes, credit in Credits screen. Keep OFL.txt | 2026-11-14 | Body font: captions and descriptions (default weight only; variable axis unused) |
| Assets/Art/UI/Generated/ui_*.png | generated in-house by `UIThemeFactory` | project team | own work | no | 2026-11-14 | UI shapes (button, panel, popup, card, pill, bars) |
| Assets/Plugins/Demigiant/DOTween/*, Assets/Resources/DOTweenSettings.asset | https://dotween.demigiant.com/ | Daniele Giardini (Demigiant) | DOTween free licence (imported by the user from the official download) | Terms not yet re-read by us: confirm at https://dotween.demigiant.com/license.php before release whether credit is requested | 2026-11-14 | All UI and feel animation (`UIAnim`) |
| Assets/TextMesh Pro/* | Unity package `com.unity.ugui` (TMP Essential Resources) | Unity Technologies | Unity package licence | no | 2026-11-14 | Text rendering; includes Liberation Sans (SIL OFL) as the fallback font |
