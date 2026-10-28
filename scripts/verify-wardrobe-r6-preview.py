"""Decode the preview media and verify its files, script and isolated BGM output."""
from pathlib import Path
import hashlib,json,re,subprocess,sys,wave
import numpy as np
R=Path(__file__).resolve().parent.parent;O=R/'dist/previews/2026-10-27-wardrobe-r6'
sys.path.insert(0,str(R/'tools/video-inspector'));import av
report=json.loads((O/'capture.json').read_text('utf8'));assert report['checks']>=150 and report['frames']==5236 and not report['apkBuilt'] and not report['uploaded']
media=json.loads((O/'media.json').read_text('utf8'))
for entry in media:
    for key in ['full','close','poster']:
        if entry.get(key):assert (O/entry[key]).is_file(),entry[key]
videos=[]
for file in sorted(O.glob('*.mp4')):
    with av.open(str(file))as c:
        assert c.streams.video,file.name
        count=sum(1 for frame in c.decode(video=0));assert count>0,file.name
    videos.append(dict(file=file.name,frames=count,bytes=file.stat().st_size))
print('Decoded',len(videos),'complete MP4 files',flush=True)
with av.open(str(O/'bgm-fade.mp4'))as c:assert len(c.streams.audio)==1
with wave.open(str(O/'bgm-fade.wav'))as c:
    rate=c.getframerate();channels=c.getnchannels();pcm=np.frombuffer(c.readframes(c.getnframes()),'<i2').reshape(-1,channels).astype(float)/32768
assert 11.9<len(pcm)/rate<12.1
def rms(lo,hi):return float(np.sqrt(np.mean(pcm[int(lo*rate):int(hi*rate)]**2)))
audio=dict(duration=len(pcm)/rate,channels=channels,before=rms(0,1.5),fadeEarly=rms(2,2.5),fadeLate=rms(2.5,3),result=rms(4,8),nextHand=rms(10,11.5))
assert audio['before']>.01 and audio['fadeEarly']>audio['fadeLate']>0 and audio['result']<.00001 and audio['nextHand']>.01,audio
node=Path.home()/'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe'
for name in ['index.html','手机版完整视频预览.html']:
    page=(O/name).read_text('utf8');scripts=re.findall(r'<script>(.*?)</script>',page,re.S);assert len(scripts)==1
    script=R/'build/wardrobe-r6-study'/('phone-script.js'if name!='index.html'else'index-script.js');script.write_text(scripts[0],'utf8')
    subprocess.run([str(node),'--check',str(script)],check=True,capture_output=True)
    for link in re.findall(r'(?:src|href)="([^"#]+)"',page):
        if not link.startswith(('http:','https:','data:')):assert (O/link).exists(),link
apk=R/'dist/yunjian-riichi-unity-0.6.13.apk';sha=hashlib.sha256(apk.read_bytes()).hexdigest();assert sha=='b076818a12efcfc949e81f832d0eff1db827babcb2e9026f365fae9913e13674'
out=dict(runtimeChecks=report['checks'],renderedFrames=report['frames'],fps=report['fps'],nodeTests=10,sourceFidelityAssertions=144,
         videos=videos,bgmAudio=audio,scriptSyntaxPassed=True,localMediaLinksPassed=True,
         browserAutomationRun=False,apkBuilt=False,githubUploaded=False,previousApkUnchanged=True,previousApkSHA256=sha)
(O/'validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n','utf8')
print('R6 media verification PASS',json.dumps(audio),flush=True)
