"""Report native material gaps separately from resource and simulation completeness."""
from pathlib import Path
import hashlib,json,re,sys
R=Path(__file__).resolve().parent.parent;sys.path.insert(0,str(R/'scripts'));from effect_compatibility import unsupported_features
A=R/'unity/Assets/Resources/Majsoul/Actions';S=R/'build/wardrobe-r10-study';rows=[]
for path in sorted(A.glob('*.json')):
    converted=json.loads(path.read_text('utf8'))
    if not isinstance(converted,dict)or converted.get('schema')!=2 or not converted.get('layers'):continue
    name=converted['nodes'][0]['name'];raw=next((R/'build'/folder/'source/scene'/(name+'.lh')for folder in ['wardrobe-r6-study','wardrobe-r4-study']if(R/'build'/folder/'source/scene'/(name+'.lh')).is_file()),None)
    if raw is None:continue
    issues=unsupported_features(json.loads(raw.read_text('utf8')))
    for layer in converted['layers']:
        if layer.get('sizeX')and not layer.get('size'):issues.append((layer['name'],'separate-size-not-enabled-by-runtime-gate','size[] empty'))
        if layer.get('colors')and not layer.get('alpha'):issues.append((layer['name'],'rgb-gradient-not-enabled-by-runtime-gate','alpha[] empty'))
    rows.append(dict(asset=path.name,origin='custom-trial'if path.stem.startswith('callout-')else'web-port',sourceScene=name,sourceSHA256=hashlib.sha256(raw.read_bytes()).hexdigest(),layers=len(converted['layers']),unsupported=issues,status='unsupported-behavior'if issues else'parameters-imported-rendering-not-proven',remaining=['sampler state, shader implementation, owner binding and final visual equivalence require separate reference checks']))
materials=json.loads((S/'native-material-audit.json').read_text('utf8'));native=[]
for m in materials:
    native.append(dict(effect=m['effect'],node=m['node'],sourceShader=m['shader'],currentShader='YunJian/WinParticle',status='custom-shader-active-profile-requires-reference-check'if m['shader']=='FizzFX/Normal'else'legacy-material-family-requires-reference-check',textureSlots=m['textures']))
programs=[]
for path in sorted((S/'fizzfx-programs').glob('platform-*.bin')):
    blob=path.read_bytes();platform=int(path.stem.split('-')[-1])
    for i,m in enumerate(re.finditer(rb'[\x09\x0a\x0d\x20-\x7e]{400,}',blob)):
        code=m.group()
        if b'void main'not in code:continue
        target=path.parent/(path.stem+'-source-'+str(len([v for v in programs if v['platform']==platform]))+'.glsl');target.write_bytes(code)
        programs.append(dict(platform=platform,file=target.relative_to(R).as_posix(),bytes=len(code),sha256=hashlib.sha256(code).hexdigest(),mainFunctions=code.count(b'void main')))
(S/'fizzfx-programs/index.json').write_text(json.dumps(programs,indent=2),'utf8')
report=dict(assets=rows,nativeMaterials=native,nativeGLESPrograms=programs,originalAPKScriptStatus='IL2CPP and protected Lua; full native playback scheduling source not recovered',trialPolicy='Sakura/frost/vortex are cross-category adaptations, not verified original callout costumes')
(S/'fidelity-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n','utf8')
print('FIDELITY AUDIT',len(rows),'converted graphs;',len(native),'native materials;',sum(x['sourceShader']=='FizzFX/Normal'for x in native),'custom shader profiles;',len(programs),'recovered GLES variants')
print('UNSUPPORTED',[(v['asset'],len(v['unsupported']))for v in rows if v['unsupported']])
