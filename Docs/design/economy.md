# Economy: vertical slice (coins, gems, scrap)

Owner: economy-balancer. Status: PROPOSED, design only, nothing built or measured. Date: 2026-11-15.
Reconciled with `VERTICAL_SLICE_BRIEF.md` (source of truth; payout table in its F7, log in section 13) on 2026-11-15. Sections 2.2, 10.2 and 10.3 were re-simulated on the reconciled brief on 2026-11-15 (invented profiles, see section 13, item 8). Section 2 segment table and 2.1 totals still use the pre-sim estimates.
Read with: `Docs/design/VERTICAL_SLICE_BRIEF.md` (section 10 task list), `Docs/GAMEPLAY_RULES.md`,
`Assets/Scripts/Game/Gameplay/GameplayTuning.cs`, `Assets/Scripts/Game/Model/GameCatalog.cs`.

**Every number below is a HYPOTHESIS until a telemetry run measures it (marked H).** The simulation uses invented
player profiles (section 8); its output shows what the assumptions imply, not what players do.
Numbers marked "existing" are read from code and are facts about the current build, not about balance.

## 0. Summary of decisions to review
1. Coin income bands (section 2) are for **organic + near-miss + ring coins**. Event payouts (Eater escape 150, RO-B0 200) come on top;
   paid in full the first time only, then 50 percent (H). Otherwise a finished slice run (2750 m) breaks the 3000 m band.
2. Vehicle prices: ClassicRed 0 (existing), SportCyan **600** (code today: 200), MonsterTruck **2000** (code today: 350). Needs a
   change in `GameCatalog.VehicleCost` (route to implementer, data to SO). APPLIED 2026-11-15 (600 / 2000 in `GameCatalog.VehicleCost`).
3. Near-miss coin steps must drop from 15/20/25/30 to **8/10/12/15** or the 12 percent share cap in F4 is impossible (section 4).
4. Surface coin factors alone (ice x1.5) pay only a few coins per zone; add a flat zone-clear bonus (section 5).
5. Scrap exists as a defined currency but is **switched off in the slice** (brief section 8: no new currencies).
6. The 5-coin boost price is not a decision for a median player (section 6); test arm at 10 (OPEN, brief question 8).
7. Timeout payouts: Eater timeout 75 coins, boss timeout 100 (first time); repeat 38 and 50 (50 percent, derived by the reconciliation, PROPOSED). Timeouts give no gem and no catalogue card.

## 1. Currencies
| Currency | Earned by | Spent on | Slice status | Kids-safe rule |
|---|---|---|---|---|
| Coins | Pickups (10 each, combo up to x5), air time, flips, 250 m chests, near miss, rings, events, daily reward, missions | Vehicles, upgrades (50 x level), boost (5, existing) | Live (existing) | Never sold for money; no coin packs |
| Gems | Rare pickup (3 percent of spawns, existing), chest roll 25 percent, day-7 daily (3), RO-B0 first defeat (1) | Nothing in the slice (existing: no shop). Post-slice: cosmetics only | Earn only | No gem packs sold for money |
| Scrap (H) | Consolation: a run that ends before 500 m pays 10 scrap per 100 m; a crash never pays zero | Post-slice: paint and decal cosmetics, 20 to 80 scrap each | **OFF** (design only) | Cosmetic only; no gameplay effect |

Sinks that exist: vehicle cost, upgrades (`UpgradeCost(level) = level x 50`, max level 5, so 50+100+150+200 = 500 per stat,
2000 for four stats; existing), boost (5 per use when meter not full).

## 2. Coin income per run (median player)
Targets from the task: 500 m 80-120, 1500 m 180-300, 3000 m 350-500, 5000 m 600-900 (all H). The slice ends at 2750 m,
so 3000 and 5000 m only apply to later endless play.

Organic rate per 100 m (pickups, air, flips, chests, ring coins, all included) by slice segment (H):
| Segment (m) | Beat | Coins per 100 m | Cumulative at segment end | Target band at that distance |
|---|---|---|---|---|
| 0-500 | warm-up, ice, breather | 20 | 100 | 80-120 (500 m) OK |
| 500-1000 | mud, meteors | 14 | 170 | |
| 1000-1750 | Road-Eater chase (timeout at 1750; sums not recomputed, see section 13 item 8) | 12 | 254 | 180-300 at 1500 m: 230 OK |
| 1750-2000 | escape payoff, ring gauntlet | 14 | 296 | |
| 2000-2750 | RO-B0 | 9 | 364 | 350-500 at 3000 m: 364 OK (low edge) |
Rates fall with distance on purpose (stakes rise, coin arcs get thinner while the player is busy dodging; also stops
long runs from dominating). The 5000 m band (600-900) cannot be hit by this table (it stays at 364); endless mode after the
slice needs a post-2750 rate of about 11-13 per 100 m (H). Flag to the team lead; not in slice scope.

### 2.1 Split by source (median run that reaches the finish, repeat clear; H)
| Source | Coins | Share | Note |
|---|---|---|---|
| Pickups, air, flips, 250 m chests, ring coins | 364 organic | about 63 percent (repeat clear, see totals below) | existing + ring +1 per chain link (F5) |
| Near miss | up to 12 percent cap of run total | 8 to 9 percent in sim | capped, section 4 |
| Eater escape / timeout | 150 first, 75 repeat / timeout 75 first, 38 repeat | | brief F2, F7 |
| RO-B0 defeat (Overload) / timeout | 200 first, 100 repeat / timeout 100 first, 50 repeat | | brief F3, F7 |
| Ring Gauntlet | inside organic (14 per 100 m) | | no hazard, keep it a breather |
Totals for a full clear: **first clear about 364 + near miss + 350 = roughly 790 (H)**, repeat clear about 364 + near miss + 175 =
roughly 580 (H). Shares for a repeat clear: organic about 63, near miss about 8, events about 30 percent.
Why repeat pays half: kids-safe and anti-grind. Retrying the same boss for the same 200 would be a farm loop and would
undercut the 3000 m band. No timers or energy are involved; it is just a smaller number.

### 2.2 What the simulation says about run totals (SIMULATED ON INVENTED PLAYER PROFILES; n = 4000 runs per profile, repeat-run payouts; all H)
Re-run 2026-11-15 on the approved brief: meteor at 505, mud at 735, glass at 1350 inside the chase, Eater timeout at 1750 m, rings 1800 to 1990,
payouts 150/75 escape, 75/38 Eater timeout, 200/100 defeat, 100/50 boss timeout, flat zone clears 20/15/30 (p clear 0.84/0.88/0.78), near-miss steps
8/10/12/15 with perfect x2 (p 0.2), Eater near miss 20, 12 percent run cap. Profile inputs (death split, P(escape | reached 1750) 0.50/0.60/0.75,
P(defeat | survived boss) 0.50/0.65/0.80) are invented, not measured. Script: scratchpad `sim3.py` (not in the repo).
| Profile | Mean distance | Mean run time (incl. 25 s menu) | Coins per run p10 / median / p90 | Same, excluding Eater and boss payouts | Near-miss share | Event-payout share |
|---|---|---|---|---|---|---|
| new | 1137 m | 145 s | 59 / 194 / 528 | 59 / 194 / 438 | 7.6 percent | 7.4 percent |
| median | 1682 m | 202 s | 111 / 315 / 624 | 111 / 313 / 484 | 8.0 percent | 13.4 percent |
| good | 2225 m | 259 s | 219 / 570 / 666 | 219 / 430 / 506 | 7.8 percent | 20.1 percent |
Outcome mix per run (percent of all runs): median escape 26.9, Eater timeout 16.6, boss defeat 16.9, boss timeout 9.3, finish 26.2;
good escape 54.0, timeout 16.8, defeat 45.7, boss timeout 11.3, finish 57.0; new escape 9.2, timeout 9.5, defeat 5.0, boss timeout 4.7, finish 9.7.
Escape as a share of chases entered (alive at 1000 m): new 20, median 36, good 59 percent. The brief target is 35 to 65 percent for the median;
the invented median sits at the low edge (watch this in telemetry).
Full-clear (boss defeated) median coins: **first clear about 770 to 800, repeat clear about 620 to 630**, of which about 463 is organic + zone + near
miss (all three profiles). The 12 percent near-miss cap binds in 34 / 44 / 51 percent of runs (new / median / good): near-miss share is capped by design.
Reading against the task bands: median run 315 at mean distance 1682 m is a little above the 1500 m band (180-300); excluding event payouts it is
313 at 1682 m, still above. Cause: flat zone clears add about 65 coins that the old sim left out. A full clear (2750 m) excluding events is about 463
(inside 350-500 for 3000 m). If the 1500 m band is binding, lower `organicRate` segment 0-500 from 20 to 17 (H); not changed here.

## 3. Event rewards (all H)
| Event | Reward | Rule |
|---|---|---|
| EVT-ICE | 20 coins flat on clear without losing a heart | on top of surface x1.5 |
| EVT-MUD | 15 coins flat, same rule | boost or low angle is the decision |
| EVT-GLASS | 30 coins flat, same rule | hardest geometry, highest flat |
| EVT-METEOR | none flat; near-miss only (existing 15, see section 4) | |
| EVT-RINGS | rings 5 coins each (existing) + 1 per chain link; frenzy x2 for 7 s (existing) | no flat bonus, rings are the reward |
| Road-Eater escape | 150 first, 75 repeat; plus 0.5 frenzy | brief F2 |
| Road-Eater timeout retreat (1750 m) | 75 first, 38 repeat; plus 0.25 frenzy; no card | brief F2 (PROPOSED) |
| RO-B0 defeat (Overload) | 200 first, 100 repeat; 1 gem first time only; catalogue card `boss-robo-b0` | brief F3 |
| RO-B0 timeout (105 s) | 100 first, 50 repeat; no gem, no card | brief F3 (PROPOSED) |
| First Eater escape | catalogue card (data only) | brief section 2 |
| Finish gate | 0 extra | no pile of fixed bonuses; the boss payout is the finish |

## 4. Near-miss bonus and the 12 percent cap
Conflict found: F4 pays 15/20/25 (max 30) and expects 4 to 12 near misses per median run. Four near misses at the 15 base alone
is 60 coins; the cap of 12 percent of a 300-coin run is 36. The two rules cannot both hold.
Proposal (H):
| Chain step | Coins (was) | Coins (proposed) |
|---|---|---|
| 1st | 15 | 8 |
| 2nd within 4 s | 20 | 10 |
| 3rd | 25 | 12 |
| 4th and later | 30 | 15 |
| Perfect (half radius) | x2 | x2 (unchanged) |
| Eater near miss | 40 | 20 |
Plus a **run cap**: near-miss coins are capped at 12 percent of the run total (payment = `min(step, floor(0.12 / 0.88 x nonNearMissCoins) - nearMissPaid)`, floored at 0) (the "CLOSE!" toast, sound and
frenzy still fire). Surface coin factor applies after the cap check so ice never breaks it. Existing field `nearMissCoins = 15`
became 8 (applied 2026-11-15, Phase A). Frenzy per near miss stays 0.15 (existing).
Median at 6 near misses with mixed chains is about 60 coins, which the cap trims to about 12 percent. The F4 acceptance line (b) is
then satisfied by construction; line (a) (4 to 12 near misses) is unaffected because the frenzy and the toast still pay.

## 5. Surfaces and vehicle coin multipliers
Surface coin factors are existing (ice 1.50, mud 1.30, glass 1.40, asphalt 1.00).
Check against risk (brief F1 crash-rate target 8 to 25 percent per zone, H): a 140 m ice zone at about 14 coins per 100 m is 20 base
coins, so the x1.5 adds **about +10 coins**; a 150 m mud zone at x1.3 adds about +6. A single lost heart is worth far more than that, so
the factor alone does not pay for risk and players will not feel it. Hence the flat zone-clear bonus in section 3 (20/15/30).
Expected value check (H): ice 10 + 20 x (1 - 0.16 crash) = about +27 per zone vs. a crash costing one heart; mud about +19; glass
about +33 with a higher expected crash rate (more flat coins on purpose). A heart is not a currency, so this is a feel check, not an
equation; needs the `surface_exit {crashed}` telemetry to confirm.

Vehicles (F8): **no coin multiplier per vehicle** (H). Income per run stays equal across vehicles; they differ in speed,
mass, mud drag, ice slip, glass cracks and Eater push-back, so none is strictly better. Side effect to measure: SportCyan (x1.08
speed) covers distance faster so earns more coins per minute but the same per run; MonsterTruck (x0.95) earns slightly fewer per
minute. This is acceptable and keeps the "no pay-to-win" rule: price buys variety, not income.

## 6. Boost economy (existing numbers, H on conclusions)
Boost costs 5 coins unless the free meter is full (existing `boostCoinCost = 5`). A pickup is 10 coins, up to x5 on a combo (50),
and x2 during frenzy or the x2 power-up. A median run earns about 267 coins, so one boost is about 2 percent of a run and under
1 percent of a high combo. Conclusion: in mud and the Eater chase the 5-coin price is **not a real decision** (answer to the open
question in the brief). Test arm: `boostCoinCost = 10` (one base coin). Keep the free meter (air, flips, coins) as the main source;
telemetry to decide: boosts per run and share of boosts bought with coins (target 1 to 3 bought per run; H).
Not simulated: expected boosts per run (needs the draw-bot telemetry).

## 7. Ink budget (NOT simulated)
Existing: tank 100 (+30 per tank level), regen 1/s, pickup 45, rescue below 45 percent. Slice ink factors 0.75 to 1.30.
A real ink curve (min, median, p10) needs measured ink per metre drawn (`draw_stats` telemetry) and the Eater gap data; I
have none and will not invent them. The requirement the economy places on the director (carried to stage-director): ink
never below the rescue threshold (45 percent) for more than 15 s, and during the Eater chase and glass a rescue pickup is placed
in reach whenever ink is 0 (existing rule). Open item: simulate once `draw_stats` exists.

## 8. Upgrades
Existing: 4 stats, level 1 to 5, cost = currentLevel x 50 (50, 100, 150, 200 per stat). Cap rule (H): each level adds
**+6 percent** to its stat, so level 5 is +24 percent and power stays forgiving (task cap +5 to +10 per level). Boost upgrade
already adds 0.25 s per level (existing `boostDurationPerLevel`); at base 1.4 s that is +18 percent per level, **above the cap**:
change to 0.08 s per level (+6 percent) (change request). Ink tank is +30 per level on 100, +30 percent per level, also above the cap:
change to +6 per level (change request, affects ink balance, re-sim).
Open: whether upgrades are per vehicle or per profile (not verified in code). The simulation assumes per profile.

Status: upgrade scaling of +6 percent per level is adopted into the brief (F7) as PROPOSED; it needs user approval (brief question 7).
Applied 2026-11-15 (Phase A, user-approved): `boostDurationPerLevel` 0.25 to 0.08 (GameplayTuning code default and asset). NOT applied: engine (0.8 m/s per level in `RoadGameManager`, about +19 percent), suspension (+3 torque per level) and ink tank (+30 per level) are hard-coded formulas, so they are a code change, not a data edit; they need an approved task.

## 9. Vehicle prices
| Vehicle | Price now (existing) | Price proposed (H) | Role |
|---|---|---|---|
| ClassicRed | 0 | 0 | Balanced, tutorial car |
| SportCyan | 200 | **600** | Fast and light, hard in mud |
| MonsterTruck | 350 | **2000** | Heavy, mud and Eater push-back |
No vehicle is strictly better (F8 matrix: each has one best event). Prices pay only for choice, never for income (section 5).

## 10. First-hour schedule and runs-to-unlock simulation
### 10.1 Model (own script, all inputs H)
- Run outcome by profile: probability of dying before 1000 m / in the chase / 1750-2000 / in the boss / finishing.
  new .55/.25/.03/.07/.10, median .25/.30/.05/.12/.28, good .08/.20/.04/.10/.58. Death distance uniform within its range.
  The median profile gives Eater escape in 45 percent of runs, inside the brief target of 35 to 65 percent.
- Organic coins from the table in section 2 times a uniform 0.85 to 1.15 noise; near-miss coins capped at 12 percent;
  Eater 150/75 and boss 200/100 as in section 2; run time = distance / 9.5 m/s + 25 s menu and retry overhead.
- Purchase policy: upgrade 1 (50), upgrade 2 (100), then SportCyan, then upgrade 3 (150), then MonsterTruck (needs 3 upgrades),
  then upgrade 4 (200). 3000 simulated players per profile, 60 minutes each. Script is in my scratchpad only (not in the repo);
  the tables below are its output with seeded randomness.
- Missing from the model: daily reward, missions, power-ups, combos beyond the organic rate, upgrade levels changing difficulty.
  These all add coins, so the unlock times below are conservative (late).

### 10.2 Result: runs-to-unlock with approved prices 600/2000 (SIMULATED ON INVENTED PLAYER PROFILES; minutes into the session; 3000 players per profile, 60 min each; all H)
Same model and purchase policy as 10.1 but with the reconciled events and payouts of section 2.2 and first-time flags kept per outcome row (escape,
Eater timeout, defeat, boss timeout each pay "first" once). The old code prices (200/350) are not re-simulated; the earlier result (everything
unlocked in about 10 minutes) stays qualitatively true because income is similar.
| Milestone | Profile | Run number p10 / median / p90 | Minutes p10 / median / p90 |
|---|---|---|---|
| Upgrade 1 (50) | median | 1 / 1 / 1 | 1.4 / 3.3 / 5.2 |
| Upgrade 2 (100) | median | 1 / 1 / 2 | 2.2 / 3.9 / 5.5 |
| SportCyan (600) | new | 2 / 4 / 5 | 6.8 / 8.7 / 11.1 |
| SportCyan (600) | median | 1 / 2 / 4 | 5.2 / 8.3 / 10.9 |
| SportCyan (600) | good | 1 / 2 / 3 | 5.2 / 7.5 / 10.7 |
| Upgrade 3 (150) | median | 2 / 3 / 4 | 7.8 / 9.9 / 12.3 |
| Upgrade 4 (200) | median | 2 / 3 / 5 | 9.4 / 11.4 / 14.1 |
| MonsterTruck (2000) | new | 10 / 12 / 16 | 28.5 / 31.1 / 34.1 |
| MonsterTruck (2000) | median | 7 / 8 / 11 | 26.8 / 29.2 / 32.0 |
| MonsterTruck (2000) | good | 5 / 6 / 8 | 25.6 / 27.7 / 30.4 |
Runs in the first hour (p10 / median / p90): new 22 / 25 / 29, median 16 / 18 / 21, good 13 / 14 / 16. All three profiles reach MonsterTruck within the
hour (100 percent of simulated players), at about minute 28 to 31. Purchase order is a model assumption (upgrades 1 and 2, SportCyan, upgrade 3,
MonsterTruck, upgrade 4); a real player will order differently.
Brief goal "a meaningful unlock every 3 to 5 early sessions": SportCyan at session 2 (median; p90 4), MonsterTruck at session 8 (median; p10 to p90 7 to 11).
The 4-to-8 session gap is carried by upgrades 3 and 4 (sessions 3) and the non-vehicle rewards in 10.3. Daily reward, missions and power-ups are not
modelled, so these times are late by an unknown amount.

### 10.3 First-hour reward schedule (median player; coin items from the simulation, others are design intent; all H, invented profile)
Rule from the task: a new reward every 3 to 7 minutes in the first hour.
| Minute (about) | Reward | Source | Simulated |
|---|---|---|---|
| 0-3 | Run 1: first 50-coin upgrade, daily day-1 coins (50) | purchase, daily | upgrade yes (median minute 3.3) |
| 3-5 | Mastery level 2 (100 xp; run 1 gives about 166 xp), second upgrade (100) | mastery, purchase | upgrade yes (3.9) |
| 5-9 | First Eater escape (150 coins + catalogue card) or first timeout (75); first mission claim (30-60 coins) | event, missions | coins yes; card, missions no |
| 8-11 | SportCyan unlocked (600) | purchase | yes (median 8.3, p10 to p90 5.2 to 10.9) |
| 10-14 | Upgrade 3 (150) and upgrade 4 (200) | purchase | yes (9.9 and 11.4) |
| 13-17 | Mastery level 3-4 (cumulative 300, 600 xp); second mission claim | mastery, missions | partial |
| 14-22 | RO-B0 first defeat: 200 coins + 1 gem + card (only if reached; defeat in 17 percent of median runs, so a first defeat by about run 6) | boss | probability only |
| 22-29 | Save for MonsterTruck: Garage progress bar ("1,640 / 2,000") | meter | yes |
| 29 | MonsterTruck unlocked (2000; p10 to p90 minute 27 to 32) | purchase | yes |
| 29-60 | Day-2+ daily (75), 250 m chests with gem roll, remaining mastery levels, new road styles (surfaces from Roads tab, cost TBD) | mixed | no |
Gap warning: from about minute 14 to 22 the only guaranteed rewards are a mission claim and a mastery level (toast only; the brief cuts cosmetic mastery
rewards), and from 14 to 29 nothing is bought except MonsterTruck savings. If playtest shows a flat spot, the cheapest fixes are (a) raise mission payouts,
(b) a catalogue road-style card at 300 coins priced inside the gap, (c) scrap (section 1) for a cosmetic. All three are data changes.

## 11. Kids-safe monetisation rules
Hard rules (CLAUDE.md, Play Store readiness; non-negotiable):
1. **No loot boxes, gacha, random paid rewards.** The 25 percent gem roll on chests is free play, no purchase involved.
2. **No energy, lives timers, wait-or-pay timers, fake scarcity or countdown offers.** Daily reward is a calendar day, not a gate
   (missed day restarts streak but nothing is locked; H: consider a softer restart, 7-day streak falls back one day not to zero).
3. **No pay-to-win.** Nothing bought with money changes speed, ink, power, income or run length. Money buys cosmetics only.
4. **Rewarded ads, opt-in only** (not in the slice; slice has no ads):
   - 2x coins on the result screen, **max 2 per day** (H), the player taps "Double (watch ad)"; the base coins are already saved.
   - One revive per run, offered only on the death result screen or the revive prompt after a crash, never mid-run and never
     interrupting an active hazard; the player always sees a one-tap "No thanks".
   - Never forced, never an interstitial, never a banner, never after a failed purchase. Never offered while a telegraph is active.
   - Ad SDK must be configured child-directed / designed for families; no personalised ads; confirm in `PLAY_STORE_READINESS.md`.
5. **Cosmetics only for money**: paint, decals, road skins (no income-bearing surfaces are sold: coin factors are earned effects
   of road choice and surfaces unlock with coins). Prices shown in plain currency, no bundles with hidden contents, no "limited time".
6. **No real-money-to-currency conversion pressure on children**: no coin or gem packs in the slice; any later purchase sits behind
   a parental gate (adult maths or long-press) per Play Families policy; confirm with the policy text before build.
7. No new `PlayerPrefs`; counters (ads watched today, streak) live in `SaveService`.
8. Telemetry stays local until a data policy exists (brief section 6).
Ad value arithmetic (H): 2x on a median repeat run (267) adds 267 coins, about one extra run of income. Capped at 2 a day it cannot
skip the ladder: MonsterTruck (2000) still needs about 8 median-equivalent runs of income.

## 12. Tunables table (SO fields; default / min / max / telemetry that justifies a change; all H)
| Field | Default | Min | Max | Change if telemetry shows |
|---|---|---|---|---|
| `coinValue` (existing 10) | 10 | 5 | 20 | coins per run p50 outside band |
| `organicRate[segment]` (new, per 100 m) | 20/14/12/14/9 | 6 | 30 | `run_end` coins by distance vs bands |
| `boostCoinCost` (existing 5) | 5 (test 10) | 0 | 20 | share of boosts bought with coins outside 1-3 per run |
| `nearMissCoinsStep[]` | 8/10/12/15 | 4 | 20 | near-miss share over 12 percent |
| `nearMissRunCapFraction` | 0.12 | 0.05 | 0.15 | farming (time near hazard over 3 s) |
| `nearMissEaterPay` | 20 | 10 | 40 | `eater_gap_sample` shows recoveries rare |
| `eaterEscapeCoins` first / repeat | 150 / 75 | 50 | 250 | escape rate outside 35-65 percent |
| `bossDefeatCoins` first / repeat | 200 / 100 | 80 | 300 | boss reached under 25 percent of runs |
| `eaterTimeoutCoins` first / repeat | 75 / 38 | 25 | 150 | timeout rate over 40 percent of chases (then escape is too hard) |
| `bossTimeoutCoins` first / repeat | 100 / 50 | 40 | 150 | timeout rate over 25 percent of boss fights |
| `zoneClearCoins` ice / mud / glass | 20 / 15 / 30 | 5 | 50 | crash rate per zone vs 8-25 percent |
| `ringCoins` (existing 5), `ringChainCoinBonus` | 5, +1 | 3 / 0 | 10 / 3 | median chain below 3 |
| `milestoneEvery` / `milestoneCoins` (existing) | 250 / 50 per chest index | 150 / 25 | 500 / 100 | chests under 10 percent of coins |
| `recordBonus` (existing) | 100 | 25 | 150 | run 2-5 sessions chasing record only |
| `dailyCoins[]` (existing) | 50,75,100,125,150,200,300 | | | day-2 retention vs coin share |
| `vehiclePrice[ClassicRed/SportCyan/MonsterTruck]` | 0 / 600 / 2000 | 0/300/1000 | 0/1000/3500 | Cyan session outside 2-5, Monster outside 7-14 |
| `upgradeCostPerLevel` (existing 50) | 50 | 30 | 100 | upgrade bought every run (too cheap) or never |
| `upgradeBonusPerLevel` | +6 percent | 5 | 10 | win-rate spread by upgrade level |
| `adDoubleCapPerDay` | 2 | 0 | 2 | fixed, never above 2 |
| `scrapPer100mBeforeFinish` (off) | 10 | 0 | 25 | only if a flat spot appears |

## 13. Contradictions and open items for the team lead
1. Task-list filename is `ECONOMY_SLICE.md`; this file is `economy.md` as instructed in this task. Rename or alias if needed.
2. Escape (150) + boss (200) = 350, larger than the entire organic income of a slice run. First-clear vs repeat split (section 2.1)
   resolves it; the game-designer must accept the half-pay repeat rule.
3. F4 near-miss amounts conflict with its own 12 percent cap (section 4). Needs agreement.
4. Brief scope cut says "no new currencies"; the task asks for three. Scrap is documented but disabled.
5. Existing code prices (200/350) contradict the "unlock every 3-5 sessions" pacing; prices and boost/ink upgrade scaling need
   implementer changes (`GameCatalog`, `GameplayTuning`). Not done here (no code touched).
6. Not verified: whether upgrades are per vehicle; mission reward values; ink curves; boosts per run; real run pace
   (9.5 m/s assumption from the brief).
7. Next measurements needed before trusting any number: `run_end` coins by distance, `near_miss`, `surface_exit`, `T-RUN-PACE`,
   `draw_stats`, mission claim timing.
8. Re-run done 2026-11-15 (sections 2.2, 10.2, 10.3) with the reconciled brief (chase to 1750 with timeout, new event positions, all payouts, near-miss steps and cap,
   prices 600/2000). Profiles are invented. Still missing from the model: Eater gap dynamics (escape vs timeout split is a single invented probability), ink, boost,
   daily, missions, power-ups. Findings: median 1500 m coin figure runs slightly above the band (315 vs 180-300) because flat zone clears were added; median escape
   share of chases is 36 percent (low edge of 35 to 65).
9. Ambiguity: "half pay on repeat clears" is applied here to Eater and boss payouts only. Flat zone-clear coins (20 / 15 / 30) and ring coins
   are paid in full every run. If the user wants those halved too, section 2 income bands drop and need re-balancing.

## 15. Change request for the implementer (approved numbers only; 2026-11-15)
Source of approval: `VERTICAL_SLICE_BRIEF.md` section 14 (prices, near-miss steps and 12 percent cap, half pay on repeat Eater and boss clears, timeout payouts
75 and 100, upgrade scaling +6 percent per level). All values remain hypotheses until telemetry. Economy owner touched no code; paths are as read from the repo.
| # | File / field | Old value | New value | Notes |
|---|---|---|---|---|
| 1 | `Assets/Scripts/Game/Model/GameCatalog.cs`, `VehicleCost(SportCyan)` | 200 | 600 | Better: read from `vehiclePrice` SO field (default 0 / 600 / 2000) |
| 2 | same, `VehicleCost(MonsterTruck)` | 350 | 2000 | |
| 3 | `Assets/Scripts/Game/Gameplay/GameplayTuning.cs`, `nearMissCoins` (asset `Assets/Resources/Gameplay/GameplayTuning.asset`) | 15 flat | chain step array `nearMissCoinsStep[] = {8, 10, 12, 15}` (1st, 2nd, 3rd, 4th and later within 4 s) | Field `nearMissCoins` is replaced; `RunDirector.cs` line 231 `Pay("NEAR MISS", _t.nearMissCoins)` reads the step instead |
| 4 | new field `nearMissPerfectMul` | none | 2 (perfect = within half radius) | Brief F4 |
| 5 | new field `nearMissEaterPay` | none | 20 | In brief F4 table; not listed separately in the approval log, confirm with lead |
| 6 | new field `nearMissRunCapFraction` | none | 0.12 | Payment = `min(step, floor(0.12 / 0.88 x nonNearMissCoins) - nearMissPaid)`, floored at 0; toast, sound and frenzy (0.15) still fire; surface factor applied after the cap check |
| 7 | new fields `eaterEscapeCoins` first / repeat | none | 150 / 75 | "First" flag stored in `SaveService` (no PlayerPrefs). Frenzy 0.5 both |
| 8 | new fields `bossDefeatCoins` first / repeat | none | 200 / 100 | Gem (1) and card `boss-robo-b0` first time only |
| 9 | new fields `eaterTimeoutCoins` first / repeat | none | 75 / 38 | 75 approved; 38 derived by the 50 percent rule (brief U3). Frenzy 0.25, no card |
| 10 | new fields `bossTimeoutCoins` first / repeat | none | 100 / 50 | 100 approved; 50 derived (U3). No gem, no card |
| 11 | `GameplayTuning.cs`, `boostDurationPerLevel` | 0.25 | 0.08 | +6 percent of the 1.4 s `boostDuration` per level |
| 12 | `Assets/Scripts/Game/RoadGameManager.cs` line 388 (`maxInk = 100f + (inkTankLevel - 1) * 30f`) | 30 per level | 6 per level, move the constant to a `GameplayTuning` field `inkTankPerLevel` | +6 percent per level; re-check ink rescue (ink balance is not simulated) |
| 13 | `GameCatalog.UpgradeCostPerLevel` | 50 | 50 (no change) | Listed to confirm: not part of the approval |
Checked and left alone: engine bonus 0.8 m/s per level (RoadGameManager line 384) is 7.0 percent of `baseMaxSpeed` 11.5, and suspension torque +3 per level is
8.8 percent of the 34 default torque, both inside the 5 to 10 percent cap. Engine is 19 percent of `baseStartSpeed` 4.2; if the cap is meant against the start
speed, set the engine bonus to 0.25 (lead to decide, no data).
Excluded because not approved or still open: flat zone-clear coins 20 / 15 / 30, boost price (keep 5, test arm 10 is OPEN), ring chain bonus, scrap, ad numbers.
