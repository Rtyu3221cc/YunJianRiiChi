"""User-approved original voice supplements from the official resource server."""
from pathlib import Path
import concurrent.futures,json,urllib.request,hashlib,time
R=Path(__file__).resolve().parent.parent;O=R/'unity/Assets/Resources/Majsoul/Characters/Voice'
V=json.loads((R/'build/reference/public/resversion0.11.252.w.json').read_text('utf8'))['res']
C=json.loads((R/'build/characters-1018/config.json').read_text('utf8'))
roles=['yiji','erjietang','siqiqiansuili','weilai']
jobs=[(k,v)for k,v in V.items()if any(k.startswith('audio/sound/'+r+'/')for r in roles)and k.endswith('.mp3')]
def get(job):
 k,v=job;relative=k.removeprefix('audio/sound/');p=O/relative;p.parent.mkdir(exist_ok=True,parents=True);url='https://game.maj-soul.com/1/'+v['prefix']+'/'+k
 if p.exists():data=p.read_bytes()
 else:
  for attempt in range(3):
   try:data=urllib.request.urlopen(url,timeout=40).read();break
   except Exception:
    if attempt==2:raise
    time.sleep(.4)
  assert len(data)>500 and not data.startswith(b'<'),url;p.write_bytes(data)
 return dict(output='Voice/'+relative,source=url,sha256=hashlib.sha256(data).hexdigest(),bytes=len(data))
if __name__=='__main__':
 with concurrent.futures.ThreadPoolExecutor(max_workers=8)as pool:
  records=[]
  for i,r in enumerate(pool.map(get,jobs)):
   records.append(r)
   if i%50==0:print('Voice files',i,'/',len(jobs),flush=True)
 dest=O.parent/'voice-provenance.json';dest.write_text(json.dumps(dict(authorization='User approved official voice supplementation on 2026-10-18',resourceVersion='v0.11.252.w',clips=records),ensure_ascii=False,indent=2),encoding='utf8')
 # Keep subtitles, event names and character IDs aligned with the source table.
 lines=[x for x in C['voice/sound']if x['id']in [1,2,33,69]]
 (O.parent/'voice-lines.json').write_text(json.dumps(dict(lines=lines),ensure_ascii=False),encoding='utf8')
 print('Imported',len(records),'original official voice clips,',sum(x['bytes']for x in records),'bytes.',flush=True)
