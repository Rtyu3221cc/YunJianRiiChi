"""Recover UI_GameEnd card-local geometry and untrimmed rank sprites.

The existing score-change rank sprites are deliberately left untouched. Their
trimmed PNGs are used by the hand result page, whereas GameEnd uses the original
48 x 78 sprite canvas and independent placements for the digit and 位.
"""
from pathlib import Path
import hashlib
import io
import json
import sys
import warnings
import zipfile

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'unity/Assets/Resources/Majsoul/Settlement/Final'
OUT.mkdir(parents=True, exist_ok=True)
sys.path.insert(0, str(ROOT / 'tools/asset-inspector'))
import UnityPy
from PIL import Image

warnings.filterwarnings('ignore')
UnityPy.config.FALLBACK_UNITY_VERSION = '2022.3.62f2c1'
rows = [json.loads(s) for file in ['catalog.jsonl', 'fast-catalog.jsonl']
        for s in (ROOT / 'build/reference' / file).read_text('utf8').splitlines()]
cabmap = json.loads((ROOT / 'build/reference/tile-study/cab-map.json').read_text('utf8'))
archive = zipfile.ZipFile(ROOT / 'dist/Majsoul_4.0.16_MC.apk')
cache = {}
records = []

def asset_file(bundle):
    if bundle not in cache:
        cache[bundle] = next(o.assets_file for o in UnityPy.load(archive.read(bundle)).objects)
    return cache[bundle]

def resolve(file, ptr):
    if not ptr['m_PathID']:
        return None
    if ptr['m_FileID']:
        file = asset_file(cabmap[Path(file.externals[ptr['m_FileID'] - 1].path).name])
    return file.objects[ptr['m_PathID']]

def save(name, raw, source, **extra):
    (OUT / name).write_bytes(raw)
    records.append(dict(output=name, bundle=cabmap[source.assets_file.name],
                        pathID=source.path_id, sha256=hashlib.sha256(raw).hexdigest(), **extra))

prefab_path = 'myassets/ui/mj/main/prefab_output/ui_gameend.prefab'
source_row = next(r for r in rows if prefab_path in r.get('paths', []))
file = asset_file(source_row['bundle'])
root = next(o for o in file.objects.values()
            if o.type.name == 'GameObject' and o.peek_name() == 'UI_GameEnd')
nodes = {}

def walk(go, path=''):
    path += '/' + go.peek_name()
    components = [resolve(go.assets_file, c['component']) for c in go.read_typetree()['m_Component']]
    tr = next(o for o in components if o.type.name in ['RectTransform', 'Transform'])
    data = tr.read_typetree()
    nodes[path] = dict(transform=data, components=[o.read_typetree() for o in components
                                                  if o.type.name == 'MonoBehaviour'])
    for ptr in data['m_Children']:
        child_tr = resolve(tr.assets_file, ptr)
        walk(resolve(child_tr.assets_file, child_tr.read_typetree()['m_GameObject']), path)

walk(root)

def local_rect(path, origin, parent_size, parent_pivot, parent_scale):
    t = nodes[path]['transform']
    size = t['m_SizeDelta']
    pivot = t['m_Pivot']
    anchor = t['m_AnchorMin']
    pos = t['m_AnchoredPosition']
    sx = parent_scale[0] * t['m_LocalScale']['x']
    sy = parent_scale[1] * t['m_LocalScale']['y']
    px = origin[0] + (parent_size[0] * (anchor['x'] - parent_pivot[0]) + pos['x']) * parent_scale[0]
    py = origin[1] + (parent_size[1] * (anchor['y'] - parent_pivot[1]) + pos['y']) * parent_scale[1]
    return dict(origin=(px, py), size=(size['x'], size['y']),
                pivot=(pivot['x'], pivot['y']), scale=(sx, sy),
                rect=[px - size['x'] * pivot['x'] * sx,
                      -(py + size['y'] * (1 - pivot['y']) * sy), size['x'] * sx, size['y'] * sy])

players = nodes['/UI_GameEnd/root/container_players']['transform']
parent_size = (players['m_SizeDelta']['x'], players['m_SizeDelta']['y'])
parent_pivot = (players['m_Pivot']['x'], players['m_Pivot']['y'])
parent_origin = (1920 + players['m_AnchoredPosition']['x'],
                 540 + players['m_AnchoredPosition']['y'])
wanted = ['rank', 'rank/node', 'rank/ASPECIALTEXTNODE!@#1', 'head/bg', 'head/node',
          'container_name/label_name', 'container_score', 'val', 'img_g0', 'img_g0/val',
          'img_g1', 'img_g1/val', 'myself']
cards = []
for index in range(4):
    path = f'/UI_GameEnd/root/container_players/p{index}'
    t = nodes[path]['transform']
    card_global = local_rect(path, parent_origin, parent_size, parent_pivot, (1, 1))
    # Card-local coordinates have their origin at the top-left, y increases up.
    computed = {path: dict(origin=(0, 0), size=(t['m_SizeDelta']['x'], t['m_SizeDelta']['y']),
                          pivot=(0, 1), scale=(1, 1))}
    items = []
    for child_path in sorted((p for p in nodes if p.startswith(path + '/')), key=lambda p:p.count('/')):
        parent = computed[child_path.rsplit('/', 1)[0]]
        placement = local_rect(child_path, parent['origin'], parent['size'], parent['pivot'], parent['scale'])
        computed[child_path] = placement
        name = child_path[len(path) + 1:]
        if name not in wanted:
            continue
        font = next((c['m_FontData']['m_FontSize'] for c in nodes[child_path]['components'] if 'm_FontData' in c), 0)
        asset = next((c['asset_path'] for c in nodes[child_path]['components'] if c.get('asset_path')), '')
        items.append(dict(name=name, rect=[round(n, 5) for n in placement['rect']],
                          fontSize=round(font * placement['scale'][1], 4), asset=asset))
    rect = card_global['rect']
    cards.append(dict(rect=[round(rect[0] * 2/3, 5), round((1080 + rect[1]) * 2/3, 5),
                            t['m_SizeDelta']['x'], t['m_SizeDelta']['y']],
                      scale=card_global['scale'][0] * 2/3, items=items))
layout = dict(referenceCanvas=[1920, 1080], cards=cards)
save('gameend-layout.json', (json.dumps(layout, ensure_ascii=False, indent=2) + '\n').encode('utf8'), root)

names = {f's_{i}' for i in range(1,5)} | {f's_{i}_{i}' for i in range(1,5)}
exported = set()
for row in rows:
    if not any('atlas_mj_main_common_' in p or 'atlas_mj_main_chs_0' in p for p in row.get('paths', [])):
        continue
    for obj in asset_file(row['bundle']).objects.values():
        if obj.type.name != 'Sprite' or obj.peek_name() not in names or obj.peek_name() in exported:
            continue
        d = obj.read()
        im = d.image
        # UnityPy exports the packed crop. Restore the Sprite's original canvas
        # so UI_GameEnd's image box does not stretch a narrow rank digit.
        rect = d.m_Rect
        canvas = Image.new('RGBA', (round(rect.width), round(rect.height)))
        offset = d.m_RD.textureRectOffset
        canvas.paste(im, (round(offset.x), round(rect.height - im.height - offset.y)))
        raw = io.BytesIO()
        canvas.save(raw, format='PNG')
        save(d.m_Name + '.png', raw.getvalue(), obj,
             canvas=list(canvas.size), crop=list(im.size),
             textureRectOffset=[offset.x, offset.y])
        exported.add(d.m_Name)

(OUT / 'source.json').write_text(json.dumps(dict(apk='Majsoul_4.0.16_MC.apk',
    description='UI_GameEnd original card-local RectTransforms and untrimmed rank sprite canvases.',
    assets=records), ensure_ascii=False, indent=2) + '\n', encoding='utf8')
print('Final settlement resources:', len(records), 'records')
for card in cards:
    print('CARD', card['rect'], 'scale', card['scale'])
