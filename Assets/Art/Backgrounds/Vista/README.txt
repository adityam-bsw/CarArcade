VISTA - ten layered pixel-art backgrounds
=========================================

10 layered pixel-art backgrounds, 384 x 216, that loop horizontally.

WHAT IS IN HERE

  <scene>/<scene>_0_sky.png     the layer files, numbered back to front
  <scene>/<scene>_1_far.png
  <scene>/<scene>_2_mid.png
  <scene>/<scene>_3_near.png
  <scene>/<scene>.png           the four composited, for reference
  <scene>/<scene>@3x.png        the same at 3x, nearest-neighbour
  <scene>/<scene>.json          layer order, scroll factors, palette
  <scene>/<scene>.gpl / .hex    the palette, for Aseprite / GIMP / LibreSprite

HOW TO USE THEM

Draw the layers back to front - 0, then 1, then 2, then 3 - and move each one
by the camera's x times its `scroll` factor:

    layer_x = -camera_x * scroll

The factors are in the JSON and are the same four in every scene, so one camera
rig takes any background in the pack:

    sky   0.00    far   0.20    mid   0.40    near  0.80

THEY LOOP. Each layer's right edge is its own left edge, so draw it twice side
by side and wrap at the frame width and it scrolls forever with no seam. That
is a property of the terrain itself, not a blur over the join.

    draw(layer, (x % W) - W)
    draw(layer, (x % W))

SCALING

384 x 216 is exactly 1920 x 1080 divided by 5, and 1280 x 720 divided by
3.333. Scale by a WHOLE NUMBER with nearest-neighbour filtering. Any other
factor makes some pixels bigger than others, which is visible immediately and
is the single most common way pixel art is displayed wrong.

COLOURS

Each scene uses at most 36 colours, shared across all four of its layers -
so a layer can be recoloured, or a sprite matched to the background, from the
one .gpl. The colours differ BETWEEN scenes: each is quantized to its own light.

LICENCE

CC0 - public domain. Free for anything, including commercial games. No credit
needed. The only thing you cannot do is resell them as an asset pack.

SEEDS AND SOURCE

Every scene here is a function, not a painting. `source/scenes.py` holds the
parameters for all ten - ridge frequencies, palettes, depths, the lot - so a
scene can be re-rendered at another size, another palette or another seed
rather than edited pixel by pixel. `source/README.md` says how.

Version 1.0.0
