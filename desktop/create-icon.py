"""Regenerate clock.ico using only Python's standard library."""
from pathlib import Path
import math
import struct
import zlib

size = 128
segments = [(64, 19, 64, 109), (101, 19, 101, 48),
            (64, 48, 27, 48), (64, 80, 27, 80)]  # Cistercian 2026

def distance(x, y, segment):
    ax, ay, bx, by = segment
    t = max(0, min(1, ((x-ax)*(bx-ax)+(y-ay)*(by-ay))/((bx-ax)**2+(by-ay)**2)))
    return math.hypot(x-ax-t*(bx-ax), y-ay-t*(by-ay))

rows = bytearray()
for y in range(size):
    rows.append(0)
    for x in range(size):
        d = min(distance(x, y, s) for s in segments)
        glow = .24*math.exp(-d*d/48)
        core = max(0, min(1, 2.7-d))
        light = max(core, glow)
        alpha = max(0, min(1, 16-math.hypot(max(0, 16-x, x-111), max(0, 16-y, y-111))))
        rows.extend((round(2+102*light), round(6+249*light), round(4+149*light), round(255*alpha)))

def chunk(kind, data):
    return struct.pack('!I', len(data))+kind+data+struct.pack('!I', zlib.crc32(kind+data))

png = b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR', struct.pack('!2I5B', size, size, 8, 6, 0, 0, 0))+chunk(b'IDAT', zlib.compress(rows))+chunk(b'IEND', b'')
ico = struct.pack('<3H', 0, 1, 1)+struct.pack('<4B2H2I', size, size, 0, 0, 1, 32, len(png), 22)+png
Path(__file__).with_name('clock.ico').write_bytes(ico)
