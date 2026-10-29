"""Register exactly one source-backed Fate effect, preserving other outfits."""
from pathlib import Path
import hashlib,json,re,runpy
R=Path(__file__).resolve().parent.parent;A=R/'unity/Assets/Resources/Majsoul/Actions';W=A.parent/'Wardrobe';B=R/'build/fate-r19'
manifest=json.loads((A.parent/'NativeReview/FateR19/manifest.json').read_text('utf8'));entry=manifest['effects'][0]
assert entry['sourceKey']=='ron_25hf' and entry['particleLayers']==43
catalog=json.loads((W/'catalog.json').read_text('utf8'));preserved={i['id']:i for i in catalog['items'] if i['id']!='ron-fate-hf'}
snapshot=B/'catalog-before.json'
if not snapshot.exists():snapshot.write_bytes((W/'catalog.json').read_bytes())
config=dict(schema=3,nativeEffect='FateR19/ron_25hf',sourceId=308041,sourceVersion='4.0.51',luaBinding='DesktopMgr.setNewHuleEffect',sourceLifetimeSeconds=6,sourceResultDelaySeconds=3.3)
(A/'native-ron_25hf.json').write_text(json.dumps(config,indent=2)+'\n','utf8')
item=dict(id='ron-fate-hf',category='winEffect',name='和牌-咒层界・恶念祝祭',builtin=True,icon=entry['icon'],effect='Majsoul/Actions/native-ron_25hf',nativeEffect='FateR19/ron_25hf',duration=6,cue='ron_25hf',groundEffect=False)
catalog['items']=[i for i in catalog['items'] if i['id']!='ron-fate-hf']+[item]
assert preserved=={i['id']:i for i in catalog['items'] if i['id']!='ron-fate-hf'}
(W/'catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n','utf8')
audio=A.parent/'Audio/ron_25hf.mp3'
if not audio.exists():audio.write_bytes(runpy.run_path(str(R/'scripts/import-wardrobe-r6.py'))['fetch']('audio/audio_mj/ron_25hf.mp3'))
ids={}
for i in catalog['items']:ids.setdefault(i['category'],[]).append(i['id'])
engine=R/'app/src/main/assets/engine.js';raw=engine.read_bytes();ending=b'\r\n' if raw.count(b'\r\n')>raw.count(b'\n')/2 else b'\n'
code=raw.decode('utf8').replace('\r\n','\n');code,n=re.subn(r'const WARDROBE_CATALOG=\{[^\n]+\};','const WARDROBE_CATALOG='+json.dumps(ids,separators=(',',':'))+';',code);assert n==1
engine.write_bytes(code.encode('utf8').replace(b'\n',ending));(R/'unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js').write_bytes(engine.read_bytes())
report=dict(sourceId=308041,sourceKey='ron_25hf',name=item['name'],apkLua='4.0.16',nativeGraphics='4.0.51 / 0.16.289',audioSHA256=hashlib.sha256(audio.read_bytes()).hexdigest(),existingEntriesPreserved=len(preserved),apkBuilt=False)
(W/'provenance-r19.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf8')
print('FATE R19 REGISTERED;',len(preserved),'other entries preserved; audio',audio.stat().st_size,'bytes')
