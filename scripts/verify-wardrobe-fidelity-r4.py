"""Compare converted data against the fixed original input graphs, not a screenshot mock."""
from pathlib import Path
import json
R=Path(__file__).resolve().parent.parent
checks=0
def check(value,message):
    global checks
    assert value,message
    checks+=1
for source,target,count in [('ron_huolongjuan','fire-effect',9),('effect_hupai_fire','flame-win-effect',14),('effect_hupai_default','default-win-effect',7),('effect_liqi_ice','riichi-ice-effect',8),('effect_liqi_fire','riichi-fire-effect',9)]:
    raw=json.loads((R/'build/wardrobe-r4-study/source/scene'/(source+'.lh')).read_text('utf8'))
    actual=json.loads((R/'unity/Assets/Resources/Majsoul/Actions'/(target+'.json')).read_text('utf8'))
    nodes=[];particles=[]
    def visit(n,parent=-1):
        i=len(nodes);nodes.append((n,parent))
        if n['type']=='ShuriKenParticle3D':particles.append((n,i))
        for child in n.get('child',[]):visit(child,i)
    visit(raw)
    check(len(actual['nodes'])==len(nodes),source+' hierarchy retained');check(len(actual['layers'])==len(particles)==count,source+' complete layer count')
    for (n,parent),node in zip(nodes,actual['nodes']):
        c=n.get('customProps',{});p=c.get('translate',[0,0,0]);q=c.get('rotation',[0,0,0,1])
        check(node['parent']==parent and node['position']==[p[0],p[1],-p[2]] and node['rotation']==[-q[0],-q[1],q[2],q[3]] and node['scale']==c.get('scale',[1,1,1]),source+' source node pose')
    for (n,index),layer in zip(particles,actual['layers']):
        c=n['customProps'];check(layer['node']==index and layer['renderMode']==c['renderMode'] and layer['scaleMode']==c.get('scaleMode',0),source+' renderer/scaling semantics')
        if c.get('threeDStartSize') and c['startSizeType']==0:check(layer['size3d']==c['startSizeConstantSeparate'],source+' per-axis size')
        size=c.get('sizeOverLifetime',{}).get('size',{})
        if size.get('separateAxes'):
            for axis in 'XYZ':check(layer['size'+axis]==[dict(time=k['key'],value=k['value']) for k in size['gradient'+axis]['sizes']],source+' per-axis lifetime curve')
        sheet=c.get('textureSheetAnimation',{})
        if sheet.get('enable') and sheet['frame']['type']==1:
            total=sheet['tiles'][0]*sheet['tiles'][1];expected=[dict(time=k['key'],value=k['value']/total) for k in sheet['frame']['overTime']['frames']]
            check(layer['frames']==expected and all(0<=k['value']<=1 for k in layer['frames']),source+' normalized texture-sheet timeline')
    print('PASS',source,count,'source layers')
catalog=json.loads((R/'unity/Assets/Resources/Majsoul/Wardrobe/catalog.json').read_text('utf8'))['items']
check(next(i for i in catalog if i['category']=='stick')['id']=='stick-default','Plain stick is the default')
check(next(i for i in catalog if i['id']=='riichi-ice')['groundEffect'],'Ice uses source shu ground branch')
print('PASS',checks,'source-data fidelity assertions')
