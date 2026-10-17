"""Fetch the official autumn lobby track identified by the cached official manifest."""
from pathlib import Path
import hashlib,json,sys,urllib.request
R=Path(__file__).resolve().parent.parent;O=R/'unity/Assets/Resources/Majsoul/Lobby/Audio'
key='audio/music/lobby_autumn.mp3';manifest=R/'build/review-1016b/reference/official-resversion.json'
prefix=json.loads(manifest.read_text('utf8'))['res'][key]['prefix'];url='https://game.maj-soul.com/1/'+prefix+'/'+key
file=O/'lobby_autumn.mp3'
if not file.exists():
    raw=urllib.request.urlopen(url,timeout=30).read();assert len(raw)>100000
    file.write_bytes(raw)
sys.path.insert(0,str(R/'tools/video-inspector'));import av
with av.open(str(file))as audio:
    assert len(audio.streams.audio)==1
    duration=float(audio.duration/av.time_base);assert duration>30
(O/'autumn-source.json').write_text(json.dumps({'title':'金秋送爽','resource':'Majsoul/Lobby/Audio/lobby_autumn','key':key,'url':url,'manifest':str(manifest.relative_to(R)),'bytes':file.stat().st_size,'sha256':hashlib.sha256(file.read_bytes()).hexdigest(),'duration':duration},ensure_ascii=False,indent=2)+'\n','utf8')
print('Autumn lobby track',file.stat().st_size,'bytes,',round(duration,2),'seconds')
