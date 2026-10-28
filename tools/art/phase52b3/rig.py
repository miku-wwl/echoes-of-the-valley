"""2D cut-out skinning for illustrated characters: bones, smooth weights and a dense warp of the illustration's own pixels.

Each layer is an RGBA image of the original illustration (or a masked part of it). Bones are 2D rigid transforms with a hierarchy; every
pixel gets weights from soft capsules around the bones, so the warp is linear-blend skinning (as in Spine/Live2D), evaluated densely.
The inverse map needed for resampling is found by fixed-point iteration on the smooth displacement field. Nothing is drawn: every output
pixel is resampled from the source illustration.
"""
import math

import cv2
import numpy as np


def T(x, y):
    return np.array([[1, 0, x], [0, 1, y], [0, 0, 1]], np.float64)


def R(deg):
    c, s = math.cos(math.radians(deg)), math.sin(math.radians(deg))
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]], np.float64)


def S(sx, sy):
    return np.array([[sx, 0, 0], [0, sy, 0], [0, 0, 1]], np.float64)


class Bone:
    def __init__(self, name, pivot, parent=None):
        self.name, self.pivot, self.parent = name, np.array(pivot, np.float64), parent


class Skeleton:
    """Bones in parent-before-child order. pose = {bone: dict(dx, dy, rot, sx, sy)}; rot in degrees, positive = clockwise on screen (y down)."""

    def __init__(self, bones):
        self.bones = bones
        self.index = {b.name: i for i, b in enumerate(bones)}

    def matrices(self, pose):
        world = {}
        for b in self.bones:
            p = pose.get(b.name, {})
            local = T(*(b.pivot + (p.get('dx', 0.0), p.get('dy', 0.0)))) @ R(p.get('rot', 0.0)) @ S(p.get('sx', 1.0), p.get('sy', 1.0)) @ T(*(-b.pivot))
            world[b.name] = (world[b.parent] @ local) if b.parent else local
        return [world[b.name] for b in self.bones]


def segment_distance(h, w, a, b, step=1):
    ys, xs = np.mgrid[0:h:step, 0:w:step].astype(np.float32)
    a, b = np.array(a, np.float32), np.array(b, np.float32)
    ab = b - a
    t = np.clip(((xs - a[0]) * ab[0] + (ys - a[1]) * ab[1]) / max(float(ab @ ab), 1e-6), 0, 1)
    return np.hypot(xs - (a[0] + t * ab[0]), ys - (a[1] + t * ab[1]))


def capsule(h, w, a, b, radius, feather):
    """1 inside a capsule around segment a-b, smoothly falling to 0 over `feather` pixels."""
    d = segment_distance(h, w, a, b)
    x = np.clip((radius + feather - d) / max(feather, 1e-6), 0, 1)
    return (x * x * (3 - 2 * x)).astype(np.float32)


def band_y(h, w, y0, y1, invert=False):
    """Smooth 0->1 ramp from y0 to y1 (rows); invert gives 1->0."""
    ys = np.arange(h, dtype=np.float32)[:, None] * np.ones((1, w), np.float32)
    x = np.clip((ys - y0) / (y1 - y0), 0, 1)
    x = x * x * (3 - 2 * x)
    return (1 - x) if invert else x


def band_x(h, w, x0, x1, invert=False):
    xs = np.ones((h, 1), np.float32) * np.arange(w, dtype=np.float32)[None, :]
    x = np.clip((xs - x0) / (x1 - x0), 0, 1)
    x = x * x * (3 - 2 * x)
    return (1 - x) if invert else x


def normalise(weights, root_floor=0.02):
    """weights: list of HxW maps in skeleton order (index 0 = root). Adds a small root floor and normalises to sum 1."""
    w = np.stack(weights, 0).astype(np.float32)
    w[0] = np.maximum(w[0], root_floor)
    w /= np.maximum(w.sum(0, keepdims=True), 1e-6)
    return w


def warp(layer, weights, mats, iterations=24):
    """Linear-blend-skin the RGBA layer (H x W x 4, float premultiplied) with per-pixel weights (B x H x W) and bone matrices.
    Returns the warped premultiplied layer on the same canvas."""
    h, w = layer.shape[:2]
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    fx = np.zeros((h, w), np.float32)
    fy = np.zeros((h, w), np.float32)
    for wb, m in zip(weights, mats):
        fx += wb * (m[0, 0] * xs + m[0, 1] * ys + m[0, 2]).astype(np.float32)
        fy += wb * (m[1, 0] * xs + m[1, 1] * ys + m[1, 2]).astype(np.float32)
    dx, dy = fx - xs, fy - ys
    # inverse: find source s with s + D(s) = q, iterating s <- q - D(s)
    sx, sy = xs.copy(), ys.copy()
    for _ in range(iterations):
        ddx = cv2.remap(dx, sx, sy, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REPLICATE)
        ddy = cv2.remap(dy, sx, sy, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REPLICATE)
        sx, sy = xs - ddx, ys - ddy
    out = cv2.remap(layer, sx, sy, cv2.INTER_CUBIC, borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 0, 0, 0))
    return np.clip(out, 0, 1)


def premultiply(rgba_u8):
    f = rgba_u8.astype(np.float32) / 255.0
    f[..., :3] *= f[..., 3:4]
    return f


def over(dst, src):
    """Premultiplied 'src over dst'."""
    return src + dst * (1 - src[..., 3:4])


def to_rgba_u8(premul):
    a = premul[..., 3:4]
    rgb = np.where(a > 1e-4, premul[..., :3] / np.maximum(a, 1e-4), 0)
    return (np.clip(np.concatenate([rgb, a], -1), 0, 1) * 255 + 0.5).astype(np.uint8)


def pad(rgba, left, top, right, bottom):
    return cv2.copyMakeBorder(rgba, top, bottom, left, right, cv2.BORDER_CONSTANT, value=(0, 0, 0, 0))


def polygon_mask(h, w, points, feather=0):
    m = np.zeros((h, w), np.uint8)
    cv2.fillPoly(m, [np.array(points, np.int32)], 255)
    m = m.astype(np.float32) / 255.0
    if feather > 0:
        m = cv2.GaussianBlur(m, (0, 0), feather)
    return m
