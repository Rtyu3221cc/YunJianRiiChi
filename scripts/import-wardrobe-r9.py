"""Complete sakura trial and add two effects per requested category."""
from pathlib import Path
import copy,hashlib,json,re,runpy
R=Path(__file__).resolve().parent.parent
source=runpy.run_path(str(R/'scripts/import-wardrobe-r6.py'))
OUT=source['OUT'];WARDROBE=source['WARDROBE'];fetch=source['fetch'];tables=source['TABLES'];records=source['records']
catalog_file=WARDROBE/'catalog.json';catalog=json.loads(catalog_file.read_text('utf8'))
study=R/'build/wardrobe-r9-study';study.mkdir(exist_ok=True)
before=study/'sakura-before.json'
if not before.exists():before.write_bytes((OUT/'callout-sakura.json').read_bytes())

def add(entry):
    old=next((i for i in catalog['items']if i['id']==entry['id']),None)
    if old:old.update(entry)
    else:catalog['items'].append(entry)

for name,key,category in [('effect_hupai_heihuo','ron-blackfire','winEffect'),('ron_keji','ron-reactor','winEffect'),('effect_liqi_canying','riichi-afterimage','riichiEffect'),('effect_liqi_feiyu','riichi-feather','riichiEffect')]:
    output=key+'-effect';source['scope']['convert'](name,output)
    view=next(v for v in tables['item_definition/view']if v['res_name']==name);item=next(v for v in tables['item_definition/item']if v['id']==view['id'])
    graph=json.loads((OUT/(output+'.json')).read_text('utf8'))
    duration=max(l['delay']+max(l['duration']if l['rate']>0 else 0,max((b['time']for b in l['bursts']),default=0))+l['lifeMax']for l in graph['layers'])
    audio=next((a for a in tables['audio/audio']if a['id']==view['audio_id']),None);cue=''
    if audio:
        path=audio['path'];cue=Path(path).stem;path=path if path.endswith('.mp3')else path+'.mp3';(R/'unity/Assets/Resources/Majsoul/Audio'/(cue+'.mp3')).write_bytes(fetch('audio/'+path if not path.startswith('audio/')else path))
    add(dict(id=key,category=category,name=item['name_chs'],builtin=True,icon=source['image'](item['icon'],'Icons',key),effect='Majsoul/Actions/'+output,groundEffect=view['sargs'][0]=='shu',cue=cue,duration=max(2.6,duration)))

adaptations=[]
for key,base,scale,icon,title,intensity,face in [
    ('callout-sakura','sakura-win-effect',.18,'ron-sakura','鸣牌指示－樱花（试装）',.72,.06),
    ('callout-frost','riichi-ice-effect',.25,'riichi-ice','鸣牌指示－冰晶（试装）',.78,.07),
    ('callout-vortex','wind-win-effect',.18,'ron-wind','鸣牌指示－风涡（试装）',.70,.04)]:
    from effect_compatibility import require_supported
    original=json.loads((OUT/(base+'.json')).read_text('utf8'));scene=original['nodes'][0]['name'];raw_scene=next(R/'build'/folder/'source/scene'/(scene+'.lh')for folder in ['wardrobe-r6-study','wardrobe-r4-study']if(R/'build'/folder/'source/scene'/(scene+'.lh')).exists());require_supported(json.loads(raw_scene.read_text('utf8')),key);data=copy.deepcopy(original);changes=[]
    for layer in data['layers']:
        old=copy.deepcopy(layer);layer['duration']=.7
        # Retain every source layer and texture. Normalize the towering winning
        # glow for a small river indicator, while retaining visible petal size.
        if layer['renderMode']==3 and len(layer['size3d'])==3:
            layer['size3d'][1]=min(layer['size3d'][1],.45)
            if len(layer['size3dMin'])==3:layer['size3dMin'][1]=min(layer['size3dMin'][1],.45)
        if layer['renderMode']==1:layer['stretch']=min(layer['stretch'],2)
        if key=='callout-sakura'and layer['name'].startswith('yezi'):
            layer['sizeMin']*=1.75;layer['sizeMax']*=1.75
        changes.append(dict(node=layer['node'],name=layer['name'],fields={k:dict(before=old[k],after=layer[k])for k in layer if old.get(k)!=layer[k]}))
    assert len(data['layers'])==len(original['layers'])
    target=OUT/(key+'.json');target.write_text(json.dumps(data,ensure_ascii=False,indent=1)+'\n','utf8')
    add(dict(id=key,category='callout',name=title,builtin=True,icon='Majsoul/Wardrobe/Icons/'+icon,effect='Majsoul/Actions/'+key,effectScale=scale,trialStyle=True,duration=3,calloutIntensity=intensity,calloutFaceOpacity=face))
    adaptations.append(dict(id=key,source=base+'.json',sourceSHA256=hashlib.sha256((OUT/(base+'.json')).read_bytes()).hexdigest(),retainedLayers=len(data['layers']),retainedNames=[l['name']for l in data['layers']],changes=changes))

catalog_file.write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n','utf8')
(WARDROBE/'provenance-r9.json').write_text(json.dumps(dict(records=records,trialIndicators=adaptations,sakuraPreviouslyRetainedLayers=len(json.loads(before.read_text('utf8'))['layers'])),ensure_ascii=False,indent=2)+'\n','utf8')
ids={}
for item in catalog['items']:ids.setdefault(item['category'],[]).append(item['id'])
engine=R/'app/src/main/assets/engine.js';raw=engine.read_bytes();ending=b'\r\n'if raw.count(b'\r\n')>raw.count(b'\n')/2 else b'\n';code=raw.decode('utf8').replace('\r\n','\n')
code,n=re.subn(r'const WARDROBE_CATALOG=\{[^\n]+\};','const WARDROBE_CATALOG='+json.dumps(ids,separators=(',',':'))+';',code);assert n==1
engine.write_bytes(code.encode('utf8').replace(b'\n',ending));(R/'unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js').write_bytes(engine.read_bytes())
runpy.run_path(str(R/'scripts/apply-wardrobe-r7-particle-seeds.py'))
print('R9 import PASS',[(k,len(v))for k,v in ids.items()if k in ['callout','riichiEffect','winEffect']],flush=True)
