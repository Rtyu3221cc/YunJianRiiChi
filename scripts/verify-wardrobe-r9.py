"""Check complete source graphs and the two Android/desktop registries."""
from pathlib import Path
import hashlib,json,re
R=Path(__file__).resolve().parent.parent;A=R/'unity/Assets/Resources/Majsoul/Actions';checks=0
def check(value,label):
    global checks
    assert value,label;checks+=1

for source,key,count in [('effect_hupai_heihuo','ron-blackfire',10),('ron_keji','ron-reactor',9),('effect_liqi_canying','riichi-afterimage',6),('effect_liqi_feiyu','riichi-feather',5)]:
    raw=R/'build/wardrobe-r6-study/source/scene'/(source+'.lh');graph=json.loads(raw.read_text('utf8'));data=json.loads((A/(key+'-effect.json')).read_text('utf8'));nodes=[];particles=[]
    def visit(node,parent=-1):
        i=len(nodes);nodes.append((node,parent));check(not node.get('components'),'No unported animation component: '+source)
        if node['type']=='ShuriKenParticle3D':particles.append((node,i))
        for child in node.get('child',[]):visit(child,i)
    visit(graph)
    check(len(data['nodes'])==len(nodes)and len(data['layers'])==len(particles)==count,source+' complete hierarchy and layer count')
    for (original,parent),node in zip(nodes,data['nodes']):
        c=original.get('customProps',{});p=c.get('translate',[0,0,0]);q=c.get('rotation',[0,0,0,1])
        check(node['parent']==parent and node['position']==[p[0],p[1],-p[2]]and node['rotation']==[-q[0],-q[1],q[2],q[3]]and node['scale']==c.get('scale',[1,1,1]),source+' original node pose')
    for (node,index),layer in zip(particles,data['layers']):
        c=node['customProps'];check(layer['node']==index and layer['name']==node['props']['name']and layer['renderMode']==c['renderMode'],source+' particle renderer')
        check(layer['sourceBursts']and layer['sourceSeed']==c['randomSeed']and layer['sourceAutoSeed']==c['autoRandomSeed'],source+' original emission seed')
        check((A/(layer['texture']+'.png')).is_file(),source+' resolved particle texture')
        velocity=c.get('velocityOverLifetime',{}).get('velocity',{})
        if velocity.get('type')==2:check(layer['velocityMin']==velocity['constantMin']and layer['velocityMax']==velocity['constantMax'],source+' random velocity retained')
    print('PASS',source,count,'complete layers')

for key,base,count in [('callout-sakura','sakura-win-effect',8),('callout-frost','riichi-ice-effect',8),('callout-vortex','wind-win-effect',7)]:
    trial=json.loads((A/(key+'.json')).read_text('utf8'));original=json.loads((A/(base+'.json')).read_text('utf8'))
    check(len(trial['layers'])==len(original['layers'])==count,key+' retains complete layer set')
    check(trial['nodes']==original['nodes'],key+' retains complete node hierarchy')
    check([(l['node'],l['name'],l['texture'],l['tint'],l['colorMin'],l['colorMax'])for l in trial['layers']]==[(l['node'],l['name'],l['texture'],l['tint'],l['colorMin'],l['colorMax'])for l in original['layers']],key+' preserves source textures and colors')

catalog=json.loads((A.parent/'Wardrobe/catalog.json').read_text('utf8'));expected={}
for item in catalog['items']:expected.setdefault(item['category'],[]).append(item['id'])
engine=(R/'app/src/main/assets/engine.js').read_bytes();registry=json.loads(re.search(r'const WARDROBE_CATALOG=(\{[^\r\n]+\});',engine.decode('utf8'))[1])
check(registry==expected,'Complete native selection registry')
check(engine==(R/'unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js').read_bytes(),'Android host has identical resource whitelist')
check(all(next(i for i in catalog['items']if i['id']==key)['trialStyle']for key in ['callout-sakura','callout-frost','callout-vortex']),'Adapted river styles remain explicitly marked as trials')
result=dict(checks=checks,categories={k:len(expected[k])for k in ['callout','riichiEffect','winEffect']},engineSHA256=hashlib.sha256(engine).hexdigest(),sakuraLayers=8)
(R/'build/wardrobe-r9-study/source-validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n','utf8')
print('R9 SOURCE PASS',checks,'checks')
