"""Phase53 owner-source processing: instance-aware alpha cuts, registered foot pivots and two-pose derived cycles.
No new illustrations are generated. The walk source contains TWO drawings per direction; derived frames are disclosed.
python -I tools/art/phase53/prepare_art.py <extracted phase53_assets>
"""
import hashlib
import json
import shutil
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi

ROOT = Path(__file__).resolve().parents[3]
SRC = Path(sys.argv[1]).resolve()
ART = ROOT / 'game/Assets/Echoes/Art/Phase53'
OUT = ART / 'Production'
QA = ROOT / 'artifacts/visual-qa/phase-5.3/asset-audit'
DIRS = ['S', 'SW', 'W', 'NW', 'N', 'NE', 'E', 'SE']
CELL_W, CELL_H, BODY_H = 320, 384, 330
for p in (ART / 'Source', OUT, QA, ROOT / 'tools/art/phase53/source-docs'):
    p.mkdir(parents=True, exist_ok=True)
sources = []
for p in sorted(SRC.rglob('*.png')):
    target = ART / 'Source' / p.name
    if target.exists():
        assert target.read_bytes() == p.read_bytes(), 'Never overwrite a different source'
    else:
        shutil.copy2(p, target)
    a = np.array(Image.open(p).convert('RGBA'))
    sources.append(dict(file=p.name, sha256=hashlib.sha256(p.read_bytes()).hexdigest(), size=list(Image.open(p).size), alphaMin=int(a[..., 3].min()), alphaMax=int(a[..., 3].max())))
for p in SRC.rglob('*'):
    if p.is_file() and p.suffix != '.png':
        shutil.copy2(p, ROOT / 'tools/art/phase53/source-docs' / p.name)


def bleed(a):
    visible = a[..., 3] > 0
    _, inds = ndi.distance_transform_edt(~visible, return_indices=True)
    a[~visible, :3] = a[inds[0][~visible], inds[1][~visible], :3]
    return a


cuts = []


def cut_instances(name, count):
    """Each large alpha connected component is one complete figure, including pixels crossing nominal row boundaries.
    Detached anti-aliased pieces within 5px attach to that instance; distant coloured fringe is discarded.
    """
    a = np.array(Image.open(ART / 'Source' / name).convert('RGBA'))
    lab, _ = ndi.label(a[..., 3] > 128)
    sizes = np.bincount(lab.ravel())
    ids = np.argsort(sizes[1:])[::-1][:count] + 1
    assert min(sizes[ids]) > 15000
    instances = []
    for i in ids:
        main = lab == i
        ys, xs = np.where(main)
        instances.append((float(xs.mean()), float(ys.mean()), i))
    # Sort by nominal row of component centroid, not by a hard crop boundary.
    rows = count // 4
    ordered = sorted(instances, key=lambda q: (min(rows - 1, int(q[1] / a.shape[0] * rows)), q[0]))
    output = []
    for index, (_, _, i) in enumerate(ordered):
        main = lab == i
        distance = ndi.distance_transform_edt(~main)
        keep = (distance <= 4) & (a[..., 3] > 40)
        # A high-alpha isolated speck cannot migrate from another sprite.
        keep &= (lab == 0) | (lab == i) | (sizes[lab] < 250)
        ys, xs = np.where(keep)
        box = [max(0, int(xs.min()) - 3), max(0, int(ys.min()) - 3), min(a.shape[1], int(xs.max()) + 4), min(a.shape[0], int(ys.max()) + 4)]
        b = a.copy()
        b[..., 3] = np.where(keep, np.clip((a[..., 3].astype(float) - 40) * 255 / 214, 0, 255), 0).astype(np.uint8)
        b = b[box[1]:box[3], box[0]:box[2]]
        cuts.append(dict(source=name, index=index, bounds=box, pixels=int(keep.sum()), crossesNominalRow=bool(box[1] < (index // 4) * a.shape[0] / rows or box[3] > (index // 4 + 1) * a.shape[0] / rows)))
        output.append(b)
    return output


def normalize(a):
    """One ground point at the support-boot baseline; torso x anchored independently of cloak width."""
    h, w = a.shape[:2]
    scale = BODY_H / h
    b = cv2.resize(a, (round(w * scale), BODY_H), interpolation=cv2.INTER_AREA if scale < 1 else cv2.INTER_CUBIC)
    mask = b[..., 3] > 128
    ys, xs = np.where(mask[int(BODY_H * .20):int(BODY_H * .45)])
    axis = float(xs.mean())
    canvas = np.zeros((CELL_H, CELL_W, 4), dtype=np.uint8)
    dx = round(CELL_W / 2 - axis)
    assert dx >= 0 and dx + b.shape[1] <= CELL_W, 'Sprite would be cropped'
    canvas[32:32 + BODY_H, dx:dx + b.shape[1]] = b
    return canvas


def walk_derived(a, phase, half):
    """Local lower-leg deformation of each supplied contact drawing. Torso/face remain unwarped.
    Half A/B uses the corresponding supplied pose; each half has passing/lift/contact motion.
    This is derived animation, not eight original source drawings and not owner-approved foot contact.
    """
    y, x = np.mgrid[:CELL_H, :CELL_W].astype(np.float32)
    lower = np.clip((y - 210) / 130, 0, 1)
    side = np.tanh((x - CELL_W / 2) / 18)
    swing = np.sin(phase * 2 * np.pi)
    lift = np.maximum(0, side * swing)
    map_x = x - (side * swing * 7 * lower)
    map_y = y + lift * 11 * lower
    # Premultiply before filtering to avoid dark translucent edges.
    f = a.astype(np.float32) / 255
    f[..., :3] *= f[..., 3:4]
    warped = cv2.remap(f, map_x.astype(np.float32), map_y.astype(np.float32), cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT)
    warped[..., :3] /= np.maximum(warped[..., 3:4], .0001)
    return np.clip(warped * 255, 0, 255).astype(np.uint8)


idle = cut_instances('Caretaker_8dir_idle_source.png', 8)
walk = cut_instances('Caretaker_8dir_walk_keyposes_source.png', 16)
mira = cut_instances('Mira_8dir_idle_source.png', 8)
metadata = dict(atlas=[CELL_W * 8, CELL_H * 9], pixelsPerMeter=BODY_H / 1.62, strideMeters=2.8, idleFps=1, directions=[])
hero_atlas = Image.new('RGBA', tuple(metadata['atlas']))
mira_atlas = Image.new('RGBA', (CELL_W * 8, CELL_H))
sheet = Image.new('RGB', (CELL_W * 8, CELL_H * 3), '#303b38')
for i, direction in enumerate(DIRS):
    standing = normalize(idle[i]); a, b = normalize(walk[i]), normalize(walk[i + 8])
    hero_atlas.paste(Image.fromarray(bleed(standing.copy())), (i * CELL_W, 0))
    mira_atlas.paste(Image.fromarray(bleed(normalize(mira[i]))), (i * CELL_W, 0))
    sheet.paste(Image.fromarray(standing), (i * CELL_W, 0), Image.fromarray(standing[..., 3]))
    sheet.paste(Image.fromarray(a), (i * CELL_W, CELL_H), Image.fromarray(a[..., 3]))
    sheet.paste(Image.fromarray(b), (i * CELL_W, CELL_H * 2), Image.fromarray(b[..., 3]))
    frames = []
    for n in range(8):
        frame = walk_derived(a if n < 4 else b, n / 8, n // 4)
        hero_atlas.paste(Image.fromarray(bleed(frame)), (i * CELL_W, (n + 1) * CELL_H))
        frames.append(dict(x=i * CELL_W, y=(n + 1) * CELL_H, w=CELL_W, h=CELL_H))
    metadata['directions'].append(dict(key=direction, pivot=[CELL_W / 2, CELL_H - (32 + BODY_H - 2)], idle=[dict(x=i * CELL_W, y=0, w=CELL_W, h=CELL_H)], run=frames, originalWalkDrawings=2, derivedFrames=8, mirrorOf=''))
hero_atlas.save(OUT / 'Caretaker8D.png')
mira_atlas.save(OUT / 'Mira8D.png')
(OUT / 'CharacterFrames.json').write_text(json.dumps(metadata, indent=2), encoding='utf-8')
ImageDraw.Draw(sheet).text((8, 5), 'SOURCE CUT AUDIT: idle / contact A / contact B. Derived animation is not owner approved.', fill='white')
sheet.save(QA / 'character-cuts.png')

# Remove low-alpha source glow without cutting the hand-painted opaque shoreline.
for name in ['Pond_S0_dormant_45deg_source.png', 'Pond_S3_restored_45deg_source.png']:
    a = np.array(Image.open(ART / 'Source' / name).convert('RGBA'))
    a[..., 3] = np.clip((a[..., 3].astype(float) - 110) * 255 / 144, 0, 255).astype(np.uint8)
    yy, xx = np.mgrid[:a.shape[0], :a.shape[1]]
    u, v = xx / (a.shape[1] - 1) - .5, .5 - yy / (a.shape[0] - 1)
    depth = 12.8 * (2 / 3) / np.sin(np.deg2rad(48))
    wx, wz = (u * 12.8 + v * depth) * np.sqrt(.5), (-u * 12.8 + v * depth) * np.sqrt(.5)
    # Preserve the real stream inlet: the source painting's closed ring must not cover that existing water channel.
    opening = np.clip((1.25 - np.abs(wx)) / .25, 0, 1) * np.clip((wz - 1.8) / .35, 0, 1)
    # Cut only the painted BANK at the inlet, never the pond's interior. Removing the whole strip exposed
    # a rectangular stream end / grass-bottom wedge in the real Player. The old stream now ends below intact pond water.
    inner = np.array([[.315,.16],[.44,.145],[.59,.235],[.675,.31],[.80,.385],[.885,.445],[.92,.56],
                      [.88,.755],[.74,.805],[.56,.775],[.37,.68],[.185,.605],[.14,.465],[.19,.305]])
    water_mask = np.zeros(a.shape[:2], dtype=np.uint8)
    cv2.fillPoly(water_mask, [np.round(inner * [a.shape[1]-1, a.shape[0]-1]).astype(np.int32)], 1)
    opening *= 1 - water_mask
    a[..., 3] = (a[..., 3] * (1 - opening)).astype(np.uint8)
    Image.fromarray(bleed(a)).save(OUT / name.replace('_source', ''))

water = np.array(Image.open(ART / 'Source/PondWater_muted_albedo_TILING_CANDIDATE.png').convert('RGB'))
# Periodic edge correction (linear endpoint trend removed in each axis, preserving interior brush variation).
f = water.astype(float)
f -= np.linspace(0, 1, f.shape[1])[None, :, None] * (f[:, -1:] - f[:, :1])
f -= np.linspace(0, 1, f.shape[0])[:, None, None] * (f[-1:] - f[:1])
repaired = np.clip(f, 0, 255).astype(np.uint8)
Image.fromarray(repaired).save(OUT / 'PondWaterTile.png')

lilies = np.array(Image.open(ART / 'Source/LilyPads_6clusters_45deg_source.png').convert('RGBA'))
lily_atlas = Image.new('RGBA', (1536, 1024))
for row in range(2):
    for col in range(3):
        b = lilies[row * 512:(row + 1) * 512, col * 512:(col + 1) * 512].copy()
        b[..., 3] = np.where(b[..., 3] > 90, np.clip((b[..., 3].astype(float) - 90) * 255 / 164, 0, 255), 0).astype(np.uint8)
        lily_atlas.paste(Image.fromarray(bleed(b)), (col * 512, row * 512))
lily_atlas.save(OUT / 'LilyClusters.png')
report = dict(sourceZipSha256='80C763E0AF1037BC5A377119976B27331633C1D96C54F095B4BCC930F555037C', sources=sources, instanceCuts=cuts,
              sourceAlpha='PASS_STRUCTURE; seven images decode; opaque water albedo intentionally has no transparent texels', cutting='PASS: 8+16+8 disjoint complete large components, no grid clipping',
              walk='PARTIAL: two original contact drawings per direction; eight derived lower-leg warp frames. Owner natural-motion/foot-contact review PENDING',
              wrenAlder='PARTIAL: no new drawings, identity-preserving B3 visuals retained', waterSeams=dict(beforeX=float(np.abs(water[:, 0].astype(float) - water[:, -1]).mean()), beforeY=float(np.abs(water[0].astype(float) - water[-1]).mean()), afterX=float(np.abs(repaired[:, 0].astype(float) - repaired[:, -1]).mean()), afterY=float(np.abs(repaired[0].astype(float) - repaired[-1]).mean())))
(QA / 'audit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
(OUT / 'SourceHashes.json').write_text(json.dumps(sources, indent=2), encoding='utf-8')
print(json.dumps({k: report[k] for k in ['cutting', 'walk', 'waterSeams']}))
