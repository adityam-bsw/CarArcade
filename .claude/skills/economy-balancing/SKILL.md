---
name: economy-balancing
description: Use when tuning coins, gems, scrap, vehicle prices, upgrades, unlock gates, rewards, or monetisation.
---
# Economy Balancing

## Procedure
1. Export current reward sources, sinks, item prices, unlock rates, and player progression.
2. Build a spreadsheet or deterministic simulation for 1, 5, 10, and 30 sessions.
3. Separate coins (gameplay progression), gems (optional cosmetics/convenience), and scrap (upgrade crafting).
4. Calculate runs-to-unlock using measured median earnings, not best-case earnings.
5. Protect early progression: meaningful unlock every 3–5 sessions.
6. Add vehicle asymmetry; no vehicle may strictly dominate all others.
7. Validate upgrade power caps and avoid exponential compounding.
8. Keep monetisation optional and separate from gameplay balance.
9. Run cohort simulations and update prices from telemetry.

## Rules
- No energy timers, paid revives, paid power exclusives, or paid random loot.
- Do not tune economy from one developer's best run.
- Record price changes with a version and reason.
- Show ad reward amount and action before opt-in.
- Avoid pressure countdowns and false scarcity.

## Checklist
- [ ] Median runs-to-unlock calculated.
- [ ] Early-hour rewards paced.
- [ ] Currency sinks are understandable.
- [ ] No pay-to-win advantage.
- [ ] Rewarded ad is optional.
- [ ] Economy simulation is reproducible.
