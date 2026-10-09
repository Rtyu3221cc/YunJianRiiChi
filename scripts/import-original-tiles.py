"""Import source tile geometry/UVs and separate hand/table atlases from the supplied APK."""
import sys,json,zipfile,warnings,pathlib,hashlib
sys.path.insert(0,'tools/asset-inspector');import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
out=pathlib.Path('unity/Assets/Resources/Majsoul/OriginalTile');out.mkdir(parents=True,exist_ok=True)
rows=[json.loads(s) for s in open('build/reference/fast-catalog.jsonl',encoding='utf8')]
mesh_bundle='assets/Android/9ux7zauuw_@zz1dbm8@hb99$ngvlcce8_f9ac547b89183e960982.majset'
records=[]
with zipfile.ZipFile('dist/Majsoul_4.0.16_MC.apk') as z:
 env=UnityPy.load(z.read(mesh_bundle));obj=next(o for o in env.objects if o.path_id==-6171723574934221196);mesh=obj.read();h=MeshHandler(mesh);h.process();tri=h.get_triangles()
 data={'vertices':[v for p in h.m_Vertices for v in p], 'normals':[v for p in h.m_Normals for v in p[:3]],'uv':[v for p in h.m_UV0 for v in p[:2]],'body':[v for t in tri[0] for v in t],'face':[v for t in tri[1] for v in t]}
 (out/'mesh.json').write_text(json.dumps(data),encoding='utf8')
 records.append({'file':'mesh.json','bundle':mesh_bundle,'pathID':obj.path_id,'name':mesh.m_Name,'bounds':[float(mesh.m_LocalAABB.m_Extent.x)*2,float(mesh.m_LocalAABB.m_Extent.y)*2,float(mesh.m_LocalAABB.m_Extent.z)*2]})
 for i,t in enumerate(tri):
  ids=set(v for f in t for v in f);print('submesh',i,'vertices',len(ids),'xyz',[(min(h.m_Vertices[j][k] for j in ids),max(h.m_Vertices[j][k] for j in ids)) for k in range(3)],'uv',[(min(h.m_UV0[j][k] for j in ids),max(h.m_UV0[j][k] for j in ids)) for k in range(2)])
 for name,path in [('hand-face','myassets/deco/mjpface/mjpface_default/3d/texture/hand.png'),('table-face','myassets/deco/mjpface/mjpface_default/3d/texture/mjp.png'),('hand-back','myassets/deco/mjpai/mjp_rosered/3d/texture/hand.png'),('table-back','myassets/deco/mjpai/mjp_rosered/3d/texture/mjp.png')]:
  r=next(r for r in rows if path in r['paths']);env=UnityPy.load(z.read(r['bundle']));d=env.container[path].read();p=out/(name+'.png');d.image.save(p);print(name,d.m_Width,d.m_Height)
  records.append({'file':p.name,'source':path,'bundle':r['bundle'],'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(out/'provenance.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8')
