---
name: stage-event-design
description: Use when designing, tuning, or implementing distance-triggered stage events and mutations.
---
# Stage Event Design & Implementation

## Trigger
Use for every 1000m stage mutation, event pool change, compatibility rule, warning, or event telemetry task.

## Procedure
1. Read the current StageDirector, distance tracker, mode rules, event definitions, and telemetry.
2. Write a one-sentence player fantasy and the exact counterplay.
3. Define trigger distance, warning distance (default 150m), active duration (350–650m), cooldown, difficulty (1–10), and reward.
4. Define the event as immutable ScriptableObject data plus isolated runtime state.
5. Add compatibility and forbidden-event IDs. Never assume two events can stack safely.
6. Add warning cues: announcer at 5s, visual warning at 3s, hazard cue at 1s; use distance-based cues if timing is unreliable.
7. Add deterministic selection using runSeed + stageIndex + modeId.
8. Add pause, death, restart, app-resume, and cleanup handling.
9. Add telemetry for selected, warned, activated, countered, completed, failed.
10. Test with fixed seeds and 20 repeatable runs before enabling random selection.

## Rules
- Never repeat the same primary event twice consecutively.
- Guarantee a breather after two high-difficulty stages.
- Maximum two normal modifiers; bosses replace overlapping attack patterns.
- Never spawn unavoidable damage.
- Do not allocate memory in per-frame Tick.
- Do not put event rules in vehicle controller code.

## Checklist
- [ ] Exact counterplay exists.
- [ ] Warning is readable.
- [ ] Compatibility is validated.
- [ ] Deterministic replay works.
- [ ] Runtime state resets cleanly.
- [ ] No unavoidable death.
- [ ] Telemetry and tests exist.
