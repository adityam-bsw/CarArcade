"""Seamless noise. Everything here is periodic in X so layers tile without a seam."""
import math, random
from functools import lru_cache

@lru_cache(maxsize=None)
def _phases(seed, octaves):
    rnd = random.Random(seed)
    return tuple(rnd.uniform(0, 6.2832) for _ in range(octaves))

def wave_fbm(x, period, seed, octaves=5, base=1, gain=0.5, lacunarity=1.7):
    """Periodic 1D fractal wave, value roughly in -1..1. Integer cycle counts per period guarantee a seamless wrap."""
    ph = _phases(seed, octaves)
    tot = amp = 0.0
    out, a, k = 0.0, 1.0, float(base)
    for o in range(octaves):
        kk = max(1, int(round(k)))
        out += a * math.sin(2 * math.pi * kk * x / period + ph[o])
        tot += a; a *= gain; k *= lacunarity
    return out / tot

class ValueNoise2D:
    """Smooth value noise on a lattice whose X cells wrap (cells_x must divide the tile width conceptually)."""
    def __init__(self, cells_x, cells_y, seed):
        rnd = random.Random(seed)
        self.cx, self.cy = cells_x, cells_y
        self.g = [[rnd.random() for _ in range(cells_x)] for _ in range(cells_y + 1)]
    def at(self, u, v):
        """u in [0,1) wraps, v in [0,1]. Returns 0..1."""
        x = (u % 1.0) * self.cx; y = min(max(v, 0.0), 1.0) * self.cy
        x0 = int(x); y0 = min(int(y), self.cy - 1)
        fx = x - x0; fy = y - y0
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy)
        x1 = (x0 + 1) % self.cx; x0 %= self.cx
        g = self.g
        a = g[y0][x0] + (g[y0][x1] - g[y0][x0]) * fx
        b = g[y0 + 1][x0] + (g[y0 + 1][x1] - g[y0 + 1][x0]) * fx
        return a + (b - a) * fy
