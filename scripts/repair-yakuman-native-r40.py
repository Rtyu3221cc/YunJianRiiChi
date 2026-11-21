"""Repair retained controller states and add source L0 illustration placements."""
from pathlib import Path
import json,sys,hashlib
R=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(R/'scripts'))
import character_framing
U=R/'unity/Assets/Resources/Majsoul';B=R/'build/feedback-1114-r39/yakuman/export/YakumanR39'
audit=[]
for key in ['fan_sianke','fan_sianke18','fan_siankedanqi18']:
    path=B/(key+'-source.json');source=json.loads(path.read_text('utf8'))
    motion_path=U/'NativeReview/YakumanR39'/(key+'-motion.json');motion=json.loads(motion_path.read_text('utf8'))
    for i,n in enumerate(source['nodes']):
        if len(n.get('animations',[]))<2:continue
        all_paths=n['animations'];ids=[int(Path(p).stem[len(key)+1:])for p in all_paths]
        controller=next(json.loads(p.read_text('utf8'))for p in (B/'Animations').glob(key+'-*controller.json') if [v['m_PathID']for v in json.loads(p.read_text('utf8'))['m_AnimationClips']]==ids)
        machine=controller['m_Controller']['m_StateMachineArray'][0]['data'];state=machine['m_StateConstantArray'][machine['m_DefaultState']]['data']
        assert not state['m_TransitionConstantArray']
        nodes=state['m_BlendTreeConstantArray'][0]['data']['m_NodeArray'];clip_ids=[v['data']['m_ClipID']for v in nodes if not v['data']['m_ChildIndices']];assert len(clip_ids)==1
        chosen=clip_ids[0];tracks=[t for t in motion['tracks']if t['owner']==i];assert len(tracks)==len(all_paths)
        unused={id(t)for j,t in enumerate(tracks)if j!=chosen};motion['tracks']=[t for t in motion['tracks']if id(t)not in unused]
        name=json.loads((R/'build/feedback-1114-r39/yakuman/export'/all_paths[chosen]).read_text('utf8'))['AnimationClip']['m_Name']
        audit.append(dict(scene=key,node=n['name'],defaultClip=name,removedTracks=len(unused),source=all_paths))
        n['animations']=[all_paths[chosen]];n['defaultAnimation']=name
    path.write_text(json.dumps(source,ensure_ascii=False,indent=1)+'\n','utf8');motion_path.write_text(json.dumps(motion,separators=(',',':'))+'\n','utf8')
P=R/'build/character-review-r2';defaults=json.loads((P/'default.json').read_text('utf8'))['default']
catalog=json.loads((U/'Characters/framing.json').read_text('utf8'));config=json.loads((R/'build/characters-1018/config.json').read_text('utf8'));items={v['id']:v for v in config['item_definition/skin']}
old_scenes=character_framing.SCENES;character_framing.SCENES=('L0',);count=0;legacy=0
for skin in catalog['skins']:
    item=items[skin['id']];key=item['illust_data'];path=P/(key+'.json')
    if not path.exists():path=P/('official-'+key+'.json')
    data=json.loads(path.read_text('utf8'))if key and path.exists()else None
    placements=character_framing.placements(item,data,defaults)
    skin['placements']=[p for p in skin['placements']if p['scene']!='L0']+placements
    count+=len(placements);legacy+=not bool(placements)
character_framing.SCENES=old_scenes
(U/'Characters/framing.json').write_bytes(json.dumps(catalog,ensure_ascii=False,indent=2).encode('utf8'))
O=R/'build/yakuman-alignment-r40'
if not audit and (O/'native-repair.json').exists():audit=json.loads((O/'native-repair.json').read_text('utf8'))['controllers']
(O/'native-repair.json').write_text(json.dumps(dict(controllers=audit,L0Placements=count,legacyFallbackSkins=legacy),ensure_ascii=False,indent=2)+'\n','utf8')
print(json.dumps(dict(controllers=audit,L0Placements=count,legacyFallbackSkins=legacy),ensure_ascii=False))
