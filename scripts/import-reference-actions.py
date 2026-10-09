"""Import call UI from the supplied APK and bake the original public hand animation.

Laya binary layout reference: layabox/LayaAir-v1 AnimationClipParser02.as and
LoadModelV03.as. This reader converts data; game scripts are not bundled.
"""
import hashlib, json, math, struct, sys, urllib.parse, urllib.request, warnings, zipfile
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT/'tools/asset-inspector'))
import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION = '2022.3.62f2c1'
warnings.filterwarnings('ignore')
OUT = ROOT/'unity/Assets/Resources/Majsoul/Actions'
OUT.mkdir(parents=True, exist_ok=True)
CACHE = ROOT/'build/reference/hand-study'
CACHE.mkdir(parents=True, exist_ok=True)
INDEX = json.loads((ROOT/'build/reference/public/resversion0.11.252.w.json').read_text())['res']
records = []

def fetch(path):
    dest = CACHE/Path(path).name
    versions = list(dict.fromkeys([INDEX[path]['prefix'], 'v0.10.1.w']))
    if dest.exists():
        data=dest.read_bytes()
        version='v0.10.1.w' if path.endswith('Dapai_Ani-Dapai.lani') else INDEX[path]['prefix']
        records.append({'source':'https://game.maj-soul.com/1/'+version+'/'+urllib.parse.quote(path),'sha256':hashlib.sha256(data).hexdigest()})
        return data
    for version in versions:
        url = 'https://game.maj-soul.com/1/'+version+'/'+urllib.parse.quote(path)
        try:
            data = urllib.request.urlopen(url, timeout=30).read()
            dest.write_bytes(data)
            records.append({'source':url, 'sha256':hashlib.sha256(data).hexdigest()})
            return data
        except urllib.error.HTTPError as error:
            if error.code != 404: raise
    raise RuntimeError('Missing source: '+path)

class Reader:
    def __init__(self, data): self.data, self.p = data, 0
    def read(self, fmt):
        values = struct.unpack_from('<'+fmt, self.data, self.p)
        self.p += struct.calcsize('<'+fmt)
        return values[0] if len(values)==1 else list(values)
    def text(self):
        n=self.read('H'); value=self.data[self.p:self.p+n].decode();self.p+=n;return value
    def header(self):
        self.version=self.text();self.offset,self.size=self.read('II')
        self.blocks=[self.read('II') for _ in range(self.read('H'))]
        offset,count=self.read('IH');position=self.p;self.p=self.offset+offset
        self.strings=[self.text() for _ in range(count)];self.p=position
    def string(self):return self.strings[self.read('H')]

def clip(data):
    r=Reader(data);r.header();assert r.version=='LAYAANIMATION:02'
    assert r.string()=='ANIMATIONS'
    widths=[r.read('H')//4 for _ in range(r.read('B'))]
    times=[r.read('f') for _ in range(r.read('H'))]
    name=r.string();duration=r.read('f');loop=r.read('B');fps=r.read('h');count=r.read('h');tracks=[]
    for _ in range(count):
        path='/'.join(r.string() for _ in range(r.read('H')));component=r.read('h');prop=r.string();width=widths[r.read('B')]
        keys=[]
        for _ in range(r.read('H')):
            time=times[r.read('H')];values=[r.read(str(width)+'f') for _ in range(3)]
            keys.append({'t':time,'in':np.atleast_1d(values[0]),'out':np.atleast_1d(values[1]),'v':np.atleast_1d(values[2])})
        tracks.append((path,prop,keys))
    print('Animation',name,'duration',duration,'tracks',len(tracks),flush=True)
    return duration,tracks

def sample(keys,time):
    if time<=keys[0]['t']:return keys[0]['v']
    for a,b in zip(keys,keys[1:]):
        if time>b['t']:continue
        dt=b['t']-a['t'];t=(time-a['t'])/dt
        # The source uses Hermite curves, including stepped infinite tangents.
        slope=np.isfinite(a['out'])&np.isfinite(b['in'])
        return np.where(slope,(2*t**3-3*t*t+1)*a['v']+(t**3-2*t*t+t)*dt*np.nan_to_num(a['out'],posinf=0,neginf=0)+(-2*t**3+3*t*t)*b['v']+(t**3-t*t)*dt*np.nan_to_num(b['in'],posinf=0,neginf=0),a['v'])
    return keys[-1]['v']

def matrix(p,q,s):
    x,y,z,w=np.array(q)/np.linalg.norm(q)
    m=np.eye(4);m[:3,:3]=np.array([[1-2*y*y-2*z*z,2*x*y-2*z*w,2*x*z+2*y*w],[2*x*y+2*z*w,1-2*x*x-2*z*z,2*y*z-2*x*w],[2*x*z-2*y*w,2*y*z+2*x*w,1-2*x*x-2*y*y]])@np.diag(s)
    m[:3,3]=p;return m

def bake_hand():
    prefix='scene/Assets/Resource/table/hand/'
    mesh=fetch(prefix+'Hand_Skin-hand.lm');avatar=json.loads(fetch(prefix+'Hand_Skin-hand_human-Hand_SkinAvatar.lav'))
    duration,tracks=clip(fetch(prefix+'Dapai_Ani-Dapai.lani'))
    r=Reader(mesh);r.header();assert r.version=='LAYAMODEL:0301'
    r.p=r.blocks[0][0];assert r.string()=='MESH';name=r.string();assert r.read('h')==1
    vs,vl=r.read('II');attribute=r.string();print('Mesh',name,attribute,vl,flush=True)
    sizes={'POSITION':12,'NORMAL':12,'UV':8,'BLENDWEIGHT':16,'BLENDINDICES':16,'TANGENT':16,'COLOR':16}
    attrs=attribute.split(',');stride=sum(sizes[a] for a in attrs);n=vl//stride
    offsets={};offset=0
    for a in attrs:offsets[a]=offset;offset+=sizes[a]
    def floats(attr,width):return np.array([struct.unpack_from('<'+str(width)+'f',mesh,r.offset+vs+i*stride+offsets[attr]) for i in range(n)])
    vertices=floats('POSITION',3);uv=floats('UV',2);normals=floats('NORMAL',3);weights=floats('BLENDWEIGHT',4)
    bones=floats('BLENDINDICES',4).astype(int)
    start,length=r.read('II');indices=np.frombuffer(mesh,dtype='<u2',count=length//2,offset=r.offset+start).copy()
    names=[r.string() for _ in range(r.read('H'))];r.p+=8;bs,bl=r.read('II');bind=np.frombuffer(mesh,dtype='<f4',count=bl//4,offset=r.offset+bs).reshape(-1,4,4).transpose(0,2,1)
    submeshes=[]
    for block in r.blocks[1:]:
        r.p=block[0];assert r.string()=='SUBMESH';r.read('h');r.read('II');start,count=r.read('II');submeshes.append(indices[start:start+count].tolist())
        for _ in range(r.read('H')):
            drawstart,drawcount,ds,dl=r.read('IIII');dictionary=np.frombuffer(mesh,dtype='u1',count=dl,offset=r.offset+ds)
            vertexids=np.unique(indices[drawstart:drawstart+drawcount]);bones[vertexids]=dictionary[bones[vertexids]]
    nodes=[]
    def visit(node,parent,path):
        name=node['props']['name'];path=path+'/'+name if path else name;props=node['customProps'];nodes.append((name,parent,path,props))
        index=len(nodes)-1
        for child in node.get('child',[]):visit(child,index,path)
    visit(avatar['rootNode'],-1,'')
    nodeids={name:i for i,(name,_,_,_) in enumerate(nodes)}
    rest=[]
    for name,parent,path,props in nodes:
        m=matrix(props['translate'],props['rotation'],props['scale']);rest.append(rest[parent]@m if parent>=0 else m)
    expected=rest[nodeids[names[0]]]@bind[0]
    assert max(np.max(np.abs(rest[nodeids[name]]@bind[i]-expected)) for i,name in enumerate(names))<.001,'Inconsistent bind pose'
    assert np.max(np.abs(weights.sum(axis=1)-1))<.001,'Invalid skin weights'
    worldvertices=[];worldnormals=[];tilepoints=[];frames=round(duration*30)+1
    for frame in range(frames):
        time=min(duration,frame/30);pose={path:{'localPosition':props['translate'],'localRotation':props['rotation'],'localScale':props['scale']} for _,_,path,props in nodes}
        for path,prop,keys in tracks:
            path=nodes[0][0]+('/'+path if path else '')
            if path in pose:pose[path][prop]=sample(keys,time)
        worlds=[]
        for name,parent,path,props in nodes:
            t=pose[path];m=matrix(t['localPosition'],t['localRotation'],t['localScale']);worlds.append(worlds[parent]@m if parent>=0 else m)
        skin=np.array([worlds[nodeids[name]]@bind[i] for i,name in enumerate(names)])
        v=np.concatenate([vertices,np.ones((n,1))],axis=1);result=np.zeros((n,3));normal=np.zeros((n,3))
        for k in range(4):
            mats=skin[bones[:,k]];result+=np.einsum('nij,nj->ni',mats,v)[:,:3]*weights[:,k,None];normal+=np.einsum('nij,nj->ni',mats[:,:3,:3],normals)*weights[:,k,None]
        # Reflect the Laya coordinates; this exporter already has opposite winding.
        result[:,2]*=-1;normal[:,2]*=-1
        worldvertices.append(result);worldnormals.append(normal/np.maximum(np.linalg.norm(normal,axis=1)[:,None],1e-8))
        point=worlds[nodeids['node_tile']][:3,3].copy();point[2]*=-1;tilepoints.append(point.tolist())
    data=np.stack([worldvertices,worldnormals],axis=1).astype('<f4')
    (OUT/'discard-hand-frames.bytes').write_bytes(data.tobytes())
    # This legacy exporter already stores the opposite winding to its normals.
    # The coordinate reflection makes these source indices outward in Unity.
    triangles=submeshes
    for frame,(positions,normals_at_frame) in enumerate(zip(worldvertices,worldnormals)):
        for group in triangles:
            ids=np.asarray(group).reshape(-1,3)
            a,b,c=(positions[ids[:,i]] for i in range(3))
            area=np.cross(b-a,c-a)
            dot=np.einsum('ij,ij->i',area,normals_at_frame[ids].mean(axis=1))
            valid=np.linalg.norm(area,axis=1)>1e-9
            assert (dot[valid]>0).mean()>.98, f'Inward hand surfaces in frame {frame}'
    result={'vertices':n,'frames':frames,'fps':30,'duration':duration,'uv':uv.reshape(-1).tolist(),'body':triangles[0],'nails':triangles[1],'tilePoints':[x for p in tilepoints for x in p]}
    (OUT/'discard-hand.json').write_text(json.dumps(result,separators=(',',':')))
    print('Baked original hand:',n,'vertices',frames,'frames; bounds',np.min(worldvertices,axis=(0,1)),np.max(worldvertices,axis=(0,1)),flush=True)
    for path,output in [(prefix+'Majiang_Hand_Girl_Hand_001_Low.png','hand-skin.png'),('scene/Assets/Resource/Materials/Majiang_Hand_Girl_Fingernail_001 _Low.png','hand-nails.png')]:
        (OUT/output).write_bytes(fetch(path))

def import_ui():
    rows=[json.loads(line) for line in (ROOT/'build/reference/fast-catalog.jsonl').read_text(encoding='utf8').splitlines()]
    with zipfile.ZipFile(ROOT/'dist/Majsoul_4.0.16_MC.apk') as z:
        for needle,names in [('atlas_mj_main_chs_0.png',{'op_peng','op_chi','op_gang','op_hu','op_zimo','op_lizhi','op_babei','op_liuju','op_x','shout_peng','shout_chi','shout_gang','shout_lizhi','shout_babei','show_rong','show_zimo'}),('atlas_mj_main_common_0.png',{'bg_action','bg_chipengchoose','btn_skip','shout_horizontal','shout_vertical','shout_maskleftright','shout_maskup'})]:
            row=next(r for r in rows if any(p.endswith(needle) for p in r['paths']));env=UnityPy.load(z.read(row['bundle']))
            for obj in env.objects:
                if obj.type.name=='Sprite' and obj.peek_name() in names:
                    d=obj.read();p=OUT/(d.m_Name+'.png');d.image.save(p);records.append({'file':p.name,'bundle':row['bundle'],'pathID':obj.path_id})
        row=next(r for r in rows if any(p.endswith('/character/yiji/full/full.png') for p in r['paths']));env=UnityPy.load(z.read(row['bundle']));obj=next(o for o in env.objects if o.type.name=='Sprite');obj.read().image.save(OUT/'yiji-full.png');records.append({'file':'yiji-full.png','bundle':row['bundle'],'pathID':obj.path_id})
        path='myassets/deco/effect_mingpaizhishi/effect_zhishi/3d/texture/majiangarrow.png'
        row=next(r for r in rows if path in r['paths']);env=UnityPy.load(z.read(row['bundle']))
        arrow=env.container[path].read().image.convert('RGBA')
        # Original particle material uses additive black. Reconstruct its alpha for GUI.
        rgba=np.asarray(arrow).copy();rgba[:,:,3]=np.max(rgba[:,:,:3],axis=2)
        from PIL import Image
        Image.fromarray(rgba).save(OUT/'response-arrow.png')
        records.append({'file':'response-arrow.png','bundle':row['bundle'],'source':path,'conversion':'additive black to alpha for GUI'})

if __name__=='__main__':
    import_ui();bake_hand()
    for file in OUT.iterdir():
        if file.suffix in {'.png','.bytes','.json'} and file.name!='provenance.json':records.append({'output':file.name,'sha256':hashlib.sha256(file.read_bytes()).hexdigest()})
    (OUT/'provenance.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8')
