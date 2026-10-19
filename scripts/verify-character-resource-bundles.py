"""Check both delivery platforms, including every declared outfit and voice bank."""
from pathlib import Path
import gc, hashlib, json, sys
R=Path(__file__).resolve().parent.parent
sys.path.insert(0,str(R/'tools/asset-inspector'))
import UnityPy
B=R/'build/character-resources-v1'
directory=json.loads((R/'unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json').read_text('utf8'))
roles={r['key']:r for r in directory['roles']}
checks=[]
for platform in ['android','windows']:
    catalog=json.loads((B/('catalog-'+platform+'.json')).read_text('utf8'))
    for p in catalog['packs']:
        if not p['available']:continue
        path=B/platform/(p['key']+'-v1-'+platform+'.bundle')
        env=UnityPy.load(str(path))
        bundle=next(o.read_typetree()for o in env.objects if o.type.name=='AssetBundle')
        names={a for a,_ in bundle['m_Container']}
        assert len(names)==len(bundle['m_Container'])
        for skin in roles[p['key']]['skins']:
            for kind in ['full','bighead','smallhead']:assert f'skins/{skin["id"]}/{kind}'in names,(p['key'],skin['id'],kind)
            for part in skin['parts']if skin['dynamic']else[]:
                assert part.lower()+'/skeleton'in names and part.lower()+'/atlas'in names,(p['key'],part)
        voices=[s for s in names if s.startswith('voice/'+p['key']+'/')]
        emojis=[s for s in names if s.startswith('emoji/'+p['key']+'/')]
        assert voices and emojis,p['key']
        digest=hashlib.sha256(path.read_bytes()).hexdigest()
        if not p['builtin']:assert p['sha256']==digest and p['bytes']==path.stat().st_size
        checks.append(dict(platform=platform,key=p['key'],skins=len(roles[p['key']]['skins']),addresses=len(names),voices=len(voices),emojis=len(emojis),bytes=path.stat().st_size,sha256=digest))
        print('BUNDLE VERIFIED',platform,p['name'],len(names),'assets',flush=True)
        del env,bundle;gc.collect()
assert len(checks)==26
(B/'bundle-verification.json').write_text(json.dumps(dict(bundleChecks=checks,androidDeviceTested=False),ensure_ascii=False,indent=2)+'\n','utf8')
print('BUNDLE VERIFICATION PASS',len(checks),flush=True)
