"""Painted foliage and terrain textures for the Phase 5.2-B2 route (procedural, deterministic, original).

LeafClusters.png   1024x1024, 2x2 cells of 512: oak puff, aspen (small round leaves), willow (long narrow leaves), maple-like broad leaves; alpha = leaf shape
GroundPlants.png   1024x1024, 2x2 cells: grass tuft, fern, clover with white flowers, tall grass with lavender spikes; alpha = shape
RouteGrass.png     512 tileable RGB: lush painterly meadow
RouteDirt.png      512 tileable RGB: packed earth with pebbles
python tools/art/phase52b2/foliage_textures.py [LeafClusters GroundPlants RouteGrass RouteDirt]
"""
import math, os, sys
import numpy as np
from scipy import ndimage
from PIL import Image, ImageDraw
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from paint import Canvas, srgb, smooth
OUT = os.path.normpath(os.path.join(HERE, '..', '..', '..', 'game', 'Assets', 'Echoes', 'Art', 'Phase52', 'Textures'))


def wrap_noise(h, w, sigma, seed):
    rng = np.random.default_rng(seed)
    n = ndimage.gaussian_filter(rng.random((h, w)).astype(np.float32), sigma, mode='wrap')
    return (n - n.min()) / max(n.max() - n.min(), 1e-6)


def lens(cx, cy, length, width, ang, n=20, bulge=0.5):
    """Pointed leaf outline (lens shape), base at (cx, cy) pointing along `ang`."""
    pts = []
    for k in range(n + 1):
        t = k / n
        w = width * 0.5 * math.sin(math.pi * t) ** 0.85 * (1 - 0.35 * (t - bulge) ** 2 * 2)
        pts.append((t * length, w))
    outline = [(x, y) for x, y in pts] + [(x, -y) for x, y in pts[::-1]]
    ca, sa = math.cos(ang), math.sin(ang)
    return [(cx + x * ca - y * sa, cy + x * sa + y * ca) for x, y in outline]


def leaf(c, cx, cy, length, width, ang, base, tip, rib, edge, alpha=1.0):
    poly = lens(cx, cy, length, width, ang)
    m = c.poly_mask(poly, 0.35)
    ys, xs = np.nonzero(m > 0.02)
    if len(xs) == 0:
        return
    # colour ramps from the leaf base to its tip
    ca, sa = math.cos(ang), math.sin(ang)
    t = np.clip(((xs - cx) * ca + (ys - cy) * sa) / length, 0, 1)[:, None]
    col = base[None, :] * (1 - t) + tip[None, :] * t
    across = np.abs(-(xs - cx) * sa + (ys - cy) * ca) / (width * 0.5 + 1e-3)
    col = col * (1 - 0.22 * across[:, None] ** 2.2) + edge[None, :] * 0.0
    a = (m[ys, xs] * alpha)[:, None]
    c.rgb[ys, xs] = c.rgb[ys, xs] * (1 - a) + col * a
    c.a[ys, xs] = np.maximum(c.a[ys, xs], m[ys, xs])
    c.put(c.stroke_mask([(cx, cy), (cx + ca * length * 0.9, cy + sa * length * 0.9)], max(1.2, width * 0.06), 0.3) * m, rib, 0.55)
    # darker rim so each leaf reads as an illustrated shape
    rim = m * (1 - ndimage.minimum_filter(m, 5)) if False else None


def paint_leaf_cell(c, ox, oy, kind, seed):
    rng = np.random.default_rng(seed)
    cell = Canvas(512, 512, (0, 0, 0), alpha=0.0, ss=2)
    deep, mid, light, sun = srgb('1F4B1C'), srgb('3C7A28'), srgb('7FB43A'), srgb('B9D54E')
    count = {'oak': 46, 'aspen': 70, 'willow': 60, 'broad': 30}[kind]
    leaves = []
    for _ in range(count):
        if kind == 'willow':
            x = rng.uniform(70, 440)
            y = rng.uniform(40, 210)
            ang = math.pi / 2 + rng.normal(0, 0.28)
            L, W = rng.uniform(150, 215), rng.uniform(17, 25)
        else:
            r = math.sqrt(rng.random()) * (190 if kind != 'broad' else 160)
            th = rng.uniform(0, math.tau)
            x, y = 256 + r * math.cos(th), 256 + r * math.sin(th) * 0.92
            ang = math.atan2(y - 256, x - 256) + rng.normal(0, 0.75)
            if kind == 'aspen':
                L, W = rng.uniform(62, 84), rng.uniform(52, 70)
            elif kind == 'broad':
                L, W = rng.uniform(110, 150), rng.uniform(64, 86)
            else:
                L, W = rng.uniform(84, 124), rng.uniform(30, 46)
        tipx, tipy = x + math.cos(ang) * L, y + math.sin(ang) * L
        if not (46 < x < 466 and 46 < y < 466 and 46 < tipx < 466 and 46 < tipy < 466):
            continue
        d = math.hypot(x - 256, y - 256) / 190
        leaves.append((d, x, y, L, W, ang))
    leaves.sort(key=lambda v: -v[0])                       # inner leaves are painted last (on top)
    for d, x, y, L, W, ang in leaves:
        shade = float(np.clip(0.25 + 0.75 * d + rng.normal(0, 0.12), 0, 1))
        sunny = rng.random() < 0.28 * shade
        base = deep * (1 - shade) + mid * shade
        tip = (sun if sunny else light) * (0.55 + 0.5 * shade) + mid * (0.45 - 0.5 * shade * 0.5)
        leaf(cell, x, y, L, W, ang, np.clip(base, 0, 1), np.clip(tip, 0, 1), np.clip(light * 1.15, 0, 1), deep)
    # keep a transparent margin so no cutout edge ever touches the cell border
    cell.a[:34, :] = 0; cell.a[-34:, :] = 0; cell.a[:, :34] = 0; cell.a[:, -34:] = 0
    # soft darkening towards the bottom of the cluster (volume) and a clean outline
    yy = np.linspace(0, 1, 512, dtype=np.float32)[:, None]
    cell.rgb *= (1 - 0.18 * yy)[..., None]
    grey = cell.rgb.mean(-1, keepdims=True)
    cell.rgb = (grey + (cell.rgb - grey) * 0.86) * 0.95
    edge = np.clip(cell.a - ndimage.minimum_filter(cell.a, size=5), 0, 1)
    cell.rgb *= (1 - 0.35 * edge)[..., None]
    c.rgb[oy:oy + 512, ox:ox + 512] = cell.rgb
    c.a[oy:oy + 512, ox:ox + 512] = cell.a


def paint_leaf_clusters():
    c = Canvas(1024, 1024, (0.2, 0.4, 0.15), alpha=0.0, ss=2)
    for (ox, oy, kind, seed) in ((0, 0, 'oak', 301), (512, 0, 'aspen', 302), (0, 512, 'willow', 303), (512, 512, 'broad', 304)):
        paint_leaf_cell(c, ox, oy, kind, seed)
    return c


# ---------------------------------------------------------------------------- ground plants

def blade(c, x, y, h, w, lean, base, tip, curve=0.35):
    pts = [(x + lean * h * t ** 1.6 + curve * h * 0.25 * math.sin(t * 3.1) , y - h * t) for t in np.linspace(0, 1, 14)]
    widths = [max(0.8, w * (1 - t) ** 0.8) for t in np.linspace(0, 1, 14)]
    m = c.stroke_mask(pts, 1, 0.3, widths)
    ys, xs = np.nonzero(m > 0.02)
    if len(xs) == 0:
        return
    t = np.clip((y - ys) / max(h, 1), 0, 1)[:, None]
    col = base[None, :] * (1 - t) + tip[None, :] * t
    a = m[ys, xs][:, None]
    c.rgb[ys, xs] = c.rgb[ys, xs] * (1 - a) + col * a
    c.a[ys, xs] = np.maximum(c.a[ys, xs], m[ys, xs])


def paint_ground_plants():
    c = Canvas(1024, 1024, (0.25, 0.4, 0.18), alpha=0.0, ss=2)
    rng = np.random.default_rng(311)
    dark, light = srgb('2C5E1F'), srgb('8CC043')
    # cell 0 (top-left): grass tuft
    for _ in range(34):
        blade(c, 256 + rng.normal(0, 26), 470, rng.uniform(150, 330), rng.uniform(8, 15), rng.normal(0, 0.28), dark * rng.uniform(0.8, 1.1), light * rng.uniform(0.85, 1.1))
    # cell 1 (top-right): fern
    ox = 512
    stem = [(ox + 256 + 60 * math.sin(t * 1.4) * t - 20, 480 - t * 400) for t in np.linspace(0, 1, 30)]
    c.put(c.stroke_mask(stem, 4, 0.3), srgb('4A7F2A'))
    for i, (sx, sy) in enumerate(stem[3:-1:1]):
        t = i / 26
        ln = 120 * (1 - t) ** 0.8 + 18
        for sgn in (-1, 1):
            ang = -math.pi / 2 + sgn * (1.0 - 0.35 * t)
            pts = lens(sx, sy, ln, ln * 0.26, ang)
            m = c.poly_mask(pts, 0.3)
            col = srgb('3F7B27') * (1 - t) + srgb('8CC447') * t
            c.put(m, col)
            c.put_alpha(m)
    # cell 2 (bottom-left): clover with small white flowers
    oy = 512
    for _ in range(18):
        cx, cy = 256 + rng.normal(0, 70), oy + 330 + rng.normal(0, 55)
        for k in range(3):
            a = k * math.tau / 3 + rng.uniform(0, 0.5)
            m = c.ellipse_mask(cx + math.cos(a) * 20, cy + math.sin(a) * 20, 18, 13, a)
            c.put(m, srgb('3E8A2C') * rng.uniform(0.85, 1.15)); c.put_alpha(m)
    for _ in range(7):
        cx, cy = 256 + rng.normal(0, 85), oy + 250 + rng.normal(0, 60)
        c.put(c.stroke_mask([(cx, cy + 90), (cx + 3, cy)], 3, 0.3), srgb('4A8A30')); c.put_alpha(c.stroke_mask([(cx, cy + 90), (cx + 3, cy)], 3, 0.3))
        for k in range(6):
            a = k * math.tau / 6
            m = c.ellipse_mask(cx + math.cos(a) * 11, cy + math.sin(a) * 11, 11, 6, a)
            c.put(m, srgb('FFFDF4')); c.put_alpha(m)
        m = c.ellipse_mask(cx, cy, 6, 6)
        c.put(m, srgb('F2C94C')); c.put_alpha(m)
    # cell 3 (bottom-right): tall grass with lavender spikes
    ox, oy = 512, 512
    for _ in range(22):
        blade(c, ox + 256 + rng.normal(0, 30), oy + 470, rng.uniform(220, 400), rng.uniform(7, 12), rng.normal(0, 0.22), dark * rng.uniform(0.8, 1.1), light * rng.uniform(0.9, 1.15))
    for k in range(5):
        x0 = ox + 256 + rng.normal(0, 50)
        top = oy + rng.uniform(70, 150)
        pts = [(x0 + 8 * math.sin(t * 2.0), oy + 470 - t * (oy + 470 - top)) for t in np.linspace(0, 1, 18)]
        m = c.stroke_mask(pts, 3.2, 0.3)
        c.put(m, srgb('4C8A31')); c.put_alpha(m)
        for t in np.linspace(0.62, 1.0, 9):
            px_, py_ = pts[min(int(t * 17), 17)]
            for sgn in (-1, 1):
                mm = c.ellipse_mask(px_ + sgn * 6, py_ + (1 - t) * 40, 7, 4.4, sgn * 0.9)
                c.put(mm, srgb('9C7BD6') * rng.uniform(0.9, 1.1)); c.put_alpha(mm)
    for (cx, cy) in ((0, 0), (512, 0), (0, 512), (512, 512)):      # transparent margins
        c.a[cy:cy + 28, cx:cx + 512] = 0; c.a[cy + 484:cy + 512, cx:cx + 512] = 0; c.a[cy:cy + 512, cx:cx + 28] = 0; c.a[cy:cy + 512, cx + 484:cx + 512] = 0
    return c


# ---------------------------------------------------------------------------- terrain layers (tileable)

def tiled_strokes(c, count, seed, length, width, colors, lean=0.5, vertical=True, alpha=(0.45, 0.9)):
    rng = np.random.default_rng(seed)
    S = c.w

    def draw(d, s):
        pass
    for _ in range(count):
        x, y = rng.uniform(0, S), rng.uniform(0, S)
        ang = (-math.pi / 2 if vertical else rng.uniform(0, math.tau)) + rng.normal(0, lean)
        L = rng.uniform(*length)
        col = colors[int(rng.integers(0, len(colors)))] * rng.uniform(0.88, 1.12)
        for dx in (-S, 0, S):
            for dy in (-S, 0, S):
                xx, yy = x + dx, y + dy
                if -L < xx < S + L and -L < yy < S + L:
                    pts = [(xx, yy), (xx + math.cos(ang) * L * 0.5 + rng.normal(0, 1), yy + math.sin(ang) * L * 0.5), (xx + math.cos(ang) * L, yy + math.sin(ang) * L)]
                    c.put(c.stroke_mask(pts, rng.uniform(*width), 0.2), np.clip(col, 0, 1), rng.uniform(*alpha))


def paint_route_grass(S=512):
    c = Canvas(S, S, srgb('4E7F33'), ss=2)
    broad = wrap_noise(S, S, 38, 401)
    mid = wrap_noise(S, S, 9, 402)
    c.rgb = c.rgb * (0.82 + 0.34 * broad[..., None]) * (0.95 + 0.10 * mid[..., None])
    tiled_strokes(c, 3200, 403, (5, 13), (1.3, 2.3), [srgb('2F6424'), srgb('3F7A2C'), srgb('6FA838'), srgb('8DBD45'), srgb('5C9A33')], 0.35, alpha=(0.20, 0.45))
    c.rgb = ndimage.gaussian_filter(c.rgb, (0.8, 0.8, 0), mode='wrap')
    rng = np.random.default_rng(404)
    for _ in range(26):                                            # a few wildflower specks
        x, y = rng.uniform(0, S), rng.uniform(0, S)
        col = [srgb('FFF7E0'), srgb('F2D04A'), srgb('B49AE0')][int(rng.integers(0, 3))]
        c.put(c.ellipse_mask(x, y, 2.4, 2.4, 0, 0.3), col, 0.85)
    return c


def paint_route_dirt(S=512):
    c = Canvas(S, S, srgb('9C7F57'), ss=2)
    broad = wrap_noise(S, S, 44, 411)
    fine = wrap_noise(S, S, 3, 412)
    c.rgb = c.rgb * (0.80 + 0.36 * broad[..., None]) * (0.93 + 0.14 * fine[..., None])
    rng = np.random.default_rng(413)
    for _ in range(260):                                           # pebbles with a darker underside
        x, y = rng.uniform(0, S), rng.uniform(0, S)
        r = rng.uniform(2.5, 9.0)
        tone = rng.uniform(0.78, 1.18)
        col = np.array([0.68, 0.60, 0.50], np.float32) * tone
        for dx in (-S, 0, S):
            for dy in (-S, 0, S):
                if -12 < x + dx < S + 12 and -12 < y + dy < S + 12:
                    c.put(c.ellipse_mask(x + dx + 1.2, y + dy + 1.6, r, r * 0.8, rng.uniform(0, 3), 0.4), srgb('4C3B2A'), 0.45)
                    c.put(c.ellipse_mask(x + dx, y + dy, r, r * 0.8, rng.uniform(0, 3), 0.3), col, 0.95)
    tiled_strokes(c, 300, 414, (6, 14), (1.2, 2.2), [srgb('5F7F36'), srgb('3F6A2A')], 0.9, vertical=True)   # grass fringe flecks
    return c


if __name__ == '__main__':
    which = sys.argv[1:] or ['LeafClusters', 'GroundPlants', 'RouteGrass', 'RouteDirt']
    os.makedirs(OUT, exist_ok=True)
    painters = {'LeafClusters': paint_leaf_clusters, 'GroundPlants': paint_ground_plants, 'RouteGrass': paint_route_grass, 'RouteDirt': paint_route_dirt}
    for name in which:
        cv = painters[name]()
        if name.startswith('Route'):
            cv.a[:] = 1.0
        cv.save(os.path.join(OUT, name + '.png'))
        print('painted', name, flush=True)
