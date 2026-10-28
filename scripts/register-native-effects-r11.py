"""Register source-backed pool effects while preserving existing outfit IDs."""
from pathlib import Path
import hashlib,json,re,runpy

R=Path(__file__).resolve().parent.parent
A=R/'unity/Assets/Resources/Majsoul/Actions'
W=A.parent/'Wardrobe'
N=A.parent/'NativeReview'
manifest=json.loads((N/'EffectsR11/manifest.json').read_text('utf8'))
catalog=json.loads((W/'catalog.json').read_text('utf8'))
tables=json.loads((R/'build/wardrobe-r6-study/tables.json').read_text('utf8'))
items={i['id']:i for i in tables['item_definition/item']}
views={Path(items[v['id']]['icon']).stem:v for v in tables['item_definition/view'] if v['id']in items and items[v['id']].get('icon')}
audio={a['id']:a for a in tables['audio/audio']}
contracts=json.loads((A/'playback-contracts.json').read_text('utf8'))
fetch=runpy.run_path(str(R/'scripts/import-wardrobe-r6.py'))['fetch']
records=[]
for entry in manifest['effects']:
    key=entry['sourceKey'];view=views[key];item=items[view['id']]
    config=dict(schema=3,nativeEffect=entry['nativeEffect'],sourceVersion='4.0.51',sourceId=view['id'])
    # Preview-verified attachment frames; the tile model's mesh conversion is
    # not the prefab's runtime attachment orientation.
    if key=='ron_ko':config['nativeWorldEuler']=[0,180,0]
    if key in ['effect_liqi_23ex_maozhua','effect_liqi_23ex_xueqiu']:config['nativeTileEuler']=[0,180,0]
    if key=='effect_liqi_23ex_liandao':config['nativeWorldEuler']=[0,180,0]
    if entry.get('nativeVariants'):config['nativeVariants']=entry['nativeVariants']
    path=A/('native-'+key+'.json');path.write_text(json.dumps(config,ensure_ascii=False,indent=2)+'\n','utf8')
    cue=''
    if view['audio_id'] in audio:
        source=audio[view['audio_id']]['path'];cue=Path(source).stem
        target=A.parent/'Audio'/(cue+'.mp3')
        if not target.exists():
            source=source if source.endswith('.mp3')else source+'.mp3'
            raw=fetch(source if source.startswith('audio/')else'audio/'+source);target.write_bytes(raw)
        records.append(dict(cue=cue,sha256=hashlib.sha256(target.read_bytes()).hexdigest(),sourceTableVersion='0.11.252.w',referenceAudioCaptured=False))
    existing=next((i for i in catalog['items']if i['id']==entry['id']),None)
    if existing is None:existing=dict(id=entry['id']);catalog['items'].append(existing)
    existing.update(category=entry['category'],name=item['name_chs'],builtin=True,effect='Majsoul/Actions/'+path.stem,nativeEffect=entry['nativeEffect'],duration=5,cue=cue,groundEffect=view['sargs'][0]in['','shu'],icon=entry.get('icon',existing.get('icon','Majsoul/Wardrobe/UI/slot_effect_tip')))
    existing.pop('effectScale',None)
    if entry['category']=='riichiEffect':
        ground=view['sargs'][0]in['','shu']
        binding=dict(id=entry['id'],sourceId=view['id'],sourceName=key,controller='legacy',ground=ground,action='Dapai'if ground else'LiQiPai')
        contracts['bindings']=[b for b in contracts['bindings']if b['id']!=entry['id']]+[binding]
    records.append(dict(id=entry['id'],sourceId=view['id'],sourcePrefab=entry['prefab'],nativeEffect=entry['nativeEffect'],nativeVariants=entry.get('nativeVariants'),wrapperSHA256=hashlib.sha256(path.read_bytes()).hexdigest()))
(W/'catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n','utf8')
(A/'playback-contracts.json').write_text(json.dumps(contracts,ensure_ascii=False,indent=2)+'\n','utf8')
(W/'provenance-r11.json').write_text(json.dumps(dict(graphicsClientVersion='4.0.51',graphicsResourceVersion='0.16.289',records=records),ensure_ascii=False,indent=2)+'\n','utf8')
ids={}
for item in catalog['items']:ids.setdefault(item['category'],[]).append(item['id'])
engine=R/'app/src/main/assets/engine.js';raw=engine.read_bytes();ending=b'\r\n'if raw.count(b'\r\n')>raw.count(b'\n')/2 else b'\n'
code=raw.decode('utf8').replace('\r\n','\n');code,count=re.subn(r'const WARDROBE_CATALOG=\{[^\n]+\};','const WARDROBE_CATALOG='+json.dumps(ids,separators=(',',':'))+';',code)
assert count==1
engine.write_bytes(code.encode('utf8').replace(b'\n',ending))
(R/'unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js').write_bytes(engine.read_bytes())
print('REGISTERED',len(manifest['effects']),'native pool effects;',len(contracts['bindings']),'declaration bindings',flush=True)
