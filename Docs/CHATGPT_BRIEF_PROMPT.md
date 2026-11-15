# Prompt to give ChatGPT (copy everything inside the box)

```
You are a senior mobile game designer, technical designer and AI-workflow architect. Help me turn my game into an
extremely fun, sticky, monetisable mobile game. Think big and weird, but every idea must be buildable by ONE developer
plus AI coding agents in Unity. Do not give generic advice. Give concrete rules, numbers and edge cases.

## 1. The game today
"Road Builder Racer": Unity 6, URP 2D, Android first (Google Play), side view. The player DRAWS the road with a finger
under a moving vehicle; drawing costs ink; the car must not fall, flip or crash. Already built:
- Vehicles (50+), worlds (24: hills, desert, moon, mars, volcano, underwater, candy, cave, synthwave, haunted...), road surfaces (asphalt, ice, mud, lava crust, water...).
- Boost, coin combo, air time, flips, chests every 250 m, power-ups (shield, magnet, x2, slow-mo, ghost road), gems, daily reward, missions, vehicle mastery.
- New: Frenzy mode, boost rings, meteor storms (warning marker then strike, near-miss bonus), a robot announcer (voice lines), chiptune music per world.
- Architecture: a Base game plus "Mode packs". A mode changes the game only through data (physics, economy, world, UI theme) and one rules class.
  Planned modes: Classic, Moon (low gravity), Void Run (an eraser chases you and the road behind you disappears), Cargo Courier (fragile cargo, smooth roads matter),
  Rhythm Road (road and pickups tied to a beat), Daily Run (same seed for everyone, ghosts of other players' roads; works on top of any mode).
- Constraints: 60 fps on low-end phones, no per-frame allocations, tuning in ScriptableObjects, one developer, no art team, only free/CC0 or self-made assets.

## 2. What I want ideas for

### A. Distance ladder: "every 1000 m the game changes"
- 0-1000 m: Classic (learn the game, countryside).
- 1000 m: a MONSTER (road-eater) starts chasing the player.
- 2000 m: gravity, speed and ink rules change.
- 3000 m: a brand-new mechanic. And so on: after every 1000 m a new random challenge ("stage event") begins.
Design the whole system: a list of at least 40 stage events (name, one-line pitch, exact rule, how the player counters it, how long it lasts,
which events must never combine, difficulty rating 1-10, which worlds suit it). Include a fairness/difficulty curve, how events are drawn
(weighted random, never the same twice in a row, guaranteed breather stages), how events stack at 5000 m+ and 10000 m+, and how the player is warned before each one.
Include: monster eater (how it moves, what it eats, how to escape or defeat it, variants per world), "Flappy Bird" style segments
(tap-to-flap or draw-through-gaps challenges, how that works in a road-drawing game), gravity flips, speed zones, ink storms, fog, reverse driving, mirror roads, etc.

### B. Robot that shoots damage balls
A robot appears and fires damage balls at the player, who must dodge by drawing, jumping, boosting or shielding.
The BALL TYPE CHANGES WITH THE WORLD (e.g. ice shards in arctic, lava bombs in volcano, bubbles in underwater, candy bombs in candy land, lightning in synthwave).
For at least 15 worlds give: ball name, flight pattern (straight, arc, homing, bouncing, splitting), speed, telegraph (how it warns), what a hit does,
what a near-miss gives, how it interacts with the road surface (e.g. leaves a hole, ice patch, fire), and the robot's behaviour phases/boss patterns.
Also: how the robot hovers/moves, attack patterns, escalation with distance, a "robot boss" every N km, rewards for beating it, and its voice lines.

### C. Track (road surface) properties: I do NOT like the current approach
Today each surface just multiplies five numbers (friction, bounce, speed factor, ink factor, coin factor). That is boring and invisible.
Redesign it so that the surface changes how the game PLAYS, not just numbers. Give 25+ surfaces, each with one signature behaviour
(e.g. ice: slides and you steer by drawing ahead; mud: sinks the car and needs boost to exit; lava crust: cracks and melts after a few seconds, the car must keep moving;
glass: breaks into pieces; cloud: bouncy and dissolves; rope bridge: sways; neon grid: speed pads; sand: slows then dunes launch you; wooden bridge: burns if hit by fire balls).
For each: the rule, the visual/audio feedback, the interaction with robot balls and the monster, which vehicles it favours, and how to explain it to the player in 3 seconds.
Also propose a clean data-driven architecture (ScriptableObject "behaviour modules" that can be combined) rather than hard-coded if/else.

### D. Progression and economy (Hill Climb Racing style)
- Every car costs coins/gems; every world costs more; every road surface costs more. Design the price curve, currency sinks, upgrade trees per vehicle
  (engine, suspension, ink tank, boost, shield...), world and surface unlock gates (distance goals, missions, gems, rewarded ads), daily/weekly events,
  season passes, a free-to-play-friendly and kids-safe monetisation plan (rewarded ads, no dark patterns, no gambling mechanics), and a first-hour unlock schedule.
- Give example numbers: price of each tier, expected coins per run, runs needed to unlock each item, target session length and retention hooks (D1, D7, D30).

### E. Crazy features that "sell" the game
Give 30 wild, shareable, marketable ideas (tiktok-able moments, co-op/ghost races, daily seeds, road-sharing codes, boss races, vehicle transformations,
weather, a rival robot, collectible pets, wacky physics modes, challenge of the day, etc.), ranked by (wow factor, build cost, fun). Mark the top 5 to build first.

### F. For every idea in A to E give
1. one-line pitch, 2. exact rules and numbers, 3. play case (a short story of a 20-second moment), 4. failure/edge cases and exploits,
5. data it needs (what ScriptableObject fields), 6. build cost (S/M/L for one dev + AI), 7. art/audio needed, 8. how to test that it is fun.

## 3. Art direction change: hi-res, not pixel
My current cars and worlds are too pixelated (low-res pixel art, 384x216 backgrounds). I want to ADD a high-resolution, smooth, polished 2D look
(clean vector / painted / "Hill Climb Racing 2" or "Alto's Odyssey" quality) for ALL vehicles, worlds and road surfaces, while keeping the pixel set as an alternative style.
- I cannot hire artists. Explain realistic ways for one person + AI to produce hundreds of consistent hi-res assets: AI image generation workflows
  (style locking, LoRA/reference sheets, consistent palettes), vector pipelines (SVG / procedural / shaders), parallax layer generation, seamless road-strip textures,
  car part separation (body, wheels, driver) for rigging, upscaling/cleanup tools, Unity-side tricks (Sprite Shape, shaders, URP 2D lights, normal maps).
- Licensing: what is safe for commercial Google Play release (and what is not), how to keep a provenance log.
- Mobile budget: texture sizes, atlas strategy, memory per world, compression, loading.
- Give a step-by-step production pipeline for one vehicle, one world (4 parallax layers + props) and one road surface, with exact prompts/settings, then how to scale to 50 vehicles, 24 worlds, 25 surfaces.

## 4. The part I need most: SKILLS, AGENTS and PROMPTS for my AI team
I build the game with Claude Code using "skills" (markdown instruction files) and "agents" (specialised sub-agents). Existing skills: asset pipeline,
game UX, DOTween motion, add-mode, asset agents (scout / forge / inspector). Existing agents: asset-scout (finds free licensed art), asset-forge (makes art in code),
asset-inspector (checks licences and imports).
Produce a COMPLETE, ready-to-paste kit. For each item write the real file content, not a description:
1. SKILLS (name, when to trigger, step-by-step procedure, rules, checklist). Needed at minimum: stage-event design & implementation, enemy/hazard design (monster, robot, balls),
   surface-behaviour modules, economy balancing, hi-res art generation pipeline, parallax world builder, vehicle rig builder (separate parts), VFX/juice,
   audio/music/VO production, difficulty tuning, playtest analytics, store-listing/marketing assets, kids-safety/ads compliance, performance guard for low-end Android.
2. AGENTS (name, description, tools it needs, exact system prompt, inputs/outputs, what it must never do). Needed at minimum: game-designer, level/stage-director,
   hazard-designer, economy-balancer, hires-art-director, hires-vehicle-artist, hires-world-artist, hires-surface-artist, animation/vfx-artist, sound-designer, voice-director,
   QA-playtester, unity-implementer, store-marketer. Say which can run in parallel and which must run in order.
3. PROMPT LIBRARY: ready-to-use prompts for an image-generation tool (vehicle side view with separate wheels, world parallax layers, seamless road strips, icons, store screenshots)
   with a shared "style bible" so everything matches; prompts for music (per-world BGM), SFX, and robot voice lines.
4. A phased ROADMAP (week by week for 12 weeks) saying what to build and in what order for maximum fun per hour of work, with milestones that can be playtested.

## 5. Output format
Use headings A to F, then sections 3 and 4. Use tables for lists of events, balls, surfaces and prices. Be specific, opinionated and numeric.
If something is a bad idea for a mobile road-drawing game, say so and replace it with a better one. Start with the top 10 ideas overall.
```

## How to use the answer
1. Save each SKILL it writes as `.claude/skills/<name>/SKILL.md` and each AGENT as `.claude/agents/<name>.md`.
2. Ask Claude to review every new skill against `CLAUDE.md` (layers, data in ScriptableObjects, no hardcoded UI colours).
3. Build in the order of its roadmap; the stage ladder (A) and surface behaviours (C) both fit the existing "Mode pack + data" architecture.
