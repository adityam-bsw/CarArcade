# 12-Week Roadmap — Road Builder Racer

## Operating rules
- One developer; AI agents work on isolated briefs/branches or generated files.
- One integrated build at the end of each week.
- Do not build all 24 worlds or 50 vehicles before the core loop is proven.
- Every week ends with a physical-device playtest.
- No commit/push unless explicitly requested by the developer.

| Week | Build | Playtest milestone | Exit criteria |
|---|---|---|---|
| 1 | Instrument current build; measure drawing, ink, death causes, run length; define stage/surface schemas | Baseline run with 5 players | Baseline telemetry and core-loop issues documented |
| 2 | Road-Eater prototype with pursuit, road eating, warning and escape | 1000m chase test | No unavoidable road deletion; monster readable |
| 3 | Three surface behaviours: ice, mud, glass | Same course with 3 vehicles | Each surface changes player decisions |
| 4 | RO-B0 robot, 3 projectiles, near-miss reward | Boss prototype | Telegraph comprehension >=90% in test group |
| 5 | StageDirector, deterministic seed, 8 events, breather logic | 5-stage run | No forbidden stacks; reproducible seed |
| 6 | Daily Seed Ghost recording/playback | Async ghost race | Replay deterministic within tolerance |
| 7 | Vehicle rig master + 3 variants; high-res style bible; one world four layers | Visual review on device | Art consistency and memory budget pass |
| 8 | Surface module framework; expand to 10 behaviours; add VFX/audio hooks | Surface gauntlet | No per-frame allocations in hot loops |
| 9 | First-hour progression, vehicle upgrades, coin economy simulation | 60-minute progression test | Meaningful unlock every 3–7 minutes early |
| 10 | Daily/weekly challenge, road code prototype, optional rewarded ad flow | Retention/fairness test | No forced ads; data/privacy review |
| 11 | Expand content to 15–20 events, 15 surfaces, 8 projectile worlds; polish HUD | Long-run stress test | Stable 60fps target profile on selected low-end device |
| 12 | Release candidate, store assets, crash/performance QA, policy checklist | External soft launch | No critical bugs; store claims match build |

## Weekly routine
Monday: define hypothesis and scope.
Tuesday–Wednesday: implement the smallest playable version.
Thursday: integrate art/audio and telemetry.
Friday: device test and tune.
Weekend: collect feedback and decide keep/cut.

## Cut rules
- If a mechanic needs more than 2 weeks for a prototype, reduce scope.
- If players cannot explain the counterplay after 3 encounters, redesign the warning.
- If a feature does not improve fun, retention, sharing or monetisation, do not expand it.
- Multiplayer servers, full live co-op and complex procedural terrain are out of scope for the first 12 weeks.
