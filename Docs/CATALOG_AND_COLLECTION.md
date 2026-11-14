# Asset catalogue and the Collection screen

Status: built and verified in the Editor (UI first, no gameplay logic touched).

## What it is
Every vehicle, surface and world that exists as art in the project is listed in one `AssetCatalog` asset and can be
browsed in the game: **Garage > ALL VEHICLES**, **Worlds > ALL WORLDS** and **ALL SURFACES**. Each opens the
`CollectionScreen`: group tabs, a scrolling grid of thumbnails, and a detail panel (preview, name, group, review
status, notes, SELECT). Selecting stores the pick in the save file only.

## Deliberately not wired (the logic section)
Road logic, physics, vehicle rigs, `RoadSkin` assignment and the background remixer do not read the selection yet.
The screen says so ("Saved for the next update. It is not on the track yet."). Each later step reads
`GameServices.Model.SelectedVehicleId / SelectedSurfaceId / SelectedWorldId`:
1. Vehicles: a `VehicleDefinition` and a data-driven `RoadCar` rig (wheels, hull, runner, hover).
2. Surfaces: a `RoadSkin` per surface from the forged strips (atlas) and a physics profile per surface.
3. Worlds: a `BackgroundSkin` (and mood) per world so the remixer can use the new scenes.

## The catalogue pipeline (re-run whenever assets change)
```
Docs/asset-manifest/items/*.json   (status per item, kept by the asset agents)
        |  python3 tools/assets/make_previews.py
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
- Forged vehicles are 2x taller than typical Kenney cars; scale is not normalised yet (cards scale each to fit).
