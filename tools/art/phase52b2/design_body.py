"""Body, clothing and accessories of the caretaker (Unity metres, +Z forward). See design.py for the head and hair."""
import math
import numpy as np
from kit import *
import atlas as A

GOLD = (0.97, 0.80, 0.40, 1)
TEAL_TINT = (0.30, 0.62, 0.60, 1)
BURG_TINT = (0.62, 0.20, 0.30, 1)
GLASS = (0.55, 0.92, 0.88, 1)
DARK = (0.30, 0.22, 0.18, 1)


def g(x, s):
    return np.exp(-(np.asarray(x, float) / s) ** 2)


def rot_x(a):
    c, s = math.cos(a), math.sin(a)
    return np.array([[1, 0, 0], [0, c, -s], [0, s, c]])


def rot_y(a):
    c, s = math.cos(a), math.sin(a)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])


def rot_z(a):
    c, s = math.cos(a), math.sin(a)
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])


def rotate_about(part, matrix, center):
    c = np.asarray(center, float)
    part.V = (part.V - c) @ np.asarray(matrix).T + c
    if part.N is not None:
        part.N = normalize(part.N @ np.asarray(matrix).T)
    return part


# ============================================================================ torso

# top -> bottom: (y, cx, cz, rx, rz, superellipse exponent)
TORSO_KEYS = [
    (1.404, 0, 0.000, .043, .041, 2.0),
    (1.385, 0, 0.000, .058, .050, 2.0),
    (1.355, 0, -0.002, .098, .060, 2.2),
    (1.325, 0, 0.000, .138, .074, 2.7),
    (1.270, 0, 0.002, .134, .086, 2.4),
    (1.190, 0, 0.004, .128, .094, 2.2),
    (1.100, 0, 0.002, .112, .081, 2.1),
    (1.020, 0, 0.000, .110, .078, 2.1),
    (0.930, 0, 0.000, .138, .095, 2.3),
    (0.840, 0, 0.000, .152, .104, 2.3),
]
_TK = np.array(sorted(TORSO_KEYS, key=lambda k: k[0]))


def body_r(y):
    y = np.asarray(y, float)
    return (np.interp(y, _TK[:, 0], _TK[:, 2]), np.interp(y, _TK[:, 0], _TK[:, 3]), np.interp(y, _TK[:, 0], _TK[:, 4]))


def build_torso():
    P = loft_rows(TORSO_KEYS, 26, 44).copy()
    x, y, z = P[..., 0], P[..., 1], P[..., 2]
    bump = 0.013 * g(np.abs(x) - 0.050, 0.030) * g(y - 1.195, 0.040)
    P[..., 2] = z + np.where(z > 0, bump, 0)
    return grid_part('Blouse', 'Cream', P, A.CREAM['blouse'], 'Torso', reference=axis_reference(0, 0))


# ============================================================================ coat

SKIRT_TOP, SKIRT_REF_HEM = 1.03, 0.600
COAT_CLEARANCE = 0.020      # coat centre surface to skirt centre surface: both shells' thickness plus a safety margin


def skirt_radii(y, a):
    """Outer-skirt half-widths (x, z) at height y and angle a, pleats included. Shared by the skirt and the coat so the coat can clear it."""
    f = np.clip((SKIRT_TOP - y) / (SKIRT_TOP - SKIRT_REF_HEM), 0, 1.1)         # frozen just below the reference hem: the coat tails do not keep widening
    fp = np.clip(f, 0, 1)
    pleat = 1 + 0.045 * fp ** 1.1 * np.sin(9 * a + 0.5) + 0.022 * fp * np.sin(15 * a + 1.1)
    return lerp(0.114, 0.262, f ** 1.3) * pleat, lerp(0.086, 0.246, f ** 1.3) * pleat


def coat_grid(nv=26, nu=56):
    u = np.linspace(0, 1, nu + 1)[None, :]
    t = np.linspace(0, 1, nv)[:, None]
    a_open = lerp(0.34, 0.66, t ** 0.8)
    alpha = a_open + u * (math.tau - 2 * a_open)               # 0 = front, + = character's right, pi = back
    sym = np.abs(((alpha + math.pi) % math.tau) - math.pi)
    hem = lerp(0.73, 0.41, smoothstep(0.6, 2.7, sym)) + 0.016 * np.sin(5 * alpha + 0.7) - 0.030 * np.sin(alpha)
    y = 1.372 + (hem - 1.372) * t
    cz, rx, rz = body_r(y)
    d = np.clip(1.0 - y, 0, None)
    back = smoothstep(0.0, -0.7, np.cos(alpha))
    rx = rx + 0.016 + d * (0.13 + 0.07 * np.sin(alpha) ** 2)
    rz = rz + 0.016 + d * (0.076 + 0.13 * back)
    pleat = 1 + 0.050 * smoothstep(0.97, 0.50, y) * np.sin(7 * alpha + 0.6) + 0.028 * smoothstep(0.97, 0.45, y) * np.sin(11 * alpha + 2.1)
    # the coat always stands outside the skirt (including both pleat patterns), so the cream skirt can never show through the coat
    sx, sz = skirt_radii(y, alpha)
    w = smoothstep(1.02, 0.95, y)
    rx = np.maximum(rx, w * (sx + COAT_CLEARANCE) / pleat)
    rz = np.maximum(rz, w * (sz + COAT_CLEARANCE) / pleat)
    x = rx * pleat * np.sin(alpha)
    z = cz + rz * pleat * np.cos(alpha)
    # bust relief, so the coat does not cut through the blouse shaping
    z = z + np.where(np.cos(alpha) > 0, 0.012 * g(np.abs(x) - 0.052, 0.034) * g(y - 1.195, 0.045), 0)
    return np.stack([x, y, z], -1)


def build_coat():
    P = coat_grid()
    return thick_sheet('Coat', 'Teal', P, 0.006, 'Coat', A.TEAL['coat'], A.TEAL['lining'], reference=axis_reference(0, 0), edge_uv=A.EDGE['Teal'])


SLEEVE_PATH = np.array([
    [0.100, 1.346, 0.000], [0.150, 1.324, 0.000], [0.203, 1.200, 0.004], [0.250, 1.045, 0.010], [0.276, 0.945, 0.026], [0.292, 0.865, 0.036]])
SLEEVE_R = np.array([0.026, 0.049, 0.046, 0.050, 0.068, 0.088])


def build_sleeve():
    pts = catmull(SLEEVE_PATH, 12)
    r = np.interp(np.linspace(0, 1, 12), np.linspace(0, 1, len(SLEEVE_R)), SLEEVE_R)
    P = tube_grid(pts, r, r * 0.96, nu=22, n=2.0, up_hint=(0, 0, 1))
    parts = thick_sheet('SleeveR', 'Teal', P, 0.006, 'ArmR', A.TEAL['sleeve'], A.TEAL['lining'], reference=path_reference(pts), closed_u=True, edge_uv=A.EDGE['Teal'])
    return parts


# ============================================================================ skirt

def skirt_grid(kind):
    nv, nu = 12, 56
    th = math.pi + np.linspace(0, math.tau, nu + 1)[None, :]
    a = th - math.tau
    t = np.linspace(0, 1, nv)[:, None]
    ref_hem = 0.600
    if kind == 'outer':
        hem = lerp(0.640, 0.575, smoothstep(0.4, 2.6, np.abs(a))) + 0.014 * np.sin(7 * a + 0.3) - 0.040 * np.sin(a)
        inset = 0.0
    else:
        hem = lerp(0.612, 0.545, smoothstep(0.4, 2.6, np.abs(a))) + 0.012 * np.sin(17 * a) - 0.040 * np.sin(a)
        inset = 0.011
    y = lerp(1.03, hem, t)
    f = np.clip((1.03 - y) / (1.03 - ref_hem), 0, 1.4)             # shared profile: both layers follow the same radius at the same height
    pleat = 1 + 0.045 * np.clip(f, 0, 1) ** 1.1 * np.sin(9 * a + 0.5) + 0.022 * np.clip(f, 0, 1) * np.sin(15 * a + 1.1)
    rxr = lerp(0.114, 0.262, f ** 1.3) * pleat - inset
    rzr = lerp(0.086, 0.246, f ** 1.3) * pleat - inset
    return np.stack([rxr * np.sin(a), y, rzr * np.cos(a)], -1)


def build_skirt():
    outer = thick_sheet('Skirt', 'Cream', skirt_grid('outer'), 0.005, 'Skirt', A.CREAM['skirt'], A.CREAM['skirt_in'], reference=axis_reference(0, 0), closed_u=True, edge_uv=A.EDGE['Cream'])
    under = thick_sheet('Petticoat', 'Burgundy', skirt_grid('under'), 0.004, 'Petticoat', A.BURGUNDY['petticoat'], A.BURGUNDY['petticoat'], reference=axis_reference(0, 0), closed_u=True, edge_uv=A.EDGE['Burgundy'])
    return outer + under


# ============================================================================ legs, boots

LEG_KEYS = np.array([
    # y,     cx,    cz,    rx,    rz
    [0.905, 0.092, 0.000, 0.083, 0.090],
    [0.800, 0.094, 0.004, 0.078, 0.083],
    [0.660, 0.097, 0.010, 0.067, 0.071],
    [0.520, 0.099, 0.014, 0.056, 0.059],
    [0.460, 0.099, 0.014, 0.052, 0.055],
    [0.380, 0.100, 0.008, 0.053, 0.057],
    [0.280, 0.102, 0.000, 0.055, 0.061],
    [0.170, 0.104, -0.002, 0.043, 0.048],
    [0.090, 0.105, -0.004, 0.036, 0.040],
])


def limb_keys(keys, rows):
    K = catmull(keys, rows)
    pts = np.stack([K[:, 1], K[:, 0], K[:, 2]], -1)
    return pts, K[:, 3], K[:, 4]


def build_leg():
    pts, rx, rz = limb_keys(LEG_KEYS, 20)
    return tube('LegR', 'Tights', pts, rx, rz, 'LegR', rect=A.TIGHTS['leg'], nu=20, n=2.1, up_hint=(0, 0, 1))


BOOT_KEYS = [
    (0.378, .103, -0.004, .0530, .0600, 2.2),
    (0.340, .102, -0.004, .0510, .0590, 2.2),
    (0.280, .102, -0.003, .0500, .0575, 2.2),
    (0.200, .104, -0.002, .0440, .0525, 2.3),
    (0.140, .105, 0.006, .0450, .0660, 2.4),
    (0.100, .105, 0.028, .0475, .0900, 2.5),
    (0.065, .105, 0.044, .0495, .1100, 2.6),
    (0.035, .105, 0.050, .0505, .1210, 2.6),
    (0.014, .105, 0.050, .0490, .1195, 2.6),
]


def build_boot():
    parts = []
    parts.append(closed_loft('BootR', 'Leather', BOOT_KEYS, 20, 32, A.LEATHER['boot'], 'BootR'))
    sole = closed_loft('SoleR', 'Leather', [(0.027, .105, 0.050, .0520, .1250, 2.6), (0.000, .105, 0.050, .0500, .1235, 2.6)], 4, 32, A.LEATHER['sole'], 'BootR')
    parts.append(sole)
    cuff_keys = [(0.392, .103, -0.004, .0585, .0665, 2.2), (0.356, .103, -0.004, .0630, .0710, 2.2), (0.330, .103, -0.004, .0560, .0630, 2.2)]
    P = loft_rows(cuff_keys, 6, 32)
    parts += thick_sheet('CuffR', 'Leather', P, 0.007, 'BootR', A.LEATHER['cuff'], A.LEATHER['cuff'], reference=axis_reference(0.103, -0.004), closed_u=True, edge_uv=A.EDGE['Leather'])
    for i, (y, rxs, rzs) in enumerate([(0.128, .0500, .0640), (0.262, .0525, .0600)]):
        band = loft_rows([(y + 0.013, .105 if y < .2 else .102, 0.012 if y < .2 else -0.003, rxs, rzs, 2.4), (y - 0.013, .105 if y < .2 else .102, 0.012 if y < .2 else -0.003, rxs, rzs, 2.4)], 3, 28)
        parts += thick_sheet('BootStrap%dR' % i, 'Leather', band, 0.006, 'BootR', A.LEATHER['strap2'], A.LEATHER['strap2'], reference=axis_reference(0.105 if y < .2 else .102, 0.012 if y < .2 else -0.003), closed_u=True, edge_uv=A.EDGE['Leather'])
        buckle = rbox('BootBuckle%dR' % i, 'Trinket', (0.105 + rxs + 0.006 if y < .2 else .102 + rxs + .006, y, 0.012 if y < .2 else -0.003), (0.010, 0.026, 0.020), 0.003, 'BootR', rect=A.TRINKET['flat'], color=GOLD, seg=3, outline=0.5)
        parts.append(buckle)
    return parts


# ============================================================================ arms and hands

def build_arm():
    parts = []
    pts = catmull([[.238, 1.095, .008], [.252, 1.045, .010], [.276, .925, .025], [.300, .80, .040]], 10)
    r = np.interp(np.linspace(0, 1, 10), [0, .25, .65, 1], [.032, .031, .027, .0235])
    arm = tube('ForearmR', 'Skin', pts, r, r * 0.95, 'ArmR', nu=14, up_hint=(0, 0, 1))
    arm.UV[:] = A.SKIN_FLAT
    parts.append(arm)
    palm = tube('PalmR', 'Skin', np.array([[.300, .800, .040], [.306, .765, .047], [.310, .735, .052]]), 0.034, 0.0135, 'HandR', nu=12, up_hint=(1, 0, 0))
    palm.UV[:] = A.SKIN_FLAT
    parts.append(palm)
    lengths = [0.060, 0.068, 0.062, 0.048]
    zs = [0.0195, 0.0070, -0.0055, -0.0175]
    for i, (L, dz) in enumerate(zip(lengths, zs)):
        z0 = 0.052 + dz
        path = np.array([[.310, .735, z0], [.308, .735 - L * 0.45, z0 + .001], [.302, .735 - L * 0.80, z0 + .002], [.296, .735 - L, z0 + .003]])
        f = tube('FingerR%d' % i, 'Skin', path, [.0108, .0100, .0088, .0066], [.0098, .0092, .0080, .0060], 'HandR', nu=8, up_hint=(1, 0, 0))
        f.UV[:] = A.SKIN_FLAT
        parts.append(f)
    thumb = tube('ThumbR', 'Skin', np.array([[.304, .800, .062], [.300, .768, .078], [.296, .738, .088]]), [.0115, .0100, .0072], [.0105, .0090, .0066], 'HandR', nu=8, up_hint=(1, 0, 0))
    thumb.UV[:] = A.SKIN_FLAT
    parts.append(thumb)
    return parts


# ============================================================================ scarf, belts, satchel

def build_scarf():
    parts = []
    rows = 7
    ys = np.linspace(1.419, 1.333, rows)
    rings = []
    for j, y in enumerate(ys):
        s = j / (rows - 1)
        bulge = 0.020 * math.sin(math.pi * s) ** 0.8 - 0.004 * (1 - s) + 0.004 * math.sin(2 * math.pi * s + 1.0)
        rings.append(ring(0.0, y, -0.004, 0.058 + bulge, 0.055 + bulge, 56, 2.2))
    P = np.stack(rings)
    a = (math.pi + np.linspace(0, math.tau, 57)) - math.tau
    r_scale = 1 + 0.050 * np.sin(4 * a[None, :] + 1.7 * np.arange(rows)[:, None]) + 0.035 * g(a[None, :] + 0.7, 0.55) * np.sin(math.pi * np.linspace(0, 1, rows)[:, None])
    c = np.array([0.0, 0.0, -0.004])
    P = P.copy()
    P[..., 0] *= r_scale
    P[..., 2] = (P[..., 2] + 0.004) * r_scale - 0.004
    parts += thick_sheet('ScarfCollar', 'Burgundy', P, 0.016, 'ScarfCollar', A.BURGUNDY['collar'], A.BURGUNDY['collar'], reference=axis_reference(0, -0.004), closed_u=True, edge_uv=A.EDGE['Burgundy'])

    def tail(name, sign, x0, x1, y0, y1, zoff, rect, zone):
        nv, nu = 14, 7
        v = np.linspace(0, 1, nv)[:, None]
        u = np.linspace(0, 1, nu)[None, :]
        y = lerp(y0, y1, v) + 0 * u
        xc = lerp(x0, x1, v) + 0.012 * np.sin(math.tau * v * 0.8 + (0.6 if sign > 0 else 2.2))
        w = lerp(0.078, 0.088, v)
        tw = 0.30 * np.sin(2.0 * v + (0.4 if sign > 0 else 1.5))
        x = xc + (u - 0.5) * w * np.cos(tw)
        cz, rx, rz = body_r(y)
        ell = np.sqrt(np.clip(1 - (x / np.maximum(rx, 1e-3)) ** 2, 0.25, 1))
        bust = np.where(sign > 0, 0.012 * g(np.abs(x) - 0.05, 0.034) * g(y - 1.195, 0.045), 0)
        z = sign * (cz + rz * ell + zoff) + sign * bust + (u - 0.5) * w * np.sin(tw) * 0.6
        z = z + sign * 0.006 * np.cos(math.pi * (u - 0.5) * 2) * 0
        return thick_sheet(name, 'Burgundy', np.stack([x, y, z], -1), 0.006, zone, rect, rect, reference=axis_reference(0, 0), edge_uv=A.EDGE['Burgundy'])

    parts += tail('ScarfTailF', 1, -0.054, -0.062, 1.376, 0.950, 0.036, A.BURGUNDY['tail'], 'ScarfF')
    parts += tail('ScarfTailB', -1, 0.054, 0.062, 1.372, 0.800, 0.040, A.BURGUNDY['tail'], 'ScarfB')
    return parts


def build_belts():
    parts = []
    ys = [1.165, 1.120, 1.072, 1.024, 0.995]
    keys = [(y, 0, float(body_r(y)[0]), float(body_r(y)[1]) + 0.0075, float(body_r(y)[2]) + 0.0075, 2.2) for y in ys]
    parts += thick_sheet('Corset', 'Leather', loft_rows(keys, 10, 56), 0.007, 'Torso', A.LEATHER['corset'], A.LEATHER['corset'], reference=axis_reference(0, 0), closed_u=True, edge_uv=A.EDGE['Leather'])
    # the hip belt hugs the open front but stands outside the coat at the sides and back (it is measured on the real coat surface)
    coat = coat_grid()

    def belt_ring(y, over=0.014):
        cz, rx, rz = (float(v) for v in body_r(y))
        near = np.abs(coat[..., 1] - y) < 0.025
        side = float(np.abs(coat[..., 0][near]).max()) + over
        back = float(-coat[..., 2][near].min()) + over
        front = max(cz + rz + 0.013, float(skirt_radii(y, 0.0)[1]) + 0.012)          # also clears the skirt that fills the coat opening
        return (y, 0, (front - back) / 2, max(rx + 0.031, side), (front + back) / 2, 2.2), front
    ring_a, front_a = belt_ring(0.958)
    ring_b, _ = belt_ring(0.922)
    parts += thick_sheet('HipBelt', 'Leather', loft_rows([ring_a, ring_b], 3, 56), 0.007, 'Torso', A.LEATHER['strap2'], A.LEATHER['strap2'], reference=axis_reference(0, 0), closed_u=True, edge_uv=A.EDGE['Leather'])
    # buckles and eyelets
    parts.append(rbox('CorsetBuckle', 'Trinket', (0.0, 1.040, float(body_r(1.04)[2]) + 0.0165), (0.034, 0.040, 0.010), 0.004, 'Torso', rect=A.TRINKET['flat'], color=GOLD, seg=3, outline=0.5))
    parts.append(rbox('HipBuckle', 'Trinket', (-0.050, 0.940, belt_ring(0.94)[1] + 0.010), (0.030, 0.034, 0.010), 0.004, 'Torso', rect=A.TRINKET['flat'], color=GOLD, seg=3, outline=0.5))
    for k, y in enumerate((1.150, 1.108, 1.066)):
        for sgn in (-1, 1):
            e = uv_sphere('Eyelet%d%d' % (k, sgn), 'Trinket', (sgn * 0.016, y, float(body_r(y)[2]) + 0.0145), (0.0048, 0.0048, 0.0030), 'Torso', nu=8, nv=5, rect=A.TRINKET['flat'], color=GOLD, outline=0.4)
            parts.append(e)
    # tassels at the hip
    for k, (x, y, z) in enumerate([(-0.095, 0.915, float(body_r(0.915)[2]) + 0.036), (-0.125, 0.905, 0.082)]):
        pts = np.array([[x, y, z], [x, y - 0.025, z + 0.002], [x, y - 0.075, z + 0.004]])
        tt = tube('Tassel%d' % k, 'Trinket', pts, [0.005, 0.011, 0.0035], [0.005, 0.011, 0.0035], 'Torso', rect=A.TRINKET['flat'], nu=8, color=BURG_TINT, outline=0.5)
        parts.append(tt)
    return parts


def strap_path(points):
    """(alpha, y) pairs -> xyz on the coat surface; returns (path, outward normals)."""
    out, nrm = [], []
    for a, y in points:
        cz, rx, rz = (float(v) for v in body_r(y))
        x = (rx + 0.0235) * math.sin(a)
        z = cz + (rz + 0.0235) * math.cos(a)
        out.append((x, y, z))
        nrm.append(normalize(np.array([x / (rx + .02) ** 2, 0.0, (z - cz) / (rz + .02) ** 2])))
    return np.array(out), np.array(nrm)


def build_satchel():
    parts = []
    front = [(-1.50, 1.345), (-1.12, 1.292), (-0.62, 1.185), (-0.05, 1.080), (0.55, 0.985), (1.10, 0.935), (1.30, 0.925)]
    back = [(1.38, 0.935), (1.95, 1.005), (2.55, 1.115), (3.14, 1.220), (3.80, 1.300), (4.45, 1.340), (4.72, 1.345)]
    for name, spec in (('StrapF', front), ('StrapB', back)):
        pts, nrm = strap_path(spec)
        K = catmull(pts, 18)
        Nn = catmull(nrm, 18)
        p = tube(name, 'Leather', K, 0.0175, 0.0042, 'Strap', rect=A.LEATHER['strap'], nu=10, n=3.0, up_hint=Nn, caps=True)
        parts.append(p)
    parts.append(rbox('SatchelBody', 'Leather', (0.224, 0.850, -0.012), (0.150, 0.190, 0.090), 0.024, 'Satchel', rect=A.LEATHER['bag'], seg=3))
    flap = rbox('SatchelFlap', 'Leather', (0.224, 0.905, 0.036), (0.158, 0.102, 0.022), 0.010, 'Satchel', rect=A.LEATHER['flap'], seg=3)
    rotate_about(flap, rot_x(-0.10), (0.224, 0.905, 0.036))
    parts.append(flap)
    parts.append(rbox('SatchelClasp', 'Trinket', (0.224, 0.866, 0.050), (0.028, 0.036, 0.012), 0.005, 'Satchel', rect=A.TRINKET['flat'], color=GOLD, seg=3, outline=0.5))
    for sgn, name in ((-1, 'L'), (1, 'R')):
        parts.append(rbox('SatchelSide' + name, 'Leather', (0.224 + sgn * 0.078, 0.885, -0.012), (0.012, 0.044, 0.036), 0.004, 'Satchel', rect=A.LEATHER['strap2'], seg=3, outline=0.7))
    for k, (x, y) in enumerate([(0.196, 0.752), (0.250, 0.734)]):
        parts.append(uv_sphere('Vial%d' % k, 'Trinket', (x, y, 0.052), (0.0145, 0.030, 0.0145), 'Satchel', nu=12, nv=8, rect=A.TRINKET['flat'], color=GLASS, outline=0.6))
        parts.append(uv_sphere('VialCork%d' % k, 'Trinket', (x, y + 0.033, 0.052), (0.0085, 0.0085, 0.0085), 'Satchel', nu=8, nv=5, rect=A.TRINKET['flat'], color=(0.62, 0.42, 0.26, 1), outline=0.5))
        pts = np.array([[x, y + 0.036, 0.052], [x * 0.9 + 0.224 * 0.1, y + 0.062, 0.050], [0.224, 0.83, 0.046]])
        parts.append(tube('VialCord%d' % k, 'Trinket', pts, 0.0022, 0.0022, 'Satchel', rect=A.TRINKET['flat'], nu=5, color=DARK, outline=0.3))
    return parts


def build_ornaments():
    parts = []
    # four-point coat clasps at the shoulders (original motif)
    for sgn in (-1, 1):
        cz, rx, rz = (float(v) for v in body_r(1.30))
        c = (sgn * 0.118, 1.286, cz + rz * math.sqrt(max(1 - (0.118 / rx) ** 2, 0.1)) + 0.020)
        for ang in (0.0, math.pi / 2):
            b = rbox('Clasp%d%d' % (sgn, int(ang > 0)), 'Trinket', c, (0.030, 0.0085, 0.0075), 0.003, 'Torso', rect=A.TRINKET['flat'], color=GOLD, seg=3, outline=0.4)
            rotate_about(b, rot_z(ang + math.pi / 4), c)
            parts.append(b)
    return parts


def side_name(n):
    return n.replace('R', 'L', 1)


def build_body_parts():
    parts = [build_torso()]
    parts += build_coat()
    sleeve = build_sleeve()
    parts += sleeve + [p.mirrored(side_name(p.name)) for p in sleeve]
    parts += build_skirt()
    leg = build_leg()
    parts += [leg, leg.mirrored('LegL')]
    boots = build_boot()
    parts += boots + [p.mirrored(side_name(p.name)) for p in boots]
    arm = build_arm()
    parts += arm + [p.mirrored(side_name(p.name)) for p in arm]
    parts += build_scarf()
    parts += build_belts()
    parts += build_satchel()
    parts += build_ornaments()
    return parts
