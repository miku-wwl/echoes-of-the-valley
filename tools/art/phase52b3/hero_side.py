"""Side view (facing screen-left) of the caretaker: cut-out layers and the run/idle animation.

Layers, back to front: back body (coat tail, back skirt, ponytail and scarf tail behind the legs), far leg (a darkened copy of the near
leg, as cut-out animation normally does for the hidden leg), near leg, front body (torso, head, near arm, front skirt over the thigh).
All pixels come from the owner's turnaround illustration (05); the near leg's hidden thigh top is extended from its own top rows.
"""
import math

import cv2
import numpy as np
from scipy import ndimage

import gait
import rig

# landmarks in hero_side.png pixels (view-local, before padding)
HIP, KNEE, ANKLE, SOLE_Y = (115.0, 515.0), (112.0, 700.0), (122.0, 905.0), 975.0
SHOULDER, ELBOW, NECK = (150.0, 215.0), (160.0, 330.0), (115.0, 165.0)
PAD = dict(left=260, top=40, right=200, bottom=40)


def leg_masks(rgba):
    """(leg, silhouette): `leg` = the near leg's own visible pixels (dark brown tights below the skirt, and the whole boot);
    `silhouette` = the same leg with the holes where the belt's tassel and charm hang in front of the thigh filled in."""
    h, w = rgba.shape[:2]
    poly = [(64, 538), (70, 600), (84, 660), (92, 700), (97, 760), (92, 800), (80, 850), (55, 905), (20, 925), (8, 960), (14, 984),
            (182, 984), (180, 930), (172, 870), (165, 800), (158, 760), (163, 700), (159, 640), (153, 580), (151, 538)]
    inside = rig.polygon_mask(h, w, poly) > 0.5
    c = rgba[..., :3].astype(np.float32)
    r, g, b = c[..., 0], c[..., 1], c[..., 2]
    lum = 0.299 * r + 0.587 * g + 0.114 * b
    # tights: dark and brownish; the petticoat ruffle and the tassel are dark red (r > 1.7 g) and stay with the skirt
    tights = inside & (lum < 125) & (r < 1.7 * g + 4) & (r >= g) & (rgba[..., 3] > 10)
    tights = ndimage.binary_opening(tights, iterations=1)
    lab, _ = ndimage.label(tights)
    sizes = np.bincount(lab.ravel())
    sizes[0] = 0
    tights = lab == int(np.argmax(sizes))
    boot = inside & (np.arange(h)[:, None] > 800) & (rgba[..., 3] > 10)
    leg = tights | boot
    silhouette = ndimage.binary_fill_holes(ndimage.binary_closing(leg, iterations=3)) & inside
    return leg, silhouette


# Accessories that hang on her LEFT side only (front view: screen-right). The side view shows her left side; the right-facing view (E) is
# that view mirrored, so for E these are removed instead of being swapped onto her right side. (view-local pixels of hero_side.png)
LEFT_ONLY_BODY = [
    [(98, 538), (136, 538), (140, 645), (100, 645)],      # gold charm and red tassel hanging from the belt over the left thigh
    [(50, 570), (88, 570), (88, 632), (50, 632)],         # teal crystal pendant on the garter
]
GARTER_ROWS = (552, 590)                                    # leather garter strap round the left thigh


def build_layers(view, variant='W'):
    h, w = view.shape[:2]
    leg, silhouette = leg_masks(view)
    pre = rig.premultiply(view)
    left_only = np.zeros((h, w), bool)
    touch_up = np.zeros((h, w), bool)
    if variant == 'E':
        boxes = np.zeros((h, w), bool)
        for poly in LEFT_ONLY_BODY:
            boxes |= rig.polygon_mask(h, w, poly) > 0.5
        rows = np.arange(h)[:, None] > 548
        # in front of the thigh: removing the accessory reveals the leg layer, whose tights are already painted in under it
        left_only = boxes & ndimage.binary_dilation(silhouette, iterations=1) & rows
        # beside the thigh: accessory-coloured pixels (gold, teal, red and their dark line work) are painted over from the skirt around them
        c = view[..., :3].astype(np.int32)
        r, g, b = c[..., 0], c[..., 1], c[..., 2]
        gold = (r > 120) & (g > 80) & (b < 0.8 * g) & (r > b + 45)
        teal = (g > r + 10) & (b > r)
        red = (r > 90) & (r > 1.5 * g) & (r > 1.3 * b)
        accessory = ndimage.binary_dilation(gold | teal | red, iterations=2)
        touch_up = boxes & ~left_only & accessory & (view[..., 3] > 10)
    # the leg layer: its own pixels, with the tights behind the hanging tassel and charm painted in from the surrounding tights
    holes = (silhouette & ~leg).astype(np.uint8)
    rgb = np.ascontiguousarray(view[..., :3])
    filled = cv2.inpaint(rgb, holes * 255, 6, cv2.INPAINT_TELEA)
    leg_rgba = np.dstack([np.where(holes[..., None] > 0, filled, rgb), (silhouette * 255).astype(np.uint8)])
    keep = np.clip(ndimage.gaussian_filter(ndimage.binary_dilation(silhouette, iterations=1).astype(np.float32), 0.6), 0, 1)
    leg_layer = rig.premultiply(leg_rgba) * keep[..., None]
    # keep the original anti-aliased outline where the leg meets the background
    edge = ndimage.binary_dilation(silhouette, iterations=1) & ~silhouette
    leg_layer[edge] = pre[edge] * keep[edge][..., None]
    if variant == 'E':
        # no garter on the right thigh: the strap rows are replaced by tights blended from the rows just above and below it
        y0, y1 = GARTER_ROWS
        above, below = leg_layer[y0 - 3].copy(), leg_layer[y1 + 3].copy()
        for y in range(y0, y1 + 1):
            t = (y - y0) / float(y1 - y0)
            row = above * (1 - t) + below * t
            inside = silhouette[y][:, None]
            leg_layer[y] = np.where(inside, np.where(row[..., 3:4] > 0.5, row, leg_layer[y]), leg_layer[y])
    # the body loses only the leg's own pixels (plus a one-pixel rim), so the tassel, charm and petticoat stay with the skirt
    cut = np.clip(ndimage.gaussian_filter(ndimage.binary_dilation(leg | left_only, iterations=1).astype(np.float32), 0.5), 0, 1)
    body = pre * (1 - cut[..., None])
    if touch_up.any():
        rgb_body = np.ascontiguousarray(view[..., :3])
        painted = cv2.inpaint(rgb_body, touch_up.astype(np.uint8) * 255, 5, cv2.INPAINT_TELEA).astype(np.float32) / 255.0
        a_body = body[..., 3:4]
        body = np.where(touch_up[..., None], np.concatenate([painted * a_body, a_body], -1), body)
    # extend the thigh upward under the skirt (only ever visible when the leg swings): repeat clean tights rows below the garter
    band = leg_layer[580:588].copy()
    for y in range(579, int(HIP[1]) - 12, -1):
        row = band[(579 - y) % len(band)]
        leg_layer[y] = np.where(leg_layer[y][..., 3:4] > row[..., 3:4], leg_layer[y], row * 0.98)
    # front/back split of the body below the waist: the coat tail and back skirt hang behind the legs
    ys, xs = np.mgrid[0:h, 0:w]
    split_x = 150 + (ys - 540) * 0.06
    behind = ndimage.gaussian_filter(((xs > split_x) & (ys > 470)).astype(np.float32), 1.0)
    back = body * behind[..., None]
    front = body - back
    return dict(back=back, front=front, leg=leg_layer)


def skeleton():
    B = rig.Bone
    return rig.Skeleton([
        B('root', HIP), B('chest', (110, 360), 'root'), B('head', NECK, 'chest'),
        B('arm', SHOULDER, 'chest'), B('forearm', ELBOW, 'arm'),
        B('pony1', (178, 36), 'head'), B('pony2', (228, 175), 'pony1'), B('pony3', (252, 300), 'pony2'),
        B('scarf1', (190, 200), 'chest'), B('scarf2', (232, 340), 'scarf1'),
        B('coat', (165, 430), 'root'), B('coat2', (205, 640), 'coat'),
        B('skirt', (70, 430), 'root'), B('satchel', (215, 395), 'root'),
        B('thigh', HIP, 'root'), B('shin', KNEE, 'thigh'), B('foot', ANKLE, 'shin'),
    ])


def body_weights(sk, h, w, ox, oy):
    """Weights for the body layers (same canvas as the padded view); ox, oy = padding offset."""
    def P(x, y):
        return (x + ox, y + oy)

    cap = lambda a, b, r, f: rig.capsule(h, w, P(*a), P(*b), r, f)
    W = {n: np.zeros((h, w), np.float32) for n in sk.index}
    W['root'] += 0.15
    W['chest'] += rig.band_y(h, w, 440 + oy, 380 + oy) * 0.9
    W['head'] += cap((100, 95), (115, 150), 62, 18) * rig.band_x(h, w, 200 + ox, 175 + ox) * 1.6
    W['arm'] += cap(SHOULDER, ELBOW, 30, 14) * rig.band_y(h, w, 205 + oy, 235 + oy) * 2.2
    W['forearm'] += cap(ELBOW, (172, 498), 26, 10) * rig.band_y(h, w, 318 + oy, 345 + oy) * 3.0
    xs_back = rig.band_x(h, w, 168 + ox, 190 + ox)
    W['pony1'] += cap((178, 36), (228, 175), 46, 16) * xs_back * 1.8
    W['pony2'] += cap((228, 175), (252, 300), 48, 16) * xs_back * 2.2
    W['pony3'] += cap((252, 300), (262, 345), 46, 14) * xs_back * 2.6
    W['scarf1'] += cap((195, 205), (232, 340), 26, 10) * rig.band_x(h, w, 182 + ox, 196 + ox) * 1.6
    W['scarf2'] += cap((232, 340), (250, 470), 30, 10) * rig.band_x(h, w, 182 + ox, 196 + ox) * 2.4
    W['coat'] += cap((165, 430), (205, 640), 70, 24) * rig.band_y(h, w, 420 + oy, 470 + oy) * rig.band_x(h, w, 140 + ox, 160 + ox) * 1.6
    W['coat2'] += cap((205, 640), (240, 800), 70, 22) * rig.band_x(h, w, 140 + ox, 160 + ox) * 2.4
    W['skirt'] += cap((70, 430), (40, 560), 52, 16) * rig.band_y(h, w, 420 + oy, 470 + oy) * rig.band_x(h, w, 150 + ox, 120 + ox) * 1.6
    W['satchel'] += cap((218, 400), (228, 500), 30, 10) * 1.4
    names = [b.name for b in sk.bones]
    return rig.normalise([W[n] for n in names])


def leg_weights(sk, h, w, ox, oy):
    names = [b.name for b in sk.bones]
    thigh = rig.band_y(h, w, KNEE[1] - 22 + oy, KNEE[1] + 22 + oy, invert=True)
    foot = rig.band_y(h, w, ANKLE[1] - 14 + oy, ANKLE[1] + 10 + oy)
    W = {n: np.zeros((h, w), np.float32) for n in names}
    W['thigh'] = thigh + 1e-3
    W['shin'] = (1 - thigh) * (1 - foot)
    W['foot'] = foot
    w8 = np.stack([W[n] for n in names], 0)
    return w8 / w8.sum(0, keepdims=True)


GAIT = gait.RunGait()


def run_pose(phase):
    """Body pose plus both legs from the IK gait (near leg at phase, far leg half a cycle later)."""
    dy = GAIT.pelvis_dy(phase)
    near, far = GAIT.leg(phase, dy), GAIT.leg(phase + 0.5, dy)
    step = 2 * math.pi * 2 * phase
    body = {
        'root': dict(dy=dy),
        'chest': dict(rot=4.5 + 0.8 * math.sin(step + 0.6)),
        'head': dict(rot=-2.5 + 0.6 * math.sin(step + 1.2)),
        'arm': dict(rot=-0.36 * near['thigh'] + 6.0),
        'forearm': dict(rot=20.0 + 7.0 * math.sin(2 * math.pi * phase + 1.0)),
        'pony1': dict(rot=-7.0 + 2.6 * math.sin(step - 1.2)),
        'pony2': dict(rot=-3.0 + 3.0 * math.sin(step - 1.8)),
        'pony3': dict(rot=-2.0 + 3.2 * math.sin(step - 2.4)),
        'scarf1': dict(rot=-6.0 + 2.2 * math.sin(step - 1.0)),
        'scarf2': dict(rot=-4.0 + 3.0 * math.sin(step - 1.7)),
        'coat': dict(rot=-4.0 + 1.8 * math.sin(step - 0.9)),
        'coat2': dict(rot=-3.0 + 2.6 * math.sin(step - 1.5)),
        'skirt': dict(rot=0.18 * near['thigh'] - 1.0),
        'satchel': dict(rot=2.5 * math.sin(step - 0.6), dy=1.5 * math.sin(step - 0.6)),
    }
    leg_pose = lambda L: dict(body, thigh=dict(rot=L['thigh']), shin=dict(rot=L['shin']), foot=dict(rot=L['foot']))
    return body, leg_pose(near), leg_pose(far), dy, dict(near=near, far=far)


def idle_pose(phase):
    s = math.sin(2 * math.pi * phase)
    c = math.cos(2 * math.pi * phase)
    body = {
        'root': dict(dy=0.0),
        'chest': dict(sy=1.0 + 0.007 * s, dy=-1.2 * s),
        'head': dict(rot=0.5 * c, dy=-0.6 * s),
        'arm': dict(rot=0.8 * s), 'forearm': dict(rot=1.2 * s),
        'pony1': dict(rot=1.2 * math.sin(2 * math.pi * phase - 0.8)), 'pony2': dict(rot=1.6 * math.sin(2 * math.pi * phase - 1.3)), 'pony3': dict(rot=2.0 * math.sin(2 * math.pi * phase - 1.8)),
        'scarf1': dict(rot=1.0 * math.sin(2 * math.pi * phase - 0.6)), 'scarf2': dict(rot=1.6 * math.sin(2 * math.pi * phase - 1.2)),
        'coat': dict(rot=0.6 * math.sin(2 * math.pi * phase - 0.5)), 'coat2': dict(rot=1.0 * math.sin(2 * math.pi * phase - 1.0)),
        'skirt': dict(rot=0.5 * math.sin(2 * math.pi * phase - 0.3)), 'satchel': dict(rot=0.6 * s),
    }
    return body, dict(body), dict(body), 0.0, None


def render(view, frames_run=10, frames_idle=12, variant='W'):
    """Returns dict with 'run' and 'idle' lists of premultiplied float frames on a common padded canvas, plus metadata."""
    v = rig.pad(view, PAD['left'], PAD['top'], PAD['right'], PAD['bottom'])
    ox, oy = PAD['left'], PAD['top']
    h, w = v.shape[:2]
    layers = {k: rig.pad(rig.to_rgba_u8(x), PAD['left'], PAD['top'], PAD['right'], PAD['bottom']) for k, x in build_layers(view, variant).items()}
    layers = {k: rig.premultiply(x) for k, x in layers.items()}
    sk = skeleton()
    shifted = rig.Skeleton([rig.Bone(b.name, b.pivot + (ox, oy), b.parent) for b in sk.bones])
    wb = body_weights(sk, h, w, ox, oy)
    wl = leg_weights(sk, h, w, ox, oy)
    far_tint = np.array([0.72, 0.70, 0.74, 1.0], np.float32)
    out = {'run': [], 'idle': []}
    info = []
    for kind, n, fn in (('run', frames_run, run_pose), ('idle', frames_idle, idle_pose)):
        for i in range(n):
            ph = i / n
            body, near_pose, far_pose, dy, legs = fn(ph)
            mb = shifted.matrices(body)
            frame = rig.warp(layers['back'], wb, mb)
            far_off = (6.0, -2.0) if kind == 'run' else (3.5, 0.0)
            far_leg = rig.warp(layers['leg'], wl, shifted.matrices(dict(far_pose, root=dict(dx=far_off[0], dy=body['root']['dy'] + far_off[1])))) * far_tint
            frame = rig.over(frame, far_leg)
            frame = rig.over(frame, rig.warp(layers['leg'], wl, shifted.matrices(near_pose)))
            frame = rig.over(frame, rig.warp(layers['front'], wb, mb))
            out[kind].append(frame)
            if legs:
                info.append(dict(kind=kind, phase=ph, near=legs['near'], far=legs['far'], pelvis_dy=dy))
    return out, dict(canvas=[w, h], ground_y=SOLE_Y + oy, pivot_x=HIP[0] + ox, legs=info)
