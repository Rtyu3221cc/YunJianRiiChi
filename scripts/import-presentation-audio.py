"""Extract original cutin and winlose sound clips from the user-supplied APK."""
from pathlib import Path
import sys,json,zipfile,hashlib,warnings
R=Path(__file__).resolve().parent.parent;OUT=R/'unity/Assets/Resources/Majsoul/Audio'
sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
rows=[json.loads(l)for f in ['catalog.jsonl','fast-catalog.jsonl']for l in(R/'build/reference'/f).read_text(encoding='utf8').splitlines()]
manifest=json.loads((OUT/'provenance.json').read_text(encoding='utf8'))
with zipfile.ZipFile(R/'dist/Majsoul_4.0.16_MC.apk')as z:
 for name in ['cutin','winlose']:
  source=f'myassets/ui/mj/main/audio/{name}.mp3';row=next(r for r in rows if source in r['paths']);env=UnityPy.load(z.read(row['bundle']));obj=next(o for o in env.objects if o.type.name=='AudioClip'and o.peek_name()==name);clip=obj.read();samples=clip.samples;assert len(samples)==1
  filename,data=next(iter(samples.items()));filename=name+Path(filename).suffix;(OUT/filename).write_bytes(data)
  record={'resource':'Majsoul/Audio/'+name,'source':source,'apkBundle':row['bundle'],'pathID':obj.path_id,'output':filename,'sha256':hashlib.sha256(data).hexdigest(),'duration':clip.m_Length}
  manifest['clips']=[r for r in manifest['clips']if r['resource']!=record['resource']]+[record]
  print(filename,len(data),'duration',clip.m_Length,'sha256',record['sha256'])
(OUT/'provenance.json').write_bytes((json.dumps(manifest,ensure_ascii=False,indent=2)+'\n').encode('utf8'))
