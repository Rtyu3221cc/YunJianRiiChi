"""Recover native materials and readable GLES programs without guessing shader names."""
from pathlib import Path
import hashlib,json,re,runpy
R=Path(__file__).resolve().parent.parent;S=R/'build/wardrobe-r10-study';O=S/'native-materials';O.mkdir(parents=True,exist_ok=True)
source=runpy.run_path(str(R/'scripts/study-characters-1018.py'))
from UnityPy.helpers import CompressionHelper
records=[];programs=[];shader_done=set();profiles=[]
for effect in ['effect_zhishi','mpzs_shandian','mpzs_22event','mpzs_23winter']:
    root=source['obj'](next(p for p in source['paths']if p.endswith('/'+effect+'.prefab')),'GameObject');seen=set();current=json.loads((R/'unity/Assets/Resources/Majsoul/NativeReview'/(effect+'-source.json')).read_text('utf8'))
    def visit(go):
        gd=go.read_typetree();components=[source['resolve'](go.assets_file,p['component'])for p in gd['m_Component']];tr=next(o for o in components if o and o.type.name in ['Transform','RectTransform'])
        for renderer in components:
            if not renderer or renderer.type.name!='ParticleSystemRenderer':continue
            for ptr in renderer.read_typetree()['m_Materials']:
                material=source['resolve'](renderer.assets_file,ptr)
                if not material or(material.assets_file.name,material.path_id)in seen:continue
                seen.add((material.assets_file.name,material.path_id));data=material.read_typetree();shader=source['resolve'](material.assets_file,data['m_Shader']);sd=shader.read_typetree();name=sd.get('m_ParsedForm',{}).get('m_Name',shader.peek_name());saved=data['m_SavedProperties']
                key=effect+'-'+str(len(records));(O/(key+'.json')).write_text(json.dumps(data,ensure_ascii=False,indent=2),'utf8')
                nonempty=[]
                for slot,env in saved['m_TexEnvs']:
                    tex=source['resolve'](material.assets_file,env['m_Texture'])
                    if tex:nonempty.append(dict(slot=slot,name=tex.peek_name()))
                record=dict(effect=effect,node=gd['m_Name'],material=data['m_Name'],shader=name,sourceUnityVersion=str(material.assets_file.unity_version),floats=[v[0]for v in saved['m_Floats']],colors=[v[0]for v in saved['m_Colors']],textures=[v[0]for v in saved['m_TexEnvs']],activeTextures=nonempty,validKeywords=data.get('m_ValidKeywords',[]),shaderAssetFile=shader.assets_file.name);records.append(record)
                if name=='FizzFX/Normal':
                    colors=dict(saved['m_Colors']);floats=dict(saved['m_Floats']);c=colors['_MainColor'];a=floats['_Alpha'];multi=floats['_MainColorMulti'];expected=[c[k]*multi*.5 for k in ['r','g','b']]+[a*.5];node=next(n for n in current['nodes']if n['name']==gd['m_Name']and n.get('particle'));anim=colors['_MainAnim'];scroll=[anim['r'],anim['g']]
                    profiles.append(dict(effect=effect,node=gd['m_Name'],activeTextures=nonempty,colorAndAlphaMapped=all(abs(x-y)<1e-6 for x,y in zip(expected,node['tint'])),scrollSpeedMapped=node.get('uvScroll')==scroll,sourceTimeBasis='global shader _Time.y',currentTimeBasis='effect age',note='Matching saved values does not establish complete shader/pipeline equivalence.'))
                    shader_key=(shader.assets_file.name,shader.path_id)
                    if shader_key not in shader_done:
                        shader_done.add(shader_key);folder=S/'fizzfx-programs';folder.mkdir(exist_ok=True)
                        for i,platform in enumerate(sd['platforms']):
                            offset=sd['offsets'][i][0];size=sd['compressedLengths'][i][0];blob=CompressionHelper.decompress_lz4(bytes(sd['compressedBlob'])[offset:offset+size],sd['decompressedLengths'][i][0]);(folder/('platform-'+str(platform)+'.bin')).write_bytes(blob);index=0
                            for match in re.finditer(rb'[\x09\x0a\x0d\x20-\x7e]{400,}',blob):
                                code=match.group()
                                if b'void main'not in code:continue
                                target=folder/('platform-'+str(platform)+'-source-'+str(index)+'.glsl');target.write_bytes(code);programs.append(dict(platform=platform,file=target.relative_to(R).as_posix(),bytes=len(code),sha256=hashlib.sha256(code).hexdigest(),mainFunctions=code.count(b'void main')));index+=1
        for ptr in tr.read_typetree()['m_Children']:
            child=source['resolve'](tr.assets_file,ptr);visit(source['resolve'](child.assets_file,child.read_typetree()['m_GameObject']))
    visit(root)
(S/'native-material-audit.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),'utf8');(S/'native-active-profile-audit.json').write_text(json.dumps(profiles,ensure_ascii=False,indent=2),'utf8');(S/'fizzfx-programs/index.json').write_text(json.dumps(programs,indent=2),'utf8')
print('NATIVE EXTRACTION',len(records),'materials',len(programs),'GLES variants',len(profiles),'custom profiles; saved color/alpha/scroll mapping',all(p['colorAndAlphaMapped']and p['scrollSpeedMapped']for p in profiles))
