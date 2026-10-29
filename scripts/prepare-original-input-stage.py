"""Make a small rendering checkout; immutable media use hard links, text/meta are copies."""
from pathlib import Path
import argparse,hashlib,json,os,shutil,uuid
R=Path(__file__).resolve().parent.parent
p=argparse.ArgumentParser();p.add_argument('--batch',choices=['original-input-r18','fate-r19','tsumo-r20'],default='original-input-r18');a=p.parse_args()
STAGE=R/'build'/a.batch/'stage';U=STAGE/'unity';U.mkdir(parents=True,exist_ok=True)
immutable={'.png','.jpg','.jpeg','.mp3','.wav','.ttf','.bytes'}
rows=[]
for folder in ['Assets','Packages','ProjectSettings']:
    for source in (R/'unity'/folder).rglob('*'):
        if not source.is_file():continue
        rel=source.relative_to(R/'unity')
        if rel.as_posix().startswith('Assets/CharacterPacks/Source/'):continue
        target=U/rel;target.parent.mkdir(parents=True,exist_ok=True)
        if target.exists():
            if source.suffix.lower() in immutable and os.path.samefile(source,target):continue
            if target.read_bytes()==source.read_bytes():continue
            target.unlink()
        if source.suffix.lower() in immutable:os.link(source,target);kind='hardlink-immutable-media'
        else:shutil.copy2(source,target);kind='copy'
        rows.append(dict(path=rel.as_posix(),kind=kind,bytes=source.stat().st_size))
for folder in ['build/unity-smoke','build/original-input-r18/fixtures']:
    target=STAGE/folder;target.mkdir(parents=True,exist_ok=True)
    for source in (R/folder).glob('*.json'):shutil.copy2(source,target/source.name)
for filename in ['engine.js','core.js']:
    target=STAGE/'app/src/main/assets'/filename;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(R/'app/src/main/assets'/filename,target)
(R/'build'/a.batch/'stage-inputs.json').write_text(json.dumps(rows,indent=1)+'\n','utf8')
print('Rendering project ready:',U,'updated',len(rows),'files; optional character source excluded; no APK build')
