"""Side-view running gait for the cut-out leg: the stance foot is planted by two-bone IK, the swing foot follows a Bezier arc.

Coordinates are hero_side.png pixels (y down, character faces screen-left). The planted heel/ball moves backward at a constant rate during
stance, which is exactly the ground sliding under a body moving forward at constant speed; the cycle length in metres is therefore
travel_px / px_per_m / stance_fraction (see stride_m), and the game advances the cycle by distance / stride so the foot does not skate.
"""
import math

HIP, KNEE, ANKLE = (115.0, 515.0), (112.0, 700.0), (122.0, 905.0)
HEEL, BALL = (160.0, 975.0), (30.0, 968.0)   # BALL = the boot's toe tip (a rigid boot rolls onto it)
GROUND = 975.0
LT = math.dist(HIP, KNEE)
LS = math.dist(KNEE, ANKLE)


def rot(v, deg):
    c, s = math.cos(math.radians(deg)), math.sin(math.radians(deg))
    return (c * v[0] - s * v[1], s * v[0] + c * v[1])


def add(a, b):
    return (a[0] + b[0], a[1] + b[1])


def sub(a, b):
    return (a[0] - b[0], a[1] - b[1])


def angle_from_to(u, v):
    """Degrees d such that rot(u, d) is parallel to v (rig.R uses the same convention)."""
    d = math.degrees(math.atan2(v[1], v[0]) - math.atan2(u[1], u[0]))
    return (d + 180.0) % 360.0 - 180.0


def smooth(t):
    t = min(max(t, 0.0), 1.0)
    return t * t * (3 - 2 * t)


def keys_lerp(keys, s):
    """Piecewise smoothstep through (s, value) keys on [0, 1]."""
    for (s0, v0), (s1, v1) in zip(keys, keys[1:]):
        if s <= s1:
            return v0 + (v1 - v0) * smooth((s - s0) / (s1 - s0))
    return keys[-1][1]


def ik(hip, target):
    """Two-bone IK with the knee in front (screen-left). Returns (knee, reached_ankle)."""
    d_vec = sub(target, hip)
    d = math.hypot(*d_vec)
    d_clamped = min(max(d, abs(LT - LS) + 1e-3), LT + LS - 1e-3)
    a = math.degrees(math.acos((LT * LT + d_clamped * d_clamped - LS * LS) / (2 * LT * d_clamped)))
    unit = (d_vec[0] / d, d_vec[1] / d)
    k1 = add(hip, rot((unit[0] * LT, unit[1] * LT), a))
    k2 = add(hip, rot((unit[0] * LT, unit[1] * LT), -a))
    knee = k1 if k1[0] < k2[0] else k2
    reach = add(hip, (unit[0] * d_clamped, unit[1] * d_clamped))
    return knee, reach


class RunGait:
    def __init__(self, stance=0.20, heel_ahead=96.0, travel=330.0, bob_base=18.0, bob_amp=11.0):
        self.stance, self.heel_ahead, self.travel = stance, heel_ahead, travel
        self.bob_base, self.bob_amp = bob_base, bob_amp

    def pelvis_dy(self, u):
        """Down is positive: lowest at mid-stance of either foot, highest in mid-flight."""
        return self.bob_base + self.bob_amp * math.cos(2 * math.pi * 2 * (u - self.stance / 2))

    def stance_foot(self, s):
        """Ankle position and foot angle for stance progress s in [0, 1]: heel rocker, flat foot, then ball rocker."""
        alpha = keys_lerp([(0.0, 11.0), (0.22, 0.0), (0.55, 0.0), (1.0, -38.0)], s)
        heel_x = HIP[0] - self.heel_ahead + self.travel * s
        if alpha >= 0:
            pivot, pivot_rest = (heel_x, GROUND), HEEL
        else:
            pivot, pivot_rest = (heel_x - (HEEL[0] - BALL[0]), GROUND), BALL
        ankle = add(pivot, rot(sub(ANKLE, pivot_rest), alpha))
        return ankle, alpha

    def foot_at(self, u):
        u %= 1.0
        if u < self.stance:
            return self.stance_foot(u / self.stance) + (True,)
        s = (u - self.stance) / (1 - self.stance)
        a_to, al_to = self.stance_foot(1.0)
        a_c, al_c = self.stance_foot(0.0)
        p0, p3 = a_to, a_c
        p1 = add(a_to, (24.0, -112.0))
        p2 = add(a_c, (-26.0, -44.0))
        t = s
        bx = (1 - t) ** 3 * p0[0] + 3 * (1 - t) ** 2 * t * p1[0] + 3 * (1 - t) * t * t * p2[0] + t ** 3 * p3[0]
        by = (1 - t) ** 3 * p0[1] + 3 * (1 - t) ** 2 * t * p1[1] + 3 * (1 - t) * t * t * p2[1] + t ** 3 * p3[1]
        alpha = keys_lerp([(0.0, al_to), (0.3, -52.0), (0.72, -18.0), (1.0, al_c)], s)
        return (bx, by), alpha, False

    def leg(self, u, pelvis_dy):
        """Bone rotations (thigh world, shin local, foot local) plus diagnostics for one leg at cycle phase u."""
        ankle, alpha, stance = self.foot_at(u)
        hip = (HIP[0], HIP[1] + pelvis_dy)
        knee, reach = ik(hip, ankle)
        thigh = angle_from_to(sub(KNEE, HIP), sub(knee, hip))
        shin_world = angle_from_to(sub(ANKLE, KNEE), sub(reach, knee))
        return dict(thigh=thigh, shin=shin_world - thigh, foot=alpha - shin_world, stance=stance, ankle=ankle,
                    error=math.dist(reach, ankle), knee=knee)

    def stride_m(self, px_per_m):
        return self.travel / px_per_m / self.stance


if __name__ == '__main__':
    g = RunGait()
    worst = 0.0
    for i in range(40):
        u = i / 40
        dy = g.pelvis_dy(u)
        L = g.leg(u, dy)
        worst = max(worst, L['error'])
        if i % 4 == 0:
            print('u %.3f dy %5.1f thigh %6.1f shin %6.1f foot %6.1f stance %s ankle (%6.1f,%6.1f) err %.2f' % (u, dy, L['thigh'], L['shin'], L['foot'], L['stance'], *L['ankle'], L['error']))
    print('worst IK error px', round(worst, 2), 'stride m at 579 px/m', round(g.stride_m(579.0), 3))
