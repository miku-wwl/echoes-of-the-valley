"""Villager idle sprites (from the owner's NPC lineup, 02) and dialogue busts (caretaker from 01, villagers from 02).

python tools/art/phase52b3/make_villager_sprites.py   (after extract_views.py)
Villagers stand and talk, so each gets a front-facing idle loop: gentle breathing (chest), a small head bob and a slow sway around the feet.
Outputs go to game/Assets/Echoes/Art/Phase52B3/Sprites and Portraits; review sheets to artifacts/art-work/phase52b3/sprites.
"""
import hashlib
import json
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import rig  # noqa: E402
from make_hero_sprites import SCALE, bleed, downsample  # noqa: E402

ROOT = HERE.parents[2]
VIEWS = ROOT / 'artifacts/art-work/phase52b3/views'
WORK = ROOT / 'artifacts/art-work/phase52b3/sprites'
OUT = ROOT / 'game/Assets/Echoes/Art/Phase52B3/Sprites'
PORTRAITS = ROOT / 'game/Assets/Echoes/Art/Phase52B3/Portraits'
SRC = HERE / 'source'
VILLAGERS = {'Mira': ('npc_mira', 1.68), 'Wren': ('npc_wren', 1.60), 'Alder': ('npc_alder', 1.82)}
FRAMES, FPS = 12, 5.0


def landmarks(view):
    a = view[..., 3] > 128
    ys = np.where(a.any(1))[0]
    top, sole = int(ys.min()), int(ys.max())
    h = sole - top
    head_rows = a[top:top + int(0.14 * h)]
    hx = float(np.mean(np.where(head_rows)[1]))
    foot_rows = a[sole - int(0.03 * h):sole + 1]
    fx = float(np.mean(np.where(foot_rows)[1]))
    return dict(top=top, sole=sole, height=h, head=(hx, top + 0.09 * h), feet=(fx, sole))


def idle_frames(view):
    L = landmarks(view)
    pad = 30
    v = rig.premultiply(rig.pad(view, pad, pad, pad, pad))
    h, w = v.shape[:2]
    top, H = L['top'] + pad, L['height']
    feet = (L['feet'][0] + pad, L['feet'][1] + pad)
    waist_y = top + 0.40 * H
    sk = rig.Skeleton([rig.Bone('root', feet), rig.Bone('chest', (feet[0], waist_y), 'root'), rig.Bone('head', (L['head'][0] + pad, top + 0.17 * H), 'chest')])
    W = [np.full((h, w), 0.2, np.float32),
         rig.band_y(h, w, waist_y + 0.06 * H, waist_y - 0.04 * H),
         rig.capsule(h, w, (L['head'][0] + pad, top + 0.03 * H), (L['head'][0] + pad, top + 0.15 * H), 0.075 * H, 0.03 * H) * 1.5]
    wts = rig.normalise(W)
    frames = []
    for i in range(FRAMES):
        s = np.sin(2 * np.pi * i / FRAMES)
        c = np.cos(2 * np.pi * i / FRAMES)
        pose = {'root': dict(rot=0.35 * c), 'chest': dict(sy=1 + 0.006 * s, dy=-0.8 * s), 'head': dict(rot=0.6 * np.sin(2 * np.pi * i / FRAMES - 0.7), dy=-0.5 * s)}
        frames.append(rig.warp(v, wts, sk.matrices(pose)))
    return frames, dict(feet=feet, height_px=H)


def bust(rgba, box, fade=0.22):
    x0, y0, x1, y1 = box
    crop = rgba[max(y0, 0):y1, max(x0, 0):x1].copy()
    h = crop.shape[0]
    ramp = np.ones(h, np.float32)
    n = int(h * fade)
    ramp[h - n:] = np.linspace(1, 0, n) ** 1.4
    crop[..., 3] = (crop[..., 3].astype(np.float32) * ramp[:, None]).astype(np.uint8)
    return crop


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    PORTRAITS.mkdir(parents=True, exist_ok=True)
    WORK.mkdir(parents=True, exist_ok=True)
    entries = []
    sheets = {}
    for name, (view_name, height_m) in VILLAGERS.items():
        view = np.asarray(Image.open(VIEWS / (view_name + '.png')))
        frames, info = idle_frames(view)
        small = [downsample(f) for f in frames]
        alpha = np.max(np.stack([f[..., 3] for f in small]), 0)
        ys, xs = np.where(alpha > 0.01)
        x0, x1, y0, y1 = max(xs.min() - 2, 0), xs.max() + 3, max(ys.min() - 2, 0), ys.max() + 3
        imgs = [rig.to_rgba_u8(f[y0:y1, x0:x1]) for f in small]
        pivot = (info['feet'][0] * SCALE - x0, (y1 - y0) - (info['feet'][1] * SCALE - y0))   # from the frame's bottom-left
        ppm = info['height_px'] * SCALE / height_m
        sheets[name] = dict(pivot=pivot, ppm=ppm, size=imgs[0].shape[1::-1])
        for i, img in enumerate(imgs):
            entries.append((name, i, img))
            if i == 0:
                Image.fromarray(img).save(WORK / ('villager_%s_idle_00.png' % name))
    entries.sort(key=lambda t: -t[2].shape[0])
    W = 4096
    x = y = shelf = 0
    rects = []
    for name, i, img in entries:
        h, w = img.shape[:2]
        if x + w + 2 > W:
            x, y, shelf = 0, y + shelf + 2, 0
        rects.append((name, i, x, y, w, h))
        x += w + 2
        shelf = max(shelf, h)
    H = 1 << int(np.ceil(np.log2(y + shelf + 2)))
    atlas = np.zeros((H, W, 4), np.uint8)
    for (name, i, ax, ay, w, h), (_, _, img) in zip(rects, entries):
        atlas[ay:ay + h, ax:ax + w] = bleed(img)
    Image.fromarray(atlas).save(OUT / 'VillagersAtlas.png', optimize=True)
    meta = dict(note='Villager idle sprites: gentle breathing/sway warp of the owner-supplied AI NPC lineup (02).', atlas=[W, H], idleFps=FPS,
                villagers={n: dict(pivot=[float(s['pivot'][0]), float(s['pivot'][1])], pixelsPerMeter=float(s['ppm']), size=[int(v) for v in s['size']]) for n, s in sheets.items()},
                frames=[dict(villager=n, index=i, x=ax, y=H - ay - h, w=w, h=h) for n, i, ax, ay, w, h in rects])
    meta['sources'] = {f: hashlib.sha256((SRC / f).read_bytes()).hexdigest() for f in ('02-npc-lineup.png', '01-heroine-portrait.png')}
    (OUT / 'VillagersAtlas.json').write_text(json.dumps(meta, indent=1), encoding='utf-8')
    print('villagers atlas', W, H, {n: round(s['ppm'], 1) for n, s in sheets.items()})

    # dialogue busts: head and shoulders with a soft fade at the bottom
    portrait = np.asarray(Image.open(SRC / '01-heroine-portrait.png').convert('RGBA'))
    Image.fromarray(bust(portrait, (250, 30, 800, 650))).save(PORTRAITS / 'Bust_Caretaker.png')
    for name, (view_name, _) in VILLAGERS.items():
        view = np.asarray(Image.open(VIEWS / (view_name + '.png')))
        L = landmarks(view)
        hx, top, hh = L['head'][0], L['top'], L['height']
        half = int(0.30 * hh)
        box = (int(hx - half), max(top - 10, 0), int(hx + half), int(top + 0.40 * hh))
        Image.fromarray(bust(view, box)).save(PORTRAITS / ('Bust_%s.png' % name))
    # review sheet
    tiles = []
    for p in ['Bust_Caretaker', 'Bust_Mira', 'Bust_Wren', 'Bust_Alder']:
        im = Image.open(PORTRAITS / (p + '.png'))
        im = im.resize((int(im.size[0] * 360 / im.size[1]), 360), Image.LANCZOS)
        bg = Image.new('RGBA', im.size, (28, 26, 44, 255))
        bg.alpha_composite(im)
        tiles.append(bg.convert('RGB'))
    sheet = Image.new('RGB', (sum(t.size[0] for t in tiles) + 30, 360), (0, 0, 0))
    x = 0
    for t in tiles:
        sheet.paste(t, (x, 0))
        x += t.size[0] + 10
    sheet.save(WORK / 'portraits-sheet.png')
    print('portraits', [p.name for p in sorted(PORTRAITS.glob('*.png'))])


if __name__ == '__main__':
    main()
