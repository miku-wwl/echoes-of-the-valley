"""Small painting toolkit for the caretaker textures (numpy + Pillow). Everything is procedural and deterministic: shapes are drawn as
anti-aliased masks (Pillow at `ss`x supersampling), then composited with gradients and seeded noise. Coordinates are pixels with
row 0 at the TOP of the PNG (Unity v = 1)."""
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage


def srgb(hexstr):
    return np.array([int(hexstr[i:i + 2], 16) / 255.0 for i in (0, 2, 4)], np.float32)


def rgb(r, g, b):
    return np.array([r, g, b], np.float32)


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def noise(h, w, scale, seed, octaves=1, persistence=0.5):
    """Smooth seeded value noise in [0,1]; `scale` = feature size in pixels."""
    rng = np.random.default_rng(seed)
    out = np.zeros((h, w), np.float32)
    amp, total = 1.0, 0.0
    for o in range(octaves):
        s = max(scale / (2 ** o), 1.5)
        g = rng.random((int(h / s) + 4, int(w / s) + 4)).astype(np.float32)
        z = ndimage.zoom(g, (h / (g.shape[0] - 3) * 1.0, w / (g.shape[1] - 3) * 1.0), order=3)[:h, :w]
        if z.shape != (h, w):
            z = np.pad(z, ((0, h - z.shape[0]), (0, w - z.shape[1])), mode='edge')
        out += z * amp
        total += amp
        amp *= persistence
    out /= total
    return np.clip((out - out.min()) / max(out.max() - out.min(), 1e-6), 0, 1)


def tile_noise(h, w, scale, seed):
    """Noise that tiles horizontally (for textures wrapped around a body)."""
    n = noise(h, w * 2, scale, seed)
    t = np.linspace(0, 1, w, dtype=np.float32)[None, :]
    return n[:, :w] * (1 - t) + n[:, w:] * t


class Canvas:
    def __init__(self, w, h, base=(0.5, 0.5, 0.5), alpha=1.0, ss=2):
        self.w, self.h, self.ss = w, h, ss
        self.rgb = np.empty((h, w, 3), np.float32)
        self.rgb[:] = np.asarray(base, np.float32)
        self.a = np.full((h, w), alpha, np.float32)

    # ---- masks
    def mask(self, fn, blur=0.0):
        im = Image.new('L', (self.w * self.ss, self.h * self.ss), 0)
        d = ImageDraw.Draw(im)
        fn(d, self.ss)
        if blur > 0:
            im = im.filter(ImageFilter.GaussianBlur(blur * self.ss))
        im = im.resize((self.w, self.h), Image.LANCZOS)
        return np.asarray(im, np.float32) / 255.0

    def rect_mask(self, x0, y0, x1, y1, blur=0.0):
        return self.mask(lambda d, s: d.rectangle([x0 * s, y0 * s, x1 * s, y1 * s], fill=255), blur)

    def ellipse_mask(self, cx, cy, rx, ry, rot=0.0, blur=0.0):
        def draw(d, s):
            pts = []
            for k in range(72):
                t = math.tau * k / 72
                x, y = rx * math.cos(t), ry * math.sin(t)
                pts.append(((cx + x * math.cos(rot) - y * math.sin(rot)) * s, (cy + x * math.sin(rot) + y * math.cos(rot)) * s))
            d.polygon(pts, fill=255)
        return self.mask(draw, blur)

    def poly_mask(self, pts, blur=0.0):
        return self.mask(lambda d, s: d.polygon([(x * s, y * s) for x, y in pts], fill=255), blur)

    def stroke_mask(self, pts, width, blur=0.0, widths=None):
        """Polyline stroke; `widths` (per point) gives a tapered stroke drawn as overlapping discs/quads."""
        def draw(d, s):
            if widths is None:
                d.line([(x * s, y * s) for x, y in pts], fill=255, width=max(1, int(round(width * s))), joint='curve')
                r = width * s / 2
                for x, y in (pts[0], pts[-1]):
                    d.ellipse([x * s - r, y * s - r, x * s + r, y * s + r], fill=255)
            else:
                for i in range(len(pts) - 1):
                    (x0, y0), (x1, y1) = pts[i], pts[i + 1]
                    w0, w1 = widths[i], widths[i + 1]
                    nx, ny = -(y1 - y0), (x1 - x0)
                    n = math.hypot(nx, ny) or 1
                    nx, ny = nx / n, ny / n
                    d.polygon([((x0 + nx * w0 / 2) * s, (y0 + ny * w0 / 2) * s), ((x1 + nx * w1 / 2) * s, (y1 + ny * w1 / 2) * s),
                               ((x1 - nx * w1 / 2) * s, (y1 - ny * w1 / 2) * s), ((x0 - nx * w0 / 2) * s, (y0 - ny * w0 / 2) * s)], fill=255)
                    for (x, y, w) in ((x0, y0, w0), (x1, y1, w1)):
                        r = w * s / 2
                        d.ellipse([x * s - r, y * s - r, x * s + r, y * s + r], fill=255)
        return self.mask(draw, blur)

    # ---- compositing
    def put(self, m, color, alpha=1.0):
        a = (np.asarray(m, np.float32) * alpha)[..., None]
        c = np.asarray(color, np.float32)
        self.rgb = self.rgb * (1 - a) + c * a

    def put_alpha(self, m, value=1.0):
        self.a = np.maximum(self.a, np.asarray(m, np.float32) * value)

    def multiply(self, m, color, alpha=1.0):
        a = (np.asarray(m, np.float32) * alpha)[..., None]
        self.rgb = self.rgb * (1 - a + a * np.asarray(color, np.float32))

    def gradient_v(self, stops, y0=0, y1=None, x0=0, x1=None):
        """Vertical colour ramp inside a box: stops = [(t, (r,g,b)), ...]."""
        y1 = self.h if y1 is None else y1
        x1 = self.w if x1 is None else x1
        t = np.linspace(0, 1, y1 - y0, dtype=np.float32)
        ts = [s[0] for s in stops]
        cols = np.array([s[1] for s in stops], np.float32)
        ramp = np.stack([np.interp(t, ts, cols[:, k]) for k in range(3)], -1)
        self.rgb[y0:y1, x0:x1] = ramp[:, None, :]

    def save(self, path):
        out = np.concatenate([np.clip(self.rgb, 0, 1), np.clip(self.a, 0, 1)[..., None]], -1)
        Image.fromarray((out * 255 + 0.5).astype(np.uint8), 'RGBA').save(path, optimize=True)


def dashed(canvas, pts, dash, gap, width, color, alpha=1.0):
    """Stitching: dashed polyline."""
    pts = np.asarray(pts, float)
    seg = np.linalg.norm(np.diff(pts, axis=0), axis=1)
    total = seg.sum()
    cum = np.concatenate([[0], np.cumsum(seg)])
    pos = 0.0
    m = np.zeros((canvas.h, canvas.w), np.float32)
    while pos < total:
        a, b = pos, min(pos + dash, total)
        ia = np.interp([a, b], cum, pts[:, 0]), np.interp([a, b], cum, pts[:, 1])
        m = np.maximum(m, canvas.stroke_mask([(ia[0][0], ia[1][0]), (ia[0][1], ia[1][1])], width))
        pos += dash + gap
    canvas.put(m, color, alpha)


def weave(canvas, x0, y0, x1, y1, period, amount, seed, direction=0):
    """Fine fabric thread texture multiplied into a box."""
    h, w = y1 - y0, x1 - x0
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    t = 0.5 + 0.25 * np.sin(xx * math.tau / period) + 0.25 * np.sin(yy * math.tau / period + (xx * math.tau / (period * 2) if direction else 0))
    n = noise(h, w, 3.0, seed)
    f = 1 + amount * ((t - 0.5) * 1.2 + (n - 0.5) * 0.8)
    canvas.rgb[y0:y1, x0:x1] *= f[..., None]


def leaf_vine(canvas, pts, leaf_len, leaf_w, color, alpha=1.0, stem=1.6, side=1.0):
    """A small embroidered sprig: stem along `pts` with alternating pointed leaves."""
    canvas.put(canvas.stroke_mask(pts, stem), color, alpha)
    for i in range(1, len(pts) - 1):
        (x0, y0), (x1, y1) = pts[i - 1], pts[i + 1]
        ang = math.atan2(y1 - y0, x1 - x0) + side * (1 if i % 2 else -1) * 0.95
        cx, cy = pts[i]
        c2x, c2y = cx + math.cos(ang) * leaf_len * 0.5, cy + math.sin(ang) * leaf_len * 0.5
        canvas.put(canvas.ellipse_mask(c2x, c2y, leaf_len * 0.5, leaf_w * 0.5, ang), color, alpha)
