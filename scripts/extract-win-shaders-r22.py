"""Recover primary GLES programs used by the native win-material profiles."""
from pathlib import Path
import json,re,sys,warnings
R=Path(__file__).resolve().parent.parent;S=R/'dist/previews/2026-10-27-effects-reference/source';O=R/'build/wardrobe-r22/shaders';O.mkdir(parents=True,exist_ok=True);sys.path.insert(0,str(R/'tools/asset-inspector'))
import UnityPy
from UnityPy.helpers import CompressionHelper
from UnityPy.streams import EndianBinaryReader
from UnityPy.export.ShaderConverter import ShaderProgram
UnityPy.config.FALLBACK_UNITY_VERSION='2022.3.62f2c1';warnings.filterwarnings('ignore')
env=UnityPy.load(str(S/'bundles/shaders_0efab441c9304a4cc568.majset'));wanted={'Catfood/Additive Transparent','Catfood/Particles Blend ZTest Off','FizzFX/Standard','FizzFX/Normal','Mobile/Particles/Additive'};rows=[]
for obj in env.objects:
    if obj.type.name!='Shader':continue
    d=obj.read_typetree();name=d['m_ParsedForm']['m_Name']
    if name not in wanted:continue
    key=name.replace('/','-').replace(' ','-');(O/(key+'-parsed.json')).write_text(json.dumps(d['m_ParsedForm'],indent=1),'utf8')
    for i,platform in enumerate(d['platforms']):
        if platform not in [5,9]:continue
        off=d['offsets'][i][0];size=d['compressedLengths'][i][0];raw=CompressionHelper.decompress_lz4(bytes(d['compressedBlob'])[off:off+size],d['decompressedLengths'][i][0]);program=ShaderProgram(EndianBinaryReader(raw,endian='<'),obj.version)
        for j,p in enumerate(program.m_SubPrograms):
            if b'void main'not in p.m_ProgramCode:continue
            keys=set(p.m_Keywords or[])
            if keys and keys!={'_MASKENABLE_ON'}:continue
            chunks=[m.group().decode('ascii')for m in re.finditer(rb'[\x09\x0a\x0d\x20-\x7e]{200,}',p.m_ProgramCode)if b'void main'in m.group()]
            file=key+'-'+str(platform)+'-'+str(j)+'.glsl';(O/file).write_text('\n'.join(chunks),'utf8');rows.append(dict(shader=name,platform=platform,keywords=sorted(keys),file=file))
(O/'programs.json').write_text(json.dumps(rows,indent=2),'utf8');print('RECOVERED',len(rows),'selected GLES programs',sorted({r['shader']for r in rows}))
