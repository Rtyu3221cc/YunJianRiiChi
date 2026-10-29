"""Next-conversation inventory; discovery is kept separate from completed ports."""
from pathlib import Path
import collections,json
R=Path(__file__).resolve().parent.parent;T=json.loads((R/'build/wardrobe-r14/current-tables.json').read_text('utf8'));M=json.loads((R/'build/effects-native-manifest.json').read_text('utf8'))
C=json.loads((R/'unity/Assets/Resources/Majsoul/Wardrobe/catalog.json').read_text('utf8'));views={i['id']:i for i in T['item_definition/view']};categories={1:'winEffect',2:'riichiEffect',10:'callout'}
known={}
for item in C['items']:
    if item['category']not in categories.values():continue
    keys=set()
    if item.get('nativeEffect'):keys.add(item['nativeEffect'].split('/')[-1])
    if item.get('icon'):keys.add(item['icon'].split('/')[-1])
    if item.get('effect'):
        file=R/'unity/Assets/Resources'/(item['effect']+'.json')
        if file.exists():
            data=json.loads(file.read_text('utf8'))
            if data.get('nativeEffect'):keys.add(data['nativeEffect'].split('/')[-1])
            if data.get('nodes'):keys.add(data['nodes'][0]['name'])
    for key in keys:known.setdefault((item['category'],key),[]).append(item['id'])
definitions=[]
for item in T['item_definition/item']:
    if item['category']!=5 or item['type']not in categories:continue
    v=views.get(item['id'],{});key=v.get('res_name','');category=categories[item['type']];icon=Path(item.get('icon','')).stem
    matches=sorted(set(known.get((category,key),[])+known.get((category,icon),[])))
    definitions.append(dict(sourceId=item['id'],name=item['name_chs'],category=category,sourceKey=key,iconKey=icon,currentIds=matches,expired='已过期'in item['name_chs'],sourceView=v,implemented=bool(matches)))
old=json.loads((R/'dist/unpreviewed-effects-0.6.16.json').read_text('utf8'));pending=[dict(r,previewAvailability='not-checked',status='manifest-found-not-imported')for r in old if r['id']!=308041]
groups=collections.defaultdict(list)
for g in M['groupInfos']:
    parts=g['groupName'].split('/')
    if len(parts)>=4 and parts[0]=='deco'and parts[1]in ['effect_hupai','effect_lizhi','effect_mingpaizhishi']:groups[(parts[1],parts[2])].append(g['groupName'])
mapped={r['sourceKey']for r in definitions}|{r['iconKey']for r in definitions}
extras=[dict(category=k[0],sourceKey=k[1],groups=v,status='native-group-not-mapped-to-public-definition')for k,v in sorted(groups.items())if k[1]not in mapped]
report=dict(date='2026-10-29',publicTableVersion='0.11.252.w',nativeManifestVersion='4.0.51 / 0.16.289',definitionCounts=dict(collections.Counter(r['category']for r in definitions)),definitions=definitions,remainingFrom0616=pending,remainingFrom0616Count=len(pending),nativeOnlyCandidates=extras,notes=['Definitions include expired or internal entries and are not a current shop list.','Matches identify an implemented source binding, not original-video visual approval.','The old 93-entry queue now has 92 pending entries after Fate; missing groups remain visible.','Custom cross-category callout trials are not additional official callout definitions.'])
O=R/'docs/effects-restoration-index-r20.json';O.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf8')
print('EFFECT INDEX',len(definitions),'definitions;',len(pending),'pending from 0.6.16;',len(extras),'unmapped native groups')
