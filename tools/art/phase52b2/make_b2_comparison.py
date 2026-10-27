"""Builds comparison/index.html and copies the matched BEFORE / AFTER player screenshots next to it.

BEFORE sources: the failed Gate B pilot (hero stills, foliage frames at 20 s and 30 s of its recorded tour) and the RC2 baseline mode of the
new build (original terrain, trees and rigid hero, same camera and place). AFTER sources: the B2 build. Pixels are copied unmodified; the
captions live in the HTML outside the images. Every image is a real Windows-player framebuffer capture, scripted/automated.
"""
import hashlib, json, shutil, subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[3]
qa = root / 'artifacts/visual-qa/phase-5.2-b2'
old = root / 'artifacts/visual-qa/phase-5.2'
out = qa / 'comparison'
out.mkdir(parents=True, exist_ok=True)
FFMPEG = Path(r'C:\Users\weila\AppData\Local\Wand\app-12.65.1\resources\app.asar.unpacked\static\unpacked\capture\release\bin\64bit\ffmpeg.exe')

pairs = [
    ('hero-front', 'Hero, front', 'Gate B pilot (failed)', old / 'motion/after720-2/hero-front.png', 'B2', qa / 'player/after720/hero-front.png'),
    ('hero-side', 'Hero, side', 'Gate B pilot (failed)', old / 'motion/after720-2/hero-side.png', 'B2', qa / 'player/after720/hero-side.png'),
    ('hero-rear', 'Hero, rear (the view the game shows most)', 'Gate B pilot (failed)', old / 'motion/after720-2/hero-rear.png', 'B2', qa / 'player/after720/hero-rear.png'),
    ('hero-face', 'Face close-up', 'RC2 baseline (rigid hero)', qa / 'player/baseline720/hero-face.png', 'B2', qa / 'player/after720/hero-face.png'),
    ('hero-threeq', 'Hero, three-quarter', 'RC2 baseline (rigid hero)', qa / 'player/baseline720/hero-threeq.png', 'B2', qa / 'player/after720/hero-threeq.png'),
    ('route-workshop', 'Workshop approach (original dirt path)', 'RC2 baseline terrain and trees', qa / 'player/baseline720/route-workshop.png', 'B2', qa / 'player/after720/route-workshop.png'),
    ('route-garden', 'Garden path', 'RC2 baseline terrain and trees', qa / 'player/baseline720/route-garden.png', 'B2', qa / 'player/after720/route-garden.png'),
    ('route-windmill', 'Windmill approach', 'RC2 baseline terrain and trees', qa / 'player/baseline720/route-windmill.png', 'B2', qa / 'player/after720/route-windmill.png'),
    ('route-plaza-east', 'East path leaving the plaza', 'RC2 baseline terrain and trees', qa / 'player/baseline720/route-plaza-east.png', 'B2', qa / 'player/after720/route-plaza-east.png'),
]
# foliage regression: the Gate B frames where leaf cards blocked the screen, against the same moments of the new continuous tour
video = qa / 'player/after720/actual-player-automated-motion.mp4'
for second in (20, 30):
    target = out / ('foliage-b2-%ds.png' % second)
    if video.is_file() and FFMPEG.is_file() and not target.exists():
        subprocess.run([str(FFMPEG), '-v', 'error', '-ss', str(second), '-i', str(video), '-frames:v', '1', str(target)], check=True)
    pairs.append(('foliage-%ds' % second, 'Foliage at %d s of the continuous tour (camera moving round and under canopies)' % second, 'Gate B pilot (failed)', old / ('motion/after720-2/video-frame-%d.png' % second), 'B2', target))

rows, manifest = [], []
for key, title, left_label, left, right_label, right in pairs:
    if not left.is_file() or not right.is_file():
        print('MISSING pair', key, left.is_file(), right.is_file())
        continue
    a, b = out / (key + '-before.png'), out / (key + '-after.png')
    shutil.copyfile(left, a)
    if right != b:
        shutil.copyfile(right, b)
    for p, src in ((a, left), (b, right)):
        manifest.append({'file': p.name, 'source': str(src), 'sha256': hashlib.sha256(p.read_bytes()).hexdigest()})
    rows.append('<section><h2>%s</h2><div class="pair"><figure><img src="%s" loading="lazy"><figcaption>BEFORE: %s</figcaption></figure><figure><img src="%s" loading="lazy"><figcaption>AFTER: %s</figcaption></figure></div></section>' % (title, a.name, left_label, b.name, right_label))
html = '''<!doctype html><html lang="en"><meta charset="utf-8"><title>Echoes Phase 5.2-B2 comparison</title>
<style>body{font:15px/1.5 system-ui,sans-serif;background:#16201f;color:#eee8d8;margin:0;padding:0 16px 40px}h1{font-size:22px}h2{font-size:16px;margin:28px 0 8px}
.pair{display:grid;grid-template-columns:repeat(auto-fit,minmax(420px,1fr));gap:12px}figure{margin:0}img{width:100%%;height:auto;display:block;border:1px solid #3c4b49}
figcaption{font-size:13px;color:#c9c2ad;padding:4px 0}.note{max-width:920px;color:#d6cfb8}</style>
<h1>Echoes of the Valley: Phase 5.2-B2 matched comparisons</h1>
<p class="note">Every image is an unmodified framebuffer capture from a Windows player build driven by <b>automated</b> input (no human played). BEFORE and AFTER use the same camera and place.
Captions are outside the pixels. These are engineering captures, not a beauty verdict; <b>GATE_B_OWNER_APPROVAL=PENDING</b>.</p>
%s
<p class="note">Hashes of every copied image: manifest.json.</p></html>''' % '\n'.join(rows)
(out / 'index.html').write_text(html, encoding='utf-8')
(out / 'manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
print('comparison pairs', len(rows), '->', out / 'index.html')
