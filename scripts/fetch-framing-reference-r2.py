from pathlib import Path
import json,urllib.request,hashlib
R=Path(__file__).resolve().parent.parent;O=R/'build/character-review-r2';manifest=json.loads((R/'build/reference/public/resversion0.11.252.w.json').read_text('utf8'));res=manifest.get('res',manifest)
records=[]
for key in ['default','siqiqiansuili','siqiqiansuili_0','siqiqiansuili_xr','siqiqiansuili_yy']:
 path=next((p for p in res if p.endswith('/illust_data/'+key+'.json')and not p.startswith(('jp/','en/','kr/','chs_t/'))),'extendRes/illust_data/'+key+'.json');entry=res.get(path);print('MANIFEST',path,entry,flush=True)
 if not entry:continue
 version=entry.get('prefix')if isinstance(entry,dict)else entry
 url='https://game.maj-soul.com/1/'+version+'/'+path;b=urllib.request.urlopen(url,timeout=30).read();json.loads(b)
 target=O/('official-'+key+'.json');target.write_bytes(b);records.append(dict(key=key,url=url,sha256=hashlib.sha256(b).hexdigest(),bytes=len(b)));print('FETCHED',key,len(b),flush=True)
(O/'official-framing-downloads.json').write_text(json.dumps(records,indent=2),encoding='utf8')
