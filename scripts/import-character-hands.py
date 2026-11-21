"""Convert the previously audited character-hand bundles into portable source data.

No download or public catalog changes. Unity samples the original compiled clips
in CharacterHandImport; meshes, bones, materials and role bindings come from the
same source snapshot used by the earlier hand inventory.
"""
from pathlib import Path
import argparse
import hashlib
import io
import json
import shutil
import sys
import warnings

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / 'tools/asset-inspector'))
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
UnityPy.config.FALLBACK_UNITY_VERSION = '2022.3.62f2c1'
warnings.filterwarnings('ignore')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', type=Path, default=ROOT.parent / 'YunJianRiiChi_Tmp/Majsoul_角色手型與手部裝飾')
    parser.add_argument('--output', type=Path, default=ROOT / 'build/character-hands-r37/source')
    args = parser.parse_args()
    source, output = args.source.resolve(), args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    for name in ['bundles', 'textures', 'meshes', 'materials']:
        (output / name).mkdir(exist_ok=True)
    read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
    hands = [x for x in read(source / 'data/hands.json') if x['character_ids'] and x['res_name'] not in ['hand_human', 'hand_man', 'hand_dog']]
    manifest = read(source / 'source-manifest.json')
    bundle_rows = manifest['bundles']
    if isinstance(bundle_rows, dict):
        bundle_rows = list(bundle_rows.values())
    for row in bundle_rows:
        path = source / row['file']
        assert path.stat().st_size == row['bytes'] and hashlib.sha256(path.read_bytes()).hexdigest() == row['sha256'], 'Source bundle changed: ' + row['file']
    used = set(x for hand in hands for x in hand['bundle_indices'])
    # The audit already lists dependency closures. Load all local bundles so
    # external CAB references remain resolvable, including shared shaders.
    bundles = sorted((source / 'bundles').rglob('*.majset'))
    unique = {p.name: p for p in bundles}
    env = UnityPy.load(*(str(p) for p in unique.values()))
    files = {o.assets_file.name: o.assets_file for o in env.objects}
    refs, meshes, materials, textures, clips = {}, {}, {}, {}, {}

    def resolve(owner, ptr):
        if not ptr or not ptr.get('m_PathID'):
            return None
        file = owner.assets_file
        if ptr['m_FileID']:
            name = file.externals[ptr['m_FileID'] - 1].path.replace('\\', '/').rsplit('/', 1)[-1]
            if name in ['unity_builtin_extra', 'unity default resources']:
                return None
            file = files[name]
        return file.objects[ptr['m_PathID']]

    def ident(obj):
        key = obj.assets_file.name + ':' + str(obj.path_id)
        return hashlib.sha256(key.encode()).hexdigest()[:20]

    def vec(v):
        return [v[c] for c in ('xyzw' if 'w' in v else 'xyz')]

    def save(relative, data):
        (output / relative).write_text(json.dumps(data, ensure_ascii=False, separators=(',', ':')), encoding='utf-8')
        return relative

    def texture(obj):
        if obj is None:
            return ''
        key = ident(obj)
        if key not in textures:
            path = 'textures/' + key + '.png'
            obj.read().image.save(output / path)
            textures[key] = path
        return textures[key]

    def material(obj):
        key = ident(obj)
        if key not in materials:
            raw = obj.read_typetree()
            props = raw['m_SavedProperties']
            tex = dict(props['m_TexEnvs'])
            colors = dict(props['m_Colors'])
            floats = dict(props['m_Floats'])
            main = tex.get('_MainTex', tex.get('_BaseMap', {}))
            t = resolve(obj, main.get('m_Texture'))
            tint = colors.get('_Color', colors.get('_BaseColor', {'r': 1, 'g': 1, 'b': 1, 'a': 1}))
            row = dict(key=key, name=raw['m_Name'], texture=texture(t), tint=[tint[c] for c in 'rgba'],
                       scale=vec(main.get('m_Scale', {'x': 1, 'y': 1, 'z': 0}))[:2] if 'z' in main.get('m_Scale', {}) else [main.get('m_Scale', {}).get(c, 1) for c in 'xy'],
                       offset=[main.get('m_Offset', {}).get(c, 0) for c in 'xy'],
                       transparent='shadow' in raw['m_Name'].lower(),
                       sourceShader=resolve(obj, raw['m_Shader']).read_typetree()['m_ParsedForm']['m_Name'])
            # Asuna's grey albedo is authored for FxBRDF2, not an unlit surface.
            # Keep the other hand shaders unchanged until each is adapted.
            if hand['res_name'] == 'hand_yasina' and row['sourceShader'] == 'FizzFX/FxBRDF2':
                row['brdf'] = True
                row['textures'] = []
                for name in ['_MainTex', '_BumpMap', '_ORMMap', '_RampMap', '_MatCapTex']:
                    image = resolve(obj, tex.get(name, {}).get('m_Texture'))
                    if image is None:
                        continue
                    data = image.read_typetree()
                    settings = data['m_TextureSettings']
                    row['textures'].append(dict(name=name, file=texture(image), srgb=data['m_ColorSpace'] == 1,
                                                mipmaps=data['m_MipCount'] > 1, filter=settings['m_FilterMode'],
                                                wrap=settings['m_WrapU']))
                row['floats'] = [dict(name=name, value=value) for name, value in floats.items()]
                row['colors'] = [dict(name=name, value=[value[c] for c in 'rgba']) for name, value in colors.items()]
            materials[key] = row
        return key

    def mesh(obj):
        key = ident(obj)
        if key not in meshes:
            helper = MeshHandler(obj.read()); helper.process()
            raw = obj.read_typetree()
            normals = helper.m_Normals or []
            weights = helper.m_BoneWeights or []
            def normalized(v):
                a = list(v)
                if a[3] < 0:
                    a[3] = max(0, 1 - sum(a[:3]))
                total = sum(a)
                return [x / total for x in a] if total else [1, 0, 0, 0]
            data = dict(key=key, vertices=[n for v in helper.m_Vertices for n in v],
                        tangents=[n for v in (helper.m_Tangents or []) for n in v],
                        normals=[n for v in normals for n in v], uv=[n for v in helper.m_UV0 for n in v[:2]],
                        groups=[{'indices': [n for tri in group for n in tri]} for group in helper.get_triangles()],
                        weights=[n for v in weights for n in normalized(v)], boneIndices=[int(n) for v in (helper.m_BoneIndices or []) for n in v],
                        bindposes=[matrix['e' + str(i) + str(j)] for matrix in raw['m_BindPose'] for i in range(4) for j in range(4)])
            save('meshes/' + key + '.json', data)
            meshes[key] = 'meshes/' + key + '.json'
        return key

    def clip(obj):
        key = ident(obj)
        if key not in clips:
            name = obj.assets_file.parent.name
            if name not in unique:
                name = Path(name).name
            shutil.copyfile(unique[name], output / 'bundles' / name)
            raw = obj.read_typetree()
            clips[key] = dict(name=raw['m_Name'], bundle=name, duration=raw['m_MuscleClip']['m_StopTime'])
        return clips[key]

    def graph(root):
        nodes, node_objects, sources = [], [], []
        def visit(go, parent=-1):
            raw = go.read_typetree()
            components = [resolve(go, c['component']) for c in raw['m_Component']]
            transform = next(x for x in components if x.type.name == 'Transform')
            pose = transform.read_typetree()
            index = len(nodes)
            node = dict(name=raw['m_Name'], parent=parent, position=vec(pose['m_LocalPosition']), rotation=vec(pose['m_LocalRotation']), scale=vec(pose['m_LocalScale']), active=bool(raw['m_IsActive']))
            nodes.append(node); node_objects.append((transform, components))
            for comp in components:
                if comp.type.name == 'Animator':
                    controller = resolve(comp, comp.read_typetree()['m_Controller'])
                    if controller:
                        cd = controller.read_typetree()
                        names = dict(cd['m_TOS'])
                        states = cd['m_Controller']['m_StateMachineArray'][0]['data']['m_StateConstantArray']
                        for state in states:
                            state = state['data']
                            action = next((a for a in ['Dapai', 'LiQiPai', 'LiZhi', 'Fulu', 'Zimo'] if a.lower() == names[state['m_NameID']].lower()), None)
                            if action is None:
                                continue
                            tree = state['m_BlendTreeConstantArray'][0]['data']['m_NodeArray']
                            assert len(tree) == 1, (action, 'Unexpected blended hand action')
                            clip_index = tree[0]['data']['m_ClipID']
                            c = resolve(controller, cd['m_AnimationClips'][clip_index])
                            if c:
                                row = dict(clip(c)); row['owner'] = index
                                row['action'] = action
                                sources.append(row)
            for ptr in pose['m_Children']:
                child = resolve(transform, ptr)
                visit(resolve(child, child.read_typetree()['m_GameObject']), index)
        visit(root)
        lookup = {(tr.assets_file.name, tr.path_id): i for i, (tr, _) in enumerate(node_objects)}
        def bone(owner, ptr):
            tr = resolve(owner, ptr)
            return -1 if tr is None else lookup[(tr.assets_file.name, tr.path_id)]
        for node, (tr, components) in zip(nodes, node_objects):
            renderer = next((c for c in components if c.type.name in ['SkinnedMeshRenderer', 'MeshRenderer']), None)
            if renderer is None:
                continue
            rd = renderer.read_typetree()
            mf = next((c for c in components if c.type.name == 'MeshFilter'), None)
            obj = resolve(renderer, rd.get('m_Mesh')) if renderer.type.name == 'SkinnedMeshRenderer' else resolve(mf, mf.read_typetree()['m_Mesh']) if mf else None
            if obj is None:
                continue
            node.update(mesh=mesh(obj), skinned=renderer.type.name == 'SkinnedMeshRenderer', materials=[material(resolve(renderer, ptr)) for ptr in rd['m_Materials']],
                        bones=[bone(renderer, ptr) for ptr in rd.get('m_Bones', [])], rootBone=bone(renderer, rd.get('m_RootBone')), enabled=bool(rd['m_Enabled']))
            # Table/blob shadows are separate from the visible character hand.
            node['shadow'] = any(word in node['name'].lower() for word in ['shadow', 'yingz']) or all('shadow' in materials[m]['name'].lower() or 'yingz' in materials[m]['name'].lower() or materials[m]['sourceShader'] == 'Catfood/Additive Transparent Hand' for m in node['materials'])
        return nodes, sources

    containers = {p.lower(): ptr for p, ptr in env.container.items()}
    rows = []
    for hand in hands:
        variants = []
        for path in hand['prefabs']:
            obj = containers[path.lower()].deref()
            nodes, animations = graph(obj)
            assert {x['action'] for x in animations} >= {'Dapai', 'LiQiPai', 'LiZhi', 'Fulu', 'Zimo'}, (hand['res_name'], [x['name'] for x in animations])
            variants.append(dict(name=Path(path).stem, nodes=nodes, animations=animations))
        rows.append(dict(key=hand['res_name'], id=hand['id'], characters=hand['character_ids'], timing=hand['timing'], variants=variants))
        print(hand['res_name'], len(variants), flush=True)
    save('manifest.json', dict(hands=rows, materials=list(materials.values()), meshes=[dict(key=k, file=v) for k, v in meshes.items()]))
    save('provenance.json', dict(sourceManifestSHA256=hashlib.sha256((source / 'source-manifest.json').read_bytes()).hexdigest(),
                              characterHands=len(rows), roles=sum(len(h['characters']) for h in rows),
                              bundles=[dict(file=p.name, sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in sorted((output / 'bundles').glob('*'))]))


if __name__ == '__main__':
    main()
