"""Compose an iOS AppIcon PNG from a transparent logo.

Alpha-composites the logo onto a white opaque background, centered, and writes an
RGB PNG. The source OpenTaiko.ico is a full-bleed logo on transparency, so the white
background and the centered inset have to be added here to match the original
checked-in icons (which sit on white with a margin). Uses only the standard library
so it runs at build time without Pillow or ImageMagick.

Usage: python3 compose_icon.py LOGO_PNG TARGET_SIZE OUT_PNG
"""
import zlib
import struct
import sys


def decode(path):
    data = open(path, 'rb').read()
    pos = 8
    idat = b''
    w = h = ct = bd = 0
    while pos < len(data):
        ln = struct.unpack('>I', data[pos:pos + 4])[0]
        typ = data[pos + 4:pos + 8]
        body = data[pos + 8:pos + 8 + ln]
        if typ == b'IHDR':
            w, h, bd, ct = struct.unpack('>IIBB', body[:10])
        elif typ == b'IDAT':
            idat += body
        elif typ == b'IEND':
            break
        pos += 12 + ln
    raw = zlib.decompress(idat)
    bpp = 4 if ct == 6 else 3
    stride = w * bpp
    rec = bytearray()
    prev = bytearray(stride)
    o = 0

    def paeth(a, b, c):
        p = a + b - c
        pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
        if pa <= pb and pa <= pc:
            return a
        return b if pb <= pc else c

    for _ in range(h):
        ft = raw[o]
        o += 1
        line = bytearray(raw[o:o + stride])
        o += stride
        for i in range(stride):
            a = line[i - bpp] if i >= bpp else 0
            b = prev[i]
            c = prev[i - bpp] if i >= bpp else 0
            v = line[i]
            if ft == 1:
                v = (v + a) & 255
            elif ft == 2:
                v = (v + b) & 255
            elif ft == 3:
                v = (v + ((a + b) >> 1)) & 255
            elif ft == 4:
                v = (v + paeth(a, b, c)) & 255
            line[i] = v
        rec += line
        prev = line
    return rec, w, h, ct, bpp, stride


def chunk(typ, data):
    return struct.pack('>I', len(data)) + typ + data + struct.pack('>I', zlib.crc32(typ + data) & 0xffffffff)


def main():
    logo_path = sys.argv[1]
    target = int(sys.argv[2])
    out_path = sys.argv[3]
    rec, lw, lh, ct, bpp, stride = decode(logo_path)
    ox = (target - lw) // 2
    oy = (target - lh) // 2
    canvas = bytearray([255]) * (target * target * 3)
    for y in range(lh):
        base = y * stride
        for x in range(lw):
            o = base + x * bpp
            a = rec[o + 3] if ct == 6 else 255
            if a == 0:
                continue
            cx = ox + x
            cy = oy + y
            if 0 <= cx < target and 0 <= cy < target:
                co = (cy * target + cx) * 3
                if a == 255:
                    canvas[co], canvas[co + 1], canvas[co + 2] = rec[o], rec[o + 1], rec[o + 2]
                else:
                    inv = 255 - a
                    canvas[co] = (rec[o] * a + 255 * inv) // 255
                    canvas[co + 1] = (rec[o + 1] * a + 255 * inv) // 255
                    canvas[co + 2] = (rec[o + 2] * a + 255 * inv) // 255
    ihdr = struct.pack('>IIBBBBB', target, target, 8, 2, 0, 0, 0)
    raw = bytearray()
    rowbytes = target * 3
    for y in range(target):
        raw.append(0)
        raw += canvas[y * rowbytes:(y + 1) * rowbytes]
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', ihdr) + chunk(b'IDAT', zlib.compress(bytes(raw), 9)) + chunk(b'IEND', b'')
    open(out_path, 'wb').write(png)


main()
