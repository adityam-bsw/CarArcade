# Reference Baseline: mentor game and architecture inspirations

Purpose: a shared starting point for design and architecture so our own idea is built on a
proven base, not guessed. Written from research on 2026-11-13.

**Evidence labels:** [CONFIRMED] = stated by a source linked below. [INFERRED] = deduced from how
the game plays (needs verification by playing). [MEMORY] = from general knowledge, not
re-verified this session. Do not treat INFERRED/MEMORY as fact in design decisions.

## 1. Mentor game: Hill Climb Racing (Fingersoft)

### What is publicly known [CONFIRMED]
- Built largely alone by Toni Fingerroos, about two months, simple childlike art, crowdsourced
  sound. Android Sept 2012, iOS Nov 2012. (Wikipedia)
- Controls are two pedals, Gas and Brake. In the air the same pedals rotate the vehicle.
  Physics are deliberately unrealistic; stunts (long jumps, flips) earn extra coins.
- A run ends when fuel runs out or the driver's neck breaks (head hits ground).
- Coins appear in groups; fuel and gems are placed nearby. Fuel is refilled by pickups.
- Coins buy upgrades and unlock stages and vehicles. Upgrades: engine, suspension, tires, 4WD.
- Stages: countryside, desert, arctic, moon (later more). Moon is low gravity. A guide lists the
  Moon stage unlock at 175,000 coins and says any stage can be bought if affordable.
- Later added the Garage (Dec 2016). Sequel HCR2 (2016). Freemium: ads + IAP.
- A later port (Hill Climb Racing Lite) used Defold with a community Box2D extension. The team
  said strong Box2D integration was essential. This confirms Box2D for that port only.
- Fingersoft says HCR2 moved "from standalone content toward scalable systems" for long-term
  engagement (PocketGamer.biz, talk preview).

### Not public [do not pretend to know]
Fingersoft has not published the original engine, terrain algorithm or tuning numbers. The terrain
and physics below are **our reconstruction**, informed by public Box2D car tutorials.

### Design DNA we can read from play [INFERRED, verify in the teardown below]
1. **One-thumb, two-input game.** Depth comes from physics, not controls.
2. **Resource = run timer.** Fuel makes every run finite and forces forward progress and
   pickup routing. Our equivalent is **ink**.
3. **Stages are data.** Variety = gravity, friction, terrain shape, art, pickups. Same engine.
   This is exactly the Mode-pack idea, which we generalise by adding a rules class.
4. **Short loop, long meta.** 1-3 minute runs, coins, upgrades, unlock next stage/vehicle.
5. **Readable failure.** Neck break and out-of-fuel are always the player's fault.
6. **Forgiving physics.** Assists keep the car moving; the fun is flow, not realism.
7. **Cheap, charming art.** Simple shapes, consistent style, fast to load, runs on weak phones.

## 2. Mapping: HCR to Road Builder Racer

| Hill Climb Racing | Our game | Note |
|---|---|---|
| Gas/Brake pedals | Draw stroke + Boost/Hop | We replace the pedals with the drawing verb, our unique selling point |
| Fuel | Ink | Same role; Void Run turns trailing road into ink |
| Stage (gravity, terrain, art) | **Mode pack** (physics, economy, world, rules) | Same idea, more powerful |
| Vehicles + 4 upgrade parts | Vehicles + Engine/Suspension/Ink tank | Already in prototype |
| Fixed hand-tuned terrain | Player-drawn road | Our terrain is the player's drawing |
| Coin arcs | Coin arcs (already) | Keep; they teach where to draw |
| Garage | Garage screen | Phase 1 |
| Stage unlock by coins | Mode unlock by coins/goals | Keep |

**The one thing HCR does not have and we do:** the player authors the terrain. Every mode idea
should ask "what does this change about how the player draws?".

## 3. Reference implementations for architecture

| Reference | What to study | Caveat |
|---|---|---|
| Unity "Endless Runner Sample Game" (Trash Dash), Asset Store, free | Game state machine (menu/loadout/game/game-over), object pooling, additive scene loading, origin reset, curved-world shader, track pieces spawned ahead and removed behind | Reported by search results; check in the package whether theme data is ScriptableObject-based before copying the pattern |
| Unity "Create modular game architecture with ScriptableObjects" (Unity 6 edition) | Data-driven definitions, event channels, shared runtime sets: the basis of `ModeDefinition` | General guidance, not a game |
| Box2D car tutorials by Emanuele Feronato (Flash/AS3) | Car with motors and shocks, Tiny-Wings-style terrain generated ahead of the car | Different language; port the idea, not code |
| Unity `WheelJoint2D` docs | Suspension via frequency and damping ratio, motor torque; what our `RoadCar` already uses | |
| Defold + Fingersoft interview | Modular structure through message passing; Box2D matters; text-based project files help merging | A different engine, so only principles transfer |
| `mannuking/Hill-Climb-Racing` (JS clone) | An ECS-like split (core, data, entities, systems) | Educational clone with **no stated licence**. Read for structure only, copy nothing |

No open-source project was found that combines hill-style terrain generation, Unity and a
mode system. Expect to compose the base from the pieces above; this is also why the result can
be original.

## 4. Games to study for the *mode* idea [MEMORY, verify by playing]
- **Alto's Odyssey:** biome/zone changes and an endless "Zen" mode that removes fail pressure.
  Lesson: a mode can remove rules, not only add them.
- **Bad Piggies / Line Rider / Draw-to-drive games:** player-authored paths. Study how they cap
  the drawing (budget, par, time) to keep it a game.
- **Trials-style bike games:** precision physics levels. Lesson for Cargo Courier tilt control.
- **HCR2:** cups and events with modified rules. Study how rule sets are presented and rotated
  (relevant to Daily Run). Verify details before relying on them.

## 5. Mentor teardown checklist (play HCR 30-45 min and fill this in)

Fill with real observations; this replaces every INFERRED line above.

- Run length: median ___ s. Longest ___. What ends most runs? ___
- Fuel: drain rate ___, pickup spacing ___ m, refill amount ___ %.
- Coins: group size ___, spacing ___, arc height ___. Stunt bonus values ___
- Camera: look-ahead ___, zoom change with speed ___, what it does in the air ___
- First 60 seconds: what is taught, how (text/none)? ___
- Death moment: freeze? slow-mo? time to retry ___ s. Retry button size/position ___
- Ads: where, how often, forced or rewarded? ___
- Stage differences felt: gravity ___ friction ___ terrain ___ art ___ music ___
- Upgrade feel: does level 1 -> 2 change the drive noticeably? ___
- What made me want "one more run"? ___
- What annoyed me? ___

## 6. Baseline decisions (proposed, confirm before building)

**Keep from HCR:** one-thumb simplicity, resource-as-run-timer, stages-as-data, coin arcs, short
runs with long meta, forgiving physics, readable failure, light cheerful performance.
**Do differently:** the drawn road is the verb; Mode packs carry rules, not only tuning;
UI/animation polish (`rb-game-ux`, `rb-motion-design`) is a first-class feature.
**Our own idea space:** Void Run (pressure), Cargo (smoothness), Rhythm (timing), Daily (shared
seed), Moon (data-only). All must answer: how does it change the way you draw?

## Sources
- Wikipedia, Hill Climb Racing (video game): https://en.wikipedia.org/wiki/Hill_Climb_Racing_(video_game)
- Defold interview with Fingersoft: https://defold.com/2026/03/12/Interview-Fingersoft/
- PocketGamer.biz, Otto Simola talk preview: https://www.pocketgamer.biz/fingersofts-otto-simola-on-a-decade-of-hill-climb-racing-2/
- GDC Vault, "Hill Climb Racing: A Decade-long Road to 1 Billion Installs": https://gdcvault.com/play/1027259/Free-to-Play-Summit-Hill
- Macworld stage/vehicle guide: https://www.macworld.com/article/671066/how-to-win-at-hill-climb-racing.html
- Metaplay Trash Dash page: https://docs.metaplay.io/introduction/samples/trash-dash/
- Unity ScriptableObjects architecture guide: https://unity.com/en/resources/create-modular-game-architecture-scriptableobjects-unity-6
- Feronato Box2D car: https://emanueleferonato.com/2011/08/22/step-by-step-creation-of-a-box2d-cartruck-with-motors-and-shocks/
- Feronato Tiny Wings terrain: https://emanueleferonato.com/2011/08/03/create-a-terrain-like-the-one-in-tiny-wings-with-flash-and-box2d-adding-a-car/
- Unity WheelJoint2D: https://docs.unity3d.com/2022.3/Documentation/Manual/class-WheelJoint2D.html
- Hill-Climb-Racing JS clone: https://github.com/mannuking/Hill-Climb-Racing
