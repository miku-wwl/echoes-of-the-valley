"""Painted textures for the original caretaker (procedural; deterministic seeds). Writes PNGs into
game/Assets/Echoes/Art/Phase52/Textures. Every texture is authored by this code: no photograph, scan, stock texture or
reference illustration is sampled or traced. Layout rectangles come from atlas.py (shared with the modelling code).

python tools/art/phase52b2/textures.py [Skin Hair Teal Cream Burgundy Leather Tights Trinket]
"""
import math, os, sys
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import atlas as A
from paint import *

OUT = os.path.normpath(os.path.join(HERE, '..', '..', '..', 'game', 'Assets', 'Echoes', 'Art', 'Phase52', 'Textures'))


def px(rect, w, h):
    """Rect (u0, v0, u1, v1) -> pixel box (x0, y0, x1, y1) with row 0 at the top."""
    u0, v0, u1, v1 = rect
    x0, x1 = sorted((int(round(u0 * w)), int(round(u1 * w))))
    y0, y1 = sorted((int(round((1 - v0) * h)), int(round((1 - v1) * h))))
    return x0, y0, min(x1, w), min(y1, h)


def lerp(a, b, t):
    return a + (b - a) * t


# =============================================================================== skin / face

def eye_curves(side, n=48):
    ex, ey = side * 0.0375, 1.5235
    xi, xo = side * 0.0105, side * 0.0650
    yi, yo = ey - 0.0025, ey + 0.0035
    u = np.linspace(0, 1, n)
    x = lerp(xi, xo, u)
    base = lerp(yi, yo, u)
    upper = base + 0.0275 * np.sin(np.pi * u ** 0.78) ** 0.95 * (1 - 0.15 * u)
    lower = base - 0.0155 * np.sin(np.pi * u) ** 1.15
    return x, upper, lower, base, (ex, ey)


def paint_skin(blink=False, size=1024, ss=3):
    S = size
    fx = lambda x: (x / A.FACE_U_SCALE + 0.5) * (S - 1)
    fy = lambda y: (1 - ((y - A.FACE_CENTER_Y) / A.FACE_V_SCALE + 0.5)) * (S - 1)
    c = Canvas(S, S, srgb('F8C9AE'), ss=ss)
    skin_light, skin_warm = srgb('FBD3B8'), srgb('F2A894')
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    wx = (xx / (S - 1) - 0.5) * A.FACE_U_SCALE
    wy = A.FACE_CENTER_Y + (0.5 - yy / (S - 1)) * A.FACE_V_SCALE
    n = noise(S, S, 60, 11, 2)
    base = skin_light[None, None, :] * (1 - 0.35 * smooth(1.50, 1.43, wy)[..., None]) + skin_warm * 0.35 * smooth(1.50, 1.43, wy)[..., None]
    c.rgb = base * (0.985 + 0.03 * n[..., None])
    # warm cheeks, nose tip, forehead tone
    for side in (-1, 1):
        c.put(c.ellipse_mask(fx(side * 0.052), fy(1.4935), 0.019 / 0.2 * S, 0.0105 / 0.28 * S, 0.0, 4), srgb('FF8E8E'), 0.46)
        c.put(c.ellipse_mask(fx(side * 0.047), fy(1.4960), 0.010 / 0.2 * S, 0.0055 / 0.28 * S, 0.0, 2), srgb('FFB0A8'), 0.35)
    c.put(c.ellipse_mask(fx(0), fy(1.4905), 0.0075 / 0.2 * S, 0.0055 / 0.28 * S, 0.0, 3), srgb('FFA89A'), 0.35)
    # soft shadow under the bangs
    shade = smooth(1.612, 1.575, wy) * smooth(0.095, 0.05, np.abs(wx))
    c.rgb *= 1 - 0.20 * shade[..., None] * np.array([0.35, 0.75, 0.80], np.float32)
    # neck/hands/ear patch (also the back of the head): flat, slightly deeper skin
    flat = (xx > 0.90 * S) & (yy < 0.10 * S)
    c.rgb[flat] = srgb('F6C2A6')

    px_per_m = S / A.FACE_U_SCALE
    for side in (-1, 1):
        x, up, lo, base_y, (ex, ey) = eye_curves(side)
        up_p = [(fx(a), fy(b)) for a, b in zip(x, up)]
        lo_p = [(fx(a), fy(b)) for a, b in zip(x, lo)]
        opening = c.poly_mask(up_p + lo_p[::-1])
        lid_dark, lash_col = srgb('3A1D14'), srgb('2A140E')
        if not blink:
            c.put(opening, srgb('FFF9F3'))
            top_shadow = c.stroke_mask(up_p, 0.0085 * px_per_m, blur=2.2) * opening
            c.multiply(top_shadow, srgb('C9A4AA'), 0.85)
            # iris with vertical glow, limbus, pupil and highlights, clipped by the lids
            ix = lerp(side * 0.0105, side * 0.0650, 0.505)
            iy = ey + 0.0016
            rx, ry = 0.0215, 0.0252
            fxp, fyp = fx(ix), fy(iy)
            rxp, ryp = rx * px_per_m, ry * px_per_m
            r = np.hypot((xx - fxp) / rxp, (yy - fyp) / ryp)
            tt = np.clip((yy - (fyp - ryp)) / (2 * ryp), 0, 1)
            stops = [(0.0, (0.29, 0.14, 0.07)), (0.32, (0.55, 0.29, 0.10)), (0.72, (0.88, 0.57, 0.21)), (1.0, (0.99, 0.80, 0.38))]
            col = np.stack([np.interp(tt, [s[0] for s in stops], [s[1][k] for s in stops]) for k in range(3)], -1)
            ang = np.arctan2(yy - fyp, xx - fxp)
            streak = 1 + 0.09 * np.sin(26 * ang + 3 * noise(S, S, 40, 21)) * smooth(0.35, 0.8, r)
            col = col * streak[..., None]
            col *= (1 - 0.55 * smooth(0.80, 0.97, r))[..., None] * np.array([1, 0.9, 0.85], np.float32)
            pupil = smooth(0.44, 0.36, np.hypot((xx - fxp) / (rxp * 0.9), (yy - fyp) / (ryp * 1.0)))
            col = col * (1 - pupil[..., None]) + srgb('140805') * pupil[..., None]
            iris = smooth(1.02, 0.96, r) * opening
            c.put(iris, col)
            c.multiply(c.stroke_mask(up_p, 0.0095 * px_per_m, blur=2.6) * opening * iris, srgb('7A5A5A'), 0.55)
            # highlights
            c.put(c.ellipse_mask(fx(ix - side * 0.0068), fy(iy + 0.0092), 0.0060 * px_per_m, 0.0076 * px_per_m, -0.3 * side, 0.9) * opening, srgb('FFFFFF'), 0.97)
            c.put(c.ellipse_mask(fx(ix + side * 0.0072), fy(iy - 0.0074), 0.0030 * px_per_m, 0.0030 * px_per_m, 0, 0.5) * opening, srgb('FFFFFF'), 0.92)
            c.put(c.ellipse_mask(fx(ix - side * 0.0030), fy(iy - 0.0100), 0.0016 * px_per_m, 0.0016 * px_per_m, 0, 0.4) * opening, srgb('FFFFFF'), 0.85)
            # lower lid line and tear duct
            c.put(c.stroke_mask(lo_p[3:-5], 0.0017 * px_per_m, 0.6), srgb('9A5C4F'), 0.65)
            c.put(c.ellipse_mask(fx(side * 0.0120), fy(ey - 0.0028), 0.0030 * px_per_m, 0.0022 * px_per_m, 0, 0.5), srgb('F58C8C'), 0.65)
        else:
            sleepy = [(fx(a), fy(b + 0.0005 - 0.0075 * math.sin(math.pi * t))) for a, b, t in zip(x, base_y, np.linspace(0, 1, len(x)))]
            up_p = sleepy
        # upper lid: thick tapered line with an outer wing and lashes
        widths = [0.0030 * px_per_m * (0.55 + 1.15 * math.sin(math.pi * min(t * 1.1, 1)) ** 0.7) if t > 0.04 else 0.0016 * px_per_m for t in np.linspace(0, 1, len(up_p))]
        c.put(c.stroke_mask(up_p, 1, 0.5, widths), lid_dark)
        ox, oy = up_p[-1]
        wing = [(ox - side * 0.0008 * px_per_m, oy), (ox + side * 0.0042 * px_per_m, oy - 0.0016 * px_per_m), (ox + side * 0.0072 * px_per_m, oy - 0.0030 * px_per_m)]
        c.put(c.stroke_mask(wing, 1, 0.4, [0.0026 * px_per_m, 0.0016 * px_per_m, 0.0004 * px_per_m]), lash_col)
        for k, (t, ln, ang) in enumerate(((0.66, 0.0050, 0.75), (0.82, 0.0058, 0.95), (0.94, 0.0054, 1.15))):
            i = int(t * (len(up_p) - 1))
            lx, ly = up_p[i]
            end = (lx + side * math.cos(ang - 0.4) * ln * px_per_m * 0.6 + 0, ly - math.sin(ang) * ln * px_per_m)
            c.put(c.stroke_mask([(lx, ly), (lx + side * 0.30 * ln * px_per_m, ly - 0.55 * ln * px_per_m), end], 1, 0.3, [0.0013 * px_per_m, 0.0009 * px_per_m, 0.0002 * px_per_m]), lash_col)
        if not blink:
            crease = [(fx(a), fy(b + 0.0058 + 0.0022 * math.sin(math.pi * t))) for a, b, t in zip(x[6:-8], up[6:-8], np.linspace(0, 1, len(x[6:-8])))]
            c.put(c.stroke_mask(crease, 0.0012 * px_per_m, 0.5), srgb('9C6258'), 0.5)
        # brow
        bt = np.linspace(0, 1, 18)
        bx = lerp(side * 0.0135, side * 0.0668, bt)
        by = 1.5755 + 0.0078 * np.sin(np.pi * bt ** 1.15) - 0.0016 * bt
        bw = [0.0016 * px_per_m * (0.5 + 1.6 * math.sin(math.pi * min(t * 0.92 + 0.04, 1))) for t in bt]
        c.put(c.stroke_mask([(fx(a), fy(b)) for a, b in zip(bx, by)], 1, 0.7, bw), srgb('4A2A1C'), 0.9)
    # nose: two soft nostril hints and a tip glint
    for side in (-1, 1):
        c.put(c.stroke_mask([(fx(side * 0.0070), fy(1.4895)), (fx(side * 0.0058), fy(1.4872)), (fx(side * 0.0036), fy(1.4868))], 0.0019 * px_per_m, 0.5), srgb('C27C6E'), 0.50)
    c.put(c.ellipse_mask(fx(0.0004), fy(1.4930), 0.0024 * px_per_m, 0.0018 * px_per_m, 0, 0.8), srgb('FFEDE2'), 0.30)
    # mouth: small gentle smile
    mt = np.linspace(-1, 1, 24)
    mx = mt * 0.0170
    my = 1.4690 - 0.0046 * (1 - mt ** 2) + 0.0024 * np.abs(mt) ** 3
    c.put(c.ellipse_mask(fx(0), fy(1.4640), 0.0098 * px_per_m, 0.0034 * px_per_m, 0, 0.6), srgb('F4A09A'), 0.75)
    c.put(c.stroke_mask([(fx(a), fy(b)) for a, b in zip(mx, my)], 1, 0.5, [0.0010 * px_per_m * (1.2 + math.cos(t * 1.4)) for t in mt]), srgb('8E3E3E'), 0.95)
    c.put(c.ellipse_mask(fx(-0.0020), fy(1.4615), 0.0020 * px_per_m, 0.0009 * px_per_m, 0, 0.4), srgb('FFE3DA'), 0.5)
    return c


# =============================================================================== hair

def paint_hair(size=1024):
    S = size
    c = Canvas(S, S, srgb('6B3F28'), ss=2)
    rng = np.random.default_rng(5202)
    half = S // 2
    stops = [(0.0, srgb('3A1F14')), (0.18, srgb('552E1E')), (0.42, srgb('7A4630')), (0.72, srgb('96593A')), (1.0, srgb('B97A4E'))]
    # ---- clump strip (left half): root at the top, tip at the bottom
    v = np.linspace(0, 1, S, dtype=np.float32)[:, None]
    u = np.linspace(0, 1, half, dtype=np.float32)[None, :]
    col = np.stack([np.interp(v[:, 0], [s[0] for s in stops], [s[1][k] for s in stops]) for k in range(3)], -1)[:, None, :] * np.ones((1, half, 1), np.float32)
    edge = 1 - 0.42 * np.abs(2 * u - 1) ** 2.4
    col = col * edge[..., None]
    grain = tile_noise(S, half, 10, 31)
    col *= (0.93 + 0.14 * grain)[..., None]
    clump = 0.5 + 0.5 * np.sin(math.tau * (u * 1.7 + 1.6 * noise(8, half, 40, 77)[0][None, :]))     # broad, irregular clump shading (regular fine stripes read as wood grain)
    col = col * (1 + 0.09 * (clump - 0.5))[..., None]
    c.rgb[:, :half] = col
    rng = np.random.default_rng(5203)
    for k in range(26):
        x = rng.uniform(4, half - 4)
        wob = rng.uniform(1.5, 6)
        ph = rng.uniform(0, 6.28)
        y0 = rng.uniform(0, S * 0.30)
        y1 = rng.uniform(S * 0.50, S * 0.98)
        pts = [(x + wob * math.sin(ph + y / 120.0), y) for y in np.linspace(y0, y1, 30)]
        light = rng.random() < 0.5
        m = c.stroke_mask(pts, rng.uniform(1.0, 2.8), 0.4)
        m[:, half:] = 0
        c.put(m, srgb('E2A56C') if light else srgb('2B150E'), rng.uniform(0.05, 0.11))
    # soft glossy band across each clump (the anime "ring"): rows 24..38 % from the root
    band = smooth(0.22, 0.27, v) * smooth(0.40, 0.34, v)
    sheen = band * (1 - np.abs(2 * u - 1) ** 1.6)
    c.rgb[:, :half] = c.rgb[:, :half] * (1 - sheen[..., None] * 0.58) + srgb('F6CC9A') * sheen[..., None] * 0.58
    band2 = smooth(0.62, 0.68, v) * smooth(0.76, 0.70, v)
    c.rgb[:, :half] = c.rgb[:, :half] * (1 - (band2 * (1 - np.abs(2 * u - 1)))[..., None] * 0.22) + srgb('E4B27E') * (band2 * (1 - np.abs(2 * u - 1)))[..., None] * 0.22
    # ---- scalp cap (right half): crown at the top, hairline at the bottom; u wraps around the head, front at u = 0.5
    cu = np.linspace(0, 1, half, dtype=np.float32)[None, :]
    cv = np.linspace(0, 1, S, dtype=np.float32)[:, None]
    cstops = [(0.0, srgb('3F2216')), (0.25, srgb('5A311F')), (0.6, srgb('774430')), (1.0, srgb('8F5236'))]
    ccol = np.stack([np.interp(cv[:, 0], [s[0] for s in cstops], [s[1][k] for s in cstops]) for k in range(3)], -1)[:, None, :] * np.ones((1, half, 1), np.float32)
    ccol *= (0.93 + 0.14 * tile_noise(S, half, 12, 41))[..., None]
    rng = np.random.default_rng(5204)
    cap = Canvas(half, S, (0, 0, 0), ss=2)
    cap.rgb = ccol.copy()
    for k in range(64):
        x = rng.uniform(0, half)
        pts = [(x + 4 * math.sin(y / 90.0 + k), y) for y in np.linspace(rng.uniform(0, 200), rng.uniform(S * 0.6, S), 24)]
        m = cap.stroke_mask(pts, rng.uniform(1.0, 2.4), 0.4)
        cap.put(m, srgb('D69A63') if rng.random() < 0.5 else srgb('26120C'), rng.uniform(0.04, 0.09))
    ring = smooth(0.40, 0.46, cv) * smooth(0.60, 0.53, cv)
    front = 0.35 + 0.65 * np.exp(-((cu - 0.5) / 0.30) ** 2)
    side_glow = 0.55 * (np.exp(-((cu - 0.12) / 0.10) ** 2) + np.exp(-((cu - 0.88) / 0.10) ** 2))
    r = ring * np.clip(front + side_glow * 0.4, 0, 1)
    cap.rgb = cap.rgb * (1 - r[..., None] * 0.36) + srgb('F2C894') * r[..., None] * 0.36
    cap.rgb *= (1 - 0.35 * smooth(0.86, 1.0, cv))[..., None]
    c.rgb[:, half:] = cap.rgb
    c.a[:] = 1.0
    return c


if __name__ == '__main__':
    import textures_garments as G
    which = sys.argv[1:] or ['all']
    os.makedirs(OUT, exist_ok=True)
    painters = {'Skin': lambda: paint_skin(False), 'Skin_Blink': lambda: paint_skin(True), 'Hair': paint_hair, 'Teal': G.paint_teal,
                'Cream': G.paint_cream, 'Burgundy': G.paint_burgundy, 'Leather': G.paint_leather, 'Tights': G.paint_tights, 'Trinket': G.paint_trinket}
    for name in (list(painters) if which == ['all'] else which):
        painters[name]().save(os.path.join(OUT, name + '.png'))
        print('painted', name, flush=True)
