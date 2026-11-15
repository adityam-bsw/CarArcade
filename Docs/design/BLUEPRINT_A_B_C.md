# Blueprint excerpts: sections A (events), B (robot/projectiles), C2 (28 surfaces). Repo copy of the user's pasted blueprint. Numbers are starting hypotheses.
## A2 Stage events (id# | event | exact rule | counterplay | duration m | difficulty | worlds)
1 Road-Eater | follows 18m behind, eats road within 8m of mouth | keep moving/boost | until escaped/600 | 7 | all
2 Gravity Shift | gravity 0.65x | shallower ramps, avoid airtime | 450 | 5 | Moon, Mars
3 Heavy Gravity | gravity 1.6x | smooth descents, avoid big jumps | 400 | 6 | Earth, Mars
4 Gravity Inversion | gravity reverses for 3 s windows | overhead paths + shield | 350 | 9 | Moon, Synthwave
5 Speed Tunnel | forward speed +45% | long smooth roads | 300 | 6 | Synthwave, hills
6 Ink Storm | ink regen 50% slower; random wind gusts | short efficient segments | 450 | 7 | Desert, Mars
7 Whiteout | visibility 30 m | follow markers, conservative slopes | 400 | 7 | Snow, haunted
8 Reverse Drive | motor reverses for 5 s bursts | adapt drawing/input | 350 | 8 | Haunted, cave
9 Mirror Road | drawing input mirrors horizontally | compensate using preview | 400 | 8 | Synthwave, moon
10 Flappy Tunnel | repeated ceiling/floor obstacles form gaps | draw through gaps; tap short hop | 250 | 8 | Candy, cave
11 Boost Drain | boost drains 2x faster | reserve boost | 400 | 6 | Volcano, desert
12 Coin Vacuum | coins drift away from player | draw through their trajectory | 350 | 5 | Hills, candy
13 Meteor Rain | marked impact zones strike after 1.2 s | draw away/boost through | 400 | 8 | Mars, moon
14 Road Collapse | segments behind vehicle disappear after 1.5 s | keep momentum | 400 | 8 | Cave, lava
15 Wind Canyon | side wind 3 m/s equivalent | counter-slope, lower profile | 450 | 7 | Hills, desert
16 Ice Age | every road slippery for 250 m | gradual curves | 350 | 7 | Snow, moon
17 Magnetic Chaos | pulled toward metallic hazards | counter-curves, shield | 350 | 8 | Synthwave, cave
18 Low Ink | ink capacity 45% | use terrain, short bridges | 300 | 8 | all
19 Elastic Road | new road segments bounce on impact | reduce ramp height | 350 | 6 | Candy, hills
20 Shadow Road | sections invisible after 2 s | follow ghost preview/markers | 300 | 9 | Haunted, cave
21 Road Tax | every 100 m costs 10 coins unless a coin gate crossed | take coin gates | 400 | 5 | all
22 Fragile Vehicle | collision damage threshold 60% | smooth road, avoid flips | 350 | 8 | all
23 Double Vehicle | ghost copy 0.5 s behind; crashes reduce combo | room for both | 300 | 7 | Synthwave
24 Falling Platforms | fixed platforms disappear 1 s after contact | connecting roads | 300 | 8 | Cave, moon
25 Reverse Gravity Zones | alternating gravity wells switch at gates | low vertical speed at gates | 350 | 9 | Moon, synthwave
26 Traffic Convoy | AI vehicles occupy road | draw over/under | 400 | 6 | Hills, desert
27 Rolling Boulder | boulder rolls ahead, destroys road on impact | jump it / guide into pit | 350 | 7 | Hills, cave
28 Electric Fences | pulsing barriers every 2 s | cross in 0.8 s safe window | 300 | 8 | Synthwave
29 Flood Surge | water rises 1 m every 5 s | stay above waterline | 350 | 8 | Underwater, hills
30 Lava Chase | lava rises 0.8 m/s | keep altitude and speed | 400 | 9 | Volcano
31 Wind Tunnels | wind direction changes at gates | change road angle before entry | 350 | 7 | Desert, snow
32 Sticky Road | road sticks to wheels; sharp turns drag | long curves | 400 | 5 | Mud, candy
33 Invisible Ink | drawn road visible only 1 s before lock | preview line | 300 | 9 | Haunted
34 Road Laser | laser scans ahead, destroys one road strip | temporary alternate path | 350 | 8 | Synthwave
35 Falling Stars | star pickups fall, become temporary platforms | catch to build escape route | 350 | 6 | Moon
36 Vehicle Swap | vehicle changes to random compatible class | adapt | 300 | 8 | all
37 Tiny Mode | vehicle scale 0.55x | narrow passages | 300 | 7 | Candy, cave
38 Giant Mode | vehicle scale 1.6x | wide stable roads | 300 | 7 | Hills, desert
39 Rhythm Gates | gates pulse to music; mistimed crossing drains ink | match beat | 400 | 8 | Synthwave, candy
40 Chaos Finale | two compatible modifiers + one elite projectile pattern | manage resources | 500 | 10 | all
Never stack: gravity inversion+invisible road; reverse driving+mirror controls; low ink+invisible ink+road collapse; tiny+giant; lava chase+flood surge; two homing projectiles; fragile vehicle+unavoidable meteor; flappy tunnel+reverse driving. Max 2 active modifiers; 100 m between activations at 5000 m.
Distance ladder: 0-1000 training; 1000 stage1 Road-Eater; 2000 first physics mutation; 3000 new movement mechanic; 4000 environmental hazard; 5000 two compatible modifiers; 6000 elite hazard; 7000 advanced surface combo; 8000 boss; 9000 extreme; 10000 survival gauntlet; 11000+ endless controlled random. Warning 150 m before (lock), main 350-650, recovery 100-200. Selection weights Easy 40/Medium 35/Hard 20/Extreme 5; seed = runSeed+stageIndex+modeId; no same primary event twice; breather after two hard stages; no tutorial-unseen events; hazard family max twice in four stages. Adaptive: after 3 fails at a stage reduce next weight, visible opt-in indicator in settings.
Monster variants (A3): Hills Dirt Maw burrows/emerges under road; Desert Sand Devourer moving sinkhole; Moon Lunar Leech jumps between floating segments; Mars Iron Worm pulls metal parts; Volcano Magma Maw burning ground; Underwater Abyss Eel suction current; Candy Gummy Gobbler stretches/snaps road; Cave Stone Jaw drops ceiling rocks; Synthwave Pixel Eraser scan-line erase; Haunted Shadow Hound teleports between shadows. Defeat options: lure into boost ring; road-collapse trap; heavy vehicle collapses weak platform; survive three pursuit phases; escape 8 s after final gate. Reward 250-500 coins, trophy, 1 mastery point, announcer line.
## B2 Projectiles (world | projectile | pattern | speed m/s | telegraph | surface interaction)
Hills Boulder Ball arc 9 dust cloud: breaks glass/weak bridges. Desert Sand Spike straight 12 ground crack: 2 s sand mound. Snow Ice Shard splitting 14 blue targeting line: 3 s slippery patch. Moon Gravity Orb homing 8 floating rings: distorts gravity 1.5 s. Mars Meteor Core arc 11 red marker: crater. Volcano Lava Bomb bouncing 9 orange glow: burns wooden roads. Underwater Pressure Bubble homing 7 bubble trail: lifts light vehicles. Candy Gumdrop Bomb bouncing 10 wrapper flash: sticky road. Cave Crystal Spike straight 15 ceiling glimmer: temporary road spike. Synthwave Neon Bolt zigzag 16 grid flash: charges neon grid. Haunted Shadow Orb homing 8 dark silhouette: road invisible 1 s. Forest Thorn Ball splitting 12 branch movement: temporary snag. City Traffic Drone straight 13 red laser: electric barrier. Cloud Storm Pearl arc 10 lightning flicker: charges metal. Toxic Swamp Acid Glob bouncing 9 green pool: weakens road durability. Only one persistent surface hazard per projectile. Near-miss within 1.2 m: 15 coins +1 combo (approved slice values 8/10/12/15, 12% cap override); combo timeout 4 s.
Boss tiers (every 5000 m): Scout first appearance single projectile; Hunter 3000+ two bursts; Interceptor 5000+ projectile+road trap; Commander 8000+ alternating patterns; Overlord 10000+ three-phase. Encounter 30-45 s, three phases, two attack windows each, counter meter 10 points disables; max three hits; shield blocks one. Defeat: 500 coins + 5 gems (first-time guaranteed, repeats coins+mastery) + exclusive robot cosmetic; first-time +250 coins. Fairness: no attack during tutorial prompt; no projectile inside hitbox; no homing faster than evadable; shield beats damage; despawn on stage transition. Announcer lines per situation, under 2.5 s (see docs/design/voice-lines.md).
## C2 Surfaces (28): surface | behaviour | counter
Asphalt stable | Ice lateral velocity retained, draw ahead | Mud wheels sink, boost out | Lava Crust breaks after 3 s, keep moving | Glass breaks after 2 heavy impacts | Cloud dissolves 2 s after contact | Rope Bridge oscillates vertically | Neon Grid aligned pads give speed bursts | Sand sinks slightly, dunes launch | Wooden Bridge burns after fire exposure | Spring Road compresses and launches | Magnetic Rail pulls metallic vehicles | Gravel loose, reduces stability | Snow accumulates soft drifts | Water buoyant sections, less traction | Acid durability decays while contacted | Oil rotates more under torque | Cobblestone bumps cause suspension oscillation | Conveyor Belt moves vehicle along road | Lava River temporary platforms emerge/sink | Sticky Candy wheels attach then release | Honey slows and sinks | Metal Plates heat moves plates | Ice Crystals breakable spikes | Floating Rocks platforms drift vertically | Haunted Shadow road fades in/out every 2 s | Electric Rail alternating safe/charged | Fragile Paper tears under heavy load. Each needs icon + verb + consequence 3-second tutorial; projectile and monster interaction per surface (see B2 table and A3).
