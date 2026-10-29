"""Audit the pinned Fate prefab and shader inputs without running original code."""
from pathlib import Path
import json,re,sys,warnings
R=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(R/'tools/asset-inspector'))
import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
S=R/'dist/previews/2026-10-27-effects-reference/source';O=R/'build/fate-r19/source'
acquired=json.loads((O/'effect-bundles.json').read_text('utf8'))
index=json.loads((S/'bundle-index.json').read_text('utf8'))
files={r['file']for r in acquired+index if r['file'].endswith('.majset')and'error'not in r}
env=UnityPy.Environment(str(S/'webgl.data'),*[str(S/'bundles'/f)for f in files])
cab={o.assets_file.name:o.assets_file for o in env.objects};nodes=[];materials={};clips=[]
def resolve(f,p):
    if not p or not p['m_PathID']:return None
    if p['m_FileID']:
        name=Path(f.externals[p['m_FileID']-1].path).name
        if name not in cab:return dict(builtin=name,id=p['m_PathID'])
        f=cab[name]
    return f.objects[p['m_PathID']]
def walk(o,parent=-1):
    d=o.read_typetree();cs=[resolve(o.assets_file,c['component'])for c in d['m_Component']];tr=next(c for c in cs if c.type.name=='Transform');t=tr.read_typetree();index=len(nodes)
    row=dict(name=d['m_Name'],parent=parent,active=d['m_IsActive'],position=t['m_LocalPosition'],rotation=t['m_LocalRotation'],scale=t['m_LocalScale'],components=[]);nodes.append(row)
    for c in cs:
        if c.type.name=='Transform':continue
        data=c.read_typetree();brief=dict(type=c.type.name)
        for k in ['m_Enabled','m_Mesh','m_Controller','m_Script']:
            if k not in data:continue
            v=resolve(c.assets_file,data[k])if isinstance(data[k],dict)else data[k]
            brief[k]=v if isinstance(v,(dict,int,bool)) else dict(type=v.type.name,name=v.peek_name(),id=v.path_id)if v else None
        if 'm_Materials'in data:
            brief['materials']=[]
            for ptr in data['m_Materials']:
                mat=resolve(c.assets_file,ptr)
                if not mat:continue
                if isinstance(mat,dict):brief['materials'].append(mat);continue
                md=mat.read_typetree();shader=resolve(mat.assets_file,md['m_Shader']);sd=shader if isinstance(shader,dict)else shader.read_typetree()['m_ParsedForm'];brief['materials'].append(md['m_Name'])
                materials[md['m_Name']]=dict(shader=sd.get('m_Name',sd),properties=md['m_SavedProperties'],state=sd.get('m_SubShaders',[{}])[0].get('m_Passes',[{}])[0].get('m_State'))
        row['components'].append(brief)
    for ptr in t['m_Children']:
        child=resolve(tr.assets_file,ptr);walk(resolve(child.assets_file,child.read_typetree()['m_GameObject']),index)
for path,ptr in env.container.items():
    if 'ron_25hf' in path and path.endswith('.prefab'):walk(ptr.assetsfile.objects[ptr.path_id])
for o in env.objects:
    if o.type.name=='AnimationClip' and '079' in (o.peek_name()or''):
        d=o.read_typetree();clips.append(dict(name=d['m_Name'],binding=d.get('m_ClipBindingConstant'),floatCurves=d.get('m_FloatCurves'),editorCurves=d.get('m_EditorCurves')))
    if o.type.name=='Shader' and o.path_id==-6879380027099172515:
        from UnityPy.helpers import CompressionHelper
        d=o.read_typetree();folder=O/'shader';folder.mkdir(exist_ok=True)
        (folder/'parsed.json').write_text(json.dumps(d['m_ParsedForm'],indent=1),'utf8')
        for i,platform in enumerate(d['platforms']):
            offset=d['offsets'][i][0];size=d['compressedLengths'][i][0]
            raw=CompressionHelper.decompress_lz4(bytes(d['compressedBlob'])[offset:offset+size],d['decompressedLengths'][i][0])
            for j,match in enumerate(re.finditer(rb'[\x09\x0a\x0d\x20-\x7e]{400,}',raw)):
                if b'void main' in match.group():(folder/f'{platform}-{j}.glsl').write_bytes(match.group())
(O/'prefab-audit.json').write_text(json.dumps(dict(nodes=nodes,materials=materials,clips=clips),ensure_ascii=False,indent=1)+'\n','utf8')
for i,n in enumerate(nodes):print(i,n['parent'],n['name'],n['active'],[(c['type'],c.get('m_Enabled'),c.get('materials',c.get('m_Mesh')))for c in n['components']])
print('MATERIAL SHADERS',sorted(set(str(m['shader'])for m in materials.values())))
