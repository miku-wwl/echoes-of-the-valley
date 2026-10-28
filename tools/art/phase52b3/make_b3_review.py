"""Assembles and zips the Phase 5.2-B3 independent-review package (artifacts/review/phase52-b3/).

python tools/art/phase52b3/make_b3_review.py <evidence-root> <tests-folder> <build-dir> <build-log-dir>
Every image is labelled by category in its folder name and in comparison/index.html: CONCEPT_ART (owner-supplied AI illustrations),
PRODUCTION_SPRITES (what the game draws), EDITOR_PREVIEW (Unity Editor renders, diagnostics only), REAL_PLAYER_SCREENSHOT and
REAL_PLAYER_VIDEO (actual Windows player, AUTOMATED input). Never overwrites an existing package; never includes Unity caches, the
official theme recording or unrelated files.
"""
import hashlib
import html
import json
import shutil
import subprocess
import sys
import zipfile
from datetime import datetime
from pathlib import Path

from PIL import Image

root = Path(__file__).resolve().parents[3]
ev = (root / sys.argv[1]).resolve()
tests = (root / sys.argv[2]).resolve()
build_dir = (root / sys.argv[3]).resolve()
build_logs = (root / sys.argv[4]).resolve()
stage = root / 'artifacts/review/phase52-b3/staging'
out_dir = root / 'artifacts/review/phase52-b3'
name = 'Echoes-Phase52-B3-2D5D-Review'
base = 'aef87c09cd6ceee8ed183e59608c7b833268e481'


def git(*a):
    return subprocess.check_output(['git', *a], cwd=root, text=True).strip()


if stage.exists():
    shutil.rmtree(stage)
stage.mkdir(parents=True)


staged = {}
omitted = []


def put(src, rel):
    src = Path(src)
    if not src.is_file():
        print('MISSING (skipped):', src)
        return None
    dst = stage / rel
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(src, dst)
    staged[str(src.resolve())] = rel
    return dst


def omit(src):
    src = Path(src)
    omitted.append('%s  %d  %s' % (hashlib.sha256(src.read_bytes()).hexdigest(), src.stat().st_size, src.relative_to(root).as_posix()))


def put_tree(src, rel, keep=lambda p: True):
    """Stages a folder; files that keep() rejects are listed with their hash in OMITTED-FILES-SHA256.txt (they stay on disk)."""
    src = Path(src)
    if not src.is_dir():
        print('MISSING folder (skipped):', src)
        return
    for p in sorted(src.rglob('*')):
        if p.is_file() and 'frames' not in p.parts and 'save' not in p.parts:
            if keep(p):
                put(p, rel + '/' + p.relative_to(src).as_posix())
            else:
                omit(p)


# ---- top level documents -------------------------------------------------------------------------------------------------------------
put(ev / 'README_FIRST_zh.md', 'README_FIRST_zh.md')
put(ev / 'final-report.md', 'final-report.md')

# ---- motion: the real player video, contact sheet and selected frames (REAL_PLAYER_VIDEO) --------------------------------------------
video = ev / 'player/tour720-video'
put(video / 'actual-player-2d5d-motion.mp4', 'motion/actual-player-2d5d-motion.mp4')
put(video / 'contact-sheet.png', 'motion/contact-sheet.png')
put_tree(video / 'selected-frames', 'motion/selected-frames')
for f in ('capture-provenance.json', 'tour-stats.json', 'tour.json', 'frame-manifest.json', 'process-result.json', 'ffmpeg.log'):
    put(video / f, 'motion/run-data/' + f)

# ---- real player screenshots (REAL_PLAYER_SCREENSHOT) -------------------------------------------------------------------------------
for tag in ('tour1080', 'tour1080-legacy-b2-hero'):
    put_tree(ev / 'player' / tag, 'real-player-screenshots/' + tag, keep=lambda p: p.suffix == '.png' or p.name in ('tour-stats.json', 'process-result.json'))
put_tree(ev / 'player/tour720-video', 'real-player-screenshots/tour720-video', keep=lambda p: p.name in ('dialogue-mira.png', 'inventory-portrait.png', 'hero-front-S-closest-zoom.png'))
SMOKE_KEEP = ('A1-', 'A2-', 'A3-', 'NPC', 'Clover', '03-', '05-', '06-', '13a', '15-')
put_tree(ev / 'player/smoke1080', 'real-player-screenshots/smoke1080', keep=lambda p: p.suffix == '.json' or (p.suffix == '.png' and p.name.startswith(SMOKE_KEEP)))
put_tree(ev / 'player/smoke720', 'real-player-screenshots/smoke720', keep=lambda p: p.suffix == '.json')
for tag in ('tour720-unrecorded', 'tour1080-unrecorded'):
    put_tree(ev / 'player' / tag, 'performance/' + tag, keep=lambda p: p.suffix == '.json')

t1080 = ev / 'player/tour1080'
legacy = ev / 'player/tour1080-legacy-b2-hero'
b2 = root / 'artifacts/visual-qa/phase-5.2-b2/player/after1080'
src_pack = root / 'tools/art/phase52b3/source'
views = root / 'artifacts/art-work/phase52b3/views'
sprites = root / 'artifacts/art-work/phase52b3/sprites'
# ---- character source assets ----------------------------------------------------------------------------------------------------------
for f in ('01-heroine-portrait.png', '02-npc-lineup.png', '05-heroine-turnaround-reference.png', 'SOURCES_AND_RIGHTS.md'):
    put(src_pack / f, 'character-source/CONCEPT_ART-owner-supplied/' + f)
put_tree(views, 'character-source/extracted-views')
put_tree(root / 'game/Assets/Echoes/Art/Phase52B3/Sprites', 'character-source/PRODUCTION_SPRITES', keep=lambda p: p.suffix in ('.png', '.json'))
put_tree(root / 'game/Assets/Echoes/Art/Phase52B3/Portraits', 'character-source/PRODUCTION_SPRITES/portraits', keep=lambda p: p.suffix == '.png')
put(sprites / 'contact-sheet-game-scale.png', 'character-source/PRODUCTION_SPRITES/contact-sheet-game-scale.png')
put(sprites / 'portraits-sheet.png', 'character-source/PRODUCTION_SPRITES/portraits-sheet.png')
put_tree(root / 'artifacts/visual-qa/phase-5.2-b3/editor-preview/p4', 'EDITOR_PREVIEW-not-player-evidence/camera-candidates', keep=lambda p: p.name.startswith('camera-') and p.name.endswith('-plaza.png') or p.name == 'NOTE.txt')

# ---- comparison page -------------------------------------------------------------------------------------------------------------------
cmp = stage / 'comparison'
sections = []


def pair(group, title, items):
    """items: list of (label, category, source path)."""
    figs = []
    for label, category, src in items:
        if src is None or not Path(src).is_file():
            continue
        key = str(Path(src).resolve())
        if key in staged:
            rel = '../' + staged[key]                      # already in the package: link it, do not copy it twice
        else:
            rel = '%s/%s' % (group, Path(src).name)
            dst = cmp / rel
            dst.parent.mkdir(parents=True, exist_ok=True)
            if dst.exists():
                rel = '%s/%s-%s' % (group, category.lower(), Path(src).name)
                dst = cmp / rel
            shutil.copyfile(src, dst)
        figs.append('<figure><img src="%s" loading="lazy"><figcaption><b>%s</b> &middot; %s</figcaption></figure>' % (html.escape(rel), html.escape(category), html.escape(label)))
    if figs:
        sections.append('<section><h2>%s</h2><div class="row">%s</div></section>' % (html.escape(title), ''.join(figs)))


t1080 = ev / 'player/tour1080'
legacy = ev / 'player/tour1080-legacy-b2-hero'
b2 = root / 'artifacts/visual-qa/phase-5.2-b2/player/after1080'
src_pack = root / 'tools/art/phase52b3/source'
views = root / 'artifacts/art-work/phase52b3/views'
sprites = root / 'artifacts/art-work/phase52b3/sprites'
for d, label in (('front-S', 'facing the camera (S)'), ('side-W', 'facing screen-left (W)'), ('side-E', 'facing screen-right (E)'), ('rear-N', 'facing away (N)')):
    pair('original-b2-vs-new', 'Same camera, same place: B2 3D hero vs B3 illustrated sprite, %s' % label,
         [('B2 3D hero under the B3 camera (comparison mode)', 'REAL_PLAYER_SCREENSHOT', legacy / ('hero-%s-closest-zoom.png' % d)),
          ('B3 2.5D sprite', 'REAL_PLAYER_SCREENSHOT', t1080 / ('hero-%s-closest-zoom.png' % d))])
pair('original-b2-vs-new', 'B2 as it was reviewed (its own free camera) vs B3 at the gameplay camera',
     [('B2 hero, front, B2 camera', 'REAL_PLAYER_SCREENSHOT', b2 / 'hero-front.png'), ('B2 hero, rear, B2 camera', 'REAL_PLAYER_SCREENSHOT', b2 / 'hero-rear.png'),
      ('B3 at the default gameplay zoom', 'REAL_PLAYER_SCREENSHOT', t1080 / 'hero-front-S-default-zoom.png')])
pair('character-views', 'Source illustration (owner-supplied AI turnaround) vs production sprite frames vs the real player',
     [('05 turnaround (front, side, back)', 'CONCEPT_ART', src_pack / '05-heroine-turnaround-reference.png'),
      ('All production frames at game scale (S, W, E, N; run and idle)', 'PRODUCTION_SPRITES', sprites / 'contact-sheet-game-scale.png')])
pair('character-views', 'Diagonals (four drawn directions: diagonals show the side views)',
     [('facing SE', 'REAL_PLAYER_SCREENSHOT', t1080 / 'hero-diagonal-SE-closest-zoom.png'), ('facing NW', 'REAL_PLAYER_SCREENSHOT', t1080 / 'hero-diagonal-NW-closest-zoom.png')])
pair('portrait-comparison', 'Portrait consistency: the same caretaker in the bust, the satchel and the world',
     [('01 portrait (owner-supplied AI illustration)', 'CONCEPT_ART', src_pack / '01-heroine-portrait.png'),
      ('Dialogue with Mira', 'REAL_PLAYER_SCREENSHOT', t1080 / 'dialogue-mira.png'), ('Dialogue with Wren', 'REAL_PLAYER_SCREENSHOT', t1080 / 'dialogue-wren.png'),
      ('Satchel (inventory) portrait', 'REAL_PLAYER_SCREENSHOT', t1080 / 'inventory-portrait.png'), ('Beside Mira in the world', 'REAL_PLAYER_SCREENSHOT', t1080 / 'beside-npc-mira.png')])
for loc, label in (('workshop', 'Workshop'), ('windmill', 'Windmill'), ('garden', 'Garden'), ('north-path', 'Walking route (north path)'), ('under-willow', 'Under the willow canopy'), ('plaza', 'Plaza')):
    pair('environment-comparison', '%s: B2 3D hero vs B3 sprite (same B3 camera)' % label,
         [('B2 3D hero (comparison mode)', 'REAL_PLAYER_SCREENSHOT', legacy / ('location-%s.png' % loc)), ('B3 2.5D', 'REAL_PLAYER_SCREENSHOT', t1080 / ('location-%s.png' % loc))])
pair('environment-comparison', 'Editor previews used to choose the camera pitch (diagnostic, not player evidence)',
     [(p.stem, 'EDITOR_PREVIEW', p) for p in sorted((root / 'artifacts/visual-qa/phase-5.2-b3/editor-preview/p4').glob('camera-p*-plaza.png'))])
page = '''<!doctype html><html lang="en"><meta charset="utf-8"><title>Echoes B3 review</title>
<style>body{font:15px/1.5 system-ui,sans-serif;background:#18201e;color:#ece6d6;margin:0;padding:0 16px 40px}h1{font-size:22px}h2{font-size:16px;margin:26px 0 8px}
.row{display:grid;grid-template-columns:repeat(auto-fit,minmax(340px,1fr));gap:12px}figure{margin:0}img{width:100%%;height:auto;display:block;border:1px solid #3c4b49}
figcaption{font-size:13px;color:#cfc8b2;padding:4px 0}.note{max-width:960px;color:#d8d0bb}b{color:#f2d48a}</style>
<h1>Echoes of the Valley &middot; Phase 5.2-B3 (2.5D pilot) &middot; comparisons</h1>
<p class="note">Labels: <b>CONCEPT_ART</b> = owner-supplied AI illustrations (the source of the sprites); <b>PRODUCTION_SPRITES</b> = frames the game draws;
<b>EDITOR_PREVIEW</b> = Unity Editor renders used for decisions, not player evidence; <b>REAL_PLAYER_SCREENSHOT</b> = unmodified framebuffer captures of the
Windows player driven by <b>automated</b> input (no human played). Captions are outside the pixels. This is the author's evidence, not an approval:
<b>GATE_B_OWNER_APPROVAL=PENDING</b>.</p>
%s</html>''' % '\n'.join(sections)
cmp.mkdir(parents=True, exist_ok=True)
(cmp / 'index.html').write_text(page, encoding='utf-8')

# ---- tests and logs -------------------------------------------------------------------------------------------------------------------------
put(tests / 'tests-EditMode.xml', 'tests/EditMode.xml')
put(tests / 'tests-PlayMode.xml', 'tests/PlayMode.xml')
put(build_logs / 'Build.log', 'logs/build.log')
put(video / 'player.log', 'logs/player.log')
for tag in ('tour1080', 'tour1080-legacy-b2-hero', 'tour720-unrecorded', 'tour1080-unrecorded', 'smoke720', 'smoke1080'):
    put(ev / 'player' / tag / 'player.log', 'logs/player-%s.log' % tag)
put(ev / 'player/evidence-run-summary.json', 'logs/evidence-run-summary.json')

# ---- source: changed files and git summary --------------------------------------------------------------------------------------------
head = git('rev-parse', 'HEAD')
changed = git('diff', '--name-status', base, 'HEAD')
(stage / 'source').mkdir(exist_ok=True)
(stage / 'source/changed-files.txt').write_text('base ' + base + '\nhead ' + head + '\n' + changed + '\n', encoding='utf-8')
(stage / 'source/git-change-summary.txt').write_text(git('log', '--oneline', base + '..HEAD') + '\n\n' + git('diff', '--stat', base, 'HEAD') + '\n', encoding='utf-8')
for line in changed.splitlines():
    status, _, path = line.partition('\t')
    p = root / path
    if status.startswith('D') or not p.is_file():
        continue
    if p.suffix in ('.cs', '.py', '.ps1', '.shader', '.md', '.json') and p.stat().st_size < 400_000 and '/Scenes/' not in path:
        put(p, 'source/files/' + path)

# ---- provenance --------------------------------------------------------------------------------------------------------------------------
for f in ('asset-manifest.md', 'AI-disclosure.md', 'license-notes.md'):
    put(root / 'tools/art/phase52b3/provenance' / f, 'provenance/' + f)
put(root / 'docs/09-PHASE52-B3.md', 'provenance/09-PHASE52-B3.md')
put(root / 'docs/03-RULES-CREDITS.md', 'provenance/03-RULES-CREDITS.md')

# ---- build manifest, labels, hashes, zip -----------------------------------------------------------------------------------------------------
lines = ['SHA-256  size  file (build %s)' % build_dir.name]
for p in sorted(build_dir.rglob('*')):
    if p.is_file():
        lines.append('%s  %d  %s' % (hashlib.sha256(p.read_bytes()).hexdigest(), p.stat().st_size, p.relative_to(build_dir).as_posix()))
(stage / 'logs/build-manifest-SHA256.txt').write_text('\n'.join(lines) + '\n', encoding='utf-8')
(stage / 'LABELS.md').write_text('''# Labels used in this package

| Label / folder | Meaning |
| --- | --- |
| `character-source/CONCEPT_ART-owner-supplied/` | Owner-supplied AI illustrations (OpenAI image generation). The source of the sprites. Not game renders. |
| `character-source/PRODUCTION_SPRITES/` | The atlases and busts the game actually draws (resampled from the concept art by the pipeline in `source/files/tools/art/phase52b3`). |
| `EDITOR_PREVIEW-not-player-evidence/` | Unity Editor renders used to choose the camera. Not Windows-player evidence. |
| `real-player-screenshots/`, `motion/selected-frames/` | REAL_PLAYER_SCREENSHOT: unmodified captures of the Windows player, AUTOMATED input. |
| `motion/actual-player-2d5d-motion.mp4` | REAL_PLAYER_VIDEO: the Windows player's own framebuffer frames at their real intervals, AUTOMATED input, no audio. |
''', encoding='utf-8')
if omitted:
    header = 'SHA-256  size  path (left out to keep the package reviewable; the files stay in the local artifacts/ folder)'
    (stage / 'OMITTED-FILES-SHA256.txt').write_text(header + chr(10) + chr(10).join(omitted) + chr(10), encoding='utf-8')
manifest = []
for p in sorted(stage.rglob('*')):
    if p.is_file():
        manifest.append('%s  %d  %s' % (hashlib.sha256(p.read_bytes()).hexdigest(), p.stat().st_size, p.relative_to(stage).as_posix()))
(stage / 'MANIFEST-SHA256.txt').write_text('SHA-256  size  member\n# package for commit %s\n' % head + '\n'.join(manifest) + '\n', encoding='utf-8')
out_dir.mkdir(parents=True, exist_ok=True)
zip_path = out_dir / (name + '.zip')
if zip_path.exists():
    zip_path = out_dir / (name + '-' + datetime.now().strftime('%Y%m%d-%H%M%S') + '.zip')
with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for p in sorted(stage.rglob('*')):
        if p.is_file():
            z.write(p, p.relative_to(stage).as_posix())
digest = hashlib.sha256(zip_path.read_bytes()).hexdigest().upper()
(out_dir / (zip_path.name + '.sha256.txt')).write_text(digest + '  ' + zip_path.name + '\n', encoding='utf-8')
with zipfile.ZipFile(zip_path) as z:
    assert z.testzip() is None
    print('ZIP', zip_path, 'members', len(z.namelist()), 'bytes', zip_path.stat().st_size)
print('REVIEW_ZIP_SHA256=' + digest)
