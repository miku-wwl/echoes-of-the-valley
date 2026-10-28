"""Ten labelled overview sheets of the B4 evidence for a reviewer who looks at attached images directly (e.g. ChatGPT on the web).

python -I tools/art/phase52b4/make_b4_chatgpt_sheets.py <output-folder>
Every tile is an unmodified real-player capture (or a crop of one), the production art, or the owner's concept, scaled down; its label and
its path inside the review zip are written in a strip under the picture, never on it. Nothing is generated, retouched or composited
into a scene.
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[3]
ev = root / 'artifacts/visual-qa/phase-5.2/b4'
t1080 = ev / 'player/tour1080'
t720 = ev / 'player/tour720-video'
out = Path(sys.argv[1]).resolve()
out.mkdir(parents=True, exist_ok=True)
FONT = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 17)
TITLE = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', 22)
BG, INK, DIM, GOLD = (24, 28, 30), (236, 230, 214), (170, 176, 168), (242, 212, 138)

# where each source file sits inside Echoes-Phase52-B4-Isometric-Review.zip
ZIP = {t1080: 'real-player-screenshots/tour1080', t720: 'real-player-screenshots/tour720-video'}


def zip_path(src):
    src = Path(src)
    for folder, inside in ZIP.items():
        if src.parent == folder:
            return inside + '/' + src.name
    if src.parent == t720 / 'selected-frames':
        return 'motion/selected-frames/' + src.name
    special = {ev / 'minimap-strip.png': 'motion/minimap-strip.png', t720 / 'contact-sheet.png': 'motion/contact-sheet.png',
               root / 'artifacts/art-work/phase52b4/hero/game-scale-frames.png': 'asset-audit/caretaker/game-scale-frames.png',
               root / 'artifacts/art-work/phase52b4/hero/onion-skin-heads.png': 'asset-audit/caretaker/onion-skin-heads.png',
               root / 'game/Assets/Echoes/Art/Phase52B4/Source/art-direction/b4_isometric_style_target.png': 'CONCEPT_ART-not-game-footage/b4_isometric_style_target.png'}
    return special[src]


def tile(src, label, category='REAL_PLAYER_SCREENSHOT', size=(960, 540), crop=None):
    im = Image.open(src).convert('RGB')
    if crop:
        cx, cy, w, h = crop
        im = im.crop((cx - w // 2, cy - h // 2, cx + w // 2, cy + h // 2))
    im.thumbnail(size, Image.LANCZOS)
    cell = Image.new('RGB', (size[0], size[1] + 50), BG)
    cell.paste(im, ((size[0] - im.width) // 2, (size[1] - im.height) // 2))
    d = ImageDraw.Draw(cell)
    d.text((8, size[1] + 5), '[%s] %s' % (category, label), font=FONT, fill=GOLD if category != 'CONCEPT_ART' else (255, 140, 120))
    where = zip_path(src) + (' (crop)' if crop else '')
    if FONT.getlength(where) > size[0] - 12:      # narrow tiles: the folder is in the sheet title, keep the file name whole
        where = '.../' + where.split('/', 2)[-1]
    if FONT.getlength(where) > size[0] - 12:
        where = Path(zip_path(src)).name + (' (crop)' if crop else '')
    d.text((8, size[1] + 27), where, font=FONT, fill=DIM)
    return cell


def sheet(name, title, cells, cols):
    w, h = cells[0].size
    rows = (len(cells) + cols - 1) // cols
    im = Image.new('RGB', (cols * w + (cols + 1) * 8, rows * h + (rows + 1) * 8 + 44), BG)
    d = ImageDraw.Draw(im)
    d.text((10, 10), 'Echoes of the Valley · Phase 5.2-B4 · ' + title, font=TITLE, fill=INK)
    for i, c in enumerate(cells):
        im.paste(c, (8 + (i % cols) * (w + 8), 52 + (i // cols) * (h + 8)))
    im.save(out / name, quality=88, optimize=True)
    print(name, im.size)


def wide(name, title, src, label, category, width=2400):
    im = Image.open(src).convert('RGB')
    s = min(1.0, width / im.width)
    im = im.resize((int(im.width * s), int(im.height * s)), Image.LANCZOS)
    cell = Image.new('RGB', (im.width, im.height + 50), BG)
    cell.paste(im, (0, 0))
    d = ImageDraw.Draw(cell)
    d.text((8, im.height + 5), '[%s] %s' % (category, label), font=FONT, fill=GOLD)
    d.text((8, im.height + 27), zip_path(src), font=FONT, fill=DIM)
    return cell


S = lambda n: t1080 / (n + '.png')
CENTRE = (960, 540)

sheet('01-before-after-workshop-plaza.jpg', 'same build, same place: B3 presentation (before) vs B4 (after)',
      [tile(S('before-b3-workshop-front'), 'BEFORE (B3 view): workshop front'), tile(S('after-b4-workshop-front'), 'AFTER (B4): workshop front'),
       tile(S('before-b3-plaza'), 'BEFORE (B3 view): plaza'), tile(S('after-b4-plaza'), 'AFTER (B4): plaza')], 2)
sheet('02-before-after-side-willow-spring.jpg', 'same build, same place: B3 presentation (before) vs B4 (after)',
      [tile(S('before-b3-workshop-side'), 'BEFORE: workshop side'), tile(S('after-b4-workshop-side'), 'AFTER: workshop side'),
       tile(S('before-b3-willow'), 'BEFORE: willow'), tile(S('after-b4-willow'), 'AFTER: willow'),
       tile(S('before-b3-spring'), 'BEFORE: spring (S0)'), tile(S('after-b4-spring'), 'AFTER: spring (S0, dormant painting)')], 2)
sheet('03-camera-pitch-42-48-54.jpg', 'camera pitch candidates (default 48), yaw 45, distance 14 m',
      [tile(S('pitch-%d-%s' % (p, spot)), 'pitch %d, %s' % (p, spot), size=(800, 450)) for spot in ('workshop-front', 'plaza') for p in (42, 48, 54)], 3)
sheet('04-caretaker-idle-and-walk-crops.jpg', 'caretaker at the closest zoom (10 m): idle (top) and walking (bottom), crops of real frames',
      [tile(S('hero-idle-%s-closest-zoom' % d), 'idle %s' % d, size=(470, 470), crop=CENTRE + (640, 640)) for d in 'SWEN']
      + [tile(S('hero-walk-%s-closest-zoom' % d), 'walking %s' % d, size=(470, 470), crop=CENTRE + (760, 760)) for d in 'SWEN'], 4)
frames = root / 'artifacts/art-work/phase52b4/hero/game-scale-frames.png'
heads = root / 'artifacts/art-work/phase52b4/hero/onion-skin-heads.png'
cells = [wide('', '', frames, 'every caretaker frame at game scale: idle (4 of 12) and run (8) for S, W, E, N', 'PRODUCTION_ART'),
         wide('', '', heads, 'head-size check across directions (overlay)', 'PRODUCTION_ART', width=1200)]
w = max(c.width for c in cells)
im = Image.new('RGB', (w + 16, sum(c.height for c in cells) + 8 * (len(cells) + 1) + 44), BG)
ImageDraw.Draw(im).text((10, 10), 'Echoes of the Valley · Phase 5.2-B4 · the caretaker frames the game draws (made from the owner\'s B4 drawings)', font=TITLE, fill=INK)
y = 52
for c in cells:
    im.paste(c, (8, y))
    y += c.height + 8
im.save(out / '05-caretaker-production-frames.jpg', quality=88, optimize=True)
print('05-caretaker-production-frames.jpg', im.size)
sheet('06-occlusion-and-known-issues.jpg', 'occlusion and the worst known issues',
      [tile(S('occlusion-behind-workshop'), 'player behind the painted workshop: silhouette through the roof', crop=(900, 560, 900, 600)),
       tile(S('occlusion-behind-willow'), 'player under the painted willow: canopy opens around her', crop=(1060, 560, 1000, 640)),
       tile(S('stage-S3-set-by-script-spring'), 'KNOWN ISSUE: from S1 the B2 bloom tree hides her (stage SET BY SCRIPT)'),
       tile(t720 / 'selected-frames/064.6s-to-spring-W-run.jpg', 'KNOWN ISSUE: B2 low-poly tree canopy (bottom right) from 48 degrees', category='REAL_PLAYER_VIDEO frame')], 2)
strip = ev / 'minimap-strip.png'
ui = [tile(S('dialogue-mira'), 'dialogue with the minimap, 1080p'), tile(S('inventory-portrait'), 'satchel, 1080p'),
      tile(S('pause-menu'), 'pause, 1080p'), tile(t720 / 'pause-menu.png', 'pause, 720p')]
top = wide('', '', strip, 'minimap crops from the recorded route: the map turns/scrolls, objective Mira -> Spring -> Heart', 'REAL_PLAYER_SCREENSHOT', width=1928)
w = ui[0].width * 2 + 24
im = Image.new('RGB', (w, top.height + 2 * ui[0].height + 44 + 32), BG)
d = ImageDraw.Draw(im)
d.text((10, 10), 'Echoes of the Valley · Phase 5.2-B4 · the real minimap and the UI around it', font=TITLE, fill=INK)
im.paste(top, (8, 52))
for i, c in enumerate(ui):
    im.paste(c, (8 + (i % 2) * (c.width + 8), 52 + top.height + 8 + (i // 2) * (c.height + 8)))
im.save(out / '07-minimap-and-ui.jpg', quality=88, optimize=True)
print('07-minimap-and-ui.jpg', im.size)
sheet('08-stages-S0-to-S3.jpg', 'restoration stages: S0 played; S1, S2, S3 SET BY THE EVIDENCE SCRIPT (not reached by play)',
      [tile(S('after-b4-workshop-front'), 'workshop S0', size=(640, 360))]
      + [tile(S('stage-%s-set-by-script-workshop-front' % s), 'workshop %s (script)' % s, size=(640, 360)) for s in ('S1', 'S2', 'S3')]
      + [tile(S('after-b4-spring'), 'spring S0', size=(640, 360))]
      + [tile(S('stage-%s-set-by-script-spring' % s), 'spring %s (script)' % s, size=(640, 360)) for s in ('S1', 'S2', 'S3')], 4)
concept = root / 'game/Assets/Echoes/Art/Phase52B4/Source/art-direction/b4_isometric_style_target.png'
sheet('09-concept-target-vs-real-game.jpg', 'the owner\'s style target (CONCEPT ART, NOT GAME FOOTAGE) next to the real B4 player',
      [tile(concept, 'style target: concept painting, not the game', category='CONCEPT_ART'), tile(S('after-b4-workshop-front'), 'real B4 Windows player')], 2)
cs = wide('', '', t720 / 'contact-sheet.png', 'one real frame every 2 s of the 82 s recorded route (AUTOMATED input); captions: time, step, direction', 'REAL_PLAYER_VIDEO frames', width=1920)
im = Image.new('RGB', (cs.width + 16, cs.height + 60), BG)
ImageDraw.Draw(im).text((10, 10), 'Echoes of the Valley · Phase 5.2-B4 · contact sheet of motion/actual-player-b4-motion.mp4', font=TITLE, fill=INK)
im.paste(cs, (8, 52))
im.save(out / '10-video-contact-sheet.jpg', quality=88, optimize=True)
print('10-video-contact-sheet.jpg', im.size)
