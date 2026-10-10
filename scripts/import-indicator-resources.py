"""Extract native simplified Chinese wait/furiten/draw sprites from the supplied APK."""
import hashlib,json,sys,zipfile,warnings
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'unity/Assets/Resources/Majsoul/Indicators';OUT.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(ROOT/'tools/asset-inspector'));import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
names={'pic_zhengting','show_tingpai','noting','huangpailiuju','jiuzhongjiupai','nofan','bg_chipengchoose','bg_chipengchoose2','bg_chipengchoose_title','bg_3','bg_4'};records=[]
rows=[json.loads(s)for s in (ROOT/'build/reference/fast-catalog.jsonl').read_text(encoding='utf8').splitlines()]
with zipfile.ZipFile(ROOT/'dist/Majsoul_4.0.16_MC.apk') as z:
 for row in rows:
  if any('atlas_mj_main_chs_0' in p or 'atlas_mj_main_common_' in p for p in row['paths']):
   env=UnityPy.load(z.read(row['bundle']))
   for o in env.objects:
    if o.type.name=='Sprite' and o.peek_name() in names:
     d=o.read();d.image.save(OUT/(d.m_Name+'.png'));records.append({'name':d.m_Name,'bundle':row['bundle'],'pathID':o.path_id,'border':{axis:getattr(d.m_Border,axis)for axis in 'xyzw'},'sha256':hashlib.sha256((OUT/(d.m_Name+'.png')).read_bytes()).hexdigest()})
assert names=={x['name']for x in records},names-{x['name']for x in records}
(OUT/'source.json').write_bytes((json.dumps({'source':'Majsoul_4.0.16_MC.apk','sprites':records},ensure_ascii=False,indent=2)+'\n').encode('utf8'))
print('Imported',len(records),'native wait/furiten/draw sprites.')
