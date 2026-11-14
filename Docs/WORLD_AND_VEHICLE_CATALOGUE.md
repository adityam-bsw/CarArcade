# Worlds, surfaces and vehicles: possibilities and what we have

Status: research plus downloads done on 2026-11-14. Licences were read on each creator's own page (and in the
licence files shipped with the art) before anything was downloaded. Anything not marked "have" is a candidate only.

## 1. The idea: three independent things plus the rules
- **Surface** (what the vehicle travels on): asphalt, dirt, ice, lava crust, water... Today this is `RoadSkin`.
- **World** (what is behind it): hills, desert, moon, underwater, a season... Today this is `BackgroundSkin`.
- **Vehicle** (what travels): car, truck, bike, jet ski... Today this is hard-coded in `RoadCar`.
- **Rules** (modes): gravity, ink, goals. They only need the surface's shape and physics numbers.
A surface also carries **physics** (grip, bounce, and for water buoyancy) separate from its look, and a vehicle
carries a **rig type** that decides which surfaces it can use.

## 2. Possibilities (the long list)
**Surfaces:** asphalt, dirt, gravel, sand, mud, grass trail, snow, ice, lava crust, moon dust, sea water, river,
wooden bridge, rope bridge, rail track, metal grating (space station), glass, candy, cloud, neon grid, cobblestone,
beach, salt flat, coral, magma bridge, ice bridge, floating islands.
**Worlds:** the four seasons (spring blossom, summer, autumn leaves, winter), moon, Mars, volcano, arctic, ocean and
underwater, jungle, desert, canyon, night city, industrial, haunted (Halloween), candy land, space station, cloud
kingdom, cave, farm, Christmas, synthwave, wild west.
**Vehicles:** car, sports car, race car, taxi, police car, monster truck, pickup, semi truck, bus, tractor, fire truck,
camper, dune buggy, motorcycle (chopper, dirt bike), quad/ATV, bicycle, scooter, tuk-tuk, golf cart, mine cart,
handcar, shopping cart, jet ski, speedboat, hovercraft, snowmobile, sled, toboggan, skateboard, snowboard, moon
rover, tank, rocket sled.
**Natural pairings:** jet ski with water; snowmobile or sled with snow and ice; rover with moon; boat with river;
monster truck with mud; tractor with farm; mine cart with cave; hovercraft with lava or water.

## 3. What we have (downloaded, licence verified, imported under `Assets/Art/`)
| Group | Pack | Licence | What it gives us |
|---|---|---|---|
| Vehicles | Kenney Pixel Vehicle Pack (50 vehicles) | CC0 | Side-view pixel: cars, sports and race cars, taxi, police, fire truck, ambulance, buses, school bus, camper, vans, pickup, semi trucks, tractor, motorbikes, quads. About 2.8 units wide: same scale as our car |
| Vehicles | Reactorcore Motocross Bike | CC0 | Modular dirt bike for physics games: 8 chassis colours, 5 wheel types, front suspension, chain, canister |
| Vehicles | Chasersgaming chopper; ZaninDevelopers motorcycle, buggy, hippie van | CC0 | Extra bikes and two retro vehicles |
| Surfaces | Tio Aimar Volcano Pack 1.1 | CC0 | Lava and ash ground, rocks, crystals, volcano background. **Vector style, needs pixel conversion** |
| Surfaces | InThePixel animated lava (8 frames) | CC0 | Pixel lava animation |
| Surfaces | POKOMOKO animated ocean tile | CC0 | Pixel water texture frames |
| Worlds | ansimuz Underwater Diving Pack (art only) | CC0 | Seabed tiles, coral midground, props, bubbles, effects |
| Worlds | VISTA (earlier) | CC0 | Ten looping scenes: hills, rainforest, coast, desert, volcanic, alpine, mist, tundra, night city, industrial |
| Surfaces | ambientCG textures (earlier shortlist) | CC0 | Photo road textures to convert, not yet downloaded |

## 4. Gaps and honest notes
| Wanted | Finding |
|---|---|
| **Jet ski, speedboat, hovercraft, snowmobile, sled, rover, bicycle** | No free, licence-verified side-view pixel art found for any of them. Options: adapt a bike body, recolour and re-rig a vehicle we have (a snowmobile is a bike with a ski), or commission or draw them. This is the biggest gap |
| **Moon surface and moon world** | Only unverified candidates (Lunar Battle Pack, Stone&Moon, OpenGameArt "CC0 Backgrounds"). The earlier `assetpack.zip` has a moon set but no licence file |
| **Snow and ice surface** | CodeRaurus Snow Tileset (CC0) exists but itch.io needs a browser click to download. Kenney Platformer Art Winter (CC0, 100 files) page gave no download link to my tool |
| **Water as a surface** | We have a water tile, but a vehicle riding on water needs buoyancy physics, which is game logic, not art |
| **Seasons** | VISTA tundra and hollow cover winter and mist. No verified autumn, spring or summer pack yet (candidates: itch.io autumn forest packs, licences unchecked) |
| **Volcano pack style** | Smooth vector art: will not match the pixel world until converted |
| **Rejected** | Cartoon "Side View Cars" (vector style); Free Ice Forest and Starter Tiles (CC-BY, credit needed); several itch.io packs with custom terms |

## 5. What each new thing needs from the code
- **New surfaces** fit the existing `RoadSkin` (texture atlas along the road). A `Surface` should add a physics
  profile: friction, bounce, and later buoyancy (Unity has `BuoyancyEffector2D`).
- **New worlds** are more `BackgroundSkin` assets. Underwater and lava need their own layers and moods.
- **New vehicles** need a `VehicleDefinition` asset: body sprite, wheel sprites and anchor points, mass, motor,
  a **rig type** (two wheel, four wheel, hull that floats, ski runner, hover) and the surfaces it works on.
  Today `RoadCar` hard-codes three cars. The modular Reactorcore bike is a natural first test of the new rig data.
- **Unlocks and the Garage** then show vehicles by compatibility with the chosen world.

## 6. Suggested order
1. `VehicleDefinition` + data-driven `RoadCar`, proven first with the Kenney truck and the Reactorcore bike.
2. Surface textures for the existing road (ambientCG downloads) and pixel-convert the volcano and lava art.
3. A water surface with buoyancy and a vehicle that floats (the first jet-ski-like rig, using a simple hull).
4. Fill gaps: moon, snow, seasons, with verified downloads or commissioned art.
