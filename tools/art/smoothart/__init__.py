"""smoothart: tiny procedural vector-art toolkit (standard library only, no generative AI).

See tools/art/README.md. Public surface:
    palette:  PAL, hexrgb, lerp, Ramp, smoothstep, SCENERY
    raster:   Image, Cov, Mask, write_png, bleed, tile_seam_score
    noise:    wave_fbm (seamless 1D), ValueNoise2D (seamless in X)
    shading:  vgrad, dgrad, cel3
    sprites:  Sprite (outline from display density, cel, rim, gloss), tones, mix, round_poly, circle_pts, erase_circle
"""
from .palette import PAL, SCENERY, hexrgb, lerp, Ramp, smoothstep, clamp
from .raster import Image, Cov, Mask, write_png, bleed, tile_seam_score, fill_below_curve
from .noise import wave_fbm, ValueNoise2D
from .shading import vgrad, dgrad, cel3
from .sprites import Sprite, tones, mix, round_poly, circle_pts, erase_circle, erase_poly, shift_cov, INK
