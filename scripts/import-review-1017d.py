"""Extract the native disconnect mask, academy entry and first-rank cue."""
from pathlib import Path
import hashlib,io,json,sys,warnings,zipfile
R=Path(__file__).resolve().parent.parent;O=R/'unity/Assets/Resources/Majsoul/Review1017d';O.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
rows=[json.loads(l)for f in ['catalog.jsonl','fast-catalog.jsonl']for l in(R/'build/reference'/f).read_text('utf8').splitlines()];cab=json.loads((R/'build/reference/tile-study/cab-map.json').read_text('utf8'));z=zipfile.ZipFile(R/'dist/Majsoul_4.0.16_MC.apk');cache={};records=[]
def af(bundle):
    if bundle not in cache:cache[bundle]=next(o.assets_file for o in UnityPy.load(z.read(bundle)).objects)
    return cache[bundle]
def resolve(f,p):
    if not p['m_PathID']:return None
    if p['m_FileID']:f=af(cab[Path(f.externals[p['m_FileID']-1].path).name])
    return f.objects[p['m_PathID']]
def write(name,data,obj):
    (O/name).write_bytes(data);records.append({'output':name,'bundle':cab.get(obj.assets_file.name),'pathID':obj.path_id,'sha256':hashlib.sha256(data).hexdigest()})
def sprite(stem,name,kind):
    candidates=[r for r in rows if any('atlas_mj_main_common_'in p if kind=='table'else'ui/lobby/main/'in p for p in r['paths'])]
    seen=set()
    for row in candidates:
        if row['bundle']in seen:continue
        seen.add(row['bundle'])
        for obj in af(row['bundle']).objects.values():
            if obj.type.name=='Sprite'and obj.peek_name()==stem:
                buf=io.BytesIO();obj.read().image.save(buf,format='PNG');write(name+'.png',buf.getvalue(),obj);return
    raise RuntimeError('Missing native sprite '+stem)
desktop=json.loads((R/'build/reference/tile-study/desktop-ui.json').read_text('utf8'))
layout={}
for suffix,name in [('head/node/disconnect','disconnect-mask'),('head/node/disconnect/node','disconnect-bolt')]:
    node=next(n for n in desktop if n['path'].endswith(suffix));data=[c['data']for c in node['components']];path=next(c['asset_path']for c in data if c.get('asset_path'))
    sprite(Path(path).stem,name,'table');rect=next(c for c in data if 'm_AnchoredPosition'in c);color=next(c['m_Color']for c in data if 'm_Color'in c)
    layout[name]={'position':rect['m_AnchoredPosition'],'size':rect['m_SizeDelta'],'pivot':rect['m_Pivot'],'alpha':color['a'],'source':path};print(name,path)
path='myassets/ui/lobby/main/prefab_output/ui_lobby.prefab';row=next(r for r in rows if path in r['paths']);f=af(row['bundle']);root=next(o for o in f.objects.values()if o.type.name=='GameObject'and o.peek_name().lower()=='ui_lobby')
buttons=[];transforms={}
def walk(go,parent=''):
    path=parent+'/'+go.peek_name();objs=[resolve(go.assets_file,c['component'])for c in go.read_typetree()['m_Component']];tr=next(o for o in objs if o.type.name in ['Transform','RectTransform']);td=tr.read_typetree()
    transforms[path]=td
    if '/righttop/'in path:
        for o in objs:
            if o.type.name!='MonoBehaviour':continue
            d=o.read_typetree()
            if d.get('asset_path'):
                record={'path':path,'asset':d['asset_path'],'position':td.get('m_AnchoredPosition'),'size':td.get('m_SizeDelta'),'pivot':td.get('m_Pivot')};buttons.append(record)
    for cp in td['m_Children']:
        child=resolve(tr.assets_file,cp);walk(resolve(child.assets_file,child.read_typetree()['m_GameObject']),path)
walk(root)
(R/'build/review-1017d/reference/right-buttons.json').write_text(json.dumps(buttons,ensure_ascii=False,indent=2),'utf8')
print('RIGHT ENTRIES',[(b['path'],b['asset'])for b in buttons if '/right/'in b['path']and not any(t in b['asset']for t in ['redpoint','countdown'])])
academy=next(b for b in buttons if 'btn_xinshouyindao' in b['asset'])
for n in ['btn_set','btn_help','btn_xinshouyindao','btn_scr']:
    d=transforms['/UI_Lobby/container_top/righttop/right/'+n];print('NATIVE BUTTON',n,d['m_AnchoredPosition'],d['m_SizeDelta'],d['m_Pivot'])
academy['buttonTransform']=transforms['/UI_Lobby/container_top/righttop/right/btn_xinshouyindao']
sprite(Path(academy['asset']).stem,'academy','lobby');layout['academy']=academy
name='rank1';path='myassets/ui/mj/main/audio/rank1.mp3';row=next(r for r in rows if path in r['paths']);obj=next(o for o in af(row['bundle']).objects.values()if o.type.name=='AudioClip'and o.peek_name()==name);clip=obj.read();file,data=next(iter(clip.samples.items()));name+=Path(file).suffix
(R/'unity/Assets/Resources/Majsoul/Audio'/name).write_bytes(data)
manifest=R/'unity/Assets/Resources/Majsoul/Audio/provenance.json';audio=json.loads(manifest.read_text('utf8'));audio['clips']=[c for c in audio['clips']if c['resource']!='Majsoul/Audio/rank1']+[{'resource':'Majsoul/Audio/rank1','source':path,'apkBundle':row['bundle'],'pathID':obj.path_id,'output':name,'sha256':hashlib.sha256(data).hexdigest(),'duration':clip.m_Length}];manifest.write_text(json.dumps(audio,ensure_ascii=False,indent=2)+'\n','utf8')
(O/'layout.json').write_text(json.dumps(layout,ensure_ascii=False,indent=2)+'\n','utf8');(O/'provenance.json').write_text(json.dumps({'apk':'Majsoul_4.0.16_MC.apk','assets':records},ensure_ascii=False,indent=2)+'\n','utf8')
print('First-rank sound',name,clip.m_Length)
