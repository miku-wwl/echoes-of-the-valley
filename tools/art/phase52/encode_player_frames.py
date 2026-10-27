"""Encode consecutive true player-framebuffer images at their captured real-time intervals.
No synthetic game imagery, interpolation, concept compositing or desktop capture.
"""
import sys,json,hashlib,subprocess
from pathlib import Path
folder=Path(sys.argv[1]).resolve();record=json.loads((folder/'tour.json').read_text(encoding='utf-8-sig'))
frames=record['capturedFrames'];assert len(frames)>=600, 'Insufficient continuous framebuffer samples'
assert record['pass'] and not record['teleportsDuringMovement']
lines=['ffconcat version 1.0'];manifest=[]
for i,frame in enumerate(frames):
    path=folder/'frames'/frame['file'];assert path.is_file()
    interval=frames[i+1]['t']-frame['t'] if i+1<len(frames) else max(.01,record['movementSeconds']-frame['t'])
    assert interval>0
    lines.extend(["file '"+frame['file']+"'",f'duration {interval:.6f}'])
    manifest.append({'file':frame['file'],'t':frame['t'],'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
lines.append("file '"+frames[-1]['file']+"'")
concat=folder/'frames/actual-timestamps.ffconcat';concat.write_bytes(('\n'.join(lines)+'\n').encode('utf-8'))
ffmpeg=Path(r'C:\Users\weila\AppData\Local\Wand\app-12.65.1\resources\app.asar.unpacked\static\unpacked\capture\release\bin\64bit\ffmpeg.exe');ffprobe=ffmpeg.with_name('ffprobe.exe')
movie=folder/'actual-player-automated-motion.mp4';assert not movie.exists()
with (folder/'ffmpeg.log').open('w') as log:
    subprocess.run([str(ffmpeg),'-hide_banner','-loglevel','warning','-f','concat','-safe','0','-i',str(concat),'-fps_mode','vfr','-an','-c:v','libx264','-preset','fast','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(movie)],stderr=log,check=True)
probe=json.loads(subprocess.check_output([str(ffprobe),'-v','error','-show_entries','format=duration:stream=width,height,avg_frame_rate,nb_frames','-of','json',str(movie)]))
duration=float(probe['format']['duration']);assert 30<=duration<=46
stream=probe['streams'][0];assert stream['width']==record['width'] and stream['height']==record['height']
subprocess.run([str(ffmpeg),'-v','error','-i',str(movie),'-f','null','-'],check=True)
(folder/'frame-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
info={'method':record['videoCapture'],'automatedInput':True,'conceptCompositing':False,'desktopCaptured':False,'audioRecorded':False,'framesCaptured':len(frames),'capturedSeconds':frames[-1]['t']-frames[0]['t'],'videoDuration':duration,'probe':probe,'fullDecode':'PASS','movieSha256':hashlib.sha256(movie.read_bytes()).hexdigest()}
(folder/'capture-provenance.json').write_text(json.dumps(info,indent=2),encoding='utf-8')
print(json.dumps(info))
