"""Convert the supplied game's tile outline mesh and ground indicator art."""
import hashlib,json,sys,warnings,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(ROOT/'tools/asset-inspector'))
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
rows=[json.loads(line) for line in (ROOT/'build/reference/fast-catalog.jsonl').read_text(encoding='utf8').splitlines()]
out=ROOT/'unity/Assets/Resources/Majsoul/OriginalTile';actions=out.parent/'Actions';records=[]
with zipfile.ZipFile(ROOT/'dist/Majsoul_4.0.16_MC.apk') as archive:
    bundle='assets/Android/9ux7zauuw_@zz1dbm8@hb99$ngvlcce8_f9ac547b89183e960982.majset'
    env=UnityPy.load(archive.read(bundle));obj=next(o for o in env.objects if o.path_id==7551034896308475998)
    mesh=obj.read();handler=MeshHandler(mesh);handler.process()
    data={'vertices':[x for v in handler.m_Vertices for x in v],'triangles':[x for group in handler.get_triangles() for tri in group for x in tri], 'scale':1.047}
    material_path='myassets/necessary/material/mjp_outline.mat'
    row=next(r for r in rows if material_path in r['paths']);env=UnityPy.load(archive.read(row['bundle']));mat=env.container[material_path].read_typetree()
    color=dict(mat['m_SavedProperties']['m_Colors'])['_OutlineColor'];data['color']=[color[c] for c in 'rgba']
    (out/'outline.json').write_text(json.dumps(data,separators=(',',':')),encoding='utf8')
    records.append({'file':'outline.json','meshBundle':bundle,'meshPathID':obj.path_id,'meshName':mesh.m_Name,'material':material_path,'materialBundle':row['bundle'],'sourceScale':1.047})
    path='myassets/scenes/mj/mjtable/texture/majiangkuang.png';row=next(r for r in rows if path in r['paths'])
    env=UnityPy.load(archive.read(row['bundle']));env.container[path].read().image.save(actions/'response-glow.png')
    records.append({'file':'../Actions/response-glow.png','source':path,'bundle':row['bundle'],'usage':'original ground particle texture; additive yellow tint; world-space quad'})
    for record in records:record['sha256']=hashlib.sha256((out/record['file']).read_bytes()).hexdigest()
    (out/'outline-provenance.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8')
print('Imported original outline:',len(data['vertices'])//3,'vertices; color',data['color'],'and 256px ground glow')
