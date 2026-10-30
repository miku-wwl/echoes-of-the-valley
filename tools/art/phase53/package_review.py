"""Package completed Phase53 evidence without altering raw captures or earlier packages.
python -I tools/art/phase53/package_review.py <final-build-dir> <test-log-dir> <build-log>
Requires final-report.md and delivery.json in artifacts/visual-qa/phase-5.3.
Raw frame JPEGs and binary build remain separate; every omitted raw frame is hashed by the capture manifest.
"""
import hashlib
import html
import json
import shutil
import sys
import zipfile
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
EV = ROOT / 'artifacts/visual-qa/phase-5.3'
BUILD, TESTS, LOG = [(ROOT / p).resolve() for p in sys.argv[1:4]]
OUT = ROOT / 'artifacts/review/phase53'
STAMP = datetime.now().strftime('%Y%m%d-%H%M%S')
STAGE = OUT / ('review-' + STAMP)
STAGE.mkdir(parents=True, exist_ok=False)


def put(path, relative):
    assert path.is_file(), path
    dst = STAGE / relative
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(path, dst)


for name in ['final-report.md', 'delivery.json', 'gate-a.json', 'build-inputs-SHA256.json', 'original-tests-preserved.json', 'windows-package.json', 'remote-lfs-verification.json', 'commit-input-verification.json', 'git-preflight.json', 'performance-comparison.md', 'performance-comparison.json']:
    put(EV / name, name)
for path in (EV / 'asset-audit').glob('*'):
    if path.is_file(): put(path, Path('SOURCE_AUDIT') / path.name)
for path in TESTS.glob('*'):
    if path.is_file(): put(path, Path('TESTS') / path.name)
put(LOG, 'BUILD/final-build.log')
PLAYER = EV / 'player-final-v2'
for path in PLAYER.rglob('*'):
    if not path.is_file() or 'frames' in path.relative_to(PLAYER).parts or 'save' in path.relative_to(PLAYER).parts:
        continue
    put(path, Path('REAL_WINDOWS_PLAYER_AUTOMATED') / path.relative_to(PLAYER))
for name in ['README.md', 'docs/12-PHASE53.md', 'docs/03-RULES-CREDITS.md', 'tools/art/phase53/provenance.md']:
    put(ROOT / name, Path('DOCUMENTATION') / Path(name).name)
for path in (ROOT / 'tools/art/phase53/source-docs').glob('*'):
    put(path, Path('SOURCE_PACK_DOCUMENTS') / path.name)
for path in (ROOT / 'game/Assets/Echoes/Art/Phase53/Source').glob('*.png'):
    put(path, Path('SOURCE_ART_NOT_GAME_FOOTAGE') / path.name)
for path in (EV / 'comparison-sheets').glob('*'):
    if path.is_file(): put(path, Path('LABELLED_COMPARISON_COMPOSITES') / path.name)

(STAGE / 'READ_FIRST.txt').write_text(
    'Phase 5.3 review evidence, not a released or owner-approved game.\n'
    'Real Windows Player screenshots/video are unmodified captures with AUTOMATED controller input.\n'
    'Source art and labelled comparison sheets are not continuous game footage.\n'
    'Post-route stills allow setup teleports; S0-S3 stills named SET-BY-SCRIPT force stage for visual comparison.\n'
    'Whole-game smoke independently uses the real quest/crafting/chicken/save/ending entry points.\n'
    'MP4 is silent; runtime FPS and capture/encoding FPS are separate.\n'
    'Human playtest and owner visual approval PENDING. See final-report.md for all PARTIAL gates.\n'
    'Raw frame hashes: REAL_WINDOWS_PLAYER_AUTOMATED/tour1080-video/frame-manifest.json.\n', encoding='utf-8')
viewer=['<!doctype html><meta charset="utf-8"><title>Echoes Phase 5.3 review</title>',
        '<style>body{font:17px system-ui;background:#152126;color:#ece7d8;max-width:1200px;margin:30px auto;padding:20px}a{color:#aee4ff}img,video{max-width:100%}figure{margin:30px 0}code{overflow-wrap:anywhere}</style>',
        '<h1>Echoes of the Valley — Phase 5.3</h1><p>Engineering PASS. Visual gate and final status PARTIAL. Human playtest and owner approval PENDING.</p>',
        '<p><a href="final-report.md">Full report</a> · <a href="delivery.json">Machine-readable gates</a> · <a href="FILES-SHA256.json">Every file SHA-256</a></p>',
        '<h2>Real Windows Player video — AUTOMATED input</h2><p>60.28 s, 1080p, silent. Actual capture about 13 FPS due to synchronous readback/JPEG. Unrecorded performance is separate. GUI playback NOT VERIFIED.</p>',
        '<video controls preload="metadata" src="REAL_WINDOWS_PLAYER_AUTOMATED/tour1080-video/actual-player-phase53-motion.mp4"></video>',
        '<h2>Labelled comparisons</h2><p>These are crops/resizes of actual Player PNGs, not continuous footage. Full originals are linked below. Scripted stage/position setup is labelled.</p>']
for path in sorted((STAGE/'LABELLED_COMPARISON_COMPOSITES').glob('*.jpg')):
    rel=path.relative_to(STAGE).as_posix();viewer.append('<figure><figcaption>'+html.escape(path.stem)+'</figcaption><a href="'+rel+'"><img loading="lazy" src="'+rel+'"></a></figure>')
viewer.append('<h2>Unmodified real Player PNGs</h2><ul>')
for path in sorted((STAGE/'REAL_WINDOWS_PLAYER_AUTOMATED').rglob('*.png')):
    rel=path.relative_to(STAGE).as_posix();viewer.append('<li><a href="'+rel+'">'+html.escape(rel)+'</a></li>')
viewer.append('</ul><p>Source art is separately labelled SOURCE_ART_NOT_GAME_FOOTAGE. Extract every review part into the same folder for all links.</p>')
(STAGE/'index.html').write_text('\n'.join(viewer),encoding='utf-8')
manifest = []
for path in sorted(STAGE.rglob('*')):
    if path.is_file(): manifest.append(dict(path=path.relative_to(STAGE).as_posix(), bytes=path.stat().st_size, sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
(STAGE / 'FILES-SHA256.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
archive = OUT / ('Echoes-Phase53-Review-' + STAMP + '.zip')
with zipfile.ZipFile(archive, 'x', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for path in sorted(STAGE.rglob('*')):
        if path.is_file(): z.write(path, path.relative_to(STAGE).as_posix())
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    for item in manifest: assert hashlib.sha256(z.read(item['path'])).hexdigest() == item['sha256']
summary = dict(path=str(archive), bytes=archive.stat().st_size, sha256=hashlib.sha256(archive.read_bytes()).hexdigest(), files=len(manifest)+1, crcAndEveryMemberSha256='PASS', buildDirectory=str(BUILD), rawFramesIncluded=False)
# Ordinary independent ZIPs for review services with a 256 MiB upload limit. No split-volume reader is required.
limit = 190 * 1048576
groups, group, total = [], [], 0
paths = [p for p in sorted(STAGE.rglob('*')) if p.is_file() and p.name not in ['READ_FIRST.txt', 'FILES-SHA256.json']]
for path in paths:
    assert path.stat().st_size < limit, path
    if group and total + path.stat().st_size > limit:
        groups.append(group);group=[];total=0
    group.append(path);total+=path.stat().st_size
if group:groups.append(group)
parts=[]
for i, group in enumerate(groups, 1):
    part=archive.with_name(archive.stem+'-part%dof%d.zip'%(i,len(groups)))
    members=group+[STAGE/'READ_FIRST.txt',STAGE/'FILES-SHA256.json']
    with zipfile.ZipFile(part,'x',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
        for path in members:z.write(path,path.relative_to(STAGE).as_posix())
        z.writestr('PART-%d-OF-%d.txt'%(i,len(groups)), 'Independent ZIP. Extract all parts into one folder for the complete evidence set. FILES-SHA256.json covers the original files.\n')
    assert part.stat().st_size < 200*1048576
    with zipfile.ZipFile(part) as z:
        assert z.testzip() is None
        for path in members:assert hashlib.sha256(z.read(path.relative_to(STAGE).as_posix())).digest()==hashlib.sha256(path.read_bytes()).digest()
    parts.append(dict(path=str(part),bytes=part.stat().st_size,sha256=hashlib.sha256(part.read_bytes()).hexdigest(),verified='CRC and every member SHA256 PASS'))
summary['independentParts']=parts
archive.with_suffix('.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
print(json.dumps(summary, indent=2))
