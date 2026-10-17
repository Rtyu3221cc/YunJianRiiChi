"""Copy the two original APK particle graphs, materials, meshes and shaders."""
from pathlib import Path
import hashlib,io,json,sys,warnings,zipfile
R=Path(__file__).resolve().parent.parent;OUT=R/'unity/Assets/Resources/Majsoul/NativeReview'
sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
rows=[json.loads(l)for file in ['catalog.jsonl','fast-catalog.jsonl']for l in (R/'build/reference'/file).read_text('utf8').splitlines()]
cab=json.loads((R/'build/reference/tile-study/cab-map.json').read_text('utf8'));cache={};records=[]
z=zipfile.ZipFile(R/'dist/Majsoul_4.0.16_MC.apk')
def af(bundle):
    if bundle not in cache:cache[bundle]=next(o.assets_file for o in UnityPy.load(z.read(bundle)).objects)
    return cache[bundle]
def resolve(f,p):
    if not p['m_PathID']:return None
    if p['m_FileID']:f=af(cab[Path(f.externals[p['m_FileID']-1].path).name])
    return f.objects[p['m_PathID']]
def write(name,data,o=None):
    file=OUT/name;file.parent.mkdir(parents=True,exist_ok=True);file.write_bytes(data)
    record={'output':name,'sha256':hashlib.sha256(data).hexdigest()}
    if o:record.update(bundle=cab.get(o.assets_file.name),pathID=o.path_id,type=o.type.name)
    records.append(record);return name
def js(name,d,o=None):return write(name,(json.dumps(d,ensure_ascii=False,indent=2,default=str)+'\n').encode('utf8'),o)
def vec(v):return [v[k]for k in ('xyzw'if'w'in v else'xyz'if'z'in v else'xy')]
def editor_json(kind,data):
    schema=json.loads((R/'scripts/native-review'/('editor-particle-schema.json'if kind=='ParticleSystem'else'editor-renderer-schema.json')).read_text('utf8'))[kind]
    def merge(default,value):
        if isinstance(value,dict):
            if 'm_PathID'in value:return {'instanceID':0}
            result=dict(default)if isinstance(default,dict)else{}
            for k,v in value.items():result[k]=merge(result.get(k),v)
            # UnityPy omits versions on array elements; EditorJsonUtility needs
            # them, otherwise it silently imports a burst count of zero.
            if 'countCurve'in result and 'time'in result:result.setdefault('serializedVersion',2)
            elif 'scalar'in result and 'minMaxState'in result:result.setdefault('serializedVersion',3)
            elif 'm_Curve'in result:result.setdefault('serializedVersion',2)
            elif 'time'in result and 'inSlope'in result:result.setdefault('serializedVersion',3)
            return result
        if isinstance(value,list):
            template=default[0]if isinstance(default,list)and default else None
            return [merge(default[i]if isinstance(default,list)and i<len(default)else template,item)for i,item in enumerate(value)]
        return list(value)if isinstance(value,bytes)else value
    return {kind:merge(schema,data)}
for kind in ['jihui','queding']:
    name='effect_ui_yiman'+kind+'_chs';path='myassets/scenes/mj/effect_mj/locallize/chs/prefab/'+name+'.prefab'
    row=next(r for r in rows if path in r['paths']);f=af(row['bundle']);root=next(o for o in f.objects.values()if o.type.name=='GameObject'and o.peek_name().lower()==name);nodes=[]
    def walk(go,parent=-1):
        objs=[resolve(go.assets_file,p['component'])for p in go.read_typetree()['m_Component']];tr=next(o for o in objs if o.type.name=='Transform');td=tr.read_typetree();index=len(nodes)
        node={'name':go.peek_name(),'parent':parent,'position':vec(td['m_LocalPosition']),'rotation':vec(td['m_LocalRotation']),'scale':vec(td['m_LocalScale'])};nodes.append(node)
        p=next((o for o in objs if o.type.name=='ParticleSystem'),None)
        if p:
            data=p.read_typetree();data.pop('m_GameObject',None);js('Evidence/'+name+'-'+str(p.path_id)+'.json',data,p)
            node['particle']=js('Data/'+name+'-'+str(p.path_id)+'.json',editor_json('ParticleSystem',data),p)
            renderer=next(o for o in objs if o.type.name=='ParticleSystemRenderer');rd=renderer.read_typetree()
            node['renderer']=js('Data/'+name+'-'+str(renderer.path_id)+'-renderer.json',editor_json('ParticleSystemRenderer',{k:v for k,v in rd.items()if k not in ['m_GameObject','m_Materials','m_Mesh','m_Mesh1','m_Mesh2','m_Mesh3']}),renderer)
            mat=resolve(renderer.assets_file,rd['m_Materials'][0]);md=mat.read_typetree();props=md['m_SavedProperties'];colors=dict(props['m_Colors']);texs=dict(props['m_TexEnvs']);tex=resolve(mat.assets_file,texs['_MainTex']['m_Texture'])
            texname='Textures/yakuman-'+tex.peek_name();buf=io.BytesIO();tex.read().image.save(buf,format='PNG');write(texname+'.png',buf.getvalue(),tex)
            shader=resolve(mat.assets_file,md['m_Shader']);state=shader.read_typetree()['m_ParsedForm']['m_SubShaders'][0]['m_Passes'][0]['m_State']
            tint=colors.get('_TintColor',{'r':.5,'g':.5,'b':.5,'a':.5})
            node.update(texture=texname,tint=[tint[k]for k in 'rgba'],srcBlend=state['rtBlend0']['srcBlend']['val'],dstBlend=state['rtBlend0']['destBlend']['val'],renderMode=rd['m_RenderMode'],sortingFudge=rd['m_SortingFudge'],lengthScale=rd['m_LengthScale'])
            js('Evidence/'+name+'-'+str(mat.path_id)+'-material.json',md,mat)
            node['shaderBundle']='ShaderBundles/shader-'+str(shader.path_id);node['shaderName']=shader.read().m_ParsedForm.m_Name
            write(node['shaderBundle']+'.bytes',z.read(cab[shader.assets_file.name]),shader)
            if rd['m_RenderMode']==4:
                mesh=resolve(renderer.assets_file,rd['m_Mesh']);from UnityPy.helpers.MeshHelper import MeshHandler
                mh=MeshHandler(mesh.read());mh.process();node['mesh']=js('Meshes/yakuman-'+mesh.peek_name()+'.json',{'vertices':[v for xyz in mh.m_Vertices for v in xyz],'uv':[v for xy in mh.m_UV0 for v in xy],'indices':[i for sm in mh.get_triangles()for tri in sm for i in tri]},mesh)
        for cp in td['m_Children']:
            child=resolve(tr.assets_file,cp);walk(resolve(child.assets_file,child.read_typetree()['m_GameObject']),index)
    walk(root);js(name+'-source.json',{'nodes':nodes},root);print(name,'layers',sum(bool(n.get('particle'))for n in nodes))
for path in ['luabyte/lua/game/mj/viewplayer_me.lua.bytes','luabyte/lua/ui/ui_tingpai.lua.bytes']:
    row=next(r for r in rows if path in r['paths']);o=next(o for o in af(row['bundle']).objects.values()if o.type.name=='TextAsset');raw=o.read().m_Script
    write('Evidence/'+Path(path).name,raw.encode('utf8','surrogateescape')if isinstance(raw,str)else bytes(raw),o)
js('yakuman-provenance.json',{'apk':'Majsoul_4.0.16_MC.apk','assets':records,'note':'Particle graph code and serialized modules are copied in full. APK Lua remains encrypted evidence; offline scoring and triggers are project adapters.'})
