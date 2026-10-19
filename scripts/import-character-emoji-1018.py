"""User-approved original Chinese emoji supplements, without lock overlays."""
from pathlib import Path
import json,urllib.request,concurrent.futures,re,hashlib,io
from PIL import Image
R=Path(__file__).resolve().parent.parent;O=R/'unity/Assets/Resources/Majsoul/Characters'
version=json.loads((R/'build/reference/public/resversion0.11.252.w.json').read_text('utf8'))['res']
catalog=json.loads((O/'catalog.json').read_text('utf8'));records=[];jobs=[]
for role in catalog['roles']:
 prefix='extendRes/emo/e'+str(role['id'])+'/'
 # Numeric images are displayable emoji; l_ assets are auxiliary sheets.
 paths=sorted([k for k in version if k.startswith(prefix)and re.fullmatch(r'\d+\.png',k.removeprefix(prefix))],key=lambda p:int(Path(p).stem))
 role['emojis']=['Emoji/'+role['key']+'/'+Path(k).stem for k in paths]
 for k in paths:jobs.append((k,'Emoji/'+role['key']+'/'+Path(k).name))
def get(job):
 key,relative=job;dest=O/relative;dest.parent.mkdir(exist_ok=True,parents=True);url='https://game.maj-soul.com/1/'+version[key]['prefix']+'/'+key
 data=dest.read_bytes()if dest.exists()else urllib.request.urlopen(url,timeout=40).read();source_hash=hashlib.sha256(data).hexdigest();xor=0
 if not data.startswith(b'\x89PNG'):
  xor=data[0]^0x89;data=bytes(b^xor for b in data)
 assert data.startswith(b'\x89PNG\r\n\x1a\n'),url;Image.open(io.BytesIO(data)).verify()
 if not dest.exists():dest.write_bytes(data)
 return dict(output=relative,source=url,bytes=len(data),sourceSha256=source_hash,xorByte=xor,sha256=hashlib.sha256(data).hexdigest())
with concurrent.futures.ThreadPoolExecutor(max_workers=8)as pool:records=list(pool.map(get,jobs))
(O/'catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
(O/'emoji-provenance.json').write_text(json.dumps(dict(authorization='User approved official emoji supplementation on 2026-10-18',resourceVersion='v0.11.252.w',images=records),ensure_ascii=False,indent=2),encoding='utf8')
print('Imported',len(records),'emoji images.',[(r['name'],len(r['emojis']))for r in catalog['roles']])
