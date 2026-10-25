"""Stage existing bundles with NTFS hard links; no network or duplicate media."""
import json
import os
from pathlib import Path
R=Path(__file__).resolve().parent.parent
root=R/'build/portrait-fix-068/installed'
root.mkdir(parents=True,exist_ok=True)
catalog=json.loads((R/'unity/Assets/Resources/Majsoul/CharacterDirectory/packs-windows.json').read_text('utf8'))
folders=['build/character-resources-all','build/character-resources-v1','build/character-pack-experiment','build/character-packs-cache']
for pack in catalog['packs']:
    if pack['builtin']:continue
    source=next((R/folder/'windows'/pack['file'] for folder in folders if (R/folder/'windows'/pack['file']).is_file()),None)
    assert source and source.stat().st_size==pack['bytes'],pack['key']
    target=root/pack['key']/(pack['sha256']+'.bundle')
    target.parent.mkdir(parents=True,exist_ok=True)
    if not target.exists():os.link(source,target)
    target.with_suffix('.bundle.ok').write_text(pack['sha256'],'utf8')
(root/'startup-seen').write_text('1','utf8')
print('Staged 120 existing optional bundles as hard links:',root)
