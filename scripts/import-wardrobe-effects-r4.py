"""Port the complete original particle graphs, preserving renderer/axis semantics."""
from pathlib import Path
import ast,hashlib,io,json,posixpath,runpy,struct
from PIL import Image
R=Path(__file__).resolve().parent.parent
study=runpy.run_path(str(R/'scripts/study-wardrobe-effects-r4.py'));fetch=study['fetch']
OUT=R/'unity/Assets/Resources/Majsoul/Actions'
reader=runpy.run_path(str(R/'scripts/import-hand-animations.py'))['Reader']
records=[]
def texture(path):
    path=posixpath.normpath(path);raw=fetch('scene/'+path);key='r4-'+hashlib.sha256(path.encode()).hexdigest()[:16]
    Image.open(io.BytesIO(raw)).save(OUT/(key+'.png'));records.append(dict(path='scene/'+path,sha256=hashlib.sha256(raw).hexdigest(),output=key+'.png'));return key
def mesh(path):
    raw=fetch('scene/'+path);r=reader(raw);r.header();assert r.version=='LAYAMODEL:0301',r.version
    r.p=r.blocks[0][0];assert r.string()=='MESH';r.string();assert r.read('h')==1
    start,length=r.read('II');attrs=r.string().split(',');sizes={'POSITION':12,'NORMAL':12,'UV':8,'UV1':8,'BLENDWEIGHT':16,'BLENDINDICES':16,'TANGENT':16,'COLOR':16};stride=sum(sizes[a] for a in attrs);uvOffset=sum(sizes[a] for a in attrs[:attrs.index('UV')]);v=[];uv=[]
    for i in range(length//stride):
        p=r.offset+start+i*stride;x,y,z=struct.unpack_from('<3f',raw,p);u,w=struct.unpack_from('<2f',raw,p+uvOffset);v.extend([x,y,-z]);uv.extend([u,1-w])
    start,length=r.read('II');idx=list(struct.unpack_from('<'+str(length//2)+'H',raw,r.offset+start))
    records.append(dict(path='scene/'+path,sha256=hashlib.sha256(raw).hexdigest()))
    return dict(positions=v,uv=uv,indices=idx)
tree=ast.parse((R/'scripts/import-riichi-fire.py').read_text('utf8'))
ns=dict(Path=Path,json=json,fetch=fetch,texture=texture,mesh=mesh)
for n in tree.body:
    if isinstance(n,ast.FunctionDef) and n.name in ['keys','pick','walk']:exec(compile(ast.Module(body=[n],type_ignores=[]),'<source-graph>', 'exec'),ns)
def convert(key,output):
    from effect_compatibility import require_supported
    source=json.loads(fetch('scene/'+key+'.lh'));require_supported(source,key);ns['nodes']=[];ns['layers']=[];ns['walk'](source)
    native=[]
    def visit(n):
        if n['type']=='ShuriKenParticle3D':native.append(n['customProps'])
        for c in n.get('child',[]):visit(c)
    visit(source)
    for l,c in zip(ns['layers'],native):
        l.update(sourceBursts=True,sourceAutoSeed=c.get('autoRandomSeed',True),sourceSeed=c.get('randomSeed',0))
        material=json.loads(fetch('scene/'+c['material']['path']))['props'];l['blendSrc']=1 if material.get('srcBlend')==1 else 5
        l['scaleMode']=c.get('scaleMode',0);l['simulationSpace']=c.get('simulationSpace',1)
        size=c.get('sizeOverLifetime',{}).get('size',{})
        if size.get('separateAxes'):
            for axis in 'XYZ':l['size'+axis]=ns['keys'](size.get('gradient'+axis,{}).get('sizes',[]))
        shape=c.get('shape',{});typ=shape.get('shapeType',-1)
        l['radius']=shape.get({0:'sphereRadius',1:'hemiSphereRadius',2:'coneRadius',4:'circleRadius'}.get(typ,''),.01)
        l['box']=[shape.get('box'+a,1) for a in 'XYZ'];l['randomDirection']=shape.get({0:'sphereRandomDirection',1:'hemiSphereRandomDirection',2:'coneRandomDirection',3:'boxRandomDirection',4:'circleRandomDirection'}.get(typ,''),0)
        velocity=c.get('velocityOverLifetime',{})
        if velocity.get('enable'):
            v=velocity['velocity'];l['velocitySpace']=velocity.get('space',0);l['velocityType']=v['type']
            if v['type']==0:l['velocityMin']=l['velocityMax']=v['constant']
            if v['type']==1:
                for axis in 'XYZ':l['velocity'+axis]=ns['keys'](v.get('gradient'+axis,{}).get('velocitys',[]))
        rotation=c.get('rotationOverLifetime',{})
        if rotation.get('enable'):
            angular=rotation['angularVelocity'];l['angularSpeed']=angular.get('constant',0)
            l['angularMin']=angular.get('constantMin',0) if angular['type']==2 else l['angularSpeed'];l['angularMax']=angular.get('constantMax',0) if angular['type']==2 else l['angularSpeed']
            l['rotationCurve']=ns['keys'](angular.get('gradient',{}).get('angularVelocitys',[]))
        sheet=c.get('textureSheetAnimation',{})
        if sheet.get('enable'):
            count=sheet['tiles'][0]*sheet['tiles'][1];f=sheet['frame'];l['randomFrame']=f['type']==2
            l['frames']=[dict(time=k['key'],value=k['value']/count) for k in f.get('overTime',{}).get('frames',[])]
            l['frameMin']=f.get('constantMin',0)/count;l['frameMax']=f.get('constantMax',count-1)/count
            l['startFrame']=sheet.get('startFrame',{}).get('constant',0)/count
    data=dict(schema=2,nodes=ns['nodes'],layers=ns['layers'])
    (OUT/(output+'.json')).write_text(json.dumps(data,ensure_ascii=False,indent=1)+'\n','utf8')
    records.append(dict(path='scene/'+key+'.lh',sha256=hashlib.sha256(fetch('scene/'+key+'.lh')).hexdigest(),output=output+'.json',outputSha256=hashlib.sha256((OUT/(output+'.json')).read_bytes()).hexdigest(),layers=len(native)))
    print(key,len(native),'complete layers',flush=True)
for key,output in [('ron_huolongjuan','fire-effect'),('effect_hupai_fire','flame-win-effect'),('effect_hupai_default','default-win-effect'),('effect_liqi_ice','riichi-ice-effect'),('effect_liqi_fire','riichi-fire-effect')]:convert(key,output)
catalogFile=R/'unity/Assets/Resources/Majsoul/Wardrobe/catalog.json';catalog=json.loads(catalogFile.read_text('utf8'))
if not any(i['id']=='stick-default' for i in catalog['items']):catalog['items'].insert(0,dict(id='stick-default',category='stick',name='普通立直棒',builtin=True,icon='Majsoul/Wardrobe/UI/slot_liqibang',standardStick=True))
if not any(i['id']=='ron-flame' for i in catalog['items']):
    path='extendRes/items/ron_fire.jpg';raw=fetch(path)
    try:im=Image.open(io.BytesIO(raw));im.load()
    except Exception:im=Image.open(io.BytesIO(bytes(b^0x49 for b in raw)))
    im.save(R/'unity/Assets/Resources/Majsoul/Wardrobe/Icons/ron-flame.png')
    catalog['items'].append(dict(id='ron-flame',category='winEffect',name='和牌特效－烈焰',builtin=True,icon='Majsoul/Wardrobe/Icons/ron-flame',effect='Majsoul/Actions/flame-win-effect',duration=3,cue=''))
for i in catalog['items']:
    if i['category']=='winEffect':i.pop('effectScale',None)
    if i['id']=='riichi-ice':i['groundEffect']=True;i['duration']=5;i['cue']='effect_rich_frozen'
    if i['id']=='ron-flame':i['cue']='effect_hupai_fire'
    if i['category']=='hand':
        # Original animation/animation keypoint[0] / lifetime, before source speed.
        # Ratios preserve the existing presentation retiming without moving the cue
        # to an unrelated fixed offset from contact.
        i['riichiNormalPoint']=319/(460 if i['id']=='hand-human' else 500)
        i['riichiSidewaysPoint']=550/1615 if i['id']=='hand-human' else 400/900
for cue in ['effect_rich_frozen','effect_hupai_fire']:
    path='audio/audio_mj/'+cue+'.mp3';raw=fetch(path);(R/'unity/Assets/Resources/Majsoul/Audio'/(cue+'.mp3')).write_bytes(raw);records.append(dict(path=path,sha256=hashlib.sha256(raw).hexdigest()))
catalogFile.write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n','utf8')
(R/'unity/Assets/Resources/Majsoul/Wardrobe/provenance-r4.json').write_text(json.dumps(dict(records=records,sourceCode='ShowHuleEffect; ShowChiPengEffect; Laya ShurikenParticleSystem and particle shader'),ensure_ascii=False,indent=2)+'\n','utf8')
# Keep the earlier shared resource manifest consistent with the replacement graph.
shared=R/'unity/Assets/Resources/Majsoul/Results/source.json';entries=json.loads(shared.read_text('utf8'))
for entry in entries:
    if entry.get('output')=='Actions/fire-effect.json':
        entry['sha256']=hashlib.sha256((OUT/'fire-effect.json').read_bytes()).hexdigest();entry['adaptation']='Original ron_huolongjuan; schema 2 complete source graph, native renderer modes, per-axis curves, mesh data and rotation. See Wardrobe/provenance-r4.json.'
shared.write_text(json.dumps(entries,ensure_ascii=False,indent=2)+'\n','utf8')
aggregate=R/'unity/Assets/Resources/Majsoul/SettlementVoice/provenance.json';entries=json.loads(aggregate.read_text('utf8'))
for entry in entries:
    if entry.get('output') in ['Actions/fire-effect.json','Results/source.json']:
        entry['sha256']=hashlib.sha256((R/'unity/Assets/Resources/Majsoul'/entry['output']).read_bytes()).hexdigest()
aggregate.write_text(json.dumps(entries,ensure_ascii=False,indent=2)+'\n','utf8')
