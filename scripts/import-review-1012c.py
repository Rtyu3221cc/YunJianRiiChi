"""Recover FightBegin badges and UI_CutIn textures, layout and animation from the APK."""
from pathlib import Path
import sys,json,zipfile,warnings,hashlib,io
R=Path(__file__).resolve().parent.parent;OUT=R/'unity/Assets/Resources/Majsoul/Review1012c';OUT.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
rows=[json.loads(l)for f in ['catalog.jsonl','fast-catalog.jsonl']for l in(R/'build/reference'/f).read_text(encoding='utf8').splitlines()]
cab=json.loads((R/'build/reference/tile-study/cab-map.json').read_text(encoding='utf8'));z=zipfile.ZipFile(R/'dist/Majsoul_4.0.16_MC.apk');cache={};records=[]
def af(b):
 if b not in cache:cache[b]=next(o.assets_file for o in UnityPy.load(z.read(b)).objects)
 return cache[b]
def resolve(f,p):
 if not p['m_PathID']:return None
 if p['m_FileID']:f=af(cab[Path(f.externals[p['m_FileID']-1].path).name])
 return f.objects[p['m_PathID']]
def write(n,data,o=None):
 (OUT/n).parent.mkdir(parents=True,exist_ok=True);(OUT/n).write_bytes(data);r={'output':n,'sha256':hashlib.sha256(data).hexdigest()}
 if o:r.update(bundle=cab[o.assets_file.name],pathID=o.path_id,type=o.type.name)
 records.append(r)
def js(n,d,o=None):write(n,(json.dumps(d,ensure_ascii=False,indent=2,default=str)+'\n').encode('utf8'),o)
def png(n,o):
 b=io.BytesIO();o.read().image.save(b,format='PNG');write(n+'.png',b.getvalue(),o)
def find(p):
 r=next(r for r in rows if p in r['paths']);f=af(r['bundle']);return next(o for o in f.objects.values() if o.type.name=='GameObject' and o.peek_name().lower()==Path(p).stem)
def tree(go,path=''):
 d=go.read_typetree();objs=[resolve(go.assets_file,c['component'])for c in d['m_Component']];t=next(o for o in objs if o.type.name in ['RectTransform','Transform']);td=t.read_typetree();p=path+'/'+d['m_Name'];result=[(go,t,objs,p)]
 for c in td['m_Children']:
  tr=resolve(t.assets_file,c);result+=tree(resolve(tr.assets_file,tr.read_typetree()['m_GameObject']),p)
 return result
def vec(d):return [d[k]for k in ('xyzw'if'w'in d else 'xyz'if'z'in d else 'xy')]
cut=find('myassets/ui/mj/main/prefab_output/ui_cutin.prefab');layout=[]
for go,t,objs,path in tree(cut):
 td=t.read_typetree()
 if 'm_AnchoredPosition' in td:
  layout.append({'path':path,'position':vec(td['m_AnchoredPosition']),'size':vec(td['m_SizeDelta']),'pivot':vec(td['m_Pivot']),'anchorMin':vec(td['m_AnchorMin']),'anchorMax':vec(td['m_AnchorMax']),'rotation':vec(td['m_LocalRotation']),'scale':vec(td['m_LocalScale'])})
 for o in objs:
  if o.type.name=='Animation':
   clip=resolve(o.assets_file,o.read_typetree()['m_Animation']);cd=clip.read_typetree();js('cutin-animation.json',cd,clip)
js('cutin-layout.json',{'nodes':layout},cut)
begin=find('myassets/ui/mj/main/prefab_output/ui_fightbegin.prefab');name=[]
for go,t,objs,path in tree(begin):
 if '/container_0/container_name/' not in path:continue
 td=t.read_typetree()
 if 'm_AnchoredPosition' in td:name.append({'path':path.split('/container_name/')[1],'position':vec(td['m_AnchoredPosition']),'size':vec(td['m_SizeDelta']),'pivot':vec(td['m_Pivot']),'scale':vec(td['m_LocalScale'])})
js('intro-name-layout.json',{'nodes':name},begin)
need={'cutinbg','cutin_line','rank_bg','sima_queshi','sima_1','sima_qs','star','starbg','ww_minus'};found=set()
for r in rows:
 if not any('/ui/mj/main/pic/' in p or '/ui/common/main/pic/' in p or '/ui/necessary/pic/' in p or '/deco/title/notitle/' in p for p in r['paths']):continue
 for o in af(r['bundle']).objects.values():
  if o.type.name not in ['Sprite','Texture2D']:continue
  n=o.peek_name()
  if 'sima' in n.lower():print('rank sprite',n)
  if n in need and n not in found:png(n,o);found.add(n)
assert {'cutinbg','cutin_line','rank_bg','star','starbg'}<=found,found
js('source.json',{'apk':'Majsoul_4.0.16_MC.apk','assets':records,'limitations':['Runtime UI adapters use the APK transforms/AnimationClip. The encrypted Lua is not recovered source. Existing notitle is the official CDN asset already recorded in TableLook/source.json.']})
print('Exported',sorted(found))
