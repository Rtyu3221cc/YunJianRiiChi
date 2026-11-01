"""Retain the checked R22 import without overwriting current source code."""
from pathlib import Path
import hashlib,json,os,shutil
R=Path(__file__).resolve().parent.parent;B=R/'build/wardrobe-r22';U=B/'stage/unity'
assert 'WIN R22 IMPORT PASS' in (B/'unity-import-2.log').read_text('utf8',errors='replace')
assert not (B/'native-import-failures.txt').read_text('utf8').strip()
relative=Path('Assets/Resources/Majsoul/NativeReview/WinsR22');rows=[]
for source in (U/relative).rglob('*'):
    if not source.is_file():continue
    target=R/'unity'/source.relative_to(U);target.parent.mkdir(parents=True,exist_ok=True)
    # Media are hardlinks; shader/source edits in the maintained checkout win.
    if target.exists() and source.suffix.lower() in ['.shader','.cs']:continue
    if not (target.exists() and os.path.samefile(source,target)):shutil.copy2(source,target)
    rows.append(dict(path=target.relative_to(R).as_posix(),bytes=target.stat().st_size,sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
(B/'runtime-assets.json').write_text(json.dumps(rows,indent=1)+'\n','utf8')
print('RETAINED',len(rows),'R22 assets; 46 prefabs including fireworks auxiliary')
