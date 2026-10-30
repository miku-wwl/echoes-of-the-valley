"""Read actual runtime frame samples; recording overhead and benchmark runs remain separate."""
import json
import sys
from pathlib import Path

p=Path(sys.argv[1]);data=json.loads((p/'tour.json').read_text(encoding='utf-8-sig'))
result=dict(presentation=data['presentation'],benchmark=data['benchmark'],pass_=data['pass'],failures=data['failures'],resolution=[data['width'],data['height']],automatedInput=True,capturedFrames=len(data['capturedFrames']),routeSeconds=data['routeSeconds'],metres=data['travelledMetres'],sections={},visibility=data['visibility'])
for section in dict.fromkeys(s['section'] for s in data['samples']):
    samples=[s for s in data['samples'] if s['section']==section][5:]
    if not samples:continue
    times=sorted(s['frameMs'] for s in samples);pct=lambda k:times[min(len(times)-1,int(k*len(times)))]
    usable=lambda field:[s[field] for s in samples if s[field]>0]
    counter=lambda field:round(sum(usable(field))/len(usable(field)),2) if usable(field) else 'NOT AVAILABLE in this non-development player'
    result['sections'][section]=dict(frames=len(times),averageFps=round(1000*len(times)/sum(times),2),p95Ms=round(pct(.95),3),p99Ms=round(pct(.99),3),worstMs=round(times[-1],3),batches=counter('batches'),drawCalls=counter('drawCalls'),engineAllocatedMemoryPeakMiB=round(max(s['memoryBytes'] for s in samples)/1048576,2))
result['displayedDirections']=sorted(set(s['direction'] for s in data['samples']))
result['note']='Actual game frame samples at 60 FPS cap. Recorded-route samples include synchronous readback/JPEG overhead; only unrecorded benchmark runs are used to compare game FPS. Draw/batch counters unavailable if zero/invalid. All input automated; owner approval PENDING.'
(p/'metrics.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
