"""Export 1012 table HUD resources directly from the supplied APK."""
from pathlib import Path
import sys,json,zipfile,warnings,hashlib
R=Path(__file__).resolve().parent.parent;O=R/'unity/Assets/Resources/Majsoul/TablePolish';O.mkdir(exist_ok=True);sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
rows=[json.loads(s)for s in(R/'build/reference/fast-catalog.jsonl').read_text(encoding='utf8').splitlines()];records=[]
with zipfile.ZipFile(R/'dist/Majsoul_4.0.16_MC.apk')as z:
 for r in rows:
  if not any('atlas_mj_main_common_'in p or 'atlas_common_main_common_'in p for p in r['paths']):continue
  for o in UnityPy.load(z.read(r['bundle'])).objects:
   if o.type.name=='Sprite'and o.peek_name()in ['hint','bg_name_1','bg_name','gold0','PT','highlight_deco_1','highlight_deco_2']:
    file=O/(o.peek_name()+'.png');o.read().image.save(file);records.append({'file':file.name,'bundle':r['bundle'],'pathID':o.path_id,'sha256':hashlib.sha256(file.read_bytes()).hexdigest()})
 assert {'hint','bg_name_1','gold0','PT'}<={Path(r['file']).stem for r in records}
 cabmap=json.loads((R/'build/reference/tile-study/cab-map.json').read_text(encoding='utf8'));cache={}
 def resolve(file,ptr):
  if ptr['m_FileID']:
   key=Path(file.externals[ptr['m_FileID']-1].path).name
   if key not in cache:cache[key]=next(o.assets_file for o in UnityPy.load(z.read(cabmap[key])).objects)
   file=cache[key]
  return file.objects[ptr['m_PathID']]
 desktop=json.loads((R/'build/reference/tile-study/desktop-ui.json').read_text(encoding='utf8'));r=next(r for r in rows if 'myassets/ui/mj/main/prefab_output/ui_desktopinfo.prefab'in r['paths']);f=next(o.assets_file for o in UnityPy.load(z.read(r['bundle'])).objects)
 text=next(c['data']for n in desktop if n['path'].endswith('container_player_0/container_name/container_name/label_name')for c in n['components']if 'm_FontData'in c['data']);font=resolve(f,text['m_FontData']['m_Font']);data=font.read_typetree();file=O/'name-font.ttf';file.write_bytes(bytes(data['m_FontData']));records.append({'file':file.name,'bundle':cabmap.get(font.assets_file.name),'pathID':font.path_id,'fontName':data['m_Name'],'sha256':hashlib.sha256(file.read_bytes()).hexdigest()})
(O/'source.json').write_text(json.dumps({'apk':'Majsoul_4.0.16_MC.apk','assets':records},ensure_ascii=False,indent=2)+'\n',encoding='utf8');print('Exported',[(r['file'],r.get('fontName',''))for r in records])
