"""Rebuild every scene from the local reference APK and pinned illustration inputs.

Does not rebuild/download/overwrite any published character bundle.
"""
import hashlib
import json
from pathlib import Path
import runpy
import struct
from character_framing import placements

R = Path(__file__).resolve().parent.parent
B = R / 'build/portrait-fix-068'
B.mkdir(parents=True, exist_ok=True)
D = R / 'docs/character-resource-inputs'
C = R / 'unity/Assets/Resources/Majsoul/Characters'
catalog_path = R / 'unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json'
bank = json.loads((C / 'framing.json').read_text('utf8'))
catalog = json.loads(catalog_path.read_text('utf8'))
cfg = json.loads((D / 'config.json').read_text('utf8'))
skins = {s['id']: s for s in cfg['item_definition/skin']}
source = runpy.run_path(str(R / 'scripts/study-characters-1018.py'))
bundle = source['paths']['myassets/deco/illust_data/yiji.json']
definitions, origins = {}, {}
for obj in source['af'](bundle).objects.values():
    if obj.type.name != 'TextAsset':
        continue
    raw = source['raw'](obj)
    try:
        value = json.loads(raw)
    except (ValueError, UnicodeError):
        continue
    if isinstance(value, dict) and ('w' in value or obj.peek_name() == 'default' and 'default' in value):
        key = obj.peek_name()
        definitions[key] = value
        origins[key] = dict(bundle=bundle, pathID=obj.path_id, sha256=hashlib.sha256(raw).hexdigest())
defaults = definitions.pop('default')['default']
(D / 'framing-native-default.json').write_text(json.dumps(dict(default=defaults), ensure_ascii=False, indent=2)+'\n', 'utf8')
for folder in ['build/character-review-r2', 'build/character-pack-experiment/illust',
               'build/character-resources-v1/illust', 'build/character-resources-all/illust']:
    for file in (R / folder).glob('*.json'):
        key = file.stem.removeprefix('official-')
        if key in definitions:
            continue
        value = json.loads(file.read_text('utf8'))
        if isinstance(value, dict) and 'w' in value and 'I0' in value:
            definitions[key] = value
            origins[key] = dict(pinnedCache=file.relative_to(R).as_posix(), sha256=hashlib.sha256(file.read_bytes()).hexdigest())
baseline = json.loads((B / 'framing-before.json').read_text('utf8')) if (B / 'framing-before.json').exists() else bank
old = {s['id']: s for s in baseline['skins']}
changed, missing, aspect_errors, reverse = [], [], [], []
result = []
for role in catalog['roles']:
    root = C if role['key'] in ('yiji', 'erjietang') else R / 'unity/Assets/CharacterPacks/Source' / role['key']
    for skin in role['skins']:
        item = skins[skin['id']]
        data = definitions.get(item['illust_data'])
        if item['illust_data'] and not data:
            missing.append([skin['id'], item['illust_data']])
        width, height = struct.unpack('>II', (root / 'Skins' / str(skin['id']) / 'full.png').read_bytes()[16:24])
        skin['artWidth'], skin['artHeight'] = width, height
        skin['hasWaitingroom'] = (root / 'Skins' / str(skin['id']) / 'waitingroom.png').is_file()
        if data and abs(width / height - data['w'] / data['h']) > .004:
            aspect_errors.append([skin['id'], [width, height], [data['w'], data['h']]])
        record = dict(id=skin['id'], source=item['illust_data'] or 'legacy crop table',
            placements=placements(item, data, defaults),
            full=[item['full_'+k] for k in ['x','y','width','height']],
            head=[item['smallhead_'+k] for k in ['x','y','width']],
            direction=item['direction'], noReverse=bool(item['no_reverse']), lockReverse=bool(item['lock_reverse']))
        if any(abs(a-b) > .001 for p in old[skin['id']]['placements'] for q in record['placements']
               if p['scene'] == q['scene'] and p['index'] == q['index'] for a,b in zip(p['rect'],q['rect'])):
            changed.append(skin['id'])
        if data and item['no_reverse'] and not item['lock_reverse'] and any(p['reverse'] for k in ['F0','I0','J0'] for p in data.get(k, [])):
            reverse.append(skin['id'])
        result.append(record)
assert not missing, missing
# APK Sprite.rect can differ from the pinned web canvas. Retain both dimensions
# in the audit; renderers preserve the actual Sprite aspect instead of stretching.
if not (B / 'framing-before.json').exists():
    (B / 'framing-before.json').write_text(json.dumps(bank, ensure_ascii=False), 'utf8')
bank['skins'] = result
(C / 'framing.json').write_text(json.dumps(bank, ensure_ascii=False, indent=2)+'\n', 'utf8')
catalog_path.write_text(json.dumps(catalog, ensure_ascii=False, indent=2)+'\n', 'utf8')
wanted = {s['illust_data'] for s in skins.values() if s['character_id'] and s['illust_data']}
(D / 'framing-illustrations.json').write_text(json.dumps(dict(definitions={k:v for k,v in definitions.items() if k in wanted}, sources={k:v for k,v in origins.items() if k in wanted or k=='default'}), ensure_ascii=False, indent=2)+'\n', 'utf8')
report = dict(skins=len(result), correctedExistingPoses=len(changed), changedSkins=changed,
              placements=sum(len(s['placements']) for s in result), missing=missing,
              canvasAspectDifferencesCorrectedAtRuntime=aspect_errors, reverseVariantsNeeded=reverse,
              nativeDefault=origins['default'], publishedPacksChanged=False)
(B / 'framing-audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', 'utf8')
print(json.dumps(report, ensure_ascii=False))
