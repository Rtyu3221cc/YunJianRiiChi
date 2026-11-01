"""Apply the reviewed per-file layout to the maintained checkout, preserving GUIDs."""
from pathlib import Path
import json,shutil
R=Path(__file__).resolve().parent.parent;U=(R/'unity').resolve();B=R/'build/wardrobe-r22';S=B/'stage/unity'
plan=json.loads((B/'pack-plan.json').read_text('utf8'))
def checked(path):
 p=(U/path).resolve();assert p.is_relative_to(U/'Assets') and not p.is_symlink(),p;return p
assert all((S/m['to']).is_file() and not (S/m['from']).exists()for m in plan['moves'])
for row in plan['moves']:
 source=checked(row['from']);target=checked(row['to'])
 target.parent.mkdir(parents=True,exist_ok=True)
 if source.exists():
  assert not target.exists(),target
  source.rename(target)
  meta=Path(str(source)+'.meta')
  if meta.exists():meta.rename(Path(str(target)+'.meta'))
 else:assert target.is_file(),target
for a in plan['assets']:
 if a['path'].startswith('Assets/WardrobePacks/Markers/'):
  source=S/a['path'];target=checked(a['path']);target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
  if Path(str(source)+'.meta').exists():shutil.copy2(Path(str(source)+'.meta'),Path(str(target)+'.meta'))
for p in (S/'Assets/WardrobePacks').rglob('*.meta'):
 # Folder GUIDs are not recreated; file GUIDs were moved intact above.
 if p.with_suffix('').is_dir():
  target=checked(p.relative_to(S));target.parent.mkdir(parents=True,exist_ok=True)
  if not target.exists():shutil.copy2(p,target)
for p in (S/'Assets/Scripts').glob('*.cs.meta'):
 target=checked(p.relative_to(S))
 if not target.exists():shutil.copy2(p,target)
layout=dict(schema=1,moves=plan['moves'],base=[dict(category=d['category'],id=d['id'])for d in plan['spec']['defaults']],source='Unity AssetDatabase dependency closure, R22')
(R/'docs/wardrobe-resource-layout-r22.json').write_text(json.dumps(layout,ensure_ascii=False,indent=1)+'\n','utf8')
print('LAYOUT APPLIED',len(plan['moves']),'files; GUIDs retained; optional art outside Resources')
