# vehicle-snowmobile smooth redo (Vehicles3)
Verdict: smooth_status ready, quality 3.5.
Checked: own-work licence evidence; no scripts in pack; generator tools/art/make_vehicles3.py reproduces bodies and previews byte for byte (cmp); art viewed: faces right, inside canvas; ppu 112, bilinear, no mips, ASTC 5x5; no wheels (bodies = previews).
Notes: reads clearly: tracked pod, cowl, windshield, upright rider. Helmet of snowmobile close to the canvas top (inside). SmoothArtImporter.IsVehicleFolder does not list Vehicles3 (new files would default to ASTC 6x6; set 5x5 explicitly here) - implementer may add it.
