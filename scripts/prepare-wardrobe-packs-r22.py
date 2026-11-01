"""Generate logical roots. Unity resolves serialized dependencies before moves."""
from pathlib import Path
import collections,json,re
R=Path(__file__).resolve().parent.parent;U=R/'unity';RES=U/'Assets/Resources';B=R/'build/wardrobe-r22';B.mkdir(parents=True,exist_ok=True)
catalog=json.loads((RES/'Majsoul/Wardrobe/catalog.json').read_text('utf8'))
base={
 'stick':['stick-default','liqi_fish','liqi_scallion'],
 'cloth':['cloth-default','cloth-chigua','tablecloth_green'],
 'tileBack':['mjp_default','mjp_2308event','mjp_green'],
 'tileFace':['mjpface_default','mjpface_2308event','mjpface_25summer'],
 'headFrame':['headframe_default','headframe_catear3','headframe_douya'],
 'lobbyBackground':['background-yard','background-autumn','beijing_zhongxia'],
 'music':['music-riichi','music-8bit','music-305026'],
 'winEffect':['win-default','ron-flame','ron-fate-hf'],
 'riichiEffect':['riichi-blue','riichi-ice','riichi-fire'],
 'callout':['effect_zhishi','mpzs_shandian','mpzs_22event'],
 'hand':['hand-human','hand-cat-orange','hand-cat','hand-dog','hand-man'],
 'bgm':['bgm-306001','bgm-306003','bgm-306101'],
}
files={};typed=collections.defaultdict(list)
for p in RES.rglob('*'):
 if p.is_file() and p.suffix!='.meta':
  address=p.relative_to(RES).with_suffix('').as_posix()
  typed[address].append(p)
  if address in files:
   # Mesh/material pairs use typed serialized references, not Resources.Load.
   previous=files[address]
   if p.suffix in ['.mat','.asset'] or previous.suffix in ['.mat','.asset']:
    if previous.suffix in ['.mat','.asset'] and p.suffix not in ['.mat','.asset']:files[address]=p
    continue
   if address in ['Majsoul/Wardrobe/Motion/hand_dog-hand','Majsoul/Wardrobe/Motion/hand_man-hand']:
    if p.suffix=='.json':files[address]=p
    continue
   raise ValueError('Ambiguous Resources address '+address)
  files[address]=p
def strings(obj):
 if isinstance(obj,str):yield obj
 elif isinstance(obj,dict):
  for v in obj.values():yield from strings(v)
 elif isinstance(obj,list):
  for v in obj:yield from strings(v)
def roots(item):
 out=set();seen=set()
 def add(address):
  if not address or address in seen:return
  seen.add(address)
  if address not in files:raise ValueError('Missing logical resource '+address+' for '+item['id'])
  out.add(files[address].relative_to(U).as_posix());p=files[address]
  if item['category']=='hand':
   for t in typed[address]:out.add(t.relative_to(U).as_posix())
  if p.suffix!='.json':return
  data=json.loads(p.read_text('utf8'))
  if isinstance(data,dict) and 'vertices' in data and 'frames' in data and address+'-frames' in files:add(address+'-frames')
  for value in strings(data):
   if value.startswith('Majsoul/') and value in files:add(value)
  if isinstance(data,dict):
   for key in [data.get('nativeEffect')]+data.get('nativeVariants',[]):
    if key:add('Majsoul/NativeReview/'+key);add('Majsoul/NativeReview/'+key+'-source')
   for layer in data.get('layers',[]):
    texture=layer.get('texture')
    if texture:add('Majsoul/'+texture if '/' in texture else 'Majsoul/Actions/'+texture)
   aux=data.get('playback',{}).get('auxiliary')
   if aux:add('Majsoul/NativeReview/'+aux);add('Majsoul/NativeReview/'+aux+'-source')
   for node in data.get('nodes',[]):
    if node.get('shaderBundle'):add('Majsoul/NativeReview/'+node['shaderBundle'])
 for key,value in item.items():
  if key in ['icon','motionSet']:continue
  if isinstance(value,str) and value.startswith('Majsoul/'):
   # modelPrefab supersedes the legacy JSON geometry, keeping one model copy.
   if key=='model' and item.get('modelPrefab'):continue
   add(value)
 if item.get('nativeEffect'):
  add('Majsoul/NativeReview/'+item['nativeEffect']);add('Majsoul/NativeReview/'+item['nativeEffect']+'-source')
 if item.get('cue') and 'Majsoul/Audio/'+item['cue'] in files:add('Majsoul/Audio/'+item['cue'])
 if item.get('motionSet'):
  for suffix in ['hand','discard','riichi','meld','tsumo']:add(item['motionSet']+suffix)
 return sorted(out)
items=list(catalog['items'])
for t in json.loads((RES/'Majsoul/Settings/music.json').read_text('utf8'))['tracks']:
 items.append(dict(id='bgm-'+str(t['id']),name=t['name'],category='bgm',audio=t['resource']))
for category,ids in base.items():
 assert set(ids)<={i['id'] for i in items if i['category']==category},category
rows=[]
for item in items:
 builtin=item['id'] in base[item['category']]
 if item['id'].startswith('hand-'):builtin=True
 rows.append(dict(id=item['id'],name=item['name'],category=item['category'],hidden=item.get('hiddenFromSelection',False),builtin=builtin,roots=roots(item)))
 # Catalog flag is the physical packaging contract, not a historical import flag.
 item['builtin']=builtin
(RES/'Majsoul/Wardrobe/catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=1)+'\n','utf8')
spec=dict(schema=1,items=rows,defaults=[dict(category=c,id=ids[0]) for c,ids in base.items()],core=[p.relative_to(U).as_posix() for a,p in files.items() if p.suffix=='.shader' or '/Icons/' in a or '/UI/' in a or any(i.get('icon')==a for i in items)])
(B/'pack-roots.json').write_text(json.dumps(spec,ensure_ascii=False,indent=1)+'\n','utf8')
print('ROOTS',len(rows),'items,',sum(not x['builtin'] for x in rows),'optional; base counts',dict(collections.Counter(x['category']for x in rows if x['builtin'] and not x['hidden'])))
