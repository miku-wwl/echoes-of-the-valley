"""Garment, leather, scarf and trinket painters for the caretaker (see textures.py). Procedural, deterministic, original."""
import math
import numpy as np
import atlas as A
from paint import *

GOLD, GOLD_DARK = srgb('D6AE55'), srgb('8F6B2C')
STITCH = srgb('E1C08A')


def px(rect, w, h):
    u0, v0, u1, v1 = rect
    x0, x1 = sorted((int(round(u0 * w)), int(round(u1 * w))))
    y0, y1 = sorted((int(round((1 - v0) * h)), int(round((1 - v1) * h))))
    return x0, y0, min(x1, w), min(y1, h)


def vine_band(c, x0, x1, y, amp, period, color, leaf=14, alpha=1.0, flip=False, stem=1.8):
    """A repeating embroidered vine: wavy stem, alternating pointed leaves, small berries."""
    xs = np.linspace(x0, x1, int((x1 - x0) / 3) + 2)
    ys = y + amp * np.sin((xs - x0) * math.tau / period)
    c.put(c.stroke_mask(list(zip(xs, ys)), stem), color, alpha)
    k = 0
    for xc in np.arange(x0 + period * 0.25, x1, period * 0.5):
        yc = y + amp * math.sin((xc - x0) * math.tau / period)
        sgn = (1 if k % 2 == 0 else -1) * (-1 if flip else 1)
        ang = math.atan2(-sgn * 1.0, 0.55)
        cx, cy = xc + math.cos(ang) * leaf * 0.5, yc + math.sin(ang) * leaf * 0.5
        c.put(c.ellipse_mask(cx, cy, leaf * 0.5, leaf * 0.22, ang), color, alpha)
        if k % 2 == 0:
            c.put(c.ellipse_mask(xc + 4, yc + sgn * 7, 2.2, 2.2), color, alpha)
        k += 1


def twill(c, box, amount, seed, period=4.0):
    weave(c, box[0], box[1], box[2], box[3], period, amount, seed, 1)


def painterly(c, box, amount, seed, scale=70):
    x0, y0, x1, y1 = box
    n = noise(y1 - y0, x1 - x0, scale, seed, 3)
    c.rgb[y0:y1, x0:x1] *= (1 + amount * (n - 0.5) * 2)[..., None]


# =============================================================================== teal coat

def coat_back_details(c, box):
    """The back of the coat is what the third-person camera sees most: princess seams, a yoke seam, buttons and a gold emblem."""
    x0, y0, x1, y1 = box
    w, h = x1 - x0, y1 - y0
    cx = x0 + w // 2
    seam, seam_hi = srgb('143F40'), srgb('3B8A80')
    for sgn in (-1, 1):                                      # princess seams: shoulder -> waist, pulled in at the waist
        pts = [(cx + sgn * (0.20 * w - 0.045 * w * math.sin(math.pi * t) * 1.0), y0 + 34 + t * (0.60 * h - 34)) for t in np.linspace(0, 1, 26)]
        c.put(c.stroke_mask(pts, 3.0), seam, 0.85)
        c.put(c.stroke_mask([(a + sgn * 3, b) for a, b in pts], 1.3), seam_hi, 0.5)
    c.put(c.stroke_mask([(cx - 0.34 * w, y0 + 70), (cx, y0 + 62), (cx + 0.34 * w, y0 + 70)], 2.6), seam, 0.8)   # yoke seam
    ey = y0 + 150                                              # emblem between the shoulder blades: a leaf with three echo arcs
    c.put(c.ellipse_mask(cx, ey + 6, 40, 17, -1.15), GOLD)
    c.put(c.stroke_mask([(cx - 27, ey + 35), (cx, ey + 6), (cx + 25, ey - 24)], 2.6), srgb('1C4B4D'), 0.9)
    for r in (54, 72, 90):
        c.put(c.stroke_mask([(cx + r * math.cos(t), ey + 6 - r * math.sin(t) * 0.8) for t in np.linspace(-0.25, 1.25, 24)], 2.8), GOLD, 0.62)
    for k in range(3):                                         # three small gold buttons down the spine to the waist seam
        yy_ = y0 + 228 + k * 34
        c.put(c.ellipse_mask(cx, yy_, 8.5, 8.5), GOLD)
        c.put(c.ellipse_mask(cx, yy_, 5.0, 5.0), GOLD_DARK, 0.9)
    painterly(c, (x0, y0, x1, y1), 0.0, 1)



def paint_teal(S=1024):
    c = Canvas(S, S, srgb('275D5C'), ss=2)
    x0, y0, x1, y1 = px(A.TEAL['coat'], S, S)
    w, h = x1 - x0, y1 - y0
    c.gradient_v([(0, srgb('2F746D')), (0.35, srgb('29665F')), (0.75, srgb('215A58')), (1, srgb('1B4B4D'))], y0, y1, x0, x1)
    painterly(c, (x0, y0, x1, y1), 0.07, 71)
    twill(c, (x0, y0, x1, y1), 0.050, 72)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    flare = smooth(0.30, 0.95, yy / h)
    c.rgb[y0:y1, x0:x1] *= (1 - 0.10 * flare * (0.5 - 0.5 * np.cos(xx / w * math.tau * 7 + 0.6)))[..., None]
    for xa, xb in ((x0, x0 + 15), (x1 - 15, x1)):
        c.put(c.rect_mask(xa, y0, xb, y1), GOLD)
    for xa in (x0 + 19, x1 - 20):
        c.put(c.rect_mask(xa, y0, xa + 2, y1), GOLD_DARK, 0.8)
    c.put(c.rect_mask(x0, y0, x1, y0 + 12), GOLD)
    for ly, lw in ((y1 - 12, 7), (y1 - 28, 3), (y1 - 38, 2)):
        c.put(c.rect_mask(x0, ly, x1, ly + lw), GOLD)
    vine_band(c, x0 + 40, x1 - 40, y1 - 78, 11, 96, GOLD, leaf=17)
    vine_band(c, x0 + 40, x1 - 40, y1 - 125, 7, 128, GOLD_DARK, leaf=13, alpha=0.8, flip=True, stem=1.4)
    for xc in np.arange(x0 + 90, x1 - 60, 190):
        leaf_vine(c, [(xc + 6 * math.sin(t), y1 - 270 + t * 14) for t in range(0, 8)], 15, 6, GOLD, 0.65)
    coat_back_details(c, (x0, y0, x1, y1))
    sx0, sy0, sx1, sy1 = px(A.TEAL['sleeve'], S, S)
    c.gradient_v([(0, srgb('2E736C')), (0.5, srgb('27645F')), (1, srgb('1F5655'))], sy0, sy1, sx0, sx1)
    painterly(c, (sx0, sy0, sx1, sy1), 0.07, 73)
    twill(c, (sx0, sy0, sx1, sy1), 0.05, 74)
    for ly, lw in ((sy1 - 14, 8), (sy1 - 32, 3), (sy1 - 44, 2), (sy1 - 140, 3)):
        c.put(c.rect_mask(sx0, ly, sx1, ly + lw), GOLD)
    vine_band(c, sx0 + 18, sx1 - 18, sy1 - 92, 9, 88, GOLD, leaf=15)
    lx0, ly0, lx1, ly1 = px(A.TEAL['lining'], S, S)
    c.rgb[ly0:ly1, lx0:lx1] = srgb('E8E2C6')
    painterly(c, (lx0, ly0, lx1, ly1), 0.05, 75, 40)
    yy2, xx2 = np.mgrid[0:ly1 - ly0, 0:lx1 - lx0].astype(np.float32)
    c.rgb[ly0:ly1, lx0:lx1] *= (1 - 0.05 * (0.5 + 0.5 * np.sin((xx2 + yy2) * 0.18)))[..., None]
    tx0, ty0, tx1, ty1 = px(A.TEAL['trim'], S, S)
    c.rgb[ty0:ty1, tx0:tx1] = srgb('245A59')
    ex, ey = int(A.EDGE['Teal'][0] * S), int((1 - A.EDGE['Teal'][1]) * S)
    c.rgb[ey - 20:ey + 20, ex - 20:ex + 20] = GOLD
    return c


# =============================================================================== cream dress

def paint_cream(S=1024):
    c = Canvas(S, S, srgb('F1E1C0'), ss=2)
    cream_a, cream_b = srgb('F6E8C8'), srgb('E3CFA6')
    x0, y0, x1, y1 = px(A.CREAM['blouse'], S, S)
    w, h = x1 - x0, y1 - y0
    c.gradient_v([(0, cream_a), (1, cream_b)], y0, y1, x0, x1)
    painterly(c, (x0, y0, x1, y1), 0.04, 81)
    twill(c, (x0, y0, x1, y1), 0.04, 82, 3.5)
    cx = x0 + w // 2
    c.put(c.poly_mask([(cx - 70, y0 - 2), (cx, y0 + 215), (cx + 70, y0 - 2)], 0.6), srgb('F7C5A8'))
    c.put(c.poly_mask([(cx - 62, y0), (cx, y0 + 196), (cx + 62, y0)], 6), srgb('F2B596'), 0.45)
    for side in (-1, 1):
        pts = [(cx + side * (70 - 70 * t), y0 + 215 * t) for t in np.linspace(0, 1, 18)]
        c.put(c.stroke_mask(pts, 6), srgb('FFFBF0'))
        for (qx, qy) in pts[::2]:
            c.put(c.ellipse_mask(qx + side * 5, qy - 1, 5, 5), srgb('FFFBF0'))
        c.put(c.stroke_mask([(a + side * 9, b) for a, b in pts], 1.4), srgb('B99768'), 0.55)
    c.put(c.ellipse_mask(cx, y0 + 226, 11, 11), GOLD)
    c.put(c.ellipse_mask(cx, y0 + 226, 6.5, 6.5), srgb('3FA7A0'))
    c.put(c.ellipse_mask(cx - 2, y0 + 224, 2.2, 2.2), srgb('E8FFFB'), 0.9)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    c.rgb[y0:y1, x0:x1] *= (1 - 0.05 * (0.5 - 0.5 * np.cos(xx / w * math.tau * 14)))[..., None]
    x0, y0, x1, y1 = px(A.CREAM['skirt'], S, S)
    w, h = x1 - x0, y1 - y0
    c.gradient_v([(0, srgb('EFDDB8')), (0.7, srgb('F3E4C2')), (1, srgb('EBD8B0'))], y0, y1, x0, x1)
    painterly(c, (x0, y0, x1, y1), 0.05, 83)
    twill(c, (x0, y0, x1, y1), 0.04, 84, 3.5)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    pleat = 0.5 - 0.5 * np.cos(xx / w * math.tau * 9 + 0.5)
    c.rgb[y0:y1, x0:x1] *= (1 - 0.17 * pleat * smooth(0.05, 0.9, yy / h))[..., None]
    thread, thread2 = srgb('A8793A'), srgb('C99B52')
    for ly, lw, col in ((y1 - 10, 5, thread), (y1 - 26, 2, thread2), (y1 - 70, 2, thread2), (y1 - 82, 4, thread)):
        c.put(c.rect_mask(x0, ly, x1, ly + lw), col)
    vine_band(c, x0 + 6, x1 - 6, y1 - 48, 9, 64, thread, leaf=14, stem=1.6)
    for xc in np.arange(x0 + 40, x1, 113):
        leaf_vine(c, [(xc + 5 * math.sin(t * 0.9), y1 - 210 + t * 14) for t in range(0, 8)], 17, 7, thread2, 0.75)
        c.put(c.ellipse_mask(xc, y1 - 215, 3.4, 3.4), thread, 0.8)
    for key in ('skirt_in', 'frill'):
        lx0, ly0, lx1, ly1 = px(A.CREAM[key], S, S)
        c.rgb[ly0:ly1, lx0:lx1] = srgb('EEDFC0')
        painterly(c, (lx0, ly0, lx1, ly1), 0.04, 85 + len(key), 50)
    ex, ey = int(A.EDGE['Cream'][0] * S), int((1 - A.EDGE['Cream'][1]) * S)
    c.rgb[ey - 20:ey + 20, ex - 20:ex + 20] = srgb('E4C98C')
    return c


# =============================================================================== burgundy scarf and petticoat

def paint_burgundy(S=512):
    c = Canvas(S, S, srgb('7C2038'), ss=2)
    x0, y0, x1, y1 = px(A.BURGUNDY['collar'], S, S)
    c.gradient_v([(0, srgb('8A2842')), (1, srgb('6C1B31'))], y0, y1, x0, x1)
    painterly(c, (x0, y0, x1, y1), 0.06, 91, 30)
    yy = np.arange(y1 - y0, dtype=np.float32)[:, None]
    c.rgb[y0:y1, x0:x1] *= (1 + 0.07 * np.sin(yy * math.tau / 7.0))[..., None]
    c.put(c.rect_mask(x0, y0 + 6, x1, y0 + 9), srgb('D8B77A'), 0.85)
    c.put(c.rect_mask(x0, y1 - 10, x1, y1 - 7), srgb('D8B77A'), 0.85)
    tx0, ty0, tx1, ty1 = px(A.BURGUNDY['tail'], S, S)
    tw, th = tx1 - tx0, ty1 - ty0
    c.gradient_v([(0, srgb('8C2A45')), (1, srgb('73203A'))], ty0, ty1, tx0, tx1)
    painterly(c, (tx0, ty0, tx1, ty1), 0.06, 92, 28)
    yy = np.arange(th, dtype=np.float32)[:, None]
    c.rgb[ty0:ty1, tx0:tx1] *= (1 + 0.05 * np.sin(yy * math.tau / 6.0))[..., None]
    for xa in (tx0 + int(tw * 0.07), tx1 - int(tw * 0.07) - 6):
        c.put(c.rect_mask(xa, ty0, xa + 6, ty1), srgb('E6CFA0'), 0.92)
        c.put(c.rect_mask(xa + 9, ty0, xa + 11, ty1), srgb('D1A85E'), 0.9)
    body_end = ty1 - 48
    for yc in np.arange(ty0 + 30, body_end - 14, 44):
        cx = tx0 + tw // 2
        c.put(c.poly_mask([(cx, yc - 14), (cx + 12, yc), (cx, yc + 14), (cx - 12, yc)]), srgb('E8D2A2'), 0.95)
        c.put(c.poly_mask([(cx, yc - 7), (cx + 6, yc), (cx, yc + 7), (cx - 6, yc)]), srgb('8C2A45'))
    c.put(c.rect_mask(tx0 + 14, body_end - 5, tx1 - 14, body_end), srgb('D1A85E'), 0.9)
    c.a[body_end:ty1, tx0:tx1] = 0
    rng = np.random.default_rng(93)
    x = tx0 + 3
    while x < tx1 - 3:
        wth = int(rng.integers(5, 8))
        ln = int(rng.integers(34, 46))
        c.a[body_end:body_end + ln, x:x + wth] = 1
        c.rgb[body_end:body_end + ln, x:x + wth] = srgb('8E2B46') * (1.0 + 0.15 * np.linspace(0, 1, ln)[:, None, None])
        x += wth + int(rng.integers(2, 4))
    px0, py0, px1, py1 = px(A.BURGUNDY['petticoat'], S, S)
    c.gradient_v([(0, srgb('6B1B30')), (1, srgb('8A2842'))], py0, py1, px0, px1)
    painterly(c, (px0, py0, px1, py1), 0.06, 94, 30)
    ex, ey = int(A.EDGE['Burgundy'][0] * S), int((1 - A.EDGE['Burgundy'][1]) * S)
    c.rgb[ey - 10:ey + 10, ex - 10:ex + 10] = srgb('5B1528')
    c.a[ey - 10:ey + 10, ex - 10:ex + 10] = 1
    return c


# =============================================================================== leather

def leather_base(c, box, a, b, seed, grain=0.07):
    x0, y0, x1, y1 = box
    c.gradient_v([(0, a), (1, b)], y0, y1, x0, x1)
    n = noise(y1 - y0, x1 - x0, 5, seed, 2)
    big = noise(y1 - y0, x1 - x0, 80, seed + 1, 2)
    c.rgb[y0:y1, x0:x1] *= (1 + grain * (n - 0.5) * 2 + 0.10 * (big - 0.5))[..., None]


def stitched_frame(c, box, insets=(16, 24)):
    x0, y0, x1, y1 = box
    for inset in insets:
        pts = [(x0 + inset, y0 + inset), (x1 - inset, y0 + inset), (x1 - inset, y1 - inset), (x0 + inset, y1 - inset), (x0 + inset, y0 + inset)]
        dashed(c, pts, 10, 7, 2.2, STITCH, 0.9 if inset == insets[0] else 0.5)


def paint_leather(S=1024):
    c = Canvas(S, S, srgb('5B3A26'), ss=2)
    x0, y0, x1, y1 = px(A.LEATHER['corset'], S, S)
    w, h = x1 - x0, y1 - y0
    leather_base(c, (x0, y0, x1, y1), srgb('6A4430'), srgb('4C3020'), 101)
    cx = x0 + w // 2
    dashed(c, [(x0, y0 + 9), (x1, y0 + 9)], 9, 6, 2.2, STITCH, 0.9)
    dashed(c, [(x0, y1 - 9), (x1, y1 - 9)], 9, 6, 2.2, STITCH, 0.9)
    for xb in np.arange(x0 + 28, x1, 47):
        c.put(c.rect_mask(xb, y0 + 14, xb + 2, y1 - 14), srgb('3A2417'), 0.55)
    gap = 18
    for k in range(6):
        yy_ = y0 + 22 + k * 32
        c.put(c.stroke_mask([(cx - gap, yy_), (cx + gap, yy_ + 28)], 3.6), srgb('F1E1BC'))
        c.put(c.stroke_mask([(cx + gap, yy_), (cx - gap, yy_ + 28)], 3.6), srgb('F1E1BC'))
        for sx in (-gap, gap):
            c.put(c.ellipse_mask(cx + sx, yy_, 6.5, 6.5), srgb('C8A45A'))
            c.put(c.ellipse_mask(cx + sx, yy_, 3.2, 3.2), srgb('2A180F'))
    for key, base, seed in (('strap', srgb('6B4630'), 111), ('strap2', srgb('5E3D2A'), 112)):
        x0, y0, x1, y1 = px(A.LEATHER[key], S, S)
        leather_base(c, (x0, y0, x1, y1), base, base * 0.85, seed)
        for u in (0.10, 0.40, 0.60, 0.90):
            dashed(c, [(x0 + (x1 - x0) * u, y0 + 2), (x0 + (x1 - x0) * u, y1 - 2)], 7, 5, 2.0, STITCH, 0.85)
        for yh in np.arange(y0 + 14, y1 - 8, 26):
            c.put(c.ellipse_mask(x0 + (x1 - x0) * 0.25, yh, 3.2, 3.2), srgb('2A180F'), 0.8)
    x0, y0, x1, y1 = px(A.LEATHER['boot'], S, S)
    w, h = x1 - x0, y1 - y0
    leather_base(c, (x0, y0, x1, y1), srgb('5A3825'), srgb('3F2619'), 121, 0.10)
    cx = x0 + w // 2
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    c.rgb[y0:y1, x0:x1] *= (1 + 0.10 * np.exp(-((yy / h - 0.55) / 0.10) ** 2) * (0.5 - 0.5 * np.cos(xx / w * math.tau * 7)))[..., None]
    for k in range(7):
        yy_ = y0 + 24 + k * 22
        c.put(c.stroke_mask([(cx - 16, yy_), (cx + 16, yy_ + 20)], 3.2), srgb('EBD9B2'))
        c.put(c.stroke_mask([(cx + 16, yy_), (cx - 16, yy_ + 20)], 3.2), srgb('EBD9B2'))
        for sx in (-16, 16):
            c.put(c.ellipse_mask(cx + sx, yy_, 5, 5), srgb('B89350'))
            c.put(c.ellipse_mask(cx + sx, yy_, 2.4, 2.4), srgb('241309'))
    dashed(c, [(x0, y0 + h * 0.62), (x1, y0 + h * 0.62)], 8, 6, 2.0, STITCH, 0.8)
    x0, y0, x1, y1 = px(A.LEATHER['cuff'], S, S)
    leather_base(c, (x0, y0, x1, y1), srgb('7A5236'), srgb('5C3B28'), 131)
    for ly in (y0 + 10, y1 - 10):
        dashed(c, [(x0, ly), (x1, ly)], 9, 6, 2.2, STITCH, 0.9)
    x0, y0, x1, y1 = px(A.LEATHER['sole'], S, S)
    c.rgb[y0:y1, x0:x1] = srgb('2B1D16')
    c.put(c.rect_mask(x0, y0 + (y1 - y0) // 2 - 3, x1, y0 + (y1 - y0) // 2 + 3), srgb('9A6C45'), 0.9)
    x0, y0, x1, y1 = px(A.LEATHER['bag'], S, S)
    leather_base(c, (x0, y0, x1, y1), srgb('73492F'), srgb('55341F'), 141, 0.09)
    stitched_frame(c, (x0, y0, x1, y1))
    x0, y0, x1, y1 = px(A.LEATHER['flap'], S, S)
    leather_base(c, (x0, y0, x1, y1), srgb('7C4F32'), srgb('5C3822'), 151, 0.09)
    stitched_frame(c, (x0, y0, x1, y1))
    ecx, ecy = (x0 + x1) // 2, (y0 + y1) // 2
    c.put(c.ellipse_mask(ecx, ecy + 8, 38, 17, -1.15), srgb('D9B66A'), 0.9)
    c.put(c.stroke_mask([(ecx - 26, ecy + 34), (ecx, ecy + 8), (ecx + 24, ecy - 22)], 2.4), srgb('5C3822'), 0.9)
    for r in (62, 82, 102):
        pts = [(ecx + r * math.cos(t), ecy + 8 - r * math.sin(t) * 0.8) for t in np.linspace(-0.2, 1.1, 20)]
        c.put(c.stroke_mask(pts, 2.6), srgb('D9B66A'), 0.55)
    ex, ey = int(A.EDGE['Leather'][0] * S), int((1 - A.EDGE['Leather'][1]) * S)
    c.rgb[ey - 20:ey + 20, ex - 20:ex + 20] = srgb('2F1C12')
    return c


# =============================================================================== tights, trinkets

def paint_tights(w=256, h=512):
    c = Canvas(w, h, srgb('4B342E'), ss=2)
    c.gradient_v([(0, srgb('553A32')), (1, srgb('3F2B27'))])
    yy = np.arange(h, dtype=np.float32)[:, None]
    c.rgb *= (1 + 0.05 * np.sin(yy * math.tau / 4.0))[..., None]
    c.rgb *= (1 + 0.06 * (noise(h, w, 6, 161) - 0.5))[..., None]
    cx = w // 2
    fil = srgb('9C7048')
    y0, y1 = int(h * 0.30), int(h * 0.64)
    c.put(c.stroke_mask([(cx + 7 * math.sin((y - y0) * 0.11), y) for y in range(y0, y1, 3)], 2.0), fil, 0.75)
    for k, y in enumerate(range(y0 + 8, y1 - 8, 20)):
        sgn = 1 if k % 2 else -1
        c.put(c.ellipse_mask(cx + sgn * 12, y, 9, 3.6, sgn * -0.7), fil, 0.7)
    return c


def paint_trinket(w=128, h=128):
    c = Canvas(w, h, (0.96, 0.96, 0.96), ss=1)
    c.rgb *= (1 + 0.02 * (noise(h, w, 8, 171) - 0.5))[..., None]
    return c
