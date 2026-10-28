"""Recover the 24 standard pool preview effects from the observed WebGL manifest.

The source is the running 4.0.51 client, not a guessed Laya scene name.
"""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import ast, gzip, hashlib, io, json, runpy, sys, time, urllib.request, warnings

R = Path(__file__).resolve().parent.parent
REF = R/'dist/previews/2026-10-27-effects-reference'
NATIVE = R/'unity/Assets/Resources/Majsoul/NativeReview'
OUT = NATIVE/'EffectsR11'
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
    dump(REF/'source/effect-bundles.json', acquired)
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
    env = UnityPy.Environment(str(datafile),*[str(REF/'source/bundles'/f) for f in sorted(files)])
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
        if not main or not main['m_Texture']['m_PathID']:
            raise ValueError('Missing main texture '+md['m_Name'])
        tex = resolve(obj.assets_file,main['m_Texture'])
        texdata = tex.read_typetree()
        texfile = f'EffectsR11/Textures/{tex.assets_file.name}-{tex.path_id}'
        if not (NATIVE/(texfile+'.png')).exists():
            buf = io.BytesIO(); tex.read().image.save(buf,format='PNG');save(texfile+'.png',buf.getvalue(),tex)
        state = sd['m_ParsedForm']['m_SubShaders'][0]['m_Passes'][0]['m_State']
        blend = state['rtBlend0']
        src,dst = blend['srcBlend']['val'],blend['destBlend']['val']
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
            image=resolve(obj.assets_file,value['m_Texture']);file=f'EffectsR11/Textures/{image.assets_file.name}-{image.path_id}'
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
                    textureScale=vec(main['m_Scale']),textureOffset=vec(main['m_Offset']),
                    uvScroll=[colors.get('_MainAnim',{}).get(c,0) for c in 'rg'],
                    filterMode=settings.get('m_FilterMode',1),wrapU=settings.get('m_WrapU',0),wrapV=settings.get('m_WrapV',0),
                    zTest=state_value('zTest'),zWrite=state_value('zWrite'),cull=state_value('culling'))

    def mesh_data(mesh,relative):
        helper=MeshHandler(mesh.read());helper.process()
        return js(relative,dict(vertices=[n for v in helper.m_Vertices for n in v],uv=[n for v in helper.m_UV0 for n in v[:2]],indices=[i for sm in helper.get_triangles() for tri in sm for i in tri]),mesh)

    jobs=[]
    for target in targets:
        category='effect_hupai'if target['sourceKey']in WIN else'effect_lizhi';sourcekey=target['sourceKey']
        paths=sorted(p for p in container if p.startswith(f'myassets/deco/{category}/{sourcekey}/3d/prefab/')and p.endswith('.prefab'))
        exact=next((p for p in paths if Path(p).stem==sourcekey),None)
        if exact:paths=[exact]
        elif len(paths)>1:
            target['nativeVariants']=['EffectsR11/'+sourcekey]+['EffectsR11/'+Path(p).stem for p in paths[1:]]
        target['allPrefabs']=paths
        jobs.append((target,paths[0],sourcekey))
        for p in paths[1:]:jobs.append((dict(target),p,Path(p).stem))

    for target,main,key in jobs:
        paths=target['allPrefabs']
        ptr=container[main];root=ptr.assetsfile.objects[ptr.path_id]
        nodes=[]
        for go,td,components,parent in graph(root):
            node=dict(name=go.peek_name(),parent=parent,position=vec(td['m_LocalPosition']),rotation=vec(td['m_LocalRotation']),scale=vec(td['m_LocalScale']),active=bool(go.read_typetree()['m_IsActive']))
            for c in components:
                if c.type.name not in ['Transform','ParticleSystem','ParticleSystemRenderer','Animator','MeshFilter','MeshRenderer']:
                    unsupported.append(dict(kind='component',effect=key,node=node['name'],type=c.type.name))
                if c.type.name=='Animator':
                    ad=c.read_typetree();controller=resolve(c.assets_file,ad['m_Controller'])
                    if controller:
                        cd=controller.read_typetree()
                        js(f'EffectsR11/Animations/{key}-{c.path_id}-controller.json',cd,controller)
                        node['animations']=[]
                        for clipref in cd.get('m_AnimationClips',[]):
                            clip=resolve(controller.assets_file,clipref)
                            if clip:
                                clipdata=clip.read_typetree()
                                relative=f'EffectsR11/Animations/{key}-{clip.path_id}'
                                node['animations'].append(js(relative+'.json',{'AnimationClip':clipdata},clip))
                                animation_sources.append(dict(bundle=str(clip.assets_file.parent.name),name=clipdata['m_Name'],output=relative+'.anim',effect=key,node=node['name']))
            particle=next((c for c in components if c.type.name=='ParticleSystem'),None)
            renderer=next((c for c in components if c.type.name=='ParticleSystemRenderer'),None)
            if particle and renderer:
                data=particle.read_typetree();data.pop('m_GameObject',None)
                relative=f'EffectsR11/Data/{key}-{particle.path_id}'
                node['particle']=js(relative+'.json',schema['editor_json']('ParticleSystem',data),particle)
                rd=renderer.read_typetree()
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
                        meshes.append(mesh_data(mesh,relative+'-'+str(mesh.path_id)+'-mesh.json'))
                    node['mesh']=meshes[0];node['meshes']=meshes
                target.setdefault('particleLayers',0);target['particleLayers']+=1
            meshfilter=next((c for c in components if c.type.name=='MeshFilter'),None)
            static_renderer=next((c for c in components if c.type.name=='MeshRenderer'),None)
            if meshfilter and static_renderer:
                mesh=resolve(meshfilter.assets_file,meshfilter.read_typetree()['m_Mesh']);rd=static_renderer.read_typetree()
                node['mesh']=mesh_data(mesh,f'EffectsR11/Data/{key}-{mesh.path_id}-static-mesh.json');node['staticMesh']=True
                node['materials']=[material(resolve(static_renderer.assets_file,p))for p in rd['m_Materials']if p['m_PathID']]
                node['sortingOrder']=rd.get('m_SortingOrder',0)
            nodes.append(node)
        target['prefab']=main
        target['allPrefabs']=paths
        target['nativeEffect']='EffectsR11/'+key
        if key==target['sourceKey']:
            icons=[p for p in container if p.startswith(f'myassets/deco/{"effect_hupai"if key in WIN else"effect_lizhi"}/{key}/pic/common/')and p.endswith(('.png','.jpg'))]
            if icons:
                iconptr=container[sorted(icons)[0]];iconobj=iconptr.assetsfile.objects[iconptr.path_id]
                buf=io.BytesIO();iconobj.read().image.save(buf,format='PNG');save(f'EffectsR11/Icons/{key}.png',buf.getvalue(),iconobj)
                target['icon']='Majsoul/NativeReview/EffectsR11/Icons/'+key
        js(f'EffectsR11/{key}-source.json',dict(nodes=nodes,sourceVersion='4.0.51',preserveActiveState=True),root)
        print('EXPORTED',key,target.get('particleLayers',0),'layers',flush=True)
    dump(OUT/'manifest.json',dict(clientVersion='4.0.51',resourceVersion='0.16.289',effects=targets,records=records,unsupported=unsupported))
    dump(REF/'source/materials.json',material_audit)
    dump(REF/'source/animation-sources.json',animation_sources)
    dump(REF/'source/import-audit.json',dict(effects=targets,unsupported=unsupported))
    print('NATIVE EXPORT COMPLETE',len(targets),'effects;',len(unsupported),'items require review',flush=True)


if __name__=='__main__':main()
