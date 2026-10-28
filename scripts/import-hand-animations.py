"""Bake the original Majsoul hand animations onto the public mesh and skeleton.

Clips: the plain discard (Dapai), the riichi declaration discard (LiQiPai), the
riichi-stick placement (LiZhi) and the meld/call motion (Fulu). bake_hand() in
import-reference-actions.py produced discard-hand.*; this script reproduces it
byte for byte first, then adds the rest, so a drifting reader cannot go unnoticed.

Laya binary layout reference: layabox/LayaAir-v1 AnimationClipParser02.as and
LoadModelV03.as. This reader converts data; game scripts are not bundled.
"""
import hashlib, json, struct, sys, urllib.parse, urllib.request
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT/'unity/Assets/Resources/Majsoul/Actions'
CACHE = ROOT/'build/reference/hand-study'
PUBLIC = ROOT/'build/reference/public'
CACHE.mkdir(parents=True, exist_ok=True); PUBLIC.mkdir(parents=True, exist_ok=True)

# stem, source clip, node whose world position the carried object follows
CLIPS = [
    ('discard-hand', 'Dapai_Ani-Dapai.lani', 'node_tile'),
    ('riichi-hand', 'Anim_Dapai_heng-LiQiPai.lani', 'node_tile'),
    ('lizhi-hand', 'Lizhi_Ani-LiZhi.lani', 'node_liqibang'),
    ('fulu-hand', 'Fulu_Ani-Fulu.lani', 'node_tile'),
]
PREFIX = 'scene/Assets/Resource/table/hand/'
SELF_CHECK = 'discard-hand'
records = []

INDEX_PATH = PUBLIC/'resversion0.11.252.w.json'
if not INDEX_PATH.exists() or INDEX_PATH.stat().st_size == 0:
    INDEX_PATH.write_bytes(urllib.request.urlopen('https://game.maj-soul.com/1/resversion0.11.252.w.json', timeout=120).read())
INDEX = json.loads(INDEX_PATH.read_text(encoding='utf8'))['res']

def fetch(path):
    dest = CACHE/Path(path).name
    if dest.exists() and dest.stat().st_size > 0:
        data = dest.read_bytes()
        records.append({'source': 'https://game.maj-soul.com/1/<version>/'+urllib.parse.quote(path),
                        'cachedAs': dest.name, 'sha256': hashlib.sha256(data).hexdigest()})
        return data
    last = None
    for version in list(dict.fromkeys([INDEX[path]['prefix'], 'v0.10.1.w'])):
        url = 'https://game.maj-soul.com/1/'+version+'/'+urllib.parse.quote(path)
        try:
            data = urllib.request.urlopen(url, timeout=60).read()
            dest.write_bytes(data)
            records.append({'source': url, 'sha256': hashlib.sha256(data).hexdigest()})
            return data
        except urllib.error.HTTPError as error:
            last = error
            if error.code != 404: raise
    raise RuntimeError('Missing source: '+path) from last

class Reader:
    def __init__(self, data): self.data, self.p = data, 0
    def read(self, fmt):
        values = struct.unpack_from('<'+fmt, self.data, self.p)
        self.p += struct.calcsize('<'+fmt)
        return values[0] if len(values)==1 else list(values)
    def text(self):
        n=self.read('H'); value=self.data[self.p:self.p+n].decode(); self.p+=n; return value
    def header(self):
        self.version=self.text(); self.offset,self.size=self.read('II')
        self.blocks=[self.read('II') for _ in range(self.read('H'))]
        offset,count=self.read('IH'); position=self.p; self.p=self.offset+offset
        self.strings=[self.text() for _ in range(count)]; self.p=position
    def string(self): return self.strings[self.read('H')]

def clip(data):
    r=Reader(data); r.header(); assert r.version=='LAYAANIMATION:02'
    assert r.string()=='ANIMATIONS'
    widths=[r.read('H')//4 for _ in range(r.read('B'))]
    times=[r.read('f') for _ in range(r.read('H'))]
    name=r.string(); duration=r.read('f'); loop=r.read('B'); fps=r.read('h'); count=r.read('h'); tracks=[]
    for _ in range(count):
        path='/'.join(r.string() for _ in range(r.read('H'))); component=r.read('h'); prop=r.string(); width=widths[r.read('B')]
        keys=[]
        for _ in range(r.read('H')):
            time=times[r.read('H')]; values=[r.read(str(width)+'f') for _ in range(3)]
            keys.append({'t':time,'in':np.atleast_1d(values[0]),'out':np.atleast_1d(values[1]),'v':np.atleast_1d(values[2])})
        tracks.append((path,prop,keys))
    print('  clip %-10s duration=%.4fs fps=%d tracks=%d' % (name, duration, fps, len(tracks)), flush=True)
    return duration,tracks

def sample(keys,time):
    if time<=keys[0]['t']:return keys[0]['v']
    for a,b in zip(keys,keys[1:]):
        if time>b['t']:continue
        dt=b['t']-a['t'];t=(time-a['t'])/dt
        slope=np.isfinite(a['out'])&np.isfinite(b['in'])
        return np.where(slope,(2*t**3-3*t*t+1)*a['v']+(t**3-2*t*t+t)*dt*np.nan_to_num(a['out'],posinf=0,neginf=0)+(-2*t**3+3*t*t)*b['v']+(t**3-t*t)*dt*np.nan_to_num(b['in'],posinf=0,neginf=0),a['v'])
    return keys[-1]['v']

def matrix(p,q,s):
    x,y,z,w=np.array(q)/np.linalg.norm(q)
    m=np.eye(4);m[:3,:3]=np.array([[1-2*y*y-2*z*z,2*x*y-2*z*w,2*x*z+2*y*w],[2*x*y+2*z*w,1-2*x*x-2*z*z,2*y*z-2*x*w],[2*x*z-2*y*w,2*y*z+2*x*w,1-2*x*x-2*y*y]])@np.diag(s)
    m[:3,3]=p;return m

def load_rig():
    mesh=fetch(PREFIX+'Hand_Skin-hand.lm');avatar=json.loads(fetch(PREFIX+'Hand_Skin-hand_human-Hand_SkinAvatar.lav'))
    r=Reader(mesh);r.header();assert r.version=='LAYAMODEL:0301'
    r.p=r.blocks[0][0];assert r.string()=='MESH';r.string();assert r.read('h')==1
    vs,vl=r.read('II');attribute=r.string()
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
    for focus in {spec[2] for spec in CLIPS}: assert focus in nodeids, 'skeleton lost '+focus
    return dict(vertices=vertices,uv=uv,normals=normals,weights=weights,bones=bones,names=names,
                bind=bind,submeshes=submeshes,nodes=nodes,nodeids=nodeids)

def carried_rotation(matrix):
    reflection=np.diag([1,1,-1]);u,_,v=np.linalg.svd(matrix[:3,:3]);rotation=u@v
    if np.linalg.det(rotation)<0:u[:,-1]*=-1;rotation=u@v
    a=reflection@rotation@reflection;t=np.trace(a)
    if t>0:
        s=np.sqrt(t+1)*2;q=[(a[2,1]-a[1,2])/s,(a[0,2]-a[2,0])/s,(a[1,0]-a[0,1])/s,s/4]
    else:
        i=int(np.argmax(np.diag(a)));j=(i+1)%3;k=(i+2)%3;s=np.sqrt(max(0,1+a[i,i]-a[j,j]-a[k,k]))*2;q=[0.,0.,0.,0.]
        q[i]=s/4;q[j]=(a[j,i]+a[i,j])/s;q[k]=(a[k,i]+a[i,k])/s;q[3]=(a[k,j]-a[j,k])/s
    return (np.asarray(q)/np.linalg.norm(q)).tolist()

def bake(rig, animation, focus, fps=30):
    duration,tracks=clip(fetch(PREFIX+animation))
    nodes=rig['nodes'];nodeids=rig['nodeids'];names=rig['names'];bind=rig['bind']
    vertices=rig['vertices'];normals=rig['normals'];weights=rig['weights'];bones=rig['bones'];n=len(vertices)
    worldvertices=[];worldnormals=[];points=[];rotations=[];frames=round(duration*fps)+1
    for frame in range(frames):
        time=min(duration,frame/fps)
        pose={path:{'localPosition':props['translate'],'localRotation':props['rotation'],'localScale':props['scale']} for _,_,path,props in nodes}
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
        result[:,2]*=-1;normal[:,2]*=-1
        worldvertices.append(result);worldnormals.append(normal/np.maximum(np.linalg.norm(normal,axis=1)[:,None],1e-8))
        point=worlds[nodeids[focus]][:3,3].copy();point[2]*=-1;points.append(point.tolist())
        rotations.extend(carried_rotation(worlds[nodeids[focus]]))
    wv=np.array(worldvertices);wn=np.array(worldnormals);tp=np.array(points)
    data={'vertices':n,'frames':frames,'fps':fps,'duration':duration,'uv':rig['uv'].reshape(-1).tolist(),
          'body':rig['submeshes'][0],'nails':rig['submeshes'][1],'tilePoints':tp.reshape(-1).tolist(),'tileRotations':rotations}
    # Winding check: the exporter stores the opposite winding to its normals, and the
    # coordinate reflection makes the source indices outward in Unity. Roughly 2-3% of
    # the triangles are degenerate slivers along the UV seams in every pose, so the
    # bound is set to separate a correct mesh from a globally flipped one (which would
    # score near 0) rather than to demand perfection.
    worst=1.0
    for frame,(positions,normals_at_frame) in enumerate(zip(wv,wn)):
        for group in rig['submeshes']:
            ids=np.asarray(group).reshape(-1,3)
            a,b,c=(positions[ids[:,i]] for i in range(3))
            area=np.cross(b-a,c-a)
            dot=np.einsum('ij,ij->i',area,normals_at_frame[ids].mean(axis=1))
            valid=np.linalg.norm(area,axis=1)>1e-9
            worst=min(worst,(dot[valid]>0).mean())
            assert (dot[valid]>0).mean()>.90, 'Inward hand surfaces in frame %d' % frame
    print('  outward triangle fraction, worst frame: %.4f' % worst)
    # When the carried object stops moving, the river tile / stick replaces it.
    land=len(tp)-1
    while land>0 and np.max(np.abs(tp[land-1]-tp[-1]))<=1e-4: land-=1
    return data, np.stack([wv,wn],axis=1).astype('<f4').tobytes(), land/fps, tp

if __name__=='__main__':
    rig=load_rig()
    print('rig: %d vertices, %d submeshes, %d nodes' % (len(rig['vertices']),len(rig['submeshes']),len(rig['nodes'])),flush=True)
    written=[]
    for stem,animation,focus in CLIPS:
        print('\n[%s] %s (focus %s)' % (stem,animation,focus))
        data,payload,land_time,tp=bake(rig,animation,focus)
        # The committed discard-hand.json predates landTime, so the self-check compares
        # the legacy shape; new clips additionally carry landTime.
        plain={'vertices':data['vertices'],'frames':data['frames'],'fps':data['fps'],'duration':data['duration'],
               'uv':data['uv'],'body':data['body'],'nails':data['nails'],'tilePoints':data['tilePoints']}
        shaped={'vertices':data['vertices'],'frames':data['frames'],'fps':data['fps'],'duration':data['duration'],
                'landTime':land_time,'uv':data['uv'],'body':data['body'],'nails':data['nails'],'tilePoints':data['tilePoints'],'tileRotations':data['tileRotations']}
        final=json.dumps(shaped,separators=(',',':')).encode()
        if stem==SELF_CHECK:
            ref_json=(OUT/(stem+'.json')).read_bytes(); ref_bin=(OUT/(stem+'-frames.bytes')).read_bytes()
            reference=json.loads(ref_json);reference.pop('landTime',None);reference.pop('tileRotations',None)
            ok = plain==reference and payload==ref_bin
            print('  self-check vs committed artifacts: json %s, bytes %s' % (plain==reference, payload==ref_bin))
            if not ok:
                print('  SELF-CHECK FAILED - refusing to write any clip'); sys.exit(1)
            written.append((stem,len(final),len(payload),land_time)); continue
        (OUT/(stem+'.json')).write_bytes(final)
        (OUT/(stem+'-frames.bytes')).write_bytes(payload)
        print('  %.4fs -> %d frames; carried object settles at %.4fs' % (data['duration'],data['frames'],land_time))
        print('  trajectory start %s  end %s' % (np.round(tp[0],4), np.round(tp[-1],4)))
        written.append((stem,len(final),len(payload),land_time))
    print('\n--- summary ---')
    for stem,j,b,land in written: print('  %-14s json %6d B  frames %8d B  landTime %.4fs' % (stem,j,b,land))
    records.append({'note':'baked from the public Majsoul CDN; game scripts are not bundled'})
    for stem,_,_,_ in written:
        for name in [stem+'.json',stem+'-frames.bytes']:
            p=OUT/name
            if p.exists(): records.append({'output':name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
    (OUT/'hand-provenance.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8')
    print('wrote hand-provenance.json')
