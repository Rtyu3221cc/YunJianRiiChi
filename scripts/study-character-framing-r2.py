from pathlib import Path
import json,re,runpy
R=Path(__file__).resolve().parent.parent
c=json.loads((R/'build/characters-1018/config.json').read_text('utf8'))
S=runpy.run_path(str(R/'scripts/study-characters-1018.py'))
O=R/'build/character-review-r2';O.mkdir(exist_ok=True,parents=True)
for p in S['paths']:
 if '/illust_data/' in p:
  o=S['obj'](p,'TextAsset');b=S['raw'](o);(O/Path(p).name).write_bytes(b)
f=S['af'](S['paths']['myassets/deco/illust_data/yiji.json'])
keys={s['illust_data'] for s in c['item_definition/skin'] if s['character_id']in [200001,200002,200033,200069]};keys.add('default')
for o in f.objects.values():
 if o.type.name=='TextAsset' and o.peek_name() in keys:
  b=S['raw'](o);(O/(o.peek_name()+'.json')).write_bytes(b);print('JSON',o.peek_name(),len(b))
nodes=json.loads((R/'build/characters-1018/ui_visit.json').read_text('utf8'))
for n in nodes:
 if 'btn_page0' in n['path']and '_en/'not in n['path']:
  print('LAMP',n['path'],[(v['kind'],{k:d for k,d in v['data'].items()if k in ['asset_path','anim','anims','m_SpriteState']})for v in n['components']])
exit()
for p in [R/'build/reference/public/v0.11.252.w/code.js']:
 if not p.exists():continue
 t=p.read_text('utf8');print('JS',p,len(t))
 for pattern in ['half_width','illust_data','ck_full_','waitingroom','liaoshe_offset']:
  hits=list(re.finditer(re.escape(pattern),t));print('TERM',pattern,len(hits))
  for m in hits[:5]:print(t[max(0,m.start()-200):m.end()+450])
