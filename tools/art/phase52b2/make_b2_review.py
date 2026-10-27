"""Curated independent-review package for Phase 5.2-B2 (screenshots, video, tests, logs, changed source, provenance, hashes).

python tools/art/phase52b2/make_b2_review.py            # after the final evidence exists and the work is committed
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


def add_tree(folder, prefix, pattern='*', skip=()):
    folder = Path(folder)
    if not folder.is_dir():
        print('MISSING folder (skipped):', folder)
        return
    for p in sorted(folder.rglob(pattern)):
        if p.is_file() and not any(s in p.parts for s in skip) and 'frames' not in p.parts:
            add(p, prefix + '/' + p.relative_to(folder).as_posix())


add(qa / 'README_FIRST_zh.md', 'README_FIRST_zh.md')
add(qa / 'final-report.md', 'final-report.md')
add_tree(qa / 'comparison', 'comparison')
for tag in ('after720', 'after1080', 'baseline720'):
    add_tree(qa / 'player' / tag, 'player/' + tag, '*', skip=('save',))
for tag in ('unrecorded720', 'unrecorded1080', 'smoke720', 'smoke1080'):
    add_tree(qa / 'player' / tag, 'player/' + tag, '*', skip=('save',))
add_tree(qa / 'diagnostics-before-fix', 'terrain/diagnostics-before-fix')
add_tree(qa / 'diagnostics-after-fix', 'terrain/diagnostics-after-fix')
add_tree(qa / 'model-preview/unity', 'model-diagnostics-NOT-PLAYER-EVIDENCE/unity')
add_tree(qa / 'unity-preview', 'model-diagnostics-NOT-PLAYER-EVIDENCE/pilot-scene-editor')
add_tree(qa / 'logs', 'logs')
add_tree(qa / 'tests', 'tests')
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
add(root / 'docs/03-RULES-CREDITS.md', 'provenance/03-RULES-CREDITS.md')
add(root / 'docs/08-PHASE52-B2.md', 'provenance/08-PHASE52-B2.md')

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
