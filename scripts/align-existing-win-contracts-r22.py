"""Apply the shared Lua destruction/old-style contract without changing R13 poses."""
from pathlib import Path
import json
R=Path(__file__).resolve().parent.parent;B=R/'build/wardrobe-r22';plan=json.loads((B/'pack-plan.json').read_text('utf8'));moves={m['from']:m['to']for m in plan['moves']};rows=json.loads((B/'source-inventory.json').read_text('utf8'))['definitions'];cat=json.loads((R/'unity/Assets/Resources/Majsoul/Wardrobe/catalog.json').read_text('utf8'));changed=[]
for item in cat['items']:
 if item['category']!='winEffect' or not item.get('nativeEffect','').startswith('EffectsR11/'):continue
 key=Path(item['nativeEffect']).name;source=next(r for r in rows if r['sourceKey']==key);assert source['view']['old_effect_mark']==1
 path='Assets/Resources/'+item['effect']+'.json';actual=moves.get(path,path)
 data=json.loads((R/'unity'/actual).read_text('utf8'));before={k:v for k,v in data.items()if k!='playback'}
 data['playback']=dict(sourceId=source['id'],sourceKey=key,newStyle=False,lifetime=6,resultDelay=0,tileQueueMode=0,queueBegin=0,queueEnd=0)
 assert before=={k:v for k,v in data.items()if k!='playback'}
 for project in [R/'unity',B/'stage/unity']:(project/actual).write_text(json.dumps(data,ensure_ascii=False,indent=1)+'\n','utf8')
 changed.append(dict(id=item['id'],sourceId=source['id'],sourceKey=key,worldEulerPreserved=data.get('nativeWorldEuler'),variantsPreserved=data.get('nativeVariants')))
(B/'existing-win-contracts.json').write_text(json.dumps(changed,indent=1)+'\n','utf8');print('ALIGNED',len(changed),'existing old-style win lifetimes; R13 rotations and variants preserved')
