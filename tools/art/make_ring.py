"""Boost ring pickup (procedural, pure Python, own work): gold ring with dark outline and a glint, 96x96, transparent centre."""
import struct, sys, zlib, math
S = 96
c = S / 2
def px(x, y):
    d = math.hypot(x - c + 0.5, y - c + 0.5)
    for r0, r1, col in [(25, 26, (40, 20, 70)), (26, 30, (255, 170, 30)), (30, 36, (255, 235, 120)), (36, 42, (255, 170, 30)), (42, 46, (40, 20, 70))]:
        if r0 <= d < r1: return col + (255,)
    if 16 <= x < 22 and 22 <= y < 28: return (255, 255, 255, 255)   # glint
    return (0, 0, 0, 0)
raw = b"".join(b"\x00" + b"".join(bytes(px(x, y)) for x in range(S)) for y in range(S))
def chunk(t, d): return struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d))
open(sys.argv[1], "wb").write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", S, S, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b""))
