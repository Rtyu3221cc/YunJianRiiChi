"""Export 1011 review assets directly from the supplied Unity APK.
ParticleSystem serialized modules are kept whole, for EditorJsonUtility import.
Compiled shaders and encrypted Lua remain source evidence, not claimed source code.
"""
from pathlib import Path
import sys,json,zipfile,warnings,hashlib,re
R=Path(__file__).resolve().parent.parent;OUT=R/'unity/Assets/Resources/Majsoul/NativeReview';OUT.mkdir(exist_ok=True)
sys.path.insert(0,str(R/'tools/asset-inspector'));import UnityPy
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
rows=[json.loads(s)for file in ['catalog.jsonl','fast-catalog.jsonl']for s in(R/'build/reference'/file).read_text(encoding='utf8').splitlines()]
cabmap=json.loads((R/'build/reference/tile-study/cab-map.json').read_text(encoding='utf8'));cache={};records=[]
z=zipfile.ZipFile(R/'dist/Majsoul_4.0.16_MC.apk')
def af(bundle):
 if bundle not in cache:cache[bundle]=next(o.assets_file for o in UnityPy.load(z.read(bundle)).objects)
 return cache[bundle]
def resolve(file,ptr):
 if ptr['m_PathID']==0:return None
 if ptr['m_FileID']:file=af(cabmap[Path(file.externals[ptr['m_FileID']-1].path).name])
 return file.objects[ptr['m_PathID']]
def write(name,data,source=None):
 file=OUT/name;file.parent.mkdir(parents=True,exist_ok=True);file.write_bytes(data);rec={'output':name,'sha256':hashlib.sha256(data).hexdigest()}
 if source:rec.update(bundle=cabmap.get(source.assets_file.name),pathID=source.path_id,type=source.type.name)
 records.append(rec);return name
def js(name,value,source=None):return write(name,(json.dumps(value,ensure_ascii=False,indent=2,default=lambda x:list(x)if isinstance(x,bytes)else str(x))+'\n').encode('utf8'),source)
def editor_json(type_name,data):
 # EditorJsonUtility uses a component-name envelope and explicit nested versions.
 schema=json.loads((R/'scripts/native-review'/('editor-particle-schema.json'if type_name=='ParticleSystem'else'editor-renderer-schema.json')).read_text(encoding='utf8'))[type_name]
 def merge(default,value):
  if isinstance(value,dict):
   if 'm_PathID'in value:return {'instanceID':0}
   result=dict(default)if isinstance(default,dict)else {}
   for k,v in value.items():result[k]=merge(result.get(k),v)
   if 'countCurve'in result and 'time'in result:result.setdefault('serializedVersion',2)
   elif 'scalar'in result and 'minMaxState'in result:result.setdefault('serializedVersion',3)
   elif 'm_Curve'in result:result.setdefault('serializedVersion',2)
   elif 'time'in result and 'inSlope'in result:result.setdefault('serializedVersion',3)
   return result
  if isinstance(value,list):
   template=default[0]if isinstance(default,list)and default else None
   return [merge(default[i]if isinstance(default,list)and i<len(default)else template,item)for i,item in enumerate(value)]
  return list(value)if isinstance(value,bytes)else value
 return {type_name:merge(schema,data)}
def find(path):
 row=next(r for r in rows if path in r.get('paths',[]));file=af(row['bundle']);return next(o for o in file.objects.values()if o.type.name=='GameObject'and o.peek_name().lower()==Path(path).stem.lower())
def tree(root):
 result=[]
 def walk(go,parent=-1):
  gd=go.read_typetree();objs=[resolve(go.assets_file,c['component'])for c in gd['m_Component']];t=next(o for o in objs if o.type.name in ['Transform','RectTransform']);td=t.read_typetree();i=len(result);result.append((go,t,objs,parent))
  for p in td['m_Children']:
   tr=resolve(t.assets_file,p);walk(resolve(tr.assets_file,tr.read_typetree()['m_GameObject']),i)
 walk(root);return result
def vec(d):return [d[k]for k in ('xyzw'if'w'in d else 'xyz'if'z'in d else 'xy')]
for name in ['effect_yiman','effect_yiman2','effect_chipeng']:
 root=find('myassets/scenes/mj/effect_mj/'+('main'if name=='effect_chipeng'else'extend')+'/prefab/'+name+'.prefab');nodes=[]
 for go,t,objs,parent in tree(root):
  td=t.read_typetree();node={'name':go.peek_name(),'parent':parent,'position':vec(td['m_LocalPosition']),'rotation':vec(td['m_LocalRotation']),'scale':vec(td['m_LocalScale'])}
  particle=next((o for o in objs if o.type.name=='ParticleSystem'),None);renderer=next((o for o in objs if o.type.name=='ParticleSystemRenderer'),None)
  if particle:
   data=particle.read_typetree();data.pop('m_GameObject',None);js('Evidence/particle-'+str(particle.path_id)+'.json',data,particle);node['particle']=js('Data/'+name+'-'+str(particle.path_id)+'.json',editor_json('ParticleSystem',data),particle)
   rd=renderer.read_typetree();native_renderer={k:v for k,v in rd.items()if k not in ['m_GameObject','m_Materials','m_Mesh','m_Mesh1','m_Mesh2','m_Mesh3']};node['renderer']=js('Data/'+name+'-'+str(renderer.path_id)+'-renderer.json',editor_json('ParticleSystemRenderer',native_renderer),renderer);mat=resolve(renderer.assets_file,rd['m_Materials'][0]);md=mat.read_typetree();shader=resolve(mat.assets_file,md['m_Shader']);tex=resolve(mat.assets_file,md['m_SavedProperties']['m_TexEnvs'][0][1]['m_Texture']);texname='Textures/'+tex.peek_name()+'.png'
   import io
   buf=io.BytesIO();tex.read().image.save(buf,format='PNG');write(texname,buf.getvalue(),tex)
   props=md['m_SavedProperties'];floats=dict(props['m_Floats']);colors=dict(props['m_Colors']);node.update(texture=texname[:-4],tint=vec(dict(zip('xyzw',[colors.get('_TintColor',{'r':.5,'g':.5,'b':.5,'a':.5})[c]for c in 'rgba']))),srcBlend=floats.get('_SrcBlend',5),dstBlend=floats.get('_DstBlend',1),renderMode=rd['m_RenderMode'],sortingFudge=rd['m_SortingFudge'],lengthScale=rd['m_LengthScale'])
   state=shader.read_typetree()['m_ParsedForm']['m_SubShaders'][0]['m_Passes'][0]['m_State'];node['srcBlend']=state['rtBlend0']['srcBlend']['val'];node['dstBlend']=state['rtBlend0']['destBlend']['val'];js('Evidence/shader-'+str(shader.path_id)+'-state.json',state,shader)
   js('Evidence/'+name+'-'+go.peek_name()+'-material.json',md,mat);write('Evidence/shader-'+str(shader.path_id)+'.txt',shader.read().export().encode('utf8'),shader)
   # Keep Android shader bundle bytes; runtime can reuse its actual compiled shader.
   sb=cabmap[shader.assets_file.name];write('ShaderBundles/shader-'+str(shader.path_id)+'.bytes',z.read(sb),shader);node['shaderBundle']='ShaderBundles/shader-'+str(shader.path_id);node['shaderName']=shader.read().m_ParsedForm.m_Name
   if rd['m_RenderMode']==4:
    mesh=resolve(renderer.assets_file,rd['m_Mesh']);from UnityPy.helpers.MeshHelper import MeshHandler
    mh=MeshHandler(mesh.read());mh.process();node['mesh']=js('Meshes/'+mesh.peek_name()+'.json',{'vertices':[v for xyz in mh.m_Vertices for v in xyz],'uv':[v for xy in mh.m_UV0 for v in xy],'indices':[i for sm in mh.get_triangles()for tri in sm for i in tri]},mesh)
  nodes.append(node)
 js(name+'-source.json',{'nodes':nodes},root)
# Independent anchors and exact UI arrow transforms.
root=find('myassets/scenes/mj/mjtable/prefab/scene_mj.prefab');anchors=[]
for go,t,objs,parent in tree(root):
 if go.peek_name().startswith('babei_'):
  d=t.read_typetree();anchors.append({'name':go.peek_name(),'position':vec(d['m_LocalPosition']),'rotation':vec(d['m_LocalRotation']),'span':.03550000116229057})
js('north-anchors.json',{'anchors':sorted(anchors,key=lambda a:a['name'])},root)
root=find('myassets/ui/mj/main/prefab_output/ui_scorechange.prefab');arrows=[];cards=[]
for go,t,objs,parent in tree(root):
 d=t.read_typetree();n=go.peek_name()
 if re.fullmatch('[0-3]{2}',n):arrows.append({'name':n,'position':vec(d['m_AnchoredPosition']),'size':vec(d['m_SizeDelta']),'rotation':vec(d['m_LocalRotation']),'scale':vec(d['m_LocalScale'])})
 if n in ['player_0','player_1','player_2','player_3']:cards.append({'name':n,'position':vec(d['m_AnchoredPosition']),'size':vec(d['m_SizeDelta']),'scale':vec(d['m_LocalScale'])})
js('score-layout.json',{'arrows':arrows,'cards':cards},root)
root=find('myassets/ui/mj/main/prefab_output/ui_gameend.prefab');cards=[]
for go,t,objs,parent in tree(root):
 d=t.read_typetree();n=go.peek_name()
 if n in ['p0','p1','p2','p3']:cards.append({'name':n,'position':vec(d['m_AnchoredPosition']),'size':vec(d['m_SizeDelta']),'scale':vec(d['m_LocalScale'])})
js('gameend-layout.json',{'cards':cards},root)
# The source tile already contains the exact shine quad and UVs.
root=find('myassets/scenes/mj/mjtable/prefab/mjp_new.prefab')
for go,t,objs,parent in tree(root):
 if go.peek_name()!='dora':continue
 mf=next(o for o in objs if o.type.name=='MeshFilter');mesh=resolve(mf.assets_file,mf.read_typetree()['m_Mesh']);from UnityPy.helpers.MeshHelper import MeshHandler
 mh=MeshHandler(mesh.read());mh.process();td=t.read_typetree();js('dora-geometry.json',{'vertices':[v for xyz in mh.m_Vertices for v in xyz],'uv':[v for xy in mh.m_UV0 for v in xy],'indices':[i for sm in mh.get_triangles()for tri in sm for i in tri],'position':vec(td['m_LocalPosition']),'scale':vec(td['m_LocalScale'])},mesh);break
for row in rows:
 if not any(p.endswith('/show_num.anim')for p in row.get('paths',[])):continue
 for o in af(row['bundle']).objects.values():
  if o.type.name=='AnimationClip'and o.peek_name()=='show_num':js('rank-animation.json',o.read_typetree(),o);print('rank animation',list(o.read_typetree()))
names={'bg_end0','bg_end123','bg_end','myself','bg_rankchange','bg_rankchange_name','jiuzhongjiupai','bg_point'}
for row in rows:
 if not any('atlas_mj_main_common_'in p or 'atlas_mj_main_chs_0'in p for p in row.get('paths',[])):continue
 for o in af(row['bundle']).objects.values():
  if o.type.name=='Sprite'and o.peek_name()in names:
   import io
   buf=io.BytesIO();o.read().image.save(buf,format='PNG');write('Sprites/'+o.peek_name()+'.png',buf.getvalue(),o)
for row in rows:
 if not any(p.endswith('dora_shine.mat')or p.endswith('dora_shine.png')for p in row.get('paths',[])):continue
 for o in af(row['bundle']).objects.values():
  if o.type.name=='Texture2D'and o.peek_name().startswith('dora_shine'):
   import io
   buf=io.BytesIO();o.read().image.save(buf,format='PNG');write('Textures/'+o.peek_name()+'.png',buf.getvalue(),o)
  if o.type.name=='Material'and o.peek_name().startswith('dora_shine'):js('Evidence/'+o.peek_name()+'-material.json',o.read_typetree(),o)
for path in ['luabyte/lua/game/mj/viewpai.lua.bytes','luabyte/lua/game/mj/block_babei.lua.bytes','luabyte/lua/game/mj/viewplayer_me.lua.bytes','luabyte/lua/ui/ui_scorechange.lua.bytes','luabyte/lua/ui/ui_gameend.lua.bytes','luabyte/lua/ui/ui_win.lua.bytes']:
 row=next(r for r in rows if path in r.get('paths',[]));file=af(row['bundle']);env=file.environment;ptr=env.container[path];o=ptr.assetsfile.objects[ptr.path_id];d=o.read();raw=d.m_Script.encode('utf8','surrogateescape')if isinstance(d.m_Script,str)else bytes(d.m_Script);write('Evidence/'+Path(path).name,raw,o)
js('provenance.json',{'apk':'Majsoul_4.0.16_MC.apk','assets':records,'limitations':['Lua presentation scripts are encrypted and have not been decrypted. Encrypted originals retained as evidence only.','Shader export contains compiled properties, not original shader source. Android shader bundles retained; desktop/UI compositing uses the existing shader adapter.','UI flow, UV playback and hand-to-meld event timing are project adapters, not verbatim recovered APK scripts.']})
print('Exported native review',len(nodes),'nodes;',len(records),'source records;',len(arrows),'arrows')
