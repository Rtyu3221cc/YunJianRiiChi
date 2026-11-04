"""Recover the 24 standard pool preview effects from the observed WebGL manifest.

The source is the running 4.0.51 client, not a guessed Laya scene name.
"""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import ast, gzip, hashlib, io, json, runpy, sys, time, traceback, urllib.request, warnings

R = Path(__file__).resolve().parent.parent
REF = R/'dist/previews/2026-10-27-effects-reference'
NATIVE = R/'unity/Assets/Resources/Majsoul/NativeReview'
OUT = NATIVE/'EffectsR11'
BATCH = 'EffectsR11'
AUDIT = REF/'source'
LOCAL_TARGETS = None
sys.path.insert(0, str(R/'tools/asset-inspector'))
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
UnityPy.config.FALLBACK_UNITY_VERSION = '2022.3.62f2c1'
warnings.filterwarnings('ignore')

WIN = ['ron_fire','ron_wind','ron_sakura','ron_heihuo','ron_huolongjuan',
       'ron_keji','ron_meiguihua','ron_ko','ron_nilin','ron_youlingaoao',
       'ron_23ex_heidong','ron_23ex_liandao','ron_23ex_shuizhu']
RIICHI = ['effect_liqi_bao','effect_liqi_ice','effect_liqi_fire','effect_liqi_canying',
          'effect_liqi_longteng','effect_liqi_ding','effect_liqi_23ex_maozhua',
          'effect_liqi_bianfujiejie','effect_liqi_feiyu','effect_liqi_23ex_liandao',
          'effect_liqi_23ex_xueqiu']
EXISTING = dict(ron_fire='ron-flame',ron_wind='ron-wind',ron_sakura='ron-sakura',
                ron_heihuo='ron-blackfire',ron_huolongjuan='ron-tornado',ron_keji='ron-reactor',
                effect_liqi_bao='riichi-blue',effect_liqi_ice='riichi-ice',
                effect_liqi_fire='riichi-fire',effect_liqi_canying='riichi-afterimage',
                effect_liqi_bianfujiejie='riichi-bat',effect_liqi_feiyu='riichi-feather')


def dump(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=1)+'\n', 'utf8')


def main():
    manifest = json.loads((R/'build/effects-native-manifest.json').read_text('utf8'))
    source_index = json.loads((REF/'source/bundle-index.json').read_text('utf8'))
    collect = runpy.run_path(str(R/'scripts/collect-effect-reference.py'))
    selected = set()
    selected.update(a['ownerBundleIndex'] for a in manifest['assetInfos']
                    if a['assetPath'].lower().startswith(('myassets/common3d/','myassets/scenes/mj/effect_mj/main/','myassets/scenes/mj/mjtable/')))
    targets = []
    for key in WIN+RIICHI:
        category = 'effect_hupai' if key in WIN else 'effect_lizhi'
        prefix = f'deco/{category}/{key}/'
        groups = [g for g in manifest['groupInfos'] if g['groupName'].startswith(prefix)]
        if not groups:
            raise ValueError('Missing source group '+key)
        selected.update(i for g in groups for i in g['bundlesIndex'])
        targets.append(dict(sourceKey=key, category='winEffect' if key in WIN else 'riichiEffect',
                            id=EXISTING.get(key,key.replace('_','-')), groups=[g['groupName'] for g in groups]))
    requested = [dict(url='https://game.maj-soul.com/assetbundles/DXT/'+manifest['bundleInfos'][i]['name']) for i in sorted(selected)]
    with ThreadPoolExecutor(max_workers=4) as pool:
        acquired = list(pool.map(collect['fetch'], requested))
    failed = [v for v in acquired if 'error' in v]
    dump(AUDIT/'effect-bundles.json', acquired)
    if failed:
        raise RuntimeError('Source bundles failed: '+str([r['file'] for r in failed]))
    files = {r['file'] for r in source_index+acquired if r['file'].endswith('.majset') and 'error' not in r}
    datafile = REF/'source/webgl.data'
    if not datafile.exists():
        dataurl=json.loads((REF/'source-session.json').read_text('utf8'))['webData']
        for attempt in range(3):
            try:
                with urllib.request.urlopen(dataurl,timeout=45) as response:raw=response.read()
                break
            except OSError:
                if attempt==2:raise
                time.sleep(1)
        datafile.write_bytes(gzip.decompress(raw) if raw.startswith(b'\x1f\x8b') else raw)
    local_bundles = {p.name:p for folder in (REF/'source/bundles',AUDIT/'bundles') for p in folder.glob('*.majset')} if LOCAL_TARGETS is not None else {f:REF/'source/bundles'/f for f in sorted(files)}
    env = UnityPy.Environment(str(datafile),*map(str,local_bundles.values()))
    if LOCAL_TARGETS is not None:
        targets = LOCAL_TARGETS
    expanded = BATCH in ('FateR19','WinsR22','WardrobeR30')
    cab = {o.assets_file.name:o.assets_file for o in env.objects}
    container = {p.lower():v for p,v in env.container.items()}
    records = []
    unsupported = []
    material_audit = []
    animation_sources = []
    schema = dict(R=R,json=json)
    code = ast.parse((R/'scripts/import-apk-review-assets.py').read_text('utf8'))
    exec(compile(ast.Module(body=[n for n in code.body if isinstance(n,ast.FunctionDef) and n.name=='editor_json'],type_ignores=[]),'<native-schema>','exec'),schema)

    def resolve(file, ptr):
        if not ptr or not ptr.get('m_PathID'):
            return None
        if ptr['m_FileID']:
            name = Path(file.externals[ptr['m_FileID']-1].path).name
            if name=='unity default resources':
                builtin={10202:'Cube',10206:'Cylinder',10207:'Sphere',10208:'Capsule',10209:'Plane',10210:'Quad'}.get(ptr['m_PathID'])
                if builtin:return dict(builtin=builtin,pathID=ptr['m_PathID'])
                raise ValueError('Unknown Unity builtin resource '+str(ptr['m_PathID']))
            if name not in cab:
                raise ValueError('Missing native dependency '+name)
            file = cab[name]
        return file.objects[ptr['m_PathID']]

    def save(relative, data, obj=None):
        path = NATIVE/relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
        records.append(dict(output=relative, sha256=hashlib.sha256(data).hexdigest(),
                            pathID=obj.path_id if obj else None, cab=obj.assets_file.name if obj else None))
        return relative

    def js(relative, data, obj=None):
        return save(relative,(json.dumps(data,ensure_ascii=False,indent=1)+'\n').encode('utf8'),obj)

    def vec(value):
        return [value[k] for k in ('xyzw' if 'w' in value else 'xyz' if 'z' in value else 'xy')]

    def graph(go, parent=-1, result=None):
        result = [] if result is None else result
        gd = go.read_typetree()
        components = [resolve(go.assets_file,c['component']) for c in gd['m_Component']]
        tr = next(c for c in components if c.type.name in ['Transform','RectTransform'])
        td = tr.read_typetree()
        index = len(result)
        result.append((go,td,components,parent))
        for ptr in td['m_Children']:
            child = resolve(tr.assets_file,ptr)
            graph(resolve(child.assets_file,child.read_typetree()['m_GameObject']),index,result)
        return result

    def material(obj):
        md = obj.read_typetree()
        props = md['m_SavedProperties']
        shader = resolve(obj.assets_file,md['m_Shader'])
        sd = shader.read_typetree()
        name = sd['m_ParsedForm']['m_Name']
        floats, colors, texenv = dict(props['m_Floats']), dict(props['m_Colors']), dict(props['m_TexEnvs'])
        main = texenv.get('_MainTex')
        if (not main or not main['m_Texture']['m_PathID']) and not expanded:
            raise ValueError('Missing main texture '+md['m_Name'])
        tex = resolve(obj.assets_file,main['m_Texture']) if main else None
        texdata = tex.read_typetree() if tex else {}
        texfile = f'{BATCH}/Textures/{tex.assets_file.name}-{tex.path_id}' if tex else f'{BATCH}/Textures/builtin-white'
        if not (NATIVE/(texfile+'.png')).exists():
            from PIL import Image
            buf = io.BytesIO(); (tex.read().image if tex else Image.new('RGBA',(2,2),'white')).save(buf,format='PNG');save(texfile+'.png',buf.getvalue(),tex)
        state = sd['m_ParsedForm']['m_SubShaders'][0]['m_Passes'][0]['m_State']
        blend = state['rtBlend0']
        src,dst = blend['srcBlend']['val'],blend['destBlend']['val']
        if LOCAL_TARGETS is not None:
            src=floats.get(blend['srcBlend']['name'],src)
            dst=floats.get(blend['destBlend']['name'],dst)
        if src==0 and dst==0:
            src,dst = floats.get('_SrcBlend',5),floats.get('_DstBlend',10)
        color = colors.get('_TintColor',dict(r=.5,g=.5,b=.5,a=.5))
        tint = [color[c] for c in 'rgba']
        if name=='FizzFX/Normal':
            color = colors.get('_MainColor',dict(r=1,g=1,b=1,a=1))
            tint = [color[c]*.5 for c in 'rgba'];tint[3] *= floats.get('_Alpha',1)
        active = [k for k,v in texenv.items() if v['m_Texture']['m_PathID']]
        if name not in ['Legacy Shaders/Particles/Additive','Legacy Shaders/Particles/Alpha Blended','Particles/Additive','Particles/Alpha Blended','Mobile/Particles/Additive','Catfood/Particles Additive ZTest Off','FizzFX/Normal'] or any(k not in ['_MainTex','_MaskTex'] for k in active):
            unsupported.append(dict(kind='material',name=md['m_Name'],shader=name,textures=active))
        material_audit.append(dict(effect=key,material=md['m_Name'],shader=name,floats=floats,colors=colors,activeTextures=active,renderState=state))
        settings = texdata.get('m_TextureSettings',{})
        extra=[]
        for prop,value in texenv.items():
            if not value['m_Texture']['m_PathID'] or prop=='_MainTex':continue
            image=resolve(obj.assets_file,value['m_Texture']);file=f'{BATCH}/Textures/{image.assets_file.name}-{image.path_id}'
            if not (NATIVE/(file+'.png')).exists():
                buf=io.BytesIO();image.read().image.save(buf,format='PNG');save(file+'.png',buf.getvalue(),image)
            es=image.read_typetree().get('m_TextureSettings',{})
            extra.append(dict(name=prop,texture=file,scale=vec(value['m_Scale']),offset=vec(value['m_Offset']),filterMode=es.get('m_FilterMode',1),wrapU=es.get('m_WrapU',0),wrapV=es.get('m_WrapV',0)))
        def state_value(field):
            value=state.get(field,{})
            return floats.get(value.get('name'),value.get('val',0))
        return dict(texture=texfile,tint=tint,srcBlend=src,dstBlend=dst,shaderName=name,
                    sourceMaterialState=True,materialName=md['m_Name'],materialBundle=str(obj.assets_file.parent.name),keywords=md.get('m_ValidKeywords',[]),
                    floats=[dict(name=k,value=v)for k,v in floats.items()if k!='<noninit>'],colors=[dict(name=k,value=[v[c]for c in 'rgba'])for k,v in colors.items()if k!='<noninit>'],extraTextures=extra,
                    textureScale=vec(main['m_Scale']) if main else [1,1],textureOffset=vec(main['m_Offset']) if main else [0,0],
                    uvScroll=[colors.get('_MainAnim',{}).get(c,0) for c in 'rg'],
                    filterMode=settings.get('m_FilterMode',1),wrapU=settings.get('m_WrapU',0),wrapV=settings.get('m_WrapV',0),
                    zTest=state_value('zTest'),zWrite=state_value('zWrite'),cull=state_value('culling'))

    def mesh_data(mesh,relative):
        if isinstance(mesh,dict) and mesh.get('builtin'):
            return js(relative,dict(builtin=mesh['builtin'],sourcePathID=mesh['pathID']))
        helper=MeshHandler(mesh.read());helper.process()
        data=dict(vertices=[n for v in helper.m_Vertices for n in v],uv=[n for v in helper.m_UV0 for n in v[:2]],indices=[i for sm in helper.get_triangles() for tri in sm for i in tri])
        if expanded and helper.m_Colors:data['colors']=[n for v in helper.m_Colors for n in v]
        if expanded:
            raw=mesh.read_typetree()
            if helper.m_BoneWeights and helper.m_BoneIndices:
                data['weights']=[n for v in helper.m_BoneWeights for n in v];data['boneIndices']=[int(n)for v in helper.m_BoneIndices for n in v]
                data['bindposes']=[matrix['e'+str(i)+str(j)]for matrix in raw['m_BindPose']for i in range(4)for j in range(4)]
        return js(relative,data,mesh)

    jobs=[]
    for target in targets:
        sourcekey=target['sourceKey'];category=target.get('group') or ('effect_hupai' if sourcekey in WIN else 'effect_lizhi')
        paths=sorted(p for p in container if p.startswith(f'myassets/deco/{category}/{sourcekey}/3d/prefab/')and p.endswith('.prefab'))
        exact=next((p for p in paths if Path(p).stem==sourcekey),None)
        if exact:
            paths=[exact]+([p for p in paths if p!=exact]if BATCH=='WinsR22' else [])
        elif len(paths)>1:
            target['nativeVariants']=[BATCH+'/'+sourcekey]+[BATCH+'/'+Path(p).stem for p in paths[1:]]
        target['allPrefabs']=paths
        if not paths:
            unsupported.append(dict(effect=sourcekey,kind='missing-prefab'))
            continue
        jobs.append((target,paths[0],sourcekey))
        for p in paths[1:]:jobs.append((dict(target),p,Path(p).stem))

    def export_job(target,main,key):
        paths=target['allPrefabs']
        ptr=container[main];root=resolve(ptr.assetsfile,dict(m_FileID=ptr.m_FileID,m_PathID=ptr.path_id))
        if root.type.name!='GameObject' and LOCAL_TARGETS is not None:
            root=next(o for o in root.assets_file.objects.values() if o.type.name=='GameObject' and o.peek_name()==Path(main).stem)
        hierarchy=graph(root)
        transform_indices={(go.assets_file.name,next(c for c in components if c.type.name in ['Transform','RectTransform']).path_id):i for i,(go,td,components,parent)in enumerate(hierarchy)}
        nodes=[];extra_materials={}
        for go,td,components,parent in hierarchy:
            node=dict(name=go.peek_name(),parent=parent,position=vec(td['m_LocalPosition']),rotation=vec(td['m_LocalRotation']),scale=vec(td['m_LocalScale']),active=bool(go.read_typetree()['m_IsActive']))
            for c in components:
                if LOCAL_TARGETS is not None and c.type.name=='MonoBehaviour':
                    custom=c.read_typetree()
                    if 'skeletonDataAsset' in custom and '_animationName' in custom:
                        skeleton=resolve(c.assets_file,custom['skeletonDataAsset']);data=skeleton.read_typetree()
                        atlas=resolve(skeleton.assets_file,data['atlasAssets'][0]);atlas_data=atlas.read_typetree()
                        def text_asset(obj):
                            value=obj.read().m_Script
                            return value.encode('utf8','surrogateescape') if isinstance(value,str) else value
                        stem=f'{BATCH}/Spine/{key}-{c.path_id}'
                        node.update(spineSkeleton=save(stem+'.bytes',text_asset(resolve(skeleton.assets_file,data['skeletonJSON']))),
                                    spineAtlas=save(stem+'.txt',text_asset(resolve(atlas.assets_file,atlas_data['atlasFile']))),
                                    spineTextures=[material(resolve(atlas.assets_file,p))['texture'] for p in atlas_data['materials']],
                                    spineScale=data['scale'],spineAnimation=custom['_animationName'],spineLoop=bool(custom['loop']),spineSpeed=custom['timeScale'])
                if c.type.name not in ['Transform','ParticleSystem','ParticleSystemRenderer','Animator','MeshFilter','MeshRenderer','SkinnedMeshRenderer']:
                    unsupported.append(dict(kind='component',effect=key,node=node['name'],type=c.type.name))
                if c.type.name=='Animator':
                    ad=c.read_typetree();controller=resolve(c.assets_file,ad['m_Controller'])
                    if controller:
                        cd=controller.read_typetree()
                        js(f'{BATCH}/Animations/{key}-{c.path_id}-controller.json',cd,controller)
                        node['animations']=[]
                        for clipref in cd.get('m_AnimationClips',[]):
                            clip=resolve(controller.assets_file,clipref)
                            if clip:
                                clipdata=clip.read_typetree()
                                relative=f'{BATCH}/Animations/{key}-{clip.path_id}'
                                node['animations'].append(js(relative+'.json',{'AnimationClip':clipdata},clip))
                                animation_sources.append(dict(bundle=str(clip.assets_file.parent.name),name=clipdata['m_Name'],output=relative+'.anim',effect=key,node=node['name']))
                                if expanded:
                                    bindings=clipdata.get('m_ClipBindingConstant',{}).get('genericBindings',[])
                                    if any(b.get('isPPtrCurve')for b in bindings):unsupported.append(dict(kind='animation-pointer-curves',effect=key,node=node['name'],clip=clipdata['m_Name'],bindings=[b for b in bindings if b.get('isPPtrCurve')]))
                                    for pointer in clipdata.get('m_ClipBindingConstant',{}).get('pptrCurveMapping',[]):
                                        value=resolve(clip.assets_file,pointer)
                                        if value is None:continue
                                        if value.type.name=='Material':
                                            relative=f'{BATCH}/Materials/{key}-{value.path_id}'
                                            extra_materials[value.peek_name()]=dict(name=value.peek_name(),source=js(relative+'-source.json',material(value),value),asset=relative+'.mat')
                                        else:unsupported.append(dict(kind='unmapped-animation-object',effect=key,type=value.type.name,name=value.peek_name()))
            particle=next((c for c in components if c.type.name=='ParticleSystem'),None)
            renderer=next((c for c in components if c.type.name=='ParticleSystemRenderer'),None)
            if particle and renderer:
                data=particle.read_typetree();data.pop('m_GameObject',None)
                relative=f'{BATCH}/Data/{key}-{particle.path_id}'
                rd=renderer.read_typetree()
                if LOCAL_TARGETS is not None and (not any(p['m_PathID'] for p in rd['m_Materials']) or rd['m_RenderMode']==4 and not any(rd.get(f,{}).get('m_PathID') for f in ('m_Mesh','m_Mesh1','m_Mesh2','m_Mesh3'))):
                    nodes.append(node)
                    continue
                node['particle']=js(relative+'.json',schema['editor_json']('ParticleSystem',data),particle)
                renderer_data={k:v for k,v in rd.items() if k not in ['m_GameObject','m_Materials','m_Mesh','m_Mesh1','m_Mesh2','m_Mesh3']}
                node['renderer']=js(relative+'-renderer.json',schema['editor_json']('ParticleSystemRenderer',renderer_data),renderer)
                mats=[material(resolve(renderer.assets_file,p)) for p in rd['m_Materials'] if p['m_PathID']]
                node.update(mats[0]);node['materials']=mats
                node.update(renderMode=rd['m_RenderMode'],sortingFudge=rd['m_SortingFudge'],lengthScale=rd['m_LengthScale'])
                if rd['m_RenderMode']==4:
                    meshes=[]
                    for field in ['m_Mesh','m_Mesh1','m_Mesh2','m_Mesh3']:
                        mesh=resolve(renderer.assets_file,rd.get(field))
                        if mesh is None:continue
                        mesh_id=mesh.get('pathID')if isinstance(mesh,dict)else mesh.path_id
                        meshes.append(mesh_data(mesh,relative+'-'+str(mesh_id)+'-mesh.json'))
                    node['mesh']=meshes[0];node['meshes']=meshes
                target.setdefault('particleLayers',0);target['particleLayers']+=1
            meshfilter=next((c for c in components if c.type.name=='MeshFilter'),None)
            static_renderer=next((c for c in components if c.type.name=='MeshRenderer'),None)
            if meshfilter and static_renderer and meshfilter.read_typetree()['m_Mesh']['m_PathID']:
                mesh=resolve(meshfilter.assets_file,meshfilter.read_typetree()['m_Mesh']);rd=static_renderer.read_typetree()
                mesh_id=mesh.get('pathID') if isinstance(mesh,dict) else mesh.path_id
                node['mesh']=mesh_data(mesh,f'{BATCH}/Data/{key}-{mesh_id}-static-mesh.json');node['staticMesh']=True
                node['materials']=[material(resolve(static_renderer.assets_file,p))for p in rd['m_Materials']if p['m_PathID']]
                node['sortingOrder']=rd.get('m_SortingOrder',0)
            skin=next((c for c in components if c.type.name=='SkinnedMeshRenderer'),None)
            if skin:
                rd=skin.read_typetree();mesh=resolve(skin.assets_file,rd['m_Mesh']);node['mesh']=mesh_data(mesh,f'{BATCH}/Data/{key}-{mesh.path_id}-skinned-mesh.json');node['skinnedMesh']=True
                def bone_index(ptr):
                    transform=resolve(skin.assets_file,ptr)
                    return transform_indices[(transform.assets_file.name,transform.path_id)] if transform else -1
                node['bones']=[bone_index(p)for p in rd['m_Bones']];node['rootBone']=bone_index(rd['m_RootBone']);node['materials']=[material(resolve(skin.assets_file,p))for p in rd['m_Materials']if p['m_PathID']];node['sortingOrder']=rd.get('m_SortingOrder',0)
            nodes.append(node)
        target['prefab']=main
        target['allPrefabs']=paths
        target['nativeEffect']=BATCH+'/'+key
        if key==target['sourceKey']:
            category=target.get('group') or ('effect_hupai' if key in WIN else 'effect_lizhi')
            icons=[p for p in container if p.startswith(f'myassets/deco/{category}/{key}/pic/')and p.endswith(('.png','.jpg'))]
            if target.get('iconSource','').lower() in container:icons=[target['iconSource'].lower()]
            if icons:
                iconptr=container[sorted(icons)[0]];iconobj=iconptr.assetsfile.objects[iconptr.path_id]
                buf=io.BytesIO();iconobj.read().image.save(buf,format='PNG');save(f'{BATCH}/Icons/{key}.png',buf.getvalue(),iconobj)
                target['icon']='Majsoul/NativeReview/'+BATCH+'/Icons/'+key
        js(f'{BATCH}/{key}-source.json',dict(nodes=nodes,sourceVersion='4.0.51',preserveActiveState=True,extraMaterials=list(extra_materials.values())),root)
        print('EXPORTED',key,target.get('particleLayers',0),'layers',flush=True)
    for target,main,key in jobs:
        try:
            export_job(target,main,key)
        except Exception as error:
            if LOCAL_TARGETS is None:raise
            unsupported.append(dict(effect=target['sourceKey'],kind='export-error',variant=key,error=str(error)))
            print('EXPORT ERROR',key,str(error),flush=True)
            traceback.print_exc()
    dump(OUT/'manifest.json',dict(clientVersion='4.0.51',resourceVersion='0.16.289',effects=targets,records=records,unsupported=unsupported))
    dump(AUDIT/'materials.json',material_audit)
    dump(AUDIT/'animation-sources.json',animation_sources)
    dump(AUDIT/'import-audit.json',dict(effects=targets,unsupported=unsupported))
    print('NATIVE EXPORT COMPLETE',len(targets),'effects;',len(unsupported),'items require review',flush=True)


if __name__=='__main__':
    if '--wardrobe-r30' in sys.argv:
        WIN=[];RIICHI=[];BATCH='WardrobeR30';AUDIT=R/'build/wardrobe-r30'
        NATIVE=AUDIT/'export';OUT=NATIVE/BATCH
        LOCAL_TARGETS=json.loads((AUDIT/'targets.json').read_text('utf8'))
    if '--fate-r19' in sys.argv:
        WIN=['ron_25hf'];RIICHI=[];EXISTING={'ron_25hf':'ron-fate-hf'}
        BATCH='FateR19';OUT=NATIVE/BATCH;AUDIT=R/'build/fate-r19/source';AUDIT.mkdir(parents=True,exist_ok=True)
    if '--wins-r22' in sys.argv:
        inventory=json.loads((R/'build/wardrobe-r22/source-inventory.json').read_text('utf8'))
        current=json.loads((R/'unity/Assets/Resources/Majsoul/Wardrobe/catalog.json').read_text('utf8'))
        existing={i['nativeEffect'].split('/')[-1]for i in current['items']if i.get('nativeEffect')and i['category']=='winEffect'and not i['nativeEffect'].startswith('WinsR22/')}
        WIN=sorted({r['sourceKey']for r in inventory['definitions']if r['sourceKey']and not r['expired']} - existing);RIICHI=[];EXISTING={}
        BATCH='WinsR22';OUT=NATIVE/BATCH;AUDIT=R/'build/wardrobe-r22/source';AUDIT.mkdir(parents=True,exist_ok=True)
        if (R/'docs/wardrobe-resource-layout-r22.json').exists():
            NATIVE=R/'build/wardrobe-r22/reexport/NativeReview';OUT=NATIVE/BATCH
            print('Split layout detected; exporting to isolated reexport folder, without replacing approved runtime assets:',OUT,flush=True)
    main()
