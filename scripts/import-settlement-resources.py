import sys,json,zipfile,warnings,hashlib
from pathlib import Path
root=Path(__file__).resolve().parent.parent;out=root/'unity/Assets/Resources/Majsoul/Settlement'
sys.path.insert(0,str(root/'tools/asset-inspector'));import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
rows=[json.loads(s)for s in (root/'build/reference/fast-catalog.jsonl').read_text(encoding='utf8').splitlines()]
out.mkdir(exist_ok=True);records=[]
with zipfile.ZipFile(root/'dist/Majsoul_4.0.16_MC.apk') as z:
 for row in rows:
  isfont=any(p.startswith('myassets/fonts/')for p in row['paths'])
  isatlas=any(any(t in p for t in ['atlas_mj_main_chs_0','atlas_mj_main_common_','atlas_common_main_common_'])for p in row['paths'])
  isflame=any(p.endswith('fx_fire_003.png')for p in row['paths'])
  if not(isfont or isatlas or isflame):continue
  env=UnityPy.load(z.read(row['bundle']))
  for o in env.objects:
   if o.type.name=='Font':
    d=o.read_typetree();print('FONT',o.path_id,d.get('m_Name'),len(d.get('m_FontData',[])))
    if o.path_id not in [4190153995318525747,-7221267878960023007]:continue
    if o.path_id==-7221267878960023007:
     filename='round-font-chs.ttf';raw=bytes(d['m_FontData']);(out/filename).write_bytes(raw)
     records.append({'file':filename,'bundle':row['bundle'],'pathID':o.path_id,'sha256':hashlib.sha256(raw).hexdigest()});continue
    filename=('score-font' if o.path_id==4190153995318525747 else 'round-font')+'.ttf'
    raw=bytes(d['m_FontData']);(out/filename).write_bytes(raw)
   elif o.type.name=='Texture2D' and o.peek_name()=='fx_fire_003':
    d=o.read();filename='flame-detail.png';d.image.save(out/filename);raw=(out/filename).read_bytes()
   elif o.type.name=='Sprite' and o.peek_name() in ['pg_he','pg_zimo','s_1','s_2','s_3','s_4','s_1_1','s_2_2','s_3_3','s_4_4','rank_down_arrow','upgrade_arrow','scorechangearrow_1','Btn_common','bg_head1']:
    d=o.read();filename=d.m_Name+'.png';d.image.save(out/filename);raw=(out/filename).read_bytes();print('SPRITE',filename,d.image.size)
   else:continue
   records.append({'file':filename,'bundle':row['bundle'],'pathID':o.path_id,'sha256':hashlib.sha256(raw).hexdigest()})
(out/'source.json').write_bytes(json.dumps({'apk':'Majsoul_4.0.16_MC.apk','description':'UI_Win pg_he/pg_zimo; UI_ScoreChange native rank sprites, button and its referenced numeric/round fonts. Extracted without redrawing.','assets':records},ensure_ascii=False,indent=2).encode('utf8')+b'\n')
