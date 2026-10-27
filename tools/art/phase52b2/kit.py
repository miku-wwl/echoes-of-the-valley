"""Numpy surface kit for the Phase 5.2-B2 caretaker (original, no acquired geometry).

Unity metres everywhere: +X right, +Y up, +Z forward (the character faces +Z).
Triangles follow the numeric right-hand rule: the cross product of (v1-v0, v2-v0) points OUT of the surface,
which is what Unity renders as the front face. Blender is only used afterwards for the rig, previews and interchange files.
"""
import math
import numpy as np

TAU = math.tau


def normalize(v, eps=1e-12):
    v = np.asarray(v, float)
    return v / np.maximum(np.linalg.norm(v, axis=-1, keepdims=True), eps)


def smoothstep(a, b, x):
    t = np.clip((np.asarray(x, float) - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def lerp(a, b, t):
    return a + (b - a) * t


def catmull(keys, count):
    """Catmull-Rom resample of a key array (k, d) to `count` rows, uniform in key index."""
    keys = np.asarray(keys, float)
    if keys.ndim == 1:
        keys = keys[:, None]
    k = len(keys)
    t = np.linspace(0, k - 1, count)
    i = np.minimum(np.floor(t).astype(int), k - 2)
    f = (t - i)[:, None]
    p0 = keys[np.maximum(i - 1, 0)]
    p1 = keys[i]
    p2 = keys[np.minimum(i + 1, k - 1)]
    p3 = keys[np.minimum(i + 2, k - 1)]
    out = 0.5 * ((2 * p1) + (-p0 + p2) * f + (2 * p0 - 5 * p1 + 4 * p2 - p3) * f ** 2 + (-p0 + 3 * p1 - 3 * p2 + p3) * f ** 3)
    return out


class Part:
    """One welded surface with a single material. `P` holds optional per-vertex arrays (for example the strand parameter `s`)."""

    def __init__(self, name, material, V, F, UV, zone, color=None, outline=1.0, **params):
        self.name = name
        self.material = material
        self.V = np.asarray(V, float).reshape(-1, 3)
        self.F = np.asarray(F, int).reshape(-1, 3)
        self.UV = np.asarray(UV, float).reshape(-1, 2)
        n = len(self.V)
        if color is None:
            color = (1, 1, 1, 1)
        self.C = np.array(np.broadcast_to(np.asarray(color, float), (n, 4)))
        self.zone = zone
        self.outline = outline
        self.P = {k: np.asarray(v, float) for k, v in params.items()}
        self.N = None

    def copy(self, name=None):
        p = Part(name or self.name, self.material, self.V.copy(), self.F.copy(), self.UV.copy(), self.zone, self.C.copy(), self.outline)
        p.P = {k: v.copy() for k, v in self.P.items()}
        if self.N is not None:
            p.N = self.N.copy()
        return p

    def mirrored(self, name, uv_rect=None):
        p = self.copy(name)
        if p.zone.endswith('R') and p.zone[:-1] in ('Arm', 'Hand', 'Leg', 'Boot'):
            p.zone = p.zone[:-1] + 'L'
        p.V[:, 0] *= -1
        p.F = p.F[:, [0, 2, 1]]
        if p.N is not None:
            p.N[:, 0] *= -1
        if uv_rect is not None:
            p.UV[:, 0] = uv_rect[0] + uv_rect[1] - p.UV[:, 0]
        return p

    def transform(self, matrix3, offset=(0, 0, 0)):
        self.V = self.V @ np.asarray(matrix3).T + np.asarray(offset)
        if self.N is not None:
            self.N = normalize(self.N @ np.asarray(matrix3).T)
        return self


def merge(parts, name, material, zone):
    V = np.concatenate([p.V for p in parts])
    UV = np.concatenate([p.UV for p in parts])
    C = np.concatenate([p.C for p in parts])
    offset = np.cumsum([0] + [len(p.V) for p in parts])
    F = np.concatenate([p.F + o for p, o in zip(parts, offset)])
    merged = Part(name, material, V, F, UV, zone, outline=parts[0].outline)
    merged.C = C
    for key in parts[0].P:
        if all(key in p.P for p in parts):
            merged.P[key] = np.concatenate([p.P[key] for p in parts])
    return merged


# ------------------------------------------------------------------ orientation and normals

def signed_volume(V, F):
    a, b, c = V[F[:, 0]], V[F[:, 1]], V[F[:, 2]]
    return float(np.einsum('ij,ij->i', a, np.cross(b, c)).sum() / 6.0)


def flip_faces(F):
    return np.asarray(F)[:, [0, 2, 1]]


def orient_closed(part):
    if signed_volume(part.V, part.F) < 0:
        part.F = flip_faces(part.F)
    return part


def orient_outward(part, reference):
    """`reference(centres)` returns, for each face centre, a direction that the front face should point towards."""
    V, F = part.V, part.F
    centre = V[F].mean(axis=1)
    normal = np.cross(V[F[:, 1]] - V[F[:, 0]], V[F[:, 2]] - V[F[:, 0]])
    if float(np.einsum('ij,ij->i', normal, reference(centre)).sum()) < 0:
        part.F = flip_faces(F)
    return part


def axis_reference(axis_x=0.0, axis_z=0.0):
    """Outward from the vertical axis through (axis_x, axis_z)."""
    def ref(c):
        r = np.array(c, float)
        r[:, 0] -= axis_x
        r[:, 2] -= axis_z
        r[:, 1] = 0
        return r
    return ref


def vertex_normals(V, F, weld=True):
    fn = np.cross(V[F[:, 1]] - V[F[:, 0]], V[F[:, 2]] - V[F[:, 0]])
    N = np.zeros_like(V)
    for i in range(3):
        np.add.at(N, F[:, i], fn)
    if weld:
        key = np.round(V * 1e5).astype(np.int64)
        _, inverse = np.unique(key, axis=0, return_inverse=True)
        inverse = inverse.reshape(-1)
        acc = np.zeros((inverse.max() + 1, 3))
        np.add.at(acc, inverse, N)
        N = acc[inverse]
    return normalize(N)


def finish_normals(part):
    if part.N is None:
        part.N = vertex_normals(part.V, part.F)
    return part


# ------------------------------------------------------------------ grids

def grid_faces(nv, nu):
    idx = np.arange(nv * nu).reshape(nv, nu)
    a, b, c, d = idx[:-1, :-1], idx[:-1, 1:], idx[1:, 1:], idx[1:, :-1]
    return np.concatenate([np.stack([a, b, c], -1).reshape(-1, 3), np.stack([a, c, d], -1).reshape(-1, 3)])


def grid_uv(nv, nu, rect=(0, 0, 1, 1)):
    u = np.linspace(0, 1, nu)[None, :].repeat(nv, 0)
    v = np.linspace(0, 1, nv)[:, None].repeat(nu, 1)
    return np.stack([rect[0] + u * (rect[2] - rect[0]), rect[1] + v * (rect[3] - rect[1])], -1).reshape(-1, 2)


def grid_part(name, material, P, rect, zone, reference=None, color=None, closed=False, **kw):
    """Open or u-closed grid P (nv, nu, 3) -> Part. For a u-closed grid the last column repeats the first (UV seam)."""
    nv, nu = P.shape[:2]
    part = Part(name, material, P.reshape(-1, 3), grid_faces(nv, nu), grid_uv(nv, nu, rect), zone, color, **kw)
    if reference is not None:
        orient_outward(part, reference)
    elif closed:
        orient_closed(part)
    return part


def ring(cx, y, cz, rx, rz, nu=48, n=2.0, theta0=math.pi):
    """Superellipse ring; u runs from the BACK (theta0) around the left side to the front and on to the right (the seam is behind)."""
    t = theta0 + TAU * np.arange(nu + 1) / nu
    s, c = np.sin(t), np.cos(t)
    e = 2.0 / n
    x = rx * np.sign(s) * np.abs(s) ** e
    z = rz * np.sign(c) * np.abs(c) ** e
    return np.stack([cx + x, np.full_like(x, y), cz + z], -1)


def loft_rows(keys, nv, nu=48):
    """keys: rows of (y, cx, cz, rx, rz, n). Returns grid (nv, nu+1, 3) of smoothly interpolated superellipse rings."""
    K = catmull(np.asarray(keys, float), nv)
    return np.stack([ring(k[1], k[0], k[2], k[3], k[4], nu, k[5]) for k in K])


# ------------------------------------------------------------------ tubes and locks along a path

def path_frames(points, up_hint=(0, 0, 1)):
    tangent = normalize(np.gradient(points, axis=0))
    up = np.broadcast_to(np.asarray(up_hint, float), points.shape)
    side = np.cross(tangent, up)
    bad = np.linalg.norm(side, axis=-1) < 1e-4
    if bad.any():
        side[bad] = np.cross(tangent[bad], np.array([1.0, 0, 0]))
    side = normalize(side)
    normal = normalize(np.cross(side, tangent))
    return tangent, side, normal


def tube_grid(points, rx, rz, nu=18, n=2.0, up_hint=(0, 0, 1)):
    """Rings perpendicular to `points` (k,3); returns grid (k, nu+1, 3). rx/rz: radii along the frame side / normal."""
    k = len(points)
    rx = np.broadcast_to(np.asarray(rx, float), (k,))
    rz = np.broadcast_to(np.asarray(rz, float), (k,))
    tangent, side, normal = path_frames(points, up_hint)
    t = TAU * np.arange(nu + 1) / nu
    s, c = np.sin(t), np.cos(t)
    e = 2.0 / n
    sx = np.sign(c) * np.abs(c) ** e
    sz = np.sign(s) * np.abs(s) ** e
    return np.stack([points[i] + side[i] * sx[:, None] * rx[i] + normal[i] * sz[:, None] * rz[i] for i in range(k)])


def path_reference(points):
    """Outward = away from the nearest point of a path (limbs, sleeves)."""
    points = np.asarray(points, float)

    def away(cn):
        d = cn[:, None, :] - points[None, :, :]
        j = np.argmin(np.einsum('ijk,ijk->ij', d, d), axis=1)
        return cn - points[j]
    return away


def tube(name, material, points, rx, rz, zone, rect=(0, 0, 1, 1), nu=18, n=2.0, up_hint=(0, 0, 1), caps=True, color=None, **kw):
    """Smooth tube along `points` (k,3) with optional end caps."""
    points = np.asarray(points, float)
    P = tube_grid(points, rx, rz, nu, n, up_hint)
    k = len(points)
    part = grid_part(name, material, P, rect, zone, closed=False, color=color, **kw)
    orient_outward(part, path_reference(points))
    if caps:
        part = cap_ends(part, k, nu + 1, points)
    part.P['s'] = np.repeat(np.linspace(0, 1, k), nu + 1)[:len(part.V)] if len(part.V) == k * (nu + 1) else np.concatenate([np.repeat(np.linspace(0, 1, k), nu + 1), [0.0, 1.0]])
    return part


def cap_ends(part, nv, nu, points):
    """Close both open ends of a tube grid with a centre fan."""
    V = list(part.V)
    UV = list(part.UV)
    C = list(part.C)
    F = [part.F]
    extra = {k: list(v) for k, v in part.P.items()}
    for row, centre, flip in ((0, points[0], True), (nv - 1, points[-1], False)):
        ring_idx = np.arange(row * nu, row * nu + nu - 1)
        ci = len(V)
        V.append(centre)
        UV.append(part.UV[ring_idx].mean(0))
        C.append(part.C[ring_idx[0]])
        for key in extra:
            extra[key].append(part.P[key][ring_idx[0]] if len(part.P[key]) else 0)
        tri = np.stack([ring_idx, ring_idx + 1, np.full_like(ring_idx, ci)], -1)
        F.append(tri[:, [0, 2, 1]] if flip else tri)
    part.V = np.array(V)
    part.UV = np.array(UV)
    part.C = np.array(C)
    part.F = np.concatenate(F)
    part.P = {k: np.array(v) for k, v in extra.items()}
    orient_closed(part)
    return part


def lock(name, material, path, width, thick, zone, ref_dir=(0, 0, 1), ns=14, nc=10, n=3.0, rect=(0, 0, 1, 1), color=None, twist=0.0, **kw):
    """A tapered hair-like clump: flattened superellipse swept along `path`. width/thick are callables of s in [0,1]."""
    pts = catmull(np.asarray(path, float), ns + 1)
    s = np.linspace(0, 1, ns + 1)
    ref = np.broadcast_to(np.asarray(ref_dir, float), pts.shape)
    tangent = normalize(np.gradient(pts, axis=0))
    side = np.cross(tangent, ref)
    bad = np.linalg.norm(side, axis=-1) < 1e-4
    if bad.any():
        side[bad] = np.cross(tangent[bad], np.array([1.0, 0, 0]))
    side = normalize(side)
    normal = normalize(np.cross(side, tangent))
    a = TAU * np.arange(nc) / nc
    ca, sa = np.cos(a), np.sin(a)
    e = 2.0 / n
    cx = np.sign(ca) * np.abs(ca) ** e
    cz = np.sign(sa) * np.abs(sa) ** e
    w = np.array([width(x) for x in s])
    t = np.array([thick(x) for x in s])
    rows = []
    for i in range(ns + 1):
        tw = twist * s[i]
        sx = cx * math.cos(tw) - cz * math.sin(tw) * 0
        rows.append(pts[i] + side[i] * (cx[:, None] * w[i] / 2) + normal[i] * (cz[:, None] * t[i] / 2) * 1.0)
    P = np.stack(rows)  # (ns+1, nc, 3)
    V = P.reshape(-1, 3)
    uu = np.broadcast_to(0.5 + 0.5 * np.cos(a)[None, :], (ns + 1, nc))
    vv = np.broadcast_to(s[:, None], (ns + 1, nc))
    UV = np.stack([rect[0] + uu * (rect[2] - rect[0]), rect[1] + vv * (rect[3] - rect[1])], -1).reshape(-1, 2)
    idx = np.arange((ns + 1) * nc).reshape(ns + 1, nc)
    a_, b_, c_, d_ = idx[:-1, :], idx[:-1, np.r_[1:nc, 0]], idx[1:, np.r_[1:nc, 0]], idx[1:, :]
    F = np.concatenate([np.stack([a_, b_, c_], -1).reshape(-1, 3), np.stack([a_, c_, d_], -1).reshape(-1, 3)])
    # root cap
    r0 = len(V)
    V = np.vstack([V, pts[0][None, :], pts[-1][None, :]])
    UV = np.vstack([UV, [[rect[0] + .5 * (rect[2] - rect[0]), rect[1]], [rect[0] + .5 * (rect[2] - rect[0]), rect[3]]]])
    root = np.stack([idx[0], idx[0][np.r_[1:nc, 0]], np.full(nc, r0)], -1)
    tip = np.stack([idx[-1], idx[-1][np.r_[1:nc, 0]], np.full(nc, r0 + 1)], -1)
    F = np.concatenate([F, root, tip])
    part = Part(name, material, V, F, UV, zone, color, **kw)
    part.P['s'] = np.concatenate([np.repeat(s, nc), [0.0, 1.0]])
    orient_closed(part)
    return part


# ------------------------------------------------------------------ cloth sheets with real thickness

def _grid_normals(P, closed_u):
    dpu = np.gradient(P, axis=1)
    dpv = np.gradient(P, axis=0)
    return normalize(np.cross(dpu, dpv))


def thick_sheet(name, material, P, thickness, zone, rect_out, rect_in=None, reference=None, inner_material=None,
                color=None, edge_uv=None, edges=True, closed_u=False, **kw):
    """Cloth with thickness from a grid P (nv, nu, 3): outer shell, inner shell and edge strips. Returns a list of Parts."""
    nv, nu = P.shape[:2]
    N = _grid_normals(P, closed_u)
    if reference is not None:
        centre = P.reshape(-1, 3)
        ref = reference(centre).reshape(P.shape)
        if float((N * ref).sum()) < 0:
            N = -N
    t = thickness
    outer = P + N * (t / 2)[..., None] if np.ndim(t) else P + N * (t / 2)
    inner = P - N * (t / 2)[..., None] if np.ndim(t) else P - N * (t / 2)
    parts = []
    po = Part(name, material, outer.reshape(-1, 3), grid_faces(nv, nu), grid_uv(nv, nu, rect_out), zone, color, **kw)
    # grid normal N = cross(dP/du, dP/dv): grid_faces winding gives the same direction, so flip when the outer faces point the wrong way
    fn = np.cross(po.V[po.F[:, 1]] - po.V[po.F[:, 0]], po.V[po.F[:, 2]] - po.V[po.F[:, 0]])
    face_normal_ref = N.reshape(-1, 3)[po.F[:, 0]]
    if float(np.einsum('ij,ij->i', fn, face_normal_ref).sum()) < 0:
        po.F = flip_faces(po.F)
    parts.append(po)
    pi = Part(name + '_In', inner_material or material, inner.reshape(-1, 3), flip_faces(po.F), grid_uv(nv, nu, rect_in or rect_out), zone, color, **kw)
    parts.append(pi)
    if edges:
        V = np.concatenate([outer.reshape(-1, 3), inner.reshape(-1, 3)])
        off = nv * nu
        idx = np.arange(nv * nu).reshape(nv, nu)
        faces = []
        borders = [idx[0, :], idx[-1, :]]
        if not closed_u:
            borders += [idx[:, 0], idx[:, -1]]
        for b in borders:
            a, c = b[:-1], b[1:]
            faces.append(np.stack([a, c, c + off], -1))
            faces.append(np.stack([a, c + off, a + off], -1))
        F = np.concatenate(faces)
        uv = np.tile(np.asarray(edge_uv if edge_uv is not None else rect_out[:2], float), (len(V), 1))
        edge = Part(name + '_Edge', material, V, F, uv, zone, color, **kw)
        # orient each strip away from the sheet middle
        mid = P.reshape(-1, 3).mean(0)
        c = edge.V[edge.F].mean(1)
        n = np.cross(edge.V[edge.F[:, 1]] - edge.V[edge.F[:, 0]], edge.V[edge.F[:, 2]] - edge.V[edge.F[:, 0]])
        flipme = np.einsum('ij,ij->i', n, c - mid) < 0
        edge.F[flipme] = edge.F[flipme][:, [0, 2, 1]]
        parts.append(edge)
    for extra in kw:
        pass
    return parts


# ------------------------------------------------------------------ rounded boxes and misc solids

def rbox(name, material, center, size, radius, zone, rect=(0, 0, 1, 1), color=None, seg=6, **kw):
    """Rounded box with smooth normals (all six faces share one welded grid-of-grids)."""
    half = np.asarray(size, float) / 2
    r = min(radius, half.min() * 0.95)
    # sample positions along each axis: dense near the rounded borders
    def samples(h):
        inner = max(h - r, 0)
        core = np.array([-1.0, 0.0, 1.0])
        edge = np.linspace(0, 1, seg + 1)
        s = np.unique(np.concatenate([-1 + edge * (r / h), core, 1 - edge[::-1] * (r / h)]))
        return np.clip(s, -1, 1)
    sx, sy, sz = samples(half[0]), samples(half[1]), samples(half[2])
    verts, faces, uvs = [], [], []

    def face(axis, sign, a_s, b_s):
        a_axis, b_axis = [i for i in range(3) if i != axis]
        A, B = np.meshgrid(a_s, b_s, indexing='ij')
        p = np.zeros(A.shape + (3,))
        p[..., axis] = sign
        p[..., a_axis] = A
        p[..., b_axis] = B
        base = len(verts)
        pts = p.reshape(-1, 3) * half
        q = np.clip(pts, -(half - r), half - r)
        d = normalize(pts - q)
        out = q + d * r
        verts.extend(out)
        nA, nB = A.shape
        f = grid_faces(nA, nB)
        # orient: front face along the outward axis
        tri_n = np.cross(out[f[:, 1] - 0] - out[f[:, 0]], out[f[:, 2]] - out[f[:, 0]]) if False else None
        faces.append(f + base)
        uv = np.stack([(A - A.min()) / max(A.max() - A.min(), 1e-9), (B - B.min()) / max(B.max() - B.min(), 1e-9)], -1).reshape(-1, 2)
        uvs.append(uv)
    face(0, 1, sy, sz); face(0, -1, sy, sz)
    face(1, 1, sx, sz); face(1, -1, sx, sz)
    face(2, 1, sx, sy); face(2, -1, sx, sy)
    V = np.array(verts) + np.asarray(center, float)
    F = np.concatenate(faces)
    UV = np.concatenate(uvs)
    UV = np.stack([rect[0] + UV[:, 0] * (rect[2] - rect[0]), rect[1] + UV[:, 1] * (rect[3] - rect[1])], -1)
    part = Part(name, material, V, F, UV, zone, color, **kw)
    # per-face orientation fix: each triangle must point away from the box centre
    centre_face = part.V[part.F].mean(1) - np.asarray(center, float)
    n = np.cross(part.V[part.F[:, 1]] - part.V[part.F[:, 0]], part.V[part.F[:, 2]] - part.V[part.F[:, 0]])
    flip = np.einsum('ij,ij->i', n, centre_face) < 0
    part.F[flip] = part.F[flip][:, [0, 2, 1]]
    return part


def uv_sphere(name, material, center, radii, zone, nu=24, nv=14, rect=(0, 0, 1, 1), color=None, **kw):
    lat = np.linspace(-math.pi / 2, math.pi / 2, nv + 1)
    lon = np.linspace(0, TAU, nu + 1)
    LAT, LON = np.meshgrid(lat, lon, indexing='ij')
    P = np.stack([np.cos(LAT) * np.sin(LON) * radii[0], np.sin(LAT) * radii[1], np.cos(LAT) * np.cos(LON) * radii[2]], -1) + np.asarray(center, float)
    part = grid_part(name, material, P, rect, zone, closed=True, color=color, **kw)
    return part


def closed_loft(name, material, keys, nv, nu, rect, zone, color=None, **kw):
    """Capped solid loft of superellipse rings (keys top -> bottom) -- boots, belts."""
    P = loft_rows(keys, nv, nu)
    part = grid_part(name, material, P, rect, zone, reference=axis_reference(float(np.mean([k[1] for k in keys])), float(np.mean([k[2] for k in keys]))), color=color, **kw)
    centres = np.array([[float(row[:, 0].mean()), float(row[:, 1].mean()), float(row[:, 2].mean())] for row in P])
    part = cap_ends(part, P.shape[0], P.shape[1], np.array([centres[0], centres[-1]]))
    return part
