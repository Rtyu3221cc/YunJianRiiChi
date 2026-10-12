"""Import Majsoul's default screen-tap effect (scene/effect_click_default.lh).

Layer data comes from the official CDN scene; textures from the supplied APK:
petals use myassets/necessary/texture/yinghua.png (already UiEffects/native-ui-floower_00000.png),
the flash uses myassets/common3d/texture/shanguang.png (already Actions/win-shanguang.png).
scene_click_effect.ls renders it with an orthographic camera 18 units high.
Output follows the WinTileEffect node/layer format used by the other UiEffects.
"""
from pathlib import Path
import hashlib, json, struct, urllib.request
R = Path(__file__).resolve().parent.parent; OUT = R / 'unity/Assets/Resources/Majsoul/UiEffects'
CACHE = R / 'build/cdn/click'; CACHE.mkdir(parents=True, exist_ok=True)
index = json.loads((R / 'build/reference/public/resversion0.11.252.w.json').read_text(encoding='utf8'))['res']
records = []
def fetch(path):
    url = 'https://game.maj-soul.com/1/' + index[path]['prefix'] + '/' + path; dest = CACHE / Path(path).name
    if not dest.exists(): dest.write_bytes(urllib.request.urlopen(url, timeout=30).read())
    data = dest.read_bytes(); records.append({'source': url, 'sha256': hashlib.sha256(data).hexdigest()}); return data
def keys(items, scale=1): return [{'time': k['key'], 'value': k['value'] * scale} for k in items]
def pick(c, name):
    t = c['start' + name + 'Type']; v = c['start' + name + 'Constant']
    return (c['start' + name + 'ConstantMin'], c['start' + name + 'ConstantMax']) if t == 2 else (v, v)
def plane(data):
    # LAYAMODEL:0301 plane: 4 vertices of position(3) normal(3) uv(2) tangent(4), then 6 uint16 indices.
    start = data.index(struct.pack('<3f', -1.27, 1.27, 0)); verts = [struct.unpack_from('<12f', data, start + i * 48) for i in range(4)]
    indices = list(struct.unpack_from('<6H', data, len(data) - 12)); assert sorted(set(indices)) == [0, 1, 2, 3], indices
    # Laya -> Unity: mirror Z (as for node positions) and flip V.
    return {'positions': [x for v in verts for x in (v[0], v[1], -v[2])], 'uv': [x for v in verts for x in (v[6], 1 - v[7])], 'indices': indices}
scene = json.loads(fetch('scene/scene_click_effect.ls'))
camera = next(n for n in scene['child'] if n['type'] == 'Camera')['props']; assert camera['orthographic'] and camera['orthographicVerticalSize'] == 18
source = json.loads(fetch('scene/effect_click_default.lh'))
# A texture name with a folder is resolved from Resources/Majsoul (WinTileEffect).
textures = {'Assets/Resource/entrance/floower_00000.lmat': 'native-ui-floower_00000', 'Assets/Resource/effect/texture/paimian.lmat': 'Actions/win-shanguang'}
nodes, layers = [], []
def walk(n, parent=-1):
    c = n.get('customProps', {}); t = c.get('translate', [0, 0, 0]); i = len(nodes)
    nodes.append({'name': n['props']['name'], 'parent': parent, 'position': [t[0], t[1], -t[2]], 'rotation': c.get('rotation', [0, 0, 0, 1]), 'scale': c.get('scale', [1, 1, 1])})
    if n['type'] == 'ShuriKenParticle3D':
        mp = c['material']['path']; mat = json.loads(fetch('scene/' + mp))['props']
        assert mat['srcBlend'] == 770 and not mat['depthWrite']
        co = c['colorOverLifetime']['color']['gradient']; assert all(x['value'] == [1, 1, 1] for x in co['rgbs'])
        ts = c.get('textureSheetAnimation', {}); tiles = ts.get('tiles', [1, 1]) if ts.get('enable') else [1, 1]
        frames = keys(ts['frame']['overTime']['frames'], 1 / (tiles[0] * tiles[1])) if ts.get('enable') else []
        life, size, speed, rot = pick(c, 'Lifetime'), pick(c, 'Size'), pick(c, 'Speed'), pick(c, 'Rotation')
        sh = c.get('shape', {}); color = (c['startColorConstantMin'], c['startColorConstantMax']) if c['startColorType'] == 2 else (c['startColorConstant'],) * 2
        layer = dict(name=n['props']['name'], node=i, duration=c['duration'], delay=c['startDelay'], gravity=c['gravityModifier'], rate=c['emission']['emissionRate'],
                     maxParticles=c['maxParticles'], renderMode=c['renderMode'], stretch=c['stretchedBillboardLengthScale'], colorMin=color[0], colorMax=color[1],
                     size3d=[], size3dMin=[], alpha=keys(co['alphas']), size=[], sizeX=[], sizeY=[], sizeZ=[], tiles=tiles, frames=frames, cycles=ts.get('cycles', 1),
                     bursts=[{'time': b['time'], 'min': b['min'], 'max': b['max']} for b in c['emission']['bursts']], texture=textures[mp],
                     tint=next(v['value'] for v in mat['vectors'] if v['name'] == 'tintColor'), blendDst=mat['dstBlend'],
                     lifeMin=life[0], lifeMax=life[1], sizeMin=size[0], sizeMax=size[1], speedMin=speed[0], speedMax=speed[1], rotationMin=rot[0], rotationMax=rot[1],
                     shape=sh.get('shapeType', -1) if sh.get('enable') else -1, radius=sh.get('circleRadius', .01), coneAngle=sh.get('coneAngle', 0), angularSpeed=0,
                     velocityMin=[], velocityMax=[], randomFrame=False, box=[], nativeShape=True, randomDirection=0,
                     colors=[{'time': k['key'], 'color': k['value'] + [1]} for k in co['rgbs']])
        if c['renderMode'] == 4: layer['mesh'] = plane(fetch('scene/' + c['meshPath']))
        layers.append(layer)
    for ch in n.get('child', []): walk(ch, i)
walk(source)
assert [l['name'] for l in layers] == ['Particle System', 'gaoliang'], [l['name'] for l in layers]
for path in [OUT / 'native-ui-floower_00000.png', OUT.parent / 'Actions/win-shanguang.png']: assert path.exists(), path
out = OUT / 'effect_click_default.json'; out.write_text(json.dumps({'nodes': nodes, 'layers': layers}, ensure_ascii=False, indent=1) + '\n', encoding='utf8')
records.append({'output': 'UiEffects/' + out.name, 'sha256': hashlib.sha256(out.read_bytes()).hexdigest(), 'cameraOrthographicHeight': 18,
                'textures': {'native-ui-floower_00000': 'APK myassets/necessary/texture/yinghua.png', 'win-shanguang': 'APK myassets/common3d/texture/shanguang.png'},
                'adaptation': 'Bursts use their maximum count, as in the other WinTileEffect imports. Laya shapeType 7 maps to the existing circle emitter.'})
(OUT / 'click-source.json').write_text(json.dumps(records, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
print('Imported', len(layers), 'click layers:', [(l['name'], l['bursts'], l['lifeMin'], l['lifeMax']) for l in layers])
