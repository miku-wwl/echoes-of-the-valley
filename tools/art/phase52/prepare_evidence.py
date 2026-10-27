"""Curate actual runtime evidence; labels are outside unchanged screenshot pixels."""
from pathlib import Path
import json, hashlib, shutil, re, math, xml.etree.ElementTree as ET
from PIL import Image, ImageDraw, ImageChops
root=Path(__file__).resolve().parents[3];qa=root/'artifacts/visual-qa/phase-5.2'
old=root/'artifacts/visual-qa/phase-5.1/after'
out=qa/'comparison';out.mkdir(exist_ok=True)
names=['A1-S0-windmill-hero','A1-S3-windmill-hero','A4-S0-hero-front','A4-S0-hero-side','A2-S0-workshop','A3-S0-garden']
pairs=[];html=[]
for name in names:
    for resolution in ['720','1080']:
        src=old/('validated'+resolution)/(name+'.png');dest=qa/'before'/('rc2-'+resolution)/src.name;dest.parent.mkdir(parents=True,exist_ok=True)
        if dest.exists():assert dest.read_bytes()==src.read_bytes()
        else:shutil.copy2(src,dest)
    pairs.append((name,qa/'before/rc2-720'/(name+'.png'),qa/'after720/screenshots'/(name+'.png'),'BEFORE: RC2 520a05cc'))
pairs.append(('hero-rear-S0',qa/'motion/before720/hero-rear.png',qa/'motion/after720-2/hero-rear.png','BEFORE: P52 legacy comparison mode'))
manifest=[]
for name,before,after,label in pairs:
    with Image.open(before) as a,Image.open(after) as b:
        assert a.size==b.size==(1280,720)
        sheet=Image.new('RGB',(2560,756),(20,27,29));draw=ImageDraw.Draw(sheet)
        draw.text((16,10),label+' | 1280x720',fill='white');draw.text((1296,10),'AFTER: isolated pilot b4e97ba7 | GATE B FAIL; OWNER REVIEW PENDING',fill='white')
        sheet.paste(a.convert('RGB'),(0,36));sheet.paste(b.convert('RGB'),(1280,36))
        assert ImageChops.difference(sheet.crop((0,36,1280,756)),a.convert('RGB')).getbbox() is None
        assert ImageChops.difference(sheet.crop((1280,36,2560,756)),b.convert('RGB')).getbbox() is None
        sheet.save(out/(name+'.png'))
    with Image.open(before) as a,Image.open(after) as b:
        beforePixels=hashlib.sha256(a.convert('RGB').tobytes()).hexdigest();afterPixels=hashlib.sha256(b.convert('RGB').tobytes()).hexdigest()
    manifest.append({'view':name,'before':before.relative_to(qa).as_posix(),'after':after.relative_to(qa).as_posix(),'beforeSha256':hashlib.sha256(before.read_bytes()).hexdigest(),'afterSha256':hashlib.sha256(after.read_bytes()).hexdigest(),'beforeRgbPixelSha256':beforePixels,'afterRgbPixelSha256':afterPixels})
    html.append(f'<h2>{name}</h2><img src="{name}.png" alt="Unmodified real-player screenshots with exterior labels">')
(out/'image-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(out/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Phase 5.2 pilot review</title><style>body{background:#152327;color:#eee;font:16px system-ui;margin:24px}img{width:100%;height:auto}</style><h1>Gate B FAIL — owner review pending</h1><p>All views are actual Windows Player output. Screenshot pixels are unchanged; labels are outside the viewport. Front/side/world baseline is the preserved RC2; rear baseline is original visuals in the pilot comparison mode. Camera, resolution and stage match; transient poses/banners can differ. New rig works technically, but facial/costume finish and intrusive foliage cards do not meet the strict visual gate. No world expansion is authorized.</p>'+''.join(html),encoding='utf-8')

def percentile(data,p):
    data=sorted(data);x=(len(data)-1)*p;lo=int(x);hi=min(lo+1,len(data)-1);return data[lo]+(data[hi]-data[lo])*(x-lo)
routes=[]
for name in ['after720-2','before720','after1080']:
    p=qa/'motion'/name;record=json.loads((p/'tour.json').read_text(encoding='utf-8-sig'));dt=[s['frameMs'] for s in record['samples'] if s['frameMs']>0];slowest=sorted(dt,reverse=True)[:max(1,math.ceil(len(dt)*.01))]
    routes.append({'name':name,'seconds':record['movementSeconds'],'metres':record['travelledMetres'],'waypoints':record['visitedWaypoints'],'sampleCount':len(dt),'targetFrameRate':60,'vSync':0,'captureEnabled':bool(record.get('capturedFrames')),'medianMs':percentile(dt,.5),'p95Ms':percentile(dt,.95),'p99Ms':percentile(dt,.99),'worstMs':max(dt),'onePercentLowFps':1000/(sum(slowest)/len(slowest)),'process':json.loads((p/'process-result.json').read_text(encoding='utf-8-sig'))})
tests=[]
for folder in ['baseline','verification2']:
    for platform in ['EditMode','PlayMode']:
        xml=ET.parse(qa/'logs'/folder/('tests-'+platform+'.xml')).getroot();tests.append({'set':folder,'platform':platform,'total':int(xml.attrib['total']),'passed':int(xml.attrib['passed']),'failed':int(xml.attrib['failed']),'result':xml.attrib['result']})
buildlog=(qa/'logs/build2/Build.log').read_text(encoding='utf-8-sig');match=re.search(r'PILOT_BUILD Succeeded id=([a-f0-9]+) errors=(\d+) warnings=(\d+) bytes=(\d+)',buildlog);assert match
source=json.loads((root/'game/Assets/Echoes/Art/Phase52/Source/hero-skin.json').read_text())
facts={'gateA':'PASS','gateB':'FAIL','ownerApproval':'PENDING','worldExpansionAllowed':False,'build':{'id':match[1],'errors':int(match[2]),'warnings':int(match[3]),'bytes':int(match[4]),'version':'0.5.2-pilot.1','development':False,'exeSha256':hashlib.sha256((root/'artifacts/build/phase52-pilot-b2/EchoesOfTheValley.exe').read_bytes()).hexdigest()},'tests':tests,'routes':routes,'video':json.loads((qa/'motion/after720-2/capture-provenance.json').read_text()),'skin':{'bones':len(source['bones']),'parts':len(source['parts']),'triangles':sum(len(p['triangles'])//3 for p in source['parts']),'vertices':sum(len(p['vertices'])//3 for p in source['parts']),'materialSubmeshes':len(set(p['material'] for p in source['parts'])),'clips':[c['name'] for c in source['clips']]},'comparisons':len(pairs),'smoke':[json.loads((qa/('after'+r)/'process-result.json').read_text(encoding='utf-8-sig')) for r in ['720','1080']],'rejectedVideo':'after720-1 desktop recording is INVALID, private, excluded; its game screenshots/telemetry/exit are separate observations'}
(qa/'facts.json').write_text(json.dumps(facts,indent=2),encoding='utf-8');print(json.dumps(facts))
