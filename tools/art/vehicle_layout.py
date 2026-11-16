"""Per-vehicle wheel layout (v4) for the smooth catalogue vehicles. Own code, no generative AI.

All numbers are OLD pixel units (canvas = old canvas, scaled by S in the renderer). For every vehicle: `gb` is the ground
line (bottom of the lowest tyre), wheels are (cx, r[, flags]) so cy = gb - r. Rules: car-like rear axle 18-24 percent of body
length from the rear, front axle 74-82 percent; big-wheel vehicles keep the tyres inside the silhouette. A vehicle that is not
listed keeps its old anchors from vehicles2_inventory.json (bikes, quad, moon rover and the wheel-less ones).
flags: "noarch" = no wheel-arch liner is cut for that wheel (small jockey / caster wheels).
"""
import math
LAYOUT = {
    "vehicle-car":          dict(gb=13.5, wheels=[(7.3, 3.5), (24.4, 3.5)]),
    "vehicle-police-car":   dict(gb=14.5, wheels=[(8.1, 3.7), (27.5, 3.7)]),
    "vehicle-sports-car":   dict(gb=13.5, wheels=[(8.1, 3.6), (27.5, 3.6)]),
    "vehicle-taxi":         dict(gb=15.5, wheels=[(8.1, 3.8), (27.5, 3.8)]),
    "vehicle-race-car":     dict(gb=10.0, wheels=[(7.5, 3.7), (27.5, 3.3)]),
    "vehicle-dune-buggy":   dict(gb=14.5, wheels=[(6.0, 4.0), (19.7, 4.0)]),
    "vehicle-monster-truck": dict(gb=28.5, wheels=[(8.6, 7.5), (31.0, 7.5)]),
    "vehicle-pickup":       dict(gb=24.5, wheels=[(6.5, 3.6), (13.0, 3.6), (35.0, 3.6)]),
    "vehicle-bus":          dict(gb=21.5, wheels=[(11.0, 4.5), (36.0, 4.5)]),
    "vehicle-fire-truck":   dict(gb=21.5, wheels=[(10.5, 4.5), (35.0, 4.5)]),
    "vehicle-camper":       dict(gb=19.5, wheels=[(8.4, 4.0), (28.3, 4.0)]),
    "vehicle-semi-truck":   dict(gb=24.5, wheels=[(8.0, 3.6), (14.5, 3.6), (40.0, 2.4, "noarch")]),
    "vehicle-tractor":      dict(gb=19.0, wheels=[(6.2, 5.2), (20.0, 3.2)]),
    "vehicle-scooter":      dict(gb=10.5, wheels=[(3.7, 2.6), (13.8, 2.6)]),
    "vehicle-golf-cart":    dict(gb=18.0, wheels=[(7.4, 4.2), (24.8, 4.2)]),
    "vehicle-tuk-tuk":      dict(gb=18.0, wheels=[(7.5, 4.0), (26.5, 4.0)]),
    "vehicle-shopping-cart": dict(gb=16.0, wheels=[(7.5, 3.0, "noarch"), (21.5, 3.0, "noarch")]),
}

def wheels_for(vid, inv):
    """List of wheel dicts {cx, cy, r, size, noarch} in old px for a vehicle."""
    lay = LAYOUT.get(vid)
    if lay is None:
        return [dict(w, noarch=False) for w in inv[vid]["wheels"]]
    out = []
    for t in lay["wheels"]:
        cx, r = t[0], t[1]
        out.append(dict(cx=cx, cy=round(lay["gb"] - r, 3), r=r, size=int(math.ceil(2 * r)), noarch=(len(t) > 2 and t[2] == "noarch")))
    return out
