"""Restore the huaguang sweep-light colour animation dropped by the R39 import.

The source clips animate material._Color (rgb 0 -> 1 -> 0, alpha constant) on a
"Particles/Standard Unlit" plane. The imported shader exposes _TintColor only,
so the editor sampler never saw the channel and the plane stayed lit at rest.
This bakes the compiled streamed curves into a kind-3 _TintColor curve.
Idempotent; writes build/yakuman-alignment-r41/highlight-repair.json.
"""
from pathlib import Path
import json,struct
R=Path(__file__).resolve().parent.parent
U=R/'unity/Assets/Resources/Majsoul/NativeReview/YakumanR39';E=R/'build/feedback-1114-r39/yakuman/export'
O=R/'build/yakuman-alignment-r41';O.mkdir(parents=True,exist_ok=True)
def streamed(clip):
    words=clip['m_StreamedClip']['data'];raw=struct.pack('<%dI'%len(words),*words)
    keys=[[]for _ in range(clip['m_StreamedClip']['curveCount'])];at=0
    while at<len(raw):
        time,count=struct.unpack_from('<fi',raw,at);at+=8
        for _ in range(count):
            index,a,b,c,value=struct.unpack_from('<iffff',raw,at);at+=20;keys[index].append((time,a,b,c,value))
    return keys
def sample(keys,time):
    key=keys[0]
    for candidate in keys:
        if candidate[0]<=time+1e-6:key=candidate
    dt=time-key[0]if key[0]>-1e30 else 0
    return((key[1]*dt+key[2])*dt+key[3])*dt+key[4]
audit=[]
for source_path in sorted((E/'YakumanR39').glob('*-source.json')):
    key=source_path.name[:-len('-source.json')];motion_path=U/(key+'-motion.json')
    if not motion_path.exists():continue
    source=json.loads(source_path.read_text('utf8'));motion=None
    for index,node in enumerate(source['nodes']):
        if node['name']!='huaguang'or not node.get('animations'):continue
        data=json.loads((E/node['animations'][0]).read_text('utf8'))['AnimationClip']
        bindings=data['m_ClipBindingConstant']['genericBindings'];material=[b for b in bindings if b['customType']==22]
        # Component index is the top nibble of the attribute: r,g,b,a of one property.
        # Other scenes animate _TintColor/_MainTex_ST on a legacy shader; those imported correctly.
        if[b['attribute']for b in material]!=[1303350129+(i<<28)for i in range(4)]:continue
        assert len(bindings)==5
        clip=data['m_MuscleClip']['m_Clip'];clip=clip.get('data',clip);keys=streamed(clip);constants=clip['m_ConstantClip']['data']
        assert len(keys)==6 and len(constants)==1 and clip['m_DenseClip']['m_CurveCount']==0
        if motion is None:raw=motion_path.read_bytes();motion=json.loads(raw.decode('utf8'))
        tracks=[t for t in motion['tracks']if t['owner']==index];assert len(tracks)==1;track=tracks[0]
        if any(c['node']==index and c['kind']==3 and c['property']=='_TintColor'for c in track['curves']):
            audit.append(dict(scene=key,node=index,clip=data['m_Name'],status='already present'));continue
        position=next(c for c in track['curves']if c['node']==index and c['kind']==0);frames=len(position['values'])//3;values=[];lit=[]
        for frame in range(frames):
            time=frame/track['fps'];rgb=[min(1.0,max(0.0,sample(keys[3+i],time)))for i in range(3)]
            # The importer stores Particles/Standard _Color as _TintColor * .5.
            values+=[v*.5 for v in rgb]+[constants[0]*.5]
            if max(rgb)>0:lit.append(time)
        track['curves'].append(dict(node=index,kind=3,material=0,components=4,property='_TintColor',assets=[],values=values))
        windows=[];start=previous=None
        for time in lit:
            if previous is None or time-previous>1.5/track['fps']:
                if start is not None:windows.append([round(start,3),round(previous,3)])
                start=time
            previous=time
        if start is not None:windows.append([round(start,3),round(previous,3)])
        audit.append(dict(scene=key,node=index,clip=data['m_Name'],status='added',frames=frames,litWindows=windows,alpha=constants[0]))
        ending=b'\n'if raw.endswith(b'\n')else b'';motion_path.write_bytes(json.dumps(motion,separators=(',',':')).encode('utf8')+ending)
(O/'highlight-repair.json').write_text(json.dumps(audit,ensure_ascii=False,indent=2)+'\n','utf8')
print(json.dumps(audit,ensure_ascii=False))
