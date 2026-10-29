"""Read-only disk inventory for project-generated caches. Never follows reparse points."""
import argparse,collections,json,os,stat,time
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor

R=Path(__file__).resolve().parent.parent
O=R/'build/cleanup-20261028';O.mkdir(parents=True,exist_ok=True)
def size(path):
    total=count=0;errors=[];stack=[str(path)]
    if path.is_file():return dict(path=str(path),bytes=path.stat().st_size,files=1,errors=[])
    while stack:
        p=stack.pop()
        try:
            with os.scandir(p) as entries:
                for e in entries:
                    try:
                        s=e.stat(follow_symlinks=False)
                        if s.st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT:continue
                        if e.is_dir(follow_symlinks=False):stack.append(e.path)
                        elif e.is_file(follow_symlinks=False):total+=s.st_size;count+=1
                    except OSError as error:
                        if len(errors)<8:errors.append(str(error))
        except OSError as error:
            if len(errors)<8:errors.append(str(error))
    return dict(path=str(path),bytes=total,files=count,errors=errors)
def children(parent):
    if not parent.is_dir():return []
    return [p for p in parent.iterdir() if p.name not in ['.git'] and not p.is_symlink()]
roots=[R/'unity',R/'build',R/'dist',Path('D:/MyWorkSpace/YunJianRiichi/unity'),
       Path('C:/Users/Rtyu3221/AppData/Local/Temp/codex-riichi-build-82a62bbc1604'),
       Path('C:/Users/Rtyu3221/AppData/Local/Temp/YunJian'),
       Path('C:/Users/Rtyu3221/AppData/Local/Unity/cache'),
       Path('C:/Users/Rtyu3221/AppData/LocalLow/Unity/Caches')]
targets=[p for root in roots for p in children(root)]
with ThreadPoolExecutor(max_workers=4) as pool:rows=list(pool.map(size,targets))
rows.sort(key=lambda r:r['bytes'],reverse=True)
(O/'inventory.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),'utf8')
for r in rows[:45]:print(f"{r['bytes']/1073741824:8.3f} GiB {r['files']:7d} files {r['path']}"+(' [access errors]' if r['errors'] else ''),flush=True)
