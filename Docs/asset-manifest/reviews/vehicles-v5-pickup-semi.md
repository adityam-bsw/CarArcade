# Review: vehicle-pickup and vehicle-semi-truck, wheel layout v5 (2026-11-17)
Verdict: ready (smooth_status stays ready). Own work, procedural, no AI, no downloads.
Checked: regenerating all 28 vehicles with the committed tools/art/make_vehicles2.py + vehicle_layout.py reproduces the staged
files byte for byte, and every other vehicle (bodies, previews, wheels) matches the shipped Vehicles2 files byte for byte.
Canvas sizes match wheels_v5.json (pickup 369x234, semi 400x208, S 9 / 8); arches are empty at the anchors; sheet and body PNGs
viewed; tile seam n/a. Old/new sheet confirms the swap is fixed (pickup = cab + open bed, 2 wheels; semi = tractor + trailer, 3 equal wheels).
Open: vehicle-pickup_w2.png is still referenced by Assets/Resources/Catalog/AssetCatalog.asset; delete it (with its .meta) after the catalog is updated to v5.
Forge quality 4: agreed.
