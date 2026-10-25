from character_framing import placements as compile_placements
"""Import native illustration definitions; compile native scene placement math."""
from pathlib import Path
import runpy,json,hashlib,sys
R=Path(__file__).resolve().parent.parent;S=runpy.run_path(str(R/'scripts/study-characters-1018.py'));O=R/'build/character-review-r2';D=R/'unity/Assets/Resources/Majsoul/Characters'
catalog=json.loads((D/'catalog.json').read_text('utf8'));cfg=json.loads((R/'build/characters-1018/config.json').read_text('utf8'));skins={x['id']:x for x in cfg['item_definition/skin']};wanted={skins[s['id']]['illust_data']for c in catalog['roles']for s in c['skins']};wanted.discard('');wanted.add('default');found={};records=[]
def collect(f):
 for o in f.objects.values():
  if o.type.name!='TextAsset' or o.peek_name()not in wanted:continue
  key=o.peek_name();b=S['raw'](o)
  try:d=json.loads(b)
  except (ValueError,UnicodeError):continue
  if key=='default'and'default'not in d or key!='default'and'w'not in d:continue
  found[key]=d;(O/(key+'.json')).write_bytes(b);records.append(dict(key=key,bundle=S['cab'][o.assets_file.name],pathID=o.path_id,sha256=hashlib.sha256(b).hexdigest()))
collect(S['af'](S['paths']['myassets/deco/illust_data/yiji.json']))
if wanted-set(found)and'--scan'in sys.argv:
 entries=sorted([i for i in S['z'].infolist()if i.filename.endswith('.majset')and i.file_size<120000],key=lambda i:i.file_size)
 print('Scanning',len(entries),'small bundles; missing',sorted(wanted-set(found)),flush=True)
 for i,entry in enumerate(entries):
  try:collect(S['af'](entry.filename))
  except (ValueError,KeyError,StopIteration):continue
  if not wanted-set(found):break
 print('Definitions in APK',len(found),'/',len(wanted),flush=True)
for key in wanted-set(found):
 p=O/('official-'+key+'.json')
 if p.exists():found[key]=json.loads(p.read_text('utf8'));records.append(dict(key=key,source='official CDN; see official-framing-downloads.json',sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
defs=found['default']['default'];yiji=skins[400101];result=[]
for role in catalog['roles']:
 for s in role['skins']:
  item=skins[s['id']];data=found.get(item['illust_data']);scene=[]
  scene=compile_placements(item,data,defs)
  result.append(dict(id=s['id'],source=item['illust_data']or'legacy crop table',placements=scene,full=[item['full_'+k]for k in ['x','y','width','height']],head=[item['smallhead_'+k]for k in ['x','y','width']],direction=item['direction'],noReverse=bool(item['no_reverse']),lockReverse=bool(item['lock_reverse'])))
(D/'framing.json').write_text(json.dumps(dict(referenceFull=[yiji['full_'+k]for k in ['x','y','width','height']],referenceHead=[yiji['smallhead_'+k]for k in ['x','y','width']],skins=result),ensure_ascii=False,indent=2),encoding='utf8')
(O/'framing-provenance.json').write_text(json.dumps(dict(records=records,missing=sorted(wanted-set(found)),formulaSource='Official v0.11.252.w code.js Tools._calculateIllust and getSkinData; APK default Unity offsets and Y-up conversion',scenes=dict(F0='waitingRoom / skin card; native force offset 110,-234,1.58,1.58',I0='fightBegin',J0='desktopInfo')),ensure_ascii=False,indent=2),encoding='utf8')
print('Imported',len(result),'skin framing records,',sum(len(s['placements'])for s in result),'scene placements',flush=True)
layoutPath=R/'unity/Assets/Resources/Majsoul/MatchIntro/layout.json';layout=json.loads(layoutPath.read_text('utf8'));nodes=json.loads((O/'ui_fightbegin.json').read_text('utf8'))
for cell in layout['cells']:
 n=next(n for n in nodes if n['path'].endswith('/container_'+str(cell['seat'])+'/illust/node/body/body'));p=n['transform']['m_AnchoredPosition'];cell['anchor']=[p['x'],p['y']]
layoutPath.write_text(json.dumps(layout,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
