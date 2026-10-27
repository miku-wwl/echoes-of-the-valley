"""Curated independent-review package for Phase 5.2-B2 (screenshots, video, tests, logs, changed source, provenance, hashes).

python tools/art/phase52b2/make_b2_review.py [tests-folder] [build-dir]   # after the final evidence exists and the work is committed
Never packages the official theme recording, other people's art or the rejected desktop capture. Never overwrites an existing package."""
import hashlib, json, subprocess, sys, zipfile
from datetime import datetime
from pathlib import Path

root = Path(__file__).resolve().parents[3]
qa = root / 'artifacts/visual-qa/phase-5.2-b2'
old_qa = root / 'artifacts/visual-qa/phase-5.2'
out_dir = root / 'artifacts/review/phase52-b2'
name = 'Echoes-Phase52-B2-ChatGPT-Review'


def git(*args):
    return subprocess.check_output(['git', *args], cwd=root, text=True).strip()


head = git('rev-parse', 'HEAD')
base = '5f90a6bf906f95d3c4c344ee940df2e85fcd58a4'
files = {}


def add(path, member):
    path = Path(path)
    if not path.is_file():
        print('MISSING (skipped):', path)
        return
    assert member not in files, member
    files[member] = path


omitted = []


def add_tree(folder, prefix, pattern='*', skip=(), keep=None):
    """keep(path) -> bool filters bulky files; anything left out is listed with its SHA-256 in OMITTED-FILES-SHA256.txt (the files stay on disk)."""
    folder = Path(folder)
    if not folder.is_dir():
        print('MISSING folder (skipped):', folder)
        return
    for p in sorted(folder.rglob(pattern)):
        if p.is_file() and not any(s in p.parts for s in skip) and 'frames' not in p.parts:
            if keep is not None and not keep(p):
                omitted.append('%s  %d  %s' % (hashlib.sha256(p.read_bytes()).hexdigest(), p.stat().st_size, p.relative_to(root).as_posix()))
                continue
            add(p, prefix + '/' + p.relative_to(folder).as_posix())


# the whole-game smoke runs contribute the art-audit shots (S0/S3 hero, workshop, garden, NPCs, Clover) and the UI screens named in the brief
SMOKE_KEEP = ('A', 'NPC', 'Clover', '02-', '03-', '05-', '06-', '15-')
smoke_keep = lambda p: not p.name.lower().endswith('.png') or p.name.startswith(SMOKE_KEEP)
no_stills = lambda p: not p.name.lower().endswith('.png')          # unrecorded runs exist for frame times; their stills duplicate the recorded run


add(qa / 'README_FIRST_zh.md', 'README_FIRST_zh.md')
add(qa / 'final-report.md', 'final-report.md')
add_tree(qa / 'comparison', 'comparison')
for tag in ('after720', 'after1080', 'baseline720'):
    add_tree(qa / 'player' / tag, 'player/' + tag, '*', skip=('save',))
no_video = lambda p: not p.name.lower().endswith('.mp4')        # the 1080p recording is only mined for stills; its video stays local (hash listed)
add_tree(qa / 'player' / 'after1080-motion', 'player/after1080-motion', '*', skip=('save',), keep=no_video)
for tag in ('unrecorded720', 'unrecorded1080'):
    add_tree(qa / 'player' / tag, 'player/' + tag, '*', skip=('save',), keep=no_stills)
for tag in ('smoke720', 'smoke1080'):
    add_tree(qa / 'player' / tag, 'player/' + tag, '*', skip=('save',), keep=smoke_keep)
add_tree(qa / 'diagnostics-before-fix', 'terrain/diagnostics-before-fix')
add_tree(qa / 'diagnostics-after-fix', 'terrain/diagnostics-after-fix')
# Editor-rendered bisect of the white ground glint (found in the first B2 build, root cause and fix in docs/08-PHASE52-B2.md)
for tag in ('diagnostics-glow', 'diagnostics-glow2', 'diagnostics-glow3'):
    add_tree(qa / tag, 'terrain/ground-glint-bisect-EDITOR-RENDERS/' + tag)
add_tree(qa / 'model-preview/unity-r2c', 'model-diagnostics-NOT-PLAYER-EVIDENCE/unity-final-hero')
add_tree(qa / 'unity-preview', 'model-diagnostics-NOT-PLAYER-EVIDENCE/pilot-scene-editor')
add_tree(qa / 'logs', 'logs')
tests_folder = sys.argv[1] if len(sys.argv) > 1 else 'b2-verification3'
build_dir = root / (sys.argv[2] if len(sys.argv) > 2 else 'artifacts/build/phase52-b2-r2')
add_tree(old_qa / 'logs' / tests_folder, 'tests')
build_manifest = ['SHA-256  size  file (build %s)' % build_dir.name]
for p in sorted(build_dir.rglob('*')):
    if p.is_file():
        build_manifest.append('%s  %d  %s' % (hashlib.sha256(p.read_bytes()).hexdigest(), p.stat().st_size, p.relative_to(build_dir).as_posix()))
(qa / 'build-manifest-SHA256.txt').write_text('\n'.join(build_manifest) + '\n', encoding='utf-8')
add(qa / 'build-manifest-SHA256.txt', 'build/build-manifest-SHA256.txt')
# previous Gate B failure evidence kept for comparison (same camera positions)
for rel in ('motion/after720-2/hero-front.png', 'motion/after720-2/hero-side.png', 'motion/after720-2/hero-rear.png', 'motion/after720-2/video-frame-20.png', 'motion/after720-2/video-frame-30.png'):
    add(old_qa / rel, 'previous-gate-b-failure/' + Path(rel).name)
# changed source: every tracked file that differs from the failed Gate B commit (except bulky generated assets that are listed in the manifest only)
changed = [c for c in git('diff', '--name-status', base, 'HEAD').splitlines()]
(qa / 'git-change-summary.txt').write_text('base ' + base + '\nhead ' + head + '\n' + git('diff', '--stat', base, 'HEAD') + '\n\n' + '\n'.join(changed) + '\n', encoding='utf-8')
add(qa / 'git-change-summary.txt', 'source/git-change-summary.txt')
for line in changed:
    status, _, path = line.partition('\t')
    if status.startswith('D') or not (root / path).is_file():
        continue
    if path.startswith(('game/Assets/Echoes/Scripts/', 'game/Assets/Echoes/Tests/', 'tools/art/phase52b2/', 'tools/art/phase52/', 'docs/', 'README.md')) or path.endswith(('.shader', 'hero-skin.json')) and 'Source' not in path:
        add(root / path, 'source/' + path)
for tex in ('Skin', 'Skin_Blink', 'Hair', 'Teal', 'Cream', 'Burgundy', 'Leather', 'Tights', 'Trinket', 'RouteGrass', 'RouteDirt', 'LeafClusters', 'GroundPlants'):
    add(root / 'game/Assets/Echoes/Art/Phase52/Textures' / (tex + '.png'), 'source/painted-textures/' + tex + '.png')    # code-painted atlases, included so the paint itself can be judged
add(root / 'docs/03-RULES-CREDITS.md', 'provenance/03-RULES-CREDITS.md')
add(root / 'docs/08-PHASE52-B2.md', 'provenance/08-PHASE52-B2.md')

if omitted:
    header = 'SHA-256  size  path (left out of the zip to keep it reviewable; the files remain in the local artifacts/ folder)'
    (qa / 'OMITTED-FILES-SHA256.txt').write_text(header + chr(10) + chr(10).join(omitted) + chr(10), encoding='utf-8')
    add(qa / 'OMITTED-FILES-SHA256.txt', 'player/OMITTED-FILES-SHA256.txt')
manifest = []
for member, path in files.items():
    manifest.append((member, hashlib.sha256(path.read_bytes()).hexdigest(), path.stat().st_size))
lines = ['SHA-256  size  member', '# package for commit ' + head] + ['%s  %d  %s' % (h, s, m) for m, h, s in manifest]
stamp = datetime.now().strftime('%Y%m%d-%H%M%S')
out_dir.mkdir(parents=True, exist_ok=True)
zip_path = out_dir / (name + '.zip')
if zip_path.exists():
    zip_path = out_dir / (name + '-' + stamp + '.zip')
with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for member, path in files.items():
        z.write(path, member)
    z.writestr('MANIFEST-SHA256.txt', '\n'.join(lines) + '\n')
digest = hashlib.sha256(zip_path.read_bytes()).hexdigest().upper()
(out_dir / (zip_path.name + '.sha256.txt')).write_text(digest + '  ' + zip_path.name + '\n', encoding='utf-8')
with zipfile.ZipFile(zip_path) as z:
    assert z.testzip() is None
    print('ZIP', zip_path, 'members', len(z.namelist()), 'bytes', zip_path.stat().st_size)
print('REVIEW_ZIP_SHA256=' + digest)
