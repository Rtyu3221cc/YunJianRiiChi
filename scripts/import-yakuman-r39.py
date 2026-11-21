"""Recover native yakuman scenes from the pinned reference APK, without network access.

Reuse the portable particle/mesh exporter; retain camera/UI source metadata in
addition to art and compiled animation inputs. Generated files stay in build.
"""
from pathlib import Path
from types import SimpleNamespace
import ast,collections,hashlib,io,json,runpy,sys,warnings

R=Path(__file__).resolve().parent.parent
O=R/'build/feedback-1114-r39/yakuman';O.mkdir(parents=True,exist_ok=True)
S=runpy.run_path(str(R/'scripts/study-characters-1018.py'))
sys.path.insert(0,str(R/'tools/asset-inspector'))
from UnityPy.helpers.MeshHelper import MeshHandler
base_resolve=S['resolve']
def resolve(file,ptr):
    if ptr and ptr.get('m_FileID') and Path(file.externals[ptr['m_FileID']-1].path).name=='unity default resources':
        builtin={10202:'Cube',10206:'Cylinder',10207:'Sphere',10208:'Capsule',10209:'Plane',10210:'Quad'}.get(ptr['m_PathID'])
        if builtin:return dict(builtin=builtin,pathID=ptr['m_PathID'])
        raise ValueError('Unknown builtin '+str(ptr))
    return base_resolve(file,ptr)
S['resolve']=resolve
schema=dict(R=R,json=json)
code=ast.parse((R/'scripts/import-apk-review-assets.py').read_text('utf8'))
exec(compile(ast.Module(body=[n for n in code.body if isinstance(n,ast.FunctionDef)and n.name=='editor_json'],type_ignores=[]),'<native-schema>','exec'),schema)
tree=ast.parse((R/'scripts/import-native-effects-r11.py').read_text('utf8'))
main=next(n for n in tree.body if isinstance(n,ast.FunctionDef)and n.name=='main')
names={'save','js','vec','graph','material','mesh_data','export_job'}
scope=dict(R=R,NATIVE=O/'export',BATCH='YakumanR39',LOCAL_TARGETS=[],expanded=True,resolve=resolve,Path=Path,io=io,json=json,hashlib=hashlib,MeshHandler=MeshHandler,schema=schema,records=[],unsupported=[],material_audit=[],animation_sources=[],WIN=[],RIICHI=[])
exec(compile(ast.Module(body=[n for n in main.body if isinstance(n,ast.FunctionDef)and n.name in names],type_ignores=[]),'<native-yakuman>','exec'),scope)
base_material=scope['material']
def material(obj):
    data=base_material(obj);raw=obj.read_typetree();data.update(hasSourceQueue=True,sourceQueue=raw['m_CustomRenderQueue'],sourceMaterialCab=obj.assets_file.name,sourceMaterialPathId=obj.path_id)
    if data['shaderName']=='Particles/Standard Unlit':
        values=dict(raw['m_SavedProperties']['m_Floats'])
        for field,prop,default in [('srcBlend','_SrcBlend',5),('dstBlend','_DstBlend',10),('zTest','_ZTest',4),('zWrite','_ZWrite',0),('cull','_Cull',2)]:data[field]=values.get(prop,default)
    return data
scope['material']=material
base_mesh_data=scope['mesh_data']
def mesh_data(mesh,relative):
    file=base_mesh_data(mesh,relative)
    if not isinstance(mesh,dict):
        helper=MeshHandler(mesh.read());helper.process();data=json.loads((scope['NATIVE']/file).read_text('utf8'))
        data['groups']=[dict(indices=[i for triangle in group for i in triangle])for group in helper.get_triangles()]
        (scope['NATIVE']/file).write_text(json.dumps(data,indent=1)+'\n','utf8')
    return file
scope['mesh_data']=mesh_data
paths=sorted(p for p in S['paths']if p.startswith('myassets/scenes/mj/effect_yiman/')and p.endswith('.prefab')and('/chs/'in p or '/main/'in p or p.endswith('/maque_outline.prefab')))
if '--one'in sys.argv:paths=[p for p in paths if p.endswith('/fan_dasanyuan.prefab')]
append='--append-outline'in sys.argv
if append:paths=['myassets/scenes/mj/effect_yiman/maquecommon/common/maque_outline.prefab']
effects=[]
for p in paths:
    key=Path(p).stem;scope['key']=key
    root=S['obj'](p,'GameObject');scope['container']={p:SimpleNamespace(assetsfile=root.assets_file,m_FileID=0,path_id=root.path_id)}
    target=dict(sourceKey=key,allPrefabs=[p]);scope['export_job'](target,p,key)
    source=O/'export/YakumanR39'/f'{key}-source.json';data=json.loads(source.read_text('utf8'));data['sourceVersion']='APK 4.0.16';extra=[]
    for i,(go,td,components,parent)in enumerate(scope['graph'](root)):
        custom=[]
        if 'm_SizeDelta'in td:data['nodes'][i]['rect']=dict(size=scope['vec'](td['m_SizeDelta']),anchorMin=scope['vec'](td['m_AnchorMin']),anchorMax=scope['vec'](td['m_AnchorMax']),pivot=scope['vec'](td['m_Pivot']))
        for c in components:
            if c.type.name=='Animator':
                controller=resolve(c.assets_file,c.read_typetree()['m_Controller'])
                if controller:
                    cd=controller.read_typetree();clips=cd.get('m_AnimationClips',[])
                    if len(clips)>1:
                        machine=cd['m_Controller']['m_StateMachineArray'][0]['data']
                        state=machine['m_StateConstantArray'][machine['m_DefaultState']]['data']
                        blend=state['m_BlendTreeConstantArray'][0]['data']['m_NodeArray']
                        ids=[x['data']['m_ClipID']for x in blend if not x['data']['m_ChildIndices']]
                        if len(ids)!=1 or state['m_TransitionConstantArray']:raise ValueError('Unsupported nontrivial yakuman controller '+key)
                        clip=resolve(controller.assets_file,clips[ids[0]])
                        data['nodes'][i]['animations']=[f'YakumanR39/Animations/{key}-{clip.path_id}.json']
                        data['nodes'][i]['defaultAnimation']=clip.peek_name()
            if c.type.name in ('Camera','Canvas','CanvasGroup'):
                custom.append(dict(type=c.type.name,data=c.read_typetree()))
            elif c.type.name=='MonoBehaviour':
                cd=c.read_typetree();sc=resolve(c.assets_file,cd['m_Script']);name=sc.peek_name()if sc else ''
                if name in ('Image','CanvasScaler','CImage'):custom.append(dict(type=name,data=cd))
        for c in custom:
            cd=c['data'];n=data['nodes'][i]
            if c['type']=='Camera':n['camera']=dict(fov=cd['field of view'],near=cd['near clip plane'],far=cd['far clip plane'],depth=cd['m_Depth'],enabled=bool(cd['m_CullingMask']['m_Bits']))
            elif c['type']=='Canvas':n['canvas']=True
            elif c['type']=='CanvasGroup':n['canvasGroup']=True;n['canvasAlpha']=cd['m_Alpha']
            elif c['type']=='Image':n['image']=True
        if custom:extra.append(dict(node=i,components=custom))
    data['cinematicComponents']=extra
    source.write_text(json.dumps(data,ensure_ascii=False,indent=1)+'\n','utf8');effects.append(target)
    # Give the source bundle files stable names. UnityPy's byte-reader names are
    # not necessarily archive entry names; identify the already resolved CAB.
    for row in scope['animation_sources']:
        if row.get('copied'):continue
        clipfile=next(f for f in S['cache'].values()if str(f.parent.name)==row['bundle']and any(o.type.name=='AnimationClip'and o.peek_name()==row['name']for o in f.objects.values()))
        bundle=next(b for b,f in S['cache'].items()if f is clipfile);dest=O/'source/bundles'/bundle
        dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(S['z'].read(bundle));row['bundle']=bundle;row['copied']=True
    print('METADATA',key,len(data['nodes']),len(extra),flush=True)
for row in scope['animation_sources']:row.pop('copied',None)
for record in scope['records']:record['sha256']=hashlib.sha256((scope['NATIVE']/record['output']).read_bytes()).hexdigest()
for file,data in [('manifest.json',dict(effects=effects,records=scope['records'],unsupported=scope['unsupported'])),('materials.json',scope['material_audit']),('animation-sources.json',scope['animation_sources'])]:
    if append and(O/file).exists():
        old=json.loads((O/file).read_text('utf8'))
        if isinstance(data,list):data=old+data
        else:
            data={key:old[key]+data[key]for key in data}
            for field,key in [('effects','sourceKey'),('records','output')]:data[field]=list({row[key]:row for row in data[field]}.values())
    (O/file).write_text(json.dumps(data,ensure_ascii=False,indent=1)+'\n','utf8')
(O/'source-file-audit.json').write_text(json.dumps(dict(referenceApk='dist/Majsoul_4.0.16_MC.apk',files=[dict(file=str(p.relative_to(O)),bytes=p.stat().st_size,sha256=hashlib.sha256(p.read_bytes()).hexdigest())for folder in (O/'export',O/'source/bundles')for p in sorted(folder.rglob('*'))if p.is_file()]),indent=1)+'\n','utf8')
print('COMPLETE',len(effects),'scenes',len(scope['records']),'outputs',len(scope['animation_sources']),'clips',flush=True)
