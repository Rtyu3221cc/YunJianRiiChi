"""Complete the two-choice test pack with source ice-riichi and default win FX."""
import ast, json, runpy
from pathlib import Path
R = Path(__file__).resolve().parent.parent
s = runpy.run_path(str(R / 'scripts/import-wardrobe-r2.py'))
out = s['OUT']
actions = R / 'unity/Assets/Resources/Majsoul/Actions'
src = (R / 'scripts/import-riichi-fire.py').read_text('utf8')
tree = ast.parse(src)
ns = dict(s, CACHE=R/'build/wardrobe-reference-r3', OUT=actions, exported={}, nodes=[], layers=[])
ns['CACHE'].mkdir(parents=True, exist_ok=True)
# Preserve the full original node graph and module conversion used by fire-riichi.
# Texture retrieval uses the fixed source manifest/cache from the wardrobe importer.
def tex(path):
    key = 'riichi-ice-' + Path(path).stem
    s['image']('scene/' + path, 'Effects/' + key)
    return 'Wardrobe/Effects/' + key
ns['texture'] = tex
for n in tree.body:
    if isinstance(n, ast.FunctionDef) and n.name in ['keys','pick','mesh','walk']:
        exec(compile(ast.Module(body=[n], type_ignores=[]), '<source-particle-converter>', 'exec'), ns)
ns['struct'] = __import__('struct')
source = json.loads(s['fetch']('scene/effect_liqi_ice.lh'))
ns['walk'](source)
(actions/'riichi-ice-effect.json').write_text(json.dumps(dict(nodes=ns['nodes'],layers=ns['layers']),ensure_ascii=False,indent=1)+'\n','utf8')
# The bundled default win graph was already extracted from the APK, in the
# earlier flat-node format. Upgrade that data without substituting a fake effect.
j = json.loads((actions/'win-effect.json').read_text('utf8'))
nodes = []
for i,l in enumerate(j['layers']):
    nodes.append(dict(name=l['name'],parent=-1,position=l.pop('position'),rotation=l.pop('rotation'),scale=[1,1,1]))
    l.update(node=i,colorMin=l['color'],colorMax=l.pop('color'),blendDst=1,colors=[],size3d=[],size3dMin=[],velocityMin=[],velocityMax=[],sizeX=[],sizeY=[],sizeZ=[],frames=[],cycles=1)
j['nodes'] = nodes
(actions/'default-win-effect.json').write_text(json.dumps(j,ensure_ascii=False,indent=1)+'\n','utf8')
catalog = json.loads((out/'catalog.json').read_text('utf8'))
catalog['items'] += [
    dict(id='riichi-ice',category='riichiEffect',name='立直特效－冰霜',builtin=True,icon=s['icon']('extendRes/items/effect_liqi_ice.jpg','riichi-ice'),effect='Majsoul/Actions/riichi-ice-effect',duration=2.6,cue=''),
    dict(id='win-default',category='winEffect',name='和牌特效－雷光',builtin=True,icon=s['icon']('extendRes/items/ron_lightening.jpg','win-default'),effect='Majsoul/Actions/default-win-effect',duration=2.6,effectScale=.09,cue='')]
for i in catalog['items']:
    if i['id']=='riichi-fire':i['cue']='effect_rich_fire'
    if i['id']=='ron-tornado':i['cue']='ron_huolongjuan'
(out/'catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n','utf8')
(out/'provenance-r3.json').write_text(json.dumps(dict(note='Original source conversion; five cat action clips, ice riichi, bundled default win',optionalSources=s['sources']),ensure_ascii=False,indent=2)+'\n','utf8')
print('Wardrobe r3 complete:',len(catalog['items']),'items')
