# Review: smooth-pickups-2 - 2026-11-16, asset-inspector
Verdict: ready.
Checked: generator copied to tools/art/make_pickups2.py (paths relative to the script, output dir argument); re-run reproduces all 7 staged PNGs byte for byte (cmp).
128x128 RGBA, outlined, no pixel on the border. Viewed Magnet, Double, Star: readable, cel shaded, consistent with batch 1. Rules: own work, no AI, notes and licence evidence present, ledger row added.
Star: staged name Star.png clashed with the gold batch 1 Star.png, so it is imported as Star_White.png (generator output name changed to match). Gold Star.png untouched.
Import: SmoothArtImporter (bilinear, no mips, 128 ppu; Star_White set to 200 ppu like Star).
Ratings: 4 overall, Double 3.5 (the "2" is a stroked polyline). Gem and SlowMo are both cyan; shapes differ. Not checked in-game; not wired.
