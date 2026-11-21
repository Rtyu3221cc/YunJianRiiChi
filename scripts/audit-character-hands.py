"""Check the converted default hands against the previously extracted inventory."""
from pathlib import Path
import hashlib
import json
import math
import re

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT.parent / 'YunJianRiiChi_Tmp/Majsoul_角色手型與手部裝飾'
ASSETS = ROOT / 'unity/Assets/Resources/Majsoul/CharacterHands'
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
original = {h['res_name']: h for h in read(SOURCE / 'data/hands.json')}
catalog = read(ASSETS / 'catalog.json')['hands']
roles = {r['id']: r for r in read(ROOT / 'unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json')['roles']}
bindings = []
for hand in catalog:
    source = original[hand['key']]
    assert hand['sourceItemId'] == source['id']
    assert hand['characters'] == source['character_ids']
    assert len(hand['variants']) == len(source['prefabs'])
    for row in hand['actions']:
        timing = next(t for t in source['timing'] if t['name'] == row['action'])
        for actual, expected in [(row['speed'], timing['speed']), (row['lifetime'], timing['lifetime'] / 1000 / timing['speed']), (row['cue'], timing['keypoint'][0] / 1000 / timing['speed']), (row['release'], timing['keypoint'][-1] / 1000 / timing['speed'])]:
            assert math.isclose(actual, expected, rel_tol=1e-6, abs_tol=1e-6)
    for variant in hand['variants']:
        stem = ROOT / 'unity/Assets/Resources' / variant
        assert stem.with_suffix('.prefab').exists()
        for action in ['Dapai', 'LiQiPai', 'LiZhi', 'Fulu', 'Zimo']:
            motion = read(Path(str(stem) + '-' + action + '.json'))
            assert len(motion['tilePoints']) == motion['frames'] * 3
            assert len(motion['tileRotations']) == motion['frames'] * 4
            assert motion['pose']['tracks'][0]['curves']
    bindings.append({'hand': hand['key'], 'sourceItemId': hand['sourceItemId'], 'roles': [{'id': i, 'key': roles[i]['key'], 'name': roles[i]['name']} if i in roles else {'id': i, 'notInCurrentRoster': True} for i in hand['characters']], 'variants': len(hand['variants'])})
assets, guids = [], set()
for file in sorted(ASSETS.rglob('*')):
    if not file.is_file() or file.suffix == '.meta':
        continue
    meta = Path(str(file) + '.meta')
    guid = re.search(r'(?m)^guid: ([0-9a-f]{32})$', meta.read_text(encoding='utf-8'))[1]
    assert guid not in guids
    guids.add(guid)
    assets.append({'file': file.relative_to(ROOT).as_posix(), 'guid': guid, 'bytes': file.stat().st_size, 'sha256': hashlib.sha256(file.read_bytes()).hexdigest()})
manifest = read(SOURCE / 'source-manifest.json')
for row in manifest['bundles']:
    file = SOURCE / row['file']
    assert file.stat().st_size == row['bytes'] and hashlib.sha256(file.read_bytes()).hexdigest() == row['sha256']
report = {'sourceBundlesVerified': len(manifest['bundles']), 'newHandStyles': len(catalog), 'directionModels': sum(len(h['variants']) for h in catalog), 'motions': sum(len(h['variants']) * 5 for h in catalog), 'currentNewlySupportedRoles': sum(i in roles for h in catalog for i in h['characters']), 'existingDogHandRetained': True, 'bindings': bindings, 'assets': assets}
dest = ROOT / 'docs/character-hand-audit-2026-11-14.json'
dest.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(json.dumps({k: v for k, v in report.items() if k not in ['bindings', 'assets']}))
