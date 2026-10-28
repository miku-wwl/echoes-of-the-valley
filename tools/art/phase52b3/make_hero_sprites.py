"""Renders the caretaker's production sprites (run + idle for S, N, W; E = W mirrored at runtime) and packs them into one atlas for Unity.

python tools/art/phase52b3/make_hero_sprites.py
Outputs:
  game/Assets/Echoes/Art/Phase52B3/Sprites/CaretakerAtlas.png    straight-alpha RGBA atlas, colour bled into transparent texels
  game/Assets/Echoes/Art/Phase52B3/Sprites/CaretakerAtlas.json   frame rects, pivots (ground point under the pelvis), scale, timing
  artifacts/art-work/phase52b3/sprites/                          per-frame PNGs and contact sheets (review only)
Every pixel is resampled from the owner's turnaround illustration (05); nothing is drawn.
"""
import hashlib
import json
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gait  # noqa: E402
import hero_frontback  # noqa: E402
import hero_side  # noqa: E402
import rig  # noqa: E402

ROOT = HERE.parents[2]
VIEWS = ROOT / 'artifacts/art-work/phase52b3/views'
WORK = ROOT / 'artifacts/art-work/phase52b3/sprites'
OUT = ROOT / 'game/Assets/Echoes/Art/Phase52B3/Sprites'
SCALE = 0.5                 # source turnaround pixels -> runtime texels
CHAR_HEIGHT_M = 1.65        # head top to sole
SOURCE_HEIGHT_PX = 955.0    # head top (y ~ 20) to sole (y 975) in the turnaround
RUN_FRAMES, IDLE_FRAMES, IDLE_FPS = 10, 12, 5.0   # idle: one slow breath every 2.4 s


def downsample(premul):
    h, w = premul.shape[:2]
    small = cv2.resize(premul, (int(round(w * SCALE)), int(round(h * SCALE))), interpolation=cv2.INTER_AREA)
    return np.clip(small, 0, 1)


def bleed(rgba_u8):
    """Copy the nearest opaque colour into transparent texels so bilinear filtering and mipmaps never pull in black or white fringes."""
    a = rgba_u8[..., 3]
    solid = a > 8
    if not solid.any():
        return rgba_u8
    idx = ndimage.distance_transform_edt(~solid, return_distances=False, return_indices=True)
    out = rgba_u8.copy()
    out[..., :3] = rgba_u8[idx[0], idx[1], :3]
    out[..., 3] = a
    return out


def render_all():
    dirs = {}
    side = np.asarray(Image.open(VIEWS / 'hero_side.png'))
    frames, meta = hero_side.render(side, RUN_FRAMES, IDLE_FRAMES)
    dirs['W'] = (frames, meta)
    # E: her right side. The same side illustration without the left-only accessories, mirrored; the run is shifted half a cycle so
    # the leg nearest the viewer (now her right leg) continues the stride the far leg had in W
    frames, meta = hero_side.render(side, RUN_FRAMES, IDLE_FRAMES, variant='E')
    frames = {k: [np.ascontiguousarray(f[:, ::-1]) for f in v] for k, v in frames.items()}
    frames['run'] = frames['run'][RUN_FRAMES // 2:] + frames['run'][:RUN_FRAMES // 2]
    meta = dict(meta, pivot_x=meta['canvas'][0] - meta['pivot_x'])
    dirs['E'] = (frames, meta)
    for key, which in (('S', 'front'), ('N', 'back')):
        view = np.asarray(Image.open(VIEWS / ('hero_%s.png' % which)))
        frames, meta = hero_frontback.render(view, which, RUN_FRAMES, IDLE_FRAMES)
        dirs[key] = (frames, meta)
    return dirs


def main():
    WORK.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    dirs = render_all()
    sprites = {}
    for key, (frames, meta) in dirs.items():
        small = {k: [downsample(f) for f in v] for k, v in frames.items()}
        alpha = np.max(np.stack([f[..., 3] for v in small.values() for f in v]), 0)
        ys, xs = np.where(alpha > 0.01)
        x0, x1, y0, y1 = max(xs.min() - 2, 0), xs.max() + 3, max(ys.min() - 2, 0), ys.max() + 3
        pivot = (meta['pivot_x'] * SCALE - x0, (y1 - y0) - (meta['ground_y'] * SCALE - y0))   # from the frame's bottom-left, as Unity expects
        sprites[key] = dict(frames={k: [rig.to_rgba_u8(f[y0:y1, x0:x1]) for f in v] for k, v in small.items()}, pivot=pivot, size=(x1 - x0, y1 - y0))
        for kind, lst in sprites[key]['frames'].items():
            for i, img in enumerate(lst):
                Image.fromarray(img).save(WORK / ('%s_%s_%02d.png' % (key, kind, i)))
    # shelf-pack every frame into one atlas
    items = [(key, kind, i, img) for key, sp in sprites.items() for kind, lst in sp['frames'].items() for i, img in enumerate(lst)]
    items.sort(key=lambda t: -t[3].shape[0])
    W = 4096
    x = y = shelf = 0
    rects = []
    for key, kind, i, img in items:
        h, w = img.shape[:2]
        if x + w + 2 > W:
            x, y, shelf = 0, y + shelf + 2, 0
        rects.append((key, kind, i, x, y, w, h))
        x += w + 2
        shelf = max(shelf, h)
    H = 1 << int(np.ceil(np.log2(y + shelf + 2)))
    atlas = np.zeros((H, W, 4), np.uint8)
    for (key, kind, i, ax, ay, w, h), (_, _, _, img) in zip(rects, items):
        atlas[ay:ay + h, ax:ax + w] = bleed(img)
    Image.fromarray(atlas).save(OUT / 'CaretakerAtlas.png', optimize=True)
    px_per_m_src = SOURCE_HEIGHT_PX / CHAR_HEIGHT_M
    meta = dict(
        note='Production sprites of the caretaker: cut-out skinning of the owner-supplied AI turnaround (05), rendered offline. E is the side view without her left-only accessories (garter, crystal, tassel), mirrored, with the run phase shifted half a cycle.',
        atlas=[W, H], pixelsPerMeter=px_per_m_src * SCALE, strideMeters=gait.RunGait().stride_m(px_per_m_src), runFrames=RUN_FRAMES, idleFrames=IDLE_FRAMES, idleFps=IDLE_FPS,
        directions={key: dict(pivot=[float(sp['pivot'][0]), float(sp['pivot'][1])], size=list(map(int, sp['size']))) for key, sp in sprites.items()},
        frames=[dict(dir=key, kind=kind, index=i, x=ax, y=H - ay - h, w=w, h=h) for key, kind, i, ax, ay, w, h in rects],   # y from the bottom, as Unity UVs
    )
    # provenance travels with the sprites: the exact owner-supplied illustrations they were made from
    meta['sources'] = {f: hashlib.sha256((HERE / 'source' / f).read_bytes()).hexdigest() for f in ('05-heroine-turnaround-reference.png',)}
    (OUT / 'CaretakerAtlas.json').write_text(json.dumps(meta, indent=1), encoding='utf-8')
    print('atlas', W, H, 'frames', len(rects), 'px/m', round(meta['pixelsPerMeter'], 1), 'stride m', round(meta['strideMeters'], 3))
    # review sheet at the size the game shows her (about 300 px tall at 1080p with the default camera)
    tiles = []
    for key in ('S', 'W', 'E', 'N'):
        sp = sprites[key]
        for kind in ('run', 'idle'):
            for i, img in enumerate(sp['frames'][kind][: (10 if kind == 'run' else 4)]):
                im = Image.fromarray(img)
                sc = 300.0 / (CHAR_HEIGHT_M * meta['pixelsPerMeter'])
                im = im.resize((max(1, int(im.size[0] * sc)), max(1, int(im.size[1] * sc))), Image.LANCZOS)
                bg = Image.new('RGBA', (220, 340), (110, 140, 96, 255))
                d = ImageDraw.Draw(bg)
                gy = 320
                d.rectangle((0, gy, 220, 340), fill=(150, 125, 95, 255))
                px, py = sp['pivot'][0] * sc, (sp['size'][1] - sp['pivot'][1]) * sc
                bg.alpha_composite(im, (int(110 - px), int(gy - py)))
                d.text((3, 3), '%s %s %d' % (key, kind, i), fill=(255, 255, 0, 255))
                tiles.append(bg.convert('RGB'))
    cols = 14
    rows = (len(tiles) + cols - 1) // cols
    sheet = Image.new('RGB', (220 * cols, 340 * rows))
    for i, t in enumerate(tiles):
        sheet.paste(t, ((i % cols) * 220, (i // cols) * 340))
    sheet.save(WORK / 'contact-sheet-game-scale.png')
    print('sheet', WORK / 'contact-sheet-game-scale.png')


if __name__ == '__main__':
    main()
