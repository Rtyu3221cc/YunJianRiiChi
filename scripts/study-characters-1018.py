"""Read the supplied APK. Keep research output separate from runtime imports."""
from pathlib import Path
import sys,json,zipfile,warnings,io,re
R=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(R/'tools/asset-inspector'))
import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
O=R/'build/characters-1018';O.mkdir(exist_ok=True,parents=True)
rows=[json.loads(l)for f in ['fast-catalog.jsonl','catalog.jsonl','role-catalog.jsonl']for l in(R/'build/reference'/f).read_text('utf8').splitlines()]
paths={p:r['bundle']for r in rows for p in r.get('paths',[])if p.startswith('myassets/')or p.startswith('luabyte/')}
z=zipfile.ZipFile(R/'dist/Majsoul_4.0.16_MC.apk')
cab=json.loads((R/'build/reference/tile-study/cab-map.json').read_text('utf8'));cache={}
def af(b):
 if b not in cache:cache[b]=next(o.assets_file for o in UnityPy.load(z.read(b)).objects)
 return cache[b]
def resolve(f,p):
 if not p or not p.get('m_PathID'):return None
 if p['m_FileID']:f=af(cab[Path(f.externals[p['m_FileID']-1].path).name])
 return f.objects[p['m_PathID']]
def obj(path,kind=None):
 f=af(paths[path]);name=Path(path).stem.lower()
 for ab in f.objects.values():
  if ab.type.name!='AssetBundle':continue
  for key,entry in ab.read_typetree()['m_Container']:
   if key!=path:continue
   o=resolve(f,entry['asset'])
   if o and(not kind or o.type.name==kind):return o
 return next(o for o in f.objects.values()if(not kind or o.type.name==kind)and(o.peek_name()or'').lower()==name)
def raw(o):
 s=o.read().m_Script
 return s.encode('utf8','surrogateescape')if isinstance(s,str)else bytes(s)
def tree(go,parent=''):
 gd=go.read_typetree();comps=[resolve(go.assets_file,c['component'])for c in gd['m_Component']];t=next(o for o in comps if o.type.name in ['RectTransform','Transform']);td=t.read_typetree();path=parent+'/'+gd['m_Name'];components=[]
 for o in comps:
  if o.type.name!='MonoBehaviour':continue
  d=o.read_typetree();s=resolve(o.assets_file,d['m_Script']);kind=s.read_typetree()['m_ClassName'];components.append(dict(kind=kind,data=d))
 yield dict(path=path,active=gd['m_IsActive'],transform=td,components=components)
 for p in td['m_Children']:
  tr=resolve(t.assets_file,p);yield from tree(resolve(tr.assets_file,tr.read_typetree()['m_GameObject']),path)
if __name__=='__main__':
 for name in ['ui_liaoshemain','ui_liaosheselect','ui_liaoshechangeskin','ui_dormsortcharacter','ui_visit']:
  p='myassets/ui/lobby/extend/liaoshe/prefab_output/'+name+'.prefab'
  d=list(tree(obj(p,'GameObject')));(O/(name+'.json')).write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf8')
  print(name,len(d),flush=True)
 p='myassets/ui/lobby/extend/roleset/prefab_output/ui_roleset.prefab';(O/'ui_roleset.json').write_text(json.dumps(list(tree(obj(p,'GameObject'))),ensure_ascii=False,indent=2),encoding='utf8')
 allpaths=sorted(paths);(O/'paths.json').write_text(json.dumps(allpaths,indent=2),encoding='utf8')
 print('audio candidates',*[p for p in allpaths if any(s in p for s in ['weilai','siqiqiansuili'])and not '/deco/'in p][:16],sep='\n')
