"""Inventory original APK decoration assets without altering the runtime catalog."""
import collections
import json
from pathlib import Path
import runpy

R = Path(__file__).resolve().parent.parent
S = runpy.run_path(str(R / 'scripts/study-characters-1018.py'))
tables = json.loads((R / 'build/wardrobe-r14/current-tables.json').read_text('utf8'))
print('tables', [(k, len(v)) for k, v in tables.items() if any(s in k for s in ['item', 'view', 'bgm'])])
items = [x for x in tables['item_definition/item'] if x.get('category') == 5]
for typ in [0, 3, 4, 5, 6, 7, 8, 9, 11, 12]:
    group = [x for x in items if x['type'] == typ]
    print('TYPE', typ, len(group), 'SAMPLES', json.dumps([{k:x.get(k) for k in ['id','name_chs','icon','sargs']} for x in group[:2]], ensure_ascii=False))
paths = sorted(p for p in S['paths'] if '/deco/' in p)
out = R / 'build/wardrobe-r16'
out.mkdir(parents=True, exist_ok=True)
(out / 'apk-paths.json').write_text(json.dumps(paths, ensure_ascii=False, indent=2), 'utf8')
print('APK roots', collections.Counter('/'.join(p.split('/')[:3]) for p in paths))
for category in ['lizhibang', 'tablecloth', 'mjpai', 'mjpface', 'head_frame', 'lobby_bg', 'bgm']:
    group = [p for p in paths if '/' + category + '/' in p]
    print('PATHS', category, len(group), *group[:9], sep='\n')
views = {k:v for k,v in tables.items() if 'view' in k}
(out / 'views.json').write_text(json.dumps(views, ensure_ascii=False, indent=2), 'utf8')
catalog = json.loads((R/'unity/Assets/Resources/Majsoul/Wardrobe/catalog.json').read_text('utf8'))
print('EXISTING', collections.Counter(x['category'] for x in catalog['items']))
print('VIEW SAMPLES', json.dumps({k:v[:2] for k,v in views.items()},ensure_ascii=False)[:7000])
native = json.loads((R/'build/wardrobe-r14/manifest.json').read_text('utf8'))
index = json.loads((R/'build/reference/public/resversion0.11.252.w.json').read_text('utf8'))['res']
viewmap = {v['id']:v for v in tables['item_definition/view']}
plan=[]
for x in items:
    if x['type'] not in [0,4,5,6,7,8,9,13]:
        continue
    key=viewmap.get(x['id'],{}).get('res_name','')
    pathkey='headframe_'+key if x['type']==5 else key
    apk=[p for p in S['paths'] if key and ('/'+pathkey+'/') in p]
    nat=[a for a in native['assetInfos'] if key and ('/'+pathkey+'/') in a['assetPath'].lower()]
    web=[p for p in index if key and ('/'+pathkey+'/') in p]
    plan.append(dict(id=x['id'],type=x['type'],name=x['name_chs'],key=key,apk=apk,native=nat,web=web))
(out/'plan.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),'utf8')
for typ in [0,4,5,6,7,8,9,13]:
    group=[x for x in plan if x['type']==typ]
    print('AVAILABLE',typ,len(group),'APK',sum(bool(x['apk']) for x in group),'Native',sum(bool(x['native']) for x in group),'Web',sum(bool(x['web']) for x in group))
print('NATIVE SAMPLES',json.dumps([dict(name=x['name'],assets=x['native'][:6]) for x in plan if x['type'] in [5,6,7,8,13]][:2],ensure_ascii=False))
