# Blueprint excerpts: section C3/C4 (surfaces) and section D (progression and economy)
Source: the user's Game Design and Production Blueprint (pasted in chat 2026-11-15; this file is the repo copy). All numbers are starting hypotheses.

## C3 Surface compatibility
- Oil + fire: hazardous but allowed, visible ignition warning. Ice + wind: allowed, reduce wind force.
- Lava crust + water: obsidian conversion (one behaviour, not two). Glass + heavy vehicle: allowed only with sufficient warning.
- Cloud + gravity inversion: disallowed. Magnetic rail + electric rail: merge into one electrical-magnetic behaviour.
- Rope bridge + heavy vehicle: stronger bridge variant. Acid + lava: disallowed unless a designed chemical hazard.
- Never let the player meet an untested combination (SurfaceCompatibilityDatabase).
## C4 Surface learning
1st encounter: 1.5 s preview + demo + one announcer line. 2nd: small icon + hint. 3rd: none, optional codex entry. Track surfaceSeenCount locally.

## D Progression and economy
Three reasons to play: unlock new things, improve owned things, master owned things. No single-coin grind dependence.
### D1 Currencies
| Currency | Source | Use | Monetisation |
|---|---|---|---|
| Coins | runs, missions, chests, mastery | vehicles, upgrades, surfaces | never directly required |
| Gems | milestones, achievements, daily events | premium cosmetics, extra collection items | optional purchase |
| Scrap | duplicate parts, boss rewards, dismantling | vehicle-specific upgrades | earned gameplay only |
Rules: no energy, no forced timers, no pay-to-revive requirement, no exclusive gameplay power for money, no paid random loot boxes, no expiring currency, no deceptive buttons, no artificial scarcity. Gems mostly for optional cosmetics/convenience.
### D2 Vehicle tiers (starting values; slice prices approved separately: 600/2000)
Tier1 starter free 0m; T2 light bike 1,500 coins/500m; T3 jeep 4,000/1,500m; T4 rally 8,000/3,000m; T5 monster truck 15,000/5,000m; T6 tank 28,000/8,000m; T7 experimental rover 50,000/12,000m. Distance gates are cumulative lifetime distance. Next vehicle must NOT be strictly better; each has strength+weakness (bike low ink cost/easy flip; jeep balanced; rally speed+grip/weak impact; monster crushes fragile roads/high ink; tank heavy durable/slow; moon rover low gravity/poor accel; hovercraft floats/weak climbs).
### D3 Upgrades
Five branches per vehicle: Engine, Suspension, Ink tank, Boost, Shield; 5 levels each (25 nodes), shared definitions with per-vehicle base overrides. Costs per level 500, 900, 1500, 2500, 4000 (9,400 per branch). Benefit per level +5, +5, +7, +8, +10 percent; total power stays controlled (more forgiving, not trivial). Approved: upgrade scaling about +6% per level in the slice economy (economy.md section 15).
### D3b Vehicle mastery (per vehicle)
Levels: 1 at 500 m (sticker), 2 at 2,000 m (500 coins), 3 at 5,000 m (unique colour), 4 at 10,000 m (1 gem), 5 at 20,000 m (exclusive cosmetic). Mastery rewards USING the vehicle with vehicle-specific goals (bike 10 clean landings; tank break 20 fragile segments; rally 15 near misses; rover survive 5 low-gravity stages).
### D4 World and surface unlock gates (distance = cumulative lifetime)
| World | Unlock m | Coins | Extra gate |
|---|---|---|---|
| Countryside | 0 | free | none |
| Desert | 1,000 | 2,000 | complete first monster encounter |
| Snow | 2,500 | 4,000 | 3 missions |
| Moon | 4,000 | 6,000 | 5 clean jumps |
| Mars | 5,500 | 8,000 | boss encounter |
| Volcano | 7,000 | 12,000 | survive lava stage |
| Underwater | 8,500 | 15,000 | water surface mastery |
| Candy | 10,000 | 18,000 | complete 10 stage events |
| Haunted | 12,000 | 22,000 | 5 survival runs |
| Synthwave | 15,000 | 30,000 | complete rhythm challenge |
Surface unlocks: Basic 1,000-2,500 (early distance); Unstable 3,000-5,000 (surface tutorial); Advanced 6,000-10,000 (5 missions); Special 12,000-18,000 (boss reward); Exotic 20,000-30,000 (world mastery).
### D5 Run earnings targets
500 m: 80-120 coins; 1,500 m: 180-300; 3,000 m: 350-500; 5,000 m: 600-900; 8,000 m: 1,000-1,400. New meaningful item every 3-5 sessions early; a new mechanic/reward every 3-7 minutes in the first hour (first drive, 500 m = 500 coins, first upgrade, first monster, second vehicle, first surface, first boss, first world, first mastery reward, daily challenge).
### D6 Daily / weekly
Daily: same seed for everyone, fixed vehicle/class, fixed world, three stage events, one leaderboard (local-only for now), completion reward 100-250 coins + 1 mastery point + weekly token. Weekly: three themed challenges + boss challenge + special surface challenge. No conventional season pass first; later a 30-day Road Trip Album (20 free, 10 optional cosmetic, no gameplay advantage, no pressure notifications, always claimable).
### D7 Monetisation (kids-safe)
Yes: rewarded ad for 2x coins (max 2 offers/day), one optional post-run revive (max 1 per run), extra daily reward (max 1/day), cosmetic bundles, optional gems. No: interstitials after every run, forced ads, ads interrupting a run, paid random loot boxes, paid-only stat-superior vehicles, battle-pass pressure, real-money countdowns. Verify Google Play Families policy and approved SDK before enabling any ads: build only SDK-free hooks (an IAdService interface with a no-op/editor-fake implementation) until the user picks an SDK.
