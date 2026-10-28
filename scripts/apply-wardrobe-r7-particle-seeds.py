"""Restore Laya's independent emission random seed from cached source graphs."""
from pathlib import Path
import hashlib,json

R=Path(__file__).resolve().parent.parent
A=R/'unity/Assets/Resources/Majsoul/Actions'
W=A.parent/'Wardrobe'
records=[]
for target in sorted(A.glob('*.json')):
    data=json.loads(target.read_text('utf8'))
    if not isinstance(data,dict)or data.get('schema')!=2 or not data.get('layers'):continue
    name=data['nodes'][0]['name']
    candidates=[R/'build'/folder/'source/scene'/(name+'.lh')for folder in ['wardrobe-r6-study','wardrobe-r4-study']]
    source=next((p for p in candidates if p.is_file()),None)
    if source is None:continue
    graph=json.loads(source.read_text('utf8'));particles={};nodes=[]
    def visit(node):
        index=len(nodes);nodes.append(node)
        if node['type']=='ShuriKenParticle3D':
            particles[index]=node['customProps']
        for child in node.get('child',[]):visit(child)
    visit(graph)
    for layer in data['layers']:
        assert nodes[layer['node']]['props']['name']==layer['name']
        original=particles[layer['node']]
        layer.update(sourceBursts=True,sourceAutoSeed=original.get('autoRandomSeed',True),sourceSeed=original.get('randomSeed',0))
    target.write_text(json.dumps(data,ensure_ascii=False,indent=1)+'\n','utf8')
    records.append(dict(source=source.relative_to(R).as_posix(),sourceSHA256=hashlib.sha256(source.read_bytes()).hexdigest(),output=target.name,outputSHA256=hashlib.sha256(target.read_bytes()).hexdigest(),layers=len(data['layers'])))

(W/'provenance-r7-particles.json').write_text(json.dumps(dict(rule='ShurikenParticleSystem._burst uses Rand emission channel offset 592910910; positive fractional count is consumed by an integer loop',records=records),ensure_ascii=False,indent=2)+'\n','utf8')
# Existing records keep the original input hash and point to the current output.
changed={r['output']:r['outputSHA256']for r in records}
p=W/'provenance-r4.json';data=json.loads(p.read_text('utf8'))
for row in data['records']:
    if row.get('output')in changed:row['outputSha256']=changed[row['output']]
p.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n','utf8')
for p in [A.parent/'Results/source.json',A.parent/'SettlementVoice/provenance.json']:
    data=json.loads(p.read_text('utf8'))
    for row in data:
        if row.get('output','').removeprefix('Actions/')in changed:row['sha256']=changed[row['output'].removeprefix('Actions/')]
        if row.get('output')=='Results/source.json':row['sha256']=hashlib.sha256((A.parent/'Results/source.json').read_bytes()).hexdigest()
    p.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n','utf8')
print('R7 original emission seed import PASS',len(records),'graphs',sum(r['layers']for r in records),'layers')
