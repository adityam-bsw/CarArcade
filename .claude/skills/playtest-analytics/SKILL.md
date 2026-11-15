---
name: playtest-analytics
description: Use when instrumenting telemetry, running playtests, analysing retention, funnels, fun, or difficulty.
---
# Playtest Analytics

## Procedure
1. Define a hypothesis before adding telemetry.
2. Instrument run_start, run_end, distance_milestone, event_warning, event_start, event_counter, death, near_miss, reward, unlock and ad_opt_in.
3. Include build version, mode, seed, vehicle, world, surface, stage and session ID.
4. Avoid personal data; use pseudonymous identifiers and consent controls.
5. Create dashboards for median distance, run duration, failure cause, event comprehension, unlock time and return sessions.
6. Run structured playtests with novice and experienced players.
7. Ask players to explain failures without prompting.
8. Convert findings into one measurable change per iteration.

## Rules
- Do not collect unnecessary personal data.
- Do not call D1/D7/D30 targets proven until measured.
- Do not optimize only session length; measure satisfaction and voluntary return.
- Make telemetry robust offline and upload later only with consent/policy compliance.

## Checklist
- [ ] Hypothesis written.
- [ ] Events versioned.
- [ ] Privacy reviewed.
- [ ] Novice tests included.
- [ ] Findings tied to decisions.
