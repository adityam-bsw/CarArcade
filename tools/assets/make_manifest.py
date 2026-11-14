#!/usr/bin/env python3
"""One-time generator for Docs/asset-manifest/items/*.json from the long list in
Docs/WORLD_AND_VEHICLE_CATALOGUE.md. Safe to re-run: it never overwrites an item file that already exists,
so agent progress is kept. Standard library only.

Each item is ONE file so parallel agents never write the same file (no merge races).
"""
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[2]
ITEMS = ROOT / "Docs" / "asset-manifest" / "items"

# status vocabulary (also in Docs/asset-manifest/README.md):
# todo -> scouting -> found -> downloaded -> imported -> ready            (scout path, inspector sets 'ready')
# todo -> not_found -> forging -> forged -> ready                          (forge path)
# any -> rejected (reason in notes)
# have: already in the project and verified before the agents existed

SURFACES = {
    "asphalt": ("have", "flat colours today; ambientCG textures planned", []),
    "dirt": ("have", "flat colours today", []),
    "gravel": ("todo", "", []),
    "sand": ("todo", "", []),
    "mud": ("todo", "", []),
    "grass-trail": ("todo", "", []),
    "snow": ("todo", "CodeRaurus Snow Tileset (CC0) needs a browser click; Kenney Platformer Art Winter", []),
    "ice": ("todo", "", []),
    "lava-crust": ("have", "partial: Tio Aimar volcano (vector) + InThePixel lava strip", ["Assets/Art/Surfaces/Volcano", "Assets/Art/Surfaces/Lava"]),
    "moon-dust": ("todo", "", []),
    "sea-water": ("have", "partial: POKOMOKO ocean tile; buoyancy physics is game logic", ["Assets/Art/Surfaces/Water"]),
    "river": ("todo", "", []),
    "wooden-bridge": ("todo", "", []),
    "rope-bridge": ("todo", "", []),
    "rail-track": ("todo", "", []),
    "metal-grating": ("todo", "space station floor", []),
    "glass": ("todo", "", []),
    "candy": ("todo", "", []),
    "cloud": ("todo", "", []),
    "neon-grid": ("have", "flat neon skin in code", []),
    "cobblestone": ("todo", "", []),
    "beach": ("todo", "", []),
    "salt-flat": ("todo", "", []),
    "coral": ("todo", "", []),
    "magma-bridge": ("todo", "", []),
    "ice-bridge": ("todo", "", []),
    "floating-islands": ("todo", "", []),
}

WORLDS = {
    "spring-blossom": ("todo", "", []),
    "summer": ("todo", "", []),
    "autumn-leaves": ("todo", "", []),
    "winter": ("have", "partial: VISTA tundra and hollow", ["Assets/Art/Backgrounds/Vista/tundra"]),
    "moon": ("todo", "assetpack.zip has a moon set but no licence: not usable", []),
    "mars": ("todo", "", []),
    "volcano": ("have", "VISTA kiln", ["Assets/Art/Backgrounds/Vista/kiln"]),
    "arctic": ("have", "VISTA tundra, cairn", ["Assets/Art/Backgrounds/Vista/tundra"]),
    "underwater": ("have", "ansimuz Underwater Diving Pack (art only)", ["Assets/Art/Worlds/Underwater"]),
    "jungle": ("have", "VISTA canopy", ["Assets/Art/Backgrounds/Vista/canopy"]),
    "desert": ("have", "VISTA mesa", ["Assets/Art/Backgrounds/Vista/mesa"]),
    "canyon": ("todo", "VISTA mesa is close", []),
    "night-city": ("have", "VISTA sodium", ["Assets/Art/Backgrounds/Vista/sodium"]),
    "industrial": ("have", "VISTA flue", ["Assets/Art/Backgrounds/Vista/flue"]),
    "haunted-halloween": ("todo", "", []),
    "candy-land": ("todo", "", []),
    "space-station": ("todo", "", []),
    "cloud-kingdom": ("todo", "", []),
    "cave": ("todo", "", []),
    "farm": ("todo", "", []),
    "christmas": ("todo", "", []),
    "synthwave": ("todo", "legacy procedural CyberNight exists; wants real art", []),
    "wild-west": ("todo", "", []),
    "rolling-hills": ("have", "VISTA downs (cleaned sky)", ["Assets/Art/Backgrounds/Vista/downs"]),
    "coast": ("have", "VISTA leeward", ["Assets/Art/Backgrounds/Vista/leeward"]),
}

VEHICLES = {
    "car": ("have", "Kenney sedan, hatch etc.", ["Assets/Art/Vehicles/Kenney"]),
    "sports-car": ("have", "Kenney sports_*", ["Assets/Art/Vehicles/Kenney"]),
    "race-car": ("have", "Kenney formula, sports_race", ["Assets/Art/Vehicles/Kenney"]),
    "taxi": ("have", "Kenney taxi", ["Assets/Art/Vehicles/Kenney"]),
    "police-car": ("have", "Kenney police", ["Assets/Art/Vehicles/Kenney"]),
    "monster-truck": ("todo", "current game art is a placeholder; Kenney suv_large could be re-wheeled", []),
    "pickup": ("have", "Kenney truck/pickup", ["Assets/Art/Vehicles/Kenney"]),
    "semi-truck": ("have", "Kenney truck_trailer, transport", ["Assets/Art/Vehicles/Kenney"]),
    "bus": ("have", "Kenney bus, bus_school", ["Assets/Art/Vehicles/Kenney"]),
    "tractor": ("have", "Kenney tractor", ["Assets/Art/Vehicles/Kenney"]),
    "fire-truck": ("have", "Kenney firetruck", ["Assets/Art/Vehicles/Kenney"]),
    "camper": ("have", "Kenney van_* / station", ["Assets/Art/Vehicles/Kenney"]),
    "dune-buggy": ("have", "Kenney buggy, ZaninDevelopers vintagebuggy", ["Assets/Art/Vehicles/Kenney", "Assets/Art/Vehicles/OpenGameArt"]),
    "motorcycle": ("have", "Reactorcore dirt bike, chopper, Zanin motorcycle, Kenney cycle", ["Assets/Art/Vehicles/Reactorcore", "Assets/Art/Vehicles/OpenGameArt"]),
    "quad-atv": ("todo", "not confirmed in the Kenney pack by name", []),
    "bicycle": ("todo", "", []),
    "scooter": ("have", "Kenney scooter", ["Assets/Art/Vehicles/Kenney"]),
    "tuk-tuk": ("todo", "", []),
    "golf-cart": ("todo", "", []),
    "mine-cart": ("todo", "", []),
    "handcar": ("todo", "", []),
    "shopping-cart": ("todo", "", []),
    "jet-ski": ("todo", "no free licence-verified art found yet", []),
    "speedboat": ("todo", "", []),
    "hovercraft": ("todo", "", []),
    "snowmobile": ("todo", "", []),
    "sled": ("todo", "", []),
    "toboggan": ("todo", "", []),
    "skateboard": ("todo", "", []),
    "snowboard": ("todo", "", []),
    "moon-rover": ("todo", "", []),
    "tank": ("todo", "", []),
    "rocket-sled": ("todo", "", []),
}

SPECS = {
    "surface": "Side-view strip texture that repeats along a road ribbon: rows from the top = top surface (about 2 px), "
               "slab body (about 4 px), shadow skirt (about 20 px). Pixel art, point filtered, tileable horizontally, "
               "palette-limited to match the VISTA backgrounds. Also needs a physics hint (grip low/normal/high).",
    "world": "Four horizontally looping layers 384x216 (sky, far, mid, near) like VISTA, pixel art, transparent where "
             "the layer should show what is behind it. Or a source we can convert to that format.",
    "vehicle": "Side-view pixel art facing right, about 30 to 70 px wide (our world: one art pixel is 1/16 unit). "
               "Prefer separate wheel sprites so a physics rig can be built, or a clean body plus wheel positions.",
}


def write(category, key, status, notes, have):
    item_id = f"{category}-{key}"
    path = ITEMS / f"{item_id}.json"
    if path.exists():
        return False
    item = {
        "id": item_id,
        "category": category,
        "name": key.replace("-", " ").title(),
        "status": status,
        "requirements": SPECS[category],
        "have": have,
        "notes": notes,
        "source": None,
        "licence": None,
        "licence_evidence": None,
        "download_dir": None,
        "bytes": None,
        "sha256": None,
        "project_path": None,
        "quality": None,
        "history": [],
    }
    path.write_text(json.dumps(item, indent=2) + "\n", encoding="utf-8")
    return True


def main():
    ITEMS.mkdir(parents=True, exist_ok=True)
    made = 0
    for cat, table in (("surface", SURFACES), ("world", WORLDS), ("vehicle", VEHICLES)):
        for key, (status, notes, have) in table.items():
            if write(cat, key, status, notes, have):
                made += 1
    print(f"created {made} item files in {ITEMS.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
