"""Bring only this batch's checked generated assets back from the render project."""
from pathlib import Path
import hashlib,json,re,shutil
R=Path(__file__).resolve().parent.parent;B=R/'build/fate-r19';U=B/'stage/unity';N=Path('Assets/Resources/Majsoul/NativeReview/FateR19')
assert 'FATE R19 IMPORT PASS' in (B/'import-runtime.log').read_text('utf8',errors='replace')
rows=[]
for source in (U/N).rglob('*'):
    if not source.is_file():continue
    target=R/'unity'/source.relative_to(U);target.parent.mkdir(parents=True,exist_ok=True)
    if not (target.exists() and source.samefile(target)):shutil.copy2(source,target)
    rows.append(dict(path=target.relative_to(R).as_posix(),bytes=target.stat().st_size,sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
for rel in ['Assets/Resources/Majsoul/NativeReview/FateR19.meta','Assets/Resources/Majsoul/Actions/native-ron_25hf.json.meta','Assets/Resources/Majsoul/Wardrobe/provenance-r19.json.meta','Assets/Resources/Majsoul/Audio/ron_25hf.mp3.meta','Assets/Editor/FateR19Preview.cs.meta','Assets/Scripts/NativeWinningTileQueue.cs.meta']:
    source=U/rel
    if source.exists():shutil.copy2(source,R/'unity'/rel)
guid=re.search(r'^guid: ([a-f0-9]+)$',(R/'unity'/N/'FateParticle.shader.meta').read_text(),re.M)[1]
for settings in [R/'unity/ProjectSettings/GraphicsSettings.asset']:
    text=settings.read_text('utf8')
    if guid not in text:text=text.replace('  m_AlwaysIncludedShaders:\n','  m_AlwaysIncludedShaders:\n  - {fileID: 4800000, guid: '+guid+', type: 3}\n')
    settings.write_text(text,'utf8',newline='\n')
setup=R/'unity/Assets/Editor/ProjectSetup.cs';raw=setup.read_bytes()
if b'"YunJian/FateParticle"'not in raw:setup.write_bytes(raw.replace(b'"YunJian/NativeEffectParticle",',b'"YunJian/NativeEffectParticle", "YunJian/FateParticle",'))
(B/'runtime-assets.json').write_text(json.dumps(rows,indent=1)+'\n','utf8')
print('Retained',len(rows),'Fate files,',sum(r['bytes']for r in rows),'bytes; other effect batches untouched')
