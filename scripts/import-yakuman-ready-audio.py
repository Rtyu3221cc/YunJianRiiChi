"""Recover the native yakuman-ready cue from the supplied APK."""
from pathlib import Path
import hashlib,json,sys,warnings,zipfile
R=Path(__file__).resolve().parent.parent;OUT=R/'unity/Assets/Resources/Majsoul/Audio'
sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
name='effect_yakumanready';source='myassets/ui/mj/main/audio/'+name+'.mp3'
rows=[json.loads(l)for file in ['catalog.jsonl','fast-catalog.jsonl']for l in (R/'build/reference'/file).read_text('utf8').splitlines()]
row=next(r for r in rows if source in r['paths'])
with zipfile.ZipFile(R/'dist/Majsoul_4.0.16_MC.apk')as apk:env=UnityPy.load(apk.read(row['bundle']))
obj=next(o for o in env.objects if o.type.name=='AudioClip'and o.peek_name()==name);clip=obj.read();samples=clip.samples
assert len(samples)==1
filename,data=next(iter(samples.items()));filename=name+Path(filename).suffix;(OUT/filename).write_bytes(data)
record={'resource':'Majsoul/Audio/'+name,'source':source,'apkBundle':row['bundle'],'pathID':obj.path_id,'output':filename,'sha256':hashlib.sha256(data).hexdigest(),'duration':clip.m_Length}
manifest=json.loads((OUT/'provenance.json').read_text('utf8'));manifest['clips']=[r for r in manifest['clips']if r['resource']!=record['resource']]+[record]
(OUT/'provenance.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n','utf8')
print(filename,len(data),'bytes,',clip.m_Length,'seconds')
