"""Create a narrow, auditable cleanup plan and protected-file fingerprints."""
import hashlib,json,os,shutil,stat,subprocess
from pathlib import Path
R=Path(__file__).resolve().parent.parent
O=R/'build/cleanup-20261028';O.mkdir(parents=True,exist_ok=True)
ns={'__file__':str(R/'scripts/audit-project-cleanup.py')}
exec((R/'scripts/audit-project-cleanup.py').read_text('utf8').split('roots=')[0],ns)
def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for b in iter(lambda:f.read(4*1024*1024),b''):h.update(b)
    return h.hexdigest()
def source_fingerprint():
    rows=[]
    for name in ['unity/Assets','unity/Packages','unity/ProjectSettings','app/src','scripts','docs','tests','vendor']:
        for p in (R/name).rglob('*'):
            if p.is_file() and '__pycache__' not in p.parts:
                s=p.stat();rows.append((p.relative_to(R).as_posix(),s.st_size,s.st_mtime_ns))
    return hashlib.sha256(json.dumps(sorted(rows),separators=(',',':')).encode()).hexdigest(),len(rows)
dirs=[R/'unity/Library',R/'unity/Builds',R/'unity/Temp',R/'unity/Obj',
      Path('D:/MyWorkSpace/YunJianRiichi/unity/Library'),Path('D:/MyWorkSpace/YunJianRiichi/unity/Temp'),
      Path('C:/Users/Rtyu3221/.gradle/caches/9.3.1'),
      Path('C:/Users/Rtyu3221/AppData/Local/Temp/codex-riichi-build-82a62bbc1604')]
dirs=[p for p in dirs if p.exists()]
for p in dirs:
    assert p.resolve()==p,'Unexpected junction target '+str(p)
    assert not p.stat().st_file_attributes&stat.FILE_ATTRIBUTE_REPARSE_POINT
files=[]
for p in (R/'build').rglob('*.apk'):
    if not p.is_file() or any(s in p.name.lower() for s in ['majsoul','雀魂']):continue
    parents=list(p.parents);parents=parents[:parents.index(R)]
    if any((d/'.git').exists() for d in parents):continue
    rel=p.relative_to(R).as_posix()
    tracked=subprocess.check_output(['git','ls-files','--',rel],cwd=R,text=True).strip()
    assert not tracked,rel
    files.append(p)
protected=[R/'dist/yunjian-riichi-unity-0.6.17.apk',R/'dist/yunjian-riichi-unity-0.6.16.apk',R/'dist/Majsoul_4.0.16_MC.apk']
fingerprint,count=source_fingerprint()
plan=dict(project=str(R),directories=[ns['size'](p) for p in dirs],files=[dict(path=str(p),bytes=p.stat().st_size) for p in files],
          protected=[dict(path=str(p),bytes=p.stat().st_size,sha256=sha(p)) for p in protected],sourceFingerprint=fingerprint,sourceFiles=count,
          freeBefore={d:shutil.disk_usage(d+'/').free for d in ['C:','D:']},
          preserved=['All Assets, Packages and ProjectSettings','Source and documentation','Latest and previous distributed APKs','Original Majsoul APK and Lua research','Local source backups and release archives','Final preview galleries/videos','Signing key and downloaded SDK/Gradle dependencies'])
(O/'plan.json').write_text(json.dumps(plan,ensure_ascii=False,indent=2),'utf8')
for x in plan['directories']:print(f"DIR {x['bytes']/1073741824:.3f} GiB {x['path']}")
print('Temporary APK files',len(files),sum(x['bytes']for x in plan['files'])/1073741824,'GiB')
print('Protected source files',count,'; protected original/latest/previous APK SHA-256 recorded')
