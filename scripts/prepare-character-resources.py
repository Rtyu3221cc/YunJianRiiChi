"""Prepare the approved women TOP10 + Wangcilang sources from the supplied APK.

Heavy optional files stay outside Resources. Two newer outfits absent in that
APK use the matching official version manifest, never a guessed asset URL.
"""
from pathlib import Path
import concurrent.futures, hashlib, json, re, runpy, time, urllib.request

R = Path(__file__).resolve().parent.parent
B = R / 'build/character-resources-v1'
B.mkdir(parents=True, exist_ok=True)
S = runpy.run_path(str(R / 'scripts/study-characters-1018.py'))
C = R / 'unity/Assets/Resources/Majsoul/Characters'
D = R / 'unity/Assets/Resources/Majsoul/CharacterDirectory'
P = R / 'unity/Assets/CharacterPacks/Source'
CFG = json.loads((R / 'build/characters-1018/config.json').read_text('utf8'))
V = json.loads((R / 'build/reference/public/resversion0.11.252.w.json').read_text('utf8'))['res']
TARGETS = {200001, 200010, 200038, 200007, 200029, 200005, 200067, 200024, 200019, 200002, 200012}
BASE = {'yiji', 'erjietang'}
catalog = json.loads((D / 'catalog.json').read_text('utf8'))
byid = {s['id']: s for s in CFG['item_definition/skin']}
records = []

def js(p, value):
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', 'utf8')

def fetch(key, dest):
    assert key in V, key
    url = 'https://game.maj-soul.com/1/' + V[key]['prefix'] + '/' + key
    if not dest.exists():
        for attempt in range(5):
            try:
                with urllib.request.urlopen(url, timeout=60) as response:
                    data = response.read()
                assert len(data) > 20 and not data.startswith(b'<'), key
                if dest.suffix == '.png' and not data.startswith(b'\x89PNG'):
                    mask = data[0] ^ 0x89
                    data = bytes(b ^ mask for b in data)
                dest.parent.mkdir(parents=True, exist_ok=True)
                partial = dest.with_name(dest.name + '.download')
                partial.write_bytes(data)
                partial.replace(dest)
                break
            except Exception:
                if attempt == 4:
                    raise
                time.sleep(attempt + 1)
    return dict(path=dest.relative_to(R).as_posix(), url=url, bytes=dest.stat().st_size,
                sha256=hashlib.sha256(dest.read_bytes()).hexdigest())

def png(dest, obj, logical=True):
    if dest.exists():
        return
    from PIL import Image
    image = obj.read().image
    if logical:
        tree = obj.read_typetree()
        rect, off = tree['m_Rect'], tree['m_RD']['textureRectOffset']
        canvas = Image.new('RGBA', (round(rect['width']), round(rect['height'])))
        canvas.paste(image, (round(off['x']), round(rect['height'] - off['y'] - image.height)))
        image = canvas
    dest.parent.mkdir(parents=True, exist_ok=True)
    image.save(dest)

def spine_from_apk(source, folder):
    folder.mkdir(parents=True, exist_ok=True)
    objects = S['af'](S['paths'][source]).objects.values()
    pages, seen = set(), set()
    for obj in objects:
        if obj.type.name != 'TextAsset':
            continue
        name, data = obj.peek_name(), S['raw'](obj)
        if name.endswith('.skel'):
            (folder / 'skeleton.bytes').write_bytes(data)
        elif name.endswith('.atlas'):
            (folder / 'atlas.txt').write_bytes(data)
            pages.update(line.strip() for line in data.decode().splitlines() if line.strip().endswith('.png'))
        elif name == '_info_output':
            (folder / 'info.txt').write_bytes(data)
    for obj in objects:
        if obj.type.name != 'Material':
            continue
        for _, entry in obj.read_typetree()['m_SavedProperties']['m_TexEnvs']:
            tex = S['resolve'](obj.assets_file, entry['m_Texture'])
            if tex and tex.type.name == 'Texture2D' and tex.peek_name() + '.png' in pages:
                png(folder / (tex.peek_name() + '.png'), tex, False)
                seen.add(tex.peek_name() + '.png')
    assert seen == pages and (folder / 'skeleton.bytes').is_file(), str(folder)

bank = json.loads((C / 'framing.json').read_text('utf8'))
defaults = json.loads((R / 'build/character-review-r2/official-default.json').read_text('utf8'))['default']
new_framing, jobs, summary = [], [], []
for role in catalog['roles']:
    if role['id'] not in TARGETS:
        continue
    character = next(c for c in CFG['item_definition/character'] if c['id'] == role['id'])
    root = C if role['key'] in BASE else P / role['key']
    # Existing protected characters already have their complete 7 / 6 outfits.
    if role['key'] not in BASE:
        role['skins'] = []
        for item in (s for s in CFG['item_definition/skin'] if s['character_id'] == role['id']):
            sid, key = item['id'], item['path'].rsplit('/', 1)[-1].lower()
            prefabs = sorted(p for p in S['paths'] if p.startswith('myassets/spine/' + str(sid) + '/') and p.endswith('/character.prefab'))
            parts = ['Spine/' + str(sid) + '/' + ('main' if p.count('/') == 3 else p.split('/')[-2]) for p in prefabs]
            if item['spine_type'] and not parts:
                parts = ['Spine/' + str(sid) + '/main']
            skin = dict(id=sid, key=key, name=item['name_chs'], description=item['desc_chs'].replace('\\n', '\n'),
                        dynamic=bool(item['spine_type']), spineWidth=item['spine_width'], spineHeight=item['spine_height'],
                        pivotX=item['pivot_x'], pivotY=item['pivot_y'], parts=parts,
                        lobbyOffset=item['lobby_offset'], dormOffset=item['liaoshe_offset'])
            role['skins'].append(skin)
            original_key = item['path'].rsplit('/', 1)[-1]
            web = 'lang/base/extendRes/charactor/' + original_key + '/'
            for kind in ['full', 'bighead', 'smallhead', 'half', 'waitingroom']:
                source = 'myassets/deco/character/' + key + '/' + kind + '/' + kind + '.png'
                dest = root / 'Skins' / str(sid) / (kind + '.png')
                if source in S['paths']:
                    png(dest, S['obj'](source, 'Sprite'))
                elif web + kind + '.png' in V:
                    records.append(fetch(web + kind + '.png', dest))
            assert all((root / 'Skins' / str(sid) / (k + '.png')).is_file() for k in ['full', 'bighead', 'smallhead']), sid
            if prefabs:
                for source, part in zip(prefabs, parts):
                    spine_from_apk(source, root / part)
            elif skin['dynamic']:
                folder = root / parts[0]
                records.append(fetch(web + 'spine/spine.atlas.txt', folder / 'atlas.txt'))
                records.append(fetch(web + 'spine/spine.skel.txt', folder / 'skeleton.bytes'))
                pages = [line.strip() for line in (folder / 'atlas.txt').read_text('utf8').splitlines() if line.strip().endswith('.png')]
                for page in pages:
                    records.append(fetch(web + 'spine/' + page, folder / page))
            S['cache'].clear()
            print('Prepared', role['name'], sid, skin['name'], 'dynamic' if skin['dynamic'] else 'still', flush=True)
    for skin in role['skins']:
        item = byid[skin['id']]
        key = item['illust_data']
        data = None
        if key and 'lang/base/extendRes/illust_data/' + key + '.json' in V:
            dest = B / 'illust' / (key + '.json')
            records.append(fetch('lang/base/extendRes/illust_data/' + key + '.json', dest))
            data = json.loads(dest.read_text('utf8'))
        placements = []
        for kind in ['F0', 'I0', 'J0']:
            for index in range(1 if kind == 'F0' else 4):
                if not data:
                    continue
                d, a = defaults[kind][index], data[kind][index]
                w, h = d['scale'] * data['w'], d['scale'] * data['h']
                sign = -1 if a['reverse'] and not item['lock_reverse'] else 1
                x, y = d['x'] + a['x'] * w * a['scale'], d['y'] + a['y'] * h * a['scale']
                if sign < 0:
                    x += 2 * data['center'] * w
                off = dict(x=110, y=-234, scaleX=1.58, scaleY=1.58) if kind == 'F0' else d.get('offset', dict(x=0, y=0, scaleX=1, scaleY=1))
                sx, sy = off['scaleX'], off['scaleY']
                placements.append(dict(scene=kind, index=index,
                    rect=[off['x'] + x/sx, off['y'] + y/sy, w/abs(sx)*a['scale'], h/abs(sy)*a['scale']],
                    flip=sign * (1 if sx > 0 else -1) < 0))
        new_framing.append(dict(id=skin['id'], source=key or 'legacy crop table', placements=placements,
            full=[item['full_' + k] for k in ['x', 'y', 'width', 'height']],
            head=[item['smallhead_' + k] for k in ['x', 'y', 'width']],
            direction=item['direction'], noReverse=bool(item['no_reverse']), lockReverse=bool(item['lock_reverse'])))
    if role['key'] not in BASE:
        jobs += [(k, root / 'Voice' / k.removeprefix('audio/sound/')) for k in V if k.startswith('audio/sound/' + role['key'] + '/') and k.endswith('.mp3')]
        jobs += [(k, root / 'Emoji' / role['key'] / Path(k).name) for k in V if k.startswith(character['emo'] + '/') and re.fullmatch(r'\d+\.png', Path(k).name)]
    summary.append(dict(key=role['key'], name=role['name'], builtin=role['key'] in BASE,
                        skins=len(role['skins']), dynamics=sum(s['dynamic'] for s in role['skins'])))
with concurrent.futures.ThreadPoolExecutor(max_workers=10) as pool:
    for i, record in enumerate(pool.map(lambda j: fetch(*j), jobs)):
        records.append(record)
        if i % 100 == 0:
            print('Supplement files', i + 1, '/', len(jobs), flush=True)
bank['skins'] = [s for s in bank['skins'] if s['id'] not in {s['id'] for s in new_framing}] + new_framing
js(C / 'framing.json', bank)
js(D / 'catalog.json', catalog)
available = {r['key'] for r in catalog['roles'] if r['id'] in TARGETS} | {'weilai', 'siqiqiansuili'}
js(B / 'directory-template.json', dict(schema=1, protocol=1, packs=[dict(key=r['key'], name=r['name'], id=r['id'],
    skins=[s['id'] for s in r['skins']], builtin=r['key'] in BASE, recommended=r['id'] in TARGETS,
    available=r['key'] in available, version=1, bytes=0, sha256='', url='', file='') for r in catalog['roles']]))
js(B / 'approved-roles.json', summary)
js(B / 'official-sources.json', records)
print('SOURCE PREPARATION PASS', len(summary), 'roles', sum(r['skins'] for r in summary), 'outfits', flush=True)
