"""Numeric validation of the baked clips on the real skinned vertices (no Blender needed).

python tools/art/phase52b2/validate_anim.py
Reports IK reach shortfall, ground penetration and foot sliding at the controller speed (4.5 m/s)."""
import sys, os, math
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import design, rig, anim, skinning
import rig_spec as R


def load():
    parts = design.build_model()
    V, IDX, W, names = [], [], [], []
    for p in parts:
        i, w = rig.weights_for(p)
        V.append(p.V); IDX.append(i); W.append(w); names += [p.name] * len(p.V)
    return parts, np.concatenate(V), np.concatenate(IDX), np.concatenate(W), np.array(names)


def main():
    parts, V, I, W, names = load()
    boot = {s: np.array([n.startswith('Boot' + s) or n.startswith('Sole' + s) for n in names]) for s in 'RL'}
    clip = anim.sample_clip('Walk')
    frames = clip['frames']
    print('Walk: frames', len(frames), 'IK stance shortfall (max m):', round(clip['shortfall'], 4))
    report = {}
    low_all = 9
    for s in 'RL':
        vi = np.where(boot[s])[0]
        rest = V[vi]
        # contact candidates: sole vertices (lowest 3 cm of the rest boot)
        sole = vi[rest[:, 1] < 0.03]
        track = []
        for k, fr in enumerate(frames):
            wr, wp = skinning.forward(fr.rot, fr.pelvis)
            pv = skinning.skin(V[sole], I[sole], W[sole], wr, wp)
            t = k * clip['length'] / (len(frames) - 1)
            ground = pv + np.array([0, 0, anim.SPEED * t])      # player frame -> ground frame
            track.append((pv, ground))
        low = min(float(pv[:, 1].min()) for pv, _ in track)
        low_all = min(low_all, low)
        # sliding: speed (m/s) of vertices that touch the ground (height < 1.5 cm) between consecutive frames
        speeds = []
        for (pv0, g0), (pv1, g1) in zip(track[:-1], track[1:]):
            dt = clip['length'] / (len(frames) - 1)
            touching = (pv0[:, 1] < 0.015) & (pv1[:, 1] < 0.015)
            if touching.any():
                v = np.linalg.norm((g1 - g0)[touching][:, [0, 2]], axis=1) / dt
                speeds.extend(v.tolist())
        speeds = np.array(speeds)
        report[s] = (low, np.percentile(speeds, 50) if len(speeds) else -1, np.percentile(speeds, 95) if len(speeds) else -1, speeds.max() if len(speeds) else -1, len(speeds))
        print('Boot %s: lowest sole vertex %+.4f m; ground contact samples %d; contact-vertex ground speed p50 %.3f p95 %.3f max %.3f m/s' % (s, low, report[s][4], report[s][1], report[s][2], report[s][3]))
    # reachability and gait statistics
    ys = [fr.pelvis[1] + R.HEAD['Pelvis'][1] for fr in frames]
    print('Pelvis height range %.3f..%.3f m' % (min(ys), max(ys)))
    zr = [anim.foot_state(i / 40)[0] for i in range(40)]
    print('Right ankle z range in player frame %.3f..%.3f' % (min(zr), max(zr)))
    # clip shape checks
    for k in ('Idle', 'TurnLeft', 'TurnRight'):
        c = anim.sample_clip(k)
        print(k, 'frames', len(c['frames']), 'bones', len(c['bones']))


if __name__ == '__main__':
    main()
