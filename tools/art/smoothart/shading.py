"""Colour functions: f(x, y) -> (r, g, b) evaluated at final-resolution pixel centres. Feed them to Image.paint()."""
import math
from .palette import Ramp, lerp, smoothstep, hexrgb

def vgrad(y0, y1, ramp):
    """Vertical gradient from y0 (t=0) to y1 (t=1)."""
    span = (y1 - y0) or 1.0
    return lambda x, y: ramp(min(1.0, max(0.0, (y - y0) / span)))

def dgrad(x0, y0, x1, y1, ramp):
    """Gradient along the line (x0,y0)->(x1,y1). Use it for 'light upper-left': start top-left, end bottom-right."""
    dx, dy = x1 - x0, y1 - y0
    d2 = (dx * dx + dy * dy) or 1.0
    return lambda x, y: ramp(min(1.0, max(0.0, ((x - x0) * dx + (y - y0) * dy) / d2)))

def cel3(light, mid, deep, x0, y0, x1, y1, soft=0.12, split=(0.33, 0.66)):
    """Gradient cel shading: three tones with soft (smoothstep) transitions along a light direction.
    Bible 2.4: light, mid, dark per material, soft transitions."""
    L, M, D = (hexrgb(c) if isinstance(c, str) else c for c in (light, mid, deep))
    dx, dy = x1 - x0, y1 - y0
    d2 = (dx * dx + dy * dy) or 1.0
    def f(x, y):
        t = ((x - x0) * dx + (y - y0) * dy) / d2
        a = smoothstep(split[0] - soft, split[0] + soft, t)
        b = smoothstep(split[1] - soft, split[1] + soft, t)
        return lerp(lerp(L, M, a), D, b)
    return f
