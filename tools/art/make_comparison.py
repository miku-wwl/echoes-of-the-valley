"""Lossless evidence layout, never generates/repaints game imagery.
Usage: python tools/art/make_comparison.py <final-720-folder> <final-1080-folder>
Original runtime PNGs are preserved; side-by-side panels are pasted without resizing.
"""
from pathlib import Path
import sys, json, hashlib, html
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[2]
qa=root/'artifacts/visual-qa/phase-5.1'
before=qa/'before/visible'
after=Path(sys.argv[1]).resolve()
hi=Path(sys.argv[2]).resolve()
out=qa/'comparison';out.mkdir(exist_ok=True)
pairs=['A1-S0-windmill-hero','A1-S3-windmill-hero','A4-S0-hero-front','A4-S0-hero-side','A2-S0-workshop','A2-S3-workshop','A3-S0-garden','A3-S3-garden','05-inventory','06-crafting']
manifest=[];sections=[]
for name in pairs:
    a=before/(name+'.png');b=after/(name+'.png')
    with Image.open(a) as left,Image.open(b) as right:
        if left.size!=right.size:raise ValueError(f'Resolution mismatch: {name}: {left.size} vs {right.size}')
        if left.size!=(1280,720):raise ValueError(f'Expected fixed 720p view: {name}: {left.size}')
        for candidate in (left,right):
            if max(hi-lo for lo,hi in candidate.convert('RGB').getextrema())<5:raise ValueError(f'Blank screenshot: {name}')
        sheet=Image.new('RGB',(2560,756),(20,28,29));draw=ImageDraw.Draw(sheet)
        draw.text((16,12),'BEFORE | baseline dce908ec | 1280 x 720',fill='white')
        draw.text((1296,12),'AFTER | final RC2-Art | same camera / stage / resolution',fill='white')
        sheet.paste(left.convert('RGB'),(0,36));sheet.paste(right.convert('RGB'),(1280,36));sheet.save(out/(name+'-comparison.png'))
    record={'view':name,'before':str(a.relative_to(qa)),'after':str(b.relative_to(qa)),'width':1280,'height':720}
    for key,path in [('beforeSha256',a),('afterSha256',b)]:record[key]=hashlib.sha256(path.read_bytes()).hexdigest()
    manifest.append(record)
    sections.append(f'<h2>{html.escape(name)}</h2><img src="{name}-comparison.png" alt="Unaltered runtime screenshot comparison">')
for folder in [after,hi]:
    for file in sorted(folder.glob('*.png')):
        with Image.open(file) as im:manifest.append({'file':str(file.relative_to(qa)),'width':im.width,'height':im.height,'sha256':hashlib.sha256(file.read_bytes()).hexdigest()})
(out/'image-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(out/'index.html').write_text('''<!doctype html><meta charset="utf-8"><title>Echoes of the Valley - Phase 5.1 evidence</title>
<style>body{background:#182428;color:#eee;font:16px system-ui;margin:24px}img{width:100%;height:auto}h2{margin-top:36px}</style>
<h1>Actual Windows player: before / after</h1><p>Source PNG pixels are unchanged. Header labels are outside the captured viewport. Camera, stage and 720p resolution match; transient UI banners and animated poses can differ. These images are evidence of geometry/material changes, not human visual approval. The first hidden-window capture attempt was black and is excluded.</p>
'''+''.join(sections),encoding='utf-8')
print('COMPARISONS='+str(len(pairs))+' MANIFEST_RECORDS='+str(len(manifest)))
