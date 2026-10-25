"""Original decoration layouts, icons, preview media and APK model/particle graphs.

The APK is the primary source. Optional files absent from that base APK use the
already cached official version manifest; every fallback records its exact URL.
"""
from pathlib import Path
import ast
import hashlib
import io
import json
import runpy
import urllib.parse
import urllib.request
import numpy as np
from PIL import Image

R = Path(__file__).resolve().parent.parent
base = runpy.run_path(str(R / 'scripts/import-wardrobe-preview.py'))
S, OUT, items, records = (base[k] for k in ['S', 'OUT', 'items', 'records'])
CACHE = R / 'build/wardrobe-reference-r2/cdn'
CACHE.mkdir(parents=True, exist_ok=True)
INDEX = json.loads((R / 'build/reference/public/resversion0.11.252.w.json').read_text('utf8'))['res']
sources = []
close_atlas = 'myassets/ui/necessary/pic/common/atlas_ui_necessary_common_0.png'
for obj in S['af'](S['paths'][close_atlas]).objects.values():
    if obj.type.name == 'Sprite' and obj.peek_name() == 'btn_close':
        d = obj.read_typetree();rect=d['m_Rect'];offset=d['m_RD']['textureRectOffset'];im=obj.read().image
        canvas=Image.new('RGBA',(round(rect['width']),round(rect['height'])))
        canvas.paste(im,(round(offset['x']),round(rect['height']-offset['y']-im.height)))
        base['write_image'](canvas,'UI/preview_close',close_atlas,obj,[0,0,0,0])
        break

def fetch(path):
    f = CACHE / (hashlib.sha256(path.encode()).hexdigest()[:16] + Path(path).suffix)
    prefix = INDEX[path]['prefix']
    url = 'https://game.maj-soul.com/1/' + prefix + '/' + urllib.parse.quote(path)
    if not f.exists():
        f.write_bytes(urllib.request.urlopen(url, timeout=40).read())
    raw = f.read_bytes()
    sources.append(dict(path=path, source=url, sha256=hashlib.sha256(raw).hexdigest()))
    return raw

def image(path, key):
    raw = fetch(path)
    try:
        im = Image.open(io.BytesIO(raw))
    except Exception:
        raw = bytes(x ^ 0x49 for x in raw)
        im = Image.open(io.BytesIO(raw))
    file = OUT / (key + '.png')
    file.parent.mkdir(parents=True, exist_ok=True)
    im.save(file, format='PNG')
    sources[-1].update(output=key + '.png', width=im.width, height=im.height,
                       decodedSHA256=hashlib.sha256(file.read_bytes()).hexdigest())
    return 'Majsoul/Wardrobe/' + key

def entry(id):
    return next(x for x in items if x['id'] == id)

def icon(path, key):
    return image(path, 'Icons/' + key)

for id, path in {
    'cloth-chigua': 'extendRes/items/tablecloth_chigua.jpg',
    'cloth-default': 'extendRes/items/tablecloth_navy.jpg',
    'mjp_default': 'extendRes/items/mjp_yellow.jpg',
    'mjp_20chunjie': 'extendRes/items/mjp_20chunjie.jpg',
    'mjpface_default': 'extendRes/items2/mjpface_default.jpg',
    'mjpface_2308event': 'extendRes/items2/mjpface_2308event.jpg',
    'headframe_catear3': 'extendRes/items2/headframe_catear3.jpg',
}.items():
    entry(id)['icon'] = icon(path, id)
entry('cloth-chigua')['preview'] = image('myres2/tablecloth/tablecloth_chigua/preview.jpg', 'Preview/cloth-chigua')
blue = 'myres2/tablecloth/tablecloth_navy/preview.jpg'
entry('cloth-default')['preview'] = image(blue if blue in INDEX else 'myres2/tablecloth/tablecloth_default/preview.jpg', 'Preview/cloth-default')
entry('cloth-default')['name'] = '桌布－深蓝'
cat_back = entry('mjp_20chunjie')
cat_root = 'myassets/deco/mjpai/mjp_2308event'
cat_back.update(id='mjp_2308event', name='牌背－猫咪',
                icon=icon('extendRes/items2/mjp_2308event.jpg', 'mjp_2308event'),
                hand=base['texture'](cat_root + '/3d/texture/hand.png', 'Items/mjp_2308event/hand'),
                table=base['texture'](cat_root + '/3d/texture/mjp.png', 'Items/mjp_2308event/table'),
                preview=base['texture'](cat_root + '/preview/common/preview.png', 'Items/mjp_2308event/preview'))
entry('headframe_catear3')['texture'] = image('lang/base/extendRes/head_frame/headframe_catear3.png', 'Items/headframe_catear3/native-frame')
# The web/native avatar window is kept separate from the texture's transparent margin.
entry('headframe_catear3')['avatarWindow'] = [54 / 320, 54 / 320, 212 / 320, 212 / 320]
items[:] = [i for i in items if i['id'] != 'background-indoor']
entry('background-yard').update(name='神社庭院·春樱', icon=icon('extendRes/items/beijing_chunying.jpg', 'background-yard'), preview=image('myres2/lobby_bg/beijing_chunying.jpg', 'Preview/spring'))
autumn = image('scene/Assets/Resource/lobby/beijing_qiushen.jpg', 'Background/autumn')
autumn_preview = image('myres2/lobby_bg/beijing_qiushen.jpg', 'Preview/autumn')
items.append(dict(id='background-autumn', category='lobbyBackground', name='神社庭院·秋深', builtin=True,
                  icon=icon('extendRes/items/beijing_qiushen.jpg', 'background-autumn'), texture=autumn, preview=autumn_preview))

def write_json(relative, value):
    file = OUT / relative
    file.parent.mkdir(parents=True, exist_ok=True)
    file.write_text(json.dumps(value, ensure_ascii=False, separators=(',', ':')), 'utf8')

def vec(value):
    return [value[k] for k in ('xyzw' if 'w' in value else 'xyz' if 'z' in value else 'xy')]

def graph(root):
    result = []
    def visit(go, parent=-1):
        components = [S['resolve'](go.assets_file, c['component']) for c in go.read_typetree()['m_Component']]
        tr = next(c for c in components if c.type.name in ['Transform', 'RectTransform'])
        td = tr.read_typetree()
        index = len(result)
        result.append((go, td, components, parent))
        for p in td['m_Children']:
            child = S['resolve'](tr.assets_file, p)
            visit(S['resolve'](child.assets_file, child.read_typetree()['m_GameObject']), index)
    visit(root)
    return result

def apk_texture(obj, key):
    file = OUT / (key + '.png')
    file.parent.mkdir(parents=True, exist_ok=True)
    obj.read().image.save(file, format='PNG')
    records.append(dict(output=key, pathID=obj.path_id, bundle=S['cab'].get(obj.assets_file.name),
                        sha256=hashlib.sha256(file.read_bytes()).hexdigest()))
    return 'Majsoul/Wardrobe/' + key

def mesh_data(obj):
    from UnityPy.helpers.MeshHelper import MeshHandler
    helper = MeshHandler(obj.read())
    helper.process()
    return dict(vertices=[n for v in helper.m_Vertices for n in v],
                uv=[n for v in helper.m_UV0 for n in v[:2]],
                groups=[dict(indices=[i for tri in group for i in tri]) for group in helper.get_triangles()])

def model(path, key):
    nodes = []
    for go, td, components, parent in graph(S['obj'](path, 'GameObject')):
        node = dict(name=go.peek_name(), parent=parent, position=vec(td['m_LocalPosition']),
                    rotation=vec(td['m_LocalRotation']), scale=vec(td['m_LocalScale']))
        mf = next((c for c in components if c.type.name == 'MeshFilter'), None)
        renderer = next((c for c in components if c.type.name == 'MeshRenderer'), None)
        if mf and renderer:
            m = S['resolve'](mf.assets_file, mf.read_typetree()['m_Mesh'])
            node['mesh'] = mesh_data(m)
            mats = []
            for ptr in renderer.read_typetree()['m_Materials']:
                material = S['resolve'](renderer.assets_file, ptr)
                props = material.read_typetree()['m_SavedProperties']
                textures = dict(props['m_TexEnvs'])
                tex = next((x['m_Texture'] for x in textures.values() if x['m_Texture']['m_PathID']), None)
                if tex:
                    t = S['resolve'](material.assets_file, tex)
                    mats.append(dict(texture=apk_texture(t, 'Models/Textures/' + str(t.path_id)), transparent='shadow' in material.peek_name().lower()))
            node['materials'] = mats
        nodes.append(node)
    write_json('Models/' + key + '.json', dict(nodes=nodes))
    records.append(dict(source=path, output='Models/' + key + '.json'))
    return 'Majsoul/Wardrobe/Models/' + key

for key, name in [('liqi_fish', '立直棒－秋刀鱼'), ('liqi_scallion', '立直棒－青葱'), ('liqi_bone', '立直棒－骨头')]:
    path = f'myassets/deco/lizhibang/{key}/3d/prefab/{key}.prefab'
    items.append(dict(id=key, category='stick', name=name, builtin=True,
                      icon=icon('extendRes/items/' + key + '.jpg', key), model=model(path, key), duration=3))

# Export complete native particle modules using the established schema translator.
tree = ast.parse((R / 'scripts/import-apk-review-assets.py').read_text('utf8'))
scope = dict(R=R, json=json)
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n, ast.FunctionDef) and n.name == 'editor_json'], type_ignores=[]), '<native-particle-schema>', 'exec'), scope)
native = R / 'unity/Assets/Resources/Majsoul/NativeReview'
def particle_graph(path, key):
    nodes = []
    for go, td, components, parent in graph(S['obj'](path, 'GameObject')):
        node = dict(name=go.peek_name(), parent=parent, position=vec(td['m_LocalPosition']), rotation=vec(td['m_LocalRotation']), scale=vec(td['m_LocalScale']))
        p = next((c for c in components if c.type.name == 'ParticleSystem'), None)
        renderer = next((c for c in components if c.type.name == 'ParticleSystemRenderer'), None)
        if p and renderer:
            data = p.read_typetree();data.pop('m_GameObject', None)
            relative = 'WardrobeData/' + key + '-' + str(p.path_id)
            folder = native / 'WardrobeData';folder.mkdir(exist_ok=True)
            (native / (relative + '.json')).write_text(json.dumps(scope['editor_json']('ParticleSystem', data)), 'utf8')
            node['particle'] = relative + '.json'
            rd = renderer.read_typetree()
            renderer_data = {k: v for k, v in rd.items() if k not in ['m_GameObject', 'm_Materials', 'm_Mesh', 'm_Mesh1', 'm_Mesh2', 'm_Mesh3']}
            node['renderer'] = relative + '-renderer.json'
            (native / node['renderer']).write_text(json.dumps(scope['editor_json']('ParticleSystemRenderer', renderer_data)), 'utf8')
            mat = S['resolve'](renderer.assets_file, rd['m_Materials'][0]);md = mat.read_typetree();props = md['m_SavedProperties']
            texptr = next(v['m_Texture'] for _, v in props['m_TexEnvs'] if v['m_Texture']['m_PathID'])
            tex = S['resolve'](mat.assets_file, texptr)
            texfile = 'WardrobeData/' + str(tex.path_id)
            tex.read().image.save(native / (texfile + '.png'), format='PNG')
            floats = dict(props['m_Floats']);colors = dict(props['m_Colors']);color = colors.get('_TintColor', dict(r=.5, g=.5, b=.5, a=.5))
            node.update(texture=texfile, tint=[color[c] for c in 'rgba'], srcBlend=floats.get('_SrcBlend', 5), dstBlend=floats.get('_DstBlend', 1), renderMode=rd['m_RenderMode'], lengthScale=rd['m_LengthScale'], sortingFudge=rd['m_SortingFudge'])
            shader = S['resolve'](mat.assets_file, md['m_Shader'])
            try:
                state = shader.read_typetree()['m_ParsedForm']['m_SubShaders'][0]['m_Passes'][0]['m_State']['rtBlend0']
                node['srcBlend'], node['dstBlend'] = state['srcBlend']['val'], state['destBlend']['val']
            except (KeyError, IndexError):
                pass
            if rd['m_RenderMode'] == 4:
                m = S['resolve'](renderer.assets_file, rd['m_Mesh']);d = mesh_data(m)
                node['mesh'] = relative + '-mesh.json'
                (native / node['mesh']).write_text(json.dumps(dict(vertices=d['vertices'], uv=d['uv'], indices=[i for g in d['groups'] for i in g['indices']])), 'utf8')
        nodes.append(node)
    (native / (key + '-source.json')).write_text(json.dumps(dict(nodes=nodes)), 'utf8')
    records.append(dict(source=path, output=key + '-source.json', particleLayers=sum('particle' in n for n in nodes)))
    return key

for key, name in [('mpzs_shandian', '鸣牌指示－闪电'), ('mpzs_22event', '鸣牌指示－星光')]:
    particle_graph(f'myassets/deco/effect_mingpaizhishi/{key}/3d/prefab/{key}.prefab', key)
    image_path = ('extendRes/items/' if key == 'mpzs_shandian' else 'extendRes/items2/') + key + '.jpg'
    items.append(dict(id=key, category='callout', name=name, icon=icon(image_path, key), builtin=True, nativeEffect=key, duration=2.6))

items.append(dict(id='riichi-fire', category='riichiEffect', name='立直特效－烈焰', builtin=True,
                  icon=icon('extendRes/items/effect_liqi_fire.jpg', 'riichi-fire'), effect='Majsoul/Actions/riichi-fire-effect', duration=2.3))
items.append(dict(id='ron-tornado', category='winEffect', name='和牌特效－火龙卷', builtin=True,
                  icon=icon('extendRes/items/ron_huolongjuan.jpg', 'ron-tornado'), effect='Majsoul/Actions/fire-effect', duration=2.5))

# Human hand uses its existing original baked clip. Bake the cat's own rig below.
items.append(dict(id='hand-human', category='hand', name='默认手部', builtin=True, icon='Majsoul/Wardrobe/UI/slot_hand',
                  motion='Majsoul/Actions/lizhi-hand', texture='Majsoul/Actions/hand-skin', nailTexture='Majsoul/Actions/hand-nails', duration=2.6))
hand = runpy.run_path(str(R / 'scripts/import-hand-animations.py'))
cat_prefix = 'scene/Assets/Resource/table/hand_cat/'
mesh = fetch(cat_prefix + 'Hand_cat_skin-cat.lm')
avatar = json.loads(fetch(cat_prefix + 'Hand_cat_skin-hand_cat-Hand_cat_skinAvatar.lav'))
Reader = hand['Reader'];reader = Reader(mesh);reader.header()
reader.p = reader.blocks[0][0];reader.string();reader.string();reader.read('h')
vs, vl = reader.read('II');attrs = reader.string().split(',')
sizes = {'POSITION': 12, 'NORMAL': 12, 'UV': 8, 'BLENDWEIGHT': 16, 'BLENDINDICES': 16, 'TANGENT': 16, 'COLOR': 16}
offsets = {};stride = 0
for a in attrs:offsets[a] = stride;stride += sizes[a]
n = vl // stride
import struct
def floats(attr, width):return np.array([struct.unpack_from('<' + str(width) + 'f', mesh, reader.offset + vs + i * stride + offsets[attr]) for i in range(n)])
vertices, uv, normals, weights = floats('POSITION', 3), floats('UV', 2), floats('NORMAL', 3), floats('BLENDWEIGHT', 4)
bones = floats('BLENDINDICES', 4).astype(int)
start, length = reader.read('II');indices = np.frombuffer(mesh, dtype='<u2', count=length // 2, offset=reader.offset + start).copy()
names = [reader.string() for _ in range(reader.read('H'))];reader.p += 8;bs, bl = reader.read('II')
bind = np.frombuffer(mesh, dtype='<f4', count=bl // 4, offset=reader.offset + bs).reshape(-1, 4, 4).transpose(0, 2, 1)
groups = []
for block in reader.blocks[1:]:
    reader.p = block[0];reader.string();reader.read('h');reader.read('II');start, count = reader.read('II');groups.append(indices[start:start + count].tolist())
    for _ in range(reader.read('H')):
        drawstart, drawcount, ds, dl = reader.read('IIII');dictionary = np.frombuffer(mesh, dtype='u1', count=dl, offset=reader.offset + ds)
        vertexids = np.unique(indices[drawstart:drawstart + drawcount]);bones[vertexids] = dictionary[bones[vertexids]]
nodes = []
def visit(node, parent=-1, path=''):
    name = node['props']['name'];path = path + '/' + name if path else name;index = len(nodes);nodes.append((name, parent, path, node['customProps']))
    for child in node.get('child', []):visit(child, index, path)
visit(avatar['rootNode'])
rig = dict(vertices=vertices, uv=uv, normals=normals, weights=weights, bones=bones, names=names, bind=bind,
           submeshes=groups + groups if len(groups) == 1 else groups, nodes=nodes, nodeids={n[0]: i for i, n in enumerate(nodes)})
hand['bake'].__globals__['fetch'] = fetch
hand['bake'].__globals__['PREFIX'] = cat_prefix
for stem, source, focus in [('cat-hand', 'Lizhi-LiZhi', 'node_liqibang'),
                            ('cat-discard', 'Dapai-Dapai', 'node_tile'),
                            ('cat-riichi', 'Dapai_heng-LiQiPai', 'node_tile'),
                            ('cat-meld', 'Fulu-Fulu', 'node_tile'),
                            ('cat-tsumo', 'Zimo-Zimo', 'node_tile')]:
    data, payload, land, points = hand['bake'](rig, 'Anim_cat_' + source + '.lani', focus)
    if len(groups) == 1:
        data['nails'] = []
    write_json('Motion/' + stem + '.json', dict(data, landTime=land))
    (OUT / ('Motion/' + stem + '-frames.bytes')).write_bytes(payload)
for key, title, texture_file in [('hand-cat', '手部－猫爪', 'cat.png'), ('hand-cat-orange', '手部－橘猫爪', 'cat_orange.png')]:
    items.append(dict(id=key, category='hand', name=title, builtin=True, icon=icon('extendRes/items/' + key.replace('-', '_') + '.jpg', key),
                      motion='Majsoul/Wardrobe/Motion/cat-hand', texture=image(cat_prefix + texture_file, 'Motion/' + key), duration=2.6))
for key, name, path in [('music-riichi', '立直音乐－原曲', 'audio/music/liqi1.mp3'), ('music-8bit', '立直音乐－八位音色', 'audio/music/liqi_8bit.mp3')]:
    audio_file = OUT / ('Audio/' + key + '.mp3');audio_file.parent.mkdir(parents=True, exist_ok=True);audio_file.write_bytes(fetch(path))
    items.append(dict(id=key, category='music', name=name, builtin=True,
                      icon=icon('extendRes/items/liqibgm.jpg', key), audio='Majsoul/Wardrobe/Audio/' + key, duration=8))

catalog = dict(schema=1, ui=base['ui'], items=items)
native_names = R / 'build/wardrobe-reference-r2/original-items.json'
if native_names.exists():
    names = {r['view']['res_name']: r['item']['name_chs'] for r in json.loads(native_names.read_text('utf8')) if r.get('item') and r['item'].get('name_chs')}
    aliases = {'ron-tornado': 'ron_huolongjuan', 'riichi-fire': 'effect_liqi_fire', 'hand-cat-orange': 'hand_cat_orange', 'background-spring': 'beijing_chunying'}
    for i in items:
        res = aliases.get(i['id'], i['id'])
        if res in names:
            i['name'] = names[res]
(OUT / 'catalog.json').write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + '\n', 'utf8')
(OUT / 'provenance-r2.json').write_text(json.dumps(dict(apk='Majsoul_4.0.16_MC.apk', apkRecords=records, optionalSourceManifest='resversion0.11.252.w.json', optionalSources=sources), ensure_ascii=False, indent=2) + '\n', 'utf8')
print('Decoration r2 imported:', len(items), 'items;', len(sources), 'fixed-version optional inputs', flush=True)
