"""Rest skeleton of the caretaker (Unity metres). Every bone has an identity rest rotation (pure joint frames), so a local
rotation in an animation clip is exactly the delta from the rest pose."""
import numpy as np

# name, parent, rest head position (world, Unity)
BONES = [
    ('Root', None, (0, 0, 0)),
    ('Pelvis', 'Root', (0, 0.86, 0)),
    ('Spine', 'Pelvis', (0, 0.985, 0)),
    ('Chest', 'Spine', (0, 1.14, 0)),
    ('Neck', 'Chest', (0, 1.36, -0.004)),
    ('Head', 'Neck', (0, 1.42, 0)),
    ('Ponytail1', 'Head', (0, 1.598, -0.100)),
    ('Ponytail2', 'Ponytail1', (0, 1.50, -0.150)),
    ('Ponytail3', 'Ponytail2', (0, 1.38, -0.158)),
    ('Ponytail4', 'Ponytail3', (0, 1.26, -0.148)),
    ('SideLockL', 'Head', (-0.092, 1.560, 0.0)),
    ('SideLockR', 'Head', (0.092, 1.560, 0.0)),
    ('ClavicleL', 'Chest', (-0.050, 1.335, 0.0)),
    ('UpperArmL', 'ClavicleL', (-0.170, 1.312, 0.0)),
    ('ForearmL', 'UpperArmL', (-0.252, 1.045, 0.010)),
    ('HandL', 'ForearmL', (-0.300, 0.800, 0.040)),
    ('ClavicleR', 'Chest', (0.050, 1.335, 0.0)),
    ('UpperArmR', 'ClavicleR', (0.170, 1.312, 0.0)),
    ('ForearmR', 'UpperArmR', (0.252, 1.045, 0.010)),
    ('HandR', 'ForearmR', (0.300, 0.800, 0.040)),
    ('ThighL', 'Pelvis', (-0.092, 0.86, 0.0)),
    ('CalfL', 'ThighL', (-0.099, 0.46, 0.012)),
    ('FootL', 'CalfL', (-0.105, 0.09, -0.004)),
    ('ToeL', 'FootL', (-0.105, 0.036, 0.125)),
    ('ThighR', 'Pelvis', (0.092, 0.86, 0.0)),
    ('CalfR', 'ThighR', (0.099, 0.46, 0.012)),
    ('FootR', 'CalfR', (0.105, 0.09, -0.004)),
    ('ToeR', 'FootR', (0.105, 0.036, 0.125)),
    ('SkirtF', 'Pelvis', (0.0, 0.99, 0.095)),
    ('SkirtB', 'Pelvis', (0.0, 0.99, -0.095)),
    ('SkirtL', 'Pelvis', (-0.125, 0.99, 0.0)),
    ('SkirtR', 'Pelvis', (0.125, 0.99, 0.0)),
    ('CoatBackL1', 'Pelvis', (-0.075, 0.90, -0.105)),
    ('CoatBackL2', 'CoatBackL1', (-0.095, 0.66, -0.185)),
    ('CoatBackR1', 'Pelvis', (0.075, 0.90, -0.105)),
    ('CoatBackR2', 'CoatBackR1', (0.095, 0.66, -0.185)),
    ('CoatFrontL', 'Pelvis', (-0.150, 0.92, 0.085)),
    ('CoatFrontR', 'Pelvis', (0.150, 0.92, 0.085)),
    ('ScarfF1', 'Chest', (-0.050, 1.365, 0.078)),
    ('ScarfF2', 'ScarfF1', (-0.056, 1.20, 0.124)),
    ('ScarfB1', 'Chest', (0.052, 1.365, -0.076)),
    ('ScarfB2', 'ScarfB1', (0.060, 1.17, -0.124)),
    ('Satchel', 'Pelvis', (0.200, 0.935, 0.0)),
]
HEAD_SCALE, HEAD_PIVOT = 1.08, (0.0, 1.42, 0.0)
_SCALED = ('Ponytail1', 'Ponytail2', 'Ponytail3', 'Ponytail4', 'SideLockL', 'SideLockR')
BONES = [(n, p, tuple(HEAD_PIVOT[i] + (h[i] - HEAD_PIVOT[i]) * HEAD_SCALE for i in range(3)) if n in _SCALED else h) for n, p, h in BONES]
NAMES = [b[0] for b in BONES]
INDEX = {n: i for i, n in enumerate(NAMES)}
PARENT = {b[0]: b[1] for b in BONES}
HEAD = {b[0]: np.array(b[2], float) for b in BONES}
CHILDREN = {n: [] for n in NAMES}
for n, p, _ in BONES:
    if p:
        CHILDREN[p].append(n)


def tail(name):
    """Segment end used for distance-based skin weights and the Blender armature."""
    kids = CHILDREN[name]
    if kids:
        return HEAD[kids[0]]
    extra = {
        'Head': (0, 1.66, 0), 'HandL': (-0.315, 0.70, 0.05), 'HandR': (0.315, 0.70, 0.05),
        'ToeL': (-0.105, 0.03, 0.19), 'ToeR': (0.105, 0.03, 0.19),
        'Ponytail4': (0, 1.12, -0.140), 'SideLockL': (-0.082, 1.42, 0.07), 'SideLockR': (0.082, 1.42, 0.07),
        'SkirtF': (0, 0.64, 0.24), 'SkirtB': (0, 0.60, -0.24), 'SkirtL': (-0.26, 0.62, 0), 'SkirtR': (0.26, 0.62, 0),
        'CoatBackL2': (-0.11, 0.42, -0.21), 'CoatBackR2': (0.11, 0.42, -0.21),
        'CoatFrontL': (-0.20, 0.68, 0.12), 'CoatFrontR': (0.20, 0.68, 0.12),
        'ScarfF2': (-0.06, 0.90, 0.13), 'ScarfB2': (0.064, 0.82, -0.13), 'Satchel': (0.235, 0.78, -0.01),
    }
    return np.array(extra.get(name, tuple(HEAD[name] + np.array([0, -0.08, 0]))), float)
