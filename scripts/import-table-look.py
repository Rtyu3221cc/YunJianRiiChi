"""Export the 1012-2 table look directly from the supplied APK (plus one official CDN title image).

scene_mj supplies the tile light direction, the Catfood toon/outline parameters, the
per-tile effect_shadow quad and the room/sun_shine particle systems (whole serialized
modules, imported by Editor/ImportNativeReview.cs). UI_DesktopInfo supplies bg_type.
"""
from pathlib import Path
import sys, json, zipfile, warnings, hashlib, io, urllib.request
R = Path(__file__).resolve().parent.parent; OUT = R / 'unity/Assets/Resources/Majsoul/TableLook'; OUT.mkdir(exist_ok=True)
sys.path.insert(0, str(R / 'tools/asset-inspector')); import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
UnityPy.config.FALLBACK_UNITY_VERSION = '2022.3.62f2c1'; warnings.filterwarnings('ignore')
rows = [json.loads(s) for s in (R / 'build/reference/fast-catalog.jsonl').read_text(encoding='utf8').splitlines()]
cabmap = json.loads((R / 'build/reference/tile-study/cab-map.json').read_text(encoding='utf8')); cache = {}; records = []
z = zipfile.ZipFile(R / 'dist/Majsoul_4.0.16_MC.apk')
def af(bundle):
    if bundle not in cache: cache[bundle] = next(o.assets_file for o in UnityPy.load(z.read(bundle)).objects)
    return cache[bundle]
def resolve(file, ptr):
    if ptr['m_PathID'] == 0: return None
    if ptr['m_FileID']: file = af(cabmap[Path(file.externals[ptr['m_FileID'] - 1].path).name])
    return file.objects[ptr['m_PathID']]
def write(name, data, source=None, **extra):
    file = OUT / name; file.parent.mkdir(parents=True, exist_ok=True); file.write_bytes(data)
    rec = {'output': name, 'sha256': hashlib.sha256(data).hexdigest(), **extra}
    if source is not None: rec.update(bundle=cabmap.get(source.assets_file.name), pathID=source.path_id, type=source.type.name)
    records.append(rec); return name
def js(name, value, source=None): return write(name, (json.dumps(value, ensure_ascii=False, indent=2, default=str) + '\n').encode('utf8'), source)
def png(name, obj): buf = io.BytesIO(); obj.read().image.save(buf, format='PNG'); return write(name, buf.getvalue(), obj)
def editor_json(type_name, data):
    # Same envelope as import-apk-review-assets.py: EditorJsonUtility needs explicit nested versions.
    schema = json.loads((R / 'scripts/native-review' / ('editor-particle-schema.json' if type_name == 'ParticleSystem' else 'editor-renderer-schema.json')).read_text(encoding='utf8'))[type_name]
    def merge(default, value):
        if isinstance(value, dict):
            if 'm_PathID' in value: return {'instanceID': 0}
            result = dict(default) if isinstance(default, dict) else {}
            for k, v in value.items(): result[k] = merge(result.get(k), v)
            return result
        return list(value) if isinstance(value, bytes) else value
    return {type_name: merge(schema, data)}
def find(path):
    row = next(r for r in rows if path in r['paths']); file = af(row['bundle'])
    return next(o for o in file.objects.values() if o.type.name == 'GameObject' and o.peek_name().lower() == Path(path).stem.lower())
def tree(root):
    result = []
    def walk(go, parent=-1, path=''):
        gd = go.read_typetree(); objs = [resolve(go.assets_file, c['component']) for c in gd['m_Component']]
        t = next(o for o in objs if o.type.name in ('Transform', 'RectTransform')); i = len(result); p = path + '/' + gd['m_Name']; result.append((go, t, objs, parent, p))
        for c in t.read_typetree()['m_Children']:
            tr = resolve(t.assets_file, c); walk(resolve(tr.assets_file, tr.read_typetree()['m_GameObject']), i, p)
    walk(root); return result
def vec(d): return [d[k] for k in ('xyzw' if 'w' in d else 'xyz' if 'z' in d else 'xy')]
def color(d): return [d[k] for k in 'rgba']
def material(renderer, index=0):
    mat = resolve(renderer.assets_file, renderer.read_typetree()['m_Materials'][index]); md = mat.read_typetree(); sp = md['m_SavedProperties']
    return mat, md, dict(sp['m_Floats']), dict(sp['m_Colors']), {k: v for k, v in sp['m_TexEnvs']}

scene = tree(find('myassets/scenes/mj/mjtable/prefab/scene_mj.prefab')); by_path = {p: (go, t, objs, parent) for go, t, objs, parent, p in scene}
look = {'units': 'source scene metres; convert with (-x, y, -z) * .33 / .02947767'}
# Directional light: only its direction feeds _WorldSpaceLightPos0 in the Catfood toon shaders.
go, t, objs, _ = by_path['/scene_mj/light']; light = next(o for o in objs if o.type.name == 'Light'); ld = light.read_typetree()
look['light'] = {'rotation': vec(t.read_typetree()['m_LocalRotation']), 'type': ld['m_Type'], 'color': color(ld['m_Color']), 'intensity': ld['m_Intensity'], 'cullingMask': ld['m_CullingMask']['m_Bits']}
# Tile body/face toon parameters and the outline colour actually output by Catfood/Outline (_Tint).
go, t, objs, _ = by_path['/scene_mj/room/all/mjp_new']; mr = next(o for o in objs if o.type.name == 'MeshRenderer')
mid = material(mr, 0); face = material(mr, 1)
look['toon'] = {'body': {'material': mid[1]['m_Name'], 'splitThreshold': mid[2]['_SplitThreshold'], 'colorLight': color(mid[3]['_ColorLight']), 'colorUnlight': color(mid[3]['_ColorUnlight']), 'tint': color(mid[3]['_Tint']),
                         'tileTexScale': vec(mid[4]['_TileTex']['m_Scale']), 'tileTexOffset': vec(mid[4]['_TileTex']['m_Offset'])},
                'face': {'material': face[1]['m_Name'], 'splitThreshold': face[2]['_SplitThreshold'], 'colorLight': color(face[3]['_ColorLight']), 'colorUnlight': color(face[3]['_ColorUnlight']), 'tint': color(face[3]['_Tint']), 'mipMapBias': face[2].get('_MipMapBias', 0)}}
go, t, objs, _ = by_path['/scene_mj/room/all/mjp_new/maque_outline']; om = material(next(o for o in objs if o.type.name == 'MeshRenderer'))
look['outline'] = {'material': om[1]['m_Name'], 'tint': color(om[3]['_Tint']), 'scale': vec(t.read_typetree()['m_LocalScale'])}
# Per-tile shadow quad: effect_shadow (scale .01) > plane, with the plane mesh extents.
es = by_path['/scene_mj/room/all/mjp_new/effect_shadow']; pl = by_path['/scene_mj/room/all/mjp_new/effect_shadow/plane']
mf = next(o for o in pl[2] if o.type.name == 'MeshFilter'); mesh = resolve(mf.assets_file, mf.read_typetree()['m_Mesh']); mh = MeshHandler(mesh.read()); mh.process()
xs, ys, zs = zip(*mh.m_Vertices); sm = material(next(o for o in pl[2] if o.type.name == 'MeshRenderer'))
tex = resolve(sm[0].assets_file, sm[4]['_MainTex']['m_Texture'])
look['shadow'] = {'material': sm[1]['m_Name'], 'texture': png('Textures/' + tex.peek_name() + '.png', tex)[:-4], 'tint': color(sm[3]['_Tint']), 'renderQueue': sm[1]['m_CustomRenderQueue'],
                  'groupPosition': vec(es[1].read_typetree()['m_LocalPosition']), 'groupRotation': vec(es[1].read_typetree()['m_LocalRotation']), 'groupScale': vec(es[1].read_typetree()['m_LocalScale']),
                  'planePosition': vec(pl[1].read_typetree()['m_LocalPosition']), 'planeRotation': vec(pl[1].read_typetree()['m_LocalRotation']),
                  'mesh': mesh.peek_name(), 'meshMin': [min(xs), min(ys), min(zs)], 'meshMax': [max(xs), max(ys), max(zs)],
                  'tileBounds': [.02947767, .03913179, .01913448]}
# room/sun_shine: three additive streak/dust emitters, kept as whole serialized modules.
nodes = []; base = '/scene_mj/room/sun_shine'
for go, t, objs, parent, p in scene:
    if not p.startswith(base): continue
    td = t.read_typetree(); node = {'name': go.peek_name(), 'parent': -1 if p == base else next(i for i, n in enumerate(nodes) if n['path'] == p.rsplit('/', 1)[0]), 'path': p,
                                    'position': vec(td['m_LocalPosition']), 'rotation': vec(td['m_LocalRotation']), 'scale': vec(td['m_LocalScale'])}
    particle = next((o for o in objs if o.type.name == 'ParticleSystem'), None); renderer = next((o for o in objs if o.type.name == 'ParticleSystemRenderer'), None)
    if particle:
        data = particle.read_typetree(); data.pop('m_GameObject', None)
        node['particle'] = js('Data/sun_shine-' + str(particle.path_id) + '.json', editor_json('ParticleSystem', data), particle)
        rd = renderer.read_typetree(); native = {k: v for k, v in rd.items() if k not in ('m_GameObject', 'm_Materials', 'm_Mesh', 'm_Mesh1', 'm_Mesh2', 'm_Mesh3')}
        node['renderer'] = js('Data/sun_shine-' + str(renderer.path_id) + '-renderer.json', editor_json('ParticleSystemRenderer', native), renderer)
        mat, md, floats, colors, texs = material(renderer); shader = resolve(mat.assets_file, md['m_Shader']); ptex = resolve(mat.assets_file, texs['_MainTex']['m_Texture'])
        tname = 'Textures/' + ptex.peek_name() + '.png'
        if not (OUT / tname).exists() or not any(r['output'] == tname for r in records): png(tname, ptex)
        state = shader.read_typetree()['m_ParsedForm']['m_SubShaders'][0]['m_Passes'][0]['m_State']
        node.update(texture=tname[:-4], tint=color(colors.get('_TintColor', {'r': .5, 'g': .5, 'b': .5, 'a': .5})), srcBlend=state['rtBlend0']['srcBlend']['val'], dstBlend=state['rtBlend0']['destBlend']['val'],
                    renderMode=rd['m_RenderMode'], shaderName=shader.read().m_ParsedForm.m_Name, material=md['m_Name'])
    nodes.append(node)
for n in nodes: n.pop('path')
js('sun_shine-source.json', {'nodes': nodes})
# UI_DesktopInfo/container_lefttop/gamemode background band.
for row in rows:
    if not any('atlas_mj_main_common_' in p for p in row['paths']): continue
    for o in af(row['bundle']).objects.values():
        if o.type.name == 'Sprite' and o.peek_name() == 'bg_type': png('Sprites/bg_type.png', o)
assert any(r['output'] == 'Sprites/bg_type.png' for r in records)
# Default player title: not in the APK; official CDN, XOR 0x49 extendRes obfuscation.
url = 'https://game.maj-soul.com/1/v0.10.306.w/lang/chs/extendRes/title/notitle.png'
raw = urllib.request.urlopen(url, timeout=30).read(); image = bytes(b ^ 0x49 for b in raw)
assert hashlib.sha256(image).hexdigest() == 'd8c31bb524e348e3c0e6e1bff989eb0042354b620afc56f7ac1c5105c70b2794', 'notitle.png changed upstream'
write('Sprites/notitle.png', image, url=url, encoding='extendRes XOR 0x49', encodedSha256=hashlib.sha256(raw).hexdigest())
js('lighting.json', look)
js('source.json', {'apk': 'Majsoul_4.0.16_MC.apk', 'assets': records,
                   'limitations': ['Catfood toon/outline maths ported from the APK GLES3 programs; Unity ShaderLab wrappers are project code.',
                                   'Cloth brightness, river jitter and UI placement adapters are fitted to the 1012-2 reference screenshot, not recovered scripts.']})
print('Exported', len(records), 'records;', len(nodes), 'sun_shine nodes; shadow mesh', look['shadow']['meshMin'], look['shadow']['meshMax'])
