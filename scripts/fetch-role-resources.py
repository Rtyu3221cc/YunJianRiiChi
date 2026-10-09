"""Fetch only named resources from Majsoul's public, versioned asset index."""
import json,pathlib,urllib.request,hashlib,shutil
root=pathlib.Path('build/reference/public')
root.mkdir(parents=True,exist_ok=True)
index_file=root/'resversion0.11.252.w.json'
if not index_file.exists():
    with urllib.request.urlopen('https://game.maj-soul.com/1/resversion0.11.252.w.json',timeout=30) as response:index_file.write_bytes(response.read(32_000_000))
index=json.loads(index_file.read_text())['res']
names=[k for k in index if k.startswith('audio/sound/yiji/act_')]
names += ['scene/Assets/Resource/tablecloth/tablecloth_default/Table_Dif.jpg']
records=[]
for key in names:
    if not key.startswith(('audio/sound/yiji/','scene/Assets/Resource/tablecloth/tablecloth_default/')):raise ValueError(key)
    url='https://game.maj-soul.com/1/'+index[key]['prefix']+'/'+key
    file=root/'download'/key;file.parent.mkdir(parents=True,exist_ok=True)
    with urllib.request.urlopen(url,timeout=25) as response:data=response.read(5_000_000)
    file.write_bytes(data);records.append({'key':key,'url':url,'sha256':hashlib.sha256(data).hexdigest(),'bytes':len(data)})
    print(key,len(data),flush=True)
(root/'downloads.json').write_text(json.dumps(records,indent=2))
assets=pathlib.Path('app/src/main/assets')
keys={'kan':'act_kan','pon':'act_pon','chi':'act_chi','riichi':'act_rich','tsumo':'act_tumo','ron':'act_ron','nuki':'act_babei','double-riichi':'act_drich'}
clips=[]
for key,name in keys.items():
    source='audio/sound/yiji/'+name+'.mp3';record=next(r for r in records if r['key']==source)
    web='voice/yiji-'+key+'.mp3';shutil.copyfile(root/'download'/source,assets/web)
    clips.append({'key':key,'resource':'Majsoul/Voice/yiji-'+key,'webPath':web,'source':record['url'],'sha256':record['sha256']})
manifest={'provider':'yiji','role':'一姬','source':'Majsoul official public resources; supplement to local APK','clips':clips}
(assets/'voice-manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')
(assets/'voice-manifest.js').write_text('globalThis.MajsoulVoice='+json.dumps(manifest,ensure_ascii=False)+';\n',encoding='utf8')
from PIL import Image
Image.open(root/'download/scene/Assets/Resource/tablecloth/tablecloth_default/Table_Dif.jpg').save(assets/'table/cloth.png')
record=records[-1]
(assets/'table/cloth-source.json').write_text(json.dumps(record,indent=2))
print('Installed one-role voice manifest and official default cloth.',flush=True)
