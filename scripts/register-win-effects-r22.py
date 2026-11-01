"""Register source-backed active win definitions and decoded Lua contracts."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import hashlib,json,re,runpy
R=Path(__file__).resolve().parent.parent;B=R/'build/wardrobe-r22';A=R/'unity/Assets/Resources/Majsoul/Actions';N=A.parent/'NativeReview/WinsR22';W=A.parent/'Wardrobe'
inventory=json.loads((B/'source-inventory.json').read_text('utf8'));manifest=json.loads((N/'manifest.json').read_text('utf8'));catalog=json.loads((W/'catalog.json').read_text('utf8'));before={i['id']:i for i in catalog['items']}
audio={i['id']:i for i in json.loads((R/'build/wardrobe-r14/current-tables.json').read_text('utf8'))['audio/audio']};fetch=runpy.run_path(str(R/'scripts/import-wardrobe-r6.py'))['fetch'];records=[]
entries={e['sourceKey']:e for e in manifest['effects']}
def sound(row):
    view=row['view'];source=audio.get(view.get('audio_id'),{}).get('path');
    if not source:return row['id'],''
    name=Path(source).stem;dest=A.parent/'Audio'/(name+'.mp3')
    if not dest.exists():dest.write_bytes(fetch(source+(''if source.endswith('.mp3')else'.mp3')))
    return row['id'],name
selected=[r for r in inventory['definitions']if r['sourceKey']in entries and not r['expired']]
with ThreadPoolExecutor(max_workers=3)as pool:cues=dict(pool.map(sound,selected))
for row in selected:
    key=row['sourceKey'];entry=entries[key];view=row['view'];args=view.get('sargs',[])
    number=lambda i:float(args[i])if len(args)>i and args[i] else 0
    plan=dict(sourceId=row['id'],sourceKey=key,newStyle=view.get('old_effect_mark')!=1,lifetime=6,resultDelay=number(0)/1000,tileQueueMode=int(number(1)),queueBegin=0,queueEnd=number(0)/1000+2 if number(1)>0 else 0)
    if key in ['ron_23wenquan','ron_21winter']:plan.update(tileQueueMode=3,queueEnd=1.8)
    if key=='ron_yanhua':plan.update(auxiliary='WinsR22/delay_boom',auxiliaryAt=.6,auxiliaryLifetime=2)
    path=A/('native-'+key+'.json');path.write_text(json.dumps(dict(schema=3,nativeEffect=entry['nativeEffect'],sourceVersion='4.0.51',sourceId=row['id'],nativeWorldEuler=[0,180,0],playback=plan),ensure_ascii=False,indent=2)+'\n','utf8')
    item=dict(id=entry['id'],category='winEffect',name=row['name'],sourceItemId=row['id'],builtin=False,icon=entry['icon'],effect='Majsoul/Actions/'+path.stem,nativeEffect=entry['nativeEffect'],duration=6,cue=cues[row['id']],groundEffect=False)
    at=next((i for i,x in enumerate(catalog['items'])if x['id']==item['id']),None)
    if at is None:catalog['items'].append(item)
    else:catalog['items'][at]=item
    records.append(dict(id=item['id'],sourceId=row['id'],sourceKey=key,playback=plan,cue=item['cue'],portedFromLua=True,originalVideoComparison=False))
for key,item in before.items():assert next(i for i in catalog['items']if i['id']==key)==item,'Existing approved outfit changed: '+key
(W/'catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n','utf8')
(W/'provenance-r22-wins.json').write_text(json.dumps(dict(records=records,definitions=len(inventory['definitions']),expiredMissing=[r for r in inventory['definitions']if not r['sourceKey']],nativeOnlyKeys=[k for k in inventory['nativeKeys']if k not in {r['sourceKey']for r in inventory['definitions']}]),ensure_ascii=False,indent=2)+'\n','utf8')
ids={}
for item in catalog['items']:ids.setdefault(item['category'],[]).append(item['id'])
p=R/'app/src/main/assets/engine.js';raw=p.read_bytes();ending=b'\r\n'if raw.count(b'\r\n')>raw.count(b'\n')/2 else b'\n';s=raw.decode().replace('\r\n','\n');s,n=re.subn(r'const WARDROBE_CATALOG=\{[^\n]+\};','const WARDROBE_CATALOG='+json.dumps(ids,separators=(',',':'))+';',s);assert n==1;p.write_bytes(s.encode().replace(b'\n',ending));(R/'unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js').write_bytes(p.read_bytes())
print('REGISTERED',len(records),'new source definitions;',len(ids['winEffect']),'selectable win effects including default; runtime review still required')
