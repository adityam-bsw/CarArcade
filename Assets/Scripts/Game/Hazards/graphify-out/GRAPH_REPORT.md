# Graph Report - Hazards  (2026-11-17)

## Corpus Check
- 25 files · ~24,930 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 556 nodes · 1175 edges · 23 communities (20 shown, 3 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 74 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `0b9ee789`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- BossController
- BossModel
- EaterVariantSO
- RoadEaterModel
- WorldProjectileRunner
- EaterSignatureModule
- RoadBuilderGame
- BossTierSO
- ProjectileSurfaceEffect
- RoadEaterController
- ProjectilePattern
- EaterEvent
- BossEvent
- EaterCounterplay
- HazardArtSet.Load
- NearMissLedger
- BossOutcome
- Pose
- ThreePhaseCounter
- FinalGateCounter
- FinishGateProp

## God Nodes (most connected - your core abstractions)
1. `BossController` - 68 edges
2. `WorldProjectileRunner` - 42 edges
3. `RoadEaterController` - 40 edges
4. `BossModel` - 36 edges
5. `EaterSignatureModule` - 36 edges
6. `ProjectileDefinitionSO` - 36 edges
7. `RoB0Rig` - 33 edges
8. `RoadEaterModel` - 33 edges
9. `RoadBuilderGame` - 25 edges
10. `EaterCounterplay` - 23 edges

## Surprising Connections (you probably didn't know these)
- `BossController` --references--> `BossDefinitionSO`  [EXTRACTED]
  BossController.cs → BossDefinitionSO.cs
- `BossController` --references--> `BossModel`  [EXTRACTED]
  BossController.cs → BossModel.cs
- `BossController` --references--> `BossTierSO`  [EXTRACTED]
  BossController.cs → BossTierSO.cs
- `BossController` --references--> `HazardArtSet`  [EXTRACTED]
  BossController.cs → HazardArtSet.cs
- `BossController` --references--> `WorldProjectileRunner`  [EXTRACTED]
  BossController.cs → WorldProjectileRunner.cs

## Import Cycles
- None detected.

## Communities (23 total, 3 thin omitted)

### Community 0 - "BossController"
Cohesion: 0.06
Nodes (17): BossController, Model, Muzzle, Tier, TrapActive, World, Proj, Ring (+9 more)

### Community 1 - "BossModel"
Cohesion: 0.05
Nodes (38): AttackBeat, AttackPatternSO, BossDefinitionSO, BeatRequest, BossContext, BossModel, CurrentPattern, Data (+30 more)

### Community 2 - "EaterVariantSO"
Cohesion: 0.06
Nodes (28): Announcer, Dropped, LastSubtitle, Playing, Spoken, EaterCounterplays, EaterSignatures, EaterDefeatOption (+20 more)

### Community 3 - "RoadEaterModel"
Cohesion: 0.06
Nodes (31): MonsterDefinitionSO, EaterBand, Calm, Danger, Edge, Snap, EaterState, Done (+23 more)

### Community 4 - "WorldProjectileRunner"
Cohesion: 0.12
Nodes (11): ProjectileDefinitionSO, HitCentre, IsWorld, NearMissSource, PerfectEdge, WorldProjectileRunner, AliveCount, Definition (+3 more)

### Community 5 - "EaterSignatureModule"
Cohesion: 0.08
Nodes (9): EaterSignatureModule, Ph, Active, Rest, Tell, PullSignature, ScanSignature, SinkholeSignature (+1 more)

### Community 6 - "RoadBuilderGame"
Cohesion: 0.11
Nodes (4): RoadBuilderGame, HitStop, Active, HazardWorlds

### Community 7 - "BossTierSO"
Cohesion: 0.13
Nodes (9): BossTierId, Commander, Hunter, Interceptor, Overlord, Scout, BossTierSO, HazardPay (+1 more)

### Community 8 - "ProjectileSurfaceEffect"
Cohesion: 0.10
Nodes (21): IProjectileSurfaceHook, NoOpProjectileSurfaceHook, ProjectileSurfaceHooks, Current, ProjectileSurfaceEffect, BreakWeak, BurnWood, ChargeGrid (+13 more)

### Community 9 - "RoadEaterController"
Cohesion: 0.12
Nodes (7): RoadEaterController, Counter, DefeatedVariant, Host, Model, PursuitPhase, Variant

### Community 10 - "ProjectilePattern"
Cohesion: 0.12
Nodes (14): ProjectilePattern, Arc, Bouncing, Homing, Splitting, Straight, Zigzag, TelegraphShape (+6 more)

### Community 11 - "EaterEvent"
Cohesion: 0.12
Nodes (16): EaterEvent, BandChanged, Bite, ChaseStart, ChewStart, Escaped, Gone, NearMiss (+8 more)

### Community 12 - "BossEvent"
Cohesion: 0.18
Nodes (11): BossEvent, Cleared, Defeated, Gone, IntroStart, Overpowered, PatternStart, PhaseChanged (+3 more)

### Community 13 - "EaterCounterplay"
Cohesion: 0.22
Nodes (7): EaterCounterplay, Hint, Progress01, PursuitPhase, LureRingCounter, Hint, EaterHost

### Community 14 - "HazardArtSet.Load"
Cohesion: 0.25
Nodes (3): WeakSpanCounter, Hint, HazardArtSet

### Community 15 - "NearMissLedger"
Cohesion: 0.24
Nodes (4): NearMissLedger, Count, Paid, NearMissPay

### Community 17 - "BossOutcome"
Cohesion: 0.29
Nodes (7): BossOutcome, Dodged, Hit, HitShielded, Near, None, Perfect

### Community 18 - "Pose"
Cohesion: 0.29
Nodes (7): Pose, Angry, Blink, Damaged, Firing, Hurt, Idle

### Community 19 - "ThreePhaseCounter"
Cohesion: 0.33
Nodes (4): ThreePhaseCounter, Hint, Progress01, PursuitPhase

### Community 20 - "FinalGateCounter"
Cohesion: 0.40
Nodes (3): FinalGateCounter, Hint, Progress01

## Knowledge Gaps
- **184 isolated node(s):** `LastSubtitle`, `Spoken`, `Dropped`, `Playing`, `Model` (+179 more)
  These have ≤1 connection - possible missing edges. (Counts symbols only; 216 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **3 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `BossController` connect `BossController` to `BossModel`, `EaterVariantSO`, `WorldProjectileRunner`, `RoadBuilderGame`, `BossTierSO`, `RoadEaterController`, `EaterCounterplay`, `HazardArtSet.Load`?**
  _High betweenness centrality (0.202) - this node is a cross-community bridge._
- **Are the 3 inferred relationships involving `BossController` (e.g. with `BossModel.Resolve()` and `RoB0Fx.Trail()`) actually correct?**
  _`BossController` has 3 INFERRED edges - model-reasoned connections that need verification._
- **What connects `LastSubtitle`, `Spoken`, `Dropped` to the rest of the system?**
  _184 weakly-connected nodes found - possible missing edges._
- **Should `BossController` be split into smaller, more focused modules?**
  _Cohesion score 0.056334735857877516 - nodes in this community are weakly interconnected._
- **Why does `RoadEaterController` connect `RoadEaterController` to `BossController`, `EaterVariantSO`, `RoadEaterModel`, `EaterSignatureModule`, `RoadBuilderGame`, `EaterCounterplay`, `HazardArtSet.Load`, `RoadEaterController.Tick`, `RoadEaterController.OnLifeLost`?**
  _High betweenness centrality (0.162) - this node is a cross-community bridge._
- **Should `BossModel` be split into smaller, more focused modules?**
  _Cohesion score 0.05137844611528822 - nodes in this community are weakly interconnected._
- **Why does `RoadBuilderGame` connect `RoadBuilderGame` to `BossTierSO`?**
  _High betweenness centrality (0.121) - this node is a cross-community bridge._