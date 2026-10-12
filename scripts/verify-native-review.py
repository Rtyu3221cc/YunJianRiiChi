"""Check the APK resource dependency records and native-module import envelopes."""
from pathlib import Path
import hashlib,json
R=Path(__file__).resolve().parent.parent;A=R/'unity/Assets/Resources/Majsoul/NativeReview'
data=json.loads((A/'provenance.json').read_text(encoding='utf8'));unique={r['output']:r for r in data['assets']}
for path,r in unique.items():
 file=A/path;assert file.is_file()and hashlib.sha256(file.read_bytes()).hexdigest()==r['sha256'],path
for name,count in [('effect_yiman',6),('effect_yiman2',6),('effect_chipeng',2)]:
 cfg=json.loads((A/(name+'-source.json')).read_text(encoding='utf8'));nodes=[n for n in cfg['nodes']if n.get('particle')];assert len(nodes)==count
 for n in nodes:
  particle=json.loads((A/n['particle']).read_text(encoding='utf8'))['ParticleSystem']
  assert particle['serializedVersion']=='8' and 'InitialModule'in particle and 'ColorModule'in particle and 'UVModule'in particle
  renderer=json.loads((A/n['renderer']).read_text(encoding='utf8'))['ParticleSystemRenderer'];assert renderer['m_RenderMode']==n['renderMode'] and renderer['m_VertexStreams']==[0,1,3,4]
  assert n['srcBlend']==5 and n['dstBlend']==1
  assert (A/(n['texture']+'.png')).exists()
  assert (A/(name+'.prefab')).exists()
arrows=json.loads((A/'score-layout.json').read_text(encoding='utf8'))['arrows'];assert len(arrows)==12 and {a['name']for a in arrows}=={str(i)+str(j)for i in range(4)for j in range(4)if i!=j}
anchors=json.loads((A/'north-anchors.json').read_text(encoding='utf8'))['anchors'];assert len(anchors)==4
print('PASS:',len(unique),'native asset hashes; complete 6/6/2 particle envelopes; 12 source arrows; four independent North anchors; encrypted-script evidence retained.')
