"""Two additional native APK indicators, without rerunning earlier catalog imports."""
from pathlib import Path
import ast,hashlib,json,re,runpy
R=Path(__file__).resolve().parent.parent
S=runpy.run_path(str(R/'scripts/study-characters-1018.py'))
native=R/'unity/Assets/Resources/Majsoul/NativeReview';records=[]
scope=dict(R=R,json=json)
tree=ast.parse((R/'scripts/import-apk-review-assets.py').read_text('utf8'))
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='editor_json'],type_ignores=[]),'<particle-schema>','exec'),scope)
ns=dict(R=R,S=S,native=native,records=records,scope=scope,json=json)
tree=ast.parse((R/'scripts/import-wardrobe-r2.py').read_text('utf8'))
for n in tree.body:
    if isinstance(n,ast.FunctionDef) and n.name in ['vec','graph','mesh_data','particle_graph']:exec(compile(ast.Module(body=[n],type_ignores=[]),'<native-callout-import>','exec'),ns)
catalogPath=R/'unity/Assets/Resources/Majsoul/Wardrobe/catalog.json';catalog=json.loads(catalogPath.read_text('utf8'))
paths=json.loads((R/'build/wardrobe-plan-1024/all-apk-paths.json').read_text('utf8'))
for key,title in [('effect_zhishi','鸣牌指示－普通引导'),('mpzs_23winter','鸣牌指示-众星捧月')]:
    path=f'myassets/deco/effect_mingpaizhishi/{key}/3d/prefab/{key}.prefab';ns['particle_graph'](path,key)
    sourcePath=native/(key+'-source.json');data=json.loads(sourcePath.read_text('utf8'))
    for node,(_,_,components,_) in zip(data['nodes'],ns['graph'](S['obj'](path,'GameObject'))):
        renderer=next((c for c in components if c.type.name=='ParticleSystemRenderer'),None)
        if renderer is None:continue
        material=S['resolve'](renderer.assets_file,renderer.read_typetree()['m_Materials'][0]).read_typetree();props=material['m_SavedProperties'];floats=dict(props['m_Floats']);colors=dict(props['m_Colors'])
        # A dynamic ShaderLab blend property has val=0 in the parsed shader.
        # Resolve it from this material, rather than emitting Blend Zero Zero.
        if node['srcBlend']==0 and node['dstBlend']==0:
            node['srcBlend']=floats.get('_SrcBlend',5);node['dstBlend']=floats.get('_DstBlend',10)
            if '_MainColor' in colors:
                color=colors['_MainColor'];node['tint']=[color[c]*.5 for c in 'rgba'];node['tint'][3]*=floats.get('_Alpha',1)
        if '_MainAnim' in colors:node['uvScroll']=[colors['_MainAnim']['r'],colors['_MainAnim']['g']]
        main=next((v for k,v in props['m_TexEnvs'] if k=='_MainTex'),None)
        if main:
            node['textureScale']=[main['m_Scale']['x'],main['m_Scale']['y']];node['textureOffset']=[main['m_Offset']['x'],main['m_Offset']['y']]
    sourcePath.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n','utf8')
    images=[p for p in paths if p.startswith(f'myassets/deco/effect_mingpaizhishi/{key}/pic/common/') and p.endswith(('.jpg','.png'))]
    iconPath=R/'unity/Assets/Resources/Majsoul/Wardrobe/Icons'/(key+'.png')
    icon='Majsoul/Wardrobe/UI/slot_effect_tip'
    if images:
        image=S['obj'](images[0],'Texture2D');image.read().image.save(iconPath,format='PNG');icon='Majsoul/Wardrobe/Icons/'+key
        records.append(dict(source=images[0],output='Icons/'+key+'.png',sha256=hashlib.sha256(iconPath.read_bytes()).hexdigest()))
    entry=next((i for i in catalog['items'] if i['id']==key),None)
    if entry:entry.update(name=title,icon=icon)
    else:catalog['items'].append(dict(id=key,category='callout',name=title,builtin=True,icon=icon,nativeEffect=key,duration=3.6))
catalogPath.write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n','utf8')
(catalogPath.parent/'provenance-r5.json').write_text(json.dumps(dict(apk='Majsoul_4.0.16_MC.apk',records=records),ensure_ascii=False,indent=2)+'\n','utf8')
ids={}
for item in catalog['items']:ids.setdefault(item['category'],[]).append(item['id'])
engine=R/'app/src/main/assets/engine.js';raw=engine.read_bytes();ending=b'\r\n' if raw.count(b'\r\n')>raw.count(b'\n')/2 else b'\n'
code=raw.decode('utf8').replace('\r\n','\n');code=re.sub(r'const WARDROBE_CATALOG=\{[^\n]+\};','const WARDROBE_CATALOG='+json.dumps(ids,separators=(',',':'))+';',code)
engine.write_bytes(code.encode('utf8').replace(b'\n',ending))
(R/'unity/Assets/Plugins/Android/riichi-host.androidlib/assets/engine.js').write_bytes(engine.read_bytes())
print('Imported two native indicator prefabs and icons')
