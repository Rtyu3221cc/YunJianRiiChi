"""Link verified, immutable existing bundles into an isolated review client."""
from pathlib import Path
import hashlib, json, os
R = Path(__file__).resolve().parent.parent
B = R / 'build/review-r25'
rows = []
for kind, catalog, source_dir in [
    ('roles', 'CharacterDirectory', 'build/character-resources-v1/windows'),
    ('wardrobes', 'WardrobeDirectory', 'build/wardrobe-r22/packs/windows')]:
    packs = json.loads((R / 'unity/Assets/Resources/Majsoul' / catalog / 'packs-windows.json').read_text('utf8'))['packs']
    for pack in packs:
        if pack['builtin'] or not pack['available']:
            continue
        source = R / source_dir / pack['file']
        if kind == 'roles' and not source.is_file():
            source = R / 'build/character-resources-all/windows' / pack['file']
        assert source.is_file() and source.stat().st_size == pack['bytes'], source
        digest = hashlib.sha256()
        with source.open('rb') as stream:
            for chunk in iter(lambda: stream.read(1048576), b''):
                digest.update(chunk)
        assert digest.hexdigest() == pack['sha256'], source
        target = B / 'clients' / kind / pack['key'] / (pack['sha256'] + '.bundle')
        target.parent.mkdir(parents=True, exist_ok=True)
        if not target.exists():
            os.link(source, target)
        assert os.path.samefile(source, target), target
        Path(str(target) + '.ok').write_text(pack['sha256'], 'utf8')
        rows.append(dict(kind=kind, key=pack['key'], bytes=pack['bytes'], source=source.relative_to(R).as_posix()))
    print('Verified and linked', kind, sum(x['kind'] == kind for x in rows), flush=True)
(B / 'fixture-links.json').write_text(json.dumps(dict(immutableHardLinks=True, copiedBundleBytes=0, files=rows), ensure_ascii=False, indent=1) + '\n', 'utf8')
