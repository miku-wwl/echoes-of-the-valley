"""Procedural animation clips for the caretaker: Idle, Walk (a light jog matched to the unchanged 4.5 m/s controller),
TurnLeft and TurnRight (upper body only). Feet are planted by IK: during stance the heel, flat sole and ball of the foot
stay fixed in the ground frame, so there is no foot sliding at the controller speed. Everything is original and deterministic.

Conventions: Unity metres, +Z forward. A rotation about +X by a positive angle tilts a downward-hanging limb BACKWARD and
tips a forward vector DOWN; so arms swing forward with negative X rotation and "toe down" is positive.
"""
import math
import numpy as np
import rig_spec as R
from kit import smoothstep, lerp
from skinning import (N, NAMES, identity_pose, forward, rotx, roty, rotz, rot_between, mat_to_quat)

I3 = np.eye(3)
D = math.radians

# ----------------------------------------------------------------------------- gait constants
CYCLE = 0.64                       # seconds per full cycle (two steps)
SPEED = 4.5                        # m/s, the controller speed (unchanged)
STRIDE = SPEED * CYCLE             # metres per cycle; the animation phase is driven by travelled distance
STANCE = 0.225                     # fraction of the cycle each foot is on the ground
HEEL = (-0.09, -0.065)             # ankle -> heel contact point (y, z) at foot pitch 0
BALL = (-0.09, 0.129)              # ankle -> sole under the toe joint (the ball of the foot)
TH_TD, TH_MAX = D(-12), D(34)      # foot pitch at heel strike / toe-off
ZH0 = 0.205                        # heel contact z (ahead of the pelvis) at touchdown
PELVIS_BASE, PELVIS_DROP = 0.822, 0.052
FOOT_X = 0.105

L1 = float(np.linalg.norm(R.HEAD['CalfR'] - R.HEAD['ThighR']))
L2 = float(np.linalg.norm(R.HEAD['FootR'] - R.HEAD['CalfR']))
D_THIGH = (R.HEAD['CalfR'] - R.HEAD['ThighR']) / L1
D_CALF = (R.HEAD['FootR'] - R.HEAD['CalfR']) / L2
ANKLE_REST_Y = float(R.HEAD['FootR'][1])


def _yz(v, th):
    y, z = v
    return (y * math.cos(th) - z * math.sin(th), y * math.sin(th) + z * math.cos(th))


def foot_state(phi):
    """Foot target for a leg at cycle phase phi (0 = heel strike): (ankle z, ankle y, foot pitch, stance flag).
    z is measured from the pelvis ground point in the player frame, so planted points move back at SPEED."""
    c = STANCE
    tau = phi * CYCLE
    if phi < c:
        p = phi / c
        p1, p2 = 0.22, 0.56
        if p < p1:
            th = TH_TD * (1 - float(smoothstep(0, 1, p / p1)))
            hy, hz = _yz(HEEL, th)
            return ZH0 - SPEED * tau - hz, -hy, th, True
        if p < p2:
            return ZH0 + 0.065 - SPEED * tau, ANKLE_REST_Y, 0.0, True
        q = (p - p2) / (1 - p2)
        th = TH_MAX * float(smoothstep(0, 1, q))
        by, bz = _yz(BALL, th)
        return ZH0 + 0.065 + 0.129 - SPEED * tau - bz, -by + 0.012 * float(smoothstep(0, 1, q)), th, True
    z0, y0, th0, _ = foot_state(c - 1e-6)
    z1, y1, th1, _ = foot_state(0.0)
    s = (phi - c) / (1 - c)
    dur = (1 - c) * CYCLE
    # Hermite in z with ground-speed-matching end tangents so the foot never "stamps" or snaps at touchdown
    m0, m1 = -0.35 * SPEED * dur, -0.60 * SPEED * dur
    h00, h10, h01, h11 = 2 * s ** 3 - 3 * s ** 2 + 1, s ** 3 - 2 * s ** 2 + s, -2 * s ** 3 + 3 * s ** 2, s ** 3 - s ** 2
    z = h00 * z0 + h10 * m0 + h01 * z1 + h11 * m1
    y = lerp(y0, y1, s) + 0.165 * math.sin(math.pi * s ** 0.85) ** 1.25
    th = lerp(th0, th1, float(smoothstep(0.05, 0.95, s)))
    return z, y, th, False


def solve_leg(hip, target, pole):
    d = target - hip
    dist = float(np.linalg.norm(d))
    u = d / dist
    reach = L1 + L2 - 1e-4
    dc = min(max(dist, abs(L1 - L2) + 1e-4), reach)
    cos_a = (L1 * L1 + dc * dc - L2 * L2) / (2 * L1 * dc)
    a = math.acos(max(-1.0, min(1.0, cos_a)))
    perp = pole - u * float(pole @ u)
    perp /= np.linalg.norm(perp)
    knee = hip + L1 * (math.cos(a) * u + math.sin(a) * perp)
    ankle = hip + dc * u
    return knee, ankle, max(0.0, dist - reach)


def wave(phi, k, phase):
    return math.sin(math.tau * (k * phi + phase))


class Frame:
    def __init__(self):
        self.rot = identity_pose()
        self.pelvis = np.zeros(3)
        self.shortfall = 0.0
        self.stance = {}

    def set(self, name, m):
        self.rot[R.INDEX[name]] = m

    def add(self, name, m):
        i = R.INDEX[name]
        self.rot[i] = self.rot[i] @ m


def pelvis_height(phi):
    c = STANCE
    return PELVIS_BASE - PELVIS_DROP * (0.5 + 0.5 * math.cos(4 * math.pi * (phi - c / 2)))


def build_run(phi):
    f = Frame()
    c = STANCE
    cyc = math.tau * phi
    # ---- pelvis path, kept low enough that planted feet are always reachable (so they never slide)
    yp = pelvis_height(phi)
    xp = 0.012 * math.cos(math.tau * (phi - c / 2))
    yaw = -D(6.0) * math.cos(cyc)
    roll = D(3.0) * math.cos(math.tau * (phi - c / 2))
    pitch = D(3.0)
    Rp = roty(yaw) @ rotz(roll) @ rotx(pitch)
    for _ in range(2):
        need = []
        for side, leg_phi in (('R', phi), ('L', (phi + 0.5) % 1.0)):
            zt, yt, th, stance = foot_state(leg_phi)
            if stance:
                sx = 1 if side == 'R' else -1
                hip_x = sx * 0.092
                dxz = zt - 0.0
                dx = sx * FOOT_X - (hip_x + xp)
                rem = (L1 + L2 - 0.004) ** 2 - dxz ** 2 - dx ** 2
                need.append(yt + math.sqrt(max(rem, 0.0)))
        if need and yp > min(need):
            yp = min(need)
    f.pelvis = np.array([xp, yp - R.HEAD['Pelvis'][1], 0.0])
    f.set('Pelvis', Rp)
    f.set('Spine', rotx(D(3.5)) @ roty(D(3.5) * math.cos(cyc)) @ rotz(-roll * 0.5))
    f.set('Chest', rotx(D(3.5)) @ roty(D(6.5) * math.cos(cyc)) @ rotz(-roll * 0.7))
    f.set('Neck', rotx(D(-2)))
    f.set('Head', rotx(D(-6.5) + D(1.2) * math.cos(4 * math.pi * (phi - c / 2) + 1.0)) @ roty(-D(5.5) * math.cos(cyc)) @ rotz(roll * 0.6))
    # ---- arms: swing opposite to the legs, elbows bent
    for side, ph in (('L', 0.0), ('R', 0.5)):
        sx = -1 if side == 'L' else 1
        swing = math.cos(math.tau * (phi - ph))
        shoulder = -(D(5) + D(32) * swing)
        elbow = -(D(78) + D(16) * swing)
        f.set('Clavicle' + side, rotz(sx * D(2.0)) @ rotx(-D(2.5) * swing))
        f.set('UpperArm' + side, rotz(sx * D(10.0)) @ rotx(shoulder) @ roty(sx * D(-6.0) * swing))
        f.set('Forearm' + side, rotx(elbow) @ roty(sx * D(8.0)))
        f.set('Hand' + side, rotx(D(-8) - D(10) * swing) @ rotz(sx * D(-4.0)))
    # ---- legs: IK to planted / swinging feet
    wr, wp = forward(f.rot, f.pelvis)
    thigh_flex = {}
    for side, leg_phi in (('R', phi), ('L', (phi + 0.5) % 1.0)):
        sx = 1 if side == 'R' else -1
        zt, yt, th, stance = foot_state(leg_phi)
        swing_out = 0.0 if stance else 0.024 * math.sin(math.pi * (leg_phi - c) / (1 - c))
        target = np.array([sx * (FOOT_X + swing_out), yt, zt])
        hip = wp[R.INDEX['Thigh' + side]]
        pole = np.array([sx * 0.18, 0.0, 1.0])
        knee, ankle, short = solve_leg(hip, target, pole)
        f.shortfall = max(f.shortfall, short if stance else 0.0)
        f.stance[side] = stance
        pel = wr[R.INDEX['Pelvis']]
        thigh_w = rot_between(D_THIGH, knee - hip) @ roty(sx * D(4.0))
        calf_w = rot_between(D_CALF, ankle - knee)
        foot_w = rotx(th) @ roty(sx * D(7.0))
        f.set('Thigh' + side, pel.T @ thigh_w)
        f.set('Calf' + side, thigh_w.T @ calf_w)
        f.set('Foot' + side, calf_w.T @ foot_w)
        toe = -1.0 * th if th > 0 else 0.0
        f.set('Toe' + side, rotx(toe))
        thigh_flex[side] = math.atan2(-(knee - hip)[2], -(knee - hip)[1])   # + = forward
    # ---- secondary motion: travelling waves with a lag per link
    for i, name in enumerate(('Ponytail1', 'Ponytail2', 'Ponytail3', 'Ponytail4')):
        lag = 0.07 * i
        f.set(name, rotx(D(8 if i == 0 else 3.5) + D(3.0 + 2.0 * i) * wave(phi, 2, 0.55 - lag)) @ rotz(D(2.0 + 1.4 * i) * wave(phi, 1, 0.20 - lag)) @ roty(D(2 + i) * wave(phi, 1, 0.45 - lag)))
    for side, sx in (('L', -1), ('R', 1)):
        f.set('SideLock' + side, rotx(D(7) + D(3.5) * wave(phi, 2, 0.60)) @ rotz(sx * D(5.0 + 3 * wave(phi, 1, 0.3))))
        n = 'CoatBack%s' % side
        f.set(n + '1', rotx(D(13) + D(5.0) * wave(phi, 2, 0.40)) @ rotz(sx * D(3.5) * wave(phi, 1, 0.1)))
        f.set(n + '2', rotx(D(9) + D(7.0) * wave(phi, 2, 0.50)) @ rotz(sx * D(5.0) * wave(phi, 1, 0.2)))
        f.set('CoatFront' + side, rotx(D(6) + D(4.0) * wave(phi, 2, 0.45)) @ rotz(-sx * D(2.0)))
    f.set('ScarfF1', rotx(D(3) + D(2.0) * wave(phi, 2, 0.5)))
    f.set('ScarfF2', rotx(D(2) + D(3.0) * wave(phi, 2, 0.57)) @ rotz(D(2.0) * wave(phi, 1, 0.3)))
    f.set('ScarfB1', rotx(D(10) + D(4.0) * wave(phi, 2, 0.45)) @ rotz(D(3.0) * wave(phi, 1, 0.1)))
    f.set('ScarfB2', rotx(D(7) + D(6.0) * wave(phi, 2, 0.55)) @ rotz(D(5.0) * wave(phi, 1, 0.22)))
    f.set('Satchel', rotx(D(3.0) * wave(phi, 2, 0.35)) @ rotz(D(4.5) * wave(phi, 1, 0.12)))
    # skirt follows the leading and trailing thighs; sides flutter
    lead = max(thigh_flex.values())
    trail = min(thigh_flex.values())
    f.set('SkirtF', rotx(-0.55 * max(lead, 0.0)))
    f.set('SkirtB', rotx(-0.45 * min(trail, 0.0) * 1.0))
    f.set('SkirtL', rotz(D(-3) + D(2.5) * wave(phi, 2, 0.2)) @ rotx(-0.25 * thigh_flex['L']))
    f.set('SkirtR', rotz(D(3) - D(2.5) * wave(phi, 2, 0.2)) @ rotx(-0.25 * thigh_flex['R']))
    return f


def build_idle(t):
    f = Frame()
    ph = t / 2.4
    breathe = math.sin(math.tau * ph)
    sway = math.sin(math.tau * ph * 0.5)
    f.pelvis = np.array([0.004 * sway, -0.002 * breathe, 0.0])
    f.set('Pelvis', rotz(D(1.1) * sway) @ roty(D(1.0) * sway))
    f.set('Spine', rotx(D(0.8) * breathe) @ rotz(-D(0.7) * sway))
    f.set('Chest', rotx(D(1.1) * breathe) @ roty(-D(1.0) * sway))
    f.set('Neck', rotx(D(-0.6) * breathe))
    f.set('Head', rotx(D(0.8) * math.sin(math.tau * ph * 0.5 + 0.8)) @ roty(D(2.2) * math.sin(math.tau * ph * 0.5 + 2.0)) @ rotz(D(0.8) * sway))
    for side, sx in (('L', -1), ('R', 1)):
        f.set('Clavicle' + side, rotz(sx * D(1.4) * breathe))
        f.set('UpperArm' + side, rotz(sx * D(3.0)) @ rotx(D(-3) + D(1.6) * math.sin(math.tau * ph + (0 if sx < 0 else 0.6))))
        f.set('Forearm' + side, rotx(D(-14) + D(2.0) * math.sin(math.tau * ph + 0.4)))
        f.set('Hand' + side, rotx(D(-6)) @ rotz(sx * D(-3)))
        f.set('Thigh' + side, rotz(sx * D(-1.2) * sway) @ rotx(D(0.8) * sway * sx))
        f.set('Calf' + side, rotx(D(0.8)))
        f.set('SideLock' + side, rotx(D(1.5) * math.sin(math.tau * ph + 1.0)))
        f.set('CoatBack%s1' % side, rotx(D(1.8) * math.sin(math.tau * ph + 0.5)))
        f.set('CoatBack%s2' % side, rotx(D(2.6) * math.sin(math.tau * ph + 0.9)))
        f.set('CoatFront' + side, rotx(D(1.2) * math.sin(math.tau * ph + 0.3)))
        f.set('Skirt' + side, rotz(sx * D(0.6) * breathe))
    for i, name in enumerate(('Ponytail1', 'Ponytail2', 'Ponytail3', 'Ponytail4')):
        f.set(name, rotx(D(1.6 + 1.2 * i) * math.sin(math.tau * ph + 0.5 + 0.35 * i)) @ rotz(D(1.4 + i) * math.sin(math.tau * ph * 0.5 + 0.2 * i)))
    f.set('ScarfF1', rotx(D(1.0) * math.sin(math.tau * ph + 0.3)))
    f.set('ScarfF2', rotx(D(1.6) * math.sin(math.tau * ph + 0.7)))
    f.set('ScarfB1', rotx(D(2.0) * math.sin(math.tau * ph + 0.2)))
    f.set('ScarfB2', rotx(D(3.0) * math.sin(math.tau * ph + 0.6)))
    f.set('SkirtF', rotx(D(0.6) * breathe))
    f.set('SkirtB', rotx(D(0.6) * breathe))
    f.set('Satchel', rotz(D(1.0) * sway))
    return f


def build_turn(t, direction):
    """Upper-body lean and look for a quick turn (direction +1 = left, -1 = right). Blended over idle/walk on a layer."""
    f = Frame()
    ph = t / 0.9
    ease = math.sin(math.pi * ph) ** 1.3
    lean = direction                         # left turn leans/bank left: head toward -X is a positive Z rotation
    f.set('Spine', rotz(D(3.0) * ease * lean) @ roty(-D(5) * ease * lean))
    f.set('Chest', rotz(D(4.0) * ease * lean) @ roty(-D(8) * ease * lean))
    f.set('Neck', roty(-D(8) * ease * lean))
    f.set('Head', roty(-D(16) * ease * lean) @ rotz(D(2.0) * ease * lean))
    for side, sx in (('L', -1), ('R', 1)):
        out = (sx * lean < 0)                # the arm on the outside of the turn swings away
        f.set('UpperArm' + side, rotz(sx * D(10.0 + (9.0 * ease if out else -4.0 * ease))) @ rotx(D(-3) - D(8) * ease * (0 if out else 1)))
        f.set('Forearm' + side, rotx(D(-14) - D(10) * ease))
    for i, name in enumerate(('Ponytail1', 'Ponytail2', 'Ponytail3', 'Ponytail4')):
        f.set(name, rotz(-lean * D(6 + 5 * i) * ease) @ rotx(D(3 + 2 * i) * ease))
    f.set('SideLockL', rotz(-lean * D(6) * ease))
    f.set('SideLockR', rotz(-lean * D(6) * ease))
    f.set('ScarfB1', rotz(-lean * D(7) * ease))
    f.set('ScarfB2', rotz(-lean * D(11) * ease) @ rotx(D(5) * ease))
    f.set('ScarfF1', rotz(-lean * D(3) * ease))
    f.set('ScarfF2', rotz(-lean * D(5) * ease))
    return f


UPPER_BODY = ['Spine', 'Chest', 'Neck', 'Head', 'Ponytail1', 'Ponytail2', 'Ponytail3', 'Ponytail4', 'SideLockL', 'SideLockR',
              'ClavicleL', 'UpperArmL', 'ForearmL', 'HandL', 'ClavicleR', 'UpperArmR', 'ForearmR', 'HandR',
              'ScarfF1', 'ScarfF2', 'ScarfB1', 'ScarfB2']


def sample_clip(kind):
    """Returns dict(name, length, fps, frames, bones, rot (F, B, 3, 3), pelvis (F, 3), shortfall)."""
    if kind == 'Walk':
        length, fps = CYCLE, 60
        make = lambda t: build_run(t / CYCLE)
        bones = NAMES
    elif kind == 'Idle':
        length, fps = 2.4, 30
        make = build_idle
        bones = NAMES
    else:
        length, fps = 0.9, 30
        d = 1 if kind == 'TurnLeft' else -1
        make = lambda t: build_turn(t, d)
        bones = NAMES            # every bone is keyed (identity below the spine); the runtime layer mixes only the upper body
    count = int(round(length * fps))
    fps = count / length          # the last key lands exactly on `length` (one cycle = STRIDE metres at SPEED)
    frames = [make(i * length / count) for i in range(count + 1)]
    idx = [R.INDEX[b] for b in bones]
    rot = np.stack([fr.rot[idx] for fr in frames])
    pelvis = np.stack([fr.pelvis for fr in frames])
    return dict(name=kind, length=length, fps=fps, bones=list(bones), rot=rot, pelvis=pelvis, frames=frames,
                shortfall=max(fr.shortfall for fr in frames))


def all_clips():
    return [sample_clip(k) for k in ('Idle', 'Walk', 'TurnLeft', 'TurnRight')]
