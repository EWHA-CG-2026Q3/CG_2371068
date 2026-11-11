from pathlib import Path
import subprocess, re, json, sys
ROOT=Path(__file__).resolve().parents[4]
OUT=ROOT/'Assets/Mid/Submission'
ffmpeg=next((ROOT/'Logs/MidTools').rglob('ffmpeg*.exe'))
movie=OUT/'The_Silent_Pavilion_Midterm.mp4'
probe=subprocess.run([str(ffmpeg),'-hide_banner','-i',str(movie)],capture_output=True,text=True,encoding='utf-8',errors='replace')
text=probe.stderr
(ROOT/'Logs/MidVideoMetadata.txt').write_text(text,encoding='utf-8')
duration=re.search(r'Duration: (\d+):(\d+):(\d+\.\d+)',text)
if not duration: raise RuntimeError('No valid MP4 duration')
seconds=int(duration[1])*3600+int(duration[2])*60+float(duration[3])
video=next(line for line in text.splitlines() if 'Video:' in line)
size=re.search(r'(\d{3,5})x(\d{3,5})',video)
fps=re.search(r'(\d+(?:\.\d+)?) fps',video)
assert 0<seconds<=60, seconds
md5=ROOT/'Logs/MidVideoFrames.md5'
decoded=subprocess.run([str(ffmpeg),'-v','error','-i',str(movie),'-map','0:v:0','-f','framemd5','-y',str(md5)],capture_output=True,text=True)
if decoded.returncode or decoded.stderr.strip(): raise RuntimeError(decoded.stderr)
frames=[s for s in md5.read_text().splitlines() if s and not s.startswith('#')]
hashes={line.rsplit(',',1)[-1].strip() for line in frames}
assert len(frames)>1000 and len(hashes)>100, (len(frames),len(hashes))
for moment in [2,16,31,42,49,53]:
    result=subprocess.run([str(ffmpeg),'-v','error','-ss',str(moment),'-i',str(movie),'-frames:v','1','-q:v','2','-y',str(ROOT/f'Logs/MidVideo_{moment:02d}.jpg')],capture_output=True,text=True)
    if result.returncode:raise RuntimeError(result.stderr)
data=dict(status='PASS',duration_seconds=seconds,width=int(size[1]),height=int(size[2]),fps=float(fps[1]),
          decoded_frames=len(frames),unique_frame_hashes=len(hashes),bytes=movie.stat().st_size,
          actual_unity_recording=True,decode_errors=0)
(OUT/'MEDIA_VERIFICATION.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
sys.stdout.reconfigure(encoding='utf-8');print(json.dumps(data,ensure_ascii=False,indent=2))
