"""Picks representative real framebuffer frames from a recorded automated tour (walking at four gait phases, side-on run, turning, under the willow canopy).

python tools/art/phase52b2/select_motion_frames.py <capture-folder>
Reads tour.json (per-tick hero state) and frame-manifest.json (frame times); copies the chosen frames UNMODIFIED into <capture-folder>/selected-frames/ and writes
selected-frames.json (why each frame was chosen, hero state at that moment, SHA-256). Frames are the recorder's JPEG framebuffer captures; no repainting, no concept art."""
import bisect, json, math, shutil, sys
from pathlib import Path

folder = Path(sys.argv[1]).resolve()
tour = json.loads((folder / 'tour.json').read_text(encoding='utf-8-sig'))
manifest_file = folder / 'frame-manifest.json'
if manifest_file.is_file():
    manifest = json.loads(manifest_file.read_text(encoding='utf-8-sig'))
else:                                         # frames-only runs have no encoded video and so no manifest: take the times from tour.json and hash the files here
    import hashlib
    manifest = [dict(file=f['file'], t=f['t'], sha256=hashlib.sha256((folder / 'frames' / f['file']).read_bytes()).hexdigest()) for f in tour['capturedFrames']]
samples = tour['samples']
times = [s['t'] for s in samples]
WILLOW = (14.50, 11.00)


def wrap(a):
    return (a + 180.0) % 360.0 - 180.0


def nearest(t):
    i = bisect.bisect_left(times, t)
    if i == 0:
        return 0
    if i >= len(times):
        return len(times) - 1
    return i if abs(times[i] - t) < abs(times[i - 1] - t) else i - 1


def yaw_rate(i):
    lo = nearest(times[i] - 0.15)
    hi = nearest(times[i] + 0.15)
    dt = times[hi] - times[lo]
    return abs(wrap(samples[hi]['yaw'] - samples[lo]['yaw'])) / dt if dt > 0 else 0.0


frames = []
for f in manifest:
    i = nearest(f['t'])
    s = samples[i]
    frames.append(dict(file=f['file'], t=f['t'], sha256=f['sha256'], leg=s['leg'], speed=s['speed'], phase=s['phase'], yaw=s['yaw'], cameraYaw=s['cameraYaw'], cameraPitch=s['cameraPitch'],
                       yawRate=yaw_rate(i), toWillow=math.hypot(s['x'] - WILLOW[0], s['z'] - WILLOW[1]), relCamera=wrap(s['cameraYaw'] - s['yaw'])))

chosen, used = [], []


def take(label, reason, candidates, key, min_gap):
    """Best candidate by key that is at least min_gap seconds from every frame already chosen."""
    for f in sorted(candidates, key=key):
        if all(abs(f['t'] - u) >= min_gap for u in used):
            used.append(f['t'])
            chosen.append(dict(label=label, reason=reason, **f))
            return
    print('no candidate for', label)


def phase_distance(f, target):
    d = abs(f['phase'] % 1.0 - target)
    return min(d, 1.0 - d)


moving = [f for f in frames if f['speed'] >= 2.5]
for k in range(3):
    take('under-canopy-%d' % (k + 1), 'hero closest to Willow_Pilot (14.50, 11.00) while moving; the canopy detour passes it twice', moving, lambda f: f['toWillow'], 1.5)


running = [f for f in frames if f['speed'] >= 4.3]
for target in (0.0, 0.25, 0.5, 0.75):
    take('walk-phase-%.2f' % target, 'follow camera, full speed (4.5 m/s), gait phase %.2f' % target, [f for f in running if f['leg'] == 'follow'], lambda f, t=target: phase_distance(f, t), 0.6)
for target in (0.25, 0.75):
    take('side-run-phase-%.2f' % target, 'orbit leg, camera about 90 degrees to the heading (side-on), gait phase %.2f' % target,
         [f for f in running if f['leg'] == 'orbit' and 70 <= abs(f['relCamera']) <= 110], lambda f, t=target: phase_distance(f, t), 0.6)
turning = [f for f in frames if f['speed'] >= 2.5 and f['leg'] != 'orbit']
for k in range(3):
    take('turn-%d' % (k + 1), 'fastest heading change while moving', turning, lambda f: -f['yawRate'], 2.0)

out = folder / 'selected-frames'
if out.exists():
    raise SystemExit('selected-frames already exists: ' + str(out))
out.mkdir()
for c in chosen:
    shutil.copyfile(folder / 'frames' / c['file'], out / (c['label'] + '.jpg'))
    c['copiedAs'] = c['label'] + '.jpg'
(out / 'selected-frames.json').write_text(json.dumps(dict(source=str(folder), note='unmodified framebuffer frames from an AUTOMATED tour; JPEG as recorded', frames=chosen), indent=2), encoding='utf-8')
for c in chosen:
    print('%-20s t=%6.2fs leg=%-10s speed=%.2f phase=%.2f yawRate=%5.1f toWillow=%5.1f' % (c['label'], c['t'], c['leg'], c['speed'], c['phase'], c['yawRate'], c['toWillow']))
print('selected', len(chosen), 'frames ->', out)
