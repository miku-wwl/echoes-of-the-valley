"""Forward kinematics and linear-blend skinning for the caretaker rig (numpy). Used by the animation generator, its
validation (foot planting, ground contact) and the Blender preview poses. Unity performs the same maths at runtime."""
import numpy as np
import rig_spec as R

NAMES = R.NAMES
N = len(NAMES)
PARENT = np.array([R.INDEX[R.PARENT[n]] if R.PARENT[n] else -1 for n in NAMES])
REST = np.array([R.HEAD[n] for n in NAMES])
OFFSET = REST - np.where(PARENT[:, None] >= 0, REST[PARENT], 0)


def identity_pose():
    return np.tile(np.eye(3), (N, 1, 1))


def forward(local_rot, pelvis_pos=None, root_pos=None):
    """local_rot (N,3,3) -> world rotations (N,3,3) and positions (N,3). Rest local rotations are identity.
    pelvis_pos: optional extra offset of the pelvis (parent space); root_pos: offset of the root."""
    wr = np.zeros((N, 3, 3))
    wp = np.zeros((N, 3))
    for i in range(N):
        p = PARENT[i]
        off = OFFSET[i].copy()
        if NAMES[i] == 'Pelvis' and pelvis_pos is not None:
            off = off + pelvis_pos
        if p < 0:
            wr[i] = local_rot[i]
            wp[i] = REST[i] + (root_pos if root_pos is not None else 0)
        else:
            wr[i] = wr[p] @ local_rot[i]
            wp[i] = wp[p] + wr[p] @ off
    return wr, wp


def skin(V, idx, w, wr, wp):
    """Linear-blend skinning of rest vertices V (n,3) with (n,4) bone indices and weights."""
    out = np.zeros_like(V)
    for k in range(4):
        b = idx[:, k]
        rel = V - REST[b]
        out += w[:, k:k + 1] * (np.einsum('nij,nj->ni', wr[b], rel) + wp[b])
    return out


def mat_to_quat(m):
    """3x3 rotation -> (x, y, z, w)."""
    t = np.trace(m)
    if t > 0:
        s = np.sqrt(t + 1.0) * 2
        q = np.array([(m[2, 1] - m[1, 2]) / s, (m[0, 2] - m[2, 0]) / s, (m[1, 0] - m[0, 1]) / s, 0.25 * s])
    elif m[0, 0] > m[1, 1] and m[0, 0] > m[2, 2]:
        s = np.sqrt(1.0 + m[0, 0] - m[1, 1] - m[2, 2]) * 2
        q = np.array([0.25 * s, (m[0, 1] + m[1, 0]) / s, (m[0, 2] + m[2, 0]) / s, (m[2, 1] - m[1, 2]) / s])
    elif m[1, 1] > m[2, 2]:
        s = np.sqrt(1.0 + m[1, 1] - m[0, 0] - m[2, 2]) * 2
        q = np.array([(m[0, 1] + m[1, 0]) / s, 0.25 * s, (m[1, 2] + m[2, 1]) / s, (m[0, 2] - m[2, 0]) / s])
    else:
        s = np.sqrt(1.0 + m[2, 2] - m[0, 0] - m[1, 1]) * 2
        q = np.array([(m[0, 2] + m[2, 0]) / s, (m[1, 2] + m[2, 1]) / s, 0.25 * s, (m[1, 0] - m[0, 1]) / s])
    return q / np.linalg.norm(q)


def rotx(a):
    c, s = np.cos(a), np.sin(a)
    return np.array([[1, 0, 0], [0, c, -s], [0, s, c]])


def roty(a):
    c, s = np.cos(a), np.sin(a)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])


def rotz(a):
    c, s = np.cos(a), np.sin(a)
    return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])


def rot_between(a, b):
    a = a / np.linalg.norm(a)
    b = b / np.linalg.norm(b)
    v = np.cross(a, b)
    c = float(a @ b)
    if c < -0.999999:
        axis = np.cross(a, [1, 0, 0])
        if np.linalg.norm(axis) < 1e-6:
            axis = np.cross(a, [0, 1, 0])
        axis /= np.linalg.norm(axis)
        return 2 * np.outer(axis, axis) - np.eye(3)
    vx = np.array([[0, -v[2], v[1]], [v[2], 0, -v[0]], [-v[1], v[0], 0]])
    return np.eye(3) + vx + vx @ vx * (1.0 / (1.0 + c))
