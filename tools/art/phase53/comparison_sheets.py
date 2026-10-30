"""Labelled comparison composites from unmodified real Player PNGs. No synthesized game pixels.
python -I tools/art/phase53/comparison_sheets.py
Full 1920x1080 originals remain in player-final-v2; crops/resizes are explicitly labelled here.
"""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[3]
EV=ROOT/'artifacts/visual-qa/phase-5.3'
PLAYER=EV/'player-final-v2/tour1080-video'
OUT=EV/'comparison-sheets';OUT.mkdir(exist_ok=True)
FONT=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',18)
inputs=[]

def load(name,box=None):
    path=PLAYER/(name+'.png');im=Image.open(path).convert('RGB');assert im.size==(1920,1080)
    inputs.append(dict(file=path.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),crop=box))
    return im.crop(box) if box else im

def grid(name,title,entries,columns=2,box=None,width=960):
    ims=[load(file,box) for file,label in entries];height=round(ims[0].height*width/ims[0].width)
    rows=(len(ims)+columns-1)//columns
    result=Image.new('RGB',(width*columns,(height+35)*rows+45),'#19242a');draw=ImageDraw.Draw(result);draw.text((12,12),title,font=FONT,fill='white')
    for i,(im,(_,label)) in enumerate(zip(ims,entries)):
        x=(i%columns)*width;y=45+(i//columns)*(height+35);draw.text((x+10,y+8),label,font=FONT,fill='#ffe4a8');result.paste(im.resize((width,height),Image.Resampling.LANCZOS),(x,y+35))
    result.save(OUT/(name+'.jpg'),quality=94)

keys=['N','NE','E','SE','S','SW','W','NW']
grid('hero-eight-directions','REAL PLAYER CROPS / automated movement / eight independent drawings / walk quality PARTIAL',[(f'moving-direction-{k}',k+' : crop of the full 1080p PNG') for k in keys],4,(740,320,1180,750),440)
grid('Mira-eight-dialogue-facings','REAL PLAYER CROPS / actual Mira dialogue / post-route automated setup positions',[(f'Mira-map-facing-{k}-AUTOMATED',k+' : actual NPC facing player') for k in keys],4,(620,280,1300,780),680)
for place in ['plaza','pond','spring','garden','willow']:
    grid('B5-vs-Phase53-'+place,'REAL PLAYER RESIZED COMPARISON / same build, location and camera / automated setup',[(f'comparison-B5-{place}','B5 presentation'),(f'comparison-Phase53-{place}','Phase53 presentation')])
for zoom in [14,10]:
    grid('workshop-zoom-'+str(zoom),'REAL PLAYER RESIZED / local depth-aware workshop opening / body visibility measured separately',[(f'workshop-B5-zoom-{zoom}','B5: body 0% visible'),(f'workshop-Phase53-zoom-{zoom}','Phase53: body '+('64.7%' if zoom==14 else '66.0%')+' visible; owner approval PENDING')])
grid('pond-S0-S3','REAL PLAYER RESIZED / stage and quest flags SET BY SCRIPT for visual comparison / zoom 17',[(f'pond-S{s}-SET-BY-SCRIPT-dock',f'S{s} scripted setup (not quest completion evidence)') for s in [0,3]])
grid('pond-S0-through-S3','REAL PLAYER RESIZED / S0-S3 scripted still setup / real quest progression tested separately',[(f'pond-S{s}-SET-BY-SCRIPT-approach',f'S{s} scripted setup') for s in range(4)])
(OUT/'inputs-SHA256.json').write_text(json.dumps(dict(note='Composites only; originals retained unchanged. Crop boxes use original image pixels. No colour changes or synthetic imagery.',inputs=inputs),indent=2),encoding='utf-8')
print('LABELLED_COMPOSITES='+str(len(list(OUT.glob('*.jpg')))))
