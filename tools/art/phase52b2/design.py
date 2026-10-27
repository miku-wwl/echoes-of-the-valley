"""Original caretaker model for Phase 5.2-B2: parametric surfaces (numpy), authored in Unity metres.

Original character: chestnut high ponytail with a teal ribbon, amber eyes, burgundy scarf, teal coat over a cream
dress, brown corset belt, boots and a leather satchel. Inspired by the owner's supplied (AI) design direction pack, not a
copy of any commercial character. See docs/07 and docs/03 for provenance.
"""
import math
import numpy as np
from kit import *
import atlas as A

HEAD_C = np.array([0.0, A.FACE_CENTER_Y, 0.0])
RY = 0.1155
FLAT = A.SKIN_FLAT


def g(x, s):
    return np.exp(-(np.asarray(x, float) / s) ** 2)


# Head profile knots: height relative to the head centre -> half width, front half depth, back half depth.
_PROFILE = np.array([
    # y,      hw,    hf,    hb
    [0.1155, 0.000, 0.000, 0.000],
    [0.1100, 0.032, 0.030, 0.032],
    [0.0950, 0.058, 0.056, 0.058],
    [0.0750, 0.077, 0.078, 0.078],
    [0.0500, 0.087, 0.093, 0.090],
    [0.0200, 0.091, 0.101, 0.096],
    [-0.0100, 0.090, 0.103, 0.097],
    [-0.0350, 0.085, 0.099, 0.091],
    [-0.0600, 0.073, 0.092, 0.081],
    [-0.0800, 0.058, 0.082, 0.068],
    [-0.0960, 0.040, 0.070, 0.052],
    [-0.1070, 0.021, 0.056, 0.036],
    [-0.1155, 0.000, 0.036, 0.020],
])
_DENSE = catmull(_PROFILE, 400)


def _profile(y):
    ys, order = _DENSE[:, 0][::-1], slice(None, None, -1)
    return [np.interp(y, ys, _DENSE[:, k][order]) for k in (1, 2, 3)]


def head_point(lat, lon):
    """Head surface point relative to the head centre. lon = 0 is the front (+Z). lat in [-pi/2, pi/2]."""
    lat = np.asarray(lat, float)
    lon = np.asarray(lon, float)
    y = RY * np.sin(lat)
    hw, hf, hb = _profile(y)
    s, c = np.sin(lon), np.cos(lon)
    n = 2.15
    sx = np.sign(s) * np.abs(s) ** (2 / n)
    sz = np.sign(c) * np.abs(c) ** (2 / n)
    x = hw * sx
    z = np.where(c > 0, hf, hb) * sz
    front = c > 0
    x = x * (1 + 0.035 * g(y + 0.030, 0.040))                       # soft cheeks
    z = z + np.where(front, 0.0105 * g(x, 0.0085) * g(y + 0.038, 0.016), 0)   # nose
    z = z + np.where(front, 0.0035 * g(y - 0.040, 0.022), 0)                    # brow
    z = z + np.where(front, 0.0035 * g(x, 0.012) * g(y + 0.066, 0.009), 0)     # lips
    return np.stack([x, y, z], -1)


def head_grid(nv=34, nu=48):
    lat = np.linspace(-math.pi / 2, math.pi / 2, nv)
    lon = math.pi + np.linspace(0, math.tau, nu + 1)
    LAT, LON = np.meshgrid(lat, lon, indexing='ij')
    return HEAD_C + head_point(LAT, LON)


def planar_uv(P):
    """Planar front projection into the skin sheet; the back half blends to the flat-skin patch."""
    rel = P - HEAD_C
    u = 0.5 + rel[..., 0] / A.FACE_U_SCALE
    v = 0.5 + (P[..., 1] - A.FACE_CENTER_Y) / A.FACE_V_SCALE
    w = smoothstep(0.015, -0.025, rel[..., 2])
    u = lerp(u, FLAT[0], w)
    v = lerp(v, FLAT[1], w)
    return np.stack([u, v], -1)


def build_head():
    P = head_grid()
    nv, nu = P.shape[:2]
    part = Part('Head', 'Skin', P.reshape(-1, 3), grid_faces(nv, nu), planar_uv(P).reshape(-1, 2), 'Head', outline=1.0)
    orient_closed(part)
    # Soft, spherical-ish shading normals keep the face calm under toon lighting.
    geo = vertex_normals(part.V, part.F)
    radial = normalize((part.V - HEAD_C) * np.array([1.0, 0.85, 1.0]))
    weight = np.where(part.V[:, 2] > HEAD_C[2], 0.55, 0.25)[:, None]
    part.N = normalize(geo * (1 - weight) + radial * weight)
    return part


def build_ears():
    parts = []
    for side, tag in ((1, 'R'), (-1, 'L')):
        c = HEAD_C + np.array([side * 0.0905, -0.008, -0.012])
        e = uv_sphere('Ear' + tag, 'Skin', c, (0.0065, 0.024, 0.0165), 'Head', nu=14, nv=10, color=(1.0, 0.86, 0.82, 1))
        e.UV[:] = FLAT
        e.V = (e.V - c) @ _rot_z(side * -0.30).T + c
        orient_closed(e)
        parts.append(e)
    return parts


def _rot_x(a):
    c, s = math.cos(a), math.sin(a)
    return np.array([[1, 0, 0], [0, c, -s], [0, s, c]])


def _rot_y(a):
    c, s = math.cos(a), math.sin(a)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])


def _rot_z(a):
    c, s = math.cos(a), math.sin(a)
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])


def build_neck():
    pts = np.array([[0, 1.335, -0.006], [0, 1.385, -0.002], [0, 1.435, 0.008]])
    n = tube('Neck', 'Skin', pts, 0.037, 0.034, 'Neck', nu=20, up_hint=(0, 0, 1), caps=False)
    n.UV[:] = FLAT
    return n


# ============================================================================ hair

HAIR_TINT = [(0.92, 0.86, 0.84, 1), (1.0, 0.97, 0.95, 1), (1.08, 1.04, 1.0, 1), (0.88, 0.84, 0.82, 1)]


def hairline_lat(a):
    """Hairline latitude as a function of the angle `a` (radians, 0 = front centre, pi = nape)."""
    key = np.array([0, 25, 55, 80, 100, 125, 155, 180]) * math.pi / 180
    val = np.array([0.64, 0.58, 0.34, 0.10, -0.04, -0.32, -0.58, -0.66])
    return np.interp(a, key, val)


def head_normal(lat, lon, e=1e-3):
    p1 = head_point(lat + e, lon) - head_point(lat - e, lon)
    p2 = head_point(lat, lon + e) - head_point(lat, lon - e)
    n = normalize(np.cross(p2, p1))
    ref = head_point(lat, lon)
    flip = (np.einsum('...i,...i->...', n, ref) < 0)[..., None]
    return np.where(flip, -n, n)


def hair_dir_point(lat, lon, lift):
    """Point on the head surface pushed outwards by `lift` metres along the surface normal."""
    lat = np.asarray(lat, float)
    lon = np.asarray(lon, float)
    lat, lon = np.broadcast_arrays(lat, lon)
    lift = np.asarray(lift, float)
    if lift.ndim:
        lift = lift[..., None]
    return HEAD_C + head_point(lat, lon) + head_normal(lat, lon) * lift


def build_cap():
    nr, nu = 12, 48
    lon = np.linspace(0, math.tau, nu + 1) + math.pi                 # seam at the back
    a = np.abs(((lon + math.pi) % math.tau) - math.pi)            # angle from the front, 0..pi (lon=0 is the front)
    l0 = hairline_lat(a)
    t = np.linspace(0, 1, nr)[:, None] ** 0.9                       # row 0 = crown, last = hairline
    lat = (math.pi / 2) * (1 - t) + l0[None, :] * t
    LON = np.broadcast_to(lon[None, :], lat.shape)
    lift = 0.0075 + 0.0045 * np.sin(np.clip(lat, 0, math.pi / 2)) * (1 - t) + 0.003 * t * g(a - 1.57, 1.0)
    P = hair_dir_point(lat, LON, lift)
    part = grid_part('HairCap', 'Hair', P, A.HAIR['cap'], 'HairCap', reference=lambda c: c - HEAD_C, color=(0.96, 0.92, 0.9, 1))
    return part


def head_surface(lat, lon, lift=0.0):
    return hair_dir_point(lat, lon, lift)


def flow_path(start, end, lift_end=0.0105, bulge=0.014, steps=6):
    """A path hugging the head between two (lat, lon) points, lifting off in the middle (a natural lock arc)."""
    pts = []
    for i in range(steps):
        s = i / (steps - 1)
        lat = lerp(start[0], end[0], s)
        lon = lerp(start[1], end[1], s)
        lift = lerp(0.004, lift_end, s) + bulge * math.sin(math.pi * s) ** 0.9
        pts.append(hair_dir_point(lat, lon, lift))
    return np.array(pts)


def tapered(w0, power=2.0, shoulder=0.30, blunt=0.62):
    def f(s):
        grow = 0.62 + 0.38 * math.sin(math.pi * 0.5 * min(s / shoulder, 1.0))
        return w0 * grow * max(1.0 - s ** power, 0.0) ** blunt
    return f


def build_bangs():
    parts = []
    # (start lat, start lon, end lat, end lon, width, tip lift): side-swept, layered, overlapping, uneven lengths
    specs = [
        (1.12, -0.10, 0.30, -0.46, 0.034, 0.012),
        (1.14, 0.02, 0.36, -0.18, 0.038, 0.013),
        (1.13, 0.12, 0.27, 0.08, 0.034, 0.012),
        (1.10, 0.26, 0.37, 0.55, 0.036, 0.013),
        (1.04, 0.48, 0.27, 0.86, 0.038, 0.012),
        (1.00, 0.72, 0.08, 1.08, 0.040, 0.010),
        (1.02, -0.42, 0.31, -0.84, 0.038, 0.012),
        (0.97, -0.72, 0.06, -1.12, 0.040, 0.010),
        (1.08, -0.30, 0.42, -0.30, 0.028, 0.017),
        (1.06, 0.35, 0.44, 0.40, 0.028, 0.017),
        (1.07, -0.17, 0.33, -0.31, 0.030, 0.015),
        (1.08, 0.20, 0.32, 0.36, 0.030, 0.015),
    ]
    for i, (la, lo, le, loe, w, tip) in enumerate(specs):
        path = flow_path((la, lo), (le, loe), lift_end=tip, bulge=0.010 + 0.003 * (i % 3), steps=7)
        parts.append(lock('Bang%d' % i, 'Hair', path, tapered(w * 1.32, 2.5, 0.28, 0.42), lambda s: 0.0085 * (1 - 0.5 * s), 'Bangs',
                          ref_dir=normalize(path[len(path) // 2] - HEAD_C), ns=14, nc=10, rect=A.HAIR['lock'], color=HAIR_TINT[i % 4]))
    return parts


def build_ahoge():
    """One small curled strand on the crown: a classic anime silhouette detail that breaks the round hair outline."""
    p0 = hair_dir_point(1.46, 0.0, 0.004)
    pts = np.array([p0, p0 + (0, 0.030, 0.006), p0 + (0.004, 0.058, 0.022), p0 + (0.020, 0.071, 0.046), p0 + (0.042, 0.063, 0.060), p0 + (0.052, 0.046, 0.058)])
    return [lock('Ahoge', 'Hair', pts, tapered(0.013, 2.2, 0.20, 0.7), lambda s: 0.0055, 'Bangs', ref_dir=(0, 0, 1), ns=14, nc=8, rect=A.HAIR['lock'], color=HAIR_TINT[1], outline=0.8)]


def build_ribbon():
    """Teal hair ribbon: two loops and two tails tied at the ponytail, plus a small gold pin. Vertex-coloured trinket material."""
    teal = (0.17, 0.48, 0.47, 1)
    gold = (0.97, 0.80, 0.40, 1)
    t = PT_TIE
    parts = []
    for side in (-1, 1):
        loop = np.array([t + np.array(p) for p in (
            (0, 0.002, -0.004), (side * 0.013, 0.012, -0.010), (side * 0.030, 0.016, -0.014), (side * 0.040, 0.004, -0.015),
            (side * 0.031, -0.009, -0.013), (side * 0.014, -0.006, -0.008), (0, 0.000, -0.004))])
        parts.append(lock('RibbonLoop%d' % side, 'Trinket', loop, lambda s: 0.017 * math.sin(math.pi * (0.10 + 0.8 * s)) ** 0.5, lambda s: 0.0045, 'Ponytail',
                          ref_dir=(0, 0, 1), ns=16, nc=8, n=3.0, rect=A.TRINKET['flat'], color=teal, outline=0.6))
        tail = np.array([t + np.array(p) for p in ((side * 0.003, -0.004, -0.008), (side * 0.011, -0.040, -0.022), (side * 0.020, -0.086, -0.026), (side * 0.026, -0.130, -0.018))])
        parts.append(lock('RibbonTail%d' % side, 'Trinket', tail, tapered(0.014, 4.0, 0.12, 0.30), lambda s: 0.0035, 'Ponytail',
                          ref_dir=(0, 0, 1), ns=12, nc=8, n=3.0, rect=A.TRINKET['flat'], color=teal, outline=0.6))
    knot = uv_sphere('RibbonKnot', 'Trinket', t + np.array([0, 0.002, -0.010]), (0.010, 0.010, 0.009), 'Ponytail', nu=10, nv=6, rect=A.TRINKET['flat'], color=teal, outline=0.6)
    parts.append(knot)
    for ang in (0.0, math.pi / 2):
        b = rbox('RibbonPin%d' % int(ang > 0), 'Trinket', t + np.array([0.020, 0.012, -0.004]), (0.020, 0.0055, 0.0055), 0.002, 'Ponytail', rect=A.TRINKET['flat'], color=gold, seg=3, outline=0.4)
        c0 = t + np.array([0.020, 0.012, -0.004])
        c, s_ = math.cos(ang + math.pi / 4), math.sin(ang + math.pi / 4)
        b.V = (b.V - c0) @ np.array([[c, -s_, 0], [s_, c, 0], [0, 0, 1]]).T + c0
        parts.append(b)
    return parts


def build_side_locks():
    parts = []
    for side, tag in ((1, 'R'), (-1, 'L')):
        sets = [
            dict(top=(0.75, side * 1.05), mid=(0.15, side * 1.30), free=[(0.093, -0.040, 0.040), (0.089, -0.088, 0.056), (0.080, -0.122, 0.070)], w=0.048, ref=(0.35 * side, 0, 1.0)),
            dict(top=(0.85, side * 1.50), mid=(-0.02, side * 1.66), free=[(0.098, -0.060, -0.025), (0.099, -0.120, -0.014), (0.093, -0.172, 0.004)], w=0.052, ref=(side, 0, 0.15)),
        ]
        for j, s in enumerate(sets):
            p0 = hair_dir_point(s['top'][0], s['top'][1], 0.0100)
            p1 = hair_dir_point(s['mid'][0], s['mid'][1], 0.0110)
            free = [HEAD_C + np.array([side * f[0], f[1], f[2]]) for f in s['free']]
            path = np.array([p0, (p0 + p1) / 2 + normalize(p0 - HEAD_C) * 0.004, p1] + free)
            parts.append(lock('SideLock%s%d' % (tag, j), 'Hair', path, tapered(s['w'], 1.5, 0.22), lambda t: 0.0100 * (1 - 0.5 * t), 'Side' + tag,
                              ref_dir=s['ref'], ns=18, nc=10, rect=A.HAIR['lock'], color=HAIR_TINT[(j + (0 if side > 0 else 1)) % 4]))
    return parts


PT_TIE = HEAD_C + np.array([0.0, 0.056, -0.092])


def build_ponytail():
    parts = []
    specs = [  # (lateral factor, length, width, thickness, wave amplitude, wave phase, tint)
        (0.0, 0.50, 0.070, 0.030, 0.014, 0.0, 1),
        (-0.55, 0.43, 0.058, 0.025, 0.018, 0.9, 0),
        (0.55, 0.44, 0.058, 0.025, 0.018, 2.1, 2),
        (-1.05, 0.37, 0.050, 0.022, 0.020, 1.7, 3),
        (1.05, 0.38, 0.050, 0.022, 0.020, 2.9, 1),
        (-1.45, 0.31, 0.044, 0.020, 0.014, 0.4, 2),
        (1.45, 0.32, 0.044, 0.020, 0.014, 1.3, 0),
        (-0.25, 0.47, 0.046, 0.020, 0.022, 3.4, 3),
        (0.30, 0.48, 0.046, 0.020, 0.022, 4.1, 2),
    ]
    for i, (lat, length, w, th, amp, ph, tint) in enumerate(specs):
        pts = []
        for k in range(11):
            s = k / 10
            x = lat * 0.026 * (0.35 + 1.9 * s) + amp * math.sin(math.tau * (s * 1.25) + ph) * s ** 1.1
            y = PT_TIE[1] + 0.022 * math.sin(math.pi * min(s / 0.28, 1.0)) - length * s ** 1.12
            z = PT_TIE[2] - 0.014 - 0.100 * float(smoothstep(0.0, 0.42, s)) + 0.040 * float(smoothstep(0.45, 1.0, s)) * (length / 0.45) - 0.012 * abs(lat) * s
            pts.append((x, y, z))
        side_on = i in (0, 3, 4, 7)          # some locks show their wide face to the side so the ponytail has body from every angle
        parts.append(lock('Ponytail%d' % i, 'Hair', np.array(pts), tapered(w * (0.8 if side_on else 1.0), 1.7, 0.18), lambda s_, th=th, so=side_on: th * (1.5 if so else 1.0) * (1 - 0.5 * s_), 'Ponytail',
                          ref_dir=(1, 0, 0) if side_on else (0, 0, -1), ns=24, nc=10, rect=A.HAIR['lock'], color=HAIR_TINT[tint]))
    for j, (lat, length, amp, ph) in enumerate(((0.0, 0.40, 0.010, 0.7), (-0.35, 0.36, 0.012, 2.6), (0.35, 0.37, 0.012, 3.6))):   # round core locks fill the middle
        pts = []
        for k in range(11):
            s = k / 10
            x = lat * 0.020 * (0.4 + 1.5 * s) + amp * math.sin(math.tau * (s * 1.1) + ph) * s
            y = PT_TIE[1] + 0.012 * math.sin(math.pi * min(s / 0.28, 1.0)) - length * s ** 1.1
            z = PT_TIE[2] - 0.010 - 0.075 * float(smoothstep(0.0, 0.42, s)) + 0.03 * float(smoothstep(0.45, 1.0, s))
            pts.append((x, y, z))
        parts.append(lock('PonytailCore%d' % j, 'Hair', np.array(pts), tapered(0.046, 1.6, 0.22, 0.6), lambda s_: 0.040 * (1 - 0.35 * s_), 'Ponytail',
                          ref_dir=(0, 0, -1), ns=22, nc=10, n=2.2, rect=A.HAIR['lock'], color=HAIR_TINT[(j + 1) % 4]))
    return parts


HEAD_SCALE = 1.08
HEAD_PIVOT = np.array([0.0, 1.42, 0.0])


def scale_head(parts):
    """Slightly larger head (and hair) about the chin/neck joint: friendlier proportions and a readable face at game distance."""
    for p in parts:
        if p.zone in ('Head', 'HairCap', 'Bangs', 'Ponytail') or p.zone.startswith('Side'):
            p.V = HEAD_PIVOT + (p.V - HEAD_PIVOT) * HEAD_SCALE
            if p.N is not None:
                p.N = p.N.copy()
    return parts


def build_head_parts():
    parts = [build_head()] + build_ears() + [build_neck()] + [build_cap()] + build_bangs() + build_side_locks() + build_ponytail() + build_ribbon() + build_ahoge()
    return scale_head(parts)


def build_model():
    import design_body
    return build_head_parts() + design_body.build_body_parts()
