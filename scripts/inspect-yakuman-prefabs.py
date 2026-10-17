from pathlib import Path
import json,sys,zipfile,warnings,re
R=Path(__file__).resolve().parent.parent;OUT=R/'build/review-1017b/reference'
sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
rows=[json.loads(l)for f in ['catalog.jsonl','fast-catalog.jsonl']for l in (R/'build/reference'/f).read_text('utf8').splitlines()]
cab=json.loads((R/'build/reference/tile-study/cab-map.json').read_text('utf8'));z=zipfile.ZipFile(R/'dist/Majsoul_4.0.16_MC.apk');cache={}
def af(bundle):
    if bundle not in cache:cache[bundle]=next(o.assets_file for o in UnityPy.load(z.read(bundle)).objects)
    return cache[bundle]
def resolve(f,p):
    if not p['m_PathID']:return None
    if p['m_FileID']:f=af(cab[Path(f.externals[p['m_FileID']-1].path).name])
    return f.objects[p['m_PathID']]
for kind in ['jihui','queding']:
    name='effect_ui_yiman'+kind+'_chs';row=next(r for r in rows if any(p.endswith('/'+name+'.prefab') for p in r['paths']))
    f=af(row['bundle']);root=next(o for o in f.objects.values()if o.type.name=='GameObject'and o.peek_name().lower()==name.lower());nodes=[]
    def walk(go,path=''):
        objs=[resolve(go.assets_file,p['component'])for p in go.read_typetree()['m_Component']];t=next(o for o in objs if o.type.name in ['Transform','RectTransform']);td=t.read_typetree();path+='/'+go.peek_name()
        components=[]
        for o in objs:
            data=o.read_typetree();components.append({'type':o.type.name,'id':o.path_id,'data':data})
            if o.type.name=='Animation':
                for cp in data['m_Animations']:
                    clip=resolve(o.assets_file,cp)
                    (OUT/(name+'-'+clip.peek_name()+'.json')).write_text(json.dumps(clip.read_typetree(),indent=2,default=str),'utf8')
        nodes.append({'path':path,'components':components})
        print(path,[(o.type.name,o.peek_name())for o in objs if o.type.name not in ['Transform','RectTransform']])
        for cp in td['m_Children']:
            tr=resolve(t.assets_file,cp);walk(resolve(tr.assets_file,tr.read_typetree()['m_GameObject']),path)
    walk(root);(OUT/(name+'-raw.json')).write_text(json.dumps(nodes,indent=2,default=str),'utf8')
print('Lua matches')
for row in rows:
    for path in row.get('paths',[]):
        if path.startswith('luabyte/') and any(w in path for w in ['viewplayer_me','yiman','funcs.lua','hule','tingpai']):print(path)
code=(R/'build/reference/public/v0.11.252.w/code.js').read_text('utf8')
for term in ['yimanjihui','yimanqueding','Yiman','yiman','役满机会','役满确定']:
    matches=list(re.finditer(re.escape(term),code));print('JS',term,len(matches))
    (OUT/('js-'+term+'.txt')).write_text('\n\n'.join(code[max(0,m.start()-1800):m.end()+2200]for m in matches[:30]),'utf8')
manifest=json.loads((R/'build/review-1016b/reference/official-resversion.json').read_text('utf8'))
def scan(obj,path=''):
    if isinstance(obj,dict):
        for k,v in obj.items():
            if 'lobby_autumn' in k:print('BGM',path,k,v)
            scan(v,path+'/'+k)
scan(manifest)
