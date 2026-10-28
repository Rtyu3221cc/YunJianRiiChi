"""Derive tile mount and declaration timing from original meshes/configuration."""
from pathlib import Path
import hashlib,json,numpy as np,runpy,struct
R=Path(__file__).resolve().parent.parent;A=R/'unity/Assets/Resources/Majsoul/Actions';W=A.parent/'Wardrobe';S=R/'build/wardrobe-r10-study'
source=S/'source-tile-mesh.json'
if not source.exists():
    loader=runpy.run_path(str(R/'scripts/import-wardrobe-r6.py'));raw=loader['fetch']('scene/Assets/Resource/mjpai/maque-majiangv3_Box001.lm');reader=loader['reader'](raw);reader.header();reader.p=reader.blocks[0][0];reader.string();reader.string();reader.read('h');start,length=reader.read('II');attrs=reader.string().split(',');sizes={'POSITION':12,'NORMAL':12,'UV':8,'UV1':8,'TANGENT':16,'COLOR':16};stride=sum(sizes[a]for a in attrs);offsets={};offset=0
    for attr in attrs:offsets[attr]=offset;offset+=sizes[attr]
    def values(attr,count):return [list(struct.unpack_from('<'+str(count)+'f',raw,reader.offset+start+i*stride+offsets[attr]))for i in range(length//stride)]
    source.write_text(json.dumps(dict(positions=values('POSITION',3),uv=values('UV',2),normals=values('NORMAL',3)),indent=2),'utf8')
mesh=json.loads(source.read_text('utf8'));p=np.array(mesh['positions']);uv=np.array(mesh['uv']);n=np.array(mesh['normals']);ids=np.flatnonzero((n[:,2]>.999)&(abs(p[:,2]-p[:,2].max())<1e-6))
assert len(ids)==4
sp=p[ids].copy();sp[:,2]*=-1;su=uv[ids].copy();su[:,1]=1-su[:,1];sn=n[ids].mean(0);sn[2]*=-1
native_path=A.parent/'OriginalTile/mesh.json';native=json.loads(native_path.read_text('utf8'));np_=np.array(native['vertices']).reshape(-1,3);nu=np.array(native['uv']).reshape(-1,2);nn=np.array(native['normals']).reshape(-1,3);ni=sorted(set(native['face']));tp=np_ [ni][:,[0,2,1]].copy();tp[:,0]*=-1;tn=nn[ni].mean(0)[[0,2,1]];tn[0]*=-1
def basis(points,tex,normal):
    fit=np.linalg.lstsq(np.c_[tex,np.ones(len(tex))],points,rcond=None)[0]
    u=fit[0]/np.linalg.norm(fit[0]);v=fit[1]-u*np.dot(fit[1],u);v/=np.linalg.norm(v);front=np.cross(u,v);front*=np.sign(np.dot(front,normal));return np.column_stack([u,v,front])
sb=basis(sp,su,sn);tb=basis(tp,nu[ni],tn);mount=tb@sb.T;assert np.allclose(mount.T@mount,np.eye(3),atol=1e-6)and np.linalg.det(mount)>.9999
trace=np.trace(mount);assert trace>0;s=(trace+1)**.5*2;q=[(mount[2,1]-mount[1,2])/s,(mount[0,2]-mount[2,0])/s,(mount[1,0]-mount[0,1])/s,s/4]
frame=dict(schema=1,sourceWidth=float(np.ptp(p[:,0])),sourceHeight=float(np.ptp(p[:,1])),sourceDepth=float(np.ptp(p[:,2])),mount=q,sourceFaceNormal=sb[:,2].tolist(),sourceUTangent=sb[:,0].tolist(),sourceVTangent=sb[:,1].tolist(),targetFaceNormal=tb[:,2].tolist(),targetUTangent=tb[:,0].tolist(),targetVTangent=tb[:,1].tolist(),mountMatrix=mount.reshape(-1).tolist(),sourceMeshSHA256=hashlib.sha256(source.read_bytes()).hexdigest(),nativeMeshSHA256=hashlib.sha256(native_path.read_bytes()).hexdigest(),derivation='Match face UV tangents and geometric plane; compressed native vertex normals are used only to select the front sign.')
(A/'source-tile-frame.json').write_text(json.dumps(frame,ensure_ascii=False,indent=2)+'\n','utf8')
tables_path=R/'build/wardrobe-r6-study/tables.json';tables=json.loads(tables_path.read_text('utf8'));catalog_file=W/'catalog.json';catalog=json.loads(catalog_file.read_text('utf8'));types={'hand-human':'hand_human','hand-cat':'hand_cat_orange','hand-cat-orange':'hand_cat_orange','hand-dog':'hand_dog','hand-man':'hand_man'}
for item in catalog['items']:
    if item['id']in types:item['animationType']=types[item['id']]
rows=[]
for row in tables['animation/animation']:
    if row['type']not in set(types.values())or row['name']not in ['Dapai','LiQiPai','LiZhi','Fulu','Zimo']:continue
    points=row['keypoint'][0];release=points[1]if len(points)>1 and points[1]>0 else points[0];speed=row['speed'];rows.append(dict(type=row['type'],action=row['name'],speed=speed,lifetime=row['lifetime']/speed/1000,cue=points[0]/speed/1000,release=release/speed/1000,sourceId=row['id']))
bindings=[]
for item in catalog['items']:
    if item['category']!='riichiEffect':continue
    graph=json.loads((R/'unity/Assets/Resources'/(item['effect']+'.json')).read_text('utf8'));view=next(v for v in tables['item_definition/view']if v['res_name']==graph['nodes'][0]['name']);ground=not view['sargs'][0]or view['sargs'][0]=='shu';controller='legacy'if view.get('old_effect_mark')else'common'
    assert controller=='legacy','Unimplemented common binding must not be silently treated as a legacy effect'
    bindings.append(dict(id=item['id'],sourceId=view['id'],sourceName=view['res_name'],controller=controller,ground=ground,action='Dapai'if ground else'LiQiPai'))
catalog_file.write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n','utf8')
contract=dict(schema=1,source='Original ModelAnimationController plus declaration Dapai/LiQiPai branch',sourceTableSHA256=hashlib.sha256(tables_path.read_bytes()).hexdigest(),rows=rows,bindings=bindings)
(A/'playback-contracts.json').write_text(json.dumps(contract,ensure_ascii=False,indent=2)+'\n','utf8')
print('PLAYBACK CONTRACTS',len(rows),'actions',len(bindings),'bindings','mount',np.round(mount,5).tolist(),'sourceWidth',frame['sourceWidth'])
