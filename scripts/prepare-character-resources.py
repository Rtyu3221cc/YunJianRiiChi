"""Prepare selected or all characters from a fixed official content snapshot.

Heavy optional files stay outside Resources. Existing published packs are reused;
changed roles receive a new bundle version. --force rebuilds corrected artwork.
"""
from pathlib import Path
from character_framing import placements as compile_placements
import argparse, concurrent.futures, hashlib, json, re, runpy, time, urllib.request

R = Path(__file__).resolve().parent.parent
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--all',action='store_true')
parser.add_argument('--roles',help='Comma separated sound_folder keys')
parser.add_argument('--force',action='store_true',help='Version and rebuild selected roles even if outfit IDs match')
parser.add_argument('--web-only',action='store_true',help='Use the versioned official files without reading an APK')
parser.add_argument('--config',type=Path,default=R/('build/characters-1018/config.json'if(R/'build/characters-1018/config.json').exists()else'docs/character-resource-inputs/config.json'))
parser.add_argument('--versions',type=Path,default=R/('build/reference/public/resversion0.11.252.w.json'if(R/'build/reference/public/resversion0.11.252.w.json').exists()else'docs/character-resource-inputs/versions.json'))
parser.add_argument('--build-root',type=Path,default=R/'build/character-resources-all')
parser.add_argument('--content-tag',default='character-resources-v2')
parser.add_argument('--workers',type=int,default=16)
args=parser.parse_args()
assert not(args.all and args.roles),'Choose --all or --roles'
assert re.fullmatch(r'[a-zA-Z0-9._-]+',args.content_tag)
B = args.build_root.resolve();assert B.is_relative_to(R.resolve())
B.mkdir(parents=True, exist_ok=True)
S = dict(paths={},cache={}) if args.web_only else runpy.run_path(str(R / 'scripts/study-characters-1018.py'))
C = R / 'unity/Assets/Resources/Majsoul/Characters'
D = R / 'unity/Assets/Resources/Majsoul/CharacterDirectory'
P = R / 'unity/Assets/CharacterPacks/Source'
CFG = json.loads(args.config.read_text('utf8'))
V = json.loads(args.versions.read_text('utf8'))['res']
TARGETS = {200001, 200010, 200038, 200007, 200029, 200005, 200067, 200024, 200019, 200002, 200012}
RECOMMENDED=TARGETS.copy()
recommend_file=R/'docs/character-resource-inputs/recommended.json'
if recommend_file.exists():
    recommend_keys=set(json.loads(recommend_file.read_text('utf8'))['keys'])
    RECOMMENDED={c['id']for c in CFG['item_definition/character']if c['sound_folder']in recommend_keys}
    assert len(RECOMMENDED)==len(recommend_keys),'Unknown recommended role'
if args.all:TARGETS={c['id']for c in CFG['item_definition/character']}
if args.roles:
    keys=set(args.roles.split(','));TARGETS={c['id']for c in CFG['item_definition/character']if c['sound_folder']in keys}
    assert len(keys)==len(TARGETS),'Unknown or duplicate role keys'
BASE = {'yiji', 'erjietang'}
catalog = json.loads((D / 'catalog.json').read_text('utf8'))
previous={p['key']:p for p in json.loads((D/'packs-android.json').read_text('utf8'))['packs']}
byid = {s['id']: s for s in CFG['item_definition/skin']}
records = [];changed=set()
for c in CFG['item_definition/character']:
    if c['id']not in TARGETS or any(r['key']==c['sound_folder']for r in catalog['roles']):continue
    catalog['roles'].append(dict(id=c['id'],key=c['sound_folder'],name=c['name_chs'],sex=c['sex'],cv=c['desc_cv_chs'],height=c['desc_stature_chs'],birth=c['desc_birth_chs'],age=c['desc_age_chs'],blood=c['desc_bloodtype_chs'],hobby=c['desc_hobby_chs'],description=c['desc_chs'],defaultSkin=c['init_skin'],bondSkin=c['full_fetter_skin'],skins=[],emojis=[]))

def js(p, value):
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', 'utf8')

def fetch(key, dest):
    assert key in V, key
    # Resolve an existing directory, rather than racing another worker which is
    # creating the same previously missing Windows parent hierarchy.
    dest.parent.mkdir(parents=True,exist_ok=True)
    resolved=dest.parent.resolve()
    assert resolved.is_relative_to(R.resolve()),(str(dest),str(resolved))
    url = 'https://game.maj-soul.com/1/' + V[key]['prefix'] + '/' + key
    if not dest.exists() or args.force:
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
    if dest.exists() and not args.force:
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
    if not args.force and (folder/'skeleton.bytes').is_file()and(folder/'atlas.txt').is_file():
        pages=[s.strip()for s in(folder/'atlas.txt').read_text('utf8').splitlines()if s.strip().endswith('.png')]
        if pages and all((folder/p).is_file()for p in pages):return
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
defaults = json.loads((R/'docs/character-resource-inputs/framing-native-default.json').read_text('utf8'))['default']
new_framing, jobs, summary = [], [], []
for role in catalog['roles']:
    if role['id'] not in TARGETS:
        continue
    character = next(c for c in CFG['item_definition/character'] if c['id'] == role['id'])
    role.update(name=character['name_chs'],cv=character.get('desc_cv_chs',''),height=character.get('desc_stature_chs',''),birth=character.get('desc_birth_chs',''),age=character.get('desc_age_chs',''),blood=character.get('desc_bloodtype_chs',''),hobby=character.get('desc_hobby_chs',''),description=character.get('desc_chs',''),defaultSkin=character['init_skin'],bondSkin=character['full_fetter_skin'])
    assert re.fullmatch(r'[a-z0-9_-]+',role['key']),role['key']
    desired=[s['id']for s in CFG['item_definition/skin']if s['character_id']==role['id']]
    old=previous.get(role['key'],{})
    if old.get('available'):assert set(old['skins']).issubset(desired),'Published outfit IDs must not disappear; merge old and new input snapshots first'
    reuse=old.get('available')and set(old['skins'])==set(desired)and not args.force
    if reuse:
        summary.append(dict(key=role['key'],name=role['name'],builtin=role['key']in BASE,skins=len(role['skins']),dynamics=sum(s['dynamic']for s in role['skins'])))
        print('Reusing published pack',role['name'],flush=True);continue
    changed.add(role['key'])
    root = C if role['key'] in BASE else P / role['key']
    if not reuse:
        role['skins'] = []
        for item in (s for s in CFG['item_definition/skin'] if s['character_id'] == role['id']):
            sid, key = item['id'], item['path'].rsplit('/', 1)[-1].lower()
            prefabs = sorted(p for p in S['paths'] if p.startswith('myassets/spine/' + str(sid) + '/') and p.endswith('/character.prefab'))
            parts = ['Spine/' + str(sid) + '/' + ('main' if p.count('/') == 3 else p.split('/')[-2]) for p in prefabs]
            original_key = item['path'].rsplit('/', 1)[-1]
            web = 'lang/base/extendRes/charactor/' + original_key + '/'
            web_skeletons=sorted(k for k in V if k.startswith(web+'spine/')and k.endswith('.skel.txt'))
            if item['spine_type'] and not parts:
                assert web_skeletons,('No dynamic skeleton source',sid)
                parts=['Spine/'+str(sid)+'/'+('main'if len(web_skeletons)==1 else Path(k).name.removesuffix('.skel.txt'))for k in web_skeletons]
            skin = dict(id=sid, key=key, name=item['name_chs'], description=item['desc_chs'].replace('\\n', '\n'),
                        dynamic=bool(item['spine_type']), spineWidth=item['spine_width'], spineHeight=item['spine_height'],
                        pivotX=item['pivot_x'], pivotY=item['pivot_y'], parts=parts,
                        lobbyOffset=item['lobby_offset'], dormOffset=item['liaoshe_offset'])
            role['skins'].append(skin)
            for kind in ['full', 'bighead', 'smallhead', 'half', 'waitingroom'] + (['reverse-full'] if item['no_reverse'] else []):
                source = 'myassets/deco/character/' + key + ('/reverse/full.png' if kind == 'reverse-full' else '/' + kind + '/' + kind + '.png')
                web_kind = 'reverse/full' if kind == 'reverse-full' else kind
                dest = root / 'Skins' / str(sid) / (kind + '.png')
                if dest.exists() and not args.force:continue
                if source in S['paths']:
                    png(dest, S['obj'](source, 'Sprite'))
                elif web + web_kind + '.png' in V:
                    records.append(fetch(web + web_kind + '.png', dest))
            assert all((root / 'Skins' / str(sid) / (k + '.png')).is_file() for k in ['full', 'bighead', 'smallhead']), sid
            from PIL import Image
            with Image.open(root/'Skins'/str(sid)/'full.png') as image:skin.update(artWidth=image.width,artHeight=image.height)
            skin['hasWaitingroom']=(root/'Skins'/str(sid)/'waitingroom.png').is_file()
            if prefabs:
                for source, part in zip(prefabs, parts):
                    spine_from_apk(source, root / part)
            elif skin['dynamic']:
                for source,part in zip(web_skeletons,parts):
                    folder=root/part
                    records.append(fetch(source.replace('.skel.txt','.atlas.txt'),folder/'atlas.txt'))
                    records.append(fetch(source,folder/'skeleton.bytes'))
                    pages=[line.strip()for line in(folder/'atlas.txt').read_text('utf-8-sig').splitlines()if line.strip().endswith('.png')]
                    for page in pages:records.append(fetch(source.rsplit('/',1)[0]+'/'+page,folder/Path(page).name))
            for part in parts:
                header=(root/part/'skeleton.bytes').read_bytes()[:80]
                assert b'4.2.'in header,('Unsupported Spine binary version',sid,part,header[:24].hex())
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
        placements = compile_placements(item, data, defaults)
        new_framing.append(dict(id=skin['id'], source=key or 'legacy crop table', placements=placements,
            full=[item['full_' + k] for k in ['x', 'y', 'width', 'height']],
            head=[item['smallhead_' + k] for k in ['x', 'y', 'width']],
            direction=item['direction'], noReverse=bool(item['no_reverse']), lockReverse=bool(item['lock_reverse'])))
    if not reuse:
        jobs += [(k, root / 'Voice' / k.removeprefix('audio/sound/')) for k in V if k.startswith('audio/sound/' + role['key'] + '/') and k.endswith('.mp3')]
        emoji_keys=[]
        for prefix in [character['emo']+'/', 'lang/base/'+character['emo']+'/']:
            emoji_keys=[k for k in sorted(V)if k.startswith(prefix)and re.fullmatch(r'\d+\.png',k[len(prefix):])]
            if emoji_keys:break
        assert emoji_keys,'No expression bank found for '+role['key']
        jobs += [(k, root / 'Emoji' / role['key'] / Path(k).name) for k in emoji_keys]
        role['emojis']=['Emoji/'+role['key']+'/'+Path(k).stem for k in emoji_keys]
        avatar=D/'Avatars'/(str(role['id'])+'.png')
        if not avatar.exists():
            from PIL import Image
            image=Image.open(root/'Skins'/str(role['defaultSkin'])/'bighead.png').convert('RGBA');image.thumbnail((160,160));image.save(avatar)
    summary.append(dict(key=role['key'], name=role['name'], builtin=role['key'] in BASE,
                        skins=len(role['skins']), dynamics=sum(s['dynamic'] for s in role['skins'])))
with concurrent.futures.ThreadPoolExecutor(max_workers=args.workers) as pool:
    for i, record in enumerate(pool.map(lambda j: fetch(*j), jobs)):
        records.append(record)
        if i % 100 == 0:
            print('Supplement files', i + 1, '/', len(jobs), flush=True)
bank['skins'] = [s for s in bank['skins'] if s['id'] not in {s['id'] for s in new_framing}] + new_framing
js(C / 'framing.json', bank)
js(D / 'catalog.json', catalog)
manifest=[];plan=[]
for r in catalog['roles']:
    old=previous.get(r['key'],{});build=r['key']in changed;version=(old.get('version',0)+1 if old.get('available')else 1)if build else old.get('version',1)
    row=dict(key=r['key'],name=r['name'],id=r['id'],skins=[s['id']for s in r['skins']],builtin=r['key']in BASE,recommended=r['id']in RECOMMENDED,available=build or old.get('available',False),version=version,bytes=0,sha256='',url='',file='')
    manifest.append(row);plan.append(dict(key=r['key'],version=version,build=build,builtin=row['builtin']))
js(B/'directory-template.json',dict(schema=1,protocol=1,packs=manifest))
js(B/'build-plan.json',dict(tag=args.content_tag,output=B.relative_to(R).as_posix(),compression='lz4',packs=plan))
js(D/'build-plan.json',dict(tag=args.content_tag,output=B.relative_to(R).as_posix(),compression='lz4',packs=plan))
js(B / 'approved-roles.json', summary)
js(B / 'official-sources.json', records)
inputs=R/'docs/character-resource-inputs'
js(inputs/'config.json',{k:CFG[k]for k in ['item_definition/character','item_definition/skin']})
js(inputs/'versions.json',dict(res={k:v for k,v in V.items()if k.startswith(('audio/sound/','extendRes/emo/','lang/base/extendRes/emo/','lang/base/extendRes/charactor/','lang/base/extendRes/illust_data/'))}))
print('SOURCE PREPARATION PASS', len(summary), 'roles', sum(r['skins'] for r in summary), 'outfits', flush=True)
