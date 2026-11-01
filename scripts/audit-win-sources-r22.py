"""Inspect all acquired native win prefabs before choosing import adapters."""
from pathlib import Path
import collections,json,sys,warnings
R=Path(__file__).resolve().parent.parent;B=R/'build/wardrobe-r22';S=R/'dist/previews/2026-10-27-effects-reference/source';sys.path.insert(0,str(R/'tools/asset-inspector'))
import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
inventory=json.loads((B/'source-inventory.json').read_text('utf8'));old=json.loads((S/'bundle-index.json').read_text('utf8'));names={r['file']for r in inventory['bundles']+old if r['file'].endswith('.majset')and'error'not in r}
env=UnityPy.Environment(str(S/'webgl.data'),*[str(S/'bundles'/n)for n in names]);cab={o.assets_file.name:o.assets_file for o in env.objects};shader_cache={};rows=[]
def resolve(f,p):
    if not p or not p.get('m_PathID'):return None
    if p['m_FileID']:
        name=Path(f.externals[p['m_FileID']-1].path).name
        if name not in cab:return dict(external=name,pathID=p['m_PathID'])
        f=cab[name]
    return f.objects[p['m_PathID']]
def shader_name(o):
    if isinstance(o,dict):return str(o)
    k=(o.assets_file.name,o.path_id)
    if k not in shader_cache:shader_cache[k]=o.read_typetree()['m_ParsedForm']['m_Name']
    return shader_cache[k]
for path,pointer in env.container.items():
    if not path.startswith('myassets/deco/effect_hupai/')or not path.endswith('.prefab'):continue
    root=pointer.assetsfile.objects[pointer.path_id];components=collections.Counter();shaders=collections.Counter();nodes=[];custom=[];clips=[];textures=set()
    def walk(go,parent=-1):
        gd=go.read_typetree();cs=[resolve(go.assets_file,c['component'])for c in gd['m_Component']];tr=next(c for c in cs if c.type.name=='Transform');td=tr.read_typetree();index=len(nodes);nodes.append(dict(name=gd['m_Name'],parent=parent,components=[c.type.name for c in cs]))
        for c in cs:
            components[c.type.name]+=1
            if c.type.name in ['MeshRenderer','ParticleSystemRenderer','SkinnedMeshRenderer']:
                for p in c.read_typetree()['m_Materials']:
                    m=resolve(c.assets_file,p)
                    if m is None:continue
                    md=m.read_typetree();sh=shader_name(resolve(m.assets_file,md['m_Shader']));shaders[sh]+=1
                    textures.update(k for k,v in md['m_SavedProperties']['m_TexEnvs']if v['m_Texture']['m_PathID'])
            if c.type.name=='MonoBehaviour':
                d=c.read_typetree();script=resolve(c.assets_file,d['m_Script']);custom.append(script.read_typetree().get('m_ClassName')if script is not None and not isinstance(script,dict)else str(script))
            if c.type.name=='Animator':
                d=c.read_typetree();controller=resolve(c.assets_file,d['m_Controller'])
                if controller is not None:
                    for ptr in controller.read_typetree().get('m_AnimationClips',[]):
                        clip=resolve(controller.assets_file,ptr)
                        if clip is not None:clips.append(clip.peek_name())
        for ptr in td['m_Children']:
            child=resolve(tr.assets_file,ptr);walk(resolve(child.assets_file,child.read_typetree()['m_GameObject']),index)
    error=None
    try:
        if root.type.name!='GameObject':
            data=root.read_typetree();ptr=data.get('m_RootGameObject')or data.get('m_GameObject')
            if ptr:root=resolve(root.assets_file,ptr)
        walk(root)
    except Exception as ex:error=dict(type=type(ex).__name__,message=str(ex),rootType=root.type.name,rootFields=list(root.read_typetree()))
    rows.append(dict(path=path,key=path.split('/')[3],components=dict(components),shaders=dict(shaders),customScripts=custom,nodes=nodes,clips=clips,activeTextureSlots=sorted(textures),error=error))
    print('AUDITED',path.split('/')[-1],dict(components),list(shaders),flush=True)
(B/'native-audit.json').write_text(json.dumps(dict(prefabs=rows,shaderNames=sorted(set(k for r in rows for k in r['shaders']))),ensure_ascii=False,indent=1)+'\n','utf8')
print('ALL WIN AUDIT COMPLETE',len(rows),'prefabs;',sorted(set(k for r in rows for k in r['components'])))
