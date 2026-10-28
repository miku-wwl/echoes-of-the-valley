"""Front (facing the camera) and back views of the caretaker: one warped layer each, run and idle animation.

The legs never cross in these views, so the whole illustration is skinned as one layer. A swinging leg shortens (it comes towards or goes
away from the viewer) and lifts; the stance leg stays long and carries a slight hip sway. Timing comes from the same RunGait as the side
view, so the cycle, stance fraction and stride are identical in every direction.
"""
import math

import numpy as np

import gait
import rig

PAD = dict(left=60, top=40, right=60, bottom=40)

FRONT = dict(
    hipL=(250, 520), kneeL=(253, 690), ankleL=(258, 905), hipR=(318, 520), kneeR=(316, 690), ankleR=(314, 905), leg_split=285,
    pelvis=(284, 520), chest=(284, 380), neck=(283, 192),
    shoulderL=(215, 240), elbowL=(150, 350), handL=(70, 470), shoulderR=(355, 240), elbowR=(415, 350), handR=(485, 465),
    hair=[('hairL1', (210, 60), (160, 205), 52), ('hairL2', (160, 205), (125, 330), 50), ('hairR', (345, 105), (352, 200), 26)],
    ribbon=((222, 48), (226, 228), 16),
    scarf=((352, 218), (360, 380), (364, 520), 22),
    coatL=((175, 460), (90, 650), (60, 800), 48), coatR=((385, 460), (420, 640), (440, 790), 42),
    satchel=((140, 380), (130, 500), 42),
    left_arm_is='right',   # the arm on screen-left is her right arm when she faces the camera
)
BACK = dict(
    hipL=(218, 560), kneeL=(218, 705), ankleL=(218, 905), hipR=(285, 560), kneeR=(285, 705), ankleR=(288, 905), leg_split=251,
    pelvis=(252, 560), chest=(268, 380), neck=(272, 195),
    shoulderL=(190, 245), elbowL=(130, 360), handL=(55, 470), shoulderR=(360, 245), elbowR=(420, 360), handR=(490, 470),
    hair=[('pony1', (280, 55), (360, 160), 55), ('pony2', (360, 160), (420, 300), 55), ('pony3', (420, 300), (440, 380), 40)],
    ribbon=((296, 62), (300, 240), 16),
    scarf=((205, 215), (205, 360), (205, 470), 34),
    coatL=((175, 470), (140, 650), (120, 800), 50), coatR=((360, 470), (390, 640), (420, 790), 50),
    satchel=((380, 380), (390, 500), 52),
    left_arm_is='left',
)
SOLE_Y = 975.0


def skeleton(L):
    B = rig.Bone
    bones = [B('root', L['pelvis']), B('chest', L['chest'], 'root'), B('head', L['neck'], 'chest'),
             B('armL', L['shoulderL'], 'chest'), B('foreL', L['elbowL'], 'armL'),
             B('armR', L['shoulderR'], 'chest'), B('foreR', L['elbowR'], 'armR')]
    parent = 'head'
    for name, a, b, r in L['hair']:
        bones.append(B(name, a, parent if name.endswith('1') or name == 'hairR' else bones[-1].name))
    bones += [B('ribbon', L['ribbon'][0], 'head'), B('scarf1', L['scarf'][0], 'chest'), B('scarf2', L['scarf'][1], 'scarf1'),
              B('coatL1', L['coatL'][0], 'root'), B('coatL2', L['coatL'][1], 'coatL1'),
              B('coatR1', L['coatR'][0], 'root'), B('coatR2', L['coatR'][1], 'coatR1'),
              B('satchel', L['satchel'][0], 'root'),
              B('thighL', L['hipL'], 'root'), B('shinL', L['kneeL'], 'thighL'), B('footL', L['ankleL'], 'shinL'),
              B('thighR', L['hipR'], 'root'), B('shinR', L['kneeR'], 'thighR'), B('footR', L['ankleR'], 'shinR')]
    return rig.Skeleton(bones)


def weights(sk, L, h, w, ox, oy):
    P = lambda p: (p[0] + ox, p[1] + oy)
    cap = lambda a, b, r, f: rig.capsule(h, w, P(a), P(b), r, f)
    W = {b.name: np.zeros((h, w), np.float32) for b in sk.bones}
    W['root'] += 0.12
    W['chest'] += rig.band_y(h, w, L['pelvis'][1] - 60 + oy, L['pelvis'][1] - 150 + oy)
    W['head'] += cap((L['neck'][0], L['neck'][1] - 95), (L['neck'][0], L['neck'][1] - 10), 62, 16) * 1.6
    for side in 'LR':
        e, hd = np.array(L['elbow' + side], float), np.array(L['hand' + side], float)
        tip = tuple(hd + 0.32 * (hd - e))          # past the fingertips, so the whole hand moves as one piece
        W['arm' + side] += cap(L['shoulder' + side], L['elbow' + side], 34, 12) * 1.6
        W['fore' + side] += cap(L['elbow' + side], tip, 40, 18) * 2.4
    for name, a, b, r in L['hair']:
        W[name] += cap(a, b, r, 14) * 2.0
    W['ribbon'] += cap(L['ribbon'][0], L['ribbon'][1], L['ribbon'][2], 6) * 2.4
    W['scarf1'] += cap(L['scarf'][0], L['scarf'][1], L['scarf'][3], 8) * 1.8
    W['scarf2'] += cap(L['scarf'][1], L['scarf'][2], L['scarf'][3], 8) * 2.4
    for side in 'LR':
        c = L['coat' + side]
        W['coat%s1' % side] += cap(c[0], c[1], c[3], 16) * 1.4
        W['coat%s2' % side] += cap(c[1], c[2], c[3], 16) * 2.0
    s = L['satchel']
    W['satchel'] += cap(s[0], s[1], s[2], 10) * 1.6
    split = L['leg_split'] + ox
    side_mask = {'L': rig.band_x(h, w, split + 4, split - 4), 'R': rig.band_x(h, w, split - 4, split + 4)}
    for side in 'LR':
        hip, knee, ankle = L['hip' + side], L['knee' + side], L['ankle' + side]
        thigh_band = rig.band_y(h, w, knee[1] - 20 + oy, knee[1] + 20 + oy, invert=True)
        foot_band = rig.band_y(h, w, ankle[1] - 14 + oy, ankle[1] + 10 + oy)
        boot = cap((ankle[0], ankle[1]), (ankle[0], SOLE_Y + 12), 50, 10)   # the whole boot foot, toe cap and sole included
        leg = np.maximum(cap((hip[0], hip[1] + 30), (ankle[0], ankle[1]), 36, 10), boot) * side_mask[side] * rig.band_y(h, w, hip[1] - 10 + oy, hip[1] + 40 + oy)
        W['thigh' + side] += leg * thigh_band * 3.0
        W['shin' + side] += leg * (1 - thigh_band) * (1 - foot_band) * 3.0
        W['foot' + side] += leg * foot_band * 3.0
    return rig.normalise([W[b.name] for b in sk.bones])


GAIT = gait.RunGait()


def leg_swing(u):
    """0 in stance, rising to 1 in mid-swing (the leg is folded and closest to/farthest from the viewer)."""
    u %= 1.0
    if u < GAIT.stance:
        return 0.0
    s = (u - GAIT.stance) / (1 - GAIT.stance)
    return math.sin(math.pi * s) ** 1.2


def run_pose(L, phase, back):
    dy = GAIT.pelvis_dy(phase)
    # her left leg uses the side view's phase; on screen it is the right leg in the front view and the left leg in the back view
    screen = {'L': phase + (0.0 if back else 0.5), 'R': phase + (0.5 if back else 0.0)}
    pose = {'root': dict(dy=dy, dx=4.0 * math.sin(2 * math.pi * (phase + (0.0 if back else 0.5)) - 0.4), rot=1.2 * math.sin(2 * math.pi * phase))}
    for side in 'LR':
        lift = leg_swing(screen[side])
        sign = -1 if side == 'L' else 1
        pose['thigh' + side] = dict(sy=1 - 0.17 * lift, rot=sign * 2.5 * lift)
        pose['shin' + side] = dict(sy=1 - 0.07 * lift, rot=-sign * 3.5 * lift)
        pose['foot' + side] = dict(rot=sign * 6.0 * lift, dy=-10.0 * lift)
    # arms swing against the opposite leg: an arm comes forward (inward, up, elbow bent) while its own-side leg goes back
    for side in 'LR':
        forward = -math.cos(2 * math.pi * screen[side])
        inward = 1 if side == 'L' else -1   # screen-left arm turns inward with negative rotation
        pose['arm' + side] = dict(rot=-inward * (1.0 + 2.2 * forward))
        pose['fore' + side] = dict(rot=-inward * 1.5 * max(forward, 0))
    step = 2 * math.pi * 2 * phase
    pose['chest'] = dict(rot=-0.8 * math.sin(2 * math.pi * phase), dy=-1.0)
    pose['head'] = dict(rot=0.6 * math.sin(2 * math.pi * phase + 0.6))
    for name, a, b, r in L['hair']:
        pose[name] = dict(rot=3.0 * math.sin(2 * math.pi * phase - 1.3) + (2.0 if back else 0.0), dy=1.5 * math.sin(step - 1.0))
    pose['ribbon'] = dict(rot=4.0 * math.sin(2 * math.pi * phase - 1.6))
    pose['scarf1'] = dict(rot=2.5 * math.sin(2 * math.pi * phase - 1.1))
    pose['scarf2'] = dict(rot=3.5 * math.sin(2 * math.pi * phase - 1.7))
    for side, sgn in (('L', 1), ('R', -1)):
        pose['coat%s1' % side] = dict(rot=sgn * (1.5 + 2.0 * math.sin(2 * math.pi * phase - 0.8)))
        pose['coat%s2' % side] = dict(rot=sgn * (1.0 + 2.6 * math.sin(2 * math.pi * phase - 1.4)))
    pose['satchel'] = dict(rot=2.5 * math.sin(step - 0.7), dy=2.0 * math.sin(step - 0.7))
    return pose


def idle_pose(L, phase, back):
    s, c = math.sin(2 * math.pi * phase), math.cos(2 * math.pi * phase)
    pose = {'root': dict(dy=0.0), 'chest': dict(sy=1.0 + 0.007 * s, dy=-1.0 * s), 'head': dict(rot=0.5 * c, dy=-0.6 * s)}
    for side in 'LR':
        pose['arm' + side] = dict(rot=0.8 * s * (1 if side == 'L' else -1))
    for name, a, b, r in L['hair']:
        pose[name] = dict(rot=1.4 * math.sin(2 * math.pi * phase - 1.0))
    pose['ribbon'] = dict(rot=2.0 * math.sin(2 * math.pi * phase - 1.4))
    pose['scarf2'] = dict(rot=1.5 * math.sin(2 * math.pi * phase - 1.2))
    for side, sgn in (('L', 1), ('R', -1)):
        pose['coat%s2' % side] = dict(rot=sgn * 0.8 * math.sin(2 * math.pi * phase - 0.9))
    pose['satchel'] = dict(rot=0.6 * s)
    return pose


def render(view, which, frames_run=10, frames_idle=12):
    L = FRONT if which == 'front' else BACK
    back = which == 'back'
    ox, oy = PAD['left'], PAD['top']
    v = rig.premultiply(rig.pad(view, PAD['left'], PAD['top'], PAD['right'], PAD['bottom']))
    h, w = v.shape[:2]
    sk = skeleton(L)
    shifted = rig.Skeleton([rig.Bone(b.name, b.pivot + (ox, oy), b.parent) for b in sk.bones])
    wts = weights(sk, L, h, w, ox, oy)
    out = {'run': [], 'idle': []}
    for kind, n, fn in (('run', frames_run, run_pose), ('idle', frames_idle, idle_pose)):
        for i in range(n):
            out[kind].append(rig.warp(v, wts, shifted.matrices(fn(L, i / n, back))))
    return out, dict(canvas=[w, h], ground_y=SOLE_Y + oy, pivot_x=L['pelvis'][0] + ox)
