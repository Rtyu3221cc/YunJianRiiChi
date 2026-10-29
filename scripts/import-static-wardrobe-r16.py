"""Expand non-action decorations from the supplied APK and official source bundles.

Acquisition and export are restartable. Existing action effects and hand choices
are never regenerated. Only complete entries are added to the runtime catalog.
"""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import ast,collections,hashlib,io,json,re,runpy,sys,time,urllib.parse,urllib.request
from PIL import Image

R=Path(__file__).resolve().parent.parent
S=runpy.run_path(str(R/'scripts/study-characters-1018.py'))
from UnityPy.helpers.MeshHelper import MeshHandler
W=R/'unity/Assets/Resources/Majsoul/Wardrobe'
N=R/'unity/Assets/Resources/Majsoul/NativeReview'
B=R/'build/wardrobe-r16'; CACHE=B/'source';CACHE.mkdir(parents=True,exist_ok=True)
TABLES=json.loads((R/'build/wardrobe-r14/current-tables.json').read_text('utf8'))
MANIFEST=json.loads((R/'build/wardrobe-r14/manifest.json').read_text('utf8'))
INDEX=json.loads((R/'build/reference/public/resversion0.11.252.w.json').read_text('utf8'))['res']
VIEWS={v['id']:v for v in TABLES['item_definition/view']}
CATEGORY={0:'stick',4:'music',5:'headFrame',6:'cloth',7:'tileBack',8:'lobbyBackground',13:'tileFace'}
ASSETS={a['assetPath'].lower():a for a in MANIFEST['assetInfos']}
records=[];issues=[];sources=[];animations=[];model_entries=[];mappings=[]
only=set(sys.argv[sys.argv.index('--only')+1:]) if '--only' in sys.argv else set()
catalog=json.loads((W/'catalog.json').read_text('utf8'))
protected={x['id']:x for x in catalog['items'] if x['category'] in ['winEffect','riichiEffect','callout','hand']}
base_snapshot=B/'catalog-before.json'
if not base_snapshot.exists():base_snapshot.write_bytes((W/'catalog.json').read_bytes())
elif not only:catalog=json.loads(base_snapshot.read_text('utf8'))
if not only:
    # A later full static rebuild must retain any newer approved action entries.
    for id,item in protected.items():
        old=next((i for i,x in enumerate(catalog['items']) if x['id']==id),None)
        if old is None:catalog['items'].append(item)
        else:catalog['items'][old]=item
if only:
    previous=json.loads((W/'provenance-r16.json').read_text('utf8'))
    records=previous['records'];sources=previous['sources'];issues=[x for x in previous['issues'] if x.get('key') not in only]
    mappings=[x for x in previous['mappings'] if x['sourceKey'] not in only]
    model_entries=[x for x in json.loads((N/'StaticR16/manifest.json').read_text('utf8'))['effects'] if x['sourceKey'] not in only]
    animations=[x for x in json.loads((B/'animation-sources.json').read_text('utf8')) if x['effect'] not in only]

def dump(file,value):
    file.parent.mkdir(parents=True,exist_ok=True)
    file.write_text(json.dumps(value,ensure_ascii=False,indent=1)+'\n','utf8')
def sha(raw):return hashlib.sha256(raw).hexdigest()
def download(url,dest):
    if not dest.exists():
        for attempt in range(3):
            try:
                raw=urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'Mozilla/5.0'}),timeout=45).read()
                dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(raw);break
            except OSError:
                if attempt==2:raise
                time.sleep(1)
    raw=dest.read_bytes();sources.append(dict(url=url,cache=dest.relative_to(R).as_posix(),bytes=len(raw),sha256=sha(raw)))
    return raw
def web(path):
    if path not in INDEX:path=next((p for p in ['lang/base/'+path,'lang/chs/'+path,'lang/'+path] if p in INDEX),path)
    cached=[R/'build/wardrobe-r6-study/source'/path,R/'build/wardrobe-r4-study/source'/path,
            R/'build/wardrobe-reference-r2/cdn'/(sha(path.encode())[:16]+Path(path).suffix)]
    url='https://game.maj-soul.com/1/'+INDEX[path]['prefix']+'/'+urllib.parse.quote(path)
    for c in cached:
        if c.exists():
            raw=c.read_bytes();sources.append(dict(url=url,cache=c.relative_to(R).as_posix(),bytes=len(raw),sha256=sha(raw)));return raw
    return download(url,CACHE/'web'/path)
def native_file(index):
    entry=MANIFEST['bundleInfos'][index];name=entry['name'];dest=CACHE/'bundles'/name
    old=R/'dist/previews/2026-10-27-effects-reference/source/bundles'/name
    if old.exists():return old
    raw=download('https://game.maj-soul.com/assetbundles/DXT/'+name,dest)
    if not raw.startswith(b'UnityFS'):raise ValueError('Invalid native bundle '+name)
    if len(raw)!=entry['fileSize']:raise ValueError('Native bundle size mismatch '+name)
    return dest
native_cache={}
def native_obj(path):
    entry=ASSETS[path.lower()];i=entry['ownerBundleIndex']
    if i not in native_cache:
        native_cache[i]=S['UnityPy'].load(str(native_file(i)))
    env=native_cache[i];ptr=next(v for k,v in env.container.items() if k.lower()==path.lower())
    return ptr.assetsfile.objects[ptr.path_id]
def obj(path):
    path=path.lower()
    if path in S['paths']:return S['obj'](path)
    return native_obj(path)
def image_obj(o):
    if o.type.name=='Texture2D':return o.read().image
    if o.type.name=='Sprite':
        d=o.read_typetree();rect=d['m_Rect'];offset=d['m_RD']['textureRectOffset'];im=o.read().image
        canvas=Image.new('RGBA',(round(rect['width']),round(rect['height'])))
        canvas.paste(im,(round(offset['x']),round(rect['height']-offset['y']-im.height)))
        return canvas
    raise ValueError('Image has unexpected type '+o.type.name)
def save_image(im,relative,origin,object=None):
    file=W/(relative+'.png');file.parent.mkdir(parents=True,exist_ok=True);im.save(file)
    records.append(dict(output=file.relative_to(R).as_posix(),source=origin,pathID=object.path_id if object else None,
                        width=im.width,height=im.height,sha256=sha(file.read_bytes())))
    return 'Majsoul/Wardrobe/'+relative
def asset_image(path,relative):
    o=obj(path);return save_image(image_obj(o),relative,dict(kind='apk' if path.lower() in S['paths'] else 'native-bundle',path=path),o)
def web_image(path,relative):
    raw=web(path)
    try:im=Image.open(io.BytesIO(raw));im.load()
    except Exception:im=Image.open(io.BytesIO(bytes(x^0x49 for x in raw)));im.load()
    return save_image(im,relative,dict(kind='official-web',path=path))
def available(path):return path.lower() in S['paths'] or path.lower() in ASSETS
def key_for(x):
    key=VIEWS.get(x['id'],{}).get('res_name','')
    if x['type']==5:key=(key if key.startswith('headframe_') else 'headframe_'+key).lower()
    aliases={'liqi_wanshengjie':'liqi_xiaoemofanbian','mjp_youling':'mjp_tianrandaiyouling'}
    return aliases.get(key,key)
def paths_for(x,key):
    roots={0:'lizhibang',5:'head_frame',6:'tablecloth',7:'mjpai',8:'lobby_background',13:'mjpface'}
    pathkey='mjp_default' if x['type']==7 and key=='mjp_yellow' else key
    prefix='myassets/deco/'+roots[x['type']]+'/'+pathkey+'/'
    return sorted(set(p for p in list(S['paths'])+list(ASSETS) if p.startswith(prefix)))
def first(paths,term,extensions=('.png','.jpg')):
    return next((p for p in paths if term in p and p.endswith(extensions)),None)
def icon(x,key,paths):
    p=first(paths,'/pic/chs/') or first(paths,'/pic/common/')
    return asset_image(p,'R16/Icons/'+str(x['id'])) if p else web_image(x['icon'],'R16/Icons/'+str(x['id']))
def add(entry):
    old=next((i for i in catalog['items'] if i['id']==entry['id']),None)
    if old:old.update(entry)
    else:catalog['items'].append(entry)
def vec(v):return [v[k] for k in ('xyzw' if 'w' in v else 'xyz' if 'z' in v else 'xy')]
def resolve(file,ptr):
    if not ptr or not ptr.get('m_PathID'):return None
    if ptr['m_FileID']:
        name=Path(file.externals[ptr['m_FileID']-1].path).name
        if name in ['unity default resources','unity_builtin_extra']:
            if name not in native_cache:native_cache[name]=S['UnityPy'].load(S['z'].read('assets/bin/Data/'+('Resources/' if name=='unity_builtin_extra' else '')+name))
            return next(o.assets_file for o in native_cache[name].objects).objects[ptr['m_PathID']]
        if name in S['cab']:return S['resolve'](file,ptr)
        for env in native_cache.values():
            for o in env.objects:
                if o.assets_file.name==name:return o.assets_file.objects[ptr['m_PathID']]
        raise ValueError('Missing native dependency '+name)
    return file.objects[ptr['m_PathID']]
def graph(go,parent=-1,result=None):
    result=[] if result is None else result;gd=go.read_typetree()
    comps=[resolve(go.assets_file,c['component']) for c in gd['m_Component']]
    tr=next(c for c in comps if c.type.name in ['Transform','RectTransform']);td=tr.read_typetree();i=len(result)
    result.append((go,td,comps,parent))
    for ptr in td['m_Children']:
        c=resolve(tr.assets_file,ptr);graph(resolve(c.assets_file,c.read_typetree()['m_GameObject']),i,result)
    return result
def save_native(relative,data,object=None):
    file=N/relative;file.parent.mkdir(parents=True,exist_ok=True);file.write_bytes(data)
    records.append(dict(output=file.relative_to(R).as_posix(),source='apk-native-graph',pathID=object.path_id if object else None,sha256=sha(data)))
    return relative
def js(relative,value,object=None):return save_native(relative,(json.dumps(value,ensure_ascii=False,separators=(',',':'))+'\n').encode('utf8'),object)
def model_texture(o):
    key='StaticR16/Textures/'+o.assets_file.name+'-'+str(o.path_id)
    if not (N/(key+'.png')).exists():
        stream=io.BytesIO();image_obj(o).save(stream,format='PNG');save_native(key+'.png',stream.getvalue(),o)
    else:
        file=N/(key+'.png');records.append(dict(output=file.relative_to(R).as_posix(),source='apk-native-graph',pathID=o.path_id,sha256=sha(file.read_bytes())))
    return key
def mesh(o,relative):
    h=MeshHandler(o.read());h.process()
    return js(relative,dict(vertices=[n for v in h.m_Vertices for n in v],uv=[n for v in h.m_UV0 for n in v[:2]],
                            groups=[dict(indices=[i for tri in sm for i in tri]) for sm in h.get_triangles()],
                            indices=[i for sm in h.get_triangles() for tri in sm for i in tri]),o)
def material(o):
    d=o.read_typetree();p=d['m_SavedProperties'];shader=resolve(o.assets_file,d['m_Shader']).read_typetree()['m_ParsedForm'];name=shader['m_Name']
    floats,colors,tex=dict(p['m_Floats']),dict(p['m_Colors']),dict(p['m_TexEnvs']);main=tex.get('_MainTex')
    if not main or not main['m_Texture']['m_PathID']:raise ValueError('No main texture '+d['m_Name'])
    active=[k for k,v in tex.items() if v['m_Texture']['m_PathID']]
    supported=['Catfood/Simple Texture','Catfood/Additive Transparent','Catfood/Cartoon','Unlit/Texture','Unlit/Transparent','FizzFX/Normal','Legacy Shaders/Particles/Alpha Blended','Legacy Shaders/Particles/Additive','Mobile/Particles/Alpha Blended']
    if name not in supported or any(k not in ['_MainTex','_MaskTex','_DistortionTex','_AMBlightTex','_DissolveTex'] for k in active):raise ValueError('Unsupported source material '+name+str(active))
    t=resolve(o.assets_file,main['m_Texture']);settings=t.read_typetree().get('m_TextureSettings',{})
    state=shader['m_SubShaders'][0]['m_Passes'][0]['m_State'];blend=state['rtBlend0']
    def val(field,default):
        v=state.get(field,{});return floats.get(v.get('name'),v.get('val',default))
    src,dst=blend['srcBlend']['val'],blend['destBlend']['val']
    if src==0 and dst==0:src,dst=floats.get('_SrcBlend',5),floats.get('_DstBlend',10)
    if name=='Unlit/Transparent':src,dst=5,10
    extra=[]
    for prop,v in tex.items():
        if prop=='_MainTex' or not v['m_Texture']['m_PathID']:continue
        image=resolve(o.assets_file,v['m_Texture']);es=image.read_typetree().get('m_TextureSettings',{})
        extra.append(dict(name=prop,texture=model_texture(image),scale=vec(v['m_Scale']),offset=vec(v['m_Offset']),filterMode=es.get('m_FilterMode',1),wrapU=es.get('m_WrapU',0),wrapV=es.get('m_WrapV',0)))
    return dict(texture=model_texture(t),shaderName=name,materialName=d['m_Name'],floats=[dict(name=k,value=v) for k,v in floats.items() if k!='<noninit>'],
                colors=[dict(name=k,value=[v[c] for c in 'rgba']) for k,v in colors.items() if k!='<noninit>'],extraTextures=extra,
                textureScale=vec(main['m_Scale']),textureOffset=vec(main['m_Offset']),filterMode=settings.get('m_FilterMode',1),wrapU=settings.get('m_WrapU',0),wrapV=settings.get('m_WrapV',0),
                srcBlend=src,dstBlend=dst,zTest=val('zTest',4),zWrite=val('zWrite',1),cull=val('culling',2))

schema=dict(json=json,R=R)
code=ast.parse((R/'scripts/import-apk-review-assets.py').read_text('utf8'))
exec(compile(ast.Module(body=[n for n in code.body if isinstance(n,ast.FunctionDef) and n.name=='editor_json'],type_ignores=[]),'<native-schema>','exec'),schema)
def export_model(path,key):
    root=obj(path);nodes=[]
    for go,td,comps,parent in graph(root):
        n=dict(name=go.peek_name(),parent=parent,position=vec(td['m_LocalPosition']),rotation=vec(td['m_LocalRotation']),scale=vec(td['m_LocalScale']),active=bool(go.read_typetree()['m_IsActive']))
        for c in comps:
            if c.type.name not in ['Transform','RectTransform','MeshFilter','MeshRenderer','ParticleSystem','ParticleSystemRenderer','Animator','MeshCollider']:raise ValueError('Unsupported component '+c.type.name)
            if c.type.name=='Animator':
                controller=resolve(c.assets_file,c.read_typetree()['m_Controller']);n['animations']=[]
                if controller:
                    for p in controller.read_typetree().get('m_AnimationClips',[]):
                        clip=resolve(controller.assets_file,p)
                        if not clip:continue
                        relative='StaticR16/Animations/'+key+'-'+str(clip.path_id)
                        n['animations'].append(js(relative+'.json',{'AnimationClip':clip.read_typetree()},clip))
                        bundle=S['cab'].get(clip.assets_file.name)
                        if bundle:
                            file=CACHE/'bundles'/Path(bundle).name;file.parent.mkdir(parents=True,exist_ok=True)
                            if not file.exists():file.write_bytes(S['z'].read(bundle))
                        else:raise ValueError('Missing clip source bundle')
                        animations.append(dict(bundle=Path(bundle).name,name=clip.peek_name(),output=relative+'.anim',effect=key,node=n['name']))
        ps=next((c for c in comps if c.type.name=='ParticleSystem'),None);pr=next((c for c in comps if c.type.name=='ParticleSystemRenderer'),None)
        if ps and pr:
            d=ps.read_typetree();d.pop('m_GameObject',None);relative='StaticR16/Data/'+key+'-'+str(ps.path_id)
            n['particle']=js(relative+'.json',schema['editor_json']('ParticleSystem',d),ps);rd=pr.read_typetree()
            clean={k:v for k,v in rd.items() if k not in ['m_GameObject','m_Materials','m_Mesh','m_Mesh1','m_Mesh2','m_Mesh3']}
            n['renderer']=js(relative+'-renderer.json',schema['editor_json']('ParticleSystemRenderer',clean),pr)
            n.update(renderMode=rd['m_RenderMode'],materials=[material(resolve(pr.assets_file,p)) for p in rd['m_Materials'] if p['m_PathID']])
            if rd['m_RenderMode']==4:
                n['meshes']=[mesh(resolve(pr.assets_file,rd[field]),relative+'-'+field+'.json') for field in ['m_Mesh','m_Mesh1','m_Mesh2','m_Mesh3'] if rd.get(field,{}).get('m_PathID')];n['mesh']=n['meshes'][0]
        mf=next((c for c in comps if c.type.name=='MeshFilter'),None);mr=next((c for c in comps if c.type.name=='MeshRenderer'),None)
        if mf and mr:
            m=resolve(mf.assets_file,mf.read_typetree()['m_Mesh']);rd=mr.read_typetree()
            n.update(staticMesh=True,mesh=mesh(m,'StaticR16/Data/'+key+'-'+str(m.path_id)+'.json'),materials=[material(resolve(mr.assets_file,p)) for p in rd['m_Materials'] if p['m_PathID']],sortingOrder=rd.get('m_SortingOrder',0))
        nodes.append(n)
    js('StaticR16/'+key+'-source.json',dict(nodes=nodes,preserveActiveState=True),root)
    model_entries.append(dict(sourceKey=key,nativeEffect='StaticR16/'+key))
    return 'Majsoul/NativeReview/StaticR16/'+key

def run():
    definitions=[x for x in TABLES['item_definition/item'] if x.get('category')==5 and x['type'] in CATEGORY]
    if only:definitions=[x for x in definitions if key_for(x) in only]
    # Pre-acquire only the missing image bundles; all APK assets stay local.
    needed=set()
    for x in definitions:
        if x['type']==4:continue
        key=key_for(x)
        for p in paths_for(x,key):
            if p in S['paths']:continue
            if p.endswith(('.png','.jpg')) and any(s in p for s in ['/texture/','/pic/common/','/pic/chs/','/icon/common/','/preview/common/','/preview/chs/']):needed.add(ASSETS[p]['ownerBundleIndex'])
    print('Acquiring',len(needed),'missing image bundles',flush=True)
    with ThreadPoolExecutor(max_workers=4) as pool:list(pool.map(native_file,sorted(needed)))
    # Later client sticks absent from the base APK need their complete 3D groups.
    extra=set()
    for x in definitions:
        if x['type']!=0:continue
        key=key_for(x);paths=paths_for(x,key)
        prefab=next((p for p in paths if p.endswith('/'+key+'.prefab')),None) or first(paths,'/3d/prefab/',('.prefab',))
        if prefab and prefab not in S['paths']:
            for g in MANIFEST['groupInfos']:
                if g['groupName']=='deco/lizhibang/'+key+'/3d':
                    for field in ['bundlesIndex','extraBundlesIndex','downloadBundlesIndex']:extra.update(g[field])
    if extra:
        extra.add(0)
        extra.update(a['ownerBundleIndex'] for a in MANIFEST['assetInfos'] if a['assetPath'].lower().startswith('myassets/common3d/'))
        with ThreadPoolExecutor(max_workers=4) as pool:list(pool.map(native_file,sorted(extra)))
        for i in extra:
            if i not in native_cache:native_cache[i]=S['UnityPy'].load(str(native_file(i)))
    for x in definitions:
        key=key_for(x);typ=x['type'];category=CATEGORY[typ];paths=paths_for(x,key) if typ!=4 else []
        try:
            entry=dict(id=key if key else 'music-'+str(x['id']),category=category,name=x['name_chs'],builtin=True,sourceItemId=x['id'])
            # Retain existing IDs so old LAN snapshots and PlayerPrefs keep working.
            aliases={'tablecloth_chigua':'cloth-chigua','tablecloth_default':'cloth-default','tablecloth_navy':'cloth-default','mjp_yellow':'mjp_default','beijing_chunying':'background-yard','beijing_qiushen':'background-autumn'}
            entry['id']=aliases.get(key,entry['id'])
            if typ==0:
                prefab=next((p for p in paths if p.endswith('/'+key+'.prefab')),None) or first(paths,'/3d/prefab/',('.prefab',))
                if not prefab:raise ValueError('No original 3D prefab in supplied APK')
                entry.update(modelPrefab=export_model(prefab,key),duration=4)
            elif typ in [7,13]:
                root='myassets/deco/'+('mjpai' if typ==7 else 'mjpface')+'/'+key
                if typ==7 and key=='mjp_yellow':root='myassets/deco/mjpai/mjp_default'
                entry.update(hand=asset_image(root+'/3d/texture/hand.png','R16/Items/'+key+'/hand'),table=asset_image(root+'/3d/texture/mjp.png','R16/Items/'+key+'/table'))
                p=first(paths,'/preview/common/')
                if p:entry['preview']=asset_image(p,'R16/Items/'+key+'/preview')
                else:entry['preview']=web_image('myres2/mjpai/'+key+'/preview.png','R16/Items/'+key+'/preview')
            elif typ==5:
                p=first(paths,'/icon/chs/') or first(paths,'/icon/common/')
                if not p:raise ValueError('No original avatar frame')
                entry.update(texture=asset_image(p,'R16/Frames/'+key),avatarWindow=[54/320,54/320,212/320,212/320] if key!='headframe_default' else [0,0,1,1])
            elif typ==6:
                p=next((p for p in paths if '/3d/texture/' in p and Path(p).stem.lower() in ['table_dif','table','tablecloth','desktop']),None)
                if not p:raise ValueError('Animated multipart tablecloth requires its own renderer; excluded from static decoration batch')
                entry['texture']=asset_image(p,'R16/Cloth/'+key)
                p=first(paths,'/preview/chs/') or first(paths,'/preview/common/')
                if p:entry['preview']=asset_image(p,'R16/Preview/'+key)
                else:
                    try:entry['preview']=web_image('myres2/tablecloth/'+key+'/preview.jpg','R16/Preview/'+key)
                    except KeyError:entry['preview']=entry['texture']
            elif typ==8:
                # The full background is in the scene texture, not the square shop icon.
                prefix='myassets/extendres/background/lobby/'+key+'/'
                native=next((p for p in list(S['paths'])+list(ASSETS) if p.startswith(prefix) and p.endswith(('.jpg','.png'))),None)
                if native:entry['texture']=asset_image(native,'R16/Background/'+key)
                else:
                    p=next((p for p in ['scene/Assets/Resource/lobby/'+key+'.jpg','lang/scene/Assets/Resource/lobby/'+key+'.jpg'] if p in INDEX),None)
                    if not p:raise ValueError('No full-size original background')
                    entry['texture']=web_image(p,'R16/Background/'+key)
                prev=first(paths,'/preview/chs/') or first(paths,'/preview/common/')
                entry['preview']=asset_image(prev,'R16/Preview/'+key) if prev else web_image('myres2/lobby_bg/'+key+'.jpg','R16/Preview/'+key)
            elif typ==4:
                p='audio/'+x['sargs'][0];existing={'music/liqi1.mp3':'music-riichi','music/liqi_8bit.mp3':'music-8bit'}
                entry['id']=existing.get(x['sargs'][0],'music-'+str(x['id']))
                relative='R16/Audio/'+str(x['id']);file=W/(relative+'.mp3');file.parent.mkdir(parents=True,exist_ok=True);raw=web(p);file.write_bytes(raw)
                records.append(dict(output=file.relative_to(R).as_posix(),source=p,sha256=sha(raw)))
                entry['audio']='Majsoul/Wardrobe/'+relative
            try:entry['icon']=icon(x,key,paths)
            except KeyError:
                if typ!=6:raise
                entry['icon']=entry['texture']
            add(entry);mappings.append(dict(itemId=x['id'],runtimeId=entry['id'],category=category,sourceKey=key));print('IMPORTED',category,entry['id'],entry['name'],flush=True)
        except Exception as error:
            issues.append(dict(id=x['id'],name=x['name_chs'],key=key,category=category,reason=str(error)))
            print('INCOMPLETE',x['id'],key,str(error),flush=True)
    # Existing playlist UI already owns lobby/table music. Extend it in place.
    musicfile=R/'unity/Assets/Resources/Majsoul/Settings/music.json';music=json.loads(musicfile.read_text('utf8'));ids={x['id'] for x in music['tracks']}
    for x in json.loads((R/'build/wardrobe-r14/lobby-bgm.json').read_text('utf8')):
        if x['id'] in ids:continue
        try:
            path='audio/'+x['path'];raw=web(path);relative='R16/Audio/'+str(x['id']);file=W/(relative+'.mp3');file.parent.mkdir(parents=True,exist_ok=True);file.write_bytes(raw)
            records.append(dict(output=file.relative_to(R).as_posix(),source=path,sha256=sha(raw)))
            music['tracks'].append(dict(id=x['id'],name=x['name_chs'],resource='Majsoul/Wardrobe/'+relative,group=x['type']))
        except Exception as error:issues.append(dict(id=x['id'],name=x['name_chs'],category='backgroundMusic',reason=str(error)))
    for id,entry in protected.items():assert next(x for x in catalog['items'] if x['id']==id)==entry,'Protected action/hand modified'
    dump(W/'catalog.json',catalog);dump(musicfile,music)
    registry={}
    for x in catalog['items']:registry.setdefault(x['category'],[]).append(x['id'])
    engine=R/'app/src/main/assets/engine.js';raw=engine.read_bytes();ending=b'\r\n' if b'\r\n' in raw else b'\n';code=raw.decode('utf8').replace('\r\n','\n')
    code,count=re.subn(r'const WARDROBE_CATALOG=\{[^\n]+\};','const WARDROBE_CATALOG='+json.dumps(registry,separators=(',',':'))+';',code);assert count==1
    engine.write_bytes(code.encode('utf8').replace(b'\n',ending));(R/'unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js').write_bytes(engine.read_bytes())
    dump(N/'StaticR16/manifest.json',dict(effects=model_entries));dump(B/'animation-sources.json',animations)
    dump(W/'provenance-r16.json',dict(apk='Majsoul_4.0.16_MC.apk',sourceManifest='4.0.51 / 0.16.289',records=records,sources=sources,mappings=mappings,issues=issues,counts=dict(collections.Counter(x['category'] for x in catalog['items']))))
    print('COMPLETE',collections.Counter(x['category'] for x in catalog['items']),'issues',len(issues),flush=True)
if __name__=='__main__':run()
