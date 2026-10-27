"""Frame-time and movement statistics from a Phase52PilotTour tour.json, plus an optional contact sheet of the recorded frames.

python tools/art/phase52b2/analyze_tour.py <tour-output-folder> [--sheet]
Frame times come from the player's own unscaled delta times. A recorded run (-echoes-pilot-record-frames) adds synchronous framebuffer
readback and JPEG encoding, so only unrecorded runs say anything about rendering speed; both are labelled and never mixed."""
import json, math, sys
from pathlib import Path


def pct(values, p):
    v = sorted(values)
    return v[min(len(v) - 1, int(math.ceil(p / 100 * len(v))) - 1)]


def main():
    folder = Path(sys.argv[1])
    rec = json.loads((folder / 'tour.json').read_text(encoding='utf-8-sig'))
    s = rec['samples']
    ms = [x['frameMs'] for x in s]
    worst = max(ms)
    n1 = max(1, math.ceil(len(ms) * 0.01))
    low1 = 1000.0 / (sum(sorted(ms, reverse=True)[:n1]) / n1)
    moving = [x['speed'] for x in s if x['speed'] > 0.5]
    recorded = rec.get('videoCapture', 'None') != 'None'
    out = {
        'folder': str(folder), 'resolution': '%dx%d' % (rec['width'], rec['height']), 'recordedFramebuffer': recorded,
        'seconds': round(rec['movementSeconds'], 2), 'metres': round(rec['travelledMetres'], 1), 'waypointsVisited': rec['visitedWaypoints'],
        'teleportsDuringMovement': rec['teleportsDuringMovement'], 'canopyTree': rec.get('canopyTree'), 'frames': len(s),
        'frameMs': {'p50': round(pct(ms, 50), 2), 'p95': round(pct(ms, 95), 2), 'p99': round(pct(ms, 99), 2), 'worst': round(worst, 2), 'mean': round(sum(ms) / len(ms), 2)},
        'fps': {'mean': round(1000 / (sum(ms) / len(ms)), 1), 'onePercentLow': round(low1, 1)},
        'speedWhileMoving': {'mean': round(sum(moving) / max(1, len(moving)), 2), 'max': round(max(moving) if moving else 0, 2)},
        'legs': {}, 'note': 'recorded run: capture overhead included, not a rendering benchmark' if recorded else 'unrecorded run: no framebuffer capture; static route, bounded sample, not a human traversal benchmark'}
    for leg in sorted({x.get('leg', '') for x in s}):
        sub = [x['frameMs'] for x in s if x.get('leg', '') == leg]
        out['legs'][leg] = {'frames': len(sub), 'p95FrameMs': round(pct(sub, 95), 2), 'worstFrameMs': round(max(sub), 2),
                            'cameraPitchRange': [round(min(x['cameraPitch'] for x in s if x.get('leg', '') == leg), 1), round(max(x['cameraPitch'] for x in s if x.get('leg', '') == leg), 1)]}
    (folder / 'tour-stats.json').write_text(json.dumps(out, indent=2), encoding='utf-8')
    print(json.dumps(out))
    if '--sheet' in sys.argv and (folder / 'frames').is_dir():
        from PIL import Image, ImageDraw
        frames = rec['capturedFrames']
        picks = [min(frames, key=lambda f: abs(f['t'] - t)) for t in range(2, int(rec['movementSeconds']), 2)]
        w, h, cols = 320, 180, 5
        rows = math.ceil(len(picks) / cols)
        sheet = Image.new('RGB', (cols * w, rows * (h + 18)), (22, 32, 31))
        d = ImageDraw.Draw(sheet)
        for i, f in enumerate(picks):
            im = Image.open(folder / 'frames' / f['file']).convert('RGB').resize((w, h))
            x, y = (i % cols) * w, (i // cols) * (h + 18)
            sheet.paste(im, (x, y + 18))
            d.text((x + 4, y + 3), 't=%.1fs' % f['t'], fill=(235, 228, 205))
        sheet.save(folder / 'contact-sheet.png')
        print('contact sheet', folder / 'contact-sheet.png')


if __name__ == '__main__':
    main()
