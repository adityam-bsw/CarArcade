# Asset catalogue and the Collection screen

Status: vehicles, surfaces and worlds are wired into gameplay and verified in Play mode.

## What it is
Every vehicle, surface and world that exists as art in the project is listed in one `AssetCatalog` asset and can be
browsed in the game: **Garage > ALL VEHICLES**, **Worlds > ALL WORLDS** and **ALL SURFACES**. Each opens the
`CollectionScreen`: group tabs, a scrolling grid of thumbnails, and a detail panel (preview, name, group, review
status, notes, SELECT). Selecting stores the pick in the save file only.

## What a pick does now
- **Vehicles:** the car is built from the catalogue picture (`RoadCar.Create(pos, VehicleRig)`): the assembled sprite with
  two invisible physics wheels under it (wheel bottoms level with the picture bottom, 30% in from each end). Width is
  the picture width / 14 px, clamped to 1.5 to 3 units. Mass, torque per wheel and a speed multiplier come from a class
  (compact, car, sport, truck, heavy, bike, offroad) in `CatalogBuilder`. The three starter cars still use the old rig;
  picking one of them with the Garage arrows clears the catalogue pick.
- **Unlocking:** each vehicle has a coin cost (0 = free). Pressing UNLOCK spends the coins and saves the id in
  `PlayerProfile.unlockedVehicleIds`. Not enough coins: the button reads NEED n MORE and is disabled.
- **Not playable yet:** boats, jet ski, hovercraft, sleds, snowboard, snowmobile, toboggan, skateboard, rocket sled
  (need water or runner physics). Shown dimmed with COMING SOON; the manager refuses them.
- **Surfaces:** a `RoadSkin` is created at run time from the strip (2 curb + 4 body + 20 skirt rows, 128 px wide) and the
  road material friction and bounce are set per surface (`friction` and `bounce` on the entry). Asphalt, dirt and neon map
  to the original road styles. The road changes at once, also in the Garage showcase. Sea water and lava crust have no strip.
- **Worlds:** rolling hills, desert and night city play with the original remix of their mood; the other worlds with four layers show that one scene as drawn (`BackgroundDirector.ApplySkin`). Underwater has no layers yet.
- **Screens:** Garage and Worlds use swipeable `CardRail`s (cards = `CollectionCard`) instead of arrows. Tap equips owned or free items; locked vehicles are focused and the button unlocks. A fresh profile gets car, asphalt and the world of its saved theme. The live world shows behind translucent bands.
- All tuning (class numbers, costs, friction) lives in the catalogue asset, filled by `CatalogBuilder.FillPlayData`.

## The catalogue pipeline (re-run whenever assets change)
```
Docs/asset-manifest/items/*.json   (status per item, kept by the asset agents)
        |  python3 tools/assets/make_previews.py  then  python3 tools/assets/find_wheels.py
        v
Assets/Art/Catalog/Previews/<id>.png + Assets/Art/Catalog/catalog.json
        |  Unity menu: Road Builder > Catalog > Build Catalog
        v
Assets/Resources/Catalog/AssetCatalog.asset   (read by the UI)
```
Then, only if screens changed: Road Builder > UI > Build Screen Prefabs.
Only manifest items with status `have`, `ready` or `forged` AND a usable preview are listed. `imported`, `rejected`,
`found` and `not_found` items are not (63 listed today: 31 vehicles, 13 surfaces, 19 worlds).

## Review rule (from `Docs/ASSET_AGENT_RULES.md`)
Game code may only reference `have`/`ready` items. The catalogue honours that: `AssetCatalog.ShowUnreviewed` is true
only in the Editor and development builds, so a **release build shows only reviewed entries**. Unreviewed (`forged`)
entries carry an "UNREVIEWED" badge, quality 2 or lower a "PLACEHOLDER" badge. Today 33 of the 63 are unreviewed.

## Files
- Game layer: `Assets/Scripts/Game/Catalog/AssetCatalog.cs` (`CatalogEntry`, `CatalogKind`, `CatalogStatus`),
  save fields in `SaveService.cs` (`selectedVehicleId`, `selectedSurfaceId`, `selectedWorldId`), `GameModel`
  (`SelectedVehicleId`...), `IGameActions.SelectCatalogItem`.
- UI layer: `Assets/Scripts/UI/Collection/` (`CollectionScreen`, `CollectionCard`, `CollectionTab`), entry buttons in
  `GarageScreen` and `WorldsScreen`, prefabs `Assets/Prefabs/UI/Screens/CollectionScreen.prefab` and
  `Components/CollectionCard.prefab`, `CollectionTab.prefab`, built by `UIPrefabBuilder`.
- Editor: `Assets/Scripts/Game/Editor/CatalogBuilder.cs`. Tools: `tools/assets/make_previews.py`.
- Tests: `Assets/Tests/EditMode/AssetCatalogTests.cs` (ids unique, previews exist, every entry is a real manifest item
  with status have/ready/forged, catalogue status matches the manifest, every kind has entries).

## Verified
- Compile clean, 0 console errors. 12 of 12 EditMode tests pass (catalogue 5, asset ledger 3, road skins 4).
- In Play mode: Garage > ALL VEHICLES, Worlds > ALL SURFACES and ALL WORLDS open and show the right items; SELECT on a
  world saved `selectedWorldId` to the save file and the button changed to SELECTED. The save was restored afterwards.
- Found and fixed a stack bug: returning from a full screen left the screen below unsubscribed and faded out when the
  user pressed Back within the 0.15 s exit animation. `ScreenStack` now tracks covered screens and restores them
  (measured: event subscribers before/during/after = 1/1/1 for a quick back-tap).

## Not verified / known gaps
- Real touch on the cards and tabs (the Editor ignores injected input; buttons were clicked through their handlers).
- Scrolling by drag, 4:3 and notch devices, and how it looks on a phone.
- Not listed yet (no usable preview): 12 surfaces imported as raw tile packs (snow, ice, sand, beach, coral, grass,
  bridges, river, cloud, floating islands) and 5 imported worlds that need converting (summer, synthwave, haunted,
  cave, spring), plus the skateboard placeholder. The forge agents that would finish them were stopped.
- The nine forged worlds were never reviewed by the inspector; their records were written by the main session.
- Forged vehicle pictures had an opaque backdrop on some previews; `make_previews.py` now removes it (`strip_backdrop`).
- Wheels spin: `tools/assets/find_wheels.py` finds the drawn wheels in each vehicle preview and cuts them into round sprites (`Assets/Art/Catalog/Wheels/`, list in `wheels.json`). Build Catalog links them to the entry (`CatalogEntry.wheels`); `RoadCar` puts the physics wheels exactly at the outermost drawn wheels and the cut-out discs rotate on top. A middle wheel (pickup) stays still. Vehicles without a found pair (moon rover, boats, sleds, tank, handcar, skateboard) keep the old invisible wheels at 30% from each end and do not spin.
- Not tested on a touch device. Balance of masses, torque and costs is a first guess.
