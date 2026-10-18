"""Import the original fire-riichi graph, using APK textures where bundled.
The supplied base APK omits this optional cosmetic graph and audio; missing
files are taken from the fixed official CDN versions in the cached manifest.
"""
from pathlib import Path
import sys,json,hashlib,zipfile,urllib.request,urllib.parse,struct,io,warnings
R=Path(__file__).resolve().parent.parent;CACHE=R/'build/release-062/source';OUT=R/'unity/Assets/Resources/Majsoul/Actions'
sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
index=json.loads((R/'build/reference/public/resversion0.11.252.w.json').read_text('utf-8'))['res']
rows=[json.loads(l)for f in ['catalog.jsonl','fast-catalog.jsonl']for l in(R/'build/reference'/f).read_text('utf-8').splitlines()]
apk=zipfile.ZipFile(R/'dist/Majsoul_4.0.16_MC.apk');records=[];exported={};CACHE.mkdir(parents=True,exist_ok=True)
def fetch(path):
    file=CACHE/Path(path).name
    url='https://game.maj-soul.com/1/'+index[path]['prefix']+'/'+urllib.parse.quote(path)
    if not file.exists():
        for version in dict.fromkeys([index[path]['prefix'],'v0.10.1.w','v0.10.287.w']):
            url='https://game.maj-soul.com/1/'+version+'/'+urllib.parse.quote(path)
            try:file.write_bytes(urllib.request.urlopen(url,timeout=45).read());break
            except urllib.error.HTTPError as error:
                if error.code!=404:raise
        else:raise RuntimeError('Missing original resource '+path)
    raw=file.read_bytes();records.append(dict(source=url,sha256=hashlib.sha256(raw).hexdigest()));return raw
def texture(path):
    key=Path(path).stem
    if key in exported:return exported[key]
    name='riichi-fire-'+key.replace(' ','_');hits=[(p,r)for r in rows for p in r.get('paths',[])if p.endswith('.png')and Path(p).stem.lower()==key.lower()]
    if hits:
        p,row=hits[0];env=UnityPy.load(apk.read(row['bundle']));obj=next(o for o in env.objects if o.type.name=='Texture2D'and o.peek_name().lower()==key.lower());buf=io.BytesIO();obj.read().image.save(buf,format='PNG');raw=buf.getvalue();records.append(dict(source='APK:'+p,bundle=row['bundle'],pathID=obj.path_id,sha256=hashlib.sha256(raw).hexdigest()))
    else:raw=fetch('scene/'+path)
    (OUT/(name+'.png')).write_bytes(raw);exported[key]=name;return name
def keys(v):return [dict(time=x['key'],value=x['value'])for x in v]
def pick(c,n):return (c['start'+n+'ConstantMin'],c['start'+n+'ConstantMax'])if c['start'+n+'Type']==2 else(c['start'+n+'Constant'],)*2
def mesh(path):
    b=fetch('scene/'+path);start=b.index(struct.pack('<3f',-1.27,1.27,0));v=[struct.unpack_from('<12f',b,start+i*48)for i in range(4)]
    return dict(positions=[a for p in v for a in(p[0],p[1],-p[2])],uv=[a for p in v for a in(p[6],1-p[7])],indices=list(struct.unpack_from('<6H',b,len(b)-12)))
source=json.loads(fetch('scene/effect_liqi_fire.lh'));nodes=[];layers=[]
def walk(n,parent=-1):
    c=n.get('customProps',{});p=c.get('translate',[0,0,0]);q=c.get('rotation',[0,0,0,1]);node=len(nodes)
    nodes.append(dict(name=n['props']['name'],parent=parent,position=[p[0],p[1],-p[2]],rotation=[-q[0],-q[1],q[2],q[3]],scale=c.get('scale',[1,1,1])))
    if n['type']=='ShuriKenParticle3D':
        mat=json.loads(fetch('scene/'+c['material']['path']))['props'];tex=next(t['path']for t in mat['textures']if t['name']=='diffuseTexture')
        if not tex.startswith('Assets/'):tex=(Path(c['material']['path']).parent/tex).as_posix()
        col=c.get('colorOverLifetime',{});gradient=col.get('color',{}).get('gradient',{})if col.get('enable')else{}
        color=(c['startColorConstantMin'],c['startColorConstantMax'])if c['startColorType']==2 else(c['startColorConstant'],)*2
        life,size,speed,rot=[pick(c,x)for x in ['Lifetime','Size','Speed','Rotation']];sh=c.get('shape',{});so=c.get('sizeOverLifetime',{});sz=so.get('size',{});vel=c.get('velocityOverLifetime',{}).get('velocity',{})
        tiles=c.get('textureSheetAnimation',{});sheet=tiles if tiles.get('enable')else{}
        l=dict(name=n['props']['name'],node=node,texture=texture(tex),tint=next(v['value']for v in mat['vectors']if v['name']=='tintColor'),blendDst=mat['dstBlend'],duration=c['duration'],delay=c['startDelay'],lifeMin=life[0],lifeMax=life[1],sizeMin=size[0],sizeMax=size[1],speedMin=speed[0],speedMax=speed[1],rotationMin=rot[0],rotationMax=rot[1],colorMin=color[0],colorMax=color[1],size3d=[],size3dMin=[],maxParticles=c['maxParticles'],renderMode=c['renderMode'],stretch=c['stretchedBillboardLengthScale'],gravity=c['gravityModifier'],rate=c['emission']['emissionRate'],bursts=[dict(time=b['time'],min=b['min'],max=b['max'])for b in c['emission']['bursts']],alpha=keys(gradient.get('alphas',[])),colors=[dict(time=k['key'],color=k['value']+[1])for k in gradient.get('rgbs',[])],size=keys(sz.get('gradient',{}).get('sizes',[]))if so.get('enable')else[],sizeX=[],sizeY=[],sizeZ=[],tiles=sheet.get('tiles',[1,1]),frames=[],cycles=sheet.get('cycles',1),angularSpeed=0,shape=sh.get('shapeType',-1)if sh.get('enable')else-1,radius=sh.get('hemiSphereRadius',.01),coneAngle=sh.get('coneAngle',0),nativeShape=True,box=[],randomDirection=0,velocityMin=vel.get('constantMin',[])if vel.get('type')==2 else[],velocityMax=vel.get('constantMax',[])if vel.get('type')==2 else[],randomFrame=False)
        if c['renderMode']==4:l['mesh']=mesh(c['meshPath'])
        if c.get('threeDStartSize'):
            l['size3d']=c['startSizeConstantMaxSeparate']if c['startSizeType']==2 else c['startSizeConstantSeparate']
            l['size3dMin']=c['startSizeConstantMinSeparate']if c['startSizeType']==2 else l['size3d']
        if c.get('threeDStartRotation'):
            r=c['startRotationConstantMaxSeparate']if c['startRotationType']==2 else c['startRotationConstantSeparate'];l['rotation3d']=[-r[0],-r[1],r[2]]
            r=c['startRotationConstantMinSeparate']if c['startRotationType']==2 else c['startRotationConstantSeparate'];l['rotation3dMin']=[-r[0],-r[1],r[2]]
        layers.append(l)
    for child in n.get('child',[]):walk(child,node)
walk(source)
(OUT/'riichi-fire-effect.json').write_text(json.dumps(dict(nodes=nodes,layers=layers),ensure_ascii=False,indent=1)+'\n','utf-8')
audio=fetch('audio/audio_mj/effect_rich_fire.mp3');(R/'unity/Assets/Resources/Majsoul/Audio/effect_rich_fire.mp3').write_bytes(audio)
provenance=R/'unity/Assets/Resources/Majsoul/Audio/provenance.json';j=json.loads(provenance.read_text('utf-8'));j['clips']=[x for x in j['clips']if x['resource']!='Majsoul/Audio/effect_rich_fire']+[dict(resource='Majsoul/Audio/effect_rich_fire',source='https://game.maj-soul.com/1/v0.10.1.w/audio/audio_mj/effect_rich_fire.mp3',output='effect_rich_fire.mp3',sha256=hashlib.sha256(audio).hexdigest(),duration=1.104)];provenance.write_text(json.dumps(j,ensure_ascii=False,indent=2)+'\n','utf-8')
(OUT/'riichi-fire-source.json').write_text(json.dumps(dict(viewId=305022,audioId=236,apk='Majsoul_4.0.16_MC.apk',apkBundlesChecked=len(set(r['bundle']for r in rows)),note='Optional graph and cue absent from the base APK. Available texture assets prefer the APK; missing assets use the fixed official CDN version.',records=records),ensure_ascii=False,indent=2)+'\n','utf-8')
print('Imported',len(layers),'original particle layers,',len(exported),'textures and effect_rich_fire.mp3')
