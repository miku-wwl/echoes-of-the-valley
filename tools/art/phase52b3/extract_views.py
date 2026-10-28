"""Cuts the three views of the owner-supplied turnaround (05) and the NPC lineup (02) out of their parchment background.

python tools/art/phase52b3/extract_views.py
Writes RGBA PNGs to artifacts/art-work/phase52b3/views/. Edge pixels get a real alpha estimated from the colour distance to the parchment and
their colour is un-mixed from the parchment, so no light fringe is left around the line art. Nothing is drawn or repainted; pixels inside
the figure are the illustration's own.
"""
import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[3]
SRC = ROOT / 'tools/art/phase52b3/source'
OUT = ROOT / 'artifacts/art-work/phase52b3/views'

# (name, source file, x0, x1) column ranges found by scanning for non-parchment pixels
VIEWS = [
    ('hero_front', '05-heroine-turnaround-reference.png', 30, 580),
    ('hero_side', '05-heroine-turnaround-reference.png', 615, 940),
    ('hero_back', '05-heroine-turnaround-reference.png', 955, 1506),
    ('npc_mira', '02-npc-lineup.png', 55, 600),
    ('npc_wren', '02-npc-lineup.png', 602, 1048),
    ('npc_alder', '02-npc-lineup.png', 1145, 1612),
]


def cut(rgb, x0, x1):
    crop = rgb[:, x0:x1].astype(np.float32)
    h, w, _ = crop.shape
    border = np.concatenate([crop[:6].reshape(-1, 3), crop[-6:].reshape(-1, 3), crop[:, :6].reshape(-1, 3), crop[:, -6:].reshape(-1, 3)])
    bg = np.median(border, 0)
    # local background: the parchment has a faint vignette, so estimate it smoothly from pixels close to the median colour
    lab = cv2.cvtColor(crop.astype(np.uint8), cv2.COLOR_RGB2LAB).astype(np.float32)
    bg_lab = cv2.cvtColor(bg.reshape(1, 1, 3).astype(np.uint8), cv2.COLOR_RGB2LAB).astype(np.float32)[0, 0]
    d = np.sqrt(((lab - bg_lab) ** 2).sum(-1))
    mask = np.full((h, w), cv2.GC_PR_FGD, np.uint8)
    mask[d < 9] = cv2.GC_PR_BGD
    mask[d > 40] = cv2.GC_FGD
    # parchment connected to the crop border is certain background
    low = (d < 7).astype(np.uint8)
    n, labels = cv2.connectedComponents(low, connectivity=4)
    edge_labels = set(np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))) - {0}
    mask[np.isin(labels, list(edge_labels))] = cv2.GC_BGD
    bgd, fgd = np.zeros((1, 65), np.float64), np.zeros((1, 65), np.float64)
    cv2.grabCut(crop.astype(np.uint8), mask, None, bgd, fgd, 6, cv2.GC_INIT_WITH_MASK)
    hard = ((mask == cv2.GC_FGD) | (mask == cv2.GC_PR_FGD)).astype(np.uint8)
    # keep the main figure (and anything large attached to it), drop specks
    n, labels, stats, _ = cv2.connectedComponentsWithStats(hard, connectivity=8)
    keep = np.zeros_like(hard)
    biggest = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    for i in range(1, n):
        if i == biggest or stats[i, cv2.CC_STAT_AREA] > 400:
            keep[labels == i] = 1
    hard = keep
    # soft alpha in a thin band around the hard edge, from the distance to the parchment colour
    band = cv2.dilate(hard, np.ones((3, 3), np.uint8), iterations=2) - cv2.erode(hard, np.ones((3, 3), np.uint8), iterations=1)
    soft = np.clip((d - 6.0) / (30.0 - 6.0), 0, 1)
    alpha = hard.astype(np.float32)
    alpha[band > 0] = soft[band > 0]
    alpha = np.where(cv2.dilate(hard, np.ones((3, 3), np.uint8), iterations=2) > 0, alpha, 0)
    # the generated illustration has a faint light glow round each figure (lighter than the parchment): outside the figure that glow
    # is background, not character, so it gets no alpha (otherwise it shows as a white halo in the game)
    lighter = lab[..., 0] > bg_lab[0] + 3.0
    alpha[(hard == 0) & lighter] = 0
    # parchment showing through gaps between hair locks is enclosed by the figure, so GrabCut keeps it: any parchment-coloured patch whose
    # surroundings are mostly hair (brown, mid-dark) is a gap, not costume, and becomes transparent (cream cloth is bordered by trims,
    # teal, gold or skin, so it is kept)
    paper = (d < 13.0) | lighter
    lab_n, count = ndimage.label(paper & (alpha > 0))
    r_, g_, b_ = crop[..., 0], crop[..., 1], crop[..., 2]
    lum = 0.299 * r_ + 0.587 * g_ + 0.114 * b_
    # brown/blonde hair: r > g > b with a clear green-blue step; lips and blush (red, small g-b step) and skin (light) are not hair
    hairish = (lum > 40) & (lum < 185) & (r_ - g_ > 10) & (r_ - g_ < 60) & (g_ - b_ > 12)
    gaps = np.zeros_like(paper)
    for k, sl in enumerate(ndimage.find_objects(lab_n), start=1):
        if sl is None:
            continue
        ys0, ys1 = max(sl[0].start - 4, 0), min(sl[0].stop + 4, h)
        xs0, xs1 = max(sl[1].start - 4, 0), min(sl[1].stop + 4, w)
        comp = lab_n[ys0:ys1, xs0:xs1] == k
        size = int(comp.sum())
        if size < 40:          # eye whites, teeth and small highlights are smaller than any gap between locks
            continue
        ring = ndimage.binary_dilation(comp, iterations=3) & ~comp
        ring_vals = hairish[ys0:ys1, xs0:xs1][ring]
        if ring_vals.size and ring_vals.mean() > 0.6:
            gaps[ys0:ys1, xs0:xs1] |= comp
    if gaps.any():
        soft_gap = np.clip(ndimage.gaussian_filter(ndimage.binary_dilation(gaps, iterations=1).astype(np.float32), 0.7), 0, 1)
        alpha = alpha * (1 - soft_gap)
    # edge colour decontamination: every partly transparent pixel takes the colour of the nearest solid pixel of the figure, so the
    # soft edge is the line art's own dark colour and no parchment or glow colour can leak into the game
    solid = alpha >= 0.95
    _, (iy, ix) = ndimage.distance_transform_edt(~solid, return_indices=True)
    colour = crop.copy()
    edge = (alpha > 0) & ~solid
    colour[edge] = crop[iy[edge], ix[edge]]
    colour = np.clip(colour, 0, 255)
    rgba = np.dstack([colour, alpha * 255]).astype(np.uint8)
    ys, xs = np.where(alpha > 0.02)
    y0, y1, xa, xb = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    return rgba[y0:y1, xa:xb], dict(x=int(x0 + xa), y=int(y0), w=int(xb - xa), h=int(y1 - y0), parchment=[float(v) for v in bg])


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    meta = {}
    for name, src, x0, x1 in VIEWS:
        rgb = np.asarray(Image.open(SRC / src).convert('RGB'))
        rgba, info = cut(rgb, x0, x1)
        Image.fromarray(rgba).save(OUT / (name + '.png'))
        info['source'] = src
        meta[name] = info
        print(name, info)
    (OUT / 'views.json').write_text(json.dumps(meta, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
