"""Skin weights for the caretaker: authored by rules (not painted), at most 4 normalised influences per vertex."""
import math
import numpy as np
from kit import smoothstep, normalize
import rig_spec as R

IDX = R.INDEX


def _seg_dist(points, a, b):
    ab = b - a
    t = np.clip(((points - a) @ ab) / max(float(ab @ ab), 1e-9), 0, 1)
    return np.linalg.norm(points - (a + ab * t[:, None]), axis=1)


def chain_weights(points, names, soft):
    """Inverse-distance^4 influence of bone segments (head -> tail); every vertex keeps its strongest influences."""
    cols = []
    for n in names:
        d = _seg_dist(points, R.HEAD[n], R.tail(n))
        cols.append(1.0 / (d * d + soft * soft) ** 2)
    W = np.stack(cols, 1)
    return {n: W[:, i] for i, n in enumerate(names)}


def _normalise(acc):
    """acc: {bone: (n,) weights}. Keep the 4 strongest, normalise."""
    names = list(acc)
    W = np.stack([acc[n] for n in names], 1)
    order = np.argsort(-W, axis=1)[:, :4]
    top = np.take_along_axis(W, order, 1)
    total = np.maximum(top.sum(1, keepdims=True), 1e-9)
    top = top / total
    idx = np.array([[IDX[names[j]] for j in row] for row in order])
    n = len(W)
    if top.shape[1] < 4:
        pad = 4 - top.shape[1]
        top = np.concatenate([top, np.zeros((n, pad))], 1)
        idx = np.concatenate([idx, np.zeros((n, pad), int)], 1)
    return idx.astype(int), top


def _add(acc, name, w):
    w = np.asarray(w, float)
    acc[name] = acc.get(name, 0) + w


def torso_weights(V):
    """Piecewise-linear by height over Pelvis/Spine/Chest/Neck, with arm influence over the shoulders."""
    y = V[:, 1]
    acc = {}
    joints = [('Pelvis', 0.86), ('Spine', 0.985), ('Chest', 1.14), ('Neck', 1.36)]
    ws = {n: np.zeros(len(V)) for n, _ in joints}
    for (n0, y0), (n1, y1) in zip(joints[:-1], joints[1:]):
        t = np.clip((y - y0) / (y1 - y0), 0, 1)
        inside = (y >= y0) & (y < y1)
        ws[n0] += np.where(inside, 1 - t, 0)
        ws[n1] += np.where(inside, t, 0)
    ws['Pelvis'] += np.where(y < joints[0][1], 1, 0)
    ws['Neck'] += np.where(y >= joints[-1][1], 1, 0)
    side = np.where(V[:, 0] < 0, 'L', 'R')
    arm = smoothstep(0.105, 0.17, np.abs(V[:, 0])) * smoothstep(1.20, 1.30, y)
    for n, w in ws.items():
        _add(acc, n, w * (1 - arm))
    for s in ('L', 'R'):
        m = (side == s) * arm
        _add(acc, 'UpperArm' + s, m * 0.82)
        _add(acc, 'Clavicle' + s, m * 0.18)
    return acc


def lower_cloth(V, kind):
    """Skirt / petticoat / coat below the waist, driven by the angle around the body and the depth below the waist."""
    x, y, z = V[:, 0], V[:, 1], V[:, 2]
    acc = {}
    top = 1.03 if kind in ('Skirt', 'Petticoat') else 0.90
    bottom = 0.57 if kind in ('Skirt', 'Petticoat') else 0.41
    t = np.clip((top - y) / (top - bottom), 0, 1)
    m = smoothstep(0.0, 0.5, t)
    d = normalize(np.stack([x, np.zeros_like(x), z], 1))
    if kind in ('Skirt', 'Petticoat'):
        ang = {'SkirtF': (0, 1), 'SkirtB': (0, -1), 'SkirtL': (-1, 0), 'SkirtR': (1, 0)}
        raw = {n: np.maximum(d[:, 0] * v[0] + d[:, 2] * v[1], 0) ** 2 for n, v in ang.items()}
        total = np.maximum(sum(raw.values()), 1e-9)
        _add(acc, 'Pelvis', 1 - m)
        for n, w in raw.items():
            _add(acc, n, m * w / total)
        return acc
    cosa = d[:, 2]
    back = smoothstep(0.2, -0.5, cosa)
    front = smoothstep(0.1, 0.8, cosa)
    sidew = np.clip(1 - back - front, 0, 1)
    right = smoothstep(-0.05, 0.05, x)
    _add(acc, 'Pelvis', 1 - m)
    for s, ws in (('L', 1 - right), ('R', right)):
        _add(acc, 'CoatFront' + s, m * front * ws)
        k = smoothstep(0.3, 0.8, t)
        _add(acc, 'CoatBack%s1' % s, m * back * ws * (1 - k))
        _add(acc, 'CoatBack%s2' % s, m * back * ws * k)
    _add(acc, 'SkirtL', m * sidew * (1 - right))
    _add(acc, 'SkirtR', m * sidew * right)
    return acc


def coat_weights(V):
    """Upper coat follows the torso/arms, the skirts of the coat swing."""
    upper = torso_weights(V)
    low = lower_cloth(V, 'Coat')
    y = V[:, 1]
    mix = smoothstep(0.93, 0.86, y)      # 0 above the hip, 1 below
    acc = {}
    for n, w in upper.items():
        _add(acc, n, w * (1 - mix))
    for n, w in low.items():
        _add(acc, n, w * mix)
    return acc


def leg_weights(V, side):
    y = V[:, 1]
    acc = {}
    ch = chain_weights(V, ['Thigh' + side, 'Calf' + side, 'Foot' + side], 0.050)
    hip = smoothstep(0.80, 0.93, y)
    for n, w in ch.items():
        total = sum(ch.values())
        _add(acc, n, (w / total) * (1 - hip))
    _add(acc, 'Pelvis', hip * 0.55)
    _add(acc, 'Thigh' + side, hip * 0.45)
    return acc


def boot_weights(V, side):
    """Shaft follows the calf, the foot follows the ankle, the toe box bends at the toe joint (clean, short blend)."""
    ankle = R.HEAD['Foot' + side]
    z = V[:, 2] - ankle[2]
    y = V[:, 1]
    toe = smoothstep(0.108, 0.140, z) * smoothstep(0.13, 0.08, y)
    calf = smoothstep(0.14, 0.25, y)
    return {'Calf' + side: (1 - toe) * calf, 'Foot' + side: (1 - toe) * (1 - calf), 'Toe' + side: toe}


def arm_weights(V, side, sleeve):
    names = ['UpperArm' + side, 'Forearm' + side] + ([] if sleeve else ['Hand' + side])
    ch = chain_weights(V, names, 0.060 if sleeve else 0.040)
    total = sum(ch.values())
    acc = {n: w / total for n, w in ch.items()}
    top = smoothstep(1.24, 1.33, V[:, 1]) * (1 if sleeve else 0)
    for n in list(acc):
        acc[n] = acc[n] * (1 - top * 0.4)
    _add(acc, 'Clavicle' + side, top * 0.4)
    return acc


def ponytail_weights(V):
    y = V[:, 1]
    knots = [(n, float(R.HEAD[n][1])) for n in ('Ponytail1', 'Ponytail2', 'Ponytail3', 'Ponytail4')]
    acc = {}
    ws = {n: np.zeros(len(V)) for n, _ in knots}
    for (n0, y0), (n1, y1) in zip(knots[:-1], knots[1:]):
        t = np.clip((y0 - y) / (y0 - y1), 0, 1)
        inside = (y <= y0) & (y > y1)
        ws[n0] += np.where(inside, 1 - t, 0)
        ws[n1] += np.where(inside, t, 0)
    ws['Ponytail1'] += np.where(y > knots[0][1], 1, 0)
    ws['Ponytail4'] += np.where(y <= knots[-1][1], 1, 0)
    top = knots[0][1]
    head = smoothstep(top - 0.015, top + 0.025, y)
    for n, w in ws.items():
        _add(acc, n, w * (1 - head))
    _add(acc, 'Head', head)
    return acc


def side_lock_weights(V, side, s):
    k = smoothstep(0.12, 0.65, s)
    return {'Head': 1 - k, 'SideLock' + side: k}


def scarf_tail_weights(V, tag, top_y, bottom_y):
    s = np.clip((top_y - V[:, 1]) / (top_y - bottom_y), 0, 1)
    a = smoothstep(0.0, 0.14, s)
    b = smoothstep(0.30, 0.75, s)
    return {'Chest': 1 - a, 'Scarf%s1' % tag: a * (1 - b), 'Scarf%s2' % tag: a * b}


def part_weights(part):
    z, V = part.zone, part.V
    if z in ('Head', 'HairCap', 'Bangs'):
        return {'Head': np.ones(len(V))}
    if z == 'Neck':
        y = V[:, 1]
        head = smoothstep(1.40, 1.44, y)
        chest = smoothstep(1.35, 1.32, y)
        return {'Head': head, 'Chest': chest * 0.6, 'Neck': 1 - head - chest * 0.6}
    if z.startswith('Side') and z[-1] in 'LR':
        return side_lock_weights(V, z[-1], part.P['s'])
    if z == 'Ponytail':
        return ponytail_weights(V)
    if z in ('Torso', 'Strap'):
        acc = torso_weights(V)
        if z == 'Strap':
            sat = smoothstep(0.18, 0.225, V[:, 0]) * smoothstep(1.02, 0.95, V[:, 1])
            acc = {n: w * (1 - sat * 0.85) for n, w in acc.items()}
            _add(acc, 'Satchel', sat * 0.85)
        return acc
    if z == 'Coat':
        return coat_weights(V)
    if z in ('Skirt', 'Petticoat'):
        return lower_cloth(V, z)
    if z in ('LegL', 'LegR'):
        return leg_weights(V, z[-1])
    if z in ('BootL', 'BootR'):
        return boot_weights(V, z[-1])
    if z in ('ArmL', 'ArmR'):
        return arm_weights(V, z[-1], sleeve=part.name.startswith('Sleeve'))
    if z in ('HandL', 'HandR'):
        return {'Hand' + z[-1]: np.ones(len(V))}
    if z == 'ScarfCollar':
        h = smoothstep(1.40, 1.44, V[:, 1])
        return {'Neck': 0.7 - 0.2 * h, 'Chest': 0.3 - 0.3 * h, 'Head': 0.5 * h}
    if z == 'ScarfF':
        return scarf_tail_weights(V, 'F', 1.376, 0.95)
    if z == 'ScarfB':
        return scarf_tail_weights(V, 'B', 1.372, 0.80)
    if z == 'Satchel':
        return {'Satchel': np.ones(len(V))}
    raise KeyError('No weight rule for zone ' + z + ' (part ' + part.name + ')')


def weights_for(part):
    acc = part_weights(part)
    return _normalise(acc)
