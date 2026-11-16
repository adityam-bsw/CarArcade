# Asset status

Generated 2026-11-16 from `Docs/asset-manifest/items/*.json` by `tools/assets/report.py`. Do not edit by hand.

| Category | have | todo | scouting | found | downloaded | imported | not_found | forging | forged | ready | rejected | total |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| surface | 3 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 40 | 1 | 44 |
| world | 9 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 19 | 1 | 29 |
| vehicle | 14 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 5 | 14 | 1 | 34 |

## Surfaces

| Item | Status | Source | Licence | Project path | Notes |
|---|---|---|---|---|---|
| Smooth road strips batch 2 (sand, snow, gravel, cobblestone, moon dust, neon grid, wooden bridge, rail track) | ready | tools/art/make_road2.py (own code, tools/art/smoothart) | own work (procedural, no generative AI, no third-party pixels) | Assets/Art/Smooth/Roads | Ratings: cobblestone 4, snow 4, wooden_bridge 4, neon_grid 4, sand 3.5, gravel 3 |
| Smooth road strips batch 3 (spring_road, magnetic_rail, acid, oil, conveyor, conveyor_b, lava_river, sticky_candy, honey, metal_plates, ice_crystals, haunted_shadow, electric_rail, electric_rail_off, fragile_paper, fragile_paper_torn) | ready | tools/art/make_road3.py (own code, tools/art/smoothart) | own work (procedural, no generative AI, no third-party pixels) | Assets/Art/Smooth/Roads | Ratings: honey 4, electric_rail 4, spring_road 3.5, magnetic_rail 3.5, acid 3.5, |
| Smooth road strips batch 4 (lava_crust, magma_bridge, sea_water, river, candy, cloud, coral, beach, floating_islands, ice_bridge, rope_bridge, salt_flat) | ready | tools/art/make_road4.py (own code, tools/art/smoothart) | own work (procedural, no generative AI, no third-party pixels) | Assets/Art/Smooth/Roads | Ratings: lava_crust 4, magma_bridge 4, sea_water 4, river 3.5, candy 3.5, cloud  |
| Smooth road strips (asphalt, dirt, grass, ice, mud, glass + 3 crack stages) | ready | tools/art/make_road.py (own code, tools/art/smoothart) | own work (procedural, no generative AI, no third-party pixels) | Assets/Art/Smooth/Roads | Does not replace the pixel surface-* items (still used by the game until the imp |
| Acid | ready | tools/art/make_road3.py (own code) | own work (procedural; no generative AI) | Assets/Art/Smooth/Roads/road_acid_1024x192.png | Catalogue item for the smooth strip Assets/Art/Smooth/Roads/road_acid_1024x192.p |
| Asphalt | have |  |  |  | flat colours today; ambientCG textures planned |
| Beach | ready | https://opengameart.org/content/seasonal-platformer-tiles | own work (procedural; palette reference Blaze sea tileset and GrafxKid seasonal tropical column (CC0)) | Assets/Art/Surfaces/Forged/surface-beach/surface-beach.png | GrafxKid Tiles_4.png tropical seaside ground column (sand top, 16px) plus blaze  |
| Candy | ready |  | own work (procedural; palette reference no pack (original colours)) | Assets/Art/Surfaces/Forged/surface-candy/surface-candy.png |  | FORGED 2026-11-15: procedural 128x26 strip, grip normal, quality 3. |
| Cloud | ready | https://opengameart.org/content/blazes-platformer-pack-3-... | own work (procedural; palette reference Blaze plains sky tileset (CC0)) | Assets/Art/Surfaces/Forged/surface-cloud/surface-cloud.png | Blaze plains/sky tileset.png has a small cloud/sky tile (few tiles, 16px). Weak; |
| Cobblestone | ready |  | own work (procedural; palette sampled from CC0 Kenney-style greys) | Assets/Art/Surfaces/Forged/surface-cobblestone/surface-cobblestone.png | Procedural forge, quality 3.5/5. Physics hint: grip normal-high, bounce 0.05 (ra |
| Conveyor | ready | tools/art/make_road3.py (own code) | own work (procedural; no generative AI) | Assets/Art/Smooth/Roads/road_conveyor_1024x192.png | Catalogue item for the smooth strip Assets/Art/Smooth/Roads/road_conveyor_1024x1 |
| Coral | ready | https://opengameart.org/content/blazes-platformer-pack-3-... | own work (procedural; palette reference Blaze sea tileset coral slab (CC0)) | Assets/Art/Surfaces/Forged/surface-coral/surface-coral.png | Blaze sea/sea_tileset.png salmon/coral-pink rock slab with scalloped top, plus c |
| Dirt | have |  |  |  | flat colours today |
| Electric Rail | ready | tools/art/make_road3.py (own code) | own work (procedural; no generative AI) | Assets/Art/Smooth/Roads/road_electric_rail_1024x192.png | Catalogue item for the smooth strip Assets/Art/Smooth/Roads/road_electric_rail_1 |
| Floating Islands | ready | https://opengameart.org/content/the-field-of-the-floating... | own work (procedural; palette reference Buch Field of the Floating Islands tiles (CC0)) | Assets/Art/Surfaces/Forged/surface-floating-islands/surface-floating-islands.png | Buch Field of the Floating Islands tiles_packed_1.png / tiles_16.png: grass isla |
| Fragile Paper | ready | tools/art/make_road3.py (own code) | own work (procedural; no generative AI) | Assets/Art/Smooth/Roads/road_fragile_paper_1024x192.png | Catalogue item for the smooth strip Assets/Art/Smooth/Roads/road_fragile_paper_1 |
| Glass | ready |  | own work (procedural; palette sampled from CC0 tundra) | Assets/Art/Surfaces/Forged/surface-glass/surface-glass.png | Procedural forge, quality 3.0/5. Physics hint: grip very low (slides like ice),  |
| Grass Trail | ready | https://opengameart.org/content/the-field-of-the-floating... | own work (procedural; palette reference Buch Floating Islands grass-over-earth tiles (CC0)) | Assets/Art/Surfaces/Forged/surface-grass-trail/surface-grass-trail.png | Buch Floating Islands tiles_packed_1.png: grass-topped earth with dark rock body |
| Gravel | ready |  | own work (procedural; palette sampled from CC0 cairn/downs) | Assets/Art/Surfaces/Forged/surface-gravel/surface-gravel.png | Procedural forge, quality 3.0/5. Physics hint: grip normal-low (loose), bounce 0 |
| Haunted Shadow | ready | tools/art/make_road3.py (own code) | own work (procedural; no generative AI) | Assets/Art/Smooth/Roads/road_haunted_shadow_1024x192.png | Catalogue item for the smooth strip Assets/Art/Smooth/Roads/road_haunted_shadow_ |
| Honey | ready | tools/art/make_road3.py (own code) | own work (procedural; no generative AI) | Assets/Art/Smooth/Roads/road_honey_1024x192.png | Catalogue item for the smooth strip Assets/Art/Smooth/Roads/road_honey_1024x192. |
| Ice Bridge | ready | https://opengameart.org/content/blazes-platformer-pack-3-... | own work (procedural; palette reference Blaze snowy_world ice slab (CC0)) | Assets/Art/Surfaces/Forged/surface-ice-bridge/surface-ice-bridge.png | Blaze snow_tileset.png ice slab plus thin floating snow/ice platforms; recolour/ |
| Ice Crystals | ready | tools/art/make_road3.py (own code) | own work (procedural; no generative AI) | Assets/Art/Smooth/Roads/road_ice_crystals_1024x192.png | Catalogue item for the smooth strip Assets/Art/Smooth/Roads/road_ice_crystals_10 |
| Ice | ready | https://opengameart.org/content/blazes-platformer-pack-3-... | own work (procedural; palette reference Blaze snowy_world ice blocks (CC0)) | Assets/Art/Surfaces/Forged/surface-ice/surface-ice.png | Blaze snow_tileset.png has glossy cyan ice blocks (right half). Tileable block;  |
| Lava Crust | ready |  | own work (procedural; palette reference Lava pack (Assets/Art/Surfaces/Lava, CC0) colours and surface-magma-bridge style) | Assets/Art/Surfaces/Forged/surface-lava-crust/surface-lava-crust.png | partial: Tio Aimar volcano (vector) + InThePixel lava strip | FORGED 2026-11-15: |
| Lava River | ready | tools/art/make_road3.py (own code) | own work (procedural; no generative AI) | Assets/Art/Smooth/Roads/road_lava_river_1024x192.png | Catalogue item for the smooth strip Assets/Art/Smooth/Roads/road_lava_river_1024 |
| Magma Bridge | ready |  | own work (procedural; palette sampled from CC0 Assets/Art/Surfaces/Lava/lava_strip8.png) | Assets/Art/Surfaces/Forged/surface-magma-bridge/surface-magma-bridge.png | Procedural forge, quality 3.0/5. Physics hint: grip normal, bounce 0.0 (damage/h |
| Magnetic Rail | ready | tools/art/make_road3.py (own code) | own work (procedural; no generative AI) | Assets/Art/Smooth/Roads/road_magnetic_rail_1024x192.png | Catalogue item for the smooth strip Assets/Art/Smooth/Roads/road_magnetic_rail_1 |
| Metal Grating | rejected | https://opengameart.org/content/sci-fi-platform-tiles | CC0 |  | Eris sci-fi platform tiles 16x16/32x32: light-grey metal platform slabs with dar |
| Metal Plates | ready | tools/art/make_road3.py (own code) | own work (procedural; no generative AI) | Assets/Art/Smooth/Roads/road_metal_plates_1024x192.png | Catalogue item for the smooth strip Assets/Art/Smooth/Roads/road_metal_plates_10 |
| Moon Dust | ready |  | own work (procedural; palette sampled from CC0 tundra/cairn greys) | Assets/Art/Surfaces/Forged/surface-moon-dust/surface-moon-dust.png | Procedural forge, quality 3.0/5. Physics hint: grip normal-low, bounce 0.0 (low  |
| Mud | ready |  | own work (procedural; palette sampled from CC0 kiln/mesa) | Assets/Art/Surfaces/Forged/surface-mud/surface-mud.png | Procedural forge, quality 3.0/5. Physics hint: grip low (slippery, drags speed), |
| Neon Grid | have |  |  |  | flat neon skin in code |
| Oil | ready | tools/art/make_road3.py (own code) | own work (procedural; no generative AI) | Assets/Art/Smooth/Roads/road_oil_1024x192.png | Catalogue item for the smooth strip Assets/Art/Smooth/Roads/road_oil_1024x192.pn |
| Rail Track | ready |  | own work (procedural; palette sampled from CC0 Kenney greys, kiln browns) | Assets/Art/Surfaces/Forged/surface-rail-track/surface-rail-track.png | Procedural forge, quality 3.0/5. Physics hint: grip high, bounce 0.05 (clack). I |
| River | ready | https://opengameart.org/content/the-field-of-the-floating... | own work (procedural; palette reference Buch tiles_packed_1 water + Blaze sea waves (CC0)) | Assets/Art/Surfaces/Forged/surface-river/surface-river.png | Buch tiles_packed_1.png water tiles with grass banks and blaze sea waves (x/sea/ |
| Rope Bridge | ready | https://opengameart.org/content/the-field-of-the-floating... | own work (procedural; palette reference Buch Floating Islands plank piece (CC0), rope drawn from scratch) | Assets/Art/Surfaces/Forged/surface-rope-bridge/surface-rope-bridge.png | Same small plank piece as wooden bridge in Buch pack; no rope. Forge needed. Ken |
| Salt Flat | ready |  | own work (procedural; palette sampled from CC0 mesa) | Assets/Art/Surfaces/Forged/surface-salt-flat/surface-salt-flat.png | Procedural forge, quality 3.0/5. Physics hint: grip normal-low, bounce 0.0. Impo |
| Sand | ready | https://opengameart.org/content/blazes-platformer-pack-3-... | own work (procedural; palette reference Blaze sea tileset sandy slab (CC0, Assets/Art/Surfaces/BlazesPlatformer/sea)) | Assets/Art/Surfaces/Forged/surface-sand/surface-sand.png | Blaze sea/sea_tileset.png: sandy-coral earth slab with scalloped top. Also seaso |
| Sea Water | ready |  | own work (procedural; palette reference POKOMOKO ocean tile (Assets/Art/Surfaces/Water, CC0) colours) | Assets/Art/Surfaces/Forged/surface-sea-water/surface-sea-water.png | partial: POKOMOKO ocean tile; buoyancy physics is game logic | FORGED 2026-11-15 |
| Snow | ready | https://opengameart.org/content/blazes-platformer-pack-3-... | own work (procedural; palette reference Blaze snowy_world snow tileset (CC0)) | Assets/Art/Surfaces/Forged/surface-snow/surface-snow.png | Blaze snowy world snow_tileset.png (snow-capped earth slab, 16px tiles) in x/sno |
| Spring Road | ready | tools/art/make_road3.py (own code) | own work (procedural; no generative AI) | Assets/Art/Smooth/Roads/road_spring_road_1024x192.png | Catalogue item for the smooth strip Assets/Art/Smooth/Roads/road_spring_road_102 |
| Sticky Candy | ready | tools/art/make_road3.py (own code) | own work (procedural; no generative AI) | Assets/Art/Smooth/Roads/road_sticky_candy_1024x192.png | Catalogue item for the smooth strip Assets/Art/Smooth/Roads/road_sticky_candy_10 |
| Wooden Bridge | ready | https://opengameart.org/content/the-field-of-the-floating... | own work (procedural; palette reference Buch Floating Islands brown plank piece (CC0)) | Assets/Art/Surfaces/Forged/surface-wooden-bridge/surface-wooden-bridge.png | Buch tiles_packed_1.png contains a small brown plank/bridge piece among grass ti |

## Worlds

| Item | Status | Source | Licence | Project path | Notes |
|---|---|---|---|---|---|
| Smooth Countryside near layer v2 (fences, boulders, flowers) and light alternate | ready | tools/art/make_bg_countryside_v2.py (own code, tools/art/... | own work (procedural, no generative AI, no third-party pixels) | Assets/Art/Smooth/Backgrounds/Countryside | Quality v2 3.5, light 3. Imported alongside the v1 near layer, which is untouche |
| Smooth Countryside background (sky, sun, 4 clouds, far, mid, near) | ready | tools/art/make_bg_countryside.py (own code, tools/art/smo... | own work (procedural, no generative AI, no third-party pixels) | Assets/Art/Smooth/Backgrounds/Countryside | Pixel world-rolling-hills (VISTA downs) remains the game's world until the imple |
| Smooth Desert background (sky, sun, far, mid, near) | ready | tools/art/make_bg_worlds.py --world desert (own code, too... | own work (procedural, no generative AI, no third-party pixels) | Assets/Art/Smooth/Backgrounds/Desert | Quality 4. Four-layer contract as Countryside (41 ppu, 2048 tiling layers, near  |
| Smooth Night City background (sky, moon, stars overlay, far, mid, near) | ready | tools/art/make_bg_worlds.py --world nightcity (own code, ... | own work (procedural, no generative AI, no third-party pixels) | Assets/Art/Smooth/Backgrounds/NightCity | Quality 4. Far/mid seam scores 9.7/45 are an artefact of the metric (roof-height |
| Arctic | have |  |  |  | VISTA tundra, cairn |
| Autumn Leaves | ready |  | own work (derivative of CC0 VISTA by najjar320) | Assets/Art/Worlds/Forged/world-autumn-leaves/ | Forged scene, 4 layers 384x216. Forge run stopped before notes; records written  |
| Candy Land | ready |  | own work (derivative of CC0 VISTA by najjar320) | Assets/Art/Worlds/Forged/world-candy-land/ | Forged scene, 4 layers 384x216. Forge run stopped before notes; records written  |
| Canyon | ready |  | own work (derivative of CC0 VISTA by najjar320) | Assets/Art/Worlds/Forged/world-canyon/ | Forged scene, 4 layers 384x216. Forge run stopped before notes; records written  |
| Cave | ready | https://opengameart.org/content/warped-caves-pixel-art-pack | own work (palette inspired by CC0 ansimuz Warped Caves (no pixels copied)) | Assets/Art/Worlds/Forged/world-cave/ | Only 2 layers: background 240x176 and middleground 272x176 (purple crystal cave) |
| Christmas | ready |  | own work (derivative of CC0 VISTA by najjar320) | Assets/Art/Worlds/Forged/world-christmas/ | Forged scene, 4 layers 384x216. Forge run stopped before notes; records written  |
| Cloud Kingdom | ready |  | own work (derivative of CC0 VISTA by najjar320) | Assets/Art/Worlds/Forged/world-cloud-kingdom/ | Forged scene, 4 layers 384x216. Forge run stopped before notes; records written  |
| Coast | have |  |  |  | VISTA leeward |
| Desert | have |  |  |  | VISTA mesa |
| Farm | ready |  | own work (derivative of CC0 VISTA by najjar320) | Assets/Art/Worlds/Forged/world-farm/ | Forged scene, 4 layers 384x216. Forge run stopped before notes; records written  |
| Haunted Halloween | ready | https://opengameart.org/content/desert-forest | own work (CC0 VISTA by najjar320 (Assets/Art/Backgrounds/Vista/canopy)) | Assets/Art/Worlds/Forged/world-haunted-halloween/ | 5 layers 120x120 (bgtooga1-5) dark purple spooky dead-tree forest, CC0 pixel art |
| Industrial | have |  |  |  | VISTA flue |
| Jungle | have |  |  |  | VISTA canopy |
| Mars | ready |  | own work (derivative of CC0 VISTA by najjar320) | Assets/Art/Worlds/Forged/world-mars/ | Forged scene, 4 layers 384x216. Forge run stopped before notes; records written  |
| Moon | ready |  | own work (derivative of CC0 VISTA by najjar320) | Assets/Art/Worlds/Forged/world-moon/ | Forged scene, 4 layers 384x216. Forge run stopped before notes; records written  |
| Night City | have |  |  |  | VISTA sodium |
| Rolling Hills | have |  |  |  | VISTA downs (cleaned sky) |
| Space Station | rejected | https://opengameart.org/content/parallax-space-scene-seam... | CC0 |  | 8 CC0 PNGs, 2000x2000 seamless nebula/stars layers; smooth (not pixel art) so ne |
| Spring Blossom | ready | https://opengameart.org/content/parallax-background-fores... | own work (CC0 VISTA by najjar320 (Assets/Art/Backgrounds/Vista/downs)) | Assets/Art/Worlds/Forged/world-spring-blossom/ | Fallback only: same green-hills layered pack; no blossoms. Would need forge reco |
| Summer | ready | https://opengameart.org/content/parallax-background-fores... | own work (CC0 VISTA by najjar320 (Assets/Art/Backgrounds/Vista/downs)) | Assets/Art/Worlds/Forged/world-summer/ | 8 separate 1280x360 layers (sky, clouds, sun, mountains 1-3, trees, rocks), tile |
| Synthwave | ready | https://opengameart.org/content/parallax-city-night-4-colors | own work (palette inspired by CC0 CityNight4Col 4-colour pack (no pixels copied)) | Assets/Art/Worlds/Forged/world-synthwave/ | 4 layers + cars (sky, far, mid, close) 160x144, 4-colour purple/pink palette, ti |
| Underwater | ready |  | own work (palette inspired by CC0 ansimuz Underwater Diving Pack (no pixels copied)) | Assets/Art/Worlds/Forged/world-underwater/ | ansimuz Underwater Diving Pack (art only) | FORGED 2026-11-15: four 384x216 laye |
| Volcano | have |  |  |  | VISTA kiln |
| Wild West | ready |  | own work (derivative of CC0 VISTA by najjar320) | Assets/Art/Worlds/Forged/world-wild-west/ | Forged scene, 4 layers 384x216. Forge run stopped before notes; records written  |
| Winter | have |  |  |  | partial: VISTA tundra and hollow |

## Vehicles

| Item | Status | Source | Licence | Project path | Notes |
|---|---|---|---|---|---|
| Smooth slice vehicles (Classic cherry red, Sport cyan, Monster orange; bodies + 3 wheels) | ready | tools/art/make_vehicles.py | own work (procedural, no generative AI, no third-party pixels) | Assets/Art/Smooth/Vehicles | Same canvases as Resources/RoadBuilder CarBody/SportCarBody/MonsterTruckBody/Whe |
| Bicycle | ready |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack palette) | Assets/Art/Vehicles/Forged/vehicle-bicycle/ |  Forged: Frame is 1 px lines with no outline so it is lighter than the outlined  |
| Bus | have |  |  |  | Kenney bus, bus_school |
| Camper | have |  |  |  | Kenney van_* / station |
| Car | have |  |  |  | Kenney sedan, hatch etc. |
| Dune Buggy | have |  |  |  | Kenney buggy, ZaninDevelopers vintagebuggy |
| Fire Truck | have |  |  |  | Kenney firetruck |
| Golf Cart | ready |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack) | Assets/Art/Vehicles/Forged/vehicle-golf-cart/ | Forged: golfcart_body.png 30x15 (roof, seat, steering wheel, white body), golfca |
| Handcar | ready |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack) | Assets/Art/Vehicles/Forged/vehicle-handcar/ | Forged: handcar_platform.png 32x5, handcar_post.png 4x9, handcar_lever.png 30x9  |
| Hovercraft | forged |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack) | Assets/Art/Vehicles/Forged/vehicle-hovercraft/ | Forged: hovercraft_body.png 33x12 (hull + dome + skirt), hovercraft_fan_a.png an |
| Jet Ski | forged |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack) | Assets/Art/Vehicles/Forged/vehicle-jet-ski/ | Forged: jet_ski_body.png 38x16 (hull, seat, handlebar, facing right), jet_ski_sp |
| Mine Cart | rejected | https://opengameart.org/content/opp2017-cave-and-mine-cart | CC0 |  | OPP2017 cave pack. cart/spr_obj_cart_str_{back,front,complete}_anim.gif: 48x48 a |
| Monster Truck | ready |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack) | Assets/Art/Vehicles/Forged/vehicle-monster-truck/ | Forged: monster_body.png 33x11 (Kenney suv_large cab with its wheels erased), mo |
| Moon Rover | ready |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack palette) | Assets/Art/Vehicles/Forged/vehicle-moon-rover/ |  Forged: Dish and mast are crude; the hull looks like a boat; palette is the Ken |
| Motorcycle | have |  |  |  | Reactorcore dirt bike, chopper, Zanin motorcycle, Kenney cycle |
| Pickup | have |  |  |  | Kenney truck/pickup |
| Police Car | have |  |  |  | Kenney police |
| Quad Atv | ready |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack palette) | Assets/Art/Vehicles/Forged/vehicle-quad-atv/ | not confirmed in the Kenney pack by name Forged: Reads as a quad more by the fat |
| Race Car | have |  |  |  | Kenney formula, sports_race |
| Rocket Sled | forged |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack palette) | Assets/Art/Vehicles/Forged/vehicle-rocket-sled/ |  Forged: Flame sprites are not shown in preview.png and the flame orientation (t |
| Scooter | have |  |  |  | Kenney scooter |
| Semi Truck | have |  |  |  | Kenney truck_trailer, transport |
| Shopping Cart | ready |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack) | Assets/Art/Vehicles/Forged/vehicle-shopping-cart/ | Forged: cart_body.png 26x12 (wire basket, handle, frame), cart_wheel.png 5x5. Li |
| Skateboard | forged | https://opengameart.org/content/skateboard-0 | own work (palette from CC0 Kenney) | Assets/Art/Vehicles/Forged/vehicle-skateboard/ | Only a tiny 29x12 red board silhouette, no visible wheels, no rider. Usable as d |
| Sled | ready |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack palette) | Assets/Art/Vehicles/Forged/vehicle-sled/ |  Forged: Rider is a generic blocky figure; runner curl is only 2 px. |
| Snowboard | ready |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack palette) | Assets/Art/Vehicles/Forged/vehicle-snowboard/ |  Forged: Rider is a stiff standing pose with no lean or arms out; one pose only. |
| Snowmobile | ready |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack palette) | Assets/Art/Vehicles/Forged/vehicle-snowmobile/ |  Forged: Track has no animation frames; windscreen is a rough shape; ski does no |
| Speedboat | forged |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack) | Assets/Art/Vehicles/Forged/vehicle-speedboat/ | Forged: speedboat_body.png 50x21 (hull + cabin + windshield, facing right). Limi |
| Sports Car | have |  |  |  | Kenney sports_* |
| Tank | ready | https://opengameart.org/content/32x32-m1-shepard-tank-set... | CC0 | Assets/Art/Vehicles/Tank/ | Side view pixel tank, facing right, 32x32 frames, 11 frames (352x32 strip; movin |
| Taxi | have |  |  |  | Kenney taxi |
| Toboggan | ready |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack palette) | Assets/Art/Vehicles/Forged/vehicle-toboggan/ |  Forged: Curled front is chunky; board looks more like a banana than a slat tobo |
| Tractor | have |  |  |  | Kenney tractor |
| Tuk Tuk | ready |  | own work (derivative of CC0 Kenney Pixel Vehicle Pack) | Assets/Art/Vehicles/Forged/vehicle-tuk-tuk/ | Forged: tuktuk_body.png 31x13 (canopy, seat, green body), tuktuk_wheel.png 7x7.  |
