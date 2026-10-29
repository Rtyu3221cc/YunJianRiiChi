import collections,json,runpy
from pathlib import Path
R=Path(__file__).resolve().parent.parent
S=runpy.run_path(str(R/'scripts/study-characters-1018.py'))
plan=json.loads((R/'build/wardrobe-r16/plan.json').read_text('utf8'))
report=[]
def walk(go):
    components=[S['resolve'](go.assets_file,c['component']) for c in go.read_typetree()['m_Component']]
    yield components
    tr=next(c for c in components if c.type.name in ['Transform','RectTransform'])
    for ptr in tr.read_typetree()['m_Children']:
        child=S['resolve'](tr.assets_file,ptr)
        yield from walk(S['resolve'](child.assets_file,child.read_typetree()['m_GameObject']))
for x in plan:
    if x['type']!=0:continue
    paths=[p for p in x['apk'] if p.endswith('.prefab')]
    if not paths:
        print('NO APK PREFAB',x['id'],x['name'],x['key']);continue
    p=paths[0];counts=collections.Counter();materials=[]
    for comps in walk(S['obj'](p,'GameObject')):
        counts.update(c.type.name for c in comps)
        for c in comps:
            if c.type.name not in ['MeshRenderer','SkinnedMeshRenderer','ParticleSystemRenderer']:continue
            for ptr in c.read_typetree()['m_Materials']:
                m=S['resolve'](c.assets_file,ptr);d=m.read_typetree();sh=S['resolve'](m.assets_file,d['m_Shader'])
                materials.append(dict(name=m.peek_name(),shader=sh.read_typetree()['m_ParsedForm']['m_Name'],properties=d['m_SavedProperties']))
    row=dict(id=x['id'],name=x['name'],key=x['key'],path=p,components=dict(counts),materials=materials)
    report.append(row)
    print(x['key'],dict(counts),list(set(m['shader'] for m in materials)))
(R/'build/wardrobe-r16/models-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),'utf8')
print('MISSING',[(x['type'],x['id'],x['name'],x['key']) for x in plan if x['type'] in [0,5,7,13] and not x['native']])
