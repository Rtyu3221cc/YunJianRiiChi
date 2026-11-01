"""Restore the one packed object curve; known Unity probe proves the slot index."""
from pathlib import Path
import json,math,struct,zlib
R=Path(__file__).resolve().parent.parent;B=R/'build/wardrobe-r22';plan=json.loads((B/'pack-plan.json').read_text('utf8'));move={m['from']:m for m in plan['moves']}
probe=json.loads((B/'binding-probe/decoded.json').read_text('utf8'))['m_ClipBindingConstant']['genericBindings']
assert len(probe)==1 and probe[0]['customType']==21 and probe[0]['attribute']==0 and probe[0]['typeID']==199
def actual(relative):return move.get(relative,{}).get('to',relative)
prefix='Assets/Resources/Majsoul/NativeReview/WinsR22/'
clip=json.loads((R/'unity'/actual(prefix+'Animations/ron_24summer--356082968926937844.json')).read_text('utf8'))['AnimationClip']
bindings=clip['m_ClipBindingConstant']['genericBindings'];index=0;target=None
for b in bindings:
 if b['isPPtrCurve']:assert target is None;target=(index,b)
 index+=3 if b['typeID']==4 and b['attribute']in[1,3,4]else 4 if b['typeID']==4 and b['attribute']==2 else 1
index,binding=target;assert binding['customType']==21 and binding['typeID']==199 and binding['attribute']==1
words=clip['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data'];raw=struct.pack('<'+'I'*len(words),*words);offset=0;keys=[]
while offset+8<=len(raw):
 time,count=struct.unpack_from('<fI',raw,offset);offset+=8
 for _ in range(count):
  channel,*coefficients=struct.unpack_from('<i4f',raw,offset);offset+=20
  if channel==index:keys.append((time,int(round(coefficients[3]))))
source=json.loads((R/'unity'/actual(prefix+'ron_24summer-source.json')).read_text('utf8'));nodes=source['nodes'];owner=next(i for i,n in enumerate(nodes)if n.get('animations'))
target_node=None
for i,n in enumerate(nodes):
 parts=[];j=i
 while j>owner:parts.insert(0,nodes[j]['name']);j=nodes[j]['parent']
 if j==owner and zlib.crc32('/'.join(parts).encode())==binding['path']:target_node=i
assert target_node==25 and keys[0][1]==0 and keys[1][1]==1 and abs(keys[1][0]-.2)<1e-6
mapping=clip['m_ClipBindingConstant']['pptrCurveMapping'];assets=[]
for pointer in mapping:
 extra=next(e for e in source['extraMaterials']if e['asset'].endswith('-'+str(pointer['m_PathID'])+'.mat'))
 old='Assets/Resources/Majsoul/NativeReview/'+extra['asset'];row=move[old];new='Assets/WardrobePacks/Source/'+old[len('Assets/Resources/'):]
 for project in [R/'unity',B/'stage/unity']:
  src=project/row['to'];dst=project/new;dst.parent.mkdir(parents=True,exist_ok=True)
  if src!=dst and src.exists():
   assert not dst.exists();src.rename(dst);meta=Path(str(src)+'.meta')
   if meta.exists():meta.rename(Path(str(dst)+'.meta'))
 row.update(to=new,runtime=True)
 address='Majsoul/NativeReview/'+str(Path(extra['asset']).with_suffix('')).replace('\\','/')
 assets.append(address)
 if not any(a['address']==address for a in plan['assets']):plan['assets'].append(dict(path=new,address=address,pack='deco-ron-24summer'))
motion_path=actual(prefix+'ron_24summer-motion.json');motion=json.loads((R/'unity'/motion_path).read_text('utf8'));track=next(t for t in motion['tracks']if t['owner']==owner)
count=math.ceil(track['duration']*track['fps'])+1
curve=dict(node=target_node,kind=8,material=binding['attribute'],components=1,property='',assets=assets,values=[next(value for time,value in reversed(keys)if time<=frame/track['fps']+1e-7)for frame in range(count)])
track['curves']=[c for c in track['curves']if not(c['kind']==8 and c['node']==target_node and c['material']==binding['attribute'])]+[curve]
for project in [R/'unity',B/'stage/unity']:(project/motion_path).write_text(json.dumps(motion,separators=(',',':'))+'\n','utf8')
(B/'pack-plan.json').write_text(json.dumps(plan,ensure_ascii=False,indent=1)+'\n','utf8')
layout=R/'docs/wardrobe-resource-layout-r22.json';data=json.loads(layout.read_text('utf8'));data['moves']=plan['moves'];layout.write_text(json.dumps(data,ensure_ascii=False,indent=1)+'\n','utf8')
(B/'pointer-curve-proof.json').write_text(json.dumps(dict(probeSlot=0,sourceSlot=binding['attribute'],node=target_node,keys=keys,assets=assets,frames=count),indent=1)+'\n','utf8')
print('POINTER CURVE RESTORED: secondary material slot 1, source switch at 0.2s; main material untouched')
