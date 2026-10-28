"""Source-backed extra outfits and the original stretch-particle UV convention."""
from pathlib import Path
import ast,hashlib,io,json,posixpath,runpy,struct,urllib.parse,urllib.request
import numpy as np
from PIL import Image

R=Path(__file__).resolve().parent.parent
OUT=R/'unity/Assets/Resources/Majsoul/Actions'
WARDROBE=OUT.parent/'Wardrobe'
CACHE=R/'build/wardrobe-r6-study/source'
INDEX=json.loads((R/'build/reference/public/resversion0.11.252.w.json').read_text('utf8'))['res']
TABLES=json.loads((R/'build/wardrobe-r6-study/tables.json').read_text('utf8'))
records=[]

def fetch(path):
    dest=CACHE/path;dest.parent.mkdir(parents=True,exist_ok=True)
    old=R/'build/wardrobe-r4-study/source'/path
    cached=R/'build/wardrobe-reference-r2/cdn'/(hashlib.sha256(path.encode()).hexdigest()[:16]+Path(path).suffix)
    hand_cache=R/'build/reference/hand-study'/Path(path).name
    url=None
    if not dest.exists():
        if old.exists():dest.write_bytes(old.read_bytes())
        elif cached.exists():dest.write_bytes(cached.read_bytes())
        elif '/table/hand/'in path and hand_cache.exists():dest.write_bytes(hand_cache.read_bytes())
        else:
            for prefix in dict.fromkeys([INDEX[path]['prefix'],'v0.10.1.w']):
                candidate='https://game.maj-soul.com/1/'+prefix+'/'+urllib.parse.quote(path)
                try:
                    dest.write_bytes(urllib.request.urlopen(candidate,timeout=45).read());url=candidate;break
                except urllib.error.HTTPError as error:
                    if error.code!=404:raise
            else:raise RuntimeError('Missing source '+path)
    raw=dest.read_bytes();records.append(dict(path=path,sha256=hashlib.sha256(raw).hexdigest(),url=url,cache=dest.relative_to(R).as_posix()))
    return raw

def image(path,folder,key):
    raw=fetch(path)
    try:im=Image.open(io.BytesIO(raw));im.load()
    except Exception:im=Image.open(io.BytesIO(bytes(b^0x49 for b in raw)))
    target=WARDROBE/folder/(key+'.png');target.parent.mkdir(parents=True,exist_ok=True);im.save(target)
    return 'Majsoul/Wardrobe/'+folder+'/'+key

def texture(path):
    raw=fetch('scene/'+path);key='r6-'+hashlib.sha256(path.encode()).hexdigest()[:16]
    Image.open(io.BytesIO(raw)).save(OUT/(key+'.png'));return key

reader=runpy.run_path(str(R/'scripts/import-hand-animations.py'))['Reader']
scope=dict(R=R,fetch=fetch,reader=reader,struct=struct,np=np,json=json,hashlib=hashlib,OUT=OUT,records=records)
tree=ast.parse((R/'scripts/import-wardrobe-effects-r4.py').read_text('utf8'))
for node in tree.body:
    if isinstance(node,ast.FunctionDef)and node.name=='mesh':exec(compile(ast.Module(body=[node],type_ignores=[]),'<source-mesh>','exec'),scope)
ns=dict(Path=Path,json=json,fetch=fetch,texture=texture,mesh=scope['mesh'])
tree=ast.parse((R/'scripts/import-riichi-fire.py').read_text('utf8'))
for node in tree.body:
    if isinstance(node,ast.FunctionDef)and node.name in ['keys','pick','walk']:exec(compile(ast.Module(body=[node],type_ignores=[]),'<source-particles>','exec'),ns)
scope['ns']=ns
for node in ast.parse((R/'scripts/import-wardrobe-effects-r4.py').read_text('utf8')).body:
    if isinstance(node,ast.FunctionDef)and node.name=='convert':exec(compile(ast.Module(body=[node],type_ignores=[]),'<complete-source-graph>','exec'),scope)

def skin_rig(mesh_path,avatar_path):
    mesh=fetch('scene/'+mesh_path);avatar=json.loads(fetch('scene/'+avatar_path));r=reader(mesh);r.header();assert r.version=='LAYAMODEL:0301'
    r.p=r.blocks[0][0];r.string();r.string();r.read('h');vs,vl=r.read('II');attrs=r.string().split(',')
    sizes={'POSITION':12,'NORMAL':12,'UV':8,'BLENDWEIGHT':16,'BLENDINDICES':16,'TANGENT':16,'COLOR':16};offsets={};stride=0
    for a in attrs:offsets[a]=stride;stride+=sizes[a]
    count=vl//stride
    def floats(a,n):return np.array([struct.unpack_from('<'+str(n)+'f',mesh,r.offset+vs+i*stride+offsets[a])for i in range(count)])
    bones=floats('BLENDINDICES',4).astype(int);start,length=r.read('II');indices=np.frombuffer(mesh,dtype='<u2',count=length//2,offset=r.offset+start).copy()
    names=[r.string()for _ in range(r.read('H'))];r.p+=8;bs,bl=r.read('II');bind=np.frombuffer(mesh,dtype='<f4',count=bl//4,offset=r.offset+bs).reshape(-1,4,4).transpose(0,2,1)
    groups=[]
    for block in r.blocks[1:]:
        r.p=block[0];r.string();r.read('h');r.read('II');start,n=r.read('II');groups.append(indices[start:start+n].tolist())
        for _ in range(r.read('H')):
            first,n,ds,dl=r.read('IIII');dictionary=np.frombuffer(mesh,dtype='u1',count=dl,offset=r.offset+ds);ids=np.unique(indices[first:first+n]);bones[ids]=dictionary[bones[ids]]
    nodes=[]
    def visit(n,parent=-1,path=''):
        name=n['props']['name'];path=path+'/'+name if path else name;i=len(nodes);nodes.append((name,parent,path,n['customProps']))
        for c in n.get('child',[]):visit(c,i,path)
    visit(avatar['rootNode'])
    return dict(vertices=floats('POSITION',3),uv=floats('UV',2),normals=floats('NORMAL',3),weights=floats('BLENDWEIGHT',4),bones=bones,names=names,bind=bind,submeshes=groups+groups if len(groups)==1 else groups,single=len(groups)==1,nodes=nodes,nodeids={n[0]:i for i,n in enumerate(nodes)})

def import_hand(key,motion_prefix=None):
    graph=json.loads(fetch('scene/'+key+'.lh'));nodes=[]
    def visit(n):
        nodes.append(n)
        for child in n.get('child',[]):visit(child)
    visit(graph)
    animator=next(n['components']['Animator']for n in nodes if 'Animator'in n.get('components',{}))
    skinned=[n for n in nodes if n['type']=='SkinnedMeshSprite3D'];rigs=[skin_rig(n['customProps']['meshPath'],animator['avatar']['path'])for n in skinned]
    textures=[]
    for n in skinned:
        material_path=n['customProps']['materials'][0]['path'];m=json.loads(fetch('scene/'+material_path))['props'];tex=m['textures'][0]['path']
        if not tex.startswith('Assets/'):tex=posixpath.normpath(posixpath.join(posixpath.dirname(material_path),tex))
        textures.append(image('scene/'+tex,'Motion',key+'-'+n['props']['name']))
    hand=runpy.run_path(str(R/'scripts/import-hand-animations.py'));hand['bake'].__globals__.update(fetch=fetch,PREFIX='scene/')
    for action,clip_name,focus in [('hand','LiZhi','node_liqibang'),('discard','Dapai','node_tile'),('riichi','LiQiPai','node_tile'),('meld','Fulu','node_tile'),('tsumo','Zimo','node_tile')]:
        animation=next(p for p in animator['clipPaths']if p.endswith('-'+clip_name+'.lani'))
        parts=[hand['bake'](rig,animation,focus)for rig in rigs];base=parts[0][0];frames=base['frames'];merged=[];uv=[];body=[];nails=[];offset=0
        for i,(d,payload,land,points)in enumerate(parts):
            if rigs[i]['single']:d['nails']=[]
            merged.append(np.frombuffer(payload,'<f4').reshape(frames,2,d['vertices'],3));uv+=d['uv'];ids=[x+offset for group in [d['body'],d['nails']]for x in group]
            (body if i==0 else nails).extend(ids);offset+=d['vertices']
        base.update(vertices=offset,uv=uv,body=body,nails=nails,landTime=parts[0][2])
        file=WARDROBE/'Motion'/((motion_prefix or key+'-')+action);file.with_suffix('.json').write_text(json.dumps(base,separators=(',',':')),'utf8');Path(str(file)+'-frames.bytes').write_bytes(np.concatenate(merged,axis=2).astype('<f4').tobytes())
    config_type='hand_cat_orange'if key=='hand_cat'else key
    configs={r['name']:r for r in TABLES['animation/animation']if r['type']==config_type}
    entry=dict(id=key.replace('_','-'),category='hand',name='手部－汪爪'if key=='hand_dog'else'手部－男性手',builtin=True,icon='Majsoul/Wardrobe/UI/slot_hand',motion='Majsoul/Wardrobe/Motion/'+key+'-hand',motionSet='Majsoul/Wardrobe/Motion/'+key+'-',texture=textures[0],nailTexture=textures[-1],duration=2.6)
    for action,field in [('Dapai','riichiNormalPoint'),('LiQiPai','riichiSidewaysPoint')]:entry[field]=configs[action]['keypoint'][0][0]/configs[action]['lifetime']
    return entry

def main():
    catalog_file=WARDROBE/'catalog.json';catalog=json.loads(catalog_file.read_text('utf8'))
    previous=WARDROBE/'provenance-r6.json'
    if previous.exists():
        adapted={r['output'].removesuffix('.png'):r['source']for r in json.loads(previous.read_text('utf8'))['records']if r.get('adaptation')}
        for file in OUT.glob('*-effect.json'):
            data=json.loads(file.read_text('utf8'));changed=False
            for layer in data.get('layers',[]):
                if layer.get('sourceStretchUV'):
                    layer['texture']=adapted[layer['texture']];layer.pop('sourceStretchUV');changed=True
            if changed:file.write_text(json.dumps(data,ensure_ascii=False,indent=1)+'\n','utf8')
    for source,output in [('effect_hupai_feng','wind-win-effect'),('effect_hupai_sakura','sakura-win-effect'),('effect_liqi_bao','riichi-blue-effect'),('effect_liqi_bianfu','riichi-bat-effect')]:scope['convert'](source,output)
    def add(entry):
        old=next((i for i in catalog['items']if i['id']==entry['id']),None)
        if old:old.update(entry)
        else:catalog['items'].append(entry)
    for ident,category,res,output in [('ron-wind','winEffect','effect_hupai_feng','wind-win-effect'),('ron-sakura','winEffect','effect_hupai_sakura','sakura-win-effect'),('riichi-blue','riichiEffect','effect_liqi_bao','riichi-blue-effect'),('riichi-bat','riichiEffect','effect_liqi_bianfu','riichi-bat-effect')]:
        view=next(v for v in TABLES['item_definition/view']if v['res_name']==res);item=next(v for v in TABLES['item_definition/item']if v['id']==view['id']);cue=''
        audio=next((a for a in TABLES['audio/audio']if a['id']==view['audio_id']),None)
        if audio:
            path=audio['path'];cue=Path(path).stem;path=path if path.endswith('.mp3')else path+'.mp3';(R/'unity/Assets/Resources/Majsoul/Audio'/(cue+'.mp3')).write_bytes(fetch('audio/'+path if not path.startswith('audio/')else path))
        add(dict(id=ident,category=category,name=item['name_chs'],builtin=True,icon=image(item['icon'],'Icons',ident),effect='Majsoul/Actions/'+output,groundEffect=view['sargs'][0]=='shu',cue=cue,duration=3))
    # Two explicit trials reuse original particle shapes; they are not listed as
    # previously released Mahjong Soul indicator costumes in the item table.
    for key,base,keep,icon,title in [('callout-sakura','sakura-win-effect',['yezi','yuan add xiao'],'ron-sakura','鸣牌指示－樱瓣（试装）'),('callout-bat','riichi-bat-effect',['bianfu','quan','di'],'riichi-bat','鸣牌指示－蝙蝠（试装）')]:
        data=json.loads((OUT/(base+'.json')).read_text('utf8'))
        if key=='callout-sakura':data['layers']=[l for l in data['layers']if l['name'].startswith('yezi')or l['name']=='yuan add']
        else:data['layers']=[l for l in data['layers']if any(x in l['name']for x in keep)]
        for layer in data['layers']:layer.update(duration=1.2,rate=max(layer['rate'],1.2))
        (OUT/(key+'.json')).write_text(json.dumps(data,ensure_ascii=False,indent=1),'utf8')
        add(dict(id=key,category='callout',name=title,builtin=True,icon='Majsoul/Wardrobe/Icons/'+icon,effect='Majsoul/Actions/'+key,effectScale=.3 if key=='callout-sakura' else .4,trialStyle=True,duration=3))
    for key in ['hand_dog','hand_man']:add(import_hand(key))
    import_hand('hand_cat','cat-')
    hand=runpy.run_path(str(R/'scripts/import-hand-animations.py'));hand['bake'].__globals__.update(fetch=fetch,PREFIX='scene/Assets/Resource/table/hand/');rig=hand['load_rig']()
    for name,animation,focus in hand['CLIPS']:
        data,payload,_,_=hand['bake'](rig,animation,focus);file=OUT/(name+'.json');old=json.loads(file.read_text('utf8'))
        assert payload==(OUT/(name+'-frames.bytes')).read_bytes(),'Base hand mesh changed while adding the source rotation track'
        old['tileRotations']=data['tileRotations'];file.write_text(json.dumps(old,separators=(',',':')),'utf8')
    for item in catalog['items']:
        if item['id']in ['hand-cat','hand-cat-orange']:item['motionSet']='Majsoul/Wardrobe/Motion/cat-'
        if item['id']=='win-default':item['name']='和牌特效－雷击'
    # BakeMesh inspection confirms Unity Stretch already maps the long axis
    # to texture U. Retain original pixels/UVs rather than rotating them.
    for file in OUT.glob('r6-stretch-*.png'):
        assert file.resolve().is_relative_to(OUT.resolve());file.unlink();meta=Path(str(file)+'.meta')
        if meta.exists():meta.unlink()
    catalog_file.write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n','utf8')
    (WARDROBE/'provenance-r6.json').write_text(json.dumps(dict(records=records,trialIndicators=['callout-sakura','callout-bat'],stretchShader='build/wardrobe-r6-study/original-particle-vertex.glsl'),ensure_ascii=False,indent=2),'utf8')
    shared=OUT.parent/'Results/source.json';entries=json.loads(shared.read_text('utf8'))
    for row in entries:
        if row.get('output')=='Actions/fire-effect.json':row['sha256']=hashlib.sha256((OUT/'fire-effect.json').read_bytes()).hexdigest()
    shared.write_text(json.dumps(entries,ensure_ascii=False,indent=2)+'\n','utf8')
    hand_manifest=OUT/'hand-provenance.json';hand_records=json.loads(hand_manifest.read_text('utf8'))
    for row in hand_records:
        if row.get('output')and(OUT/row['output']).is_file():row['sha256']=hashlib.sha256((OUT/row['output']).read_bytes()).hexdigest()
    hand_manifest.write_text(json.dumps(hand_records,ensure_ascii=False,indent=2)+'\n','utf8')
    aggregate=OUT.parent/'SettlementVoice/provenance.json';entries=json.loads(aggregate.read_text('utf8'))
    for row in entries:
        output=row.get('output','')
        if output in ['Actions/fire-effect.json','Results/source.json']or output.startswith('Actions/')and('hand' in output):row['sha256']=hashlib.sha256((OUT.parent/output).read_bytes()).hexdigest()
    aggregate.write_text(json.dumps(entries,ensure_ascii=False,indent=2)+'\n','utf8')
    ids={}
    for item in catalog['items']:ids.setdefault(item['category'],[]).append(item['id'])
    import re
    engine=R/'app/src/main/assets/engine.js';raw=engine.read_bytes();ending=b'\r\n'if raw.count(b'\r\n')>raw.count(b'\n')/2 else b'\n';code=raw.decode('utf8').replace('\r\n','\n');code=re.sub(r'const WARDROBE_CATALOG=\{[^\n]+\};','const WARDROBE_CATALOG='+json.dumps(ids,separators=(',',':'))+';',code)
    engine.write_bytes(code.encode('utf8').replace(b'\n',ending));(R/'unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js').write_bytes(engine.read_bytes())
    print('R6 source import PASS',len(catalog['items']),'items',flush=True)

if __name__=='__main__':main()
